namespace Hdt.Core.Models;

public sealed record EngramStabilityFieldSummary
{
    public string SupportType { get; init; } = string.Empty;
    public string WorkingIntentState { get; init; } = string.Empty;
    public string StabilityClass { get; init; } = string.Empty;
    public double ConstraintEnergy { get; init; }
    public double CoherenceScore { get; init; }
    public double BurdenPreservationScore { get; init; }
    public double RecoveryIntegrityScore { get; init; }
    public double IntermixStabilityScore { get; init; }
    public List<string> Signals { get; init; } = [];
}
