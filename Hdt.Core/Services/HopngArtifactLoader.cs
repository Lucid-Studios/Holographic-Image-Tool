using Hdt.Core.Artifacts;
using Hdt.Core.Models;

namespace Hdt.Core.Services;

public sealed class HopngArtifactLoader
{
    private readonly ArtifactJsonStore _jsonStore = new();

    public LoadedHopngArtifact Load(string path)
    {
        var layout = HopngArtifactLayout.FromPath(path);
        var manifest = _jsonStore.Read<HopngManifest>(layout.ManifestPath);

        return new LoadedHopngArtifact
        {
            Layout = layout,
            Manifest = manifest,
            LayerMap = _jsonStore.Read<HopngLayerMap>(ResolvePath(manifest, "layer-map", layout.LayerMapPath)),
            TrustEnvelope = _jsonStore.Read<TrustEnvelope>(ResolvePath(manifest, "trust-envelope", layout.TrustEnvelopePath)),
            TransformHistory = _jsonStore.Read<TransformHistory>(ResolvePath(manifest, "transform-history", layout.TransformHistoryPath)),
            DepthField = _jsonStore.Read<DepthField>(ResolvePath(manifest, "depth-field", layout.DepthFieldPath)),
            HashSidecar = _jsonStore.Read<HashSidecar>(ResolvePath(manifest, "hash", layout.HashPath)),
            SignatureSidecar = _jsonStore.Read<SignatureSidecar>(ResolvePath(manifest, "signature", layout.SignaturePath)),
            UniverseLayerSet = TryReadOptional<UniverseLayerSet>(manifest, layout, "universe-layer"),
            GluingManifest = TryReadOptional<GluingManifest>(manifest, layout, "gluing-manifest"),
            ProjectionRules = TryReadOptional<ProjectionRules>(manifest, layout, "projection-rules"),
            LegibilityProfile = TryReadOptional<LegibilityProfile>(manifest, layout, "legibility-profile")
        };
    }

    private string ResolvePath(HopngManifest manifest, string role, string fallbackPath)
    {
        var sidecar = manifest.Sidecars.FirstOrDefault(candidate => string.Equals(candidate.Role, role, StringComparison.Ordinal));
        if (sidecar is null)
        {
            return fallbackPath;
        }

        return Path.Combine(Path.GetDirectoryName(fallbackPath)!, sidecar.Path);
    }

    private T? TryReadOptional<T>(HopngManifest manifest, HopngArtifactLayout layout, string role)
        where T : class
    {
        var sidecar = manifest.Sidecars.FirstOrDefault(candidate => string.Equals(candidate.Role, role, StringComparison.Ordinal));
        if (sidecar is null)
        {
            return null;
        }

        var path = Path.Combine(layout.DirectoryPath, sidecar.Path);
        return File.Exists(path) ? _jsonStore.Read<T>(path) : null;
    }
}
