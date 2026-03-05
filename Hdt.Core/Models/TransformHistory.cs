namespace Hdt.Core.Models;

public sealed record TransformHistory
{
    public string Schema { get; init; } = "oan.hopng_transform_history";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public List<TransformStep> Transforms { get; init; } = [];
}

public sealed record TransformStep
{
    public string StepId { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Actor { get; init; }
}
