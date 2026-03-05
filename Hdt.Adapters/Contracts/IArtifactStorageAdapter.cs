namespace Hdt.Adapters.Contracts;

public interface IArtifactStorageAdapter
{
    string Name { get; }
    IReadOnlyCollection<AdapterCapability> Capabilities { get; }
    ArtifactPointer Store(StoredArtifact artifact);
}
