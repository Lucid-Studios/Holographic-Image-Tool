namespace Hdt.Core.Models;

public sealed record ProjectionSupportComparisonResult
{
    public string LeftArtifactId { get; init; } = string.Empty;
    public string RightArtifactId { get; init; } = string.Empty;
    public ProjectionFormationStatus LeftStatus { get; init; }
    public ProjectionFormationStatus RightStatus { get; init; }
    public string Classification { get; init; } = string.Empty;
    public List<string> LeftIssues { get; init; } = [];
    public List<string> RightIssues { get; init; } = [];
    public List<string> Signals { get; init; } = [];
}
