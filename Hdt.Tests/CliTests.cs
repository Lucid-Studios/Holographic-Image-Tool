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
    public void Cli_New_Phase3_Sample_Allows_External_Private_Key_Output()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var keyDir = TestPaths.CreateTempDirectory();
        var privateKeyPath = Path.Combine(keyDir, "phase3-reference.ed25519.private.key");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(
        [
            "new-phase3-sample",
            "--output-dir", tempDir,
            "--name", "cli-phase3-external-key",
            "--private-key-out", privateKeyPath,
            "--json"
        ]);

        createExitCode.Should().Be(0, output.ToString());
        File.Exists(privateKeyPath).Should().BeTrue();
        File.Exists(Path.Combine(tempDir, "cli-phase3-external-key.ed25519.private.key")).Should().BeFalse();
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
        helpText.Should().Contain("24 temporal derivation incomplete, basis-incompatible comparison, or support-type-incompatible engram comparison");
        helpText.Should().Contain("25 flattened, unsupported, counterfeit, or invalid derivation or comparison surface");
    }

    [Fact]
    public void Cli_New_Phase4_Perspectival_Sample_Creates_A_Valid_Entry_Artifact()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new-phase4-perspectival-sample", "--output-dir", tempDir, "--name", "cli-phase4-perspectival", "--json"]);
        output.GetStringBuilder().Clear();
        var validateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-phase4-perspectival.hopng.json"), "--json"]);
        var validateJson = output.ToString();

        output.GetStringBuilder().Clear();
        var showExitCode = runner.Execute(["show", "--path", Path.Combine(tempDir, "cli-phase4-perspectival.hopng.json"), "--view", "prime", "--json"]);
        var showJson = output.ToString();

        createExitCode.Should().Be(0);
        validateExitCode.Should().Be(0, validateJson);
        showExitCode.Should().Be(0, showJson);
        showJson.Should().Contain("\"engramSupportSummary\"");
        showJson.Should().Contain("\"engramStabilityField\"");
        showJson.Should().Contain("\"supportType\": \"perspectival\"");
        showJson.Should().Contain("\"workingIntentState\": \"supported_intent\"");
    }

    [Fact]
    public void Cli_New_Phase4_Perspectival_Sample_Allows_External_Private_Key_Output()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var keyDir = TestPaths.CreateTempDirectory();
        var privateKeyPath = Path.Combine(keyDir, "phase4-reference.ed25519.private.key");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(
        [
            "new-phase4-perspectival-sample",
            "--output-dir", tempDir,
            "--name", "cli-phase4-external-key",
            "--private-key-out", privateKeyPath,
            "--json"
        ]);

        createExitCode.Should().Be(0, output.ToString());
        File.Exists(privateKeyPath).Should().BeTrue();
        File.Exists(Path.Combine(tempDir, "cli-phase4-external-key.ed25519.private.key")).Should().BeFalse();
    }

    [Fact]
    public void Cli_New_Phase4_Perspectival_Peer_Sample_Creates_A_Valid_Entry_Artifact()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new-phase4-perspectival-peer-sample", "--output-dir", tempDir, "--name", "cli-phase4-perspectival-peer", "--json"]);
        output.GetStringBuilder().Clear();
        var compareExitCode = runner.Execute(
        [
            "compare-engram-support",
            "--left", Path.Combine(tempDir, "cli-phase4-perspectival-peer.hopng.json"),
            "--right", Path.Combine(tempDir, "cli-phase4-perspectival-peer.hopng.json"),
            "--json"
        ]);
        var compareJson = output.ToString();
        using var compareDocument = JsonDocument.Parse(compareJson);

        createExitCode.Should().Be(0);
        compareExitCode.Should().Be(0, compareJson);
        compareDocument.RootElement.GetProperty("classification").GetString().Should().Be("CoherentSupport");
        compareDocument.RootElement.GetProperty("supportTypeCompatibility").GetString().Should().Be("Aligned");
    }

    [Fact]
    public void Cli_New_Phase4_Invalid_Perspectival_Sample_Fails_Validation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new-phase4-invalid-perspectival-sample", "--output-dir", tempDir, "--name", "cli-phase4-perspectival-invalid", "--json"]);
        output.GetStringBuilder().Clear();
        var validateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-phase4-perspectival-invalid.hopng.json"), "--json"]);
        var validateJson = output.ToString();
        using var validateDocument = JsonDocument.Parse(validateJson);

        createExitCode.Should().Be(0);
        validateExitCode.Should().Be((int)Hdt.Core.Validation.ValidationErrorCode.InvalidPerspectivalEngram, validateJson);
        validateDocument.RootElement.GetProperty("errors").EnumerateArray()
            .Any(error => error.GetProperty("code").GetInt32() == (int)Hdt.Core.Validation.ValidationErrorCode.InvalidPerspectivalEngram)
            .Should().BeTrue(validateJson);
    }

    [Fact]
    public void Cli_New_Phase4_Participatory_Sample_Creates_A_Valid_Entry_Artifact()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new-phase4-participatory-sample", "--output-dir", tempDir, "--name", "cli-phase4-participatory", "--json"]);
        output.GetStringBuilder().Clear();
        var validateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-phase4-participatory.hopng.json"), "--json"]);
        var validateJson = output.ToString();

        output.GetStringBuilder().Clear();
        var showExitCode = runner.Execute(["show", "--path", Path.Combine(tempDir, "cli-phase4-participatory.hopng.json"), "--view", "prime", "--json"]);
        var showJson = output.ToString();

        createExitCode.Should().Be(0);
        validateExitCode.Should().Be(0, validateJson);
        showExitCode.Should().Be(0, showJson);
        showJson.Should().Contain("\"engramStabilityField\"");
        showJson.Should().Contain("\"supportType\": \"participatory\"");
        showJson.Should().Contain("\"workingIntentState\": \"reviewable_support\"");
    }

    [Fact]
    public void Cli_Compare_Engram_Support_Classifies_Perspectival_Peer_As_Strengthened()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase4ArtifactFactory.CreatePerspectivalSupport(tempDir, "cli-phase4-perspectival-left");
        var rightArtifact = Phase4ArtifactFactory.CreatePerspectivalSupportPeer(tempDir, "cli-phase4-perspectival-right");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(
        [
            "compare-engram-support",
            "--left", leftArtifact.Layout.ManifestPath,
            "--right", rightArtifact.Layout.ManifestPath,
            "--json"
        ]);
        var comparisonJson = output.ToString();
        using var comparisonDocument = JsonDocument.Parse(comparisonJson);

        exitCode.Should().Be(0, comparisonJson);
        comparisonDocument.RootElement.GetProperty("classification").GetString().Should().Be("StrengthenedSupport");
        comparisonDocument.RootElement.GetProperty("supportIdentityCompatibility").GetString().Should().Be("RootTraceable");
        comparisonDocument.RootElement.GetProperty("counterfeitPressureStatus").GetString().Should().Be("none");
        comparisonDocument.RootElement.GetProperty("workingIntentTransitionStatus").GetString().Should().Be("Strengthening");
        comparisonDocument.RootElement.GetProperty("workingIntentRankDelta").GetInt32().Should().Be(1);
    }

    [Fact]
    public void Cli_Compare_Engram_Support_Human_Readable_Output_Includes_Counterfeit_And_Coherence_Context()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase4ArtifactFactory.CreateParticipatorySupport(tempDir, "cli-phase4-participatory-left");
        var rightArtifact = Phase4ArtifactFactory.CreateParticipatorySupportPeer(tempDir, "cli-phase4-participatory-right");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(
        [
            "compare-engram-support",
            "--left", leftArtifact.Layout.ManifestPath,
            "--right", rightArtifact.Layout.ManifestPath
        ]);
        var comparisonText = output.ToString();

        exitCode.Should().Be(0, comparisonText);
        comparisonText.Should().Contain("Engram support comparison classification: CoherentSupport");
        comparisonText.Should().Contain("Support shapes: ");
        comparisonText.Should().Contain("Support identity compatibility: ");
        comparisonText.Should().Contain("Counterfeit pressure: ");
        comparisonText.Should().Contain("Intent classifications: ");
        comparisonText.Should().Contain("Working-intent transition: Stable");
        comparisonText.Should().Contain("Working-intent rank delta: ");
        comparisonText.Should().Contain("Similarity score: ");
        comparisonText.Should().Contain("Signals: ");
    }

    [Fact]
    public void Cli_Compare_Engram_Support_Returns_Nonzero_When_Support_Types_Differ()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase4ArtifactFactory.CreatePerspectivalSupport(tempDir, "cli-phase4-incompat-left");
        var rightArtifact = Phase4ArtifactFactory.CreateParticipatorySupport(tempDir, "cli-phase4-incompat-right");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(
        [
            "compare-engram-support",
            "--left", leftArtifact.Layout.ManifestPath,
            "--right", rightArtifact.Layout.ManifestPath,
            "--json"
        ]);
        var comparisonJson = output.ToString();
        using var comparisonDocument = JsonDocument.Parse(comparisonJson);

        exitCode.Should().Be(24, comparisonJson);
        comparisonDocument.RootElement.GetProperty("classification").GetString().Should().Be("IncompatibleSupportType");
    }

    [Fact]
    public void Cli_Compare_Engram_Support_Returns_Counterfeit_When_Right_Artifact_Is_Invalid()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var lawfulArtifact = Phase4ArtifactFactory.CreatePerspectivalSupport(tempDir, "cli-phase4-lawful");
        var invalidArtifact = Phase4ArtifactFactory.CreateInvalidPerspectivalSupport(tempDir, "cli-phase4-invalid");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var exitCode = runner.Execute(
        [
            "compare-engram-support",
            "--left", lawfulArtifact.Layout.ManifestPath,
            "--right", invalidArtifact.Layout.ManifestPath,
            "--json"
        ]);
        var comparisonJson = output.ToString();
        using var comparisonDocument = JsonDocument.Parse(comparisonJson);

        exitCode.Should().Be(25, comparisonJson);
        comparisonDocument.RootElement.GetProperty("classification").GetString().Should().Be("CounterfeitOrUnsupported");
        comparisonDocument.RootElement.GetProperty("counterfeitPressureStatus").GetString().Should().Be("detected");
    }

    [Fact]
    public void Cli_New_Phase4_Restricted_Deferred_And_Rejected_Samples_Create_Valid_Entry_Artifacts()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var restrictedExitCode = runner.Execute(["new-phase4-restricted-perspectival-sample", "--output-dir", tempDir, "--name", "cli-phase4-restricted", "--json"]);
        output.GetStringBuilder().Clear();
        var deferredExitCode = runner.Execute(["new-phase4-deferred-perspectival-sample", "--output-dir", tempDir, "--name", "cli-phase4-deferred", "--json"]);
        output.GetStringBuilder().Clear();
        var rejectedExitCode = runner.Execute(["new-phase4-rejected-participatory-sample", "--output-dir", tempDir, "--name", "cli-phase4-rejected", "--json"]);
        output.GetStringBuilder().Clear();

        var restrictedValidateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-phase4-restricted.hopng.json"), "--json"]);
        var restrictedValidateJson = output.ToString();
        output.GetStringBuilder().Clear();
        var deferredValidateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-phase4-deferred.hopng.json"), "--json"]);
        var deferredValidateJson = output.ToString();
        output.GetStringBuilder().Clear();
        var rejectedValidateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-phase4-rejected.hopng.json"), "--json"]);
        var rejectedValidateJson = output.ToString();

        restrictedExitCode.Should().Be(0);
        deferredExitCode.Should().Be(0);
        rejectedExitCode.Should().Be(0);
        restrictedValidateExitCode.Should().Be(0, restrictedValidateJson);
        deferredValidateExitCode.Should().Be(0, deferredValidateJson);
        rejectedValidateExitCode.Should().Be(0, rejectedValidateJson);
    }

    [Fact]
    public void Cli_Compare_Engram_Support_Classifies_Restricted_Deferred_And_Rejected_States()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var basePerspectival = Phase4ArtifactFactory.CreatePerspectivalSupport(tempDir, "cli-phase4-base-perspectival");
        var restrictedPerspectival = Phase4ArtifactFactory.CreateRestrictedPerspectivalSupport(tempDir, "cli-phase4-restricted-perspectival");
        var deferredPerspectival = Phase4ArtifactFactory.CreateDeferredPerspectivalSupport(tempDir, "cli-phase4-deferred-perspectival");
        var baseParticipatory = Phase4ArtifactFactory.CreateParticipatorySupport(tempDir, "cli-phase4-base-participatory");
        var rejectedParticipatory = Phase4ArtifactFactory.CreateRejectedParticipatorySupport(tempDir, "cli-phase4-rejected-participatory");
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var restrictedExitCode = runner.Execute(
        [
            "compare-engram-support",
            "--left", basePerspectival.Layout.ManifestPath,
            "--right", restrictedPerspectival.Layout.ManifestPath,
            "--json"
        ]);
        var restrictedJson = output.ToString();
        using var restrictedDocument = JsonDocument.Parse(restrictedJson);
        output.GetStringBuilder().Clear();

        var deferredExitCode = runner.Execute(
        [
            "compare-engram-support",
            "--left", basePerspectival.Layout.ManifestPath,
            "--right", deferredPerspectival.Layout.ManifestPath,
            "--json"
        ]);
        var deferredJson = output.ToString();
        using var deferredDocument = JsonDocument.Parse(deferredJson);
        output.GetStringBuilder().Clear();

        var rejectedExitCode = runner.Execute(
        [
            "compare-engram-support",
            "--left", baseParticipatory.Layout.ManifestPath,
            "--right", rejectedParticipatory.Layout.ManifestPath,
            "--json"
        ]);
        var rejectedJson = output.ToString();
        using var rejectedDocument = JsonDocument.Parse(rejectedJson);

        restrictedExitCode.Should().Be(0, restrictedJson);
        deferredExitCode.Should().Be(0, deferredJson);
        rejectedExitCode.Should().Be(0, rejectedJson);
        restrictedDocument.RootElement.GetProperty("classification").GetString().Should().Be("RestrictedSupport");
        restrictedDocument.RootElement.GetProperty("workingIntentTransitionStatus").GetString().Should().Be("Restricted");
        deferredDocument.RootElement.GetProperty("classification").GetString().Should().Be("DeferredSupport");
        deferredDocument.RootElement.GetProperty("workingIntentTransitionStatus").GetString().Should().Be("Deferred");
        rejectedDocument.RootElement.GetProperty("classification").GetString().Should().Be("RejectedSupport");
        rejectedDocument.RootElement.GetProperty("workingIntentTransitionStatus").GetString().Should().Be("Rejected");
    }

    [Fact]
    public void Cli_New_Phase4_Invalid_Participatory_Sample_Fails_Validation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new-phase4-invalid-participatory-sample", "--output-dir", tempDir, "--name", "cli-phase4-participatory-invalid", "--json"]);
        output.GetStringBuilder().Clear();
        var validateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-phase4-participatory-invalid.hopng.json"), "--json"]);
        var validateJson = output.ToString();
        using var validateDocument = JsonDocument.Parse(validateJson);

        createExitCode.Should().Be(0);
        validateExitCode.Should().Be((int)Hdt.Core.Validation.ValidationErrorCode.InvalidParticipatoryEngram, validateJson);
        validateDocument.RootElement.GetProperty("errors").EnumerateArray()
            .Any(error => error.GetProperty("code").GetInt32() == (int)Hdt.Core.Validation.ValidationErrorCode.InvalidParticipatoryEngram)
            .Should().BeTrue(validateJson);
    }
}
