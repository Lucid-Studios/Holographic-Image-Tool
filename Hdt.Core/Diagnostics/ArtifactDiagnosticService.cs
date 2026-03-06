using Hdt.Core.Models;
using Hdt.Core.Validation;

namespace Hdt.Core.Diagnostics;

public sealed class ArtifactDiagnosticService
{
    public ArtifactDiagnosticReport Analyze(LoadedHopngArtifact artifact, ValidationResult validationResult)
    {
        var signals = new List<string>();
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
                or ValidationErrorCode.InvalidVisibilityPolicy);

        if (counterfeitRisk)
        {
            signals.Add("Trust material no longer matches the artifact set.");
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
            Signals = signals
        };
    }
}
