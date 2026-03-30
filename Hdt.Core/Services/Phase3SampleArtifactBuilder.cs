using Hdt.Core.Models;
using Hdt.Core.Security;
using System.Text;

namespace Hdt.Core.Services;

public sealed class Phase3SampleArtifactBuilder
{
    private enum Phase3SampleVariant
    {
        Valid,
        ComparisonPeerDelayed,
        ComparisonPeerDivergent,
        IncompatiblePrimaryHorizon,
        InvalidDerivedPhaseSlice
    }

    private readonly HopngArtifactBuilder _baseBuilder = new();
    private readonly ArtifactJsonStore _jsonStore = new();
    private readonly HopngArtifactLoader _loader = new();

    public LoadedHopngArtifact Create(NewHopngRequest request) =>
        Create(request, Phase3SampleVariant.Valid);

    public LoadedHopngArtifact CreateComparisonPeer(NewHopngRequest request) =>
        Create(request, Phase3SampleVariant.ComparisonPeerDelayed);

    public LoadedHopngArtifact CreateDivergentComparisonPeer(NewHopngRequest request) =>
        Create(request, Phase3SampleVariant.ComparisonPeerDivergent);

    public LoadedHopngArtifact CreateIncompatiblePrimaryHorizonSample(NewHopngRequest request) =>
        Create(request, Phase3SampleVariant.IncompatiblePrimaryHorizon);

    public LoadedHopngArtifact CreateInvalidDerivedPhaseSlice(NewHopngRequest request) =>
        Create(request, Phase3SampleVariant.InvalidDerivedPhaseSlice);

    private LoadedHopngArtifact Create(NewHopngRequest request, Phase3SampleVariant variant)
    {
        var artifact = _baseBuilder.Create(request);

        var universeLayerSet = new UniverseLayerSet
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            Universes =
            [
                new UniverseLayer
                {
                    UniverseId = "prime-projection",
                    Modality = "visual-symbolic",
                    NeutralPlane = 0,
                    ProjectionRole = "projection-surface",
                    CoordinateFrame = new CoordinateFrame
                    {
                        XAxis = "x",
                        YAxis = "y",
                        ZAxis = "z",
                        Units = "pixel-relative"
                    }
                },
                new UniverseLayer
                {
                    UniverseId = "cryptic-support",
                    Modality = "cryptic-support",
                    NeutralPlane = 0,
                    ProjectionRole = "projection-surface",
                    CoordinateFrame = new CoordinateFrame
                    {
                        XAxis = "x",
                        YAxis = "y",
                        ZAxis = "z",
                        Units = "relative-pressure"
                    }
                }
            ]
        };

