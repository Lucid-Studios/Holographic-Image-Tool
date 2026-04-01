namespace Hdt.Core.Models;

public sealed record PerspectivalEngramSupport
{
    public string Schema { get; init; } = "oan.hopng_perspectival_engram";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public string WorkingIntentState { get; init; } = "working_intent";
    public string IntentClassification { get; init; } = string.Empty;
    public bool SupportOnly { get; init; } = true;
    public string EvidenceClass { get; init; } = "engram_candidacy_evidence";
    public string ClaimSurface { get; init; } = "candidate_support_evidence";
    public string SupportShape { get; init; } = "root_constructor_support";
    public string ProvenanceBasis { get; init; } = string.Empty;
    public string ConstructorSupportStatus { get; init; } = string.Empty;
    public string InspectionPosture { get; init; } = "mixed_pointerized";
    public bool Phase5HandoffReady { get; init; }
    public string RootFormId { get; init; } = string.Empty;
    public string RootCoherenceStatus { get; init; } = string.Empty;
    public List<string> RootCoherenceSignals { get; init; } = [];
    public List<string> ValidationQuestions { get; init; } = [];
    public List<ProtectedEvidenceReference> ProtectedEvidenceRefs { get; init; } = [];
    public string? RestrictionReason { get; init; }
    public string? DeferReason { get; init; }
    public string? RejectionReason { get; init; }
}
