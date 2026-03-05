using FluentAssertions;
using Hdt.Adapters.Contracts;
using Hdt.Adapters.TestDoubles;
using Hdt.Core.Security;
using Hdt.Core.Services;
using Hdt.Tests.TestSupport;

namespace Hdt.Tests;

public sealed class AdapterContractTests
{
    [Fact]
    public void InMemory_And_FileSystem_Adapters_Accept_Same_StoredArtifact()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "adapter", "tester", "key-1"));
        var stored = new StoredArtifact(
            artifact.Manifest.ArtifactId,
            artifact.Layout.ManifestPath,
            artifact.Layout.ProjectionPath,
            ArtifactHashing.ComputeSha256(File.ReadAllBytes(artifact.Layout.ManifestPath)),
            new Dictionary<string, string> { ["store"] = "test" });

        var memory = new InMemoryArtifactStorageAdapter();
        var fileSystem = new FileSystemArtifactStorageAdapter(Path.Combine(tempDir, "storage"));

        var memoryPointer = memory.Store(stored);
        var fileSystemPointer = fileSystem.Store(stored);

        memoryPointer.Uri.Should().StartWith("selfgel://");
        fileSystemPointer.Uri.Should().StartWith("file://");
    }

    [Fact]
    public void Oe_Binding_Test_Double_Returns_Deterministic_Pointer()
    {
        var adapter = new InMemoryOeBindingAdapter();
        var binding = adapter.Bind(new OeBindingRequest(
            "artifact-1",
            new ArtifactPointer("selfgel://artifact/artifact-1", "abc", "Prime"),
            "header",
            "endcap"));

        binding.Should().Be("oe://binding/artifact-1");
        adapter.Bindings.Should().HaveCount(1);
    }
}
