using Hdt.Core.Models;
using Hdt.Core.Validation;

namespace Hdt.Core.Services;

public sealed class TemporalPhaseStackService
{
    private static readonly string[] RequiredChannels = ["pressure", "drift", "bloom"];
    private static readonly string[] ReservedChannels = ["force", "opacity", "hue", "saturation"];
    private static readonly HashSet<string> SupportedAggregationModes = ["latest", "mean", "delta"];
    private static readonly HashSet<ValidationErrorCode> TrustFailureCodes =
    [
        ValidationErrorCode.DigestMismatch,
        ValidationErrorCode.HashMismatch,
        ValidationErrorCode.SignatureMismatch
    ];
    private static readonly HashSet<ValidationErrorCode> TemporalFailureCodes =
    [
        ValidationErrorCode.InvalidEventSlice,
        ValidationErrorCode.InvalidPhaseSlice,
        ValidationErrorCode.InvalidPhasePolicy,
        ValidationErrorCode.InvalidOpticalChannels,
        ValidationErrorCode.InvalidUniverseLayer,
        ValidationErrorCode.InvalidGluingManifest,
        ValidationErrorCode.InvalidProjectionRules,
        ValidationErrorCode.InvalidLegibilityProfile,
        ValidationErrorCode.MissingSidecar
    ];

    private readonly HopngArtifactLoader _loader = new();

    public PhaseStackRenderResult Render(string path, string view = "prime", int? rawSliceHorizon = null)
    {
        var artifact = _loader.Load(path);
        return Render(artifact, new ValidationResult(), view, rawSliceHorizon);
    }

    public PhaseStackRenderResult Render(
        LoadedHopngArtifact artifact,
        ValidationResult validationResult,
        string view = "prime",
        int? rawSliceHorizon = null)
    {
        var hasPhase3 = HasPhase3Sidecars(artifact);
        var issues = new List<string>();
        var payloadMode = string.Equals(view, "privileged", StringComparison.OrdinalIgnoreCase)
            ? "privileged"
            : "prime";
        var horizon = rawSliceHorizon ?? artifact.PhasePolicy?.ComparisonHorizonRawSlices ?? 0;

        if (!hasPhase3)
        {
            return new PhaseStackRenderResult
            {
                ArtifactId = artifact.Manifest.ArtifactId,
                Status = TemporalStackStatus.Unsupported,
                View = payloadMode,
                PayloadMode = "phase3-not-declared",
                HorizonRawSlices = horizon,
                Issues = ["Artifact does not declare Phase 3 temporal sidecars."],
                ValidationIssues = [.. validationResult.Errors]
            };
        }

        if (validationResult.Errors.Any(error => TrustFailureCodes.Contains(error.Code)))
        {
            return new PhaseStackRenderResult
            {
                ArtifactId = artifact.Manifest.ArtifactId,
                Status = TemporalStackStatus.Unsupported,
                View = payloadMode,
                PayloadMode = payloadMode,
                HorizonRawSlices = horizon,
                Issues = ["Trust validation failed, so the temporal stack cannot be treated as lawful."],
                ValidationIssues = [.. validationResult.Errors]
            };
        }

        if (artifact.EventSliceSet is null || artifact.PhaseSliceSet is null || artifact.PhasePolicy is null || artifact.OpticalChannelsDefinition is null)
        {
            return new PhaseStackRenderResult
            {
                ArtifactId = artifact.Manifest.ArtifactId,
                Status = TemporalStackStatus.StructurallyIncomplete,
                View = payloadMode,
                PayloadMode = payloadMode,
                HorizonRawSlices = horizon,
                Issues = ["Phase 3 temporal rendering requires event slices, phase slices, phase policy, and optical channel declarations."],
                ValidationIssues = [.. validationResult.Errors]
            };
        }

        var observedSet = artifact.EventSliceSet.ObservedSet;
        var expectedPhaseSlices = DeriveExpectedPhaseSlices(artifact);
        var coverage = HasRequiredChannelCoverage(artifact.EventSliceSet, artifact.OpticalChannelsDefinition);
        var sliceSummaries = BuildSliceSummaries(artifact.EventSliceSet, artifact.PhaseSliceSet);
        var stateSummaries = BuildStateSummaries(artifact, artifact.PhaseSliceSet.Slices);
        var driftFlags = BuildDriftFlags(artifact.PhaseSliceSet.Slices, horizon, issues);
        var topologyFlags = BuildTopologyFlags(artifact, artifact.PhaseSliceSet.Slices);
        var eventIssues = ValidateTemporalContracts(artifact)
            .Select(issue => issue.Message)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var issue in eventIssues)
        {
            if (!issues.Contains(issue, StringComparer.Ordinal))
            {
                issues.Add(issue);
            }
        }

        if (expectedPhaseSlices.Count == 0)
        {
            issues.Add("Phase derivation did not yield any phase slices from the current event windows.");
        }

        var status = DetermineStatus(validationResult, issues);

