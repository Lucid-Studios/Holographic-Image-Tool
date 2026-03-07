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
    public int ComparisonHorizonRawSlices { get; init; }
    public Dictionary<string, string> AggregationPolicies { get; init; } = new(StringComparer.Ordinal);
    public string PrimeSafeInspectionMode { get; init; } = "metadata_only";
    public string PrivilegedInspectionMode { get; init; } = "full_payload";
}
