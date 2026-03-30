using Hdt.Core.Models;
using Hdt.Core.Validation;

namespace Hdt.Core.Services;

public sealed class TemporalPhaseStackService
{
    private static readonly string[] RequiredChannels = ["pressure", "drift", "bloom"];
    private static readonly string[] ReservedChannels = ["force", "opacity", "hue", "saturation"];
    private static readonly HashSet<string> SupportedAggregationModes = ["latest", "mean", "delta"];
    private static readonly HashSet<string> SupportedComparisonHorizonModes = ["raw_slices", "duration_ms"];
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
        var primaryHorizon = ResolvePrimaryComparisonHorizon(artifact.PhasePolicy, rawSliceHorizon);
        var renderedHorizons = BuildRenderedHorizons(artifact.PhasePolicy, primaryHorizon);
        var expectedPhaseSlices = DeriveExpectedPhaseSlices(artifact);
        var coverage = HasRequiredChannelCoverage(artifact.EventSliceSet, artifact.OpticalChannelsDefinition);
        var sliceSummaries = BuildSliceSummaries(artifact.EventSliceSet, artifact.PhaseSliceSet);
        var stateSummaries = BuildStateSummaries(artifact, artifact.PhaseSliceSet.Slices, primaryHorizon);
        var horizonSummaries = BuildHorizonSummaries(artifact, artifact.PhaseSliceSet.Slices, renderedHorizons);
        var driftFlags = BuildDriftFlags(artifact.PhaseSliceSet.Slices, horizonSummaries);
        var topologyFlags = BuildTopologyFlags(artifact, artifact.PhaseSliceSet.Slices, horizonSummaries);
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
            PrimaryHorizonId = primaryHorizon.HorizonId,
            HorizonRawSlices = primaryHorizon.HorizonRawSlices,
            HorizonDurationMs = primaryHorizon.HorizonDurationMs,
            RequiredChannelCoverage = coverage,
            HorizonSummaries = horizonSummaries,
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

        var primaryHorizon = ResolvePrimaryComparisonHorizon(artifact.PhasePolicy, null);
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
                DeltaHorizon = primaryHorizon.HorizonRawSlices,
                DeltaHorizonMs = primaryHorizon.HorizonDurationMs,
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

        ValidateComparisonHorizonPolicies(artifact, phasePolicy, issues);

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

    private static void ValidateComparisonHorizonPolicies(
        LoadedHopngArtifact artifact,
        PhasePolicy phasePolicy,
        List<ValidationIssue> issues)
    {
        if (phasePolicy.ComparisonHorizons.Count == 0)
        {
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var classificationHorizonCount = phasePolicy.ComparisonHorizons.Count(horizon => horizon.UseForStateClassification);

        foreach (var horizon in phasePolicy.ComparisonHorizons)
        {
            if (string.IsNullOrWhiteSpace(horizon.HorizonId) || !seenIds.Add(horizon.HorizonId))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "Comparison horizons must declare unique horizon ids.", artifact.Layout.PhasePolicyPath));
            }