        return new PhaseStackRenderResult
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            Status = status,
            View = payloadMode,
            ObservedDurationMs = observedSet.ObservedDurationMs,
            BaseRawCadenceMs = observedSet.BaseSliceCadenceMs,
            RawSliceCount = observedSet.RawSliceCount,
            ObservedEventCount = observedSet.ObservedEventCount,
            EventSliceCount = artifact.EventSliceSet.Slices.Count,
            PhaseSliceCount = artifact.PhaseSliceSet.Slices.Count,
            GroupingSummary = BuildGroupingSummary(artifact.PhasePolicy),
            HorizonRawSlices = horizon,
            HorizonDurationMs = horizon * artifact.PhasePolicy.RawCadenceMs,
            RequiredChannelCoverage = coverage,
            DriftFlags = driftFlags,
            TopologyChangeFlags = topologyFlags,
            PayloadMode = payloadMode == "privileged" ? artifact.PhasePolicy.PrivilegedInspectionMode : artifact.PhasePolicy.PrimeSafeInspectionMode,
            SliceSummaries = sliceSummaries,
            StateSummaries = stateSummaries,
            EventSlices = payloadMode == "privileged" ? artifact.EventSliceSet : null,
            PhaseSlices = payloadMode == "privileged" ? artifact.PhaseSliceSet : null,
            Issues = issues,
            ValidationIssues = [.. validationResult.Errors]
        };
    }

    public List<ValidationIssue> ValidateTemporalContracts(LoadedHopngArtifact artifact)
    {
        var issues = new List<ValidationIssue>();
        if (!HasPhase3Sidecars(artifact))
        {
            return issues;
        }

        if (artifact.UniverseLayerSet is null)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Phase 3 temporal artifacts require Phase 2 universe declarations.", artifact.Layout.ManifestPath));
            return issues;
        }

        if (artifact.GluingManifest is null || artifact.ProjectionRules is null || artifact.LegibilityProfile is null)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Phase 3 temporal artifacts require the full Phase 2 relational contract.", artifact.Layout.ManifestPath));
        }

        if (artifact.EventSliceSet is null)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Phase 3 temporal artifacts must declare an event-slices sidecar.", artifact.Layout.EventSlicePath));
        }

        if (artifact.PhaseSliceSet is null)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Phase 3 temporal artifacts must declare a phase-slices sidecar.", artifact.Layout.PhaseSlicePath));
        }

        if (artifact.PhasePolicy is null)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Phase 3 temporal artifacts must declare a phase-policy sidecar.", artifact.Layout.PhasePolicyPath));
        }

        if (artifact.OpticalChannelsDefinition is null)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Phase 3 temporal artifacts must declare an optical-channels sidecar.", artifact.Layout.OpticalChannelsPath));
        }

        if (issues.Count > 0 || artifact.EventSliceSet is null || artifact.PhaseSliceSet is null || artifact.PhasePolicy is null || artifact.OpticalChannelsDefinition is null)
        {
            return issues;
        }

        var universeIds = artifact.UniverseLayerSet.Universes.Select(universe => universe.UniverseId).ToHashSet(StringComparer.Ordinal);
        ValidateObservedSet(artifact, artifact.EventSliceSet, issues);
        ValidateOpticalChannels(artifact, artifact.OpticalChannelsDefinition, issues);
        ValidatePhasePolicy(artifact, artifact.PhasePolicy, issues);
        ValidateEventSlices(artifact, artifact.EventSliceSet, artifact.PhasePolicy, universeIds, issues);
        ValidateDerivedPhaseSlices(artifact, artifact.EventSliceSet, artifact.PhaseSliceSet, artifact.PhasePolicy, universeIds, issues);

        return issues;
    }

    public List<PhaseSlice> DeriveExpectedPhaseSlices(LoadedHopngArtifact artifact)
    {
        if (artifact.EventSliceSet is null || artifact.PhasePolicy is null)
        {
            return [];
        }

        if (!HasSupportedAggregationPolicies(artifact.PhasePolicy))
        {
            return [];
        }

        var eventSlices = artifact.EventSliceSet.Slices
            .OrderBy(slice => slice.N)
            .ToList();
        var windows = BuildPhaseWindows(eventSlices, artifact.PhasePolicy);
        if (windows.Count == 0)
        {
            return [];
        }

        var derived = new List<PhaseSlice>();
        foreach (var window in windows)
        {
            var first = window[0];
            var last = window[^1];
            var universeIds = window
                .SelectMany(slice => slice.UniverseStates.Keys)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            var universeStates = new Dictionary<string, TemporalUniverseState>(StringComparer.Ordinal);

            foreach (var universeId in universeIds)
            {
                var values = window
                    .Where(slice => slice.UniverseStates.ContainsKey(universeId))
                    .Select(slice => slice.UniverseStates[universeId])
                    .ToList();
                if (values.Count == 0)
                {
                    continue;
                }

                universeStates[universeId] = new TemporalUniverseState
                {
                    Pressure = AggregateChannel(values.Select(value => value.Pressure).ToList(), artifact.PhasePolicy.AggregationPolicies.GetValueOrDefault("pressure", "mean")),
                    Drift = AggregateChannel(values.Select(value => value.Drift).ToList(), artifact.PhasePolicy.AggregationPolicies.GetValueOrDefault("drift", "delta")),
                    Bloom = AggregateChannel(values.Select(value => value.Bloom).ToList(), artifact.PhasePolicy.AggregationPolicies.GetValueOrDefault("bloom", "latest"))
                };
            }

            var phaseSlice = new PhaseSlice
            {
                PhaseSliceId = $"phase-{last.N}",
                ArtifactId = artifact.Manifest.ArtifactId,
                N = derived.Count,
                TimestampStartUtc = first.TimestampStartUtc,
                TimestampEndUtc = last.TimestampEndUtc,
                SourceEventSliceIds = window.Select(slice => slice.EventSliceId).ToList(),
                SourceRawStartN = first.RawStartN,
                SourceRawEndN = last.RawEndN,
                DeltaHorizon = artifact.PhasePolicy.ComparisonHorizonRawSlices,
                DeltaHorizonMs = artifact.PhasePolicy.ComparisonHorizonRawSlices * artifact.PhasePolicy.RawCadenceMs,
                UniverseStates = universeStates
            };

            derived.Add(phaseSlice with
            {
                SliceDigest = TemporalSliceDigestService.ComputePhaseSliceDigest(phaseSlice)
            });
        }

        return derived;
    }

    public static bool HasPhase3Sidecars(LoadedHopngArtifact artifact) =>
        artifact.EventSliceSet is not null
        || artifact.PhaseSliceSet is not null
        || artifact.PhasePolicy is not null
        || artifact.OpticalChannelsDefinition is not null
        || artifact.Manifest.Sidecars.Any(sidecar => sidecar.Role is "event-slices" or "phase-slices" or "phase-policy" or "optical-channels");

    private static TemporalStackStatus DetermineStatus(ValidationResult validationResult, IReadOnlyCollection<string> issues)
    {
        if (validationResult.Errors.Any(error => TrustFailureCodes.Contains(error.Code)))
        {
            return TemporalStackStatus.Unsupported;
        }

        if (validationResult.Errors.Any(error => TemporalFailureCodes.Contains(error.Code)) || issues.Count > 0)
        {
            return TemporalStackStatus.StructurallyIncomplete;
        }

        return TemporalStackStatus.LawfullyDerived;
    }

    private static void ValidateObservedSet(LoadedHopngArtifact artifact, EventSliceSet eventSliceSet, List<ValidationIssue> issues)
    {
        var observedSet = eventSliceSet.ObservedSet;
        if (observedSet.ObservedDurationMs <= 0 || observedSet.BaseSliceCadenceMs <= 0)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Observed duration and base slice cadence must be positive.", artifact.Layout.EventSlicePath));
            return;
        }

        if (observedSet.ObservedDurationMs % observedSet.BaseSliceCadenceMs != 0)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Observed duration must divide evenly by the base slice cadence.", artifact.Layout.EventSlicePath));
        }

        var expectedRawSliceCount = observedSet.ObservedDurationMs / observedSet.BaseSliceCadenceMs;
        if (expectedRawSliceCount != observedSet.RawSliceCount)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Observed duration and cadence do not match the declared raw slice count.", artifact.Layout.EventSlicePath));
        }

        if (!string.Equals(observedSet.EventGroupingMode, "fixed_raw_count", StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Milestone 1 only supports fixed_raw_count event grouping.", artifact.Layout.EventSlicePath));
        }

        if (observedSet.EventGroupingSizeRawSlices <= 0)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Observed-set event grouping size must be positive.", artifact.Layout.EventSlicePath));
        }

        if (string.IsNullOrWhiteSpace(observedSet.PrimeSafeInspectionMode) || string.IsNullOrWhiteSpace(observedSet.DataCustodyMode))
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Observed set must declare inspection and custody modes.", artifact.Layout.EventSlicePath));
        }

        foreach (var reference in observedSet.ProtectedEvidenceRefs)
        {
            ValidateEvidenceReference(reference, artifact.Layout.EventSlicePath, issues);
        }
    }

    private static void ValidateOpticalChannels(LoadedHopngArtifact artifact, OpticalChannelsDefinition opticalChannels, List<ValidationIssue> issues)
    {
        foreach (var requiredChannel in RequiredChannels)
        {
            if (!opticalChannels.RequiredChannels.Contains(requiredChannel, StringComparer.Ordinal))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidOpticalChannels, $"Required channel '{requiredChannel}' is missing from the optical channel declaration.", artifact.Layout.OpticalChannelsPath));
            }

            if (!opticalChannels.Channels.Any(channel => string.Equals(channel.ChannelId, requiredChannel, StringComparison.Ordinal) && channel.Required))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidOpticalChannels, $"Required channel '{requiredChannel}' must have a required channel definition.", artifact.Layout.OpticalChannelsPath));
            }
        }

        foreach (var reservedChannel in ReservedChannels)
        {
            if (!opticalChannels.ReservedChannels.Contains(reservedChannel, StringComparer.Ordinal))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidOpticalChannels, $"Reserved channel '{reservedChannel}' is missing from the optical channel declaration.", artifact.Layout.OpticalChannelsPath));
            }
        }
    }

    private static void ValidatePhasePolicy(LoadedHopngArtifact artifact, PhasePolicy phasePolicy, List<ValidationIssue> issues)
    {
        if (phasePolicy.RawCadenceMs <= 0
            || phasePolicy.EventGroupingSizeRawSlices <= 0
            || phasePolicy.PhaseWindowDurationMs <= 0
            || phasePolicy.MaxPhaseWindowSpanMs <= 0
            || phasePolicy.ComparisonHorizonRawSlices <= 0)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "Phase policy numeric values must be positive.", artifact.Layout.PhasePolicyPath));
        }

        if (!string.Equals(phasePolicy.EventGroupingMode, "fixed_raw_count", StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "Milestone 1 only supports fixed_raw_count event grouping.", artifact.Layout.PhasePolicyPath));
        }

        if (string.Equals(phasePolicy.PhaseWindowMode, "fixed_event_count", StringComparison.Ordinal))
        {
            if (phasePolicy.PhaseWindowSizeEventSlices <= 0)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "fixed_event_count phase windows require a positive phaseWindowSizeEventSlices value.", artifact.Layout.PhasePolicyPath));
            }

            var expectedDurationMs = phasePolicy.RawCadenceMs * phasePolicy.EventGroupingSizeRawSlices * phasePolicy.PhaseWindowSizeEventSlices;
            if (phasePolicy.PhaseWindowDurationMs != expectedDurationMs)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "fixed_event_count phase windows must declare a duration that matches the raw cadence and event grouping basis.", artifact.Layout.PhasePolicyPath));
            }
        }
        else if (string.Equals(phasePolicy.PhaseWindowMode, "duration_ms", StringComparison.Ordinal))
        {
            if (phasePolicy.PhaseWindowDurationMs <= 0)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "duration_ms phase windows require a positive phaseWindowDurationMs value.", artifact.Layout.PhasePolicyPath));
            }
        }
        else
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "Phase policy only supports fixed_event_count and duration_ms phase windows.", artifact.Layout.PhasePolicyPath));
        }

        if (phasePolicy.MaxPhaseWindowSpanMs < phasePolicy.PhaseWindowDurationMs)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "Max phase-window span must be greater than or equal to the declared phase-window duration.", artifact.Layout.PhasePolicyPath));
        }

        foreach (var requiredChannel in RequiredChannels)
        {
            if (!phasePolicy.AggregationPolicies.TryGetValue(requiredChannel, out var mode) || !SupportedAggregationModes.Contains(mode))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, $"Channel '{requiredChannel}' must declare a supported aggregation mode.", artifact.Layout.PhasePolicyPath));
            }
        }

        if (phasePolicy.StateThresholds is not null)
        {
            ValidateStateThresholdPolicy(artifact, phasePolicy.StateThresholds, issues);
        }
    }

    private static void ValidateEventSlices(
        LoadedHopngArtifact artifact,
        EventSliceSet eventSliceSet,
        PhasePolicy phasePolicy,
        HashSet<string> universeIds,
        List<ValidationIssue> issues)
    {
        if (eventSliceSet.Slices.Count == 0)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "At least one event slice is required for Phase 3 artifacts.", artifact.Layout.EventSlicePath));
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var seenDigests = new HashSet<string>(StringComparer.Ordinal);
        EventSlice? previous = null;

        foreach (var slice in eventSliceSet.Slices.OrderBy(slice => slice.N))
        {
            if (string.IsNullOrWhiteSpace(slice.EventSliceId) || !seenIds.Add(slice.EventSliceId))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Event slice ids must be present and unique.", artifact.Layout.EventSlicePath));
            }

            if (string.IsNullOrWhiteSpace(slice.SliceDigest) || !seenDigests.Add(slice.SliceDigest))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Event slice digests must be present and unique.", artifact.Layout.EventSlicePath));
            }

            if (slice.TimestampStartUtc > slice.TimestampEndUtc)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, $"Event slice '{slice.EventSliceId}' must have an ordered timestamp range.", artifact.Layout.EventSlicePath));
            }

            if (slice.RawStartN < 0 || slice.RawEndN < slice.RawStartN || slice.RawEndN >= eventSliceSet.ObservedSet.RawSliceCount)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, $"Event slice '{slice.EventSliceId}' declares an invalid raw slice range.", artifact.Layout.EventSlicePath));
            }

            if (slice.RawSliceSpan != slice.RawEndN - slice.RawStartN + 1)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, $"Event slice '{slice.EventSliceId}' raw slice span does not match its declared range.", artifact.Layout.EventSlicePath));
            }

            if (slice.RawSliceSpan != phasePolicy.EventGroupingSizeRawSlices)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, $"Event slice '{slice.EventSliceId}' does not match the policy raw grouping size.", artifact.Layout.EventSlicePath));
            }

            var eventTimestampSpanMs = GetTimestampSpanMs(slice.TimestampStartUtc, slice.TimestampEndUtc);
            var expectedEventTimestampSpanMs = slice.RawSliceSpan * phasePolicy.RawCadenceMs;
            if (eventTimestampSpanMs != expectedEventTimestampSpanMs)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, $"Event slice '{slice.EventSliceId}' timestamp span does not match its raw cadence basis.", artifact.Layout.EventSlicePath));
            }

            if (previous is not null)
            {
                if (slice.N != previous.N + 1)
                {
                    issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Event slices must be strictly ordered by n without gaps.", artifact.Layout.EventSlicePath));
                }

                if (slice.RawStartN != previous.RawEndN + 1)
                {
                    issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Event slices must use contiguous raw windows.", artifact.Layout.EventSlicePath));
                }
            }
            else if (slice.N != 0)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "The first event slice must start at n = 0.", artifact.Layout.EventSlicePath));
            }

            foreach (var universeState in slice.UniverseStates)
            {
                if (!universeIds.Contains(universeState.Key))
                {
                    issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, $"Event slice '{slice.EventSliceId}' references unknown universe '{universeState.Key}'.", artifact.Layout.EventSlicePath));
                }
            }

            if (slice.UniverseStates.Count == 0)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, $"Event slice '{slice.EventSliceId}' must declare at least one participating universe state.", artifact.Layout.EventSlicePath));
            }

            foreach (var reference in slice.ProtectedEvidenceRefs)
            {
                ValidateEvidenceReference(reference, artifact.Layout.EventSlicePath, issues);
            }

            var expectedDigest = TemporalSliceDigestService.ComputeEventSliceDigest(slice);
            if (!string.Equals(expectedDigest, slice.SliceDigest, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, $"Event slice '{slice.EventSliceId}' digest does not match its canonical payload.", artifact.Layout.EventSlicePath));
            }

            previous = slice;
        }
    }

    private void ValidateDerivedPhaseSlices(
        LoadedHopngArtifact artifact,
        EventSliceSet eventSliceSet,
        PhaseSliceSet phaseSliceSet,
        PhasePolicy phasePolicy,
        HashSet<string> universeIds,
        List<ValidationIssue> issues)
    {
        if (phaseSliceSet.Slices.Count == 0)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, "At least one phase slice is required for Phase 3 artifacts.", artifact.Layout.PhaseSlicePath));
            return;
        }

        var expectedPhaseSlices = DeriveExpectedPhaseSlices(artifact);
        if (expectedPhaseSlices.Count != phaseSliceSet.Slices.Count)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, "Stored phase slice count does not match the deterministic derivation result.", artifact.Layout.PhaseSlicePath));
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var seenDigests = new HashSet<string>(StringComparer.Ordinal);
        var eventSliceIds = eventSliceSet.Slices.Select(slice => slice.EventSliceId).ToHashSet(StringComparer.Ordinal);
        PhaseSlice? previous = null;

        foreach (var pair in phaseSliceSet.Slices.OrderBy(slice => slice.N).Select((slice, index) => (slice, index)))
        {
            var slice = pair.slice;
            if (string.IsNullOrWhiteSpace(slice.PhaseSliceId) || !seenIds.Add(slice.PhaseSliceId))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, "Phase slice ids must be present and unique.", artifact.Layout.PhaseSlicePath));
            }

            if (string.IsNullOrWhiteSpace(slice.SliceDigest) || !seenDigests.Add(slice.SliceDigest))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, "Phase slice digests must be present and unique.", artifact.Layout.PhaseSlicePath));
            }

            if (slice.TimestampStartUtc > slice.TimestampEndUtc)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, $"Phase slice '{slice.PhaseSliceId}' must have an ordered timestamp range.", artifact.Layout.PhaseSlicePath));
            }

            if (string.Equals(phasePolicy.PhaseWindowMode, "fixed_event_count", StringComparison.Ordinal)
                && slice.SourceEventSliceIds.Count != phasePolicy.PhaseWindowSizeEventSlices)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, $"Phase slice '{slice.PhaseSliceId}' does not match the policy event window size.", artifact.Layout.PhaseSlicePath));
            }
            else if (string.Equals(phasePolicy.PhaseWindowMode, "duration_ms", StringComparison.Ordinal)
                && slice.SourceEventSliceIds.Count == 0)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, $"Phase slice '{slice.PhaseSliceId}' must declare at least one source event slice.", artifact.Layout.PhaseSlicePath));
            }

            if (slice.SourceRawStartN > slice.SourceRawEndN)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, $"Phase slice '{slice.PhaseSliceId}' declares an invalid source raw range.", artifact.Layout.PhaseSlicePath));
            }

            var phaseTimestampSpanMs = GetTimestampSpanMs(slice.TimestampStartUtc, slice.TimestampEndUtc);
            if (phaseTimestampSpanMs != phasePolicy.PhaseWindowDurationMs)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, $"Phase slice '{slice.PhaseSliceId}' timestamp span does not match the policy phase-window duration.", artifact.Layout.PhaseSlicePath));
            }

            if (phaseTimestampSpanMs > phasePolicy.MaxPhaseWindowSpanMs)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, $"Phase slice '{slice.PhaseSliceId}' exceeds the policy max phase-window span.", artifact.Layout.PhaseSlicePath));
            }

            foreach (var sourceEventSliceId in slice.SourceEventSliceIds)
            {
                if (!eventSliceIds.Contains(sourceEventSliceId))
                {
                    issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, $"Phase slice '{slice.PhaseSliceId}' references unknown event slice '{sourceEventSliceId}'.", artifact.Layout.PhaseSlicePath));
                }
            }

            if (previous is not null)
            {
                if (slice.N != previous.N + 1)
                {
                    issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, "Phase slices must be strictly ordered by n without gaps.", artifact.Layout.PhaseSlicePath));
                }
            }
            else if (slice.N != 0)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, "The first phase slice must start at n = 0.", artifact.Layout.PhaseSlicePath));
            }

            foreach (var universeState in slice.UniverseStates)
            {
                if (!universeIds.Contains(universeState.Key))
                {
                    issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, $"Phase slice '{slice.PhaseSliceId}' references unknown universe '{universeState.Key}'.", artifact.Layout.PhaseSlicePath));
                }
            }

            var expectedDigest = TemporalSliceDigestService.ComputePhaseSliceDigest(slice);
            if (!string.Equals(expectedDigest, slice.SliceDigest, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, $"Phase slice '{slice.PhaseSliceId}' digest does not match its canonical payload.", artifact.Layout.PhaseSlicePath));
            }

            if (pair.index < expectedPhaseSlices.Count)
            {
                var expected = expectedPhaseSlices[pair.index];
                if (!string.Equals(expected.SliceDigest, slice.SliceDigest, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhaseSlice, $"Phase slice '{slice.PhaseSliceId}' does not match the deterministic derivation result.", artifact.Layout.PhaseSlicePath));
                }
            }

            previous = slice;
        }
    }

    private static bool HasRequiredChannelCoverage(EventSliceSet eventSliceSet, OpticalChannelsDefinition opticalChannels) =>
        RequiredChannels.All(requiredChannel =>
            opticalChannels.RequiredChannels.Contains(requiredChannel, StringComparer.Ordinal)
            && eventSliceSet.Slices.All(slice => slice.UniverseStates.Values.All(state => HasChannel(requiredChannel, state))));

    private static bool HasChannel(string channelId, TemporalUniverseState state) =>
        channelId switch
        {
            "pressure" => true,
            "drift" => true,
            "bloom" => true,
            "force" => state.Force.HasValue,
            "opacity" => state.Opacity.HasValue,
            "hue" => state.Hue.HasValue,
            "saturation" => state.Saturation.HasValue,
            _ => false
        };

    private static double AggregateChannel(IReadOnlyList<double> values, string mode) =>
        mode switch
        {
            "latest" => values[^1],
            "mean" => values.Average(),
            "delta" => values.Count == 1 ? 0d : values[^1] - values[0],
            _ => throw new InvalidOperationException($"Unsupported aggregation mode '{mode}'.")
        };

    private static bool HasSupportedAggregationPolicies(PhasePolicy phasePolicy) =>
        RequiredChannels.All(requiredChannel =>
            phasePolicy.AggregationPolicies.TryGetValue(requiredChannel, out var mode)
            && SupportedAggregationModes.Contains(mode));

    private static List<TemporalSliceSummary> BuildSliceSummaries(EventSliceSet eventSliceSet, PhaseSliceSet phaseSliceSet)
    {
        var summaries = new List<TemporalSliceSummary>();

        summaries.AddRange(eventSliceSet.Slices.Select(slice => new TemporalSliceSummary
        {
            Family = "event",
            SliceId = slice.EventSliceId,
            N = slice.N,
            TimestampStartUtc = slice.TimestampStartUtc,
            TimestampEndUtc = slice.TimestampEndUtc,
            TimestampSpanMs = GetTimestampSpanMs(slice.TimestampStartUtc, slice.TimestampEndUtc),
            RawRangeSummary = $"{slice.RawStartN}-{slice.RawEndN}"
        }));

        summaries.AddRange(phaseSliceSet.Slices.Select(slice => new TemporalSliceSummary
        {
            Family = "phase",
            SliceId = slice.PhaseSliceId,
            N = slice.N,
            TimestampStartUtc = slice.TimestampStartUtc,
            TimestampEndUtc = slice.TimestampEndUtc,
            TimestampSpanMs = GetTimestampSpanMs(slice.TimestampStartUtc, slice.TimestampEndUtc),
            RawRangeSummary = $"{slice.SourceRawStartN}-{slice.SourceRawEndN}"
        }));

        return summaries;
    }

    private static void ValidateStateThresholdPolicy(
        LoadedHopngArtifact artifact,
        TemporalStateThresholdPolicy thresholds,
        List<ValidationIssue> issues)
    {
        if (thresholds.RisingPressureMin <= 0d
            || thresholds.DriftingAbsoluteMin <= 0d
            || thresholds.PropagatingBloomMin <= 0d
            || thresholds.RupturePressureMin <= 0d
            || thresholds.RuptureDriftAbsoluteMin <= 0d
            || thresholds.DirectionDriftAbsoluteMin <= 0d)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "State-threshold policy values must be positive.", artifact.Layout.PhasePolicyPath));
        }

        if (thresholds.RupturePressureMin < thresholds.RisingPressureMin)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "Rupture pressure threshold must be greater than or equal to the rising-pressure threshold.", artifact.Layout.PhasePolicyPath));
        }

        if (thresholds.RuptureDriftAbsoluteMin < thresholds.DriftingAbsoluteMin)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "Rupture drift threshold must be greater than or equal to the drifting threshold.", artifact.Layout.PhasePolicyPath));
        }

        if (thresholds.ForcePressureWeight <= 0d
            || thresholds.ForceDriftWeight <= 0d
            || thresholds.ForceBloomWeight <= 0d
            || thresholds.ForceTopologyBonus < 0d)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "Derived-force weights must keep positive channel weights and a non-negative topology bonus.", artifact.Layout.PhasePolicyPath));
        }

        if (thresholds.ForcePressureWeight + thresholds.ForceDriftWeight + thresholds.ForceBloomWeight <= 0d)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "Derived-force weights must contribute positive signal.", artifact.Layout.PhasePolicyPath));
        }
    }

    private static List<TemporalStateSummary> BuildStateSummaries(LoadedHopngArtifact artifact, IReadOnlyList<PhaseSlice> phaseSlices)
    {
        if (artifact.PhasePolicy is null)
        {
            return [];
        }

        var thresholds = artifact.PhasePolicy.StateThresholds ?? new TemporalStateThresholdPolicy();
        var summaries = new List<TemporalStateSummary>();

        for (var index = 0; index < phaseSlices.Count; index++)
        {
            var slice = phaseSlices[index];
            if (slice.UniverseStates.Count == 0)
            {
                summaries.Add(new TemporalStateSummary
                {
                    SliceId = slice.PhaseSliceId,
                    N = slice.N,
                    StateClass = "StructurallyIncomplete",
                    DerivedForceMagnitude = 0d,
                    DerivedForceDirection = "neutral",
                    BasisSignals = ["Phase slice does not declare any participating universe states."]
                });
                continue;
            }

            var averagePressure = slice.UniverseStates.Values.Average(state => state.Pressure);
            var averageSignedDrift = slice.UniverseStates.Values.Average(state => state.Drift);
            var averageAbsoluteDrift = slice.UniverseStates.Values.Average(state => Math.Abs(state.Drift));
            var averageBloom = slice.UniverseStates.Values.Average(state => state.Bloom);
            var hasTopologyChange = index > 0 && HasTopologyChangeBetween(artifact, phaseSlices[index - 1], slice);
            var derivedForce = Math.Round(
                (averagePressure * thresholds.ForcePressureWeight)
                + (averageAbsoluteDrift * thresholds.ForceDriftWeight)
                + (averageBloom * thresholds.ForceBloomWeight)
                + (hasTopologyChange ? thresholds.ForceTopologyBonus : 0d),
                6);
            var direction = averageSignedDrift >= thresholds.DirectionDriftAbsoluteMin
                ? "positive"
                : averageSignedDrift <= -thresholds.DirectionDriftAbsoluteMin
                    ? "negative"
                    : "neutral";
            var stateClass = ClassifyState(averagePressure, averageAbsoluteDrift, averageBloom, hasTopologyChange, thresholds);
            var basisSignals = BuildBasisSignals(averagePressure, averageAbsoluteDrift, averageBloom, hasTopologyChange, thresholds);

            summaries.Add(new TemporalStateSummary
            {
                SliceId = slice.PhaseSliceId,
                N = slice.N,
                StateClass = stateClass,
                DerivedForceMagnitude = derivedForce,
                DerivedForceDirection = direction,
                BasisSignals = basisSignals
            });
        }

        return summaries;
    }

    private static List<string> BuildDriftFlags(IReadOnlyList<PhaseSlice> phaseSlices, int rawSliceHorizon, List<string> issues)
    {
        var flags = new HashSet<string>(StringComparer.Ordinal);
        if (phaseSlices.Count == 0)
        {
            return [];
        }

        for (var index = 1; index < phaseSlices.Count; index++)
        {
            AddDriftFlags(phaseSlices[index - 1], phaseSlices[index], "adjacent", flags);

            var horizonAnchor = phaseSlices
                .Take(index)
                .LastOrDefault(candidate => rawSliceHorizon > 0 && phaseSlices[index].SourceRawEndN - candidate.SourceRawEndN >= rawSliceHorizon);
            if (rawSliceHorizon > 0 && horizonAnchor is null)
            {
                issues.Add($"Phase slice '{phaseSlices[index].PhaseSliceId}' does not yet have a prior slice that satisfies raw horizon {rawSliceHorizon}.");
            }
            else if (horizonAnchor is not null)
            {
                AddDriftFlags(horizonAnchor, phaseSlices[index], $"h={rawSliceHorizon}", flags);
            }
        }

        return flags.OrderBy(flag => flag, StringComparer.Ordinal).ToList();
    }

    private static void AddDriftFlags(PhaseSlice anchor, PhaseSlice current, string mode, HashSet<string> flags)
    {
        foreach (var universeId in current.UniverseStates.Keys.Intersect(anchor.UniverseStates.Keys, StringComparer.Ordinal))
        {
            var driftDelta = current.UniverseStates[universeId].Drift - anchor.UniverseStates[universeId].Drift;
            if (Math.Abs(driftDelta) > 0d)
            {
                flags.Add($"Drift changed for universe '{universeId}' between phase slices '{anchor.PhaseSliceId}' and '{current.PhaseSliceId}' ({mode}).");
            }
        }
    }

    private static List<string> BuildTopologyFlags(LoadedHopngArtifact artifact, IReadOnlyList<PhaseSlice> phaseSlices)
    {
        var flags = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 1; index < phaseSlices.Count; index++)
        {
            var prior = phaseSlices[index - 1];
            var current = phaseSlices[index];
            var priorUniverses = prior.UniverseStates.Keys.ToHashSet(StringComparer.Ordinal);
            var currentUniverses = current.UniverseStates.Keys.ToHashSet(StringComparer.Ordinal);

            if (!priorUniverses.SetEquals(currentUniverses))
            {
                flags.Add($"Universe participation changed between phase slices '{prior.PhaseSliceId}' and '{current.PhaseSliceId}'.");
            }

            var priorRelations = ResolveParticipatingRelations(artifact, priorUniverses);
            var currentRelations = ResolveParticipatingRelations(artifact, currentUniverses);
            if (!priorRelations.SetEquals(currentRelations))
            {
                flags.Add($"Gluing participation changed between phase slices '{prior.PhaseSliceId}' and '{current.PhaseSliceId}'.");
            }

            var priorProjectionRules = ResolveParticipatingProjectionRules(artifact, priorUniverses);
            var currentProjectionRules = ResolveParticipatingProjectionRules(artifact, currentUniverses);
            if (!priorProjectionRules.SetEquals(currentProjectionRules))
            {
                flags.Add($"Projection-rule participation changed between phase slices '{prior.PhaseSliceId}' and '{current.PhaseSliceId}'.");
            }
        }

        return flags.OrderBy(flag => flag, StringComparer.Ordinal).ToList();
    }

    private static bool HasTopologyChangeBetween(LoadedHopngArtifact artifact, PhaseSlice prior, PhaseSlice current)
    {
        var priorUniverses = prior.UniverseStates.Keys.ToHashSet(StringComparer.Ordinal);
        var currentUniverses = current.UniverseStates.Keys.ToHashSet(StringComparer.Ordinal);
        if (!priorUniverses.SetEquals(currentUniverses))
        {
            return true;
        }

        var priorRelations = ResolveParticipatingRelations(artifact, priorUniverses);
        var currentRelations = ResolveParticipatingRelations(artifact, currentUniverses);
        if (!priorRelations.SetEquals(currentRelations))
        {
            return true;
        }

        var priorProjectionRules = ResolveParticipatingProjectionRules(artifact, priorUniverses);
        var currentProjectionRules = ResolveParticipatingProjectionRules(artifact, currentUniverses);
        return !priorProjectionRules.SetEquals(currentProjectionRules);
    }

    private static string ClassifyState(
        double averagePressure,
        double averageAbsoluteDrift,
        double averageBloom,
        bool hasTopologyChange,
        TemporalStateThresholdPolicy thresholds) =>
        (averagePressure, averageAbsoluteDrift, averageBloom, hasTopologyChange) switch
        {
            (_, _, _, true) when averagePressure >= thresholds.RupturePressureMin && averageAbsoluteDrift >= thresholds.RuptureDriftAbsoluteMin => "RuptureRisk",
            (_, _, _, true) => "Propagating",
            (_, _, _, false) when averagePressure >= thresholds.RupturePressureMin && averageAbsoluteDrift >= thresholds.RuptureDriftAbsoluteMin => "RuptureRisk",
            (_, _, _, false) when averageBloom >= thresholds.PropagatingBloomMin => "Propagating",
            (_, _, _, false) when averageAbsoluteDrift >= thresholds.DriftingAbsoluteMin => "Drifting",
            (_, _, _, false) when averagePressure >= thresholds.RisingPressureMin => "RisingPressure",
            _ => "Stable"
        };

    private static List<string> BuildBasisSignals(
        double averagePressure,
        double averageAbsoluteDrift,
        double averageBloom,
        bool hasTopologyChange,
        TemporalStateThresholdPolicy thresholds)
    {
        var signals = new List<string>();

        if (averagePressure >= thresholds.RupturePressureMin)
        {
            signals.Add($"Average pressure {averagePressure:F3} crossed the rupture threshold {thresholds.RupturePressureMin:F3}.");
        }
        else if (averagePressure >= thresholds.RisingPressureMin)
        {
            signals.Add($"Average pressure {averagePressure:F3} crossed the rising-pressure threshold {thresholds.RisingPressureMin:F3}.");
        }

        if (averageAbsoluteDrift >= thresholds.RuptureDriftAbsoluteMin)
        {
            signals.Add($"Average absolute drift {averageAbsoluteDrift:F3} crossed the rupture threshold {thresholds.RuptureDriftAbsoluteMin:F3}.");
        }
        else if (averageAbsoluteDrift >= thresholds.DriftingAbsoluteMin)
        {
            signals.Add($"Average absolute drift {averageAbsoluteDrift:F3} crossed the drifting threshold {thresholds.DriftingAbsoluteMin:F3}.");
        }

        if (averageBloom >= thresholds.PropagatingBloomMin)
        {
            signals.Add($"Average bloom {averageBloom:F3} crossed the propagating threshold {thresholds.PropagatingBloomMin:F3}.");
        }

        if (hasTopologyChange)
        {
            signals.Add("Topology participation changed across adjacent phase slices.");
        }

        if (signals.Count == 0)
        {
            signals.Add("Phase slice remains within the stable pressure, drift, and bloom envelope.");
        }

        return signals;
    }

    private static string BuildGroupingSummary(PhasePolicy phasePolicy) =>
        string.Equals(phasePolicy.PhaseWindowMode, "duration_ms", StringComparison.Ordinal)
            ? $"{phasePolicy.EventGroupingMode}:{phasePolicy.EventGroupingSizeRawSlices} raw slices -> duration_ms:{phasePolicy.PhaseWindowDurationMs} ms (max {phasePolicy.MaxPhaseWindowSpanMs} ms)"
            : $"{phasePolicy.EventGroupingMode}:{phasePolicy.EventGroupingSizeRawSlices} raw slices -> fixed_event_count:{phasePolicy.PhaseWindowSizeEventSlices} event slices / {phasePolicy.PhaseWindowDurationMs} ms";

    private static List<List<EventSlice>> BuildPhaseWindows(IReadOnlyList<EventSlice> eventSlices, PhasePolicy phasePolicy)
    {
        if (string.Equals(phasePolicy.PhaseWindowMode, "fixed_event_count", StringComparison.Ordinal))
        {
            return BuildFixedEventCountWindows(eventSlices, phasePolicy.PhaseWindowSizeEventSlices);
        }

        if (string.Equals(phasePolicy.PhaseWindowMode, "duration_ms", StringComparison.Ordinal))
        {
            return BuildDurationWindows(eventSlices, phasePolicy.PhaseWindowDurationMs, phasePolicy.MaxPhaseWindowSpanMs);
        }

        return [];
    }

    private static List<List<EventSlice>> BuildFixedEventCountWindows(IReadOnlyList<EventSlice> eventSlices, int windowSize)
    {
        if (windowSize <= 0 || eventSlices.Count < windowSize)
        {
            return [];
        }

        var windows = new List<List<EventSlice>>();
        for (var index = windowSize - 1; index < eventSlices.Count; index++)
        {
            windows.Add(eventSlices
                .Skip(index - windowSize + 1)
                .Take(windowSize)
                .ToList());
        }

        return windows;
    }

    private static List<List<EventSlice>> BuildDurationWindows(IReadOnlyList<EventSlice> eventSlices, int targetDurationMs, int maxSpanMs)
    {
        if (targetDurationMs <= 0 || maxSpanMs <= 0 || eventSlices.Count == 0)
        {
            return [];
        }

        var windows = new List<List<EventSlice>>();
        for (var endIndex = 0; endIndex < eventSlices.Count; endIndex++)
        {
            for (var startIndex = endIndex; startIndex >= 0; startIndex--)
            {
                var spanMs = GetTimestampSpanMs(eventSlices[startIndex].TimestampStartUtc, eventSlices[endIndex].TimestampEndUtc);
                if (spanMs > maxSpanMs)
                {
                    break;
                }

                if (spanMs == targetDurationMs)
                {
                    windows.Add(eventSlices
                        .Skip(startIndex)
                        .Take(endIndex - startIndex + 1)
                        .ToList());
                    break;
                }
            }
        }

        return windows;
    }

    private static int GetTimestampSpanMs(DateTimeOffset start, DateTimeOffset end) =>
        checked((int)Math.Round((end - start).TotalMilliseconds, MidpointRounding.AwayFromZero));

    private static HashSet<string> ResolveParticipatingRelations(LoadedHopngArtifact artifact, HashSet<string> participatingUniverses) =>
        (artifact.GluingManifest?.Relations ?? [])
            .Where(relation => participatingUniverses.Contains(relation.SourceUniverseId) && participatingUniverses.Contains(relation.TargetUniverseId))
            .Select(relation => relation.RelationId)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> ResolveParticipatingProjectionRules(LoadedHopngArtifact artifact, HashSet<string> participatingUniverses) =>
        (artifact.ProjectionRules?.Rules ?? [])
            .Where(rule => participatingUniverses.Contains(rule.SourceUniverseId))
            .Select(rule => rule.RuleId)
            .ToHashSet(StringComparer.Ordinal);

    private static void ValidateEvidenceReference(ProtectedEvidenceReference reference, string path, List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(reference.RefId) || string.IsNullOrWhiteSpace(reference.PointerUri) || !reference.PointerUri.Contains("://", StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Protected evidence references must declare an id and pointer URI.", path));
        }

        if (string.IsNullOrWhiteSpace(reference.DigestSha256))
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidEventSlice, "Protected evidence references must declare a digest.", path));
        }
    }
}
