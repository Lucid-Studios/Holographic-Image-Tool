using Hdt.Core.Models;
using Hdt.Core.Validation;

namespace Hdt.Core.Services;

public sealed class TemporalPhaseStackComparisonService
{
    private static readonly Dictionary<string, int> StateRank = new(StringComparer.Ordinal)
    {
        ["Stable"] = 0,
        ["RisingPressure"] = 1,
        ["Drifting"] = 2,
        ["Propagating"] = 3,
        ["RuptureRisk"] = 4,
        ["StructurallyIncomplete"] = -1
    };

    private readonly HopngArtifactLoader _loader = new();
    private readonly HopngArtifactValidator _validator = new();
    private readonly TemporalPhaseStackService _temporalPhaseStackService = new();

    public TemporalPhaseStackComparisonResult Compare(
        string leftPath,
        string rightPath,
        string view = "prime",
        int? rawSliceHorizon = null)
    {
        var leftArtifact = _loader.Load(leftPath);
        var rightArtifact = _loader.Load(rightPath);
        var leftValidation = _validator.Validate(leftPath);
        var rightValidation = _validator.Validate(rightPath);

        return Compare(leftArtifact, leftValidation, rightArtifact, rightValidation, view, rawSliceHorizon);
    }

    public TemporalPhaseStackComparisonResult Compare(
        LoadedHopngArtifact leftArtifact,
        ValidationResult leftValidation,
        LoadedHopngArtifact rightArtifact,
        ValidationResult rightValidation,
        string view = "prime",
        int? rawSliceHorizon = null)
    {
        var leftRender = _temporalPhaseStackService.Render(leftArtifact, leftValidation, view, rawSliceHorizon);
        var rightRender = _temporalPhaseStackService.Render(rightArtifact, rightValidation, view, rawSliceHorizon);
        var payloadMode = string.Equals(view, "privileged", StringComparison.OrdinalIgnoreCase)
            ? "privileged"
            : "prime";

        var leftFinalState = leftRender.StateSummaries.LastOrDefault();
        var rightFinalState = rightRender.StateSummaries.LastOrDefault();
        var leftFinalStateRank = ResolveStateRank(leftFinalState?.StateClass);
        var rightFinalStateRank = ResolveStateRank(rightFinalState?.StateClass);

        if (leftRender.Status != TemporalStackStatus.LawfullyDerived || rightRender.Status != TemporalStackStatus.LawfullyDerived)
        {
            return new TemporalPhaseStackComparisonResult
            {
                LeftArtifactId = leftRender.ArtifactId,
                RightArtifactId = rightRender.ArtifactId,
                LeftStatus = leftRender.Status,
                RightStatus = rightRender.Status,
                BasisAlignmentStatus = "not-comparable",
                Classification = "FlattenedOrUnsupported",
                TemporalStateCompatibility = "Unavailable",
                LeftPrimaryHorizonId = leftRender.PrimaryHorizonId,
                RightPrimaryHorizonId = rightRender.PrimaryHorizonId,
                LeftPrimaryHorizonRawSlices = leftRender.HorizonRawSlices,
                LeftPrimaryHorizonDurationMs = leftRender.HorizonDurationMs,
                RightPrimaryHorizonRawSlices = rightRender.HorizonRawSlices,
                RightPrimaryHorizonDurationMs = rightRender.HorizonDurationMs,
                PrimaryHorizonRawSlices = leftRender.HorizonRawSlices,
                PrimaryHorizonDurationMs = leftRender.HorizonDurationMs,
                LeftFinalStateClass = leftFinalState?.StateClass ?? string.Empty,
                RightFinalStateClass = rightFinalState?.StateClass ?? string.Empty,
                LeftFinalStateDirection = leftFinalState?.DerivedForceDirection ?? string.Empty,
                RightFinalStateDirection = rightFinalState?.DerivedForceDirection ?? string.Empty,
                LeftFinalStateRank = leftFinalStateRank,
                RightFinalStateRank = rightFinalStateRank,
                StateRankDelta = BuildStateRankDelta(leftFinalStateRank, rightFinalStateRank),
                ComparablePhaseSliceCount = Math.Min(leftRender.PhaseSliceCount, rightRender.PhaseSliceCount),
                ClassificationReason = "At least one artifact is not lawfully derived as a Phase 3 temporal stack.",
                PayloadMode = payloadMode,
                BasisSignals =
                [
                    "At least one artifact is not lawfully derived as a Phase 3 temporal stack, so cross-artifact comparison cannot proceed."
                ],
                Signals =
                [
                    $"Left temporal status: {leftRender.Status}.",
                    $"Right temporal status: {rightRender.Status}."
                ],
                LeftIssues = leftRender.Issues,
                RightIssues = rightRender.Issues,
                LeftValidationIssues = [.. leftRender.ValidationIssues],
                RightValidationIssues = [.. rightRender.ValidationIssues]
            };
        }

        var basisSignals = EvaluateBasisAlignment(leftArtifact, leftRender, rightArtifact, rightRender);
        var basisAligned = !basisSignals.Any(signal => signal.StartsWith("Mismatch:", StringComparison.Ordinal));
        if (!basisAligned)
        {
            return new TemporalPhaseStackComparisonResult
            {
                LeftArtifactId = leftRender.ArtifactId,
                RightArtifactId = rightRender.ArtifactId,
                LeftStatus = leftRender.Status,
                RightStatus = rightRender.Status,
                BasisAlignmentStatus = "Incompatible",
                Classification = "Incompatible",
                TemporalStateCompatibility = "Unavailable",
                LeftPrimaryHorizonId = leftRender.PrimaryHorizonId,
                RightPrimaryHorizonId = rightRender.PrimaryHorizonId,
                LeftPrimaryHorizonRawSlices = leftRender.HorizonRawSlices,
                LeftPrimaryHorizonDurationMs = leftRender.HorizonDurationMs,
                RightPrimaryHorizonRawSlices = rightRender.HorizonRawSlices,
                RightPrimaryHorizonDurationMs = rightRender.HorizonDurationMs,
                PrimaryHorizonRawSlices = leftRender.HorizonRawSlices,
                PrimaryHorizonDurationMs = leftRender.HorizonDurationMs,
                LeftFinalStateClass = leftFinalState?.StateClass ?? string.Empty,
                RightFinalStateClass = rightFinalState?.StateClass ?? string.Empty,
                LeftFinalStateDirection = leftFinalState?.DerivedForceDirection ?? string.Empty,
                RightFinalStateDirection = rightFinalState?.DerivedForceDirection ?? string.Empty,
                LeftFinalStateRank = leftFinalStateRank,
                RightFinalStateRank = rightFinalStateRank,
                StateRankDelta = BuildStateRankDelta(leftFinalStateRank, rightFinalStateRank),
                ComparablePhaseSliceCount = Math.Min(leftRender.PhaseSliceCount, rightRender.PhaseSliceCount),
                ClassificationReason = basisSignals.FirstOrDefault(signal => signal.StartsWith("Mismatch:", StringComparison.Ordinal)) ?? "Temporal basis mismatch prevents lawful comparison.",
                PayloadMode = payloadMode,
                BasisSignals = basisSignals,
                LeftIssues = leftRender.Issues,
                RightIssues = rightRender.Issues,
                LeftValidationIssues = [.. leftRender.ValidationIssues],
                RightValidationIssues = [.. rightRender.ValidationIssues]
            };
        }

        var leftSnapshot = BuildSnapshot(leftArtifact, leftRender);
        var rightSnapshot = BuildSnapshot(rightArtifact, rightRender);
        if (leftSnapshot is null || rightSnapshot is null)
        {
            return new TemporalPhaseStackComparisonResult
            {
                LeftArtifactId = leftRender.ArtifactId,
                RightArtifactId = rightRender.ArtifactId,
                LeftStatus = leftRender.Status,
                RightStatus = rightRender.Status,
                BasisAlignmentStatus = "Aligned",
                Classification = "FlattenedOrUnsupported",
                TemporalStateCompatibility = "Unavailable",
                LeftPrimaryHorizonId = leftRender.PrimaryHorizonId,
                RightPrimaryHorizonId = rightRender.PrimaryHorizonId,
                LeftPrimaryHorizonRawSlices = leftRender.HorizonRawSlices,
                LeftPrimaryHorizonDurationMs = leftRender.HorizonDurationMs,
                RightPrimaryHorizonRawSlices = rightRender.HorizonRawSlices,
                RightPrimaryHorizonDurationMs = rightRender.HorizonDurationMs,
                PrimaryHorizonRawSlices = leftRender.HorizonRawSlices,
                PrimaryHorizonDurationMs = leftRender.HorizonDurationMs,
                LeftFinalStateClass = leftFinalState?.StateClass ?? string.Empty,
                RightFinalStateClass = rightFinalState?.StateClass ?? string.Empty,
                LeftFinalStateDirection = leftFinalState?.DerivedForceDirection ?? string.Empty,
                RightFinalStateDirection = rightFinalState?.DerivedForceDirection ?? string.Empty,
                LeftFinalStateRank = leftFinalStateRank,
                RightFinalStateRank = rightFinalStateRank,
                StateRankDelta = BuildStateRankDelta(leftFinalStateRank, rightFinalStateRank),
                ComparablePhaseSliceCount = Math.Min(leftRender.PhaseSliceCount, rightRender.PhaseSliceCount),
                ClassificationReason = "A lawfully derived comparison requires at least one state summary and one phase slice on both artifacts.",
                PayloadMode = payloadMode,
                BasisSignals = basisSignals,
                Signals =
                [
                    "A lawfully derived comparison requires at least one state summary and one phase slice on both artifacts."
                ],
                LeftIssues = leftRender.Issues,
                RightIssues = rightRender.Issues,
                LeftValidationIssues = [.. leftRender.ValidationIssues],
                RightValidationIssues = [.. rightRender.ValidationIssues]
            };
        }

        var temporalStateCompatibility = DetermineStateCompatibility(leftRender.StateSummaries, rightRender.StateSummaries);
        var driftDeltaMagnitude = Math.Round(
            Math.Abs(leftSnapshot.AverageSignedDrift - rightSnapshot.AverageSignedDrift)
            + Math.Abs(leftSnapshot.AverageAbsoluteDrift - rightSnapshot.AverageAbsoluteDrift),
            6);
        var derivedForceDeltaMagnitude = Math.Round(
            Math.Abs(leftSnapshot.DerivedForceMagnitude - rightSnapshot.DerivedForceMagnitude),
            6);
        var topologyDeltaCount =
            CountSymmetricDifference(leftSnapshot.UniverseIds, rightSnapshot.UniverseIds)
            + CountSymmetricDifference(leftSnapshot.RelationIds, rightSnapshot.RelationIds)
            + CountSymmetricDifference(leftSnapshot.ProjectionRuleIds, rightSnapshot.ProjectionRuleIds);
        var similarityScore = BuildSimilarityScore(
            driftDeltaMagnitude,
            derivedForceDeltaMagnitude,
            topologyDeltaCount,
            temporalStateCompatibility);
        var classification = Classify(
            temporalStateCompatibility,
            driftDeltaMagnitude,
            derivedForceDeltaMagnitude,
            topologyDeltaCount);
        var signals = BuildSignals(
            leftSnapshot,
            rightSnapshot,
            temporalStateCompatibility,
            driftDeltaMagnitude,
            derivedForceDeltaMagnitude,
            topologyDeltaCount,
            similarityScore,
            classification);
        var leftSnapshotStateRank = ResolveStateRank(leftSnapshot.StateClass);
        var rightSnapshotStateRank = ResolveStateRank(rightSnapshot.StateClass);

        return new TemporalPhaseStackComparisonResult
        {
            LeftArtifactId = leftRender.ArtifactId,
            RightArtifactId = rightRender.ArtifactId,
            LeftStatus = leftRender.Status,
            RightStatus = rightRender.Status,
            BasisAlignmentStatus = "Aligned",
            Classification = classification,
            TemporalStateCompatibility = temporalStateCompatibility,
            LeftPrimaryHorizonId = leftRender.PrimaryHorizonId,
            RightPrimaryHorizonId = rightRender.PrimaryHorizonId,
            LeftPrimaryHorizonRawSlices = leftRender.HorizonRawSlices,
            LeftPrimaryHorizonDurationMs = leftRender.HorizonDurationMs,
            RightPrimaryHorizonRawSlices = rightRender.HorizonRawSlices,
            RightPrimaryHorizonDurationMs = rightRender.HorizonDurationMs,
            PrimaryHorizonRawSlices = leftRender.HorizonRawSlices,
            PrimaryHorizonDurationMs = leftRender.HorizonDurationMs,
            LeftFinalStateClass = leftSnapshot.StateClass,
            RightFinalStateClass = rightSnapshot.StateClass,
            LeftFinalStateDirection = leftSnapshot.StateDirection,
            RightFinalStateDirection = rightSnapshot.StateDirection,
            LeftFinalStateRank = leftSnapshotStateRank,
            RightFinalStateRank = rightSnapshotStateRank,
            StateRankDelta = BuildStateRankDelta(leftSnapshotStateRank, rightSnapshotStateRank),
            ComparablePhaseSliceCount = Math.Min(leftRender.PhaseSliceCount, rightRender.PhaseSliceCount),
            DriftDeltaMagnitude = driftDeltaMagnitude,
            DerivedForceDeltaMagnitude = derivedForceDeltaMagnitude,
            TopologyDeltaCount = topologyDeltaCount,
            SimilarityScore = similarityScore,
            ClassificationReason = BuildClassificationReason(classification),
            PayloadMode = payloadMode,
            BasisSignals = basisSignals,
            Signals = signals,
            LeftIssues = leftRender.Issues,
            RightIssues = rightRender.Issues,
            LeftValidationIssues = [.. leftRender.ValidationIssues],
            RightValidationIssues = [.. rightRender.ValidationIssues]
        };
    }

