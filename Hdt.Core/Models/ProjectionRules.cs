namespace Hdt.Core.Models;

public sealed record ProjectionRules
{
    public string Schema { get; init; } = "oan.hopng_projection_rules";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public List<ProjectionRule> Rules { get; init; } = [];
}

public sealed record ProjectionRule
{
    public string RuleId { get; init; } = string.Empty;
    public string SourceUniverseId { get; init; } = string.Empty;
    public string TargetProjectionRole { get; init; } = string.Empty;
    public string MappingType { get; init; } = string.Empty;
    public int Precedence { get; init; }
}
