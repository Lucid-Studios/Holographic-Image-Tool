using System.Text.Json;
using Hdt.Core.Artifacts;
using Hdt.Core.Models;
using Hdt.Core.Security;
using Hdt.Core.Validation;
using Hdt.Schemas;

namespace Hdt.Core.Services;

public sealed class HopngArtifactValidator
{
    private readonly ArtifactJsonStore _jsonStore = new();
    private readonly HopngArtifactLoader _loader = new();
    private readonly Ed25519SignatureService _signatureService = new();

    public ValidationResult Validate(string path)
    {
        var result = new ValidationResult();
        var layout = HopngArtifactLayout.FromPath(path);
        var requiredPaths = new[]
        {
            layout.ProjectionPath,
            layout.ManifestPath,
            layout.LayerMapPath,
            layout.TrustEnvelopePath,
            layout.TransformHistoryPath,
            layout.DepthFieldPath,
            layout.HashPath,
            layout.SignaturePath
        };

        foreach (var requiredPath in requiredPaths)
        {
            if (!File.Exists(requiredPath))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.MissingFile, "Required artifact file is missing.", requiredPath));
            }
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        ValidateSchema(layout.ManifestPath, "oan.hopng_manifest", result);
        ValidateSchema(layout.LayerMapPath, "oan.hopng_layer_map", result);
        ValidateSchema(layout.TrustEnvelopePath, "oan.hopng_trust_envelope", result);
        ValidateSchema(layout.TransformHistoryPath, "oan.hopng_transform_history", result);
        ValidateSchema(layout.DepthFieldPath, "oan.hopng_depth_field", result);

        LoadedHopngArtifact artifact;
        try
        {
            artifact = _loader.Load(path);
        }
        catch (JsonException ex)
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidJson, ex.Message, path));
            return result;
        }

        ValidateLawfulStructure(artifact, result);
        ValidateDigests(artifact, result);
        ValidateHashSidecar(artifact, result);
        ValidateSignature(artifact, result);
        ValidateVisibilityPolicy(artifact, result);

        return result;
    }

    private void ValidateSchema(string path, string logicalName, ValidationResult result)
    {
        try
        {
            using var document = _jsonStore.ReadDocument(path);
            var root = document.RootElement;
            var schema = root.GetProperty("schema").GetString();
            var schemaVersion = root.GetProperty("schemaVersion").GetString();

            if (!string.Equals(schema, logicalName, StringComparison.Ordinal))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.SchemaMismatch, $"Schema '{schema}' does not match expected '{logicalName}'.", path));
                return;
            }

            if (!SchemaCatalog.TryGet(schema!, schemaVersion!, out var definition))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.UnsupportedSchema, $"Schema '{schema}' version '{schemaVersion}' is not supported.", path));
                return;
            }

            foreach (var property in definition.RequiredProperties)
            {
                if (!root.TryGetProperty(property, out _))
                {
                    result.Errors.Add(new ValidationIssue(ValidationErrorCode.SchemaMismatch, $"Required property '{property}' is missing.", path));
                }
            }
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.SchemaMismatch, ex.Message, path));
        }
    }

    private static void ValidateLawfulStructure(LoadedHopngArtifact artifact, ValidationResult result)
    {
        if (artifact.LayerMap.Layers.Count == 0)
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidLayerMap, "At least one coordinate-bound layer is required.", artifact.Layout.LayerMapPath));
        }

        foreach (var layer in artifact.LayerMap.Layers)
        {
            if (string.IsNullOrWhiteSpace(layer.ProjectionRole) || layer.CoordinateFrame is null)
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidLayerMap, $"Layer '{layer.LayerId}' is missing projection or coordinate frame information.", artifact.Layout.LayerMapPath));
                continue;
            }

            if (string.IsNullOrWhiteSpace(layer.CoordinateFrame.XAxis)
                || string.IsNullOrWhiteSpace(layer.CoordinateFrame.YAxis)
                || string.IsNullOrWhiteSpace(layer.CoordinateFrame.ZAxis))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidLayerMap, $"Layer '{layer.LayerId}' must declare x, y, and z axes.", artifact.Layout.LayerMapPath));
            }
        }

        if (!artifact.Manifest.FileDigests.Any(digest => string.Equals(digest.Role, "projection", StringComparison.Ordinal)))
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Manifest must declare the visible projection digest.", artifact.Layout.ManifestPath));
        }
    }

    private static void ValidateDigests(LoadedHopngArtifact artifact, ValidationResult result)
    {
        foreach (var digest in artifact.Manifest.FileDigests)
        {
            var fullPath = Path.Combine(artifact.Layout.DirectoryPath, digest.Path);
            if (!File.Exists(fullPath))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, $"Referenced file '{digest.Path}' is missing.", fullPath));
                continue;
            }

            var actualDigest = ArtifactHashing.ComputeSha256(fullPath);
            if (!string.Equals(actualDigest, digest.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.DigestMismatch, $"Digest mismatch for '{digest.Path}'.", fullPath));
            }
        }
    }

    private static void ValidateHashSidecar(LoadedHopngArtifact artifact, ValidationResult result)
    {
        var manifestCanonicalSha256 = ArtifactHashing.ComputeSha256(File.ReadAllBytes(artifact.Layout.ManifestPath));
        if (!string.Equals(artifact.HashSidecar.ManifestCanonicalSha256, manifestCanonicalSha256, StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.HashMismatch, "Manifest canonical hash does not match the manifest sidecar.", artifact.Layout.HashPath));
        }

        var artifactSetSha256 = ArtifactHashing.ComputeArtifactSetSha256(artifact.Manifest.FileDigests, manifestCanonicalSha256);
        if (!string.Equals(artifact.HashSidecar.ArtifactSetSha256, artifactSetSha256, StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.HashMismatch, "Artifact set hash does not match the manifest digest set.", artifact.Layout.HashPath));
        }
    }

    private void ValidateSignature(LoadedHopngArtifact artifact, ValidationResult result)
    {
        var hashBytes = File.ReadAllBytes(artifact.Layout.HashPath);
        var hashDigest = ArtifactHashing.ComputeSha256(hashBytes);
        if (!string.Equals(artifact.SignatureSidecar.SignedObjectSha256, hashDigest, StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.SignatureMismatch, "Signature sidecar is bound to the wrong hash payload.", artifact.Layout.SignaturePath));
            return;
        }

        var signatureBytes = Convert.FromBase64String(artifact.SignatureSidecar.SignatureBase64);
        if (!_signatureService.Verify(artifact.TrustEnvelope.PublicKey, hashBytes, signatureBytes))
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.SignatureMismatch, "Signature verification failed.", artifact.Layout.SignaturePath));
        }
    }

    private static void ValidateVisibilityPolicy(LoadedHopngArtifact artifact, ValidationResult result)
    {
        var policy = artifact.Manifest.VisibilityPolicy;
        if (!policy.PrimeSafeProjection)
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidVisibilityPolicy, "Prime-safe projection must remain enabled for v1 artifacts.", artifact.Layout.ManifestPath));
        }

        if (policy.CrypticReferences.Count > 0 && !policy.CrypticPointersAllowed)
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidVisibilityPolicy, "Cryptic references require pointer-only policy approval.", artifact.Layout.ManifestPath));
        }

        foreach (var reference in policy.CrypticReferences)
        {
            if (string.IsNullOrWhiteSpace(reference.PointerUri) || !reference.PointerUri.Contains("://", StringComparison.Ordinal))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidVisibilityPolicy, $"Cryptic reference '{reference.Id}' must be pointer-only.", artifact.Layout.ManifestPath));
            }
        }
    }
}
