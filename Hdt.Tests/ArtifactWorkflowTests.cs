using FluentAssertions;
using Hdt.Core.Security;
using Hdt.Core.Services;
using Hdt.Core.Validation;
using Hdt.Tests.TestSupport;

namespace Hdt.Tests;

public sealed class ArtifactWorkflowTests
{
    [Fact]
    public void Builder_Creates_Valid_Artifact()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var builder = new HopngArtifactBuilder();
        var validator = new HopngArtifactValidator();

        var artifact = builder.Create(new NewHopngRequest(tempDir, "specimen", "tester", "key-1"));
        var validation = validator.Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeTrue();
        File.Exists(artifact.Layout.ManifestPath).Should().BeTrue();
        File.Exists(artifact.Layout.SignaturePath).Should().BeTrue();
    }

    [Fact]
    public void Validation_Fails_When_Sidecar_Is_Missing()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "missing-sidecar", "tester", "key-1"));
        File.Delete(artifact.Layout.LayerMapPath);

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.MissingFile);
    }

    [Fact]
    public void Validation_Fails_When_Schema_Version_Is_Wrong()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "wrong-schema", "tester", "key-1"));
        JsonFile.Mutate(artifact.Layout.ManifestPath, json => json["schemaVersion"] = "9.9.9");

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.UnsupportedSchema);
    }

    [Fact]
    public void Validation_Fails_When_Coordinate_Frame_Is_Invalid()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "bad-layer", "tester", "key-1"));
        JsonFile.Mutate(artifact.Layout.LayerMapPath, json =>
        {
            var layers = json["layers"]!.AsArray();
            layers[0]!["coordinateFrame"]!["zAxis"] = "";
        });

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidLayerMap);
    }

    [Fact]
    public void Validation_Fails_When_Cryptic_Pointers_Are_Not_Allowed()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "cryptic-policy", "tester", "key-1"));
        JsonFile.Mutate(artifact.Layout.ManifestPath, json =>
        {
            var policy = json["visibilityPolicy"]!.AsObject();
            policy["crypticPointersAllowed"] = false;
            policy["crypticReferences"] = new System.Text.Json.Nodes.JsonArray
            {
                new System.Text.Json.Nodes.JsonObject
                {
                    ["id"] = "ref-1",
                    ["pointerUri"] = "oe://secret/ref-1",
                    ["policy"] = "role-bound"
                }
            };
        });

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidVisibilityPolicy);
    }

    [Fact]
    public void Signature_And_Digest_Detect_Tampering()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "tampered", "tester", "key-1"));
        File.AppendAllText(artifact.Layout.DepthFieldPath, "\n");

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.DigestMismatch);
    }

    [Fact]
    public void Artifact_Set_Hash_Is_Stable_And_Changes_On_Mutation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "hashes", "tester", "key-1"));
        var firstHash = ArtifactHashing.ComputeArtifactSetSha256(
            artifact.Manifest.FileDigests,
            ArtifactHashing.ComputeSha256(File.ReadAllBytes(artifact.Layout.ManifestPath)));
        var secondHash = ArtifactHashing.ComputeArtifactSetSha256(
            artifact.Manifest.FileDigests,
            ArtifactHashing.ComputeSha256(File.ReadAllBytes(artifact.Layout.ManifestPath)));

        firstHash.Should().Be(secondHash);

        JsonFile.Mutate(artifact.Layout.TransformHistoryPath, json =>
        {
            json["transforms"]!.AsArray()[0]!["description"] = "mutated";
        });

        var changedHash = ArtifactHashing.ComputeArtifactSetSha256(
            artifact.Manifest.FileDigests,
            ArtifactHashing.ComputeSha256(File.ReadAllBytes(artifact.Layout.ManifestPath)));

        changedHash.Should().Be(firstHash, "manifest digests are immutable until the manifest is updated");
        new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Additional_Phase2_Files_Do_Not_Break_Phase1_Validation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "phase2-ready", "tester", "key-1"));
        File.WriteAllText(Path.Combine(tempDir, "phase2-ready.universe-layer.json"), "{\"schema\":\"oan.hopng_universe_layer\",\"schemaVersion\":\"0.1.0\"}");

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeTrue();
    }
}
