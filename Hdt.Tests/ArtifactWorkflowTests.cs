using FluentAssertions;
using System.Text.Json;
using Hdt.Core.Security;
using Hdt.Core.Services;
using Hdt.Core.Models;
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

    [Fact]
    public void Phase1_Artifacts_Are_Not_Marked_As_Flattening_Risk_During_Inspection()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "phase1-inspection", "tester", "key-1"));
        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);
        var diagnostics = new Hdt.Core.Diagnostics.ArtifactDiagnosticService().Analyze(artifact, validation);

        diagnostics.FlattenedProjectionRisk.Should().BeFalse();
        diagnostics.Signals.Should().Contain(signal => signal.Contains("Phase 1 only", StringComparison.Ordinal));
    }

    [Fact]
    public void Valid_Phase2_Artifact_Passes_Validation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-valid");

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeTrue();
        artifact.UniverseLayerSet.Should().NotBeNull();
        artifact.GluingManifest.Should().NotBeNull();
        artifact.ProjectionRules.Should().NotBeNull();
        artifact.LegibilityProfile.Should().NotBeNull();
    }

    [Fact]
    public void Phase2_Validation_Fails_When_Gluing_References_Unknown_Universe()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-bad-glue");
        JsonFile.Mutate(artifact.Layout.GluingManifestPath, json =>
        {
            json["relations"]!.AsArray()[0]!["targetUniverseId"] = "missing-universe";
        });

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidGluingManifest);
    }

    [Fact]
    public void Phase2_Validation_Fails_When_Projection_Target_Is_Unknown()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-bad-rule");
        JsonFile.Mutate(artifact.Layout.ProjectionRulesPath, json =>
        {
            json["rules"]!.AsArray()[0]!["targetProjectionRole"] = "missing-role";
        });

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidProjectionRules);
    }

    [Fact]
    public void Phase2_Validation_Fails_When_Legibility_Profile_Requires_Unknown_Universe()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-bad-legibility");
        JsonFile.Mutate(artifact.Layout.LegibilityProfilePath, json =>
        {
            json["requiredUniverses"]!.AsArray()[0] = "missing-universe";
        });

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidLegibilityProfile);
    }

    [Fact]
    public void Phase2_Diagnostics_Mark_Relationally_Incomplete_Artifacts_As_Flattened_Risk()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-diagnostics");
        JsonFile.Mutate(artifact.Layout.GluingManifestPath, json =>
        {
            json["relations"]!.AsArray()[0]!["targetUniverseId"] = "missing-universe";
        });

        var validator = new HopngArtifactValidator();
        var validation = validator.Validate(artifact.Layout.ManifestPath);
        var inspectedArtifact = new HopngArtifactLoader().Load(artifact.Layout.ManifestPath);
        var diagnostics = new Hdt.Core.Diagnostics.ArtifactDiagnosticService().Analyze(inspectedArtifact, validation);

        validation.IsValid.Should().BeFalse();
        diagnostics.FlattenedProjectionRisk.Should().BeTrue();
    }

    [Fact]
    public void Privileged_Inspection_Exposes_Phase2_Relational_Structures()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-inspection");
        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);
        var view = new HopngArtifactInspector().BuildPrivilegedView(artifact, validation);
        var json = JsonSerializer.Serialize(view);

        json.Should().Contain("universeLayerSet");
        json.Should().Contain("gluingManifest");
        json.Should().Contain("projectionRules");
        json.Should().Contain("legibilityProfile");
    }

    [Fact]
    public void Governed_Projection_Derivation_Is_Deterministic_And_Ordered()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-derivation");
        var service = new GovernedProjectionDerivationService();

        var first = service.Derive(artifact.Layout.ManifestPath);
        var second = service.Derive(artifact.Layout.ManifestPath);

        first.Status.Should().Be(ProjectionFormationStatus.LawfullyFormed);
        first.RuleTrace.Select(trace => trace.RuleId).Should().ContainInOrder("rule-1", "rule-2");
        first.Should().BeEquivalentTo(second);
    }

    [Fact]
    public void Governed_Projection_Becomes_Incomplete_When_Required_Universe_Is_Not_Derived()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-missing-universe");
        JsonFile.Mutate(artifact.Layout.ProjectionRulesPath, json =>
        {
            var rules = json["rules"]!.AsArray();
            rules.RemoveAt(1);
        });
        artifact = Phase2ArtifactFactory.RefreshIntegrity(artifact);

        var result = new GovernedProjectionDerivationService().Derive(artifact.Layout.ManifestPath);

        result.Status.Should().Be(ProjectionFormationStatus.StructurallyIncomplete);
        result.Issues.Should().Contain(issue => issue.Contains("cryptic-support", StringComparison.Ordinal));
    }

    [Fact]
    public void Governed_Projection_Becomes_Incomplete_When_Required_Relation_Is_Not_Present()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-missing-relation");
        JsonFile.Mutate(artifact.Layout.GluingManifestPath, json =>
        {
            json["relations"] = new System.Text.Json.Nodes.JsonArray();
        });
        artifact = Phase2ArtifactFactory.RefreshIntegrity(artifact);

        var result = new GovernedProjectionDerivationService().Derive(artifact.Layout.ManifestPath);

        result.Status.Should().Be(ProjectionFormationStatus.StructurallyIncomplete);
        result.Issues.Should().Contain(issue => issue.Contains("glue-1", StringComparison.Ordinal));
    }

    [Fact]
    public void Projection_Comparison_Distinguishes_Formed_From_Flattened_Artifacts()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var phase2Artifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-formed");
        var phase1Artifact = new HopngArtifactBuilder().Create(new NewHopngRequest(tempDir, "phase1-flat", "tester", "key-1"));
        var derivationService = new GovernedProjectionDerivationService();
        var comparisonService = new ProjectionSupportComparisonService();

        var comparison = comparisonService.Compare(
            derivationService.Derive(phase2Artifact.Layout.ManifestPath),
            derivationService.Derive(phase1Artifact.Layout.ManifestPath));

        comparison.Classification.Should().Be("formed-vs-flattened");
        comparison.Signals.Should().Contain(signal => signal.Contains("lacks governed derivation support", StringComparison.Ordinal));
    }

    [Fact]
    public void Projection_Comparison_Distinguishes_Formed_From_Incomplete_Artifacts()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var formedArtifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-formed-compare");
        var incompleteArtifact = Phase2ArtifactFactory.CreateValid(tempDir, "phase2-incomplete-compare");
        JsonFile.Mutate(incompleteArtifact.Layout.ProjectionRulesPath, json =>
        {
            var rules = json["rules"]!.AsArray();
            rules.RemoveAt(1);
        });
        incompleteArtifact = Phase2ArtifactFactory.RefreshIntegrity(incompleteArtifact);

        var derivationService = new GovernedProjectionDerivationService();
        var comparisonService = new ProjectionSupportComparisonService();
        var comparison = comparisonService.Compare(
            derivationService.Derive(formedArtifact.Layout.ManifestPath),
            derivationService.Derive(incompleteArtifact.Layout.ManifestPath));

        comparison.Classification.Should().Be("formed-vs-incomplete");
        comparison.RightIssues.Should().Contain(issue => issue.Contains("cryptic-support", StringComparison.Ordinal));
    }

    [Fact]
    public void Valid_Phase3_Artifact_Passes_Validation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-valid");

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeTrue();
        artifact.EventSliceSet.Should().NotBeNull();
        artifact.PhaseSliceSet.Should().NotBeNull();
        artifact.PhasePolicy.Should().NotBeNull();
        artifact.OpticalChannelsDefinition.Should().NotBeNull();
    }

    [Fact]
    public void Phase3_Validation_Fails_When_Observed_Set_Does_Not_Match_Raw_Count()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-bad-observed-set");
        JsonFile.Mutate(artifact.Layout.EventSlicePath, json =>
        {
            json["observedSet"]!["rawSliceCount"] = 31;
        });
        artifact = Phase2ArtifactFactory.RefreshIntegrity(artifact);

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidEventSlice);
    }

    [Fact]
    public void Phase3_Validation_Fails_When_Phase_Policy_Uses_Unsupported_Aggregation_Mode()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-bad-policy");
        JsonFile.Mutate(artifact.Layout.PhasePolicyPath, json =>
        {
            json["aggregationPolicies"]!["drift"] = "median";
        });
        artifact = Phase2ArtifactFactory.RefreshIntegrity(artifact);

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidPhasePolicy);
    }

    [Fact]
    public void Phase3_Validation_Allows_Duration_Window_Mode_When_Timestamps_Align()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-duration-window");
        JsonFile.Mutate(artifact.Layout.PhasePolicyPath, json =>
        {
            json["phaseWindowMode"] = "duration_ms";
            json["phaseWindowSizeEventSlices"] = 0;
            json["phaseWindowDurationMs"] = 20000;
            json["maxPhaseWindowSpanMs"] = 20000;
        });
        artifact = Phase2ArtifactFactory.RefreshIntegrity(artifact);

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Phase3_Validation_Fails_When_Duration_Window_Does_Not_Align_With_Timestamps()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-duration-misaligned");
        JsonFile.Mutate(artifact.Layout.PhasePolicyPath, json =>
        {
            json["phaseWindowMode"] = "duration_ms";
            json["phaseWindowSizeEventSlices"] = 0;
            json["phaseWindowDurationMs"] = 15000;
            json["maxPhaseWindowSpanMs"] = 20000;
        });
        artifact = Phase2ArtifactFactory.RefreshIntegrity(artifact);

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidPhaseSlice);
    }

    [Fact]
    public void Phase3_Validation_Fails_When_State_Thresholds_Are_Invalid()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-bad-thresholds");
        JsonFile.Mutate(artifact.Layout.PhasePolicyPath, json =>
        {
            json["stateThresholds"]!["rupturePressureMin"] = 0.2;
            json["stateThresholds"]!["risingPressureMin"] = 0.35;
            json["stateThresholds"]!["forceTopologyBonus"] = -0.1;
        });
        artifact = Phase2ArtifactFactory.RefreshIntegrity(artifact);

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidPhasePolicy);
    }

    [Fact]
    public void Phase3_Validation_Fails_When_Primary_Comparison_Horizon_Does_Not_Match_Legacy_Raw_Horizon()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-bad-horizon");
        JsonFile.Mutate(artifact.Layout.PhasePolicyPath, json =>
        {
            json["comparisonHorizons"]!.AsArray()[1]!["value"] = 30000;
        });
        artifact = Phase2ArtifactFactory.RefreshIntegrity(artifact);

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidPhasePolicy);
    }

    [Fact]
    public void Phase3_Render_Is_Deterministic_And_Flags_Drift()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-render");
        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);
        var service = new TemporalPhaseStackService();

        var first = service.Render(artifact, validation, "privileged");
        var second = service.Render(artifact, validation, "privileged");

        first.Status.Should().Be(TemporalStackStatus.LawfullyDerived);
        first.RequiredChannelCoverage.Should().BeTrue();
        first.DriftFlags.Should().NotBeEmpty();
        first.PrimaryHorizonId.Should().Be("widened-duration-20000");
        first.HorizonSummaries.Should().Contain(summary => summary.UseForStateClassification && summary.Mode == "duration_ms");
        first.StateSummaries.Should().Contain(summary => summary.AnchorSliceId == "phase-1");
        first.Should().BeEquivalentTo(second);
    }

    [Fact]
    public void Prime_Safe_View_Exposes_Temporal_Metadata_Without_Raw_Payloads()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-prime-view");
        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);
        var view = new HopngArtifactInspector().BuildPrimeSafeView(artifact, validation);
        var json = JsonSerializer.Serialize(view);

        json.Should().Contain("temporalSummary");
        json.Should().Contain("SliceSummaries");
        json.Should().NotContain("\"eventSlices\"");
        json.Should().NotContain("\"universeStates\"");
    }

    [Fact]
    public void Phase3_Slice_Digests_Remain_Stable_Across_Repeated_Validation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-digests");
        var firstLoad = new HopngArtifactLoader().Load(artifact.Layout.ManifestPath);
        var secondLoad = new HopngArtifactLoader().Load(artifact.Layout.ManifestPath);

        firstLoad.EventSliceSet!.Slices.Select(slice => slice.SliceDigest)
            .Should()
            .Equal(secondLoad.EventSliceSet!.Slices.Select(slice => slice.SliceDigest));
        firstLoad.PhaseSliceSet!.Slices.Select(slice => slice.SliceDigest)
            .Should()
            .Equal(secondLoad.PhaseSliceSet!.Slices.Select(slice => slice.SliceDigest));
    }

    [Fact]
    public void Phase3_Comparison_Classifies_Matching_Artifacts_As_Convergent()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-convergent");
        var comparison = new TemporalPhaseStackComparisonService().Compare(
            artifact.Layout.ManifestPath,
            artifact.Layout.ManifestPath);

        comparison.Classification.Should().Be("Convergent");
        comparison.BasisAlignmentStatus.Should().Be("Aligned");
        comparison.TemporalStateCompatibility.Should().Be("Aligned");
        comparison.TopologyDeltaCount.Should().Be(0);
    }

    [Fact]
    public void Phase3_Comparison_Classifies_Lawful_Peer_As_Delayed()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-left");
        var rightArtifact = Phase3ArtifactFactory.CreateDelayedPeer(tempDir, "phase3-right-delayed");
        var comparison = new TemporalPhaseStackComparisonService().Compare(
            leftArtifact.Layout.ManifestPath,
            rightArtifact.Layout.ManifestPath);

        comparison.Classification.Should().Be("Delayed");
        comparison.BasisAlignmentStatus.Should().Be("Aligned");
        comparison.TemporalStateCompatibility.Should().Be("Delayed");
        comparison.StateRankDelta.Should().Be(1);
        comparison.ClassificationReason.Should().Contain("immediately prior lawful state");
        comparison.Signals.Should().Contain(signal => signal.Contains("immediately prior lawful state", StringComparison.Ordinal));
    }

    [Fact]
    public void Phase3_Comparison_Classifies_Lawful_Peer_As_Divergent()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-left-divergent");
        var rightArtifact = Phase3ArtifactFactory.CreateDivergentPeer(tempDir, "phase3-right-divergent");
        var comparison = new TemporalPhaseStackComparisonService().Compare(
            leftArtifact.Layout.ManifestPath,
            rightArtifact.Layout.ManifestPath);

        comparison.Classification.Should().Be("Divergent");
        comparison.BasisAlignmentStatus.Should().Be("Aligned");
        comparison.TemporalStateCompatibility.Should().Be("Divergent");
        comparison.StateRankDelta.Should().Be(2);
        comparison.ClassificationReason.Should().Contain("no longer converges");
        comparison.Signals.Should().Contain(signal => signal.Contains("no longer converges", StringComparison.Ordinal));
    }

    [Fact]
    public void Phase3_Comparison_Fails_When_Primary_Horizon_Basis_Differs()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase3ArtifactFactory.CreateValid(tempDir, "phase3-basis-left");
        var rightArtifact = Phase3ArtifactFactory.CreateIncompatiblePrimaryHorizon(tempDir, "phase3-basis-right");

        var rightValidation = new HopngArtifactValidator().Validate(rightArtifact.Layout.ManifestPath);
        rightValidation.IsValid.Should().BeTrue();

        var comparison = new TemporalPhaseStackComparisonService().Compare(
            leftArtifact.Layout.ManifestPath,
            rightArtifact.Layout.ManifestPath);

        comparison.Classification.Should().Be("Incompatible");
        comparison.BasisAlignmentStatus.Should().Be("Incompatible");
        comparison.StateRankDelta.Should().Be(0);
        comparison.ClassificationReason.Should().Contain("primary comparison horizon basis differs");
        comparison.BasisSignals.Should().Contain(signal => signal.Contains("primary comparison horizon basis differs", StringComparison.Ordinal));
    }

    [Fact]
    public void Phase4_Perspectival_Support_Artifact_Passes_Validation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase4ArtifactFactory.CreatePerspectivalSupport(tempDir, "phase4-perspectival");

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeTrue();
        artifact.PerspectivalEngramSupport.Should().NotBeNull();
        artifact.ParticipatoryEngramSupport.Should().BeNull();
    }

    [Fact]
    public void Phase4_Participatory_Support_Artifact_Passes_Validation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase4ArtifactFactory.CreateParticipatorySupport(tempDir, "phase4-participatory");

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeTrue();
        artifact.ParticipatoryEngramSupport.Should().NotBeNull();
        artifact.PerspectivalEngramSupport.Should().BeNull();
    }

    [Fact]
    public void Phase4_Restricted_And_Deferred_And_Rejected_Support_Artifacts_Pass_Validation()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var restrictedArtifact = Phase4ArtifactFactory.CreateRestrictedPerspectivalSupport(tempDir, "phase4-restricted");
        var deferredArtifact = Phase4ArtifactFactory.CreateDeferredPerspectivalSupport(tempDir, "phase4-deferred");
        var rejectedArtifact = Phase4ArtifactFactory.CreateRejectedParticipatorySupport(tempDir, "phase4-rejected");
        var validator = new HopngArtifactValidator();

        var restrictedValidation = validator.Validate(restrictedArtifact.Layout.ManifestPath);
        var deferredValidation = validator.Validate(deferredArtifact.Layout.ManifestPath);
        var rejectedValidation = validator.Validate(rejectedArtifact.Layout.ManifestPath);

        restrictedValidation.IsValid.Should().BeTrue();
        deferredValidation.IsValid.Should().BeTrue();
        rejectedValidation.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Phase4_Validation_Fails_When_Perspectival_Support_Overclaims_Authority()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase4ArtifactFactory.CreateInvalidPerspectivalSupport(tempDir, "phase4-perspectival-invalid");

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidPerspectivalEngram);
    }

    [Fact]
    public void Phase4_Validation_Fails_When_Participatory_Support_Breaks_Handoff_And_Branch_Rules()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase4ArtifactFactory.CreateInvalidParticipatorySupport(tempDir, "phase4-participatory-invalid");

        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidParticipatoryEngram);
    }

    [Fact]
    public void Phase4_Comparison_Classifies_Perspectival_Peer_As_Strengthened_Support()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase4ArtifactFactory.CreatePerspectivalSupport(tempDir, "phase4-perspectival-left");
        var rightArtifact = Phase4ArtifactFactory.CreatePerspectivalSupportPeer(tempDir, "phase4-perspectival-right");

        var comparison = new EngramSupportComparisonService().Compare(
            leftArtifact.Layout.ManifestPath,
            rightArtifact.Layout.ManifestPath);

        comparison.Classification.Should().Be("StrengthenedSupport");
        comparison.SupportTypeCompatibility.Should().Be("Aligned");
        comparison.SupportIdentityCompatibility.Should().Be("RootTraceable");
        comparison.CounterfeitPressureStatus.Should().Be("none");
        comparison.WorkingIntentRankDelta.Should().Be(1);
        comparison.WorkingIntentTransitionStatus.Should().Be("Strengthening");
    }

    [Fact]
    public void Phase4_Comparison_Classifies_Participatory_Peer_As_Coherent_Support()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var leftArtifact = Phase4ArtifactFactory.CreateParticipatorySupport(tempDir, "phase4-participatory-left");
        var rightArtifact = Phase4ArtifactFactory.CreateParticipatorySupportPeer(tempDir, "phase4-participatory-right");

        var comparison = new EngramSupportComparisonService().Compare(
            leftArtifact.Layout.ManifestPath,
            rightArtifact.Layout.ManifestPath);

        comparison.Classification.Should().Be("CoherentSupport");
        comparison.SupportTypeCompatibility.Should().Be("Aligned");
        comparison.SupportIdentityCompatibility.Should().Be("BranchTraceable");
        comparison.CounterfeitPressureStatus.Should().Be("none");
        comparison.SharedSupportSignalCount.Should().BeGreaterThan(0);
        comparison.WorkingIntentTransitionStatus.Should().Be("Stable");
    }

    [Fact]
    public void Phase4_Comparison_Classifies_Restricted_Deferred_And_Rejected_Support_As_Lawful_Negative_States()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var basePerspectival = Phase4ArtifactFactory.CreatePerspectivalSupport(tempDir, "phase4-perspectival-base");
        var restrictedPerspectival = Phase4ArtifactFactory.CreateRestrictedPerspectivalSupport(tempDir, "phase4-perspectival-restricted");
        var deferredPerspectival = Phase4ArtifactFactory.CreateDeferredPerspectivalSupport(tempDir, "phase4-perspectival-deferred");
        var baseParticipatory = Phase4ArtifactFactory.CreateParticipatorySupport(tempDir, "phase4-participatory-base");
        var rejectedParticipatory = Phase4ArtifactFactory.CreateRejectedParticipatorySupport(tempDir, "phase4-participatory-rejected");
        var comparisonService = new EngramSupportComparisonService();

        var restrictedComparison = comparisonService.Compare(basePerspectival.Layout.ManifestPath, restrictedPerspectival.Layout.ManifestPath);
        var deferredComparison = comparisonService.Compare(basePerspectival.Layout.ManifestPath, deferredPerspectival.Layout.ManifestPath);
        var rejectedComparison = comparisonService.Compare(baseParticipatory.Layout.ManifestPath, rejectedParticipatory.Layout.ManifestPath);

        restrictedComparison.Classification.Should().Be("RestrictedSupport");
        restrictedComparison.WorkingIntentTransitionStatus.Should().Be("Restricted");
        restrictedComparison.SupportIdentityCompatibility.Should().Be("RootTraceable");
        restrictedComparison.CounterfeitPressureStatus.Should().Be("none");

        deferredComparison.Classification.Should().Be("DeferredSupport");
        deferredComparison.WorkingIntentTransitionStatus.Should().Be("Deferred");
        deferredComparison.SupportIdentityCompatibility.Should().Be("RootTraceable");
        deferredComparison.CounterfeitPressureStatus.Should().Be("none");

        rejectedComparison.Classification.Should().Be("RejectedSupport");
        rejectedComparison.WorkingIntentTransitionStatus.Should().Be("Rejected");
        rejectedComparison.SupportIdentityCompatibility.Should().Be("BranchTraceable");
        rejectedComparison.CounterfeitPressureStatus.Should().Be("none");
    }

    [Fact]
    public void Phase4_Comparison_Classifies_Invalid_Peer_As_Counterfeit_Or_Unsupported()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var lawfulArtifact = Phase4ArtifactFactory.CreatePerspectivalSupport(tempDir, "phase4-lawful-compare");
        var invalidArtifact = Phase4ArtifactFactory.CreateInvalidPerspectivalSupport(tempDir, "phase4-invalid-compare");

        var comparison = new EngramSupportComparisonService().Compare(
            lawfulArtifact.Layout.ManifestPath,
            invalidArtifact.Layout.ManifestPath);

        comparison.Classification.Should().Be("CounterfeitOrUnsupported");
        comparison.CounterfeitPressureStatus.Should().Be("detected");
        comparison.ClassificationReason.Should().Contain("fails Phase 4 support validation");
    }

    [Fact]
    public void Phase4_Comparison_Fails_When_Support_Types_Differ()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var perspectivalArtifact = Phase4ArtifactFactory.CreatePerspectivalSupport(tempDir, "phase4-compare-perspectival");
        var participatoryArtifact = Phase4ArtifactFactory.CreateParticipatorySupport(tempDir, "phase4-compare-participatory");

        var comparison = new EngramSupportComparisonService().Compare(
            perspectivalArtifact.Layout.ManifestPath,
            participatoryArtifact.Layout.ManifestPath);

        comparison.Classification.Should().Be("IncompatibleSupportType");
        comparison.SupportTypeCompatibility.Should().Be("Incompatible");
        comparison.ClassificationReason.Should().Contain("same Phase 4 support type");
    }

    [Fact]
    public void Prime_Safe_View_Exposes_Engram_Support_Summary_Without_Protected_Payloads()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var artifact = Phase4ArtifactFactory.CreatePerspectivalSupport(tempDir, "phase4-prime-view");
        var validation = new HopngArtifactValidator().Validate(artifact.Layout.ManifestPath);
        var view = new HopngArtifactInspector().BuildPrimeSafeView(artifact, validation);
        var json = JsonSerializer.Serialize(view);

        json.Should().Contain("engramSupportSummary");
        json.Should().Contain("engramStabilityField");
        json.Should().Contain("ConstraintEnergy");
        json.Should().Contain("perspectival");
        json.Should().Contain("supported_intent");
        json.Should().NotContain("protectedEvidenceRefs");
    }

    [Fact]
    public void Committed_Examples_Do_Not_Include_Private_Keys()
    {
        var examplePrivateKeys = Directory.GetFiles(
            Path.Combine(TestPaths.RepositoryRoot, "examples"),
            "*.ed25519.private.key",
            SearchOption.AllDirectories);

        examplePrivateKeys.Should().BeEmpty("committed examples must remain public-safe");
    }

    [Fact]
    public void Committed_Phase4_Reference_Artifacts_Preserve_Support_Boundaries()
    {
        var repoRoot = TestPaths.RepositoryRoot;
        var validator = new HopngArtifactValidator();

        var lawfulPerspectivalValidation = validator.Validate(Path.Combine(repoRoot, "examples", "phase4-perspectival-sample.hopng.json"));
        var lawfulPerspectivalPeerValidation = validator.Validate(Path.Combine(repoRoot, "examples", "phase4-perspectival-peer.hopng.json"));
        var lawfulRestrictedPerspectivalValidation = validator.Validate(Path.Combine(repoRoot, "examples", "phase4-restricted-perspectival.hopng.json"));
        var lawfulDeferredPerspectivalValidation = validator.Validate(Path.Combine(repoRoot, "examples", "phase4-deferred-perspectival.hopng.json"));
        var lawfulParticipatoryValidation = validator.Validate(Path.Combine(repoRoot, "examples", "phase4-participatory-sample.hopng.json"));
        var lawfulParticipatoryPeerValidation = validator.Validate(Path.Combine(repoRoot, "examples", "phase4-participatory-peer.hopng.json"));
        var lawfulRejectedParticipatoryValidation = validator.Validate(Path.Combine(repoRoot, "examples", "phase4-rejected-participatory.hopng.json"));
        var invalidPerspectivalValidation = validator.Validate(Path.Combine(repoRoot, "examples", "phase4-invalid-perspectival.hopng.json"));
        var invalidParticipatoryValidation = validator.Validate(Path.Combine(repoRoot, "examples", "phase4-invalid-participatory.hopng.json"));

        lawfulPerspectivalValidation.IsValid.Should().BeTrue();
        lawfulPerspectivalPeerValidation.IsValid.Should().BeTrue();
        lawfulRestrictedPerspectivalValidation.IsValid.Should().BeTrue();
        lawfulDeferredPerspectivalValidation.IsValid.Should().BeTrue();
        lawfulParticipatoryValidation.IsValid.Should().BeTrue();
        lawfulParticipatoryPeerValidation.IsValid.Should().BeTrue();
        lawfulRejectedParticipatoryValidation.IsValid.Should().BeTrue();
        invalidPerspectivalValidation.IsValid.Should().BeFalse();
        invalidParticipatoryValidation.IsValid.Should().BeFalse();
        invalidPerspectivalValidation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidPerspectivalEngram);
        invalidParticipatoryValidation.Errors.Should().Contain(issue => issue.Code == ValidationErrorCode.InvalidParticipatoryEngram);

        var inspector = new HopngArtifactInspector();
        var lawfulPrimeView = JsonSerializer.Serialize(inspector.BuildPrimeSafeView(
            new HopngArtifactLoader().Load(Path.Combine(repoRoot, "examples", "phase4-perspectival-sample.hopng.json")),
            lawfulPerspectivalValidation));
        var participatoryPrimeView = JsonSerializer.Serialize(inspector.BuildPrimeSafeView(
            new HopngArtifactLoader().Load(Path.Combine(repoRoot, "examples", "phase4-participatory-sample.hopng.json")),
            lawfulParticipatoryValidation));

        lawfulPrimeView.Should().Contain("engramSupportSummary");
        lawfulPrimeView.Should().Contain("engramStabilityField");
        lawfulPrimeView.Should().Contain("supported_intent");
        participatoryPrimeView.Should().Contain("engramSupportSummary");
        participatoryPrimeView.Should().Contain("engramStabilityField");
        participatoryPrimeView.Should().Contain("reviewable_support");

        var comparisonService = new EngramSupportComparisonService();
        var committedPerspectivalComparison = comparisonService.Compare(
            Path.Combine(repoRoot, "examples", "phase4-perspectival-sample.hopng.json"),
            Path.Combine(repoRoot, "examples", "phase4-perspectival-peer.hopng.json"));
        var committedParticipatoryComparison = comparisonService.Compare(
            Path.Combine(repoRoot, "examples", "phase4-participatory-sample.hopng.json"),
            Path.Combine(repoRoot, "examples", "phase4-participatory-peer.hopng.json"));
        var committedRestrictedComparison = comparisonService.Compare(
            Path.Combine(repoRoot, "examples", "phase4-perspectival-sample.hopng.json"),
            Path.Combine(repoRoot, "examples", "phase4-restricted-perspectival.hopng.json"));
        var committedDeferredComparison = comparisonService.Compare(
            Path.Combine(repoRoot, "examples", "phase4-perspectival-sample.hopng.json"),
            Path.Combine(repoRoot, "examples", "phase4-deferred-perspectival.hopng.json"));
        var committedRejectedComparison = comparisonService.Compare(
            Path.Combine(repoRoot, "examples", "phase4-participatory-sample.hopng.json"),
            Path.Combine(repoRoot, "examples", "phase4-rejected-participatory.hopng.json"));

        committedPerspectivalComparison.Classification.Should().Be("StrengthenedSupport");
        committedParticipatoryComparison.Classification.Should().Be("CoherentSupport");
        committedRestrictedComparison.Classification.Should().Be("RestrictedSupport");
        committedDeferredComparison.Classification.Should().Be("DeferredSupport");
        committedRejectedComparison.Classification.Should().Be("RejectedSupport");
    }

    [Fact]
    public void Committed_Phase4_Reference_Artifacts_Preserve_Working_Intent_And_Support_Markers()
    {
        var repoRoot = TestPaths.RepositoryRoot;
        var loader = new HopngArtifactLoader();

        var lawfulPerspectival = loader.Load(Path.Combine(repoRoot, "examples", "phase4-perspectival-sample.hopng.json"));
        var lawfulRestrictedPerspectival = loader.Load(Path.Combine(repoRoot, "examples", "phase4-restricted-perspectival.hopng.json"));
        var lawfulDeferredPerspectival = loader.Load(Path.Combine(repoRoot, "examples", "phase4-deferred-perspectival.hopng.json"));
        var lawfulParticipatory = loader.Load(Path.Combine(repoRoot, "examples", "phase4-participatory-sample.hopng.json"));
        var lawfulRejectedParticipatory = loader.Load(Path.Combine(repoRoot, "examples", "phase4-rejected-participatory.hopng.json"));
        var invalidPerspectival = loader.Load(Path.Combine(repoRoot, "examples", "phase4-invalid-perspectival.hopng.json"));
        var invalidParticipatory = loader.Load(Path.Combine(repoRoot, "examples", "phase4-invalid-participatory.hopng.json"));

        lawfulPerspectival.PerspectivalEngramSupport.Should().NotBeNull();
        lawfulPerspectival.PerspectivalEngramSupport!.WorkingIntentState.Should().Be("supported_intent");
        lawfulPerspectival.PerspectivalEngramSupport.IntentClassification.Should().Be("bounded_support_evidence");
        lawfulPerspectival.PerspectivalEngramSupport.SupportShape.Should().Be("root_constructor_support");

        lawfulRestrictedPerspectival.PerspectivalEngramSupport.Should().NotBeNull();
        lawfulRestrictedPerspectival.PerspectivalEngramSupport!.WorkingIntentState.Should().Be("restricted_support");
        lawfulRestrictedPerspectival.PerspectivalEngramSupport.IntentClassification.Should().Be("restricted_support_evidence");
        lawfulRestrictedPerspectival.PerspectivalEngramSupport.SupportShape.Should().Be("root_constructor_support");
        lawfulRestrictedPerspectival.PerspectivalEngramSupport.RestrictionReason.Should().NotBeNullOrWhiteSpace();

        lawfulDeferredPerspectival.PerspectivalEngramSupport.Should().NotBeNull();
        lawfulDeferredPerspectival.PerspectivalEngramSupport!.WorkingIntentState.Should().Be("deferred_support");
        lawfulDeferredPerspectival.PerspectivalEngramSupport.IntentClassification.Should().Be("deferred_support_evidence");
        lawfulDeferredPerspectival.PerspectivalEngramSupport.SupportShape.Should().Be("root_constructor_support");
        lawfulDeferredPerspectival.PerspectivalEngramSupport.DeferReason.Should().NotBeNullOrWhiteSpace();

        lawfulParticipatory.ParticipatoryEngramSupport.Should().NotBeNull();
        lawfulParticipatory.ParticipatoryEngramSupport!.WorkingIntentState.Should().Be("reviewable_support");
        lawfulParticipatory.ParticipatoryEngramSupport.IntentClassification.Should().Be("reviewable_support_evidence");
        lawfulParticipatory.ParticipatoryEngramSupport.SupportShape.Should().Be("branch_set_support");
        lawfulParticipatory.ParticipatoryEngramSupport.Phase5HandoffReady.Should().BeTrue();

        lawfulRejectedParticipatory.ParticipatoryEngramSupport.Should().NotBeNull();
        lawfulRejectedParticipatory.ParticipatoryEngramSupport!.WorkingIntentState.Should().Be("rejected_support");
        lawfulRejectedParticipatory.ParticipatoryEngramSupport.IntentClassification.Should().Be("rejected_support_evidence");
        lawfulRejectedParticipatory.ParticipatoryEngramSupport.SupportShape.Should().Be("branch_set_support");
        lawfulRejectedParticipatory.ParticipatoryEngramSupport.RejectionReason.Should().NotBeNullOrWhiteSpace();

        invalidPerspectival.PerspectivalEngramSupport.Should().NotBeNull();
        invalidPerspectival.PerspectivalEngramSupport!.WorkingIntentState.Should().Be("reviewable_support");
        invalidPerspectival.PerspectivalEngramSupport.IntentClassification.Should().Be("reviewable_support_evidence");
        invalidPerspectival.PerspectivalEngramSupport.SupportShape.Should().Be("root_constructor_support");
        invalidPerspectival.PerspectivalEngramSupport.SupportOnly.Should().BeFalse();

        invalidParticipatory.ParticipatoryEngramSupport.Should().NotBeNull();
        invalidParticipatory.ParticipatoryEngramSupport!.WorkingIntentState.Should().Be("structured_intent");
        invalidParticipatory.ParticipatoryEngramSupport.IntentClassification.Should().Be("typed_support_claim");
        invalidParticipatory.ParticipatoryEngramSupport.SupportShape.Should().Be("branch_set_support");
        invalidParticipatory.ParticipatoryEngramSupport.ParticipantBranches.Should().ContainSingle();
    }
}
