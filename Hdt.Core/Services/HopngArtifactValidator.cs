using System.Text.Json;
using Hdt.Core.Artifacts;
using Hdt.Core.Models;
using Hdt.Core.Security;
using Hdt.Core.Validation;
using Hdt.Schemas;

namespace Hdt.Core.Services;

public sealed class HopngArtifactValidator
{
    private static readonly IReadOnlyDictionary<string, string> Phase2RoleSchemaMap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["universe-layer"] = "oan.hopng_universe_layer",
        ["gluing-manifest"] = "oan.hopng_gluing_manifest",
        ["projection-rules"] = "oan.hopng_projection_rules",
        ["legibility-profile"] = "oan.hopng_legibility_profile"
    };

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

        ValidateReferencedSidecars(artifact, result);
        ValidateOptionalPhase2Schemas(artifact, result);
        ValidateLawfulStructure(artifact, result);
        ValidateDigests(artifact, result);
        ValidateHashSidecar(artifact, result);
        ValidateSignature(artifact, result);
        ValidateVisibilityPolicy(artifact, result);
        ValidateRelationalStructure(artifact, result);

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

    private void ValidateOptionalPhase2Schemas(LoadedHopngArtifact artifact, ValidationResult result)
    {
        foreach (var pair in Phase2RoleSchemaMap)
        {
            var sidecar = artifact.Manifest.Sidecars.FirstOrDefault(candidate => string.Equals(candidate.Role, pair.Key, StringComparison.Ordinal));
            if (sidecar is null)
            {
                continue;
            }

            var path = Path.Combine(artifact.Layout.DirectoryPath, sidecar.Path);
            if (File.Exists(path))
            {
                ValidateSchema(path, pair.Value, result);
            }
        }
    }

    private static void ValidateReferencedSidecars(LoadedHopngArtifact artifact, ValidationResult result)
    {
        foreach (var sidecar in artifact.Manifest.Sidecars)
        {
            var fullPath = Path.Combine(artifact.Layout.DirectoryPath, sidecar.Path);
            if (!File.Exists(fullPath))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, $"Referenced sidecar '{sidecar.Role}' is missing.", fullPath));
                continue;
            }

            if (sidecar.Role is "hash" or "signature")
            {
                continue;
            }

            var digestExists = artifact.Manifest.FileDigests.Any(digest =>
                string.Equals(digest.Role, sidecar.Role, StringComparison.Ordinal)
                && string.Equals(digest.Path, sidecar.Path, StringComparison.Ordinal));

            if (!digestExists)
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, $"Referenced sidecar '{sidecar.Role}' is missing a manifest digest entry.", artifact.Layout.ManifestPath));
            }
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

    private static void ValidateRelationalStructure(LoadedHopngArtifact artifact, ValidationResult result)
    {
        var hasPhase2References = artifact.Manifest.Sidecars.Any(sidecar => Phase2RoleSchemaMap.ContainsKey(sidecar.Role));
        if (!hasPhase2References)
        {
            return;
        }

        var universeLayerSet = artifact.UniverseLayerSet;
        if (universeLayerSet is null)
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Phase 2 relational artifacts must declare a universe-layer sidecar.", artifact.Layout.ManifestPath));
            return;
        }

        ValidateUniverseLayerSet(artifact, universeLayerSet, result);
        ValidateGluingManifest(artifact, universeLayerSet, result);
        ValidateProjectionRules(artifact, universeLayerSet, result);
        ValidateLegibilityProfile(artifact, universeLayerSet, result);
    }

    private static void ValidateUniverseLayerSet(LoadedHopngArtifact artifact, UniverseLayerSet universeLayerSet, ValidationResult result)
    {
        if (universeLayerSet.Universes.Count == 0)
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidUniverseLayer, "At least one universe must be declared for relational artifacts.", artifact.Layout.UniverseLayerPath));
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var universe in universeLayerSet.Universes)
        {
            if (string.IsNullOrWhiteSpace(universe.UniverseId) || !seenIds.Add(universe.UniverseId))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidUniverseLayer, "Universe ids must be present and unique.", artifact.Layout.UniverseLayerPath));
            }

            if (string.IsNullOrWhiteSpace(universe.Modality) || string.IsNullOrWhiteSpace(universe.ProjectionRole))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidUniverseLayer, $"Universe '{universe.UniverseId}' is missing modality or projection role.", artifact.Layout.UniverseLayerPath));
            }

            if (string.IsNullOrWhiteSpace(universe.CoordinateFrame.XAxis)
                || string.IsNullOrWhiteSpace(universe.CoordinateFrame.YAxis)
                || string.IsNullOrWhiteSpace(universe.CoordinateFrame.ZAxis))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidUniverseLayer, $"Universe '{universe.UniverseId}' must declare x, y, and z axes.", artifact.Layout.UniverseLayerPath));
            }
        }
    }

    private static void ValidateGluingManifest(LoadedHopngArtifact artifact, UniverseLayerSet universeLayerSet, ValidationResult result)
    {
        if (artifact.GluingManifest is null)
        {
            if (artifact.Manifest.Sidecars.Any(sidecar => string.Equals(sidecar.Role, "gluing-manifest", StringComparison.Ordinal)))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Referenced gluing manifest could not be loaded.", artifact.Layout.GluingManifestPath));
            }

            return;
        }

        if (artifact.GluingManifest.Relations.Count == 0)
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidGluingManifest, "Gluing manifest must declare at least one relation.", artifact.Layout.GluingManifestPath));
            return;
        }

        var universeIds = universeLayerSet.Universes.Select(universe => universe.UniverseId).ToHashSet(StringComparer.Ordinal);
        var relationIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var relation in artifact.GluingManifest.Relations)
        {
            if (string.IsNullOrWhiteSpace(relation.RelationId) || !relationIds.Add(relation.RelationId))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidGluingManifest, "Gluing relation ids must be present and unique.", artifact.Layout.GluingManifestPath));
            }

            if (!universeIds.Contains(relation.SourceUniverseId) || !universeIds.Contains(relation.TargetUniverseId))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidGluingManifest, $"Gluing relation '{relation.RelationId}' references an unknown universe.", artifact.Layout.GluingManifestPath));
            }

            if (string.IsNullOrWhiteSpace(relation.RelationType))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidGluingManifest, $"Gluing relation '{relation.RelationId}' must declare a relation type.", artifact.Layout.GluingManifestPath));
            }
        }
    }

    private static void ValidateProjectionRules(LoadedHopngArtifact artifact, UniverseLayerSet universeLayerSet, ValidationResult result)
    {
        if (artifact.ProjectionRules is null)
        {
            if (artifact.Manifest.Sidecars.Any(sidecar => string.Equals(sidecar.Role, "projection-rules", StringComparison.Ordinal)))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Referenced projection rules could not be loaded.", artifact.Layout.ProjectionRulesPath));
            }

            return;
        }

        if (artifact.ProjectionRules.Rules.Count == 0)
        {
            result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidProjectionRules, "Projection rules must declare at least one rule.", artifact.Layout.ProjectionRulesPath));
            return;
        }

        var universeIds = universeLayerSet.Universes.Select(universe => universe.UniverseId).ToHashSet(StringComparer.Ordinal);
        var projectionRoles = artifact.LayerMap.Layers.Select(layer => layer.ProjectionRole).ToHashSet(StringComparer.Ordinal);
        var ruleIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in artifact.ProjectionRules.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.RuleId) || !ruleIds.Add(rule.RuleId))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidProjectionRules, "Projection rule ids must be present and unique.", artifact.Layout.ProjectionRulesPath));
            }

            if (!universeIds.Contains(rule.SourceUniverseId))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidProjectionRules, $"Projection rule '{rule.RuleId}' references an unknown universe.", artifact.Layout.ProjectionRulesPath));
            }

            if (!projectionRoles.Contains(rule.TargetProjectionRole))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidProjectionRules, $"Projection rule '{rule.RuleId}' targets an unknown projection role.", artifact.Layout.ProjectionRulesPath));
            }

            if (string.IsNullOrWhiteSpace(rule.MappingType))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidProjectionRules, $"Projection rule '{rule.RuleId}' must declare a mapping type.", artifact.Layout.ProjectionRulesPath));
            }
        }
    }

    private static void ValidateLegibilityProfile(LoadedHopngArtifact artifact, UniverseLayerSet universeLayerSet, ValidationResult result)
    {
        if (artifact.LegibilityProfile is null)
        {
            if (artifact.Manifest.Sidecars.Any(sidecar => string.Equals(sidecar.Role, "legibility-profile", StringComparison.Ordinal)))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.MissingSidecar, "Referenced legibility profile could not be loaded.", artifact.Layout.LegibilityProfilePath));
            }

            return;
        }

        var universeIds = universeLayerSet.Universes.Select(universe => universe.UniverseId).ToHashSet(StringComparer.Ordinal);
        var relationIds = (artifact.GluingManifest?.Relations ?? []).Select(relation => relation.RelationId).ToHashSet(StringComparer.Ordinal);
        var projectedUniverseIds = (artifact.ProjectionRules?.Rules ?? []).Select(rule => rule.SourceUniverseId).ToHashSet(StringComparer.Ordinal);

        foreach (var requiredUniverse in artifact.LegibilityProfile.RequiredUniverses)
        {
            if (!universeIds.Contains(requiredUniverse))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidLegibilityProfile, $"Legibility profile requires unknown universe '{requiredUniverse}'.", artifact.Layout.LegibilityProfilePath));
            }
        }

        foreach (var requiredRelation in artifact.LegibilityProfile.RequiredRelations)
        {
            if (!relationIds.Contains(requiredRelation))
            {
                result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidLegibilityProfile, $"Legibility profile requires unknown relation '{requiredRelation}'.", artifact.Layout.LegibilityProfilePath));
            }
        }

        if (artifact.LegibilityProfile.ProjectionIntegrityRequired)
        {
            foreach (var requiredUniverse in artifact.LegibilityProfile.RequiredUniverses)
            {
                if (!projectedUniverseIds.Contains(requiredUniverse))
                {
                    result.Errors.Add(new ValidationIssue(ValidationErrorCode.InvalidLegibilityProfile, $"Legibility profile requires projection integrity for universe '{requiredUniverse}'.", artifact.Layout.LegibilityProfilePath));
                }
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