    private static List<string> EvaluateBasisAlignment(
        LoadedHopngArtifact leftArtifact,
        PhaseStackRenderResult leftRender,
        LoadedHopngArtifact rightArtifact,
        PhaseStackRenderResult rightRender)
    {
        var signals = new List<string>();
        var leftPolicy = leftArtifact.PhasePolicy;
        var rightPolicy = rightArtifact.PhasePolicy;
        var leftChannels = leftArtifact.OpticalChannelsDefinition;
        var rightChannels = rightArtifact.OpticalChannelsDefinition;
        var leftUniverses = leftArtifact.UniverseLayerSet?.Universes.Select(universe => universe.UniverseId).OrderBy(id => id, StringComparer.Ordinal).ToList() ?? [];
        var rightUniverses = rightArtifact.UniverseLayerSet?.Universes.Select(universe => universe.UniverseId).OrderBy(id => id, StringComparer.Ordinal).ToList() ?? [];

        if (leftPolicy is null || rightPolicy is null || leftChannels is null || rightChannels is null)
        {
            signals.Add("Mismatch: One or both artifacts do not declare the full Phase 3 policy and optical-channel contract.");
            return signals;
        }

        if (leftPolicy.RawCadenceMs != rightPolicy.RawCadenceMs)
        {
            signals.Add($"Mismatch: raw cadence differs ({leftPolicy.RawCadenceMs} ms vs {rightPolicy.RawCadenceMs} ms).");
        }

        if (!string.Equals(leftPolicy.EventGroupingMode, rightPolicy.EventGroupingMode, StringComparison.Ordinal)
            || leftPolicy.EventGroupingSizeRawSlices != rightPolicy.EventGroupingSizeRawSlices)
        {
            signals.Add("Mismatch: event grouping mode or size differs across artifacts.");
        }

        if (!string.Equals(leftPolicy.PhaseWindowMode, rightPolicy.PhaseWindowMode, StringComparison.Ordinal)
            || leftPolicy.PhaseWindowSizeEventSlices != rightPolicy.PhaseWindowSizeEventSlices
            || leftPolicy.PhaseWindowDurationMs != rightPolicy.PhaseWindowDurationMs
            || leftPolicy.MaxPhaseWindowSpanMs != rightPolicy.MaxPhaseWindowSpanMs)
        {
            signals.Add("Mismatch: phase-window policy differs across artifacts.");
        }

        if (leftRender.HorizonRawSlices != rightRender.HorizonRawSlices
            || leftRender.HorizonDurationMs != rightRender.HorizonDurationMs)
        {
            signals.Add("Mismatch: primary comparison horizon basis differs across artifacts.");
        }

        if (leftRender.RequiredChannelCoverage != rightRender.RequiredChannelCoverage
            || !leftRender.RequiredChannelCoverage
            || !rightRender.RequiredChannelCoverage)
        {
            signals.Add("Mismatch: required temporal channels are not fully covered on both artifacts.");
        }

        if (!leftChannels.RequiredChannels.OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(rightChannels.RequiredChannels.OrderBy(id => id, StringComparer.Ordinal), StringComparer.Ordinal))
        {
            signals.Add("Mismatch: required optical-channel semantics differ across artifacts.");
        }

        if (!leftUniverses.SequenceEqual(rightUniverses, StringComparer.Ordinal))
        {
            signals.Add("Mismatch: declared universe sets differ across artifacts.");
        }

        if (signals.Count == 0)
        {
            signals.Add($"Temporal basis aligned on raw cadence {leftPolicy.RawCadenceMs} ms, event grouping {leftPolicy.EventGroupingMode}:{leftPolicy.EventGroupingSizeRawSlices}, and primary horizon {leftRender.HorizonDurationMs} ms / {leftRender.HorizonRawSlices} raw slices.");
        }

        return signals;
    }

