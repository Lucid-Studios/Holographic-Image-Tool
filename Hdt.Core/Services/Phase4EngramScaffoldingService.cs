using Hdt.Core.Models;
using Hdt.Core.Validation;

namespace Hdt.Core.Services;

public sealed class Phase4EngramScaffoldingService
{
    private static readonly IReadOnlyDictionary<string, string> IntentClassificationByState = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["working_intent"] = "exploratory_support_claim",
        ["structured_intent"] = "typed_support_claim",
        ["supported_intent"] = "bounded_support_evidence",
        ["reviewable_support"] = "reviewable_support_evidence",
        ["restricted_support"] = "restricted_support_evidence",
        ["deferred_support"] = "deferred_support_evidence",
        ["rejected_support"] = "rejected_support_evidence"
    };

    private static readonly HashSet<string> WorkingIntentStates =
    [
        "working_intent",
        "structured_intent",
        "supported_intent",
        "reviewable_support",
        "restricted_support",
        "deferred_support",
        "rejected_support"
    ];

    private static readonly HashSet<string> InspectionPostures =
    [
        "prime_safe",
        "mixed_pointerized",
        "privileged"
    ];

    private static readonly HashSet<string> CoherenceStatuses =
    [
        "coherent_support",
        "restricted_support",
        "deferred_support",
        "rejected_support"
    ];

    public static bool HasPhase4Sidecars(LoadedHopngArtifact artifact) =>
        artifact.PerspectivalEngramSupport is not null
        || artifact.ParticipatoryEngramSupport is not null
        || artifact.Manifest.Sidecars.Any(sidecar => sidecar.Role is "perspectival-engram" or "participatory-engram");

    public IReadOnlyList<ValidationIssue> ValidateEntryScaffolding(LoadedHopngArtifact artifact)
    {
        var issues = new List<ValidationIssue>();
        var hasPerspectival = artifact.PerspectivalEngramSupport is not null
            || artifact.Manifest.Sidecars.Any(sidecar => string.Equals(sidecar.Role, "perspectival-engram", StringComparison.Ordinal));
        var hasParticipatory = artifact.ParticipatoryEngramSupport is not null
            || artifact.Manifest.Sidecars.Any(sidecar => string.Equals(sidecar.Role, "participatory-engram", StringComparison.Ordinal));

        if (!hasPerspectival && !hasParticipatory)
        {
            return issues;
        }

        if (hasPerspectival && hasParticipatory)
        {
            issues.Add(new ValidationIssue(
                ValidationErrorCode.InvalidEngramSupport,
                "Phase 4 entry artifacts must declare exactly one engram-support form, not both perspectival and participatory.",
                artifact.Layout.ManifestPath));
        }

        if (artifact.EventSliceSet is null
            || artifact.PhaseSliceSet is null
            || artifact.PhasePolicy is null
            || artifact.OpticalChannelsDefinition is null)
        {
            issues.Add(new ValidationIssue(
                ValidationErrorCode.InvalidEngramSupport,
                "Phase 4 entry artifacts must inherit the approved Phase 3 temporal sidecars before engram support is admitted.",
                artifact.Layout.ManifestPath));
        }

        if (artifact.PerspectivalEngramSupport is not null)
        {
            ValidateCommonSupport(
                artifact.PerspectivalEngramSupport.ArtifactId,
                artifact.PerspectivalEngramSupport.WorkingIntentState,
                artifact.PerspectivalEngramSupport.IntentClassification,
                artifact.PerspectivalEngramSupport.SupportOnly,
                artifact.PerspectivalEngramSupport.EvidenceClass,
                artifact.PerspectivalEngramSupport.ClaimSurface,
                artifact.PerspectivalEngramSupport.SupportShape,
                artifact.PerspectivalEngramSupport.ProvenanceBasis,
                artifact.PerspectivalEngramSupport.ConstructorSupportStatus,
                artifact.PerspectivalEngramSupport.InspectionPosture,
                artifact.PerspectivalEngramSupport.Phase5HandoffReady,
                artifact.PerspectivalEngramSupport.ValidationQuestions,
                artifact.PerspectivalEngramSupport.ProtectedEvidenceRefs,
                artifact.PerspectivalEngramSupport.RestrictionReason,
                artifact.PerspectivalEngramSupport.DeferReason,
                artifact.PerspectivalEngramSupport.RejectionReason,
                artifact.Manifest.ArtifactId,
                artifact.Layout.PerspectivalEngramPath,
                ValidationErrorCode.InvalidPerspectivalEngram,
                issues);

            if (!string.Equals(artifact.PerspectivalEngramSupport.SupportShape, "root_constructor_support", StringComparison.Ordinal))
            {
                issues.Add(new ValidationIssue(
                    ValidationErrorCode.InvalidPerspectivalEngram,
                    $"Perspectival support shape '{artifact.PerspectivalEngramSupport.SupportShape}' is not supported.",
                    artifact.Layout.PerspectivalEngramPath));
            }

            if (string.IsNullOrWhiteSpace(artifact.PerspectivalEngramSupport.RootFormId))
            {
                issues.Add(new ValidationIssue(
                    ValidationErrorCode.InvalidPerspectivalEngram,
                    "Perspectival engram support must declare a root-form id.",
                    artifact.Layout.PerspectivalEngramPath));
            }

            if (!CoherenceStatuses.Contains(artifact.PerspectivalEngramSupport.RootCoherenceStatus))
            {
                issues.Add(new ValidationIssue(
                    ValidationErrorCode.InvalidPerspectivalEngram,
                    $"Perspectival root coherence status '{artifact.PerspectivalEngramSupport.RootCoherenceStatus}' is not supported.",
                    artifact.Layout.PerspectivalEngramPath));
            }

            if (artifact.PerspectivalEngramSupport.RootCoherenceSignals.Count == 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationErrorCode.InvalidPerspectivalEngram,
                    "Perspectival engram support must declare at least one root-coherence signal.",
                    artifact.Layout.PerspectivalEngramPath));
            }
        }

        if (artifact.ParticipatoryEngramSupport is not null)
        {
            ValidateCommonSupport(
                artifact.ParticipatoryEngramSupport.ArtifactId,
                artifact.ParticipatoryEngramSupport.WorkingIntentState,
                artifact.ParticipatoryEngramSupport.IntentClassification,
                artifact.ParticipatoryEngramSupport.SupportOnly,
                artifact.ParticipatoryEngramSupport.EvidenceClass,
                artifact.ParticipatoryEngramSupport.ClaimSurface,
                artifact.ParticipatoryEngramSupport.SupportShape,
                artifact.ParticipatoryEngramSupport.ProvenanceBasis,
                artifact.ParticipatoryEngramSupport.ConstructorSupportStatus,
                artifact.ParticipatoryEngramSupport.InspectionPosture,
                artifact.ParticipatoryEngramSupport.Phase5HandoffReady,
                artifact.ParticipatoryEngramSupport.ValidationQuestions,
                artifact.ParticipatoryEngramSupport.ProtectedEvidenceRefs,
                artifact.ParticipatoryEngramSupport.RestrictionReason,
                artifact.ParticipatoryEngramSupport.DeferReason,
                artifact.ParticipatoryEngramSupport.RejectionReason,
                artifact.Manifest.ArtifactId,
                artifact.Layout.ParticipatoryEngramPath,
                ValidationErrorCode.InvalidParticipatoryEngram,
                issues);

            if (!string.Equals(artifact.ParticipatoryEngramSupport.SupportShape, "branch_set_support", StringComparison.Ordinal))
            {
                issues.Add(new ValidationIssue(
                    ValidationErrorCode.InvalidParticipatoryEngram,
                    $"Participatory support shape '{artifact.ParticipatoryEngramSupport.SupportShape}' is not supported.",
                    artifact.Layout.ParticipatoryEngramPath));
            }

            if (string.IsNullOrWhiteSpace(artifact.ParticipatoryEngramSupport.BranchSetId))
            {
                issues.Add(new ValidationIssue(
                    ValidationErrorCode.InvalidParticipatoryEngram,
                    "Participatory engram support must declare a branch-set id.",
                    artifact.Layout.ParticipatoryEngramPath));
            }

            if (!CoherenceStatuses.Contains(artifact.ParticipatoryEngramSupport.BranchCoherenceStatus))
            {
                issues.Add(new ValidationIssue(
                    ValidationErrorCode.InvalidParticipatoryEngram,
                    $"Participatory branch coherence status '{artifact.ParticipatoryEngramSupport.BranchCoherenceStatus}' is not supported.",
                    artifact.Layout.ParticipatoryEngramPath));
            }

            if (artifact.ParticipatoryEngramSupport.BranchCoherenceSignals.Count == 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationErrorCode.InvalidParticipatoryEngram,
                    "Participatory engram support must declare at least one branch-coherence signal.",
                    artifact.Layout.ParticipatoryEngramPath));
            }

            if (artifact.ParticipatoryEngramSupport.ParticipantBranches.Count < 2)
            {
                issues.Add(new ValidationIssue(
                    ValidationErrorCode.InvalidParticipatoryEngram,
                    "Participatory engram support must declare at least two participant branches.",
                    artifact.Layout.ParticipatoryEngramPath));
            }

            var branchIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var branch in artifact.ParticipatoryEngramSupport.ParticipantBranches)
            {
                if (string.IsNullOrWhiteSpace(branch.BranchId)
                    || string.IsNullOrWhiteSpace(branch.Role)
                    || !branchIds.Add(branch.BranchId))
                {
                    issues.Add(new ValidationIssue(
                        ValidationErrorCode.InvalidParticipatoryEngram,
                        "Participatory branches must declare unique branch ids and non-empty roles.",
                        artifact.Layout.ParticipatoryEngramPath));
                    break;
                }

                if (!WorkingIntentStates.Contains(branch.SupportState))
                {
                    issues.Add(new ValidationIssue(
                        ValidationErrorCode.InvalidParticipatoryEngram,
                        $"Participatory branch support state '{branch.SupportState}' is not supported.",
                        artifact.Layout.ParticipatoryEngramPath));
                }
            }
        }

        return issues;
    }

    public EngramSupportSummary? BuildPrimeSafeSummary(LoadedHopngArtifact artifact)
    {
        if (artifact.PerspectivalEngramSupport is not null)
        {
            return new EngramSupportSummary
            {
                SupportType = "perspectival",
                WorkingIntentState = artifact.PerspectivalEngramSupport.WorkingIntentState,
                IntentClassification = artifact.PerspectivalEngramSupport.IntentClassification,
                SupportOnly = artifact.PerspectivalEngramSupport.SupportOnly,
                EvidenceClass = artifact.PerspectivalEngramSupport.EvidenceClass,
                ClaimSurface = artifact.PerspectivalEngramSupport.ClaimSurface,
                SupportShape = artifact.PerspectivalEngramSupport.SupportShape,
                InspectionPosture = artifact.PerspectivalEngramSupport.InspectionPosture,
                Phase5HandoffReady = artifact.PerspectivalEngramSupport.Phase5HandoffReady,
                RootFormId = artifact.PerspectivalEngramSupport.RootFormId,
                StateReason = ResolveStateReason(
                    artifact.PerspectivalEngramSupport.WorkingIntentState,
                    artifact.PerspectivalEngramSupport.RestrictionReason,
                    artifact.PerspectivalEngramSupport.DeferReason,
                    artifact.PerspectivalEngramSupport.RejectionReason),
                SupportSignals = artifact.PerspectivalEngramSupport.RootCoherenceSignals,
                ValidationQuestions = artifact.PerspectivalEngramSupport.ValidationQuestions
            };
        }

        if (artifact.ParticipatoryEngramSupport is not null)
        {
            return new EngramSupportSummary
            {
                SupportType = "participatory",
                WorkingIntentState = artifact.ParticipatoryEngramSupport.WorkingIntentState,
                IntentClassification = artifact.ParticipatoryEngramSupport.IntentClassification,
                SupportOnly = artifact.ParticipatoryEngramSupport.SupportOnly,
                EvidenceClass = artifact.ParticipatoryEngramSupport.EvidenceClass,
                ClaimSurface = artifact.ParticipatoryEngramSupport.ClaimSurface,
                SupportShape = artifact.ParticipatoryEngramSupport.SupportShape,
                InspectionPosture = artifact.ParticipatoryEngramSupport.InspectionPosture,
                Phase5HandoffReady = artifact.ParticipatoryEngramSupport.Phase5HandoffReady,
                BranchSetId = artifact.ParticipatoryEngramSupport.BranchSetId,
                ParticipantBranchCount = artifact.ParticipatoryEngramSupport.ParticipantBranches.Count,
                StateReason = ResolveStateReason(
                    artifact.ParticipatoryEngramSupport.WorkingIntentState,
                    artifact.ParticipatoryEngramSupport.RestrictionReason,
                    artifact.ParticipatoryEngramSupport.DeferReason,
                    artifact.ParticipatoryEngramSupport.RejectionReason),
                SupportSignals = artifact.ParticipatoryEngramSupport.BranchCoherenceSignals,
                ValidationQuestions = artifact.ParticipatoryEngramSupport.ValidationQuestions
            };
        }

        return null;
    }

    private static void ValidateCommonSupport(
        string artifactId,
        string workingIntentState,
        string intentClassification,
        bool supportOnly,
        string evidenceClass,
        string claimSurface,
        string supportShape,
        string provenanceBasis,
        string constructorSupportStatus,
        string inspectionPosture,
        bool phase5HandoffReady,
        IReadOnlyCollection<string> validationQuestions,
        IReadOnlyCollection<ProtectedEvidenceReference> protectedEvidenceRefs,
        string? restrictionReason,
        string? deferReason,
        string? rejectionReason,
        string manifestArtifactId,
        string path,
        ValidationErrorCode issueCode,
        ICollection<ValidationIssue> issues)
    {
        if (!string.Equals(artifactId, manifestArtifactId, StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                "Engram support artifact id must match the parent manifest artifact id.",
                path));
        }

        if (!WorkingIntentStates.Contains(workingIntentState))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                $"Working-intent state '{workingIntentState}' is not supported in Phase 4 entry scaffolding.",
                path));
        }

        if (!IntentClassificationByState.TryGetValue(workingIntentState, out var expectedIntentClassification)
            || !string.Equals(intentClassification, expectedIntentClassification, StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                $"Intent classification '{intentClassification}' does not match the working-intent state '{workingIntentState}'.",
                path));
        }

        if (!supportOnly)
        {
            issues.Add(new ValidationIssue(
                issueCode,
                "Phase 4 entry scaffolding must remain support-only and must not assert constitutive identity authority.",
                path));
        }

        if (!string.Equals(evidenceClass, "engram_candidacy_evidence", StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                $"Evidence class '{evidenceClass}' is not supported for Phase 4 entry scaffolding.",
                path));
        }

        if (!string.Equals(claimSurface, "candidate_support_evidence", StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                $"Claim surface '{claimSurface}' is not support-only.",
                path));
        }

        if (string.IsNullOrWhiteSpace(supportShape))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                "Phase 4 entry scaffolding must declare a support shape.",
                path));
        }

        if (string.IsNullOrWhiteSpace(provenanceBasis))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                "Phase 4 entry scaffolding must declare a provenance basis.",
                path));
        }

        if (string.IsNullOrWhiteSpace(constructorSupportStatus))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                "Phase 4 entry scaffolding must declare constructor support status.",
                path));
        }

        if (!InspectionPostures.Contains(inspectionPosture))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                $"Inspection posture '{inspectionPosture}' is not supported in Phase 4 entry scaffolding.",
                path));
        }

        if (validationQuestions.Count == 0)
        {
            issues.Add(new ValidationIssue(
                issueCode,
                "Phase 4 entry scaffolding must keep at least one explicit validation question.",
                path));
        }

        if (protectedEvidenceRefs.Count == 0)
        {
            issues.Add(new ValidationIssue(
                issueCode,
                "Phase 4 entry scaffolding must retain at least one protected evidence reference.",
                path));
        }

        if (phase5HandoffReady && !string.Equals(workingIntentState, "reviewable_support", StringComparison.Ordinal))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                "Phase 5 handoff readiness may only be asserted from the 'reviewable_support' stance.",
                path));
        }

        ValidateStateReasonField(
            workingIntentState,
            "restricted_support",
            restrictionReason,
            "restriction reason",
            path,
            issueCode,
            issues);
        ValidateStateReasonField(
            workingIntentState,
            "deferred_support",
            deferReason,
            "defer reason",
            path,
            issueCode,
            issues);
        ValidateStateReasonField(
            workingIntentState,
            "rejected_support",
            rejectionReason,
            "rejection reason",
            path,
            issueCode,
            issues);
    }

    private static string? ResolveStateReason(
        string workingIntentState,
        string? restrictionReason,
        string? deferReason,
        string? rejectionReason) =>
        workingIntentState switch
        {
            "restricted_support" => restrictionReason,
            "deferred_support" => deferReason,
            "rejected_support" => rejectionReason,
            _ => null
        };

    private static void ValidateStateReasonField(
        string state,
        string gatedState,
        string? value,
        string label,
        string path,
        ValidationErrorCode issueCode,
        ICollection<ValidationIssue> issues)
    {
        if (string.Equals(state, gatedState, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add(new ValidationIssue(
                    issueCode,
                    $"Phase 4 state '{gatedState}' must declare a {label}.",
                    path));
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            issues.Add(new ValidationIssue(
                issueCode,
                $"Phase 4 state '{state}' must not carry a {label} reserved for '{gatedState}'.",
                path));
        }
    }
}
