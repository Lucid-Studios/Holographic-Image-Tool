namespace Hdt.Adapters.Contracts;

public sealed record StoredArtifact(
    string ArtifactId,
    string ManifestPath,
    string ProjectionPath,
    string Sha256,
    IReadOnlyDictionary<string, string> Metadata);
