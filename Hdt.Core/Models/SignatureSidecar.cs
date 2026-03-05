namespace Hdt.Core.Models;

public sealed record SignatureSidecar
{
    public string Schema { get; init; } = "oan.hopng_signature";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public string Algorithm { get; init; } = "Ed25519";
    public string KeyId { get; init; } = string.Empty;
    public DateTimeOffset SignedUtc { get; init; }
    public string SignedObjectSha256 { get; init; } = string.Empty;
    public string SignatureBase64 { get; init; } = string.Empty;
}
