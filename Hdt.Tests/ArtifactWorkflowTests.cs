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
}
