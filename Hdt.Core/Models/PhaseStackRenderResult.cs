using Hdt.Core.Validation;

namespace Hdt.Core.Models;

public sealed record PhaseStackRenderResult
{
    public string ArtifactId { get; init; } = string.Empty;
    public TemporalStackStatus Status { get; init; }
    public string View { get; init; } = "prime";
    public int ObservedDurationMs { get; init; }
    public int BaseRawCadenceMs { get; init; }
    public int RawSliceCount { get; init; }
    public int ObservedEventCount { get; init; }
    public int EventSliceCount { get; init; }
    public int PhaseSliceCount { get; init; }
    public string GroupingSummary { get; init; } = string.Empty;
    public int HorizonRawSlices { get; init; }
    public int HorizonDurationMs { get; init; }
    public bool RequiredChannelCoverage { get; init; }
    public List<string> DriftFlags { get; init; } = [];
    public List<string> TopologyChangeFlags { get; init; } = [];
    public string PayloadMode { get; init; } = string.Empty;
    public List<TemporalSliceSummary> SliceSummaries { get; init; } = [];
    public object? EventSlices { get; init; }
    public object? PhaseSlices { get; init; }
    public List<string> Issues { get; init; } = [];
    public List<ValidationIssue> ValidationIssues { get; init; } = [];
}

public sealed record TemporalSliceSummary
{
    public string Family { get; init; } = string.Empty;
    public string SliceId { get; init; } = string.Empty;
    public int N { get; init; }
    public DateTimeOffset TimestampStartUtc { get; init; }
    public DateTimeOffset TimestampEndUtc { get; init; }
    public string RawRangeSummary { get; init; } = string.Empty;
    public List<string> Flags { get; init; } = [];
}