    private static TemporalComparisonSnapshot? BuildSnapshot(
        LoadedHopngArtifact artifact,
        PhaseStackRenderResult render)
    {
        var latestPhaseSlice = artifact.PhaseSliceSet?.Slices
            .OrderBy(slice => slice.N)
            .LastOrDefault();
        var latestState = render.StateSummaries
            .OrderBy(summary => summary.N)
            .LastOrDefault();
        if (latestPhaseSlice is null || latestState is null || latestPhaseSlice.UniverseStates.Count == 0)
        {
            return null;
        }

        var participatingUniverses = latestPhaseSlice.UniverseStates.Keys.ToHashSet(StringComparer.Ordinal);
        var relationIds = ResolveParticipatingRelations(artifact, participatingUniverses);
        var projectionRuleIds = ResolveParticipatingProjectionRules(artifact, participatingUniverses);

        return new TemporalComparisonSnapshot
        {
            PhaseSliceId = latestPhaseSlice.PhaseSliceId,
            StateClass = latestState.StateClass,
            StateDirection = latestState.DerivedForceDirection,
            DerivedForceMagnitude = latestState.DerivedForceMagnitude,
            AverageSignedDrift = latestPhaseSlice.UniverseStates.Values.Average(state => state.Drift),
            AverageAbsoluteDrift = latestPhaseSlice.UniverseStates.Values.Average(state => Math.Abs(state.Drift)),
            UniverseIds = participatingUniverses,
            RelationIds = relationIds,
            ProjectionRuleIds = projectionRuleIds
        };
    }

