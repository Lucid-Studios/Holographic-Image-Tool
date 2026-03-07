namespace Hdt.Core.Models;

public sealed record OpticalChannelsDefinition
{
    public string Schema { get; init; } = "oan.hopng_optical_channels";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public List<string> RequiredChannels { get; init; } = [];
    public List<string> ReservedChannels { get; init; } = [];
    public List<OpticalChannelDefinition> Channels { get; init; } = [];
}

public sealed record OpticalChannelDefinition
{
    public string ChannelId { get; init; } = string.Empty;
    public string CanonicalMeaning { get; init; } = string.Empty;
    public bool Required { get; init; }
    public bool DerivedOnly { get; init; }
    public string UsageMode { get; init; } = string.Empty;
}
