namespace Hdt.Core.Models;

public sealed record PhasePolicy
{
    public string Schema { get; init; } = "oan.hopng_phase_policy";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public int RawCadenceMs { get; init; }
    public string EventGroupingMode { get; init; } = string.Empty;
    public int EventGroupingSizeRawSlices { get; init; }
    public string PhaseWindowMode { get; init; } = string.Empty;
    public int PhaseWindowSizeEventSlices { get; init; }
    public int PhaseWindowDurationMs { get; init; }
    public int MaxPhaseWindowSpanMs { get; init; }
    public int ComparisonHorizonRawSlices { get; init; }
    public List<TemporalComparisonHorizonPolicy> ComparisonHorizons { get; init; } = [];
    public Dictionary<string, string> AggregationPolicies { get; init; } = new(StringComparer.Ordinal);
    public TemporalStateThresholdPolicy? StateThresholds { get; init; }
    public string PrimeSafeInspectionMode { get; init; } = "metadata_only";
    public string PrivilegedInspectionMode { get; init; } = "full_payload";
}

public sealed record TemporalComparisonHorizonPolicy
{
    public string HorizonId { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public int Value { get; init; }
    public bool UseForStateClassification { get; init; }
}

public sealed record TemporalStateThresholdPolicy
{
    public double RisingPressureMin { get; init; } = 0.35;
    public double DriftingAbsoluteMin { get; init; } = 0.15;
    public double PropagatingBloomMin { get; init; } = 0.75;
    public double RupturePressureMin { get; init; } = 0.45;
    public double RuptureDriftAbsoluteMin { get; init; } = 0.20;
    public double DirectionDriftAbsoluteMin { get; init; } = 0.05;
    public double ForcePressureWeight { get; init; } = 0.45;
    public double ForceDriftWeight { get; init; } = 0.35;
    public double ForceBloomWeight { get; init; } = 0.20;
    public double ForceTopologyBonus { get; init; } = 0.15;
}
