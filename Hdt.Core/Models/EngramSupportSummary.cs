namespace Hdt.Core.Models;

public sealed record EngramSupportSummary
{
    public string SupportType { get; init; } = string.Empty;
    public string WorkingIntentState { get; init; } = string.Empty;
    public string IntentClassification { get; init; } = string.Empty;
    public bool SupportOnly { get; init; }
    public string EvidenceClass { get; init; } = string.Empty;
    public string ClaimSurface { get; init; } = string.Empty;
    public string SupportShape { get; init; } = string.Empty;
    public string InspectionPosture { get; init; } = string.Empty;
    public bool Phase5HandoffReady { get; init; }
    public string? RootFormId { get; init; }
    public string? BranchSetId { get; init; }
    public int ParticipantBranchCount { get; init; }
    public string? StateReason { get; init; }
    public IReadOnlyList<string> SupportSignals { get; init; } = [];
    public IReadOnlyList<string> ValidationQuestions { get; init; } = [];
}
