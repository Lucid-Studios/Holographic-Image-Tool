using Hdt.Core.Models;
using Hdt.Core.Services;

namespace Hdt.Tests.TestSupport;

public static class Phase4ArtifactFactory
{
    public static LoadedHopngArtifact CreatePerspectivalSupport(string tempDir, string name) =>
        new Phase4SampleArtifactBuilder().CreatePerspectivalSupportSample(
            new NewHopngRequest(tempDir, name, "tester", "key-1"));

    public static LoadedHopngArtifact CreatePerspectivalSupportPeer(string tempDir, string name) =>
        new Phase4SampleArtifactBuilder().CreatePerspectivalSupportPeerSample(
            new NewHopngRequest(tempDir, name, "tester", "key-1"));

    public static LoadedHopngArtifact CreateRestrictedPerspectivalSupport(string tempDir, string name) =>
        new Phase4SampleArtifactBuilder().CreateRestrictedPerspectivalSupportSample(
            new NewHopngRequest(tempDir, name, "tester", "key-1"));

    public static LoadedHopngArtifact CreateDeferredPerspectivalSupport(string tempDir, string name) =>
        new Phase4SampleArtifactBuilder().CreateDeferredPerspectivalSupportSample(
            new NewHopngRequest(tempDir, name, "tester", "key-1"));

    public static LoadedHopngArtifact CreateInvalidPerspectivalSupport(string tempDir, string name) =>
        new Phase4SampleArtifactBuilder().CreateInvalidPerspectivalSupportSample(
            new NewHopngRequest(tempDir, name, "tester", "key-1"));

    public static LoadedHopngArtifact CreateParticipatorySupport(string tempDir, string name) =>
        new Phase4SampleArtifactBuilder().CreateParticipatorySupportSample(
            new NewHopngRequest(tempDir, name, "tester", "key-1"));

    public static LoadedHopngArtifact CreateParticipatorySupportPeer(string tempDir, string name) =>
        new Phase4SampleArtifactBuilder().CreateParticipatorySupportPeerSample(
            new NewHopngRequest(tempDir, name, "tester", "key-1"));

    public static LoadedHopngArtifact CreateRejectedParticipatorySupport(string tempDir, string name) =>
        new Phase4SampleArtifactBuilder().CreateRejectedParticipatorySupportSample(
            new NewHopngRequest(tempDir, name, "tester", "key-1"));

    public static LoadedHopngArtifact CreateInvalidParticipatorySupport(string tempDir, string name) =>
        new Phase4SampleArtifactBuilder().CreateInvalidParticipatorySupportSample(
            new NewHopngRequest(tempDir, name, "tester", "key-1"));
}
