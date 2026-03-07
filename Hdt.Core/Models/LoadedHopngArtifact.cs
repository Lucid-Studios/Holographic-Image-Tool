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
    public UniverseLayerSet? UniverseLayerSet { get; init; }
    public GluingManifest? GluingManifest { get; init; }
    public ProjectionRules? ProjectionRules { get; init; }
    public LegibilityProfile? LegibilityProfile { get; init; }
    public EventSliceSet? EventSliceSet { get; init; }
    public PhaseSliceSet? PhaseSliceSet { get; init; }
    public PhasePolicy? PhasePolicy { get; init; }
    public OpticalChannelsDefinition? OpticalChannelsDefinition { get; init; }
}
