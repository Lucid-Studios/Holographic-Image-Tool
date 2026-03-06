using Hdt.Core.Models;
using Hdt.Core.Security;
using Hdt.Core.Services;

namespace Hdt.Tests.TestSupport;

public static class Phase2ArtifactFactory
{
    public static LoadedHopngArtifact CreateValid(string tempDir, string name)
    {
        var builder = new HopngArtifactBuilder();
        var artifact = builder.Create(new NewHopngRequest(tempDir, name, "tester", "key-1"));
        var jsonStore = new ArtifactJsonStore();

        var universeLayerSet = new UniverseLayerSet
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            Universes =
            [
                new UniverseLayer
                {
                    UniverseId = "prime-projection",
                    Modality = "visual-symbolic",
                    NeutralPlane = 0,
                    ProjectionRole = "projection-surface",
                    CoordinateFrame = new CoordinateFrame
                    {
                        XAxis = "x",
                        YAxis = "y",
                        ZAxis = "z",
                        Units = "pixel-relative"
                    }
                },
                new UniverseLayer
                {
                    UniverseId = "cryptic-support",
                    Modality = "cryptic-support",
                    NeutralPlane = 0,
                    ProjectionRole = "projection-surface",
                    CoordinateFrame = new CoordinateFrame
                    {
                        XAxis = "x",
                        YAxis = "y",
                        ZAxis = "z",
                        Units = "relative-pressure"
                    }
                }
            ]
        };