    private static string DetermineStateCompatibility(
        IReadOnlyList<TemporalStateSummary> leftStates,
        IReadOnlyList<TemporalStateSummary> rightStates)
    {
        if (leftStates.Count == 0 || rightStates.Count == 0)
        {
            return "Unavailable";
        }

        var leftFinal = leftStates[^1];
        var rightFinal = rightStates[^1];
        if (string.Equals(leftFinal.StateClass, rightFinal.StateClass, StringComparison.Ordinal)
            && string.Equals(leftFinal.DerivedForceDirection, rightFinal.DerivedForceDirection, StringComparison.Ordinal))
        {
            return "Aligned";
        }

        if (MatchesPriorState(leftStates, rightFinal) || MatchesPriorState(rightStates, leftFinal))
        {
            return "Delayed";
        }

        return "Divergent";
    }

    private static bool MatchesPriorState(
        IReadOnlyList<TemporalStateSummary> sequence,
        TemporalStateSummary otherFinal)
    {
        if (sequence.Count < 2)
        {
            return false;
        }

        var prior = sequence[^2];
        return string.Equals(prior.StateClass, otherFinal.StateClass, StringComparison.Ordinal)
            && string.Equals(prior.DerivedForceDirection, otherFinal.DerivedForceDirection, StringComparison.Ordinal);
    }

