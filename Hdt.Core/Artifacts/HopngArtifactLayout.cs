namespace Hdt.Core.Artifacts;

public sealed record HopngArtifactLayout
{
    public required string DirectoryPath { get; init; }
    public required string BaseName { get; init; }

    public string ProjectionPath => Path.Combine(DirectoryPath, $"{BaseName}.png");
    public string ManifestPath => Path.Combine(DirectoryPath, $"{BaseName}.hopng.json");
    public string LayerMapPath => Path.Combine(DirectoryPath, $"{BaseName}.layer-map.json");
    public string TrustEnvelopePath => Path.Combine(DirectoryPath, $"{BaseName}.trust-envelope.json");
    public string TransformHistoryPath => Path.Combine(DirectoryPath, $"{BaseName}.transform-history.json");
    public string DepthFieldPath => Path.Combine(DirectoryPath, $"{BaseName}.depth-field.json");
    public string UniverseLayerPath => Path.Combine(DirectoryPath, $"{BaseName}.universe-layer.json");
    public string GluingManifestPath => Path.Combine(DirectoryPath, $"{BaseName}.gluing-manifest.json");
    public string ProjectionRulesPath => Path.Combine(DirectoryPath, $"{BaseName}.projection-rules.json");
    public string LegibilityProfilePath => Path.Combine(DirectoryPath, $"{BaseName}.legibility-profile.json");
    public string EventSlicePath => Path.Combine(DirectoryPath, $"{BaseName}.event-slices.json");
    public string PhaseSlicePath => Path.Combine(DirectoryPath, $"{BaseName}.phase-slices.json");
    public string PhasePolicyPath => Path.Combine(DirectoryPath, $"{BaseName}.phase-policy.json");
    public string OpticalChannelsPath => Path.Combine(DirectoryPath, $"{BaseName}.optical-channels.json");
    public string HashPath => Path.Combine(DirectoryPath, $"{BaseName}.hash.json");
    public string SignaturePath => Path.Combine(DirectoryPath, $"{BaseName}.signature.json");
    public string PrivateKeyPath => Path.Combine(DirectoryPath, $"{BaseName}.ed25519.private.key");
    public string PublicKeyPath => Path.Combine(DirectoryPath, $"{BaseName}.ed25519.public.key");

    public static HopngArtifactLayout Create(string directoryPath, string baseName) => new()
    {
        DirectoryPath = directoryPath,
        BaseName = baseName
    };

    public static HopngArtifactLayout FromPath(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            var manifest = Directory.EnumerateFiles(fullPath, "*.hopng.json", SearchOption.TopDirectoryOnly).SingleOrDefault()
                ?? throw new FileNotFoundException("No .hopng manifest was found in the provided directory.", fullPath);
            return FromPath(manifest);
        }

        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("Artifact path must include a directory.");
        var fileName = Path.GetFileName(fullPath);

        var baseName = fileName.EndsWith(".hopng.json", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^".hopng.json".Length]
            : Path.GetFileNameWithoutExtension(fileName);

        return Create(directory, baseName);
    }
}
