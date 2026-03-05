namespace Hdt.Core.Models;

public sealed record TrustEnvelope
{
    public string Schema { get; init; } = "oan.hopng_trust_envelope";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public string Signer { get; init; } = string.Empty;
    public string KeyId { get; init; } = string.Empty;
    public DateTimeOffset IssuedUtc { get; init; }
    public string PublicKey { get; init; } = string.Empty;
    public List<string> SigningScope { get; init; } = [];
    public string SignatureFile { get; init; } = string.Empty;
}
