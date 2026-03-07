namespace Hdt.Core.Models;

public sealed record PhaseSliceSet
{
    public string Schema { get; init; } = "oan.hopng_phase_slice";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public List<PhaseSlice> Slices { get; init; } = [];
}

public sealed record PhaseSlice
{
    public string PhaseSliceId { get; init; } = string.Empty;
    public string ArtifactId { get; init; } = string.Empty;
    public int N { get; init; }
    public DateTimeOffset TimestampStartUtc { get; init; }
    public DateTimeOffset TimestampEndUtc { get; init; }
    public List<string> SourceEventSliceIds { get; init; } = [];
    public int SourceRawStartN { get; init; }
    public int SourceRawEndN { get; init; }
    public int DeltaHorizon { get; init; }
    public int DeltaHorizonMs { get; init; }
    public Dictionary<string, TemporalUniverseState> UniverseStates { get; init; } = new(StringComparer.Ordinal);
    public string SliceDigest { get; init; } = string.Empty;
}
