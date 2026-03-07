using Hdt.Core.Models;
using Hdt.Core.Services;
using Hdt.Core.Validation;

namespace Hdt.Core.Diagnostics;

public sealed class ArtifactDiagnosticService
{
    private readonly GovernedProjectionDerivationService _projectionDerivationService = new();

    public ArtifactDiagnosticReport Analyze(LoadedHopngArtifact artifact, ValidationResult validationResult)
    {
        var signals = new List<string>();
        var derivation = _projectionDerivationService.Derive(artifact, validationResult);
        var hasRelationalArtifacts = artifact.UniverseLayerSet is not null
            || artifact.GluingManifest is not null
            || artifact.ProjectionRules is not null
            || artifact.LegibilityProfile is not null;
        var counterfeitRisk = validationResult.Errors.Any(error =>
            error.Code is ValidationErrorCode.SignatureMismatch
                or ValidationErrorCode.HashMismatch
                or ValidationErrorCode.DigestMismatch);
        var flattenedProjectionRisk = validationResult.Errors.Any(error =>
            error.Code is ValidationErrorCode.MissingSidecar
                or ValidationErrorCode.InvalidLayerMap
                or ValidationErrorCode.InvalidUniverseLayer
                or ValidationErrorCode.InvalidGluingManifest
                or ValidationErrorCode.InvalidProjectionRules
                or ValidationErrorCode.InvalidLegibilityProfile
                or ValidationErrorCode.InvalidVisibilityPolicy)
            || derivation.Status == ProjectionFormationStatus.StructurallyIncomplete
            || (hasRelationalArtifacts && derivation.Status == ProjectionFormationStatus.FlattenedOrUnsupported);

        if (counterfeitRisk)
        {
            signals.Add("Trust material no longer matches the artifact set.");
        }

        if (derivation.Status == ProjectionFormationStatus.LawfullyFormed)
        {
            signals.Add("Projection surface is supported by a governed relational derivation trace.");
        }
        else if (derivation.Status == ProjectionFormationStatus.StructurallyIncomplete)
        {
            signals.Add("Projection surface declares relational structure but does not satisfy lawful derivation requirements.");
        }
        else if (hasRelationalArtifacts)
        {
            signals.Add("Projection surface lacks governed Phase 2 derivation support.");
        }
        else
        {
            signals.Add("Artifact is Phase 1 only; Phase 2 governed derivation is not declared.");
        }

        if (flattenedProjectionRisk)
        {
            signals.Add("Projection surface has lost required governed sidecars or lawful relational structure.");
        }

        if (!signals.Any())
        {
            signals.Add("No immediate counterfeit or flattening signals were detected.");
        }

        return new ArtifactDiagnosticReport
        {
            CounterfeitRisk = counterfeitRisk,
            FlattenedProjectionRisk = flattenedProjectionRisk,
            ProjectionFormationStatus = derivation.Status,
            ParticipatingUniverses = derivation.ParticipatingUniverses,
            ParticipatingRelations = derivation.ParticipatingRelations,
            ProjectionIssues = derivation.Issues,
            Signals = signals
        };
    }
}