    private static string Classify(
        string temporalStateCompatibility,
        double driftDeltaMagnitude,
        double derivedForceDeltaMagnitude,
        int topologyDeltaCount)
    {
        if (temporalStateCompatibility == "Aligned"
            && topologyDeltaCount == 0
            && driftDeltaMagnitude <= 0.20d
            && derivedForceDeltaMagnitude <= 0.15d)
        {
            return "Convergent";
        }

        if (temporalStateCompatibility == "Delayed"
            && topologyDeltaCount == 0
            && driftDeltaMagnitude <= 0.35d
            && derivedForceDeltaMagnitude <= 0.20d)
        {
            return "Delayed";
        }

        return "Divergent";
    }

    private static double BuildSimilarityScore(
        double driftDeltaMagnitude,
        double derivedForceDeltaMagnitude,
        int topologyDeltaCount,
        string temporalStateCompatibility)
    {
        var statePenalty = temporalStateCompatibility switch
        {
            "Aligned" => 0d,
            "Delayed" => 0.10d,
            _ => 0.25d
        };
        var topologyPenalty = Math.Min(0.30d, topologyDeltaCount * 0.10d);
        var rawPenalty = Math.Min(
            1d,
            (driftDeltaMagnitude * 0.45d)
            + (derivedForceDeltaMagnitude * 0.35d)
            + topologyPenalty
            + statePenalty);

        return Math.Round(1d - rawPenalty, 6);
    }

