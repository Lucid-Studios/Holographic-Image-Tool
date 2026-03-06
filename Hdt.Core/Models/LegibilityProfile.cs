namespace Hdt.Core.Models;

public sealed record LegibilityProfile
{
    public string Schema { get; init; } = "oan.hopng_legibility_profile";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public List<string> RequiredUniverses { get; init; } = [];
    public List<string> RequiredRelations { get; init; } = [];
    public bool ProjectionIntegrityRequired { get; init; }
}
