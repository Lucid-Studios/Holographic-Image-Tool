using Hdt.Core.Models;
using Hdt.Core.Security;
using System.Text;

namespace Hdt.Core.Services;

public sealed class Phase4SampleArtifactBuilder
{
    private enum Phase4SampleVariant
    {
        PerspectivalValid,
        PerspectivalPeerValid,
        PerspectivalRestricted,
        PerspectivalDeferred,
        PerspectivalInvalidUnsupported,
        ParticipatoryValid,
        ParticipatoryPeerValid,
        ParticipatoryRejected,
        ParticipatoryInvalidUnsupported
    }

    private readonly ArtifactJsonStore _jsonStore = new();
    private readonly HopngArtifactLoader _loader = new();
    private readonly Phase3SampleArtifactBuilder _phase3SampleBuilder = new();

    public LoadedHopngArtifact CreatePerspectivalSupportSample(NewHopngRequest request) =>
        Create(request, Phase4SampleVariant.PerspectivalValid);

    public LoadedHopngArtifact CreatePerspectivalSupportPeerSample(NewHopngRequest request) =>
        Create(request, Phase4SampleVariant.PerspectivalPeerValid);

    public LoadedHopngArtifact CreateRestrictedPerspectivalSupportSample(NewHopngRequest request) =>
        Create(request, Phase4SampleVariant.PerspectivalRestricted);

    public LoadedHopngArtifact CreateDeferredPerspectivalSupportSample(NewHopngRequest request) =>
        Create(request, Phase4SampleVariant.PerspectivalDeferred);

    public LoadedHopngArtifact CreateInvalidPerspectivalSupportSample(NewHopngRequest request) =>
        Create(request, Phase4SampleVariant.PerspectivalInvalidUnsupported);

    public LoadedHopngArtifact CreateParticipatorySupportSample(NewHopngRequest request) =>
        Create(request, Phase4SampleVariant.ParticipatoryValid);

    public LoadedHopngArtifact CreateParticipatorySupportPeerSample(NewHopngRequest request) =>
        Create(request, Phase4SampleVariant.ParticipatoryPeerValid);

    public LoadedHopngArtifact CreateRejectedParticipatorySupportSample(NewHopngRequest request) =>
        Create(request, Phase4SampleVariant.ParticipatoryRejected);

    public LoadedHopngArtifact CreateInvalidParticipatorySupportSample(NewHopngRequest request) =>
        Create(request, Phase4SampleVariant.ParticipatoryInvalidUnsupported);

    private LoadedHopngArtifact Create(NewHopngRequest request, Phase4SampleVariant variant)
    {
        var artifact = _phase3SampleBuilder.Create(request);
        switch (variant)
        {
            case Phase4SampleVariant.PerspectivalValid:
            case Phase4SampleVariant.PerspectivalPeerValid:
            case Phase4SampleVariant.PerspectivalRestricted:
            case Phase4SampleVariant.PerspectivalDeferred:
            case Phase4SampleVariant.PerspectivalInvalidUnsupported:
            {
                var sidecar = BuildPerspectivalSupportArtifact(artifact.Manifest.ArtifactId, variant);
                _jsonStore.WriteCanonical(artifact.Layout.PerspectivalEngramPath, sidecar);
                WriteManifestSidecars(artifact, "perspectival-engram", "oan.hopng_perspectival_engram", artifact.Layout.PerspectivalEngramPath);
                break;
            }
            case Phase4SampleVariant.ParticipatoryValid:
            case Phase4SampleVariant.ParticipatoryPeerValid:
            case Phase4SampleVariant.ParticipatoryRejected:
            case Phase4SampleVariant.ParticipatoryInvalidUnsupported:
            {
                var sidecar = BuildParticipatorySupportArtifact(artifact.Manifest.ArtifactId, variant);
                _jsonStore.WriteCanonical(artifact.Layout.ParticipatoryEngramPath, sidecar);
                WriteManifestSidecars(artifact, "participatory-engram", "oan.hopng_participatory_engram", artifact.Layout.ParticipatoryEngramPath);
                break;
            }
        }

        var signingKeyPath = ResolveSigningKeyPath(request, artifact.Layout.PrivateKeyPath);
        return RefreshIntegrity(artifact, signingKeyPath);
    }

