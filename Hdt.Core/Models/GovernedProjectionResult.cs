using Hdt.Core.Validation;

namespace Hdt.Core.Models;

public sealed record GovernedProjectionResult
{
    public string ArtifactId { get; init; } = string.Empty;
    public ProjectionFormationStatus Status { get; init; }
    public bool IsLawfullyFormed => Status == ProjectionFormationStatus.LawfullyFormed;
    public bool LegibilitySatisfied { get; init; }
    public bool ProjectionIntegritySatisfied { get; init; }
    public List<string> ParticipatingUniverses { get; init; } = [];
    public List<string> ParticipatingRelations { get; init; } = [];
    public List<ProjectionRuleApplicationTrace> RuleTrace { get; init; } = [];
    public List<string> Issues { get; init; } = [];
    public List<ValidationIssue> ValidationIssues { get; init; } = [];
}

public sealed record ProjectionRuleApplicationTrace
{
    public string RuleId { get; init; } = string.Empty;
    public string SourceUniverseId { get; init; } = string.Empty;
    public string TargetProjectionRole { get; init; } = string.Empty;
    public string MappingType { get; init; } = string.Empty;
    public int Precedence { get; init; }
    public List<string> SupportingRelationIds { get; init; } = [];
    public bool Applied { get; init; }
    public string Outcome { get; init; } = string.Empty;
}
