using Hdt.Core.Validation;

namespace Hdt.Core.Models;

public sealed record EngramSupportComparisonResult
{
    public string LeftArtifactId { get; init; } = string.Empty;
    public string RightArtifactId { get; init; } = string.Empty;
    public string LeftSupportType { get; init; } = string.Empty;
    public string RightSupportType { get; init; } = string.Empty;
    public string LeftWorkingIntentState { get; init; } = string.Empty;
    public string RightWorkingIntentState { get; init; } = string.Empty;
    public string LeftIntentClassification { get; init; } = string.Empty;
    public string RightIntentClassification { get; init; } = string.Empty;
    public string LeftSupportShape { get; init; } = string.Empty;
    public string RightSupportShape { get; init; } = string.Empty;
    public string LeftSupportIdentifier { get; init; } = string.Empty;
    public string RightSupportIdentifier { get; init; } = string.Empty;
    public string WorkingIntentTransitionStatus { get; init; } = string.Empty;
    public string SupportTypeCompatibility { get; init; } = string.Empty;
    public string SupportIdentityCompatibility { get; init; } = string.Empty;
    public string CounterfeitPressureStatus { get; init; } = string.Empty;
    public string LeftStabilityClass { get; init; } = string.Empty;
    public string RightStabilityClass { get; init; } = string.Empty;
    public double LeftConstraintEnergy { get; init; }
    public double RightConstraintEnergy { get; init; }
    public double ConstraintEnergyDelta { get; init; }
    public double LeftBurdenPreservationScore { get; init; }
    public double RightBurdenPreservationScore { get; init; }
    public int SharedSupportSignalCount { get; init; }
    public int SharedValidationQuestionCount { get; init; }
    public int? WorkingIntentRankDelta { get; init; }
    public double SimilarityScore { get; init; }
    public string Classification { get; init; } = string.Empty;
    public string ClassificationReason { get; init; } = string.Empty;
    public string PayloadMode { get; init; } = string.Empty;
    public List<string> Signals { get; init; } = [];
    public List<string> LeftIssues { get; init; } = [];
    public List<string> RightIssues { get; init; } = [];
    public List<ValidationIssue> LeftValidationIssues { get; init; } = [];
    public List<ValidationIssue> RightValidationIssues { get; init; } = [];
}
