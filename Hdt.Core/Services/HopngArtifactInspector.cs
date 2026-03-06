using Hdt.Core.Diagnostics;
using Hdt.Core.Models;
using Hdt.Core.Validation;

namespace Hdt.Core.Services;

public sealed class HopngArtifactInspector
{
    private readonly ArtifactDiagnosticService _diagnosticService = new();
    private readonly GovernedProjectionDerivationService _projectionDerivationService = new();

    public object BuildPrimeSafeView(LoadedHopngArtifact artifact, ValidationResult validationResult)
    {
        var approved = artifact.Manifest.VisibilityPolicy.ApprovedMetadataFields;
        var summary = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["artifactId"] = artifact.Manifest.ArtifactId,
            ["displayName"] = artifact.Manifest.DisplayName,
            ["createdUtc"] = artifact.Manifest.CreatedUtc,
            ["projectionFile"] = artifact.Manifest.ProjectionFile
        };

        var metadata = summary
            .Where(pair => approved.Contains(pair.Key, StringComparer.Ordinal))
            .ToDictionary();

        return new
        {
            view = "prime",
            metadata,
            crypticPointers = artifact.Manifest.VisibilityPolicy.CrypticReferences.Select(reference => new
            {
                reference.Id,
                reference.PointerUri,
                reference.Policy,
                reference.Summary
            }),
            diagnostics = _diagnosticService.Analyze(artifact, validationResult),
            isValid = validationResult.IsValid
        };
    }

    public object BuildPrivilegedView(LoadedHopngArtifact artifact, ValidationResult validationResult) => new
    {
        view = "privileged",
        manifest = artifact.Manifest,
        layerMap = artifact.LayerMap,
        universeLayerSet = artifact.UniverseLayerSet,
        gluingManifest = artifact.GluingManifest,
        projectionRules = artifact.ProjectionRules,
        legibilityProfile = artifact.LegibilityProfile,
        trustEnvelope = artifact.TrustEnvelope,
        transformHistory = artifact.TransformHistory,
        depthField = artifact.DepthField,
        hashSidecar = artifact.HashSidecar,
        signatureSidecar = artifact.SignatureSidecar,
        governedProjection = _projectionDerivationService.Derive(artifact, validationResult),
        diagnostics = _diagnosticService.Analyze(artifact, validationResult),
        isValid = validationResult.IsValid,
        errors = validationResult.Errors
    };
}
