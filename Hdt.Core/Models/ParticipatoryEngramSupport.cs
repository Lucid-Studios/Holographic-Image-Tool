namespace Hdt.Core.Models;

public sealed record ParticipatoryEngramSupport
{
    public string Schema { get; init; } = "oan.hopng_participatory_engram";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public string WorkingIntentState { get; init; } = "working_intent";
    public string IntentClassification { get; init; } = string.Empty;
    public bool SupportOnly { get; init; } = true;
    public string EvidenceClass { get; init; } = "engram_candidacy_evidence";
    public string ClaimSurface { get; init; } = "candidate_support_evidence";
    public string SupportShape { get; init; } = "branch_set_support";
    public string ProvenanceBasis { get; init; } = string.Empty;
    public string ConstructorSupportStatus { get; init; } = string.Empty;
    public string InspectionPosture { get; init; } = "mixed_pointerized";
    public bool Phase5HandoffReady { get; init; }
    public string BranchSetId { get; init; } = string.Empty;
    public string BranchCoherenceStatus { get; init; } = string.Empty;
    public List<string> BranchCoherenceSignals { get; init; } = [];
    public List<ParticipatoryBranchSupport> ParticipantBranches { get; init; } = [];
    public List<string> ValidationQuestions { get; init; } = [];
    public List<ProtectedEvidenceReference> ProtectedEvidenceRefs { get; init; } = [];
    public string? RestrictionReason { get; init; }
    public string? DeferReason { get; init; }
    public string? RejectionReason { get; init; }
}

public sealed record ParticipatoryBranchSupport
{
    public string BranchId { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string SupportState { get; init; } = "working_intent";
}
