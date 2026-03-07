namespace Hdt.Core.Models;

public sealed record EventSliceSet
{
    public string Schema { get; init; } = "oan.hopng_event_slice";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public ObservedSetHeader ObservedSet { get; init; } = new();
    public List<EventSlice> Slices { get; init; } = [];
}

public sealed record ObservedSetHeader
{
    public string ObservedSetId { get; init; } = string.Empty;
    public string ArtifactId { get; init; } = string.Empty;
    public int ObservedDurationMs { get; init; }
    public int BaseSliceCadenceMs { get; init; }
    public int RawSliceCount { get; init; }
    public int ObservedEventCount { get; init; }
    public string EventGroupingMode { get; init; } = string.Empty;
    public int EventGroupingSizeRawSlices { get; init; }
    public List<ProtectedEvidenceReference> ProtectedEvidenceRefs { get; init; } = [];
    public string PrimeSafeInspectionMode { get; init; } = "metadata_only";
    public string DataCustodyMode { get; init; } = "protected_external";
}

public sealed record EventSlice
{
    public string EventSliceId { get; init; } = string.Empty;
    public string ArtifactId { get; init; } = string.Empty;
    public int N { get; init; }
    public DateTimeOffset TimestampStartUtc { get; init; }
    public DateTimeOffset TimestampEndUtc { get; init; }
    public int RawStartN { get; init; }
    public int RawEndN { get; init; }
    public int RawSliceSpan { get; init; }
    public int ObservedEventCount { get; init; }
    public Dictionary<string, TemporalUniverseState> UniverseStates { get; init; } = new(StringComparer.Ordinal);
    public List<ProtectedEvidenceReference> ProtectedEvidenceRefs { get; init; } = [];
    public string SliceDigest { get; init; } = string.Empty;
}

public sealed record ProtectedEvidenceReference
{
    public string RefId { get; init; } = string.Empty;
    public string PointerUri { get; init; } = string.Empty;
    public string DigestSha256 { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}

public sealed record TemporalUniverseState
{
    public double Pressure { get; init; }
    public double Drift { get; init; }
    public double Bloom { get; init; }
    public double? Force { get; init; }
    public double? Opacity { get; init; }
    public double? Hue { get; init; }
    public double? Saturation { get; init; }
}