            if (!SupportedComparisonHorizonModes.Contains(horizon.Mode))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, $"Comparison horizon '{horizon.HorizonId}' must use raw_slices or duration_ms mode.", artifact.Layout.PhasePolicyPath));
            }

            if (horizon.Value <= 0)
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, $"Comparison horizon '{horizon.HorizonId}' must declare a positive value.", artifact.Layout.PhasePolicyPath));
            }

            if (string.Equals(horizon.Mode, "duration_ms", StringComparison.Ordinal)
                && (phasePolicy.RawCadenceMs <= 0 || horizon.Value % phasePolicy.RawCadenceMs != 0))
            {
                issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, $"Comparison horizon '{horizon.HorizonId}' duration_ms value must divide evenly by the raw cadence.", artifact.Layout.PhasePolicyPath));
            }
        }

        if (classificationHorizonCount != 1)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "Explicit comparison horizons must declare exactly one state-classification horizon.", artifact.Layout.PhasePolicyPath));
            return;
        }

        var primaryHorizon = phasePolicy.ComparisonHorizons.First(horizon => horizon.UseForStateClassification);
        if (!TryResolveComparisonHorizon(phasePolicy, primaryHorizon, out var resolvedPrimary)
            || resolvedPrimary.HorizonRawSlices != phasePolicy.ComparisonHorizonRawSlices)
        {
            issues.Add(new ValidationIssue(ValidationErrorCode.InvalidPhasePolicy, "comparisonHorizonRawSlices must match the raw-slice basis of the primary comparison horizon.", artifact.Layout.PhasePolicyPath));
        }
    }

    private static List<TemporalStateSummary> BuildStateSummaries(
        LoadedHopngArtifact artifact,
        IReadOnlyList<PhaseSlice> phaseSlices,
        ResolvedComparisonHorizon primaryHorizon)
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

            var metrics = BuildSliceMetrics(slice);
            var anchor = ResolveHorizonAnchor(phaseSlices, index, primaryHorizon);
            var anchorMetrics = anchor is null ? null : BuildSliceMetrics(anchor);
            var fallbackAdjacent = anchor is null && index > 0 ? phaseSlices[index - 1] : null;
            var topologyReference = anchor ?? fallbackAdjacent;
            var hasTopologyChange = topologyReference is not null && HasTopologyChangeBetween(artifact, topologyReference, slice);
            var directionBasis = anchorMetrics is null
                ? metrics.AverageSignedDrift
                : metrics.AverageSignedDrift - anchorMetrics.AverageSignedDrift;
            var derivedForce = Math.Round(
                (metrics.AveragePressure * thresholds.ForcePressureWeight)
                + (metrics.AverageAbsoluteDrift * thresholds.ForceDriftWeight)
                + (metrics.AverageBloom * thresholds.ForceBloomWeight)
                + (hasTopologyChange ? thresholds.ForceTopologyBonus : 0d),
                6);
            var direction = directionBasis >= thresholds.DirectionDriftAbsoluteMin
                ? "positive"
                : directionBasis <= -thresholds.DirectionDriftAbsoluteMin
                    ? "negative"
                    : "neutral";
            var stateClass = ClassifyState(metrics.AveragePressure, metrics.AverageAbsoluteDrift, metrics.AverageBloom, hasTopologyChange, thresholds);
            var basisSignals = BuildBasisSignals(metrics, hasTopologyChange, thresholds, primaryHorizon, anchor, anchorMetrics, fallbackAdjacent is not null);

            summaries.Add(new TemporalStateSummary
            {
                SliceId = slice.PhaseSliceId,
                N = slice.N,
                StateClass = stateClass,
                ComparisonHorizonId = primaryHorizon.HorizonId,
                AnchorSliceId = anchor?.PhaseSliceId ?? string.Empty,
                DerivedForceMagnitude = derivedForce,
                DerivedForceDirection = direction,
                BasisSignals = basisSignals
            });
        }

        return summaries;
    }

    private static List<TemporalHorizonSummary> BuildHorizonSummaries(
        LoadedHopngArtifact artifact,
        IReadOnlyList<PhaseSlice> phaseSlices,
        IReadOnlyList<ResolvedComparisonHorizon> horizons)
    {
        if (phaseSlices.Count == 0 || horizons.Count == 0)
        {
            return [];
        }

        var summaries = new List<TemporalHorizonSummary>();
        foreach (var horizon in horizons)
        {
            var driftFlags = new HashSet<string>(StringComparer.Ordinal);
            var topologyFlags = new HashSet<string>(StringComparer.Ordinal);
            var missingAnchorSliceIds = new List<string>();
            var comparableSliceCount = 0;

            for (var index = 1; index < phaseSlices.Count; index++)
            {
                var current = phaseSlices[index];
                var anchor = ResolveHorizonAnchor(phaseSlices, index, horizon);
                if (anchor is null)
                {
                    missingAnchorSliceIds.Add(current.PhaseSliceId);
                    continue;
                }

                comparableSliceCount++;
                AddDriftFlags(anchor, current, $"horizon:{horizon.HorizonId}", driftFlags);
                AddTopologyFlags(artifact, anchor, current, $"horizon:{horizon.HorizonId}", topologyFlags);
            }

            summaries.Add(new TemporalHorizonSummary
            {
                HorizonId = horizon.HorizonId,
                Mode = horizon.Mode,
                Value = horizon.Value,
                HorizonRawSlices = horizon.HorizonRawSlices,
                HorizonDurationMs = horizon.HorizonDurationMs,
                UseForStateClassification = horizon.UseForStateClassification,
                ComparableSliceCount = comparableSliceCount,
                MissingAnchorSliceIds = missingAnchorSliceIds,
                DriftFlags = driftFlags.OrderBy(flag => flag, StringComparer.Ordinal).ToList(),
                TopologyFlags = topologyFlags.OrderBy(flag => flag, StringComparer.Ordinal).ToList()
            });
        }

        return summaries;
    }

    private static List<string> BuildDriftFlags(
        IReadOnlyList<PhaseSlice> phaseSlices,
        IReadOnlyList<TemporalHorizonSummary> horizonSummaries)
    {
        var flags = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 1; index < phaseSlices.Count; index++)
        {
            AddDriftFlags(phaseSlices[index - 1], phaseSlices[index], "adjacent", flags);
        }

        foreach (var horizonSummary in horizonSummaries)
        {
            flags.UnionWith(horizonSummary.DriftFlags);
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

    private static List<string> BuildTopologyFlags(
        LoadedHopngArtifact artifact,
        IReadOnlyList<PhaseSlice> phaseSlices,
        IReadOnlyList<TemporalHorizonSummary> horizonSummaries)
    {
        var flags = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 1; index < phaseSlices.Count; index++)
        {
            AddTopologyFlags(artifact, phaseSlices[index - 1], phaseSlices[index], null, flags);
        }

        foreach (var horizonSummary in horizonSummaries)
        {
            flags.UnionWith(horizonSummary.TopologyFlags);
        }

        return flags.OrderBy(flag => flag, StringComparer.Ordinal).ToList();
    }

    private static void AddTopologyFlags(
        LoadedHopngArtifact artifact,
        PhaseSlice prior,
        PhaseSlice current,
        string? mode,
        HashSet<string> flags)
    {
        var priorUniverses = prior.UniverseStates.Keys.ToHashSet(StringComparer.Ordinal);
        var currentUniverses = current.UniverseStates.Keys.ToHashSet(StringComparer.Ordinal);
        var suffix = string.IsNullOrWhiteSpace(mode) ? "." : $" ({mode}).";

        if (!priorUniverses.SetEquals(currentUniverses))
        {
            flags.Add($"Universe participation changed between phase slices '{prior.PhaseSliceId}' and '{current.PhaseSliceId}'{suffix}");
        }

        var priorRelations = ResolveParticipatingRelations(artifact, priorUniverses);
        var currentRelations = ResolveParticipatingRelations(artifact, currentUniverses);
        if (!priorRelations.SetEquals(currentRelations))
        {
            flags.Add($"Gluing participation changed between phase slices '{prior.PhaseSliceId}' and '{current.PhaseSliceId}'{suffix}");
        }

        var priorProjectionRules = ResolveParticipatingProjectionRules(artifact, priorUniverses);
        var currentProjectionRules = ResolveParticipatingProjectionRules(artifact, currentUniverses);
        if (!priorProjectionRules.SetEquals(currentProjectionRules))
        {
            flags.Add($"Projection-rule participation changed between phase slices '{prior.PhaseSliceId}' and '{current.PhaseSliceId}'{suffix}");
        }
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
        TemporalSliceMetrics metrics,
        bool hasTopologyChange,
        TemporalStateThresholdPolicy thresholds,
        ResolvedComparisonHorizon primaryHorizon,
        PhaseSlice? anchor,
        TemporalSliceMetrics? anchorMetrics,
        bool usedAdjacentFallback)
    {
        var signals = new List<string>();
        var stateSignalAdded = false;

        if (anchor is not null && anchorMetrics is not null)
        {
            signals.Add($"Primary horizon '{primaryHorizon.HorizonId}' anchored on phase slice '{anchor.PhaseSliceId}' ({primaryHorizon.HorizonDurationMs} ms / {primaryHorizon.HorizonRawSlices} raw slices).");
            AppendDeltaSignal(signals, "Average pressure", metrics.AveragePressure - anchorMetrics.AveragePressure, anchor.PhaseSliceId);
            AppendDeltaSignal(signals, "Average signed drift", metrics.AverageSignedDrift - anchorMetrics.AverageSignedDrift, anchor.PhaseSliceId);
            AppendDeltaSignal(signals, "Average bloom", metrics.AverageBloom - anchorMetrics.AverageBloom, anchor.PhaseSliceId);
        }
        else if (usedAdjacentFallback)
        {
            signals.Add($"No prior slice satisfies primary horizon '{primaryHorizon.HorizonId}' yet; adjacent continuity remained the fallback basis.");
        }
        else
        {
            signals.Add($"No prior slice satisfies primary horizon '{primaryHorizon.HorizonId}' yet; this slice is classified from intrinsic values only.");
        }

        if (metrics.AveragePressure >= thresholds.RupturePressureMin)
        {
            signals.Add($"Average pressure {metrics.AveragePressure:F3} crossed the rupture threshold {thresholds.RupturePressureMin:F3}.");
            stateSignalAdded = true;
        }
        else if (metrics.AveragePressure >= thresholds.RisingPressureMin)
        {
            signals.Add($"Average pressure {metrics.AveragePressure:F3} crossed the rising-pressure threshold {thresholds.RisingPressureMin:F3}.");
            stateSignalAdded = true;
        }

        if (metrics.AverageAbsoluteDrift >= thresholds.RuptureDriftAbsoluteMin)
        {
            signals.Add($"Average absolute drift {metrics.AverageAbsoluteDrift:F3} crossed the rupture threshold {thresholds.RuptureDriftAbsoluteMin:F3}.");
            stateSignalAdded = true;
        }
        else if (metrics.AverageAbsoluteDrift >= thresholds.DriftingAbsoluteMin)
        {
            signals.Add($"Average absolute drift {metrics.AverageAbsoluteDrift:F3} crossed the drifting threshold {thresholds.DriftingAbsoluteMin:F3}.");
            stateSignalAdded = true;
        }

        if (metrics.AverageBloom >= thresholds.PropagatingBloomMin)
        {
            signals.Add($"Average bloom {metrics.AverageBloom:F3} crossed the propagating threshold {thresholds.PropagatingBloomMin:F3}.");
            stateSignalAdded = true;
        }

        if (hasTopologyChange)
        {
            signals.Add(anchor is null
                ? "Topology participation changed across adjacent phase slices."
                : $"Topology participation changed across primary horizon '{primaryHorizon.HorizonId}'.");
            stateSignalAdded = true;
        }

        if (!stateSignalAdded)
        {
            signals.Add("Phase slice remains within the stable pressure, drift, and bloom envelope.");
        }

        return signals;
    }

    private static string BuildGroupingSummary(PhasePolicy phasePolicy) =>
        string.Equals(phasePolicy.PhaseWindowMode, "duration_ms", StringComparison.Ordinal)
            ? $"{phasePolicy.EventGroupingMode}:{phasePolicy.EventGroupingSizeRawSlices} raw slices -> duration_ms:{phasePolicy.PhaseWindowDurationMs} ms (max {phasePolicy.MaxPhaseWindowSpanMs} ms)"
            : $"{phasePolicy.EventGroupingMode}:{phasePolicy.EventGroupingSizeRawSlices} raw slices -> fixed_event_count:{phasePolicy.PhaseWindowSizeEventSlices} event slices / {phasePolicy.PhaseWindowDurationMs} ms";

    private static List<ResolvedComparisonHorizon> BuildRenderedHorizons(
        PhasePolicy phasePolicy,
        ResolvedComparisonHorizon primaryHorizon)
    {
        var horizons = ResolveComparisonHorizons(phasePolicy);
        var rendered = new List<ResolvedComparisonHorizon> { primaryHorizon };
        rendered.AddRange(horizons.Where(horizon => !string.Equals(horizon.HorizonId, primaryHorizon.HorizonId, StringComparison.Ordinal)));
        return rendered;
    }

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

    private static ResolvedComparisonHorizon ResolvePrimaryComparisonHorizon(PhasePolicy phasePolicy, int? rawSliceHorizon)
    {
        if (rawSliceHorizon is > 0)
        {
            return new ResolvedComparisonHorizon
            {
                HorizonId = "cli-raw-horizon",
                Mode = "raw_slices",
                Value = rawSliceHorizon.Value,
                HorizonRawSlices = rawSliceHorizon.Value,
                HorizonDurationMs = rawSliceHorizon.Value * Math.Max(phasePolicy.RawCadenceMs, 1),
                UseForStateClassification = true
            };
        }

        var horizons = ResolveComparisonHorizons(phasePolicy);
        return horizons.FirstOrDefault(horizon => horizon.UseForStateClassification) ?? horizons[0];
    }

    private static List<ResolvedComparisonHorizon> ResolveComparisonHorizons(PhasePolicy phasePolicy)
    {
        if (phasePolicy.ComparisonHorizons.Count == 0)
        {
            return [BuildLegacyRawHorizon(phasePolicy)];
        }

        var horizons = phasePolicy.ComparisonHorizons
            .Select(horizon => TryResolveComparisonHorizon(phasePolicy, horizon, out var resolved) ? resolved : null)
            .Where(horizon => horizon is not null)
            .Cast<ResolvedComparisonHorizon>()
            .ToList();

        return horizons.Count > 0 ? horizons : [BuildLegacyRawHorizon(phasePolicy)];
    }

    private static ResolvedComparisonHorizon BuildLegacyRawHorizon(PhasePolicy phasePolicy)
    {
        var cadenceMs = Math.Max(phasePolicy.RawCadenceMs, 1);
        var rawSlices = Math.Max(phasePolicy.ComparisonHorizonRawSlices, 0);

        return new ResolvedComparisonHorizon
        {
            HorizonId = "policy-raw-horizon",
            Mode = "raw_slices",
            Value = rawSlices,
            HorizonRawSlices = rawSlices,
            HorizonDurationMs = rawSlices * cadenceMs,
            UseForStateClassification = true
        };
    }

    private static bool TryResolveComparisonHorizon(
        PhasePolicy phasePolicy,
        TemporalComparisonHorizonPolicy horizon,
        out ResolvedComparisonHorizon resolved)
    {
        resolved = default!;
        if (string.IsNullOrWhiteSpace(horizon.HorizonId) || horizon.Value <= 0)
        {
            return false;
        }

        if (string.Equals(horizon.Mode, "raw_slices", StringComparison.Ordinal))
        {
            resolved = new ResolvedComparisonHorizon
            {
                HorizonId = horizon.HorizonId,
                Mode = horizon.Mode,
                Value = horizon.Value,
                HorizonRawSlices = horizon.Value,
                HorizonDurationMs = horizon.Value * Math.Max(phasePolicy.RawCadenceMs, 1),
                UseForStateClassification = horizon.UseForStateClassification
            };
            return true;
        }

        if (string.Equals(horizon.Mode, "duration_ms", StringComparison.Ordinal)
            && phasePolicy.RawCadenceMs > 0
            && horizon.Value % phasePolicy.RawCadenceMs == 0)
        {
            resolved = new ResolvedComparisonHorizon
            {
                HorizonId = horizon.HorizonId,
                Mode = horizon.Mode,
                Value = horizon.Value,
                HorizonRawSlices = horizon.Value / phasePolicy.RawCadenceMs,
                HorizonDurationMs = horizon.Value,
                UseForStateClassification = horizon.UseForStateClassification
            };
            return true;
        }

        return false;
    }

    private static PhaseSlice? ResolveHorizonAnchor(
        IReadOnlyList<PhaseSlice> phaseSlices,
        int currentIndex,
        ResolvedComparisonHorizon horizon)
    {
        if (currentIndex <= 0 || horizon.Value <= 0)
        {
            return null;
        }

        var current = phaseSlices[currentIndex];
        return phaseSlices
            .Take(currentIndex)
            .LastOrDefault(candidate => HorizonSatisfied(candidate, current, horizon));
    }

    private static bool HorizonSatisfied(PhaseSlice anchor, PhaseSlice current, ResolvedComparisonHorizon horizon) =>
        string.Equals(horizon.Mode, "duration_ms", StringComparison.Ordinal)
            ? GetTimestampSpanMs(anchor.TimestampEndUtc, current.TimestampEndUtc) >= horizon.HorizonDurationMs
            : current.SourceRawEndN - anchor.SourceRawEndN >= horizon.HorizonRawSlices;

    private static TemporalSliceMetrics BuildSliceMetrics(PhaseSlice slice) =>
        new()
        {
            AveragePressure = slice.UniverseStates.Values.Average(state => state.Pressure),
            AverageSignedDrift = slice.UniverseStates.Values.Average(state => state.Drift),
            AverageAbsoluteDrift = slice.UniverseStates.Values.Average(state => Math.Abs(state.Drift)),
            AverageBloom = slice.UniverseStates.Values.Average(state => state.Bloom)
        };

    private static void AppendDeltaSignal(List<string> signals, string label, double delta, string anchorSliceId)
    {
        if (Math.Abs(delta) <= 0d)
        {
            return;
        }

        signals.Add($"{label} changed by {delta:+0.000;-0.000;0.000} since anchor '{anchorSliceId}'.");
    }

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

    private sealed record ResolvedComparisonHorizon
    {
        public string HorizonId { get; init; } = string.Empty;
        public string Mode { get; init; } = string.Empty;
        public int Value { get; init; }
        public int HorizonRawSlices { get; init; }
        public int HorizonDurationMs { get; init; }
        public bool UseForStateClassification { get; init; }
    }

    private sealed record TemporalSliceMetrics
    {
        public double AveragePressure { get; init; }
        public double AverageSignedDrift { get; init; }
        public double AverageAbsoluteDrift { get; init; }
        public double AverageBloom { get; init; }
    }
}