    private static List<string> BuildSignals(
        TemporalComparisonSnapshot leftSnapshot,
        TemporalComparisonSnapshot rightSnapshot,
        string temporalStateCompatibility,
        double driftDeltaMagnitude,
        double derivedForceDeltaMagnitude,
        int topologyDeltaCount,
        double similarityScore,
        string classification)
    {
        var signals = new List<string>
        {
            $"Final phase slices compared: '{leftSnapshot.PhaseSliceId}' vs '{rightSnapshot.PhaseSliceId}'.",
            $"Final state classes: {leftSnapshot.StateClass} ({leftSnapshot.StateDirection}) vs {rightSnapshot.StateClass} ({rightSnapshot.StateDirection}).",
            $"Temporal-state compatibility resolved as {temporalStateCompatibility}.",
            $"Drift delta magnitude: {driftDeltaMagnitude:0.000000}.",
            $"Derived force delta magnitude: {derivedForceDeltaMagnitude:0.000000}.",
            $"Topology delta count: {topologyDeltaCount}.",
            $"Similarity score: {similarityScore:0.000000}.",
            $"Comparison classified as {classification}."
        };

        if (classification == "Delayed")
        {
            signals.Add("One artifact's final temporal posture matches the other's immediately prior lawful state, so the pair is treated as delayed rather than divergent.");
        }
        else if (classification == "Convergent")
        {
            signals.Add("Both artifacts remain inside the same latest temporal posture with low drift, force, and topology delta.");
        }
        else
        {
            signals.Add("The artifacts remain lawfully comparable, but their latest temporal posture no longer converges under the aligned basis.");
        }

        return signals;
    }

    private static int? ResolveStateRank(string? stateClass) =>
        !string.IsNullOrWhiteSpace(stateClass) && StateRank.TryGetValue(stateClass, out var rank)
            ? rank
            : null;

    private static int? BuildStateRankDelta(int? leftStateRank, int? rightStateRank) =>
        leftStateRank.HasValue && rightStateRank.HasValue
            ? rightStateRank.Value - leftStateRank.Value
            : null;

    private static string BuildClassificationReason(string classification) =>
        classification switch
        {
            "Convergent" => "Both artifacts remain inside the same latest temporal posture with low drift, force, and topology delta.",
            "Delayed" => "One artifact matches the other's immediately prior lawful state under the aligned basis.",
            "Divergent" => "Artifacts remain comparable, but their latest temporal posture no longer converges under the aligned basis.",
            "Incompatible" => "Temporal basis mismatch prevents lawful comparison.",
            _ => "Comparison could not be completed as a lawful Phase 3 temporal comparison."
        };

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

    private static int CountSymmetricDifference(HashSet<string> left, HashSet<string> right)
    {
        var difference = new HashSet<string>(left, StringComparer.Ordinal);
        difference.SymmetricExceptWith(right);
        return difference.Count;
    }

    private sealed record TemporalComparisonSnapshot
    {
        public string PhaseSliceId { get; init; } = string.Empty;
        public string StateClass { get; init; } = string.Empty;
        public string StateDirection { get; init; } = string.Empty;
        public double DerivedForceMagnitude { get; init; }
        public double AverageSignedDrift { get; init; }
        public double AverageAbsoluteDrift { get; init; }
        public HashSet<string> UniverseIds { get; init; } = new(StringComparer.Ordinal);
        public HashSet<string> RelationIds { get; init; } = new(StringComparer.Ordinal);
        public HashSet<string> ProjectionRuleIds { get; init; } = new(StringComparer.Ordinal);
    }
}
