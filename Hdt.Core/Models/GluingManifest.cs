namespace Hdt.Core.Models;

public sealed record GluingManifest
{
    public string Schema { get; init; } = "oan.hopng_gluing_manifest";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public List<GluingRelation> Relations { get; init; } = [];
}

public sealed record GluingRelation
{
    public string RelationId { get; init; } = string.Empty;
    public string SourceUniverseId { get; init; } = string.Empty;
    public string TargetUniverseId { get; init; } = string.Empty;
    public string RelationType { get; init; } = string.Empty;
    public bool RequiredForFormation { get; init; }
}
