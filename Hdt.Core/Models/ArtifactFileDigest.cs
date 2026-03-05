namespace Hdt.Core.Models;

public sealed record ArtifactFileDigest
{
    public string Role { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
}