    private void WriteManifestSidecars(LoadedHopngArtifact artifact, string role, string schema, string path)
    {
        var sidecars = artifact.Manifest.Sidecars
            .Where(candidate => !string.Equals(candidate.Role, role, StringComparison.Ordinal))
            .Append(Sidecar(role, schema, path))
            .ToList();
        var fileDigests = artifact.Manifest.FileDigests
            .Where(candidate => !string.Equals(candidate.Role, role, StringComparison.Ordinal))
            .Append(FileDigest(role, path))
            .ToList();
        var manifest = artifact.Manifest with
        {
            Sidecars = sidecars,
            FileDigests = fileDigests
        };

        _jsonStore.WriteCanonical(artifact.Layout.ManifestPath, manifest);
    }

    private static PerspectivalEngramSupport BuildPerspectivalSupportArtifact(string artifactId, Phase4SampleVariant variant)
    {
        var lawful = new PerspectivalEngramSupport
        {
            ArtifactId = artifactId,
            WorkingIntentState = "supported_intent",
            IntentClassification = "bounded_support_evidence",
            SupportOnly = true,
            EvidenceClass = "engram_candidacy_evidence",
            ClaimSurface = "candidate_support_evidence",
            SupportShape = "root_constructor_support",
            ProvenanceBasis = "phase3_temporal_support",
            ConstructorSupportStatus = "root_traceable",
            InspectionPosture = "mixed_pointerized",
            Phase5HandoffReady = false,
            RootFormId = $"{artifactId}-root-form",
            RootCoherenceStatus = "coherent_support",
            RootCoherenceSignals =
            [
                "root-form remains coherent across the lawful widened horizon",
                "constructor trace remains pointerized and reviewable",
                "visible claim surface is bounded by protected provenance"
            ],
            ValidationQuestions =
            [
                "Does the root-form remain traceable to the protected provenance basis?",
                "Does the visible claim surface stop short of identity admission?"
            ],
            ProtectedEvidenceRefs =
            [
                EvidenceRef("engram-root", "custody://engram/root", "Protected constructor evidence for the root form")
            ]
        };

        return variant switch
        {
            Phase4SampleVariant.PerspectivalPeerValid => lawful with
            {
                WorkingIntentState = "reviewable_support",
                IntentClassification = "reviewable_support_evidence",
                Phase5HandoffReady = true,
                RootCoherenceSignals =
                [
                    .. lawful.RootCoherenceSignals,
                    "root-form remains reviewable for later human support examination"
                ],
                ValidationQuestions =
                [
                    .. lawful.ValidationQuestions,
                    "Is the strengthened support still bounded by Phase 4 reviewable-support limits?"
                ]
            },
            Phase4SampleVariant.PerspectivalRestricted => lawful with
            {
                WorkingIntentState = "restricted_support",
                IntentClassification = "restricted_support_evidence",
                ConstructorSupportStatus = "root_restricted_pending_clarification",
                RootCoherenceStatus = "restricted_support",
                RootCoherenceSignals =
                [
                    .. lawful.RootCoherenceSignals,
                    "root-form remains traceable but requires bounded restriction before promotion"
                ],
                RestrictionReason = "Protected constructor evidence remains lawful, but custody posture requires a restricted support lane."
            },
            Phase4SampleVariant.PerspectivalDeferred => lawful with
            {
                WorkingIntentState = "deferred_support",
                IntentClassification = "deferred_support_evidence",
                ConstructorSupportStatus = "root_traceable_incomplete",
                RootCoherenceStatus = "deferred_support",
                RootCoherenceSignals =
                [
                    .. lawful.RootCoherenceSignals,
                    "root-form remains bounded but presently incomplete for later review"
                ],
                DeferReason = "Support remains lawful but is deferred pending fuller constructor corroboration."
            },
            Phase4SampleVariant.PerspectivalInvalidUnsupported => lawful with
            {
                WorkingIntentState = "reviewable_support",
                IntentClassification = "reviewable_support_evidence",
                SupportOnly = false,
                ClaimSurface = "candidate_identity_assertion",
                Phase5HandoffReady = false,
                ProtectedEvidenceRefs = []
            },
            _ => lawful
        };
    }

