using Hdt.Adapters.Contracts;

namespace Hdt.Adapters.TestDoubles;

public sealed class InMemoryArtifactStorageAdapter : IArtifactStorageAdapter
{
    private readonly Dictionary<string, StoredArtifact> _artifacts = new(StringComparer.Ordinal);

    public string Name => "in-memory";

    public IReadOnlyCollection<AdapterCapability> Capabilities =>
    [
        new("selfgel-pointer", "Stores artifact pointers in memory for tests."),
        new("prime-visibility", "Retains visibility boundary metadata.")
    ];

    public ArtifactPointer Store(StoredArtifact artifact)
    {
        _artifacts[artifact.ArtifactId] = artifact;
        return new ArtifactPointer($"selfgel://artifact/{artifact.ArtifactId}", artifact.Sha256, "Prime");
    }

    public bool TryGet(string artifactId, out StoredArtifact artifact) => _artifacts.TryGetValue(artifactId, out artifact!);
}
