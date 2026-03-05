using Hdt.Core.Artifacts;
using Hdt.Core.Models;
using Hdt.Core.Security;
using Hdt.Schemas;

namespace Hdt.Core.Services;

public sealed class HopngArtifactBuilder
{
    private static readonly byte[] PlaceholderPng =
    [
        137, 80, 78, 71, 13, 10, 26, 10,
        0, 0, 0, 13, 73, 72, 68, 82,
        0, 0, 0, 1, 0, 0, 0, 1,
        8, 6, 0, 0, 0, 31, 21, 196,
        137, 0, 0, 0, 13, 73, 68, 65,
        84, 120, 156, 99, 248, 15, 4, 0,
        9, 251, 3, 253, 167, 26, 129, 165,
        0, 0, 0, 0, 73, 69, 78, 68,
        174, 66, 96, 130
    ];

    private readonly ArtifactJsonStore _jsonStore = new();
    private readonly Ed25519SignatureService _signatureService = new();

    public LoadedHopngArtifact Create(NewHopngRequest request)
    {
        var outputDirectory = Path.GetFullPath(request.OutputDirectory);
        var layout = HopngArtifactLayout.Create(outputDirectory, request.Name);
        Directory.CreateDirectory(outputDirectory);

        var artifactId = request.ArtifactId ?? Guid.NewGuid().ToString("D");
        var now = DateTimeOffset.UtcNow;
        var keyMaterial = _signatureService.CreateOrLoad(request.PrivateKeyPath, request.PrivateKeyOutputPath ?? layout.PrivateKeyPath);
        var publicKeyPath = request.PublicKeyOutputPath ?? layout.PublicKeyPath;
        File.WriteAllText(publicKeyPath, keyMaterial.PublicKeyBase64);
        File.WriteAllBytes(layout.ProjectionPath, PlaceholderPng);

        var layerMap = new HopngLayerMap
        {
            ArtifactId = artifactId,
            Layers =
            [
                new LayerDefinition
                {
                    LayerId = "visible-prime",
                    UniverseId = "prime-projection",
                    Modality = "visual-symbolic",
                    ProjectionRole = "projection-surface",
                    NeutralPlane = 0,
                    CoordinateFrame = new CoordinateFrame
                    {
                        XAxis = "x",
                        YAxis = "y",
                        ZAxis = "z",
                        Units = "pixel-relative"
                    }
                }
            ]
        };

        var depthField = new DepthField
        {
            ArtifactId = artifactId,
            Planes =
            [
                new DepthPlane
                {
                    LayerId = "visible-prime",
                    NeutralPlane = 0,
                    MinimumZ = -1,
                    MaximumZ = 1
                }
            ]
        };

        var transformHistory = new TransformHistory
        {
            ArtifactId = artifactId,
            Transforms =
            [
                new TransformStep
                {
                    StepId = "genesis",
                    Kind = "artifact-foundation",
                    TimestampUtc = now,
                    Description = "Created Phase 1 HOPNG artifact foundation.",
                    Actor = request.Signer
                }
            ]
        };

        var trustEnvelope = new TrustEnvelope
        {
            ArtifactId = artifactId,
            Signer = request.Signer,
            KeyId = request.KeyId,
            IssuedUtc = now,
            PublicKey = keyMaterial.PublicKeyBase64,
            SigningScope =
            [
                Path.GetFileName(layout.ManifestPath),
                Path.GetFileName(layout.HashPath)
            ],
            SignatureFile = Path.GetFileName(layout.SignaturePath)
        };

        _jsonStore.WriteCanonical(layout.LayerMapPath, layerMap);
        _jsonStore.WriteCanonical(layout.DepthFieldPath, depthField);
        _jsonStore.WriteCanonical(layout.TransformHistoryPath, transformHistory);
        _jsonStore.WriteCanonical(layout.TrustEnvelopePath, trustEnvelope);

        var manifest = new HopngManifest
        {
            ArtifactId = artifactId,
            DisplayName = request.DisplayName ?? request.Name,
            CreatedUtc = now,
            ProjectionFile = Path.GetFileName(layout.ProjectionPath),
            Sidecars =
            [
                Sidecar("layer-map", "oan.hopng_layer_map", layout.LayerMapPath),
                Sidecar("trust-envelope", "oan.hopng_trust_envelope", layout.TrustEnvelopePath),
                Sidecar("transform-history", "oan.hopng_transform_history", layout.TransformHistoryPath),
                Sidecar("depth-field", "oan.hopng_depth_field", layout.DepthFieldPath),
                Sidecar("hash", "oan.hopng_hash_set", layout.HashPath),
                Sidecar("signature", "oan.hopng_signature", layout.SignaturePath)
            ],
            FileDigests =
            [
                FileDigest("projection", layout.ProjectionPath),
                FileDigest("layer-map", layout.LayerMapPath),
                FileDigest("trust-envelope", layout.TrustEnvelopePath),
                FileDigest("transform-history", layout.TransformHistoryPath),
                FileDigest("depth-field", layout.DepthFieldPath)
            ],
            VisibilityPolicy = new VisibilityPolicy(),
            PhaseReservations = SchemaCatalog.All
                .Where(schema => !schema.Implemented)
                .OrderBy(schema => schema.Phase)
                .Select(schema => schema.LogicalName)
                .Distinct(StringComparer.Ordinal)
                .ToList()
        };

        _jsonStore.WriteCanonical(layout.ManifestPath, manifest);
        var manifestCanonicalSha256 = ArtifactHashing.ComputeSha256(File.ReadAllBytes(layout.ManifestPath));
        var hashSidecar = new HashSidecar
        {
            ArtifactId = artifactId,
            ManifestCanonicalSha256 = manifestCanonicalSha256,
            ArtifactSetSha256 = ArtifactHashing.ComputeArtifactSetSha256(manifest.FileDigests, manifestCanonicalSha256),
            FileDigests = manifest.FileDigests
        };

        _jsonStore.WriteCanonical(layout.HashPath, hashSidecar);
        var hashBytes = File.ReadAllBytes(layout.HashPath);
        var signature = _signatureService.Sign(keyMaterial.PrivateKeyBase64, hashBytes);
        var signatureSidecar = new SignatureSidecar
        {
            ArtifactId = artifactId,
            KeyId = request.KeyId,
            SignedUtc = now,
            SignedObjectSha256 = ArtifactHashing.ComputeSha256(hashBytes),
            SignatureBase64 = Convert.ToBase64String(signature)
        };
        _jsonStore.WriteCanonical(layout.SignaturePath, signatureSidecar);

        return new HopngArtifactLoader().Load(layout.ManifestPath);
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

public sealed record NewHopngRequest(
    string OutputDirectory,
    string Name,
    string Signer,
    string KeyId,
    string? DisplayName = null,
    string? ArtifactId = null,
    string? PrivateKeyPath = null,
    string? PrivateKeyOutputPath = null,
    string? PublicKeyOutputPath = null);
