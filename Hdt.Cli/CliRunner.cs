using System.Text.Json;
using Hdt.Core;
using Hdt.Core.Models;
using Hdt.Core.Services;
using Hdt.Core.Validation;

namespace Hdt.Cli;

public sealed class CliRunner
{
    private readonly TextWriter _writer;
    private readonly HopngArtifactBuilder _builder = new();
    private readonly HopngArtifactValidator _validator = new();
    private readonly HopngArtifactLoader _loader = new();
    private readonly HopngArtifactInspector _inspector = new();
    private readonly Phase3SampleArtifactBuilder _phase3SampleBuilder = new();
    private readonly Phase4SampleArtifactBuilder _phase4SampleBuilder = new();
    private readonly GovernedProjectionDerivationService _projectionDerivationService = new();
    private readonly ProjectionSupportComparisonService _projectionComparisonService = new();
    private readonly EngramSupportComparisonService _engramSupportComparisonService = new();
    private readonly TemporalPhaseStackService _temporalPhaseStackService = new();
    private readonly TemporalPhaseStackComparisonService _temporalPhaseStackComparisonService = new();

    public CliRunner(TextWriter writer)
    {
        _writer = writer;
    }

    public int Execute(string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            WriteUsage();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var options = CliOptions.Parse(args.Skip(1).ToArray());

        try
        {
            return command switch
            {
                "new" => CreateArtifact(options),
                "new-phase3-sample" => CreatePhase3Sample(options),
                "new-phase3-peer-sample" => CreatePhase3PeerSample(options),
                "new-phase3-divergent-peer-sample" => CreatePhase3DivergentPeerSample(options),
                "new-phase3-incompatible-basis-sample" => CreatePhase3IncompatibleBasisSample(options),
                "new-phase3-invalid-sample" => CreateInvalidPhase3Sample(options),
                "new-phase4-perspectival-sample" => CreatePhase4PerspectivalSample(options),
                "new-phase4-perspectival-peer-sample" => CreatePhase4PerspectivalPeerSample(options),
                "new-phase4-restricted-perspectival-sample" => CreateRestrictedPhase4PerspectivalSample(options),
                "new-phase4-deferred-perspectival-sample" => CreateDeferredPhase4PerspectivalSample(options),
                "new-phase4-invalid-perspectival-sample" => CreateInvalidPhase4PerspectivalSample(options),
                "new-phase4-participatory-sample" => CreatePhase4ParticipatorySample(options),
                "new-phase4-participatory-peer-sample" => CreatePhase4ParticipatoryPeerSample(options),
                "new-phase4-rejected-participatory-sample" => CreateRejectedPhase4ParticipatorySample(options),
                "new-phase4-invalid-participatory-sample" => CreateInvalidPhase4ParticipatorySample(options),
                "validate" => ValidateArtifact(options),
                "show" => ShowArtifact(options),
                "merge-layers" => MergeLayers(options),
                "render-phase-stack" => RenderPhaseStack(options),
                "compare-surfaces" => CompareSurfaces(options),
                "compare-phase-stacks" => ComparePhaseStacks(options),
                "compare-engram-support" => CompareEngramSupport(options),
                "invoke-formation" => Reserved("Invoke-HOPNGFormation", 5),
                "bind-oe" => Reserved("Bind-HOPNGToOE", 6),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Write(new { error = ex.Message }, options.Json);
            return 1;
        }
    }

    private int CreateArtifact(CliOptions options)
    {
        var request = new NewHopngRequest(
            options.Require("output-dir"),
            options.Require("name"),
            options.Get("signer", Environment.UserName),
            options.Get("key-id", "local-dev-key"),
            options.Get("display-name"),
            options.Get("artifact-id"),
            options.Get("private-key"),
            options.Get("private-key-out"),
            options.Get("public-key-out"));

        var artifact = _builder.Create(request);
        Write(new
        {
            artifactId = artifact.Manifest.ArtifactId,
            manifest = artifact.Layout.ManifestPath,
            projection = artifact.Layout.ProjectionPath,
            signature = artifact.Layout.SignaturePath
        }, options.Json);

        return 0;
    }

    private int CreatePhase3Sample(CliOptions options)
    {
        var request = new NewHopngRequest(
            options.Require("output-dir"),
            options.Require("name"),
            options.Get("signer", Environment.UserName),
            options.Get("key-id", "local-dev-key"),
            options.Get("display-name"),
            options.Get("artifact-id"),
            options.Get("private-key"),
            options.Get("private-key-out"),
            options.Get("public-key-out"));

        var artifact = _phase3SampleBuilder.Create(request);
        Write(new
        {
            artifactId = artifact.Manifest.ArtifactId,
            manifest = artifact.Layout.ManifestPath,
            projection = artifact.Layout.ProjectionPath,
            signature = artifact.Layout.SignaturePath,
            eventSlices = artifact.Layout.EventSlicePath,
            phaseSlices = artifact.Layout.PhaseSlicePath
        }, options.Json);

        return 0;
    }

    private int CreateInvalidPhase3Sample(CliOptions options)
    {
        var request = new NewHopngRequest(
            options.Require("output-dir"),
            options.Require("name"),
            options.Get("signer", Environment.UserName),
            options.Get("key-id", "local-dev-key"),
            options.Get("display-name"),
            options.Get("artifact-id"),
            options.Get("private-key"),
            options.Get("private-key-out"),
            options.Get("public-key-out"));

        var artifact = _phase3SampleBuilder.CreateInvalidDerivedPhaseSlice(request);
        Write(new
        {
            artifactId = artifact.Manifest.ArtifactId,
            manifest = artifact.Layout.ManifestPath,
            projection = artifact.Layout.ProjectionPath,
            signature = artifact.Layout.SignaturePath,
            eventSlices = artifact.Layout.EventSlicePath,
            phaseSlices = artifact.Layout.PhaseSlicePath
        }, options.Json);

        return 0;
    }

    private int CreatePhase3PeerSample(CliOptions options)
    {
        var request = new NewHopngRequest(
            options.Require("output-dir"),
            options.Require("name"),
            options.Get("signer", Environment.UserName),
            options.Get("key-id", "local-dev-key"),
            options.Get("display-name"),
            options.Get("artifact-id"),
            options.Get("private-key"),
            options.Get("private-key-out"),
            options.Get("public-key-out"));

        var artifact = _phase3SampleBuilder.CreateComparisonPeer(request);
        Write(new
        {
            artifactId = artifact.Manifest.ArtifactId,
            manifest = artifact.Layout.ManifestPath,
            projection = artifact.Layout.ProjectionPath,
            signature = artifact.Layout.SignaturePath,
            eventSlices = artifact.Layout.EventSlicePath,
            phaseSlices = artifact.Layout.PhaseSlicePath
        }, options.Json);

        return 0;
    }

    private int CreatePhase3IncompatibleBasisSample(CliOptions options)
    {
        var request = new NewHopngRequest(
            options.Require("output-dir"),
            options.Require("name"),
            options.Get("signer", Environment.UserName),
            options.Get("key-id", "local-dev-key"),
            options.Get("display-name"),
            options.Get("artifact-id"),
            options.Get("private-key"),
            options.Get("private-key-out"),
            options.Get("public-key-out"));

        var artifact = _phase3SampleBuilder.CreateIncompatiblePrimaryHorizonSample(request);
        Write(new
        {
            artifactId = artifact.Manifest.ArtifactId,
            manifest = artifact.Layout.ManifestPath,
            projection = artifact.Layout.ProjectionPath,
            signature = artifact.Layout.SignaturePath,
            eventSlices = artifact.Layout.EventSlicePath,
            phaseSlices = artifact.Layout.PhaseSlicePath
        }, options.Json);

        return 0;
    }

    private int CreatePhase3DivergentPeerSample(CliOptions options)
    {
        var request = new NewHopngRequest(
            options.Require("output-dir"),
            options.Require("name"),
            options.Get("signer", Environment.UserName),
            options.Get("key-id", "local-dev-key"),
            options.Get("display-name"),
            options.Get("artifact-id"),
            options.Get("private-key"),
            options.Get("private-key-out"),
            options.Get("public-key-out"));

        var artifact = _phase3SampleBuilder.CreateDivergentComparisonPeer(request);
        Write(new
        {
            artifactId = artifact.Manifest.ArtifactId,
            manifest = artifact.Layout.ManifestPath,
            projection = artifact.Layout.ProjectionPath,
            signature = artifact.Layout.SignaturePath,
            eventSlices = artifact.Layout.EventSlicePath,
            phaseSlices = artifact.Layout.PhaseSlicePath
        }, options.Json);

        return 0;
    }

    private int CreatePhase4PerspectivalSample(CliOptions options)
    {
        var artifact = _phase4SampleBuilder.CreatePerspectivalSupportSample(BuildArtifactRequest(options));
        WritePhase4CreateResult(artifact, options.Json);
        return 0;
    }

    private int CreateInvalidPhase4PerspectivalSample(CliOptions options)
    {
        var artifact = _phase4SampleBuilder.CreateInvalidPerspectivalSupportSample(BuildArtifactRequest(options));
        WritePhase4CreateResult(artifact, options.Json);
        return 0;
    }

    private int CreatePhase4PerspectivalPeerSample(CliOptions options)
    {
        var artifact = _phase4SampleBuilder.CreatePerspectivalSupportPeerSample(BuildArtifactRequest(options));
        WritePhase4CreateResult(artifact, options.Json);
        return 0;
    }

    private int CreateRestrictedPhase4PerspectivalSample(CliOptions options)
    {
        var artifact = _phase4SampleBuilder.CreateRestrictedPerspectivalSupportSample(BuildArtifactRequest(options));
        WritePhase4CreateResult(artifact, options.Json);
        return 0;
    }

    private int CreateDeferredPhase4PerspectivalSample(CliOptions options)
    {
        var artifact = _phase4SampleBuilder.CreateDeferredPerspectivalSupportSample(BuildArtifactRequest(options));
        WritePhase4CreateResult(artifact, options.Json);
        return 0;
    }

    private int CreatePhase4ParticipatorySample(CliOptions options)
    {
        var artifact = _phase4SampleBuilder.CreateParticipatorySupportSample(BuildArtifactRequest(options));
        WritePhase4CreateResult(artifact, options.Json);
        return 0;
    }

    private int CreatePhase4ParticipatoryPeerSample(CliOptions options)
    {
        var artifact = _phase4SampleBuilder.CreateParticipatorySupportPeerSample(BuildArtifactRequest(options));
        WritePhase4CreateResult(artifact, options.Json);
        return 0;
    }

    private int CreateRejectedPhase4ParticipatorySample(CliOptions options)
    {
        var artifact = _phase4SampleBuilder.CreateRejectedParticipatorySupportSample(BuildArtifactRequest(options));
        WritePhase4CreateResult(artifact, options.Json);
        return 0;
    }

    private int CreateInvalidPhase4ParticipatorySample(CliOptions options)
    {
        var artifact = _phase4SampleBuilder.CreateInvalidParticipatorySupportSample(BuildArtifactRequest(options));
        WritePhase4CreateResult(artifact, options.Json);
        return 0;
    }

    private int ValidateArtifact(CliOptions options)
    {
        var result = _validator.Validate(options.Require("path"));
        Write(new { isValid = result.IsValid, errors = result.Errors }, options.Json);
        return result.IsValid ? 0 : (int)result.Errors[0].Code;
    }

    private int ShowArtifact(CliOptions options)
    {
        var path = options.Require("path");
        var view = options.Get("view", "prime");
        var artifact = _loader.Load(path);
        var validation = _validator.Validate(path);
        var payload = string.Equals(view, "privileged", StringComparison.OrdinalIgnoreCase)
            ? _inspector.BuildPrivilegedView(artifact, validation)
            : _inspector.BuildPrimeSafeView(artifact, validation);

        Write(payload, options.Json);
        return validation.IsValid ? 0 : (int)validation.Errors[0].Code;
    }

    private int MergeLayers(CliOptions options)
    {
        var result = _projectionDerivationService.Derive(options.Require("path"));
        Write(new
        {
            artifactId = result.ArtifactId,
            status = result.Status.ToString(),
            isLawfullyFormed = result.IsLawfullyFormed,
            legibilitySatisfied = result.LegibilitySatisfied,
            projectionIntegritySatisfied = result.ProjectionIntegritySatisfied,
            participatingUniverses = result.ParticipatingUniverses,
            participatingRelations = result.ParticipatingRelations,
            ruleTrace = result.RuleTrace,
            issues = result.Issues,
            validationErrors = result.ValidationIssues
        }, options.Json);

        return result.Status switch
        {
            Hdt.Core.Models.ProjectionFormationStatus.LawfullyFormed => 0,
            Hdt.Core.Models.ProjectionFormationStatus.StructurallyIncomplete => 24,
            _ => 25
        };
    }

    private int CompareSurfaces(CliOptions options)
    {
        var result = _projectionComparisonService.Compare(
            options.Require("left"),
            options.Require("right"));

        Write(new
        {
            leftArtifactId = result.LeftArtifactId,
            rightArtifactId = result.RightArtifactId,
            leftStatus = result.LeftStatus.ToString(),
            rightStatus = result.RightStatus.ToString(),
            classification = result.Classification,
            leftIssues = result.LeftIssues,
            rightIssues = result.RightIssues,
            signals = result.Signals
        }, options.Json);

        return result.Classification switch
        {
            "equivalent-lawful-support" => 0,
            "equivalent-incomplete-support" or "formed-vs-incomplete" => 24,
            _ => 25
        };
    }

    private int RenderPhaseStack(CliOptions options)
    {
        var view = options.Get("view", "prime");
        var rawSliceHorizon = options.Get("h") is { } horizonText
            ? int.Parse(horizonText)
            : (int?)null;
        var path = options.Require("path");
        var artifact = _loader.Load(path);
        var validation = _validator.Validate(path);
        var result = _temporalPhaseStackService.Render(artifact, validation, view, rawSliceHorizon);

        if (options.Json)
        {
            Write(result, true);
        }
        else
        {
            _writer.WriteLine($"Temporal stack status: {result.Status}");
            _writer.WriteLine($"Observed duration: {result.ObservedDurationMs} ms");
            _writer.WriteLine($"Base cadence: {result.BaseRawCadenceMs} ms");
            _writer.WriteLine($"Raw slices: {result.RawSliceCount}");
            _writer.WriteLine($"Observed events: {result.ObservedEventCount}");
            _writer.WriteLine($"Event slices: {result.EventSliceCount}");
            _writer.WriteLine($"Phase slices: {result.PhaseSliceCount}");
            _writer.WriteLine($"Grouping: {result.GroupingSummary}");
            _writer.WriteLine($"Primary horizon: {result.PrimaryHorizonId}");
            _writer.WriteLine($"Horizon: {result.HorizonRawSlices} raw slices / {result.HorizonDurationMs} ms");
            _writer.WriteLine($"Declared horizon summaries: {result.HorizonSummaries.Count}");
            _writer.WriteLine($"Required channels covered: {result.RequiredChannelCoverage}");
            _writer.WriteLine($"Payload mode: {result.PayloadMode}");
            _writer.WriteLine($"Drift flags: {result.DriftFlags.Count}");
            _writer.WriteLine($"Topology flags: {result.TopologyChangeFlags.Count}");

            if (result.StateSummaries.Count > 0)
            {
                var finalState = result.StateSummaries[^1];
                _writer.WriteLine($"Final state: {finalState.StateClass} on {finalState.SliceId}");
                _writer.WriteLine($"Final anchor: {FormatOptional(finalState.AnchorSliceId)}");
                _writer.WriteLine($"Final derived force: {finalState.DerivedForceMagnitude:0.000000} ({finalState.DerivedForceDirection})");
                WriteSignalSection("Final basis signals", finalState.BasisSignals);
            }

            WriteHorizonSummarySection(result.HorizonSummaries);
            WriteSignalSection("Issues", result.Issues);
            WriteValidationIssueSection("Validation issues", result.ValidationIssues);
        }

        return result.Status switch
        {
            Hdt.Core.Models.TemporalStackStatus.LawfullyDerived => 0,
            Hdt.Core.Models.TemporalStackStatus.StructurallyIncomplete => 24,
            _ => 25
        };
    }

    private int ComparePhaseStacks(CliOptions options)
    {
        var view = options.Get("view", "prime");
        var rawSliceHorizon = options.Get("h") is { } horizonText
            ? int.Parse(horizonText)
            : (int?)null;
        var result = _temporalPhaseStackComparisonService.Compare(
            options.Require("left"),
            options.Require("right"),
            view,
            rawSliceHorizon);

        if (options.Json)
        {
            Write(result, true);
        }
        else
        {
            _writer.WriteLine($"Temporal comparison classification: {result.Classification}");
            _writer.WriteLine($"Basis alignment: {result.BasisAlignmentStatus}");
            _writer.WriteLine($"State compatibility: {result.TemporalStateCompatibility}");
            if (string.Equals(result.BasisAlignmentStatus, "Aligned", StringComparison.Ordinal))
            {
                _writer.WriteLine($"Primary horizon: {result.PrimaryHorizonDurationMs} ms / {result.PrimaryHorizonRawSlices} raw slices");
            }
            else
            {
                _writer.WriteLine($"Primary horizons: left {result.LeftPrimaryHorizonDurationMs} ms / {result.LeftPrimaryHorizonRawSlices} raw slices vs right {result.RightPrimaryHorizonDurationMs} ms / {result.RightPrimaryHorizonRawSlices} raw slices");
            }
            _writer.WriteLine($"Final states: {result.LeftFinalStateClass} ({result.LeftFinalStateDirection}) vs {result.RightFinalStateClass} ({result.RightFinalStateDirection})");
            _writer.WriteLine($"State rank delta: {FormatStateRankDelta(result.StateRankDelta)}");
            _writer.WriteLine($"Comparable phase slices: {result.ComparablePhaseSliceCount}");
            _writer.WriteLine($"Drift delta magnitude: {result.DriftDeltaMagnitude:0.000000}");
            _writer.WriteLine($"Derived force delta magnitude: {result.DerivedForceDeltaMagnitude:0.000000}");
            _writer.WriteLine($"Topology delta count: {result.TopologyDeltaCount}");
            _writer.WriteLine($"Similarity score: {result.SimilarityScore:0.000000}");
            _writer.WriteLine($"Classification reason: {result.ClassificationReason}");
            _writer.WriteLine($"Payload mode: {result.PayloadMode}");
            WriteSignalSection("Basis signals", result.BasisSignals);
            WriteSignalSection("Signals", result.Signals);
            WriteSignalSection("Left issues", result.LeftIssues);
            WriteSignalSection("Right issues", result.RightIssues);
            WriteValidationIssueSection("Left validation issues", result.LeftValidationIssues);
            WriteValidationIssueSection("Right validation issues", result.RightValidationIssues);
        }

        return result.Classification switch
        {
            "Convergent" or "Delayed" or "Divergent" => 0,
            "Incompatible" => 24,
            _ => 25
        };
    }

    private int CompareEngramSupport(CliOptions options)
    {
        var view = options.Get("view", "prime");
        var result = _engramSupportComparisonService.Compare(
            options.Require("left"),
            options.Require("right"),
            view);

        if (options.Json)
        {
            Write(result, true);
        }
        else
        {
            _writer.WriteLine($"Engram support comparison classification: {result.Classification}");
            _writer.WriteLine($"Support type compatibility: {result.SupportTypeCompatibility}");
            _writer.WriteLine($"Support shapes: {result.LeftSupportShape} vs {result.RightSupportShape}");
            _writer.WriteLine($"Support identity compatibility: {result.SupportIdentityCompatibility}");
            _writer.WriteLine($"Counterfeit pressure: {result.CounterfeitPressureStatus}");
            _writer.WriteLine($"Working-intent states: {result.LeftWorkingIntentState} vs {result.RightWorkingIntentState}");
            _writer.WriteLine($"Intent classifications: {result.LeftIntentClassification} vs {result.RightIntentClassification}");
            _writer.WriteLine($"Working-intent transition: {result.WorkingIntentTransitionStatus}");
            _writer.WriteLine($"Working-intent rank delta: {FormatStateRankDelta(result.WorkingIntentRankDelta)}");
            _writer.WriteLine($"Support identifiers: {FormatOptional(result.LeftSupportIdentifier)} vs {FormatOptional(result.RightSupportIdentifier)}");
            _writer.WriteLine($"Stability classes: {result.LeftStabilityClass} vs {result.RightStabilityClass}");
            _writer.WriteLine($"Constraint energy delta: {result.ConstraintEnergyDelta:0.000000}");
            _writer.WriteLine($"Burden preservation: {result.LeftBurdenPreservationScore:0.000000} vs {result.RightBurdenPreservationScore:0.000000}");
            _writer.WriteLine($"Shared support signals: {result.SharedSupportSignalCount}");
            _writer.WriteLine($"Shared validation questions: {result.SharedValidationQuestionCount}");
            _writer.WriteLine($"Similarity score: {result.SimilarityScore:0.000000}");
            _writer.WriteLine($"Classification reason: {result.ClassificationReason}");
            _writer.WriteLine($"Payload mode: {result.PayloadMode}");
            WriteSignalSection("Signals", result.Signals);
            WriteSignalSection("Left issues", result.LeftIssues);
            WriteSignalSection("Right issues", result.RightIssues);
            WriteValidationIssueSection("Left validation issues", result.LeftValidationIssues);
            WriteValidationIssueSection("Right validation issues", result.RightValidationIssues);
        }

        return result.Classification switch
        {
            "CoherentSupport" or "StrengthenedSupport" or "DivergentSupport" or "RestrictedSupport" or "DeferredSupport" or "RejectedSupport" => 0,
            "IncompatibleSupportType" => 24,
            _ => 25
        };
    }

    private int Reserved(string commandName, int phase)
    {
        _writer.WriteLine(ReservedPhaseCommand.BuildMessage(commandName, phase));
        return 21;
    }

    private int UnknownCommand(string command)
    {
        _writer.WriteLine($"Unknown command '{command}'.");
        WriteUsage();
        return 1;
    }

    private void WriteUsage()
    {
        _writer.WriteLine("Hdt.Cli commands:");
        _writer.WriteLine("  new --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase3-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase3-peer-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase3-divergent-peer-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase3-incompatible-basis-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase3-invalid-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase4-perspectival-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase4-perspectival-peer-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase4-restricted-perspectival-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase4-deferred-perspectival-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase4-invalid-perspectival-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase4-participatory-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase4-participatory-peer-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase4-rejected-participatory-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  new-phase4-invalid-participatory-sample --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  validate --path <manifest-or-png> [--json]");
        _writer.WriteLine("  show --path <manifest-or-png> [--view prime|privileged] [--json]");
        _writer.WriteLine("  merge-layers --path <manifest-or-png> [--json]");
        _writer.WriteLine("  render-phase-stack --path <manifest-or-png> [--view prime|privileged] [--h <raw-slice-horizon>] [--json]");
        _writer.WriteLine("  compare-phase-stacks --left <manifest-or-png> --right <manifest-or-png> [--view prime|privileged] [--h <raw-slice-horizon>] [--json]");
        _writer.WriteLine("  compare-engram-support --left <manifest-or-png> --right <manifest-or-png> [--view prime|privileged] [--json]");
        _writer.WriteLine("  compare-surfaces --left <manifest-or-png> --right <manifest-or-png> [--json]");
        _writer.WriteLine("  invoke-formation | bind-oe");
        _writer.WriteLine("Exit codes:");
        _writer.WriteLine("  0  lawful success, valid temporal render, or aligned comparison result");
        _writer.WriteLine("  21 reserved later-phase command invoked");
        _writer.WriteLine("  24 temporal derivation incomplete, basis-incompatible comparison, or support-type-incompatible engram comparison");
        _writer.WriteLine("  25 flattened, unsupported, counterfeit, or invalid derivation or comparison surface");
        _writer.WriteLine("  10-36 validation failures returned from the core validator");
    }

    private void WriteHorizonSummarySection(IReadOnlyList<TemporalHorizonSummary> horizonSummaries)
    {
        _writer.WriteLine($"Horizon summaries: {horizonSummaries.Count}");
        foreach (var horizon in horizonSummaries)
        {
            var primaryMarker = horizon.UseForStateClassification ? " [primary]" : string.Empty;
            _writer.WriteLine(
                $"  - {horizon.HorizonId}{primaryMarker}: {horizon.HorizonDurationMs} ms / {horizon.HorizonRawSlices} raw slices, comparable={horizon.ComparableSliceCount}, missing anchors={horizon.MissingAnchorSliceIds.Count}, drift flags={horizon.DriftFlags.Count}, topology flags={horizon.TopologyFlags.Count}");
        }
    }

    private void WriteSignalSection(string heading, IReadOnlyCollection<string> signals)
    {
        _writer.WriteLine($"{heading}: {signals.Count}");
        foreach (var signal in signals.Take(3))
        {
            _writer.WriteLine($"  - {signal}");
        }

        if (signals.Count > 3)
        {
            _writer.WriteLine($"  - ... {signals.Count - 3} more");
        }
    }

    private void WriteValidationIssueSection(string heading, IReadOnlyCollection<ValidationIssue> issues)
    {
        _writer.WriteLine($"{heading}: {issues.Count}");
        foreach (var issue in issues.Take(3))
        {
            _writer.WriteLine($"  - {issue.Code}: {issue.Message}");
        }

        if (issues.Count > 3)
        {
            _writer.WriteLine($"  - ... {issues.Count - 3} more");
        }
    }

    private static string FormatOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "(none)" : value;

    private static string FormatStateRankDelta(int? stateRankDelta) =>
        stateRankDelta.HasValue
            ? stateRankDelta.Value.ToString("+0;-0;0")
            : "(unavailable)";

    private void Write(object value, bool asJson)
    {
        if (asJson)
        {
            _writer.WriteLine(JsonSerializer.Serialize(value, JsonDefaults.SerializerOptions));
            return;
        }

        if (value is string text)
        {
            _writer.WriteLine(text);
            return;
        }

        _writer.WriteLine(JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonDefaults.SerializerOptions)
        {
            WriteIndented = true
        }));
    }

    private static NewHopngRequest BuildArtifactRequest(CliOptions options) =>
        new(
            options.Require("output-dir"),
            options.Require("name"),
            options.Get("signer", Environment.UserName),
            options.Get("key-id", "local-dev-key"),
            options.Get("display-name"),
            options.Get("artifact-id"),
            options.Get("private-key"),
            options.Get("private-key-out"),
            options.Get("public-key-out"));

    private void WritePhase4CreateResult(LoadedHopngArtifact artifact, bool asJson)
    {
        Write(new
        {
            artifactId = artifact.Manifest.ArtifactId,
            manifest = artifact.Layout.ManifestPath,
            projection = artifact.Layout.ProjectionPath,
            signature = artifact.Layout.SignaturePath,
            perspectivalEngram = File.Exists(artifact.Layout.PerspectivalEngramPath)
                ? artifact.Layout.PerspectivalEngramPath
                : null,
            participatoryEngram = File.Exists(artifact.Layout.ParticipatoryEngramPath)
                ? artifact.Layout.ParticipatoryEngramPath
                : null
        }, asJson);
    }
}

public sealed class CliOptions
{
    private readonly Dictionary<string, string?> _values;

    private CliOptions(Dictionary<string, string?> values)
    {
        _values = values;
    }

    public bool Json => _values.ContainsKey("json");

    public string Require(string key) =>
        _values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Missing required option '--{key}'.");

    public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

    public string Get(string key, string fallback) => Get(key) ?? fallback;

    public static CliOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = token[2..];
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[key] = args[index + 1];
                index++;
            }
            else
            {
                values[key] = "true";
            }
        }

        return new CliOptions(values);
    }
}
