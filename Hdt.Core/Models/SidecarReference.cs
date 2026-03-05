namespace Hdt.Core.Models;

public sealed record SidecarReference
{
    public string Role { get; init; } = string.Empty;
    public string Schema { get; init; } = string.Empty;
    public string SchemaVersion { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public bool Required { get; init; } = true;
}
