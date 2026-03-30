using Hdt.Core.Validation;

namespace Hdt.Core.Models;

public sealed record TemporalPhaseStackComparisonResult
{
    public string LeftArtifactId { get; init; } = string.Empty;
    public string RightArtifactId { get; init; } = string.Empty;
    public TemporalStackStatus LeftStatus { get; init; }
    public TemporalStackStatus RightStatus { get; init; }
    public string BasisAlignmentStatus { get; init; } = "unchecked";
    public string Classification { get; init; } = string.Empty;
    public string TemporalStateCompatibility { get; init; } = string.Empty;
    public string LeftPrimaryHorizonId { get; init; } = string.Empty;
    public string RightPrimaryHorizonId { get; init; } = string.Empty;
    public int LeftPrimaryHorizonRawSlices { get; init; }
    public int LeftPrimaryHorizonDurationMs { get; init; }
    public int RightPrimaryHorizonRawSlices { get; init; }
    public int RightPrimaryHorizonDurationMs { get; init; }
    public int PrimaryHorizonRawSlices { get; init; }
    public int PrimaryHorizonDurationMs { get; init; }
    public string LeftFinalStateClass { get; init; } = string.Empty;
    public string RightFinalStateClass { get; init; } = string.Empty;
    public string LeftFinalStateDirection { get; init; } = string.Empty;
    public string RightFinalStateDirection { get; init; } = string.Empty;
    public int? LeftFinalStateRank { get; init; }
    public int? RightFinalStateRank { get; init; }
    public int? StateRankDelta { get; init; }
    public int ComparablePhaseSliceCount { get; init; }
    public double DriftDeltaMagnitude { get; init; }
    public double DerivedForceDeltaMagnitude { get; init; }
    public int TopologyDeltaCount { get; init; }
    public double SimilarityScore { get; init; }
    public string ClassificationReason { get; init; } = string.Empty;
    public string PayloadMode { get; init; } = string.Empty;
    public List<string> BasisSignals { get; init; } = [];
    public List<string> Signals { get; init; } = [];
    public List<string> LeftIssues { get; init; } = [];
    public List<string> RightIssues { get; init; } = [];
    public List<ValidationIssue> LeftValidationIssues { get; init; } = [];
    public List<ValidationIssue> RightValidationIssues { get; init; } = [];
}
