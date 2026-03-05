namespace Hdt.Core.Models;

public sealed record HopngLayerMap
{
    public string Schema { get; init; } = "oan.hopng_layer_map";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public List<LayerDefinition> Layers { get; init; } = [];
}

public sealed record LayerDefinition
{
    public string LayerId { get; init; } = string.Empty;
    public string UniverseId { get; init; } = string.Empty;
    public string Modality { get; init; } = string.Empty;
    public string ProjectionRole { get; init; } = string.Empty;
    public double NeutralPlane { get; init; }
    public CoordinateFrame CoordinateFrame { get; init; } = new();
}
