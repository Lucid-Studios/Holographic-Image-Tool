using Hdt.Core.Models;

namespace Hdt.Core.Diagnostics;

public sealed record ArtifactDiagnosticReport
{
    public bool CounterfeitRisk { get; init; }
    public bool FlattenedProjectionRisk { get; init; }
    public ProjectionFormationStatus ProjectionFormationStatus { get; init; }
    public List<string> ParticipatingUniverses { get; init; } = [];
    public List<string> ParticipatingRelations { get; init; } = [];
    public List<string> ProjectionIssues { get; init; } = [];
    public List<string> Signals { get; init; } = [];
}
