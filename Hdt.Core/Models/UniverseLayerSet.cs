namespace Hdt.Core.Models;

public sealed record UniverseLayerSet
{
    public string Schema { get; init; } = "oan.hopng_universe_layer";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public List<UniverseLayer> Universes { get; init; } = [];
}

public sealed record UniverseLayer
{
    public string UniverseId { get; init; } = string.Empty;
    public string Modality { get; init; } = string.Empty;
    public double NeutralPlane { get; init; }
    public CoordinateFrame CoordinateFrame { get; init; } = new();
    public string ProjectionRole { get; init; } = string.Empty;
}
