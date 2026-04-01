using Hdt.Core.Models;
using Hdt.Core.Validation;

namespace Hdt.Core.Services;

public sealed class EngramSupportComparisonService
{
    private static readonly IReadOnlyDictionary<string, int> WorkingIntentRank = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["working_intent"] = 0,
        ["structured_intent"] = 1,
        ["supported_intent"] = 2,
        ["reviewable_support"] = 3,
        ["restricted_support"] = 1,
        ["deferred_support"] = 0,
        ["rejected_support"] = -1
    };

    private static readonly HashSet<ValidationErrorCode> Phase4ValidationCodes =
    [
        ValidationErrorCode.InvalidEngramSupport,
        ValidationErrorCode.InvalidPerspectivalEngram,
        ValidationErrorCode.InvalidParticipatoryEngram
    ];

    private readonly HopngArtifactLoader _loader = new();
    private readonly HopngArtifactValidator _validator = new();
    private readonly TemporalPhaseStackService _temporalPhaseStackService = new();
    private readonly Phase4EngramScaffoldingService _phase4EngramScaffoldingService = new();
    private readonly EngramStabilityFieldService _engramStabilityFieldService = new();

    public EngramSupportComparisonResult Compare(string leftPath, string rightPath, string view = "prime")
    {
        var leftArtifact = _loader.Load(leftPath);
        var rightArtifact = _loader.Load(rightPath);
        var leftValidation = _validator.Validate(leftPath);
        var rightValidation = _validator.Validate(rightPath);

        return Compare(leftArtifact, leftValidation, rightArtifact, rightValidation, view);
    }

    public EngramSupportComparisonResult Compare(
        LoadedHopngArtifact leftArtifact,
        ValidationResult leftValidation,
        LoadedHopngArtifact rightArtifact,
        ValidationResult rightValidation,
        string view = "prime")
    {
        var payloadMode = string.Equals(view, "privileged", StringComparison.OrdinalIgnoreCase)
            ? "privileged"
            : "prime";
        var leftHasSupport = Phase4EngramScaffoldingService.HasPhase4Sidecars(leftArtifact);
        var rightHasSupport = Phase4EngramScaffoldingService.HasPhase4Sidecars(rightArtifact);
        if (!leftHasSupport || !rightHasSupport)
        {
            return BuildUnsupportedResult(
                leftArtifact,
                rightArtifact,
                leftValidation,
                rightValidation,
                payloadMode,
                "FlattenedOrUnsupported",
                "At least one artifact does not expose Phase 4 engram-support sidecars, so support comparison cannot proceed.");
        }

        var leftTemporal = TemporalPhaseStackService.HasPhase3Sidecars(leftArtifact)
            ? _temporalPhaseStackService.Render(leftArtifact, leftValidation, view)
            : null;
        var rightTemporal = TemporalPhaseStackService.HasPhase3Sidecars(rightArtifact)
            ? _temporalPhaseStackService.Render(rightArtifact, rightValidation, view)
            : null;
        var leftSummary = _phase4EngramScaffoldingService.BuildPrimeSafeSummary(leftArtifact);
        var rightSummary = _phase4EngramScaffoldingService.BuildPrimeSafeSummary(rightArtifact);
        var leftField = _engramStabilityFieldService.Build(leftArtifact, leftValidation, leftTemporal);
        var rightField = _engramStabilityFieldService.Build(rightArtifact, rightValidation, rightTemporal);
        if (leftSummary is null || rightSummary is null || leftField is null || rightField is null)
        {
            return BuildUnsupportedResult(
                leftArtifact,
                rightArtifact,
                leftValidation,
                rightValidation,
                payloadMode,
                "FlattenedOrUnsupported",
                "At least one artifact cannot produce a lawful Phase 4 support summary or stability field.");
        }

        var workingIntentTransitionStatus = ResolveWorkingIntentTransitionStatus(
            leftSummary.WorkingIntentState,
            rightSummary.WorkingIntentState);
        var supportTypeCompatibility = ResolveSupportTypeCompatibility(leftSummary, rightSummary);
        var supportIdentityCompatibility = ResolveIdentityCompatibility(leftSummary, rightSummary);
        var sharedSupportSignalCount = CountShared(leftSummary.SupportSignals, rightSummary.SupportSignals);
        var sharedValidationQuestionCount = CountShared(leftSummary.ValidationQuestions, rightSummary.ValidationQuestions);
        var workingIntentRankDelta = BuildWorkingIntentRankDelta(leftSummary.WorkingIntentState, rightSummary.WorkingIntentState);
        var constraintEnergyDelta = Math.Round(Math.Abs(leftField.ConstraintEnergy - rightField.ConstraintEnergy), 6);

        var leftPhase4Issues = leftValidation.Errors.Where(issue => Phase4ValidationCodes.Contains(issue.Code)).ToList();
        var rightPhase4Issues = rightValidation.Errors.Where(issue => Phase4ValidationCodes.Contains(issue.Code)).ToList();
        if (!leftValidation.IsValid || !rightValidation.IsValid)
        {
            var counterfeitPressureStatus = leftPhase4Issues.Count > 0 || rightPhase4Issues.Count > 0
                ? "detected"
                : "elevated";
            var reason = leftPhase4Issues.Count > 0 || rightPhase4Issues.Count > 0
                ? "At least one artifact fails Phase 4 support validation, so the pair is treated as counterfeit or unsupported."
                : "At least one artifact fails core validation, so support comparison cannot treat the pair as lawful support evidence.";

            return BuildComparisonResult(
                leftArtifact,
                rightArtifact,
                leftSummary,
                rightSummary,
                leftField,
                rightField,
                payloadMode,
                workingIntentTransitionStatus,
                supportTypeCompatibility,
                supportIdentityCompatibility,
                counterfeitPressureStatus,
                sharedSupportSignalCount,
                sharedValidationQuestionCount,
                workingIntentRankDelta,
                0d,
                "CounterfeitOrUnsupported",
                reason,
                BuildValidationSignals(leftValidation, rightValidation, reason),
                leftValidation,
                rightValidation);
        }

        if (!string.Equals(supportTypeCompatibility, "Aligned", StringComparison.Ordinal))
        {
            return BuildComparisonResult(
                leftArtifact,
                rightArtifact,
                leftSummary,
                rightSummary,
                leftField,
                rightField,
                payloadMode,
                workingIntentTransitionStatus,
                supportTypeCompatibility,
                supportIdentityCompatibility,
                "none",
                sharedSupportSignalCount,
                sharedValidationQuestionCount,
                workingIntentRankDelta,
                0d,
                "IncompatibleSupportType",
                "Support comparison requires artifacts of the same Phase 4 support type.",
                [
                    $"Left support type: {leftSummary.SupportType}.",
                    $"Right support type: {rightSummary.SupportType}."
                ],
                leftValidation,
                rightValidation);
        }

        var counterfeitPressureStatusForLawfulPair = ResolveCounterfeitPressure(
            leftField,
            rightField,
            supportIdentityCompatibility,
            sharedSupportSignalCount,
            sharedValidationQuestionCount);
        if (string.Equals(counterfeitPressureStatusForLawfulPair, "detected", StringComparison.Ordinal))
        {
            return BuildComparisonResult(
                leftArtifact,
                rightArtifact,
                leftSummary,
                rightSummary,
                leftField,
                rightField,
                payloadMode,
                workingIntentTransitionStatus,
                supportTypeCompatibility,
                supportIdentityCompatibility,
                counterfeitPressureStatusForLawfulPair,
                sharedSupportSignalCount,
                sharedValidationQuestionCount,
                workingIntentRankDelta,
                0d,
                "CounterfeitOrUnsupported",
                "The pair remains typed as the same support class, but burden-preservation and support-identity signals fall below lawful support-comparison thresholds.",
                [
                    $"Support identity compatibility: {supportIdentityCompatibility}.",
                    $"Shared support signals: {sharedSupportSignalCount}.",
                    $"Shared validation questions: {sharedValidationQuestionCount}."
                ],
                leftValidation,
                rightValidation);
        }

        var similarityScore = BuildSimilarityScore(
            leftField,
            rightField,
            sharedSupportSignalCount,
            sharedValidationQuestionCount,
            workingIntentTransitionStatus,
            supportIdentityCompatibility,
            workingIntentRankDelta,
            constraintEnergyDelta);
        var classification = Classify(
            workingIntentTransitionStatus,
            supportIdentityCompatibility,
            counterfeitPressureStatusForLawfulPair,
            workingIntentRankDelta,
            similarityScore,
            constraintEnergyDelta);

        return BuildComparisonResult(
            leftArtifact,
            rightArtifact,
            leftSummary,
            rightSummary,
            leftField,
            rightField,
            payloadMode,
            workingIntentTransitionStatus,
            supportTypeCompatibility,
            supportIdentityCompatibility,
            counterfeitPressureStatusForLawfulPair,
            sharedSupportSignalCount,
            sharedValidationQuestionCount,
            workingIntentRankDelta,
            similarityScore,
            classification,
            BuildClassificationReason(classification),
            BuildSignals(
                leftSummary,
                rightSummary,
                leftField,
                rightField,
                workingIntentTransitionStatus,
                supportIdentityCompatibility,
                counterfeitPressureStatusForLawfulPair,
                sharedSupportSignalCount,
                sharedValidationQuestionCount,
                workingIntentRankDelta,
                similarityScore,
                constraintEnergyDelta,
                classification),
            leftValidation,
            rightValidation);
    }

    private static EngramSupportComparisonResult BuildUnsupportedResult(
        LoadedHopngArtifact leftArtifact,
        LoadedHopngArtifact rightArtifact,
        ValidationResult leftValidation,
        ValidationResult rightValidation,
        string payloadMode,
        string classification,
        string reason) =>
        new()
        {
            LeftArtifactId = leftArtifact.Manifest.ArtifactId,
            RightArtifactId = rightArtifact.Manifest.ArtifactId,
            WorkingIntentTransitionStatus = "Unavailable",
            SupportTypeCompatibility = "Unavailable",
            SupportIdentityCompatibility = "Unavailable",
            CounterfeitPressureStatus = "unavailable",
            Classification = classification,
            ClassificationReason = reason,
            PayloadMode = payloadMode,
            Signals = [reason],
            LeftIssues = leftValidation.Errors.Select(issue => $"{issue.Code}: {issue.Message}").ToList(),
            RightIssues = rightValidation.Errors.Select(issue => $"{issue.Code}: {issue.Message}").ToList(),
            LeftValidationIssues = [.. leftValidation.Errors],
            RightValidationIssues = [.. rightValidation.Errors]
        };

    private static EngramSupportComparisonResult BuildComparisonResult(
        LoadedHopngArtifact leftArtifact,
        LoadedHopngArtifact rightArtifact,
        EngramSupportSummary leftSummary,
        EngramSupportSummary rightSummary,
        EngramStabilityFieldSummary leftField,
        EngramStabilityFieldSummary rightField,
        string payloadMode,
        string workingIntentTransitionStatus,
        string supportTypeCompatibility,
        string supportIdentityCompatibility,
        string counterfeitPressureStatus,
        int sharedSupportSignalCount,
        int sharedValidationQuestionCount,
        int? workingIntentRankDelta,
        double similarityScore,
        string classification,
        string classificationReason,
        IReadOnlyCollection<string> signals,
        ValidationResult leftValidation,
        ValidationResult rightValidation) =>
        new()
        {
            LeftArtifactId = leftArtifact.Manifest.ArtifactId,
            RightArtifactId = rightArtifact.Manifest.ArtifactId,
            LeftSupportType = leftSummary.SupportType,
            RightSupportType = rightSummary.SupportType,
            LeftWorkingIntentState = leftSummary.WorkingIntentState,
            RightWorkingIntentState = rightSummary.WorkingIntentState,
            LeftIntentClassification = leftSummary.IntentClassification,
            RightIntentClassification = rightSummary.IntentClassification,
            LeftSupportShape = leftSummary.SupportShape,
            RightSupportShape = rightSummary.SupportShape,
            LeftSupportIdentifier = ResolveSupportIdentifier(leftSummary),
            RightSupportIdentifier = ResolveSupportIdentifier(rightSummary),
            WorkingIntentTransitionStatus = workingIntentTransitionStatus,
            SupportTypeCompatibility = supportTypeCompatibility,
            SupportIdentityCompatibility = supportIdentityCompatibility,
            CounterfeitPressureStatus = counterfeitPressureStatus,
            LeftStabilityClass = leftField.StabilityClass,
            RightStabilityClass = rightField.StabilityClass,
            LeftConstraintEnergy = leftField.ConstraintEnergy,
            RightConstraintEnergy = rightField.ConstraintEnergy,
            ConstraintEnergyDelta = Math.Round(Math.Abs(leftField.ConstraintEnergy - rightField.ConstraintEnergy), 6),
            LeftBurdenPreservationScore = leftField.BurdenPreservationScore,
            RightBurdenPreservationScore = rightField.BurdenPreservationScore,
            SharedSupportSignalCount = sharedSupportSignalCount,
            SharedValidationQuestionCount = sharedValidationQuestionCount,
            WorkingIntentRankDelta = workingIntentRankDelta,
            SimilarityScore = similarityScore,
            Classification = classification,
            ClassificationReason = classificationReason,
            PayloadMode = payloadMode,
            Signals = [.. signals],
            LeftIssues = leftField.Signals,
            RightIssues = rightField.Signals,
            LeftValidationIssues = [.. leftValidation.Errors],
            RightValidationIssues = [.. rightValidation.Errors]
        };

    private static string ResolveSupportTypeCompatibility(EngramSupportSummary leftSummary, EngramSupportSummary rightSummary) =>
        string.Equals(leftSummary.SupportType, rightSummary.SupportType, StringComparison.Ordinal)
            ? "Aligned"
            : "Incompatible";

    private static string ResolveIdentityCompatibility(EngramSupportSummary leftSummary, EngramSupportSummary rightSummary)
    {
        if (string.Equals(leftSummary.SupportType, "perspectival", StringComparison.Ordinal))
        {
            if (string.Equals(leftSummary.RootFormId, rightSummary.RootFormId, StringComparison.Ordinal))
            {
                return "RootAligned";
            }

            return CountShared(leftSummary.SupportSignals, rightSummary.SupportSignals) > 0
                && string.Equals(leftSummary.EvidenceClass, rightSummary.EvidenceClass, StringComparison.Ordinal)
                && string.Equals(leftSummary.ClaimSurface, rightSummary.ClaimSurface, StringComparison.Ordinal)
                && string.Equals(leftSummary.SupportShape, rightSummary.SupportShape, StringComparison.Ordinal)
                    ? "RootTraceable"
                    : "RootDivergent";
        }

        if (string.Equals(leftSummary.SupportType, "participatory", StringComparison.Ordinal))
        {
            if (string.Equals(leftSummary.BranchSetId, rightSummary.BranchSetId, StringComparison.Ordinal))
            {
                return "BranchAligned";
            }

            return Math.Abs(leftSummary.ParticipantBranchCount - rightSummary.ParticipantBranchCount) <= 1
                && CountShared(leftSummary.SupportSignals, rightSummary.SupportSignals) > 0
                && string.Equals(leftSummary.SupportShape, rightSummary.SupportShape, StringComparison.Ordinal)
                    ? "BranchTraceable"
                    : "BranchDivergent";
        }

        return "Unavailable";
    }

    private static string ResolveCounterfeitPressure(
        EngramStabilityFieldSummary leftField,
        EngramStabilityFieldSummary rightField,
        string supportIdentityCompatibility,
        int sharedSupportSignalCount,
        int sharedValidationQuestionCount)
    {
        if (leftField.BurdenPreservationScore < 0.60d || rightField.BurdenPreservationScore < 0.60d)
        {
            return "detected";
        }

        if (supportIdentityCompatibility.EndsWith("Divergent", StringComparison.Ordinal)
            && sharedSupportSignalCount == 0
            && sharedValidationQuestionCount == 0)
        {
            return "elevated";
        }

        return "none";
    }

    private static double BuildSimilarityScore(
        EngramStabilityFieldSummary leftField,
        EngramStabilityFieldSummary rightField,
        int sharedSupportSignalCount,
        int sharedValidationQuestionCount,
        string workingIntentTransitionStatus,
        string supportIdentityCompatibility,
        int? workingIntentRankDelta,
        double constraintEnergyDelta)
    {
        var sharedSignalScore = Math.Min(1d, sharedSupportSignalCount / 3d);
        var sharedQuestionScore = Math.Min(1d, sharedValidationQuestionCount / 2d);
        var identityScore = supportIdentityCompatibility switch
        {
            "RootAligned" or "BranchAligned" => 1d,
            "RootTraceable" or "BranchTraceable" => 0.85d,
            _ => 0.35d
        };
        var intentPenalty = Math.Min(0.25d, Math.Abs(workingIntentRankDelta ?? 0) * 0.08d);
        var energyPenalty = Math.Min(0.35d, constraintEnergyDelta * 0.75d);
        var burdenPenalty = Math.Abs(leftField.BurdenPreservationScore - rightField.BurdenPreservationScore) * 0.25d;
        var transitionPenalty = workingIntentTransitionStatus switch
        {
            "Restricted" => 0.08d,
            "Deferred" => 0.12d,
            "Rejected" => 0.20d,
            _ => 0d
        };
        var rawScore = ((sharedSignalScore * 0.30d) + (sharedQuestionScore * 0.15d) + (identityScore * 0.35d) + ((1d - energyPenalty) * 0.20d))
            - intentPenalty
            - burdenPenalty
            - transitionPenalty;

        return Math.Round(Math.Max(0d, Math.Min(1d, rawScore)), 6);
    }

    private static string Classify(
        string workingIntentTransitionStatus,
        string supportIdentityCompatibility,
        string counterfeitPressureStatus,
        int? workingIntentRankDelta,
        double similarityScore,
        double constraintEnergyDelta)
    {
        if (string.Equals(counterfeitPressureStatus, "detected", StringComparison.Ordinal))
        {
            return "CounterfeitOrUnsupported";
        }

        if (string.Equals(workingIntentTransitionStatus, "Rejected", StringComparison.Ordinal))
        {
            return "RejectedSupport";
        }

        if (string.Equals(workingIntentTransitionStatus, "Deferred", StringComparison.Ordinal))
        {
            return "DeferredSupport";
        }

        if (string.Equals(workingIntentTransitionStatus, "Restricted", StringComparison.Ordinal))
        {
            return "RestrictedSupport";
        }

        if ((supportIdentityCompatibility is "RootAligned" or "RootTraceable" or "BranchAligned" or "BranchTraceable")
            && (workingIntentRankDelta ?? 0) > 0
            && similarityScore >= 0.72d
            && constraintEnergyDelta <= 0.18d)
        {
            return "StrengthenedSupport";
        }

        if ((supportIdentityCompatibility is "RootAligned" or "RootTraceable" or "BranchAligned" or "BranchTraceable")
            && similarityScore >= 0.68d
            && constraintEnergyDelta <= 0.22d)
        {
            return "CoherentSupport";
        }

        return "DivergentSupport";
    }

    private static string BuildClassificationReason(string classification) =>
        classification switch
        {
            "StrengthenedSupport" => "Both artifacts remain inside the same bounded support shape, and the right artifact advances the lawful support posture without claiming later-phase authority.",
            "CoherentSupport" => "Both artifacts remain inside one bounded support shape with aligned or traceable identity signals and low stability drift.",
            "RestrictedSupport" => "The pair remains lawful support evidence, but the right artifact moves into a restriction-bounded Phase 4 state rather than strengthening freely.",
            "DeferredSupport" => "The pair remains lawful support evidence, but the right artifact preserves an unresolved support candidate rather than advancing it.",
            "RejectedSupport" => "The pair remains lawful support history, but the right artifact records a rejected-support state rather than an admitted support continuation.",
            "DivergentSupport" => "Both artifacts remain Phase 4 support artifacts, but their support identity or stability signals no longer cohere strongly enough to be treated as one strengthened support line.",
            "CounterfeitOrUnsupported" => "At least one artifact fails lawful support comparison because support boundaries or validation posture collapse below the Phase 4 entry threshold.",
            "IncompatibleSupportType" => "Support comparison requires artifacts of the same Phase 4 support type.",
            _ => "Support comparison could not be completed as a lawful Phase 4 entry comparison."
        };

    private static List<string> BuildSignals(
        EngramSupportSummary leftSummary,
        EngramSupportSummary rightSummary,
        EngramStabilityFieldSummary leftField,
        EngramStabilityFieldSummary rightField,
        string workingIntentTransitionStatus,
        string supportIdentityCompatibility,
        string counterfeitPressureStatus,
        int sharedSupportSignalCount,
        int sharedValidationQuestionCount,
        int? workingIntentRankDelta,
        double similarityScore,
        double constraintEnergyDelta,
        string classification) =>
        [
            $"Support types compared: {leftSummary.SupportType} vs {rightSummary.SupportType}.",
            $"Intent classifications: {leftSummary.IntentClassification} vs {rightSummary.IntentClassification}.",
            $"Support shapes: {leftSummary.SupportShape} vs {rightSummary.SupportShape}.",
            $"Support identity compatibility resolved as {supportIdentityCompatibility}.",
            $"Working-intent states: {leftSummary.WorkingIntentState} vs {rightSummary.WorkingIntentState}.",
            $"Working-intent transition status: {workingIntentTransitionStatus}.",
            $"Working-intent rank delta: {(workingIntentRankDelta.HasValue ? workingIntentRankDelta.Value.ToString("+0;-0;0") : "(unavailable)")}.",
            $"Constraint energy delta: {constraintEnergyDelta:0.000000}.",
            $"Shared support signals: {sharedSupportSignalCount}.",
            $"Shared validation questions: {sharedValidationQuestionCount}.",
            $"Counterfeit pressure: {counterfeitPressureStatus}.",
            $"Similarity score: {similarityScore:0.000000}.",
            $"Comparison classified as {classification}.",
            $"Stability classes: {leftField.StabilityClass} vs {rightField.StabilityClass}.",
            $"Left state reason: {leftSummary.StateReason ?? "(none)"}; right state reason: {rightSummary.StateReason ?? "(none)"}."
        ];

    private static List<string> BuildValidationSignals(
        ValidationResult leftValidation,
        ValidationResult rightValidation,
        string reason)
    {
        var signals = new List<string>
        {
            reason,
            $"Left validation status: {(leftValidation.IsValid ? "valid" : "invalid")}.",
            $"Right validation status: {(rightValidation.IsValid ? "valid" : "invalid")}."
        };
        if (!leftValidation.IsValid)
        {
            signals.Add($"Left first validation issue: {leftValidation.Errors[0].Code}.");
        }

        if (!rightValidation.IsValid)
        {
            signals.Add($"Right first validation issue: {rightValidation.Errors[0].Code}.");
        }

        return signals;
    }

    private static int CountShared(IReadOnlyList<string> leftValues, IReadOnlyList<string> rightValues)
    {
        var leftSet = leftValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightSet = rightValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        leftSet.IntersectWith(rightSet);
        return leftSet.Count;
    }

    private static int? BuildWorkingIntentRankDelta(string leftWorkingIntentState, string rightWorkingIntentState) =>
        WorkingIntentRank.TryGetValue(leftWorkingIntentState, out var leftRank)
        && WorkingIntentRank.TryGetValue(rightWorkingIntentState, out var rightRank)
            ? rightRank - leftRank
            : null;

    private static string ResolveWorkingIntentTransitionStatus(string leftWorkingIntentState, string rightWorkingIntentState)
    {
        if (string.Equals(leftWorkingIntentState, rightWorkingIntentState, StringComparison.Ordinal))
        {
            return "Stable";
        }

        if (string.Equals(rightWorkingIntentState, "restricted_support", StringComparison.Ordinal))
        {
            return "Restricted";
        }

        if (string.Equals(rightWorkingIntentState, "deferred_support", StringComparison.Ordinal))
        {
            return "Deferred";
        }

        if (string.Equals(rightWorkingIntentState, "rejected_support", StringComparison.Ordinal))
        {
            return "Rejected";
        }

        if (string.Equals(leftWorkingIntentState, "restricted_support", StringComparison.Ordinal)
            && (string.Equals(rightWorkingIntentState, "supported_intent", StringComparison.Ordinal)
                || string.Equals(rightWorkingIntentState, "reviewable_support", StringComparison.Ordinal)))
        {
            return "Recovered";
        }

        if (string.Equals(leftWorkingIntentState, "deferred_support", StringComparison.Ordinal)
            && (string.Equals(rightWorkingIntentState, "structured_intent", StringComparison.Ordinal)
                || string.Equals(rightWorkingIntentState, "supported_intent", StringComparison.Ordinal)
                || string.Equals(rightWorkingIntentState, "reviewable_support", StringComparison.Ordinal)))
        {
            return "Resumed";
        }

        return (BuildWorkingIntentRankDelta(leftWorkingIntentState, rightWorkingIntentState) ?? 0) > 0
            ? "Strengthening"
            : "Shifted";
    }

    private static string ResolveSupportIdentifier(EngramSupportSummary summary) =>
        string.Equals(summary.SupportType, "perspectival", StringComparison.Ordinal)
            ? summary.RootFormId ?? string.Empty
            : summary.BranchSetId ?? string.Empty;
}
