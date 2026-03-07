using FluentAssertions;
using Hdt.Cli;
using Hdt.Core.Models;
using Hdt.Core.Services;
using Hdt.Tests.TestSupport;
using System.Text.Json;

namespace Hdt.Tests;

public sealed class CliTests
{
    [Fact]
    public void Cli_New_Validate_And_Show_Work_End_To_End()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new", "--output-dir", tempDir, "--name", "cli-artifact", "--signer", "tester", "--key-id", "cli-key", "--json"]);
        output.GetStringBuilder().Clear();
        var validateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-artifact.hopng.json"), "--json"]);
        var validateJson = output.ToString();

        output.GetStringBuilder().Clear();
        var showPrimeExitCode = runner.Execute(["show", "--path", Path.Combine(tempDir, "cli-artifact.hopng.json"), "--view", "prime", "--json"]);
        var showPrime = output.ToString();

        output.GetStringBuilder().Clear();
        var showPrivilegedExitCode = runner.Execute(["show", "--path", Path.Combine(tempDir, "cli-artifact.hopng.json"), "--view", "privileged", "--json"]);
        var showPrivileged = output.ToString();

        createExitCode.Should().Be(0);
        validateExitCode.Should().Be(0, validateJson);
        showPrimeExitCode.Should().Be(0);
        showPrivilegedExitCode.Should().Be(0);
        showPrime.Should().Contain("\"view\": \"prime\"");
        showPrivileged.Should().Contain("\"view\": \"privileged\"");
        showPrime.Should().NotContain("\"trustEnvelope\"");
        showPrivileged.Should().Contain("\"trustEnvelope\"");

        var validateDocument = JsonDocument.Parse(validateJson);
        validateDocument.RootElement.GetProperty("isValid").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void Cli_Merge_Layers_Returns_Governed_Projection_Status()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "cli-phase2");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(["merge-layers", "--path", artifact.Layout.ManifestPath, "--json"]);
        var mergeJson = output.ToString();
        using var mergeDocument = JsonDocument.Parse(mergeJson);

        exitCode.Should().Be(0, mergeJson);
        mergeDocument.RootElement.GetProperty("status").GetString().Should().Be("LawfullyFormed");
        mergeDocument.RootElement.GetProperty("ruleTrace")[0].GetProperty("ruleId").GetString().Should().Be("rule-1");
    }

    [Fact]
    public void Cli_Merge_Layers_Returns_Nonzero_When_Derivation_Is_Unsupported()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "cli-phase1", "tester", "key-1"));
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(["merge-layers", "--path", artifact.Layout.ManifestPath, "--json"]);
        var mergeJson = output.ToString();
        using var mergeDocument = JsonDocument.Parse(mergeJson);

        exitCode.Should().Be(25, mergeJson);
        mergeDocument.RootElement.GetProperty("status").GetString().Should().Be("FlattenedOrUnsupported");
    }

    [Fact]
    public void Cli_Compare_Surfaces_Classifies_Formed_Vs_Flattened()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var phase2Artifact = Phase2ArtifactFactory.CreateValid(tempDir, "cli-compare-phase2");
        var phase1Artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "cli-compare-phase1", "tester", "key-1"));
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(
        [
            "compare-surfaces",
            "--left", phase2Artifact.Layout.ManifestPath,
            "--right", phase1Artifact.Layout.ManifestPath,
            "--json"
        ]);
        var comparisonJson = output.ToString();
        using var comparisonDocument = JsonDocument.Parse(comparisonJson);

        exitCode.Should().Be(25, comparisonJson);
        comparisonDocument.RootElement.GetProperty("classification").GetString().Should().Be("formed-vs-flattened");
        comparisonDocument.RootElement.GetProperty("leftStatus").GetString().Should().Be("LawfullyFormed");
        comparisonDocument.RootElement.GetProperty("rightStatus").GetString().Should().Be("FlattenedOrUnsupported");
    }

    [Fact]
    public void Cli_Render_Phase_Stack_Returns_Temporal_Summary()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "cli-phase3");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(["render-phase-stack", "--path", artifact.Layout.ManifestPath, "--view", "prime", "--json"]);
        var renderJson = output.ToString();
        using var renderDocument = JsonDocument.Parse(renderJson);

        exitCode.Should().Be(0, renderJson);
        renderDocument.RootElement.GetProperty("status").GetInt32().Should().Be((int)TemporalStackStatus.LawfullyDerived);
        renderDocument.RootElement.GetProperty("phaseSliceCount").GetInt32().Should().BeGreaterThan(0);
        renderDocument.RootElement.TryGetProperty("eventSlices", out _).Should().BeFalse();
    }

    [Fact]
    public void Cli_Render_Phase_Stack_Returns_Nonzero_When_Temporal_Support_Is_Absent()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "cli-no-phase3");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(["render-phase-stack", "--path", artifact.Layout.ManifestPath, "--json"]);
        var renderJson = output.ToString();
        using var renderDocument = JsonDocument.Parse(renderJson);

        exitCode.Should().Be(25, renderJson);
        renderDocument.RootElement.GetProperty("status").GetInt32().Should().Be((int)TemporalStackStatus.Unsupported);
    }
}