        var gluingManifest = new GluingManifest
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            Relations =
            [
                new GluingRelation
                {
                    RelationId = "glue-1",
                    SourceUniverseId = "cryptic-support",
                    TargetUniverseId = "prime-projection",
                    RelationType = "projection-support",
                    RequiredForFormation = true
                }
            ]
        };

        var projectionRules = new ProjectionRules
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            Rules =
            [
                new ProjectionRule
                {
                    RuleId = "rule-1",
                    SourceUniverseId = "prime-projection",
                    TargetProjectionRole = "projection-surface",
                    MappingType = "direct",
                    Precedence = 0
                },
                new ProjectionRule
                {
                    RuleId = "rule-2",
                    SourceUniverseId = "cryptic-support",
                    TargetProjectionRole = "projection-surface",
                    MappingType = "modulated-overlay",
                    Precedence = 1
                }
            ]
        };

        var legibilityProfile = new LegibilityProfile
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            RequiredUniverses = ["prime-projection", "cryptic-support"],
            RequiredRelations = ["glue-1"],
            ProjectionIntegrityRequired = true
        };

        jsonStore.WriteCanonical(artifact.Layout.UniverseLayerPath, universeLayerSet);
        jsonStore.WriteCanonical(artifact.Layout.GluingManifestPath, gluingManifest);
        jsonStore.WriteCanonical(artifact.Layout.ProjectionRulesPath, projectionRules);
        jsonStore.WriteCanonical(artifact.Layout.LegibilityProfilePath, legibilityProfile);

        var sidecars = artifact.Manifest.Sidecars.Concat(
        [
            Sidecar("universe-layer", "oan.hopng_universe_layer", artifact.Layout.UniverseLayerPath),
            Sidecar("gluing-manifest", "oan.hopng_gluing_manifest", artifact.Layout.GluingManifestPath),
            Sidecar("projection-rules", "oan.hopng_projection_rules", artifact.Layout.ProjectionRulesPath),
            Sidecar("legibility-profile", "oan.hopng_legibility_profile", artifact.Layout.LegibilityProfilePath)
        ]).ToList();

        var fileDigests = artifact.Manifest.FileDigests.Concat(
        [
            FileDigest("universe-layer", artifact.Layout.UniverseLayerPath),
            FileDigest("gluing-manifest", artifact.Layout.GluingManifestPath),
            FileDigest("projection-rules", artifact.Layout.ProjectionRulesPath),
            FileDigest("legibility-profile", artifact.Layout.LegibilityProfilePath)
        ]).ToList();

        var manifest = artifact.Manifest with
        {
            Sidecars = sidecars,
            FileDigests = fileDigests
        };

        jsonStore.WriteCanonical(artifact.Layout.ManifestPath, manifest);

        var manifestCanonicalSha256 = ArtifactHashing.ComputeSha256(File.ReadAllBytes(artifact.Layout.ManifestPath));
        var hashSidecar = artifact.HashSidecar with
        {
            ManifestCanonicalSha256 = manifestCanonicalSha256,
            ArtifactSetSha256 = ArtifactHashing.ComputeArtifactSetSha256(fileDigests, manifestCanonicalSha256),
            FileDigests = fileDigests
        };
        jsonStore.WriteCanonical(artifact.Layout.HashPath, hashSidecar);

        var signatureService = new Ed25519SignatureService();
        var privateKey = File.ReadAllText(artifact.Layout.PrivateKeyPath).Trim();
        var hashBytes = File.ReadAllBytes(artifact.Layout.HashPath);
        var signature = signatureService.Sign(privateKey, hashBytes);
        var signatureSidecar = artifact.SignatureSidecar with
        {
            SignedObjectSha256 = ArtifactHashing.ComputeSha256(hashBytes),
            SignatureBase64 = Convert.ToBase64String(signature)
        };
        jsonStore.WriteCanonical(artifact.Layout.SignaturePath, signatureSidecar);

        return new HopngArtifactLoader().Load(artifact.Layout.ManifestPath);
    }

    public static LoadedHopngArtifact RefreshIntegrity(LoadedHopngArtifact artifact)
    {
        var jsonStore = new ArtifactJsonStore();
        var loader = new HopngArtifactLoader();
        var current = loader.Load(artifact.Layout.ManifestPath);
        var refreshedDigests = current.Manifest.FileDigests
            .Select(digest => digest with
            {
                Sha256 = ArtifactHashing.ComputeSha256(Path.Combine(current.Layout.DirectoryPath, digest.Path))
            })
            .ToList();
        var refreshedManifest = current.Manifest with
        {
            FileDigests = refreshedDigests
        };

        jsonStore.WriteCanonical(current.Layout.ManifestPath, refreshedManifest);
        current = loader.Load(current.Layout.ManifestPath);

        var manifestCanonicalSha256 = ArtifactHashing.ComputeSha256(File.ReadAllBytes(current.Layout.ManifestPath));
        var hashSidecar = current.HashSidecar with
        {
            ManifestCanonicalSha256 = manifestCanonicalSha256,
            ArtifactSetSha256 = ArtifactHashing.ComputeArtifactSetSha256(refreshedDigests, manifestCanonicalSha256),
            FileDigests = refreshedDigests
        };
        jsonStore.WriteCanonical(current.Layout.HashPath, hashSidecar);

        var signatureService = new Ed25519SignatureService();
        var privateKey = File.ReadAllText(current.Layout.PrivateKeyPath).Trim();
        var hashBytes = File.ReadAllBytes(current.Layout.HashPath);
        var signature = signatureService.Sign(privateKey, hashBytes);
        var signatureSidecar = current.SignatureSidecar with
        {
            SignedObjectSha256 = ArtifactHashing.ComputeSha256(hashBytes),
            SignatureBase64 = Convert.ToBase64String(signature)
        };
        jsonStore.WriteCanonical(current.Layout.SignaturePath, signatureSidecar);

        return loader.Load(current.Layout.ManifestPath);
    }

    private static SidecarReference Sidecar(string role, string schema, string path) =>
        new()
        {
            Role = role,
            Schema = schema,
            SchemaVersion = "0.1.0",
            Path = Path.GetFileName(path),
            Required = true
        };

    private static ArtifactFileDigest FileDigest(string role, string path) =>
        new()
        {
            Role = role,
            Path = Path.GetFileName(path),
            Sha256 = ArtifactHashing.ComputeSha256(path)
        };
}
