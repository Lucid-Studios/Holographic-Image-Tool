using Hdt.Core.Artifacts;

namespace Hdt.Core.Models;

public sealed record LoadedHopngArtifact
{
    public required HopngArtifactLayout Layout { get; init; }
    public required HopngManifest Manifest { get; init; }
    public required HopngLayerMap LayerMap { get; init; }
    public required TrustEnvelope TrustEnvelope { get; init; }
    public required TransformHistory TransformHistory { get; init; }
    public required DepthField DepthField { get; init; }
    public required HashSidecar HashSidecar { get; init; }
    public required SignatureSidecar SignatureSidecar { get; init; }
}