        var gluingManifest = new GluingManifest
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            Relations =
            [
                new GluingRelation
                {
                    RelationId = "glue-1",
                    SourceUniverseId = "cryptic-support",
                    TargetUniverseId = "prime-projection",
                    RelationType = "projection-support",
                    RequiredForFormation = true
                }
            ]
        };

        var projectionRules = new ProjectionRules
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            Rules =
            [
                new ProjectionRule
                {
                    RuleId = "rule-1",
                    SourceUniverseId = "prime-projection",
                    TargetProjectionRole = "projection-surface",
                    MappingType = "direct",
                    Precedence = 0
                },
                new ProjectionRule
                {
                    RuleId = "rule-2",
                    SourceUniverseId = "cryptic-support",
                    TargetProjectionRole = "projection-surface",
                    MappingType = "modulated-overlay",
                    Precedence = 1
                }
            ]
        };

        var legibilityProfile = new LegibilityProfile
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            RequiredUniverses = ["prime-projection", "cryptic-support"],
            RequiredRelations = ["glue-1"],
            ProjectionIntegrityRequired = true
        };

        var eventSliceSet = new EventSliceSet
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            ObservedSet = new ObservedSetHeader
            {
                ObservedSetId = $"{artifact.Manifest.ArtifactId}-observed",
                ArtifactId = artifact.Manifest.ArtifactId,
                ObservedDurationMs = 40000,
                BaseSliceCadenceMs = 1000,
                RawSliceCount = 40,
                ObservedEventCount = 420,
                EventGroupingMode = "fixed_raw_count",
                EventGroupingSizeRawSlices = 10,
                PrimeSafeInspectionMode = "metadata_only",
                DataCustodyMode = "protected_external",
                ProtectedEvidenceRefs =
                [
                    EvidenceRef("observed-root", "custody://observed/root", "Observed-set custody root")
                ]
            },
            Slices =
            [
                EventSlice(artifact.Manifest.ArtifactId, 0, "event-0", 0, 9, 120, 0.20, 0.10, 0.40, 0.30, 0.20, 0.50),
                EventSlice(artifact.Manifest.ArtifactId, 1, "event-1", 10, 19, 90, 0.45, 0.30, 0.70, 0.10, 0.10, 0.40),
                EventSlice(artifact.Manifest.ArtifactId, 2, "event-2", 20, 29, 100, 0.50, 0.35, 0.90, 0.40, 0.30, 0.65),
                EventSlice(artifact.Manifest.ArtifactId, 3, "event-3", 30, 39, 110, 0.38, 0.15, 0.60, 0.55, 0.40, 0.80)
            ]
        };
        eventSliceSet = ApplyEventSliceVariant(eventSliceSet, variant);

        var phasePolicy = new PhasePolicy
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            RawCadenceMs = 1000,
            EventGroupingMode = "fixed_raw_count",
            EventGroupingSizeRawSlices = 10,
            PhaseWindowMode = "fixed_event_count",
            PhaseWindowSizeEventSlices = 2,
            PhaseWindowDurationMs = 20000,
            MaxPhaseWindowSpanMs = 20000,
            ComparisonHorizonRawSlices = 20,
            ComparisonHorizons =
            [
                new TemporalComparisonHorizonPolicy
                {
                    HorizonId = "adjacent-raw-10",
                    Mode = "raw_slices",
                    Value = 10,
                    UseForStateClassification = false
                },
                new TemporalComparisonHorizonPolicy
                {
                    HorizonId = "widened-duration-20000",
                    Mode = "duration_ms",
                    Value = 20000,
                    UseForStateClassification = true
                }
            ],
            AggregationPolicies = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["pressure"] = "mean",
                ["drift"] = "delta",
                ["bloom"] = "latest"
            },
            StateThresholds = new TemporalStateThresholdPolicy(),
            PrimeSafeInspectionMode = "metadata_only",
            PrivilegedInspectionMode = "full_payload"
        };
        phasePolicy = ApplyPhasePolicyVariant(phasePolicy, variant);

        var opticalChannels = new OpticalChannelsDefinition
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            RequiredChannels = ["pressure", "drift", "bloom"],
            ReservedChannels = ["force", "opacity", "hue", "saturation"],
            Channels =
            [
                Channel("pressure", true, false, "analytic-first", "local coherence strain / torsion tension near threshold or break"),
                Channel("drift", true, false, "analytic-first", "accumulated deviation from prior stable or expected trajectory"),
                Channel("bloom", true, false, "analytic-first", "coherent spread or intensification across neighboring slices"),
                Channel("force", false, true, "derived", "reserved or derived only"),
                Channel("opacity", false, true, "render-aid", "derived render aid only"),
                Channel("hue", false, false, "visual-reserved", "reserved visual channel only"),
                Channel("saturation", false, false, "visual-reserved", "reserved visual channel only")
            ]
        };

        _jsonStore.WriteCanonical(artifact.Layout.UniverseLayerPath, universeLayerSet);
        _jsonStore.WriteCanonical(artifact.Layout.GluingManifestPath, gluingManifest);
        _jsonStore.WriteCanonical(artifact.Layout.ProjectionRulesPath, projectionRules);
        _jsonStore.WriteCanonical(artifact.Layout.LegibilityProfilePath, legibilityProfile);
        _jsonStore.WriteCanonical(artifact.Layout.EventSlicePath, eventSliceSet);
        _jsonStore.WriteCanonical(artifact.Layout.PhasePolicyPath, phasePolicy);
        _jsonStore.WriteCanonical(artifact.Layout.OpticalChannelsPath, opticalChannels);

        var derivationArtifact = artifact with
        {
            UniverseLayerSet = universeLayerSet,
            GluingManifest = gluingManifest,
            ProjectionRules = projectionRules,
            LegibilityProfile = legibilityProfile,
            EventSliceSet = eventSliceSet,
            PhasePolicy = phasePolicy,
            OpticalChannelsDefinition = opticalChannels
        };

        var phaseSliceSet = new PhaseSliceSet
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            Slices = new TemporalPhaseStackService().DeriveExpectedPhaseSlices(derivationArtifact)
        };
        phaseSliceSet = ApplyVariant(phaseSliceSet, variant);
        _jsonStore.WriteCanonical(artifact.Layout.PhaseSlicePath, phaseSliceSet);

        var sidecars = artifact.Manifest.Sidecars.Concat(
        [
            Sidecar("universe-layer", "oan.hopng_universe_layer", artifact.Layout.UniverseLayerPath),
            Sidecar("gluing-manifest", "oan.hopng_gluing_manifest", artifact.Layout.GluingManifestPath),
            Sidecar("projection-rules", "oan.hopng_projection_rules", artifact.Layout.ProjectionRulesPath),
            Sidecar("legibility-profile", "oan.hopng_legibility_profile", artifact.Layout.LegibilityProfilePath),
            Sidecar("event-slices", "oan.hopng_event_slice", artifact.Layout.EventSlicePath),
            Sidecar("phase-slices", "oan.hopng_phase_slice", artifact.Layout.PhaseSlicePath),
            Sidecar("phase-policy", "oan.hopng_phase_policy", artifact.Layout.PhasePolicyPath),
            Sidecar("optical-channels", "oan.hopng_optical_channels", artifact.Layout.OpticalChannelsPath)
        ]).ToList();

        var fileDigests = artifact.Manifest.FileDigests.Concat(
        [
            FileDigest("universe-layer", artifact.Layout.UniverseLayerPath),
            FileDigest("gluing-manifest", artifact.Layout.GluingManifestPath),
            FileDigest("projection-rules", artifact.Layout.ProjectionRulesPath),
            FileDigest("legibility-profile", artifact.Layout.LegibilityProfilePath),
            FileDigest("event-slices", artifact.Layout.EventSlicePath),
            FileDigest("phase-slices", artifact.Layout.PhaseSlicePath),
            FileDigest("phase-policy", artifact.Layout.PhasePolicyPath),
            FileDigest("optical-channels", artifact.Layout.OpticalChannelsPath)
        ]).ToList();

        var manifest = artifact.Manifest with
        {
            Sidecars = sidecars,
            FileDigests = fileDigests
        };
        _jsonStore.WriteCanonical(artifact.Layout.ManifestPath, manifest);

        return RefreshIntegrity(artifact);
    }

    private static PhaseSliceSet ApplyVariant(PhaseSliceSet phaseSliceSet, Phase3SampleVariant variant) =>
        variant switch
        {
            Phase3SampleVariant.InvalidDerivedPhaseSlice => BuildInvalidDerivedPhaseSliceSet(phaseSliceSet),
            _ => phaseSliceSet
        };

    private static EventSliceSet ApplyEventSliceVariant(EventSliceSet eventSliceSet, Phase3SampleVariant variant) =>
        variant switch
        {
            Phase3SampleVariant.ComparisonPeerDelayed => BuildComparisonPeerEventSliceSet(eventSliceSet),
            Phase3SampleVariant.ComparisonPeerDivergent => BuildDivergentComparisonPeerEventSliceSet(eventSliceSet),
            _ => eventSliceSet
        };

    private static PhasePolicy ApplyPhasePolicyVariant(PhasePolicy phasePolicy, Phase3SampleVariant variant) =>
        variant switch
        {
            Phase3SampleVariant.IncompatiblePrimaryHorizon => BuildIncompatiblePrimaryHorizonPolicy(phasePolicy),
            _ => phasePolicy
        };

    private static EventSliceSet BuildComparisonPeerEventSliceSet(EventSliceSet eventSliceSet)
    {
        if (eventSliceSet.Slices.Count == 0)
        {
            return eventSliceSet;
        }

        var slices = eventSliceSet.Slices.ToList();
        var lastSlice = slices[^1];
        var universeStates = new Dictionary<string, TemporalUniverseState>(lastSlice.UniverseStates, StringComparer.Ordinal)
        {
            ["prime-projection"] = new TemporalUniverseState
            {
                Pressure = 0.42,
                Drift = 0.45,
                Bloom = 0.80
            },
            ["cryptic-support"] = new TemporalUniverseState
            {
                Pressure = 0.55,
                Drift = 0.50,
                Bloom = 0.85
            }
        };

        var peerSlice = lastSlice with
        {
            UniverseStates = universeStates
        };
        peerSlice = peerSlice with
        {
            SliceDigest = TemporalSliceDigestService.ComputeEventSliceDigest(peerSlice)
        };

        slices[^1] = peerSlice;
        return eventSliceSet with
        {
            Slices = slices
        };
    }

    private static EventSliceSet BuildDivergentComparisonPeerEventSliceSet(EventSliceSet eventSliceSet)
    {
        if (eventSliceSet.Slices.Count == 0)
        {
            return eventSliceSet;
        }

        var slices = eventSliceSet.Slices.ToList();
        var lastSlice = slices[^1];
        var universeStates = new Dictionary<string, TemporalUniverseState>(lastSlice.UniverseStates, StringComparer.Ordinal)
        {
            ["prime-projection"] = new TemporalUniverseState
            {
                Pressure = 0.62,
                Drift = 0.70,
                Bloom = 0.95
            },
            ["cryptic-support"] = new TemporalUniverseState
            {
                Pressure = 0.75,
                Drift = 0.65,
                Bloom = 0.95
            }
        };

        var peerSlice = lastSlice with
        {
            UniverseStates = universeStates
        };
        peerSlice = peerSlice with
        {
            SliceDigest = TemporalSliceDigestService.ComputeEventSliceDigest(peerSlice)
        };

        slices[^1] = peerSlice;
        return eventSliceSet with
        {
            Slices = slices
        };
    }

    private static PhasePolicy BuildIncompatiblePrimaryHorizonPolicy(PhasePolicy phasePolicy)
    {
        var horizons = phasePolicy.ComparisonHorizons
            .Select(horizon => horizon.HorizonId switch
            {
                "adjacent-raw-10" => horizon with
                {
                    UseForStateClassification = true
                },
                "widened-duration-20000" => horizon with
                {
                    UseForStateClassification = false
                },
                _ => horizon
            })
            .ToList();

        return phasePolicy with
        {
            ComparisonHorizonRawSlices = 10,
            ComparisonHorizons = horizons
        };
    }

    private static PhaseSliceSet BuildInvalidDerivedPhaseSliceSet(PhaseSliceSet phaseSliceSet)
    {
        if (phaseSliceSet.Slices.Count == 0)
        {
            return phaseSliceSet;
        }

        var slices = phaseSliceSet.Slices.ToList();
        var lastSlice = slices[^1];
        if (!lastSlice.UniverseStates.TryGetValue("prime-projection", out var primeState))
        {
            return phaseSliceSet;
        }

        var tamperedUniverseStates = new Dictionary<string, TemporalUniverseState>(lastSlice.UniverseStates, StringComparer.Ordinal)
        {
            ["prime-projection"] = primeState with
            {
                Pressure = primeState.Pressure + 0.125
            }
        };

        var tamperedSlice = lastSlice with
        {
            UniverseStates = tamperedUniverseStates
        };
        tamperedSlice = tamperedSlice with
        {
            SliceDigest = TemporalSliceDigestService.ComputePhaseSliceDigest(tamperedSlice)
        };

        slices[^1] = tamperedSlice;
        return phaseSliceSet with
        {
            Slices = slices
        };
    }

    private LoadedHopngArtifact RefreshIntegrity(LoadedHopngArtifact artifact)
    {
        var current = _loader.Load(artifact.Layout.ManifestPath);
        var refreshedDigests = current.Manifest.FileDigests
            .Select(digest => digest with
            {
                Sha256 = ArtifactHashing.ComputeSha256(Path.Combine(current.Layout.DirectoryPath, digest.Path))
            })
            .ToList();
        var refreshedManifest = current.Manifest with
        {
            FileDigests = refreshedDigests
        };

        _jsonStore.WriteCanonical(current.Layout.ManifestPath, refreshedManifest);
        current = _loader.Load(current.Layout.ManifestPath);

        var manifestCanonicalSha256 = ArtifactHashing.ComputeSha256(File.ReadAllBytes(current.Layout.ManifestPath));
        var hashSidecar = current.HashSidecar with
        {
            ManifestCanonicalSha256 = manifestCanonicalSha256,
            ArtifactSetSha256 = ArtifactHashing.ComputeArtifactSetSha256(refreshedDigests, manifestCanonicalSha256),
            FileDigests = refreshedDigests
        };
        _jsonStore.WriteCanonical(current.Layout.HashPath, hashSidecar);

        var signatureService = new Ed25519SignatureService();
        var privateKey = File.ReadAllText(current.Layout.PrivateKeyPath).Trim();
        var hashBytes = File.ReadAllBytes(current.Layout.HashPath);
        var signature = signatureService.Sign(privateKey, hashBytes);
        var signatureSidecar = current.SignatureSidecar with
        {
            SignedObjectSha256 = ArtifactHashing.ComputeSha256(hashBytes),
            SignatureBase64 = Convert.ToBase64String(signature)
        };
        _jsonStore.WriteCanonical(current.Layout.SignaturePath, signatureSidecar);

        return _loader.Load(current.Layout.ManifestPath);
    }

    private static EventSlice EventSlice(
        string artifactId,
        int n,
        string eventSliceId,
        int rawStartN,
        int rawEndN,
        int observedEventCount,
        double primePressure,
        double primeDrift,
        double primeBloom,
        double crypticPressure,
        double crypticDrift,
        double crypticBloom)
    {
        var slice = new EventSlice
        {
            ArtifactId = artifactId,
            EventSliceId = eventSliceId,
            N = n,
            TimestampStartUtc = DateTimeOffset.Parse("2026-03-07T00:00:00Z").AddSeconds(rawStartN),
            TimestampEndUtc = DateTimeOffset.Parse("2026-03-07T00:00:00Z").AddSeconds(rawEndN + 1),
            RawStartN = rawStartN,
            RawEndN = rawEndN,
            RawSliceSpan = rawEndN - rawStartN + 1,
            ObservedEventCount = observedEventCount,
            ProtectedEvidenceRefs =
            [
                EvidenceRef(eventSliceId, $"custody://observed/{eventSliceId}", $"Protected evidence for {eventSliceId}")
            ],
            UniverseStates = new Dictionary<string, TemporalUniverseState>(StringComparer.Ordinal)
            {
                ["prime-projection"] = new()
                {
                    Pressure = primePressure,
                    Drift = primeDrift,
                    Bloom = primeBloom
                },
                ["cryptic-support"] = new()
                {
                    Pressure = crypticPressure,
                    Drift = crypticDrift,
                    Bloom = crypticBloom
                }
            }
        };

        return slice with
        {
            SliceDigest = TemporalSliceDigestService.ComputeEventSliceDigest(slice)
        };
    }

    private static ProtectedEvidenceReference EvidenceRef(string refId, string pointerUri, string summary) =>
        new()
        {
            RefId = refId,
            PointerUri = pointerUri,
            DigestSha256 = ArtifactHashing.ComputeSha256(Encoding.UTF8.GetBytes(summary)),
            Summary = summary
        };

    private static OpticalChannelDefinition Channel(string channelId, bool required, bool derivedOnly, string usageMode, string canonicalMeaning) =>
        new()
        {
            ChannelId = channelId,
            Required = required,
            DerivedOnly = derivedOnly,
            UsageMode = usageMode,
            CanonicalMeaning = canonicalMeaning
        };

    private static SidecarReference Sidecar(string role, string schema, string path) =>
        new()
        {
            Role = role,
            Schema = schema,
            SchemaVersion = "0.1.0",
            Path = Path.GetFileName(path),
            Required = true
        };

    private static ArtifactFileDigest FileDigest(string role, string path) =>
        new()
        {
            Role = role,
            Path = Path.GetFileName(path),
            Sha256 = ArtifactHashing.ComputeSha256(path)
        };
}
