using Hdt.Adapters.Contracts;

namespace Hdt.Adapters.TestDoubles;

public sealed class FileSystemArtifactStorageAdapter : IArtifactStorageAdapter
{
    private readonly string _rootDirectory;

    public FileSystemArtifactStorageAdapter(string rootDirectory)
    {
        _rootDirectory = Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(_rootDirectory);
    }

    public string Name => "filesystem";

    public IReadOnlyCollection<AdapterCapability> Capabilities =>
    [
        new("goa-pointer", "Stores manifest pointers as filesystem URIs."),
        new("content-addressable", "Persists artifacts keyed by artifact id.")
    ];

    public ArtifactPointer Store(StoredArtifact artifact)
    {
        var targetDirectory = Path.Combine(_rootDirectory, artifact.ArtifactId);
        Directory.CreateDirectory(targetDirectory);
        File.Copy(artifact.ManifestPath, Path.Combine(targetDirectory, Path.GetFileName(artifact.ManifestPath)), overwrite: true);
        File.Copy(artifact.ProjectionPath, Path.Combine(targetDirectory, Path.GetFileName(artifact.ProjectionPath)), overwrite: true);
        return new ArtifactPointer(new Uri(targetDirectory).AbsoluteUri, artifact.Sha256, "Prime");
    }
}
