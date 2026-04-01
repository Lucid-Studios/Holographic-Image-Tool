using Hdt.Core.Models;
using Hdt.Core.Validation;

namespace Hdt.Core.Services;

public sealed class EngramStabilityFieldService
{
    private static readonly IReadOnlyDictionary<string, double> IntentScores = new Dictionary<string, double>(StringComparer.Ordinal)
    {
        ["working_intent"] = 0.35d,
        ["structured_intent"] = 0.50d,
        ["supported_intent"] = 0.70d,
        ["reviewable_support"] = 0.82d,
        ["restricted_support"] = 0.45d,
        ["deferred_support"] = 0.35d,
        ["rejected_support"] = 0.10d
    };

    private static readonly IReadOnlyDictionary<string, double> SupportStatusScores = new Dictionary<string, double>(StringComparer.Ordinal)
    {
        ["coherent_support"] = 0.85d,
        ["restricted_support"] = 0.45d,
        ["deferred_support"] = 0.35d,
        ["rejected_support"] = 0.10d
    };

    private static readonly IReadOnlyDictionary<string, double> TemporalStateScores = new Dictionary<string, double>(StringComparer.Ordinal)
    {
        ["Stable"] = 0.95d,
        ["RisingPressure"] = 0.80d,
        ["Drifting"] = 0.62d,
        ["Propagating"] = 0.72d,
        ["RuptureRisk"] = 0.20d,
        ["StructurallyIncomplete"] = 0.10d
    };

    public EngramStabilityFieldSummary? Build(
        LoadedHopngArtifact artifact,
        ValidationResult validationResult,
        PhaseStackRenderResult? temporalSummary)
    {
        if (artifact.PerspectivalEngramSupport is null && artifact.ParticipatoryEngramSupport is null)
        {
            return null;
        }

        return artifact.PerspectivalEngramSupport is not null
            ? BuildPerspectival(artifact.PerspectivalEngramSupport, validationResult, temporalSummary)
            : BuildParticipatory(artifact.ParticipatoryEngramSupport!, validationResult, temporalSummary);
    }

    private static EngramStabilityFieldSummary BuildPerspectival(
        PerspectivalEngramSupport support,
        ValidationResult validationResult,
        PhaseStackRenderResult? temporalSummary)
    {
        var coherenceScore = Clamp01(
            ((ResolveIntentScore(support.WorkingIntentState) + ResolveSupportStatusScore(support.RootCoherenceStatus)) / 2d)
            + Math.Min(0.15d, support.RootCoherenceSignals.Count * 0.05d));
        var burdenPreservationScore = BuildBurdenPreservationScore(
            support.SupportOnly,
            support.EvidenceClass,
            support.ClaimSurface,
            support.ProtectedEvidenceRefs.Count,
            validationResult);
        var recoveryIntegrityScore = BuildRecoveryIntegrityScore(
            support.WorkingIntentState,
            support.Phase5HandoffReady,
            validationResult,
            temporalSummary);
        var intermixStabilityScore = BuildIntermixStabilityScore(
            temporalSummary,
            support.RootCoherenceSignals.Count,
            0);
        var constraintEnergy = BuildConstraintEnergy(coherenceScore, burdenPreservationScore, recoveryIntegrityScore, intermixStabilityScore);

        return new EngramStabilityFieldSummary
        {
            SupportType = "perspectival",
            WorkingIntentState = support.WorkingIntentState,
            StabilityClass = Classify(constraintEnergy),
            ConstraintEnergy = constraintEnergy,
            CoherenceScore = coherenceScore,
            BurdenPreservationScore = burdenPreservationScore,
            RecoveryIntegrityScore = recoveryIntegrityScore,
            IntermixStabilityScore = intermixStabilityScore,
            Signals =
            [
                $"Working-intent stance '{support.WorkingIntentState}' contributes {ResolveIntentScore(support.WorkingIntentState):0.000} to coherence.",
                $"Root coherence status '{support.RootCoherenceStatus}' contributes {ResolveSupportStatusScore(support.RootCoherenceStatus):0.000} to coherence.",
                $"Protected evidence references preserved: {support.ProtectedEvidenceRefs.Count}.",
                $"Temporal basis status: {(temporalSummary?.Status.ToString() ?? "Unavailable")}."
            ]
        };
    }

