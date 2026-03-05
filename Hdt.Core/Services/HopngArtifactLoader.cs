using Hdt.Core.Artifacts;
using Hdt.Core.Models;

namespace Hdt.Core.Services;

public sealed class HopngArtifactLoader
{
    private readonly ArtifactJsonStore _jsonStore = new();

    public LoadedHopngArtifact Load(string path)
    {
        var layout = HopngArtifactLayout.FromPath(path);
        return new LoadedHopngArtifact
        {
            Layout = layout,
            Manifest = _jsonStore.Read<HopngManifest>(layout.ManifestPath),
            LayerMap = _jsonStore.Read<HopngLayerMap>(layout.LayerMapPath),
            TrustEnvelope = _jsonStore.Read<TrustEnvelope>(layout.TrustEnvelopePath),
            TransformHistory = _jsonStore.Read<TransformHistory>(layout.TransformHistoryPath),
            DepthField = _jsonStore.Read<DepthField>(layout.DepthFieldPath),
            HashSidecar = _jsonStore.Read<HashSidecar>(layout.HashPath),
            SignatureSidecar = _jsonStore.Read<SignatureSidecar>(layout.SignaturePath)
        };
    }
}
