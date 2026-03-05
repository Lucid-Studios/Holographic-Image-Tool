namespace Hdt.Core.Diagnostics;

public sealed record ArtifactDiagnosticReport
{
    public bool CounterfeitRisk { get; init; }
    public bool FlattenedProjectionRisk { get; init; }
    public List<string> Signals { get; init; } = [];
}