    private static EngramStabilityFieldSummary BuildParticipatory(
        ParticipatoryEngramSupport support,
        ValidationResult validationResult,
        PhaseStackRenderResult? temporalSummary)
    {
        var coherenceScore = Clamp01(
            ((ResolveIntentScore(support.WorkingIntentState) + ResolveSupportStatusScore(support.BranchCoherenceStatus)) / 2d)
            + Math.Min(0.15d, support.BranchCoherenceSignals.Count * 0.05d));
        var burdenPreservationScore = BuildBurdenPreservationScore(
            support.SupportOnly,
            support.EvidenceClass,
            support.ClaimSurface,
            support.ProtectedEvidenceRefs.Count,
            validationResult);
        var recoveryIntegrityScore = BuildRecoveryIntegrityScore(
            support.WorkingIntentState,
            support.Phase5HandoffReady,
            validationResult,
            temporalSummary);
        var intermixStabilityScore = BuildIntermixStabilityScore(
            temporalSummary,
            support.BranchCoherenceSignals.Count,
            support.ParticipantBranches.Count);
        var constraintEnergy = BuildConstraintEnergy(coherenceScore, burdenPreservationScore, recoveryIntegrityScore, intermixStabilityScore);

        return new EngramStabilityFieldSummary
        {
            SupportType = "participatory",
            WorkingIntentState = support.WorkingIntentState,
            StabilityClass = Classify(constraintEnergy),
            ConstraintEnergy = constraintEnergy,
            CoherenceScore = coherenceScore,
            BurdenPreservationScore = burdenPreservationScore,
            RecoveryIntegrityScore = recoveryIntegrityScore,
            IntermixStabilityScore = intermixStabilityScore,
            Signals =
            [
                $"Working-intent stance '{support.WorkingIntentState}' contributes {ResolveIntentScore(support.WorkingIntentState):0.000} to coherence.",
                $"Branch coherence status '{support.BranchCoherenceStatus}' contributes {ResolveSupportStatusScore(support.BranchCoherenceStatus):0.000} to coherence.",
                $"Participant branches preserved: {support.ParticipantBranches.Count}.",
                $"Temporal basis status: {(temporalSummary?.Status.ToString() ?? "Unavailable")}."
            ]
        };
    }

    private static double BuildBurdenPreservationScore(
        bool supportOnly,
        string evidenceClass,
        string claimSurface,
        int protectedEvidenceRefCount,
        ValidationResult validationResult)
    {
        var score = 1d;
        if (!supportOnly)
        {
            score -= 0.40d;
        }

        if (!string.Equals(evidenceClass, "engram_candidacy_evidence", StringComparison.Ordinal))
        {
            score -= 0.20d;
        }

        if (!string.Equals(claimSurface, "candidate_support_evidence", StringComparison.Ordinal))
        {
            score -= 0.20d;
        }

        if (protectedEvidenceRefCount == 0)
        {
            score -= 0.20d;
        }

        var phase4ValidationPenalty = validationResult.Errors.Count(issue =>
            issue.Code is ValidationErrorCode.InvalidEngramSupport
                or ValidationErrorCode.InvalidPerspectivalEngram
                or ValidationErrorCode.InvalidParticipatoryEngram);
        score -= Math.Min(0.30d, phase4ValidationPenalty * 0.10d);
        return Clamp01(score);
    }

    private static double BuildRecoveryIntegrityScore(
        string workingIntentState,
        bool phase5HandoffReady,
        ValidationResult validationResult,
        PhaseStackRenderResult? temporalSummary)
    {
        var temporalScore = temporalSummary is null
            ? 0.20d
            : temporalSummary.Status == TemporalStackStatus.LawfullyDerived
                ? ResolveTemporalStateScore(temporalSummary.StateSummaries.LastOrDefault()?.StateClass)
                : 0.10d;
        var score = (ResolveIntentScore(workingIntentState) + temporalScore) / 2d;

        if (phase5HandoffReady && !string.Equals(workingIntentState, "reviewable_support", StringComparison.Ordinal))
        {
            score -= 0.25d;
        }

        score -= Math.Min(0.25d, validationResult.Errors.Count * 0.03d);
        return Clamp01(score);
    }

    private static double BuildIntermixStabilityScore(
        PhaseStackRenderResult? temporalSummary,
        int supportSignalCount,
        int participantBranchCount)
    {
        if (temporalSummary is null)
        {
            return 0.25d;
        }

        var score = 0.82d;
        score -= Math.Min(0.35d, temporalSummary.DriftFlags.Count * 0.035d);
        score -= Math.Min(0.25d, temporalSummary.TopologyChangeFlags.Count * 0.08d);
        score += Math.Min(0.10d, supportSignalCount * 0.03d);
        if (participantBranchCount >= 2)
        {
            score += 0.08d;
        }

        return Clamp01(score);
    }

    private static double BuildConstraintEnergy(
        double coherenceScore,
        double burdenPreservationScore,
        double recoveryIntegrityScore,
        double intermixStabilityScore)
    {
        var stability = (coherenceScore + burdenPreservationScore + recoveryIntegrityScore + intermixStabilityScore) / 4d;
        return Math.Round(1d - Clamp01(stability), 6);
    }

    private static string Classify(double constraintEnergy) =>
        constraintEnergy switch
        {
            <= 0.20d => "lawful_settling",
            <= 0.40d => "bounded_transition",
            <= 0.65d => "high_energy_instability",
            _ => "rejected_energy"
        };

    private static double ResolveIntentScore(string workingIntentState) =>
        IntentScores.TryGetValue(workingIntentState, out var score)
            ? score
            : 0.10d;

    private static double ResolveSupportStatusScore(string supportStatus) =>
        SupportStatusScores.TryGetValue(supportStatus, out var score)
            ? score
            : 0.10d;

    private static double ResolveTemporalStateScore(string? temporalState) =>
        !string.IsNullOrWhiteSpace(temporalState) && TemporalStateScores.TryGetValue(temporalState, out var score)
            ? score
            : 0.10d;

    private static double Clamp01(double value) => Math.Max(0d, Math.Min(1d, Math.Round(value, 6)));
}
