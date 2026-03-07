using System.Text;
using Hdt.Core.Models;
using Hdt.Core.Security;
using Hdt.Core.Services;

namespace Hdt.Tests.TestSupport;

public static class Phase3ArtifactFactory
{
    public static LoadedHopngArtifact CreateValid(string tempDir, string name)
    {
        var artifact = Phase2ArtifactFactory.CreateValid(tempDir, name);
        var jsonStore = new ArtifactJsonStore();

        var eventSliceSet = new EventSliceSet
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            ObservedSet = new ObservedSetHeader
            {
                ObservedSetId = $"{artifact.Manifest.ArtifactId}-observed",
                ArtifactId = artifact.Manifest.ArtifactId,
                ObservedDurationMs = 30000,
                BaseSliceCadenceMs = 1000,
                RawSliceCount = 30,
                ObservedEventCount = 300,
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
                EventSlice(artifact.Manifest.ArtifactId, 2, "event-2", 20, 29, 90, 0.50, 0.35, 0.90, 0.40, 0.30, 0.65)
            ]
        };

        var phasePolicy = new PhasePolicy
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            RawCadenceMs = 1000,
            EventGroupingMode = "fixed_raw_count",
            EventGroupingSizeRawSlices = 10,
            PhaseWindowMode = "fixed_event_count",
            PhaseWindowSizeEventSlices = 2,
            ComparisonHorizonRawSlices = 10,
            AggregationPolicies = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["pressure"] = "mean",
                ["drift"] = "delta",
                ["bloom"] = "latest"
            },
            PrimeSafeInspectionMode = "metadata_only",
            PrivilegedInspectionMode = "full_payload"
        };

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

        jsonStore.WriteCanonical(artifact.Layout.EventSlicePath, eventSliceSet);
        jsonStore.WriteCanonical(artifact.Layout.PhasePolicyPath, phasePolicy);
        jsonStore.WriteCanonical(artifact.Layout.OpticalChannelsPath, opticalChannels);

        var derivationArtifact = artifact with
        {
            EventSliceSet = eventSliceSet,
            PhasePolicy = phasePolicy,
            OpticalChannelsDefinition = opticalChannels
        };
        var phaseSliceSet = new PhaseSliceSet
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            Slices = new TemporalPhaseStackService().DeriveExpectedPhaseSlices(derivationArtifact)
        };

        jsonStore.WriteCanonical(artifact.Layout.PhaseSlicePath, phaseSliceSet);

        var sidecars = artifact.Manifest.Sidecars.Concat(
        [
            Sidecar("event-slices", "oan.hopng_event_slice", artifact.Layout.EventSlicePath),
            Sidecar("phase-slices", "oan.hopng_phase_slice", artifact.Layout.PhaseSlicePath),
            Sidecar("phase-policy", "oan.hopng_phase_policy", artifact.Layout.PhasePolicyPath),
            Sidecar("optical-channels", "oan.hopng_optical_channels", artifact.Layout.OpticalChannelsPath)
        ]).ToList();

        var fileDigests = artifact.Manifest.FileDigests.Concat(
        [
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

        jsonStore.WriteCanonical(artifact.Layout.ManifestPath, manifest);
        return Phase2ArtifactFactory.RefreshIntegrity(artifact);
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