    private static ParticipatoryEngramSupport BuildParticipatorySupportArtifact(string artifactId, Phase4SampleVariant variant)
    {
        var lawful = new ParticipatoryEngramSupport
        {
            ArtifactId = artifactId,
            WorkingIntentState = "reviewable_support",
            IntentClassification = "reviewable_support_evidence",
            SupportOnly = true,
            EvidenceClass = "engram_candidacy_evidence",
            ClaimSurface = "candidate_support_evidence",
            SupportShape = "branch_set_support",
            ProvenanceBasis = "phase3_temporal_support",
            ConstructorSupportStatus = "branch_traceable",
            InspectionPosture = "mixed_pointerized",
            Phase5HandoffReady = true,
            BranchSetId = $"{artifactId}-branch-set",
            BranchCoherenceStatus = "coherent_support",
            BranchCoherenceSignals =
            [
                "participant branches stay lawful under the declared support topology",
                "branch witnesses remain pointerized and bounded",
                "branch roles remain distinct while sharing one support claim"
            ],
            ParticipantBranches =
            [
                new ParticipatoryBranchSupport
                {
                    BranchId = "branch-operator",
                    Role = "operator-facing",
                    SupportState = "supported_intent"
                },
                new ParticipatoryBranchSupport
                {
                    BranchId = "branch-witness",
                    Role = "witness-facing",
                    SupportState = "reviewable_support"
                }
            ],
            ValidationQuestions =
            [
                "Do participant branches remain distinct while sharing one lawful branch set?",
                "Is the support evidence strong enough for later human review without implying acceptance?"
            ],
            ProtectedEvidenceRefs =
            [
                EvidenceRef("engram-branch", "custody://engram/branch-set", "Protected branch-set evidence for participatory support")
            ]
        };

        return variant switch
        {
            Phase4SampleVariant.ParticipatoryPeerValid => lawful with
            {
                BranchCoherenceSignals =
                [
                    .. lawful.BranchCoherenceSignals,
                    "branch-set remains stable under later reviewable support pressure"
                ],
                ParticipantBranches =
                [
                    .. lawful.ParticipantBranches,
                    new ParticipatoryBranchSupport
                    {
                        BranchId = "branch-audit",
                        Role = "audit-facing",
                        SupportState = "supported_intent"
                    }
                ],
                ValidationQuestions =
                [
                    .. lawful.ValidationQuestions,
                    "Do added participant-facing branches preserve one lawful support branch set?"
                ]
            },
            Phase4SampleVariant.ParticipatoryRejected => lawful with
            {
                WorkingIntentState = "rejected_support",
                IntentClassification = "rejected_support_evidence",
                ConstructorSupportStatus = "branch_trace_rejected",
                Phase5HandoffReady = false,
                BranchCoherenceStatus = "rejected_support",
                BranchCoherenceSignals =
                [
                    .. lawful.BranchCoherenceSignals,
                    "branch-set continuity is preserved, but the support claim is rejected as insufficient"
                ],
                ParticipantBranches =
                [
                    new ParticipatoryBranchSupport
                    {
                        BranchId = "branch-operator",
                        Role = "operator-facing",
                        SupportState = "rejected_support"
                    },
                    new ParticipatoryBranchSupport
                    {
                        BranchId = "branch-witness",
                        Role = "witness-facing",
                        SupportState = "rejected_support"
                    }
                ],
                RejectionReason = "Branch support remains traceable, but the evidence does not justify continued participatory support admission."
            },
            Phase4SampleVariant.ParticipatoryInvalidUnsupported => lawful with
            {
                WorkingIntentState = "structured_intent",
                IntentClassification = "typed_support_claim",
                Phase5HandoffReady = true,
                ParticipantBranches =
                [
                    new ParticipatoryBranchSupport
                    {
                        BranchId = "branch-operator",
                        Role = "operator-facing",
                        SupportState = "structured_intent"
                    }
                ]
            },
            _ => lawful
        };
    }

    private LoadedHopngArtifact RefreshIntegrity(LoadedHopngArtifact artifact, string privateKeyPath)
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
        var privateKey = File.ReadAllText(privateKeyPath).Trim();
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

    private static string ResolveSigningKeyPath(NewHopngRequest request, string defaultPrivateKeyPath) =>
        !string.IsNullOrWhiteSpace(request.PrivateKeyPath)
            ? request.PrivateKeyPath
            : request.PrivateKeyOutputPath ?? defaultPrivateKeyPath;

    private static ProtectedEvidenceReference EvidenceRef(string refId, string pointerUri, string summary) =>
        new()
        {
            RefId = refId,
            PointerUri = pointerUri,
            DigestSha256 = ArtifactHashing.ComputeSha256(Encoding.UTF8.GetBytes(summary)),
            Summary = summary
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
