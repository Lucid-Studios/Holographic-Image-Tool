using FluentAssertions;
using Hdt.Cli;
using Hdt.Core.Models;
using Hdt.Core.Services;
using Hdt.Tests.TestSupport;
using System.IO;
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
        renderDocument.RootElement.GetProperty("primaryHorizonId").GetString().Should().Be("widened-duration-20000");
        renderDocument.RootElement.GetProperty("horizonSummaries").GetArrayLength().Should().BeGreaterThan(0);
        renderDocument.RootElement.GetProperty("stateSummaries").GetArrayLength().Should().BeGreaterThan(0);
        renderDocument.RootElement.GetProperty("stateSummaries")[0].GetProperty("stateClass").GetString().Should().NotBeNullOrWhiteSpace();
        renderDocument.RootElement.GetProperty("stateSummaries")[2].GetProperty("anchorSliceId").GetString().Should().Be("phase-1");
        renderDocument.RootElement.GetProperty("groupingSummary").GetString().Should().Contain("20000 ms");
        renderDocument.RootElement.GetProperty("sliceSummaries")[0].GetProperty("timestampSpanMs").GetInt32().Should().BeGreaterThan(0);
        renderDocument.RootElement.TryGetProperty("eventSlices", out _).Should().BeFalse();
    }

    [Fact]
    public void Cli_Render_Phase_Stack_Human_Readable_Output_Includes_Final_State_Context()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "cli-phase3-text");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(["render-phase-stack", "--path", artifact.Layout.ManifestPath, "--view", "prime"]);
        var renderText = output.ToString();

        exitCode.Should().Be(0, renderText);
        renderText.Should().Contain("Temporal stack status: LawfullyDerived");
        renderText.Should().Contain("Final state: ");
        renderText.Should().Contain("Final anchor: ");
        renderText.Should().Contain("Final derived force: ");
        renderText.Should().Contain("Final basis signals: ");
        renderText.Should().Contain("Horizon summaries: ");
        renderText.Should().Contain("widened-duration-20000");
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

    [Fact]
    public void Cli_New_Phase3_Sample_Creates_A_Valid_Temporal_Artifact()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new-phase3-sample", "--output-dir", tempDir, "--name", "cli-phase3-sample", "--json"]);
        var createJson = output.ToString();

        output.GetStringBuilder().Clear();
        var validateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-phase3-sample.hopng.json"), "--json"]);
        var validateJson = output.ToString();

        output.GetStringBuilder().Clear();
        var renderExitCode = runner.Execute(["render-phase-stack", "--path", Path.Combine(tempDir, "cli-phase3-sample.hopng.json"), "--view", "prime", "--json"]);
        var renderJson = output.ToString();
        using var renderDocument = JsonDocument.Parse(renderJson);
        using var phasePolicyDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(tempDir, "cli-phase3-sample.phase-policy.json")));

        createExitCode.Should().Be(0, createJson);
        validateExitCode.Should().Be(0, validateJson);
        renderExitCode.Should().Be(0, renderJson);
        renderDocument.RootElement.GetProperty("status").GetInt32().Should().Be((int)TemporalStackStatus.LawfullyDerived);
        renderDocument.RootElement.GetProperty("phaseSliceCount").GetInt32().Should().BeGreaterThan(0);
        phasePolicyDocument.RootElement.TryGetProperty("phaseWindowDurationMs", out _).Should().BeTrue();
        phasePolicyDocument.RootElement.TryGetProperty("maxPhaseWindowSpanMs", out _).Should().BeTrue();
        phasePolicyDocument.RootElement.TryGetProperty("comparisonHorizons", out _).Should().BeTrue();
        phasePolicyDocument.RootElement.TryGetProperty("stateThresholds", out _).Should().BeTrue();
    }

    [Fact]
    public void Cli_New_Phase3_Invalid_Sample_Fails_Temporal_Validation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new-phase3-invalid-sample", "--output-dir", tempDir, "--name", "cli-phase3-invalid", "--json"]);
        var createJson = output.ToString();

        output.GetStringBuilder().Clear();
        var validateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-phase3-invalid.hopng.json"), "--json"]);
        var validateJson = output.ToString();
        using var validateDocument = JsonDocument.Parse(validateJson);

        output.GetStringBuilder().Clear();
        var renderExitCode = runner.Execute(["render-phase-stack", "--path", Path.Combine(tempDir, "cli-phase3-invalid.hopng.json"), "--view", "prime", "--json"]);
        var renderJson = output.ToString();
        using var renderDocument = JsonDocument.Parse(renderJson);

        createExitCode.Should().Be(0, createJson);
        validateExitCode.Should().Be((int)Hdt.Core.Validation.ValidationErrorCode.InvalidPhaseSlice, validateJson);
        validateDocument.RootElement.GetProperty("isValid").GetBoolean().Should().BeFalse();
        validateDocument.RootElement.GetProperty("errors").EnumerateArray()
            .Any(error => error.GetProperty("code").GetInt32() == (int)Hdt.Core.Validation.ValidationErrorCode.InvalidPhaseSlice)
            .Should().BeTrue(validateJson);
        renderExitCode.Should().Be(24, renderJson);
        renderDocument.RootElement.GetProperty("status").GetInt32().Should().Be((int)TemporalStackStatus.StructurallyIncomplete);
        renderDocument.RootElement.GetProperty("validationIssues").EnumerateArray()
            .Any(error => error.GetProperty("code").GetInt32() == (int)Hdt.Core.Validation.ValidationErrorCode.InvalidPhaseSlice)
            .Should().BeTrue(renderJson);
    }

    [Fact]
    public void Cli_Compare_Phase_Stacks_Classifies_Lawful_Peer_As_Delayed()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase3ArtifactFactory.CreateValid(tempDir, "cli-phase3-left");
        var rightArtifact = Phase3ArtifactFactory.CreateDelayedPeer(tempDir, "cli-phase3-right");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(
        [
            "compare-phase-stacks",
            "--left", leftArtifact.Layout.ManifestPath,
            "--right", rightArtifact.Layout.ManifestPath,
            "--json"
        ]);
        var comparisonJson = output.ToString();
        using var comparisonDocument = JsonDocument.Parse(comparisonJson);

        exitCode.Should().Be(0, comparisonJson);
        comparisonDocument.RootElement.GetProperty("classification").GetString().Should().Be("Delayed");
        comparisonDocument.RootElement.GetProperty("basisAlignmentStatus").GetString().Should().Be("Aligned");
        comparisonDocument.RootElement.GetProperty("temporalStateCompatibility").GetString().Should().Be("Delayed");
        comparisonDocument.RootElement.GetProperty("stateRankDelta").GetInt32().Should().Be(1);
        comparisonDocument.RootElement.GetProperty("classificationReason").GetString().Should().Contain("immediately prior lawful state");
    }

    [Fact]
    public void Cli_Compare_Phase_Stacks_Classifies_Lawful_Peer_As_Divergent()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase3ArtifactFactory.CreateValid(tempDir, "cli-phase3-left-divergent");
        var rightArtifact = Phase3ArtifactFactory.CreateDivergentPeer(tempDir, "cli-phase3-right-divergent");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(
        [
            "compare-phase-stacks",
            "--left", leftArtifact.Layout.ManifestPath,
            "--right", rightArtifact.Layout.ManifestPath,
            "--json"
        ]);
        var comparisonJson = output.ToString();
        using var comparisonDocument = JsonDocument.Parse(comparisonJson);

        exitCode.Should().Be(0, comparisonJson);
        comparisonDocument.RootElement.GetProperty("classification").GetString().Should().Be("Divergent");
        comparisonDocument.RootElement.GetProperty("basisAlignmentStatus").GetString().Should().Be("Aligned");
        comparisonDocument.RootElement.GetProperty("temporalStateCompatibility").GetString().Should().Be("Divergent");
        comparisonDocument.RootElement.GetProperty("stateRankDelta").GetInt32().Should().Be(2);
        comparisonDocument.RootElement.GetProperty("classificationReason").GetString().Should().Contain("no longer converges");
    }

    [Fact]
    public void Cli_Compare_Phase_Stacks_Human_Readable_Output_Includes_Basis_And_Signal_Summaries()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase3ArtifactFactory.CreateValid(tempDir, "cli-phase3-left-text");
        var rightArtifact = Phase3ArtifactFactory.CreateDelayedPeer(tempDir, "cli-phase3-right-text");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(
        [
            "compare-phase-stacks",
            "--left", leftArtifact.Layout.ManifestPath,
            "--right", rightArtifact.Layout.ManifestPath
        ]);
        var comparisonText = output.ToString();

        exitCode.Should().Be(0, comparisonText);
        comparisonText.Should().Contain("Temporal comparison classification: Delayed");
        comparisonText.Should().Contain("Comparable phase slices: ");
        comparisonText.Should().Contain("State rank delta: +1");
        comparisonText.Should().Contain("Classification reason: ");
        comparisonText.Should().Contain("Basis signals: ");
        comparisonText.Should().Contain("Signals: ");
        comparisonText.Should().Contain("Payload mode: prime");
    }

    [Fact]
    public void Cli_Compare_Phase_Stacks_Returns_Nonzero_When_Basis_Is_Incompatible()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase3ArtifactFactory.CreateValid(tempDir, "cli-phase3-basis-left");
        var rightArtifact = Phase3ArtifactFactory.CreateIncompatiblePrimaryHorizon(tempDir, "cli-phase3-basis-right");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(
        [
            "compare-phase-stacks",
            "--left", leftArtifact.Layout.ManifestPath,
            "--right", rightArtifact.Layout.ManifestPath,
            "--json"
        ]);
        var comparisonJson = output.ToString();
        using var comparisonDocument = JsonDocument.Parse(comparisonJson);

        exitCode.Should().Be(24, comparisonJson);
        comparisonDocument.RootElement.GetProperty("classification").GetString().Should().Be("Incompatible");
        comparisonDocument.RootElement.GetProperty("basisAlignmentStatus").GetString().Should().Be("Incompatible");
        comparisonDocument.RootElement.GetProperty("stateRankDelta").GetInt32().Should().Be(0);
        comparisonDocument.RootElement.GetProperty("classificationReason").GetString().Should().Contain("basis differs");
    }

    [Fact]
    public void Cli_Compare_Phase_Stacks_Returns_Unsupported_When_Temporal_Derivation_Is_Invalid()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var validArtifact = Phase3ArtifactFactory.CreateValid(tempDir, "cli-phase3-compare-valid");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new-phase3-invalid-sample", "--output-dir", tempDir, "--name", "cli-phase3-compare-invalid", "--json"]);
        createExitCode.Should().Be(0);
        output.GetStringBuilder().Clear();

        var exitCode = runner.Execute(
        [
            "compare-phase-stacks",
            "--left", validArtifact.Layout.ManifestPath,
            "--right", Path.Combine(tempDir, "cli-phase3-compare-invalid.hopng.json"),
            "--json"
        ]);
        var comparisonJson = output.ToString();
        using var comparisonDocument = JsonDocument.Parse(comparisonJson);

        exitCode.Should().Be(25, comparisonJson);
        comparisonDocument.RootElement.GetProperty("classification").GetString().Should().Be("FlattenedOrUnsupported");
        comparisonDocument.RootElement.GetProperty("classificationReason").GetString().Should().Contain("not lawfully derived");
    }

    [Fact]
    public void Cli_New_Phase3_Incompatible_Basis_Sample_Creates_A_Valid_Artifact()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new-phase3-incompatible-basis-sample", "--output-dir", tempDir, "--name", "cli-phase3-incompatible", "--json"]);
        var createJson = output.ToString();

        output.GetStringBuilder().Clear();
        var validateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-phase3-incompatible.hopng.json"), "--json"]);
        var validateJson = output.ToString();

        output.GetStringBuilder().Clear();
        var compareExitCode = runner.Execute(
        [
            "compare-phase-stacks",
            "--left", Path.Combine(tempDir, "cli-phase3-incompatible.hopng.json"),
            "--right", Path.Combine(tempDir, "cli-phase3-incompatible.hopng.json"),
            "--json"
        ]);
        var compareJson = output.ToString();
        using var compareDocument = JsonDocument.Parse(compareJson);

        createExitCode.Should().Be(0, createJson);
        validateExitCode.Should().Be(0, validateJson);
        compareExitCode.Should().Be(0, compareJson);
        compareDocument.RootElement.GetProperty("classification").GetString().Should().Be("Convergent");
    }

    [Fact]
    public void Cli_New_Phase3_Divergent_Peer_Sample_Creates_A_Divergent_Artifact()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var baseCreateExitCode = runner.Execute(["new-phase3-sample", "--output-dir", tempDir, "--name", "cli-phase3-base", "--json"]);
        output.GetStringBuilder().Clear();
        var createExitCode = runner.Execute(["new-phase3-divergent-peer-sample", "--output-dir", tempDir, "--name", "cli-phase3-divergent", "--json"]);
        var createJson = output.ToString();

        output.GetStringBuilder().Clear();
        var compareExitCode = runner.Execute(
        [
            "compare-phase-stacks",
            "--left", Path.Combine(tempDir, "cli-phase3-base.hopng.json"),
            "--right", Path.Combine(tempDir, "cli-phase3-divergent.hopng.json"),
            "--json"
        ]);
        var compareJson = output.ToString();
        using var compareDocument = JsonDocument.Parse(compareJson);

        baseCreateExitCode.Should().Be(0);
        createExitCode.Should().Be(0, createJson);
        compareExitCode.Should().Be(0, compareJson);
        compareDocument.RootElement.GetProperty("classification").GetString().Should().Be("Divergent");
    }

    [Fact]
    public void Cli_Help_Includes_Temporal_Exit_Code_Guidance()
    {
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(["help"]);
        var helpText = output.ToString();

        exitCode.Should().Be(0, helpText);
        helpText.Should().Contain("21 reserved later-phase command invoked");
        helpText.Should().Contain("24 temporal derivation incomplete or basis-incompatible temporal comparison");
        helpText.Should().Contain("25 flattened, unsupported, or invalid derivation or comparison surface");
    }
}
