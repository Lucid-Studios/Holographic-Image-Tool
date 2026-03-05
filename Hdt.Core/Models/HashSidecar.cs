namespace Hdt.Core.Models;

public sealed record HashSidecar
{
    public string Schema { get; init; } = "oan.hopng_hash_set";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public string ManifestCanonicalSha256 { get; init; } = string.Empty;
    public string ArtifactSetSha256 { get; init; } = string.Empty;
    public List<ArtifactFileDigest> FileDigests { get; init; } = [];
}
