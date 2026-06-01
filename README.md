# Holographic Data Tool

The Holographic Data Tool (HDT) is a Windows-first CLI for creating, validating, and inspecting `.hopng` artifacts. Phase 1 and Phase 2 are implemented as stable artifact and relational baselines. Phase 3 Milestones 1 through 3 now form the approved single-clock temporal release baseline as of March 30, 2026: deterministic manifests, lawful sidecars, Ed25519 trust material, Prime-safe inspection, relational universes, governed merge derivation, temporal state summaries, widened-horizon diagnostics, committed temporal reference pairs, and governed cross-artifact phase-stack comparison.

## Projects

- `Hdt.Core`: artifact models, canonical JSON, hashing, signing, validation, diagnostics
- `Hdt.Schemas`: embedded phase schema assets and schema registry
- `Hdt.Cli`: command surface, operator workflows, and reserved later-phase stubs
- `Hdt.Adapters`: storage and OE adapter contracts plus test doubles
- `Hdt.Tests`: unit and CLI integration coverage

## Commands

Supported operator commands:

```powershell
.\New-HOPNG.ps1 --output-dir .\examples --name sample --signer "Local Tester" --key-id "dev-key"
.\New-HOPNGPhase3Sample.ps1 --output-dir .\examples --name phase3-sample --json
.\New-HOPNGPhase3PeerSample.ps1 --output-dir .\examples --name phase3-peer-sample --json
.\New-HOPNGPhase3DivergentPeerSample.ps1 --output-dir .\examples --name phase3-divergent-peer --json
.\New-HOPNGPhase3IncompatibleBasisSample.ps1 --output-dir .\examples --name phase3-incompatible-basis --json
.\New-HOPNGPhase3InvalidSample.ps1 --output-dir .\examples --name phase3-invalid-derived --json
.\New-HOPNGPhase4PerspectivalSample.ps1 --output-dir $env:TEMP --name phase4-perspectival --json
.\New-HOPNGPhase4PerspectivalPeerSample.ps1 --output-dir $env:TEMP --name phase4-perspectival-peer --json
.\New-HOPNGPhase4RestrictedPerspectivalSample.ps1 --output-dir $env:TEMP --name phase4-perspectival-restricted --json
.\New-HOPNGPhase4DeferredPerspectivalSample.ps1 --output-dir $env:TEMP --name phase4-perspectival-deferred --json
.\New-HOPNGPhase4ParticipatorySample.ps1 --output-dir $env:TEMP --name phase4-participatory --json
.\New-HOPNGPhase4ParticipatoryPeerSample.ps1 --output-dir $env:TEMP --name phase4-participatory-peer --json
.\New-HOPNGPhase4RejectedParticipatorySample.ps1 --output-dir $env:TEMP --name phase4-participatory-rejected --json
.\New-HOPNGPhase4InvalidPerspectivalSample.ps1 --output-dir $env:TEMP --name phase4-perspectival-invalid --json
.\New-HOPNGPhase4InvalidParticipatorySample.ps1 --output-dir $env:TEMP --name phase4-participatory-invalid --json
.\Test-HOPNG.ps1 --path .\examples\phase2-sample.hopng.json --json
.\Show-HOPNG.ps1 --path .\examples\phase2-sample.hopng.json --view privileged --json
.\Merge-HOPNGLayers.ps1 --path .\examples\phase2-sample.hopng.json --json
.\Compare-HOPNGSurfaces.ps1 --left .\examples\phase2-sample.hopng.json --right .\examples\phase1-sample.hopng.json --json
.\Render-HOPNGPhaseStack.ps1 --path .\examples\phase3-sample.hopng.json --view prime --json
.\Compare-HOPNGPhaseStacks.ps1 --left .\examples\phase3-sample.hopng.json --right .\examples\phase3-peer-sample.hopng.json --view prime --json
.\Compare-HOPNGPhaseStacks.ps1 --left .\examples\phase3-sample.hopng.json --right .\examples\phase3-divergent-peer.hopng.json --view prime --json
.\Compare-HOPNGPhaseStacks.ps1 --left .\examples\phase3-sample.hopng.json --right .\examples\phase3-incompatible-basis.hopng.json --view prime --json
.\Compare-HOPNGEngramSupport.ps1 --left .\examples\phase4-perspectival-sample.hopng.json --right .\examples\phase4-perspectival-peer.hopng.json --view prime --json
.\Compare-HOPNGEngramSupport.ps1 --left .\examples\phase4-participatory-sample.hopng.json --right .\examples\phase4-participatory-peer.hopng.json --view prime --json
.\Show-HDTAutomationStatus.ps1 -View all
.\Show-HDTAutomationReceipt.ps1 -View all
```

Later-phase commands remain reserved:

- `.\Invoke-HOPNGFormation.ps1`
- `.\Bind-HOPNGToOE.ps1`

`Render-HOPNGPhaseStack.ps1` requires an artifact that declares the Phase 3 temporal sidecars and now includes temporal state summaries backed by explicit `stateThresholds`, `phaseWindowDurationMs`, `maxPhaseWindowSpanMs`, and explicit `comparisonHorizons` in the phase policy.
Its human-readable output now includes final state context, horizon summaries, and validation or issue counts without requiring JSON mode.

`Compare-HOPNGPhaseStacks.ps1` now reports basis alignment, final state posture, state-rank delta, classification reason, comparable slice count, and basis or signal summaries in human-readable mode, while keeping deterministic exit codes for automation.

The first executable Phase 4 entry slice is now available through support-only sample builders and validation. Prime-safe inspection of these artifacts exposes an `engramSupportSummary` plus a deterministic `engramStabilityField` while preserving the stop line between support evidence and later candidacy, commitment, or runtime authority.
Phase 4 support sidecars now machine-check `intentClassification` and `supportShape`, and Prime-safe inspection exposes lawful negative support posture through `restricted_support`, `deferred_support`, and `rejected_support` without silently upgrading those states into later-phase decision authority.

`Compare-HOPNGEngramSupport.ps1` now reports support-type compatibility, support shapes, intent classifications, working-intent transition status, root or branch identity compatibility, counterfeit pressure, working-intent rank delta, burden preservation, and similarity score in human-readable mode while preserving deterministic exit codes for automation.

The committed public-safe temporal reference sets live under `.\examples\phase3-sample.*` and `.\examples\phase3-invalid-derived.*`.
The committed lawful comparison peer lives under `.\examples\phase3-peer-sample.*`.
The committed lawful divergent comparison peer lives under `.\examples\phase3-divergent-peer.*`.
The committed lawful incompatible-basis comparison artifact lives under `.\examples\phase3-incompatible-basis.*`.
The committed lawful Phase 4 entry references now live under `.\examples\phase4-perspectival-sample.*` and `.\examples\phase4-participatory-sample.*`.
The committed lawful Phase 4 comparison peers now live under `.\examples\phase4-perspectival-peer.*` and `.\examples\phase4-participatory-peer.*`.
The committed lawful Phase 4 negative-state references now live under `.\examples\phase4-restricted-perspectival.*`, `.\examples\phase4-deferred-perspectival.*`, and `.\examples\phase4-rejected-participatory.*`.
The committed unlawful Phase 4 entry references now live under `.\examples\phase4-invalid-perspectival.*` and `.\examples\phase4-invalid-participatory.*`.

## Temporal Exit Codes

For the active Phase 3 operator surface:

- `Render-HOPNGPhaseStack.ps1` returns `0` for lawful temporal derivation, `24` for structurally incomplete temporal derivation, and `25` for unsupported temporal surfaces.
- `Compare-HOPNGPhaseStacks.ps1` returns `0` for aligned comparison results such as `Convergent`, `Delayed`, or `Divergent`, `24` for lawful but basis-incompatible comparison pairs, and `25` for flattened, unsupported, or otherwise invalid temporal comparison surfaces.
- `Compare-HOPNGEngramSupport.ps1` returns `0` for lawful support comparison results such as `CoherentSupport`, `StrengthenedSupport`, `DivergentSupport`, `RestrictedSupport`, `DeferredSupport`, or `RejectedSupport`, `24` for lawful but support-type-incompatible pairs, and `25` for flattened, counterfeit, unsupported, or otherwise invalid support comparison surfaces.

## Repo Checks

Repo-local verification helpers:

```powershell
.\scripts\Invoke-HdtAutomationCycle.ps1 -DevelopmentPosture Closing
.\scripts\Invoke-HdtAutomationStatusSmoke.ps1
.\scripts\Invoke-HdtAutomationReceiptSmoke.ps1
.\scripts\Invoke-HdtRepoChecks.ps1 -DevelopmentPosture Closing
.\scripts\Invoke-HdtAutomationCycleSmoke.ps1
.\scripts\Invoke-HdtAutomationCycleFailureSmoke.ps1
.\scripts\Invoke-Phase2ReleaseSmoke.ps1
.\scripts\Invoke-Phase3ReleaseSmoke.ps1
.\scripts\Invoke-Phase3ComparisonSmoke.ps1
.\scripts\Invoke-Phase3ComparisonFailureSmoke.ps1
.\scripts\Invoke-Phase3FailureSmoke.ps1
.\scripts\Invoke-Phase4EntrySmoke.ps1
.\scripts\Invoke-Phase4EntryFailureSmoke.ps1
.\scripts\Invoke-Phase4SupportComparisonSmoke.ps1
.\scripts\Invoke-Phase4SupportComparisonFailureSmoke.ps1
```

`Invoke-HdtAutomationCycle.ps1` is now the parent HDT-local automation lane. It wraps the posture-aware repo checks, writes release-candidate and digest bundles into `.audit/`, and keeps the live automation state, tasking, and orchestration surfaces current.

`Show-HDTAutomationStatus.ps1` is the public visibility wrapper for that lane. It reads the current `.audit` state surfaces and renders either operator-readable status text or combined JSON for the live cycle, tasking, and orchestration posture.
It also overlays the current observed git branch and worktree state so operators can see when the repo has drifted from the last emitted `.audit` bundle without forcing an out-of-cadence cycle refresh.

`Show-HDTAutomationReceipt.ps1` is the public receipt wrapper for that lane. It reads the latest or requested release-candidate bundle and digest from `.audit/` and renders either operator-readable text or combined JSON for the emitted evidence surface. When a newer cycle skips digest or work-report emission because cadence is not yet due, the wrapper still exposes the newer bundle while keeping the last emitted digest pinned and reporting `workReport.emitted` truthfully.

The parent lane now also reconciles root standing against `OAN Tech Stack` and `Documentation Repo`, emits a local steward triad (`doping-header`, `receipt`, `notice`) into each release-candidate bundle, and emits a lighter work-report surface alongside the existing digest cadence.

`Invoke-HdtAutomationCycleSmoke.ps1` and `Invoke-HdtAutomationCycleFailureSmoke.ps1` are the dedicated public smoke surfaces for that parent lane. They stay manual-only and stub the repo-check child so the automation conveyor can be verified without recursing back through the full repo-check graph.

`Invoke-HdtAutomationStatusSmoke.ps1` is the dedicated visibility smoke surface for the status wrapper. It verifies that operators can read the live `.audit` automation posture without opening raw state files directly.

`Invoke-HdtAutomationReceiptSmoke.ps1` is the dedicated receipt smoke surface for the release-candidate and digest wrapper. It verifies that operators can read the emitted evidence bundles without opening raw bundle files directly, including the skipped-not-due path where the latest bundle advances before digest or work-report cadence is due.

The Phase 3 smoke scripts now assert both JSON output and human-readable operator text for render and comparison paths, and they cover delayed, divergent, incompatible, and flattened-or-unsupported comparison outcomes through wrapper-backed artifact flows rather than validating only machine-readable branches.

The Phase 4 smoke scripts create lawful and unlawful support-only engram entry artifacts through the public wrapper surface, validate the committed Phase 4 reference corpus, and verify that support posture, machine-checked transition markers, Prime-safe inspection, root or branch coherence comparison, lawful restricted/deferred/rejected support states, counterfeit detection, failure-path validation, human-readable comparison output, and public-safe example hygiene remain bounded.

`Invoke-HdtRepoChecks.ps1` remains the mechanical child primitive inside that lane and still supports `Initial`, `Formal`, `Closing`, and `Approved` development postures. See [docs/LOCAL_AUTOMATION_POSTURES.md](docs/LOCAL_AUTOMATION_POSTURES.md) and [docs/HDT_AUTOMATION_LANE.md](docs/HDT_AUTOMATION_LANE.md).

## Current Development Lane

The current admitted main lane is post-release maintenance of the approved Phase 3 single-clock `.hopng` temporal surface, executable Phase 4 entry scaffolding through validator-first engram support work, and validator-first promotion prep for the deferred Milestone 4 research lane. The future `.hogif` and heterochronous continuity track remains doctrine-first and validator-first rather than container-first.

See [docs/DEVELOPMENT_PATH.md](docs/DEVELOPMENT_PATH.md) for the full lane model, promotion rules, and repo-wide execution path.
See [docs/INTERLACED_MILESTONE_MAPPING.md](docs/INTERLACED_MILESTONE_MAPPING.md) for the current milestone read when HDT is evaluated inside the wider `OAN Tech Stack` / `Documentation Repo` / `GNOMERONACORDE` split.
See [docs/HDT_AUTOMATION_LANE.md](docs/HDT_AUTOMATION_LANE.md) for the first end-to-end HDT-local automation conveyor and its `.audit` receipt surface.
See [docs/HDT_AUTOMATION_STEWARD_LOOP.md](docs/HDT_AUTOMATION_STEWARD_LOOP.md) for the current steward-form loop, root standing, triad emission, and cadence boundary.

## Stack Alignment

As of March 31, 2026, this repo should be read inside the broader OAN build interlace recorded in `Documentation Repo/architecture/oan-tech-stack-build-interlace-summary.md`.

That split is:

- `OAN Tech Stack` owns executable present truth and active build readiness
- `Documentation Repo` owns stabilized doctrine, chapter uptake, and publication-facing interpretation
- `GNOMERONACORDE` carries first-run, certification, and pedagogy law in publication form
- `HDT` remains an optional governed `.hopng` validation and inspection lane

When all roots are available locally, read `OAN Tech Stack` first, consult `Documentation Repo` second, reconcile contradictions explicitly, and only return receipted admitted law to the active build lane.

That is also why `.hopng` remains optional and bounded for the wider stack right now: HDT can strengthen evidence, inspection, and comparison posture, but it does not outrank executable build truth by itself.

## Governance

Lucid Technologies repository standards for AI governance, citizen science, data handling, and GitHub workflow are recorded in [docs/LUCID_TECHNOLOGIES_STANDARDS.md](docs/LUCID_TECHNOLOGIES_STANDARDS.md).

Operator execution continuity and HITL pause conditions are recorded in [docs/OPERATOR_CONTINUITY_INSTRUCTIONS.md](docs/OPERATOR_CONTINUITY_INSTRUCTIONS.md).

Local automation development postures and their research best-practice mapping are recorded in [docs/LOCAL_AUTOMATION_POSTURES.md](docs/LOCAL_AUTOMATION_POSTURES.md).

The parent HDT-local automation lane and its receipt surfaces are recorded in [docs/HDT_AUTOMATION_LANE.md](docs/HDT_AUTOMATION_LANE.md).

The steward-form loop and its current machine-enforced boundary are recorded in [docs/HDT_AUTOMATION_STEWARD_LOOP.md](docs/HDT_AUTOMATION_STEWARD_LOOP.md).

The Sanctuary EC lab bridge and ChatGPT-readable holographic slice orientation are recorded in [docs/SANCTUARY_EC_LAB_BRIDGE.md](docs/SANCTUARY_EC_LAB_BRIDGE.md). This bridge explains how HDT supports Engineered Cognition formation-loop inspection without claiming GEL/SelfGEL admission, runtime identity authority, or `.hogif` implementation.

Community and contribution guidance lives in:

- [.github/CONTRIBUTING.md](.github/CONTRIBUTING.md)
- [.github/SECURITY.md](.github/SECURITY.md)
- [.github/CODE_OF_CONDUCT.md](.github/CODE_OF_CONDUCT.md)
- [docs/LICENSING_POLICY.md](docs/LICENSING_POLICY.md)
- [docs/DCO_CLA_POLICY.md](docs/DCO_CLA_POLICY.md)
- [docs/AI_USAGE_DISCLOSURE.md](docs/AI_USAGE_DISCLOSURE.md)
- [docs/DATA_CLASSIFICATION.md](docs/DATA_CLASSIFICATION.md)

## Artifact Layout

A v1 artifact is a loose-sidecar set stored in one directory:

- `<name>.png`
- `<name>.hopng.json`
- `<name>.layer-map.json`
- `<name>.trust-envelope.json`
- `<name>.transform-history.json`
- `<name>.depth-field.json`
- `<name>.universe-layer.json` when Phase 2 relational structure is declared
- `<name>.gluing-manifest.json` when Phase 2 relational structure is declared
- `<name>.projection-rules.json` when Phase 2 governed projection is declared
- `<name>.legibility-profile.json` when Phase 2 governed projection is declared
- `<name>.event-slices.json` when Phase 3 temporal summaries are declared
- `<name>.phase-slices.json` when Phase 3 derived temporal layers are declared
- `<name>.phase-policy.json` when Phase 3 temporal derivation is declared
- `<name>.optical-channels.json` when Phase 3 channel semantics are declared
- `<name>.perspectival-engram.json` when Phase 4 perspectival support scaffolding is declared
- `<name>.participatory-engram.json` when Phase 4 participatory support scaffolding is declared
- `<name>.hash.json`
- `<name>.signature.json`
- `<name>.ed25519.public.key`

Committed reference artifacts omit private signing keys. Signing-key generation is a local operator action.

See [`examples/README.md`](examples/README.md) for the current committed reference sets, including the Phase 3 temporal sample.

See [docs/ARTIFACT_MODEL.md](docs/ARTIFACT_MODEL.md) for the artifact contract, [docs/DEVELOPMENT_PATH.md](docs/DEVELOPMENT_PATH.md) for the current execution path, [docs/PHASE_ROADMAP.md](docs/PHASE_ROADMAP.md) for the semantic roadmap, and [docs/PHASE_BACKLOG.md](docs/PHASE_BACKLOG.md) for the execution backlog.

For the semantic progression of the system from trusted artifact to Sanctuary/OE runtime participant, see [docs/PHASE_ROADMAP.md](docs/PHASE_ROADMAP.md).

For the bounded bridge from the approved Phase 3 baseline into Phase 4 engrammatic work, see [docs/PHASE_4_ENTRY_CRITERIA.md](docs/PHASE_4_ENTRY_CRITERIA.md), [docs/PHASE_4_MILESTONE_1.md](docs/PHASE_4_MILESTONE_1.md), and [docs/PHASE_4_WORKING_INTENT_TRANSITIONS.md](docs/PHASE_4_WORKING_INTENT_TRANSITIONS.md).

For the first concrete execution slice of lawful relationality, see [docs/PHASE_2_MILESTONE_1.md](docs/PHASE_2_MILESTONE_1.md).

For the next Phase 2 execution slice focused on governed projection derivation and merge behavior, see [docs/PHASE_2_MILESTONE_2.md](docs/PHASE_2_MILESTONE_2.md).

For the completed Phase 2 release baseline, see [docs/PHASE_2_RELEASE_READY.md](docs/PHASE_2_RELEASE_READY.md).

For the first temporal implementation slice, see [docs/PHASE_3_MILESTONE_1.md](docs/PHASE_3_MILESTONE_1.md).

For the remaining single-clock Phase 3 execution track, see [docs/PHASE_3_MILESTONE_2.md](docs/PHASE_3_MILESTONE_2.md), [docs/PHASE_3_MILESTONE_3.md](docs/PHASE_3_MILESTONE_3.md), and [docs/PHASE_3_RELEASE_READY.md](docs/PHASE_3_RELEASE_READY.md).

For the planned heterochronous channel extension that could later ground a `.hogif` carrier, see [docs/PHASE_3_MILESTONE_4.md](docs/PHASE_3_MILESTONE_4.md).

For the build-governance and research-planning documents behind that future `.hogif` track, see [docs/HOGIF_BUILD_GOVERNANCE.md](docs/HOGIF_BUILD_GOVERNANCE.md) and [docs/HOGIF_RESEARCH_TRACK.md](docs/HOGIF_RESEARCH_TRACK.md).

For the planned morphism law that governs which transformations may act on identity-bearing `.hogif` fragments, see [docs/HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md](docs/HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md).

For the broader doctrine tying identity continuity, telos, cryptographic authority, evidence carriers, and privacy posture together, see [docs/HONEST_SUBSTRATE_PRINCIPLES.md](docs/HONEST_SUBSTRATE_PRINCIPLES.md).

For typed evidentiary artifact classes and layer-carrier guidance, see [docs/CAPTURE_PROFILES.md](docs/CAPTURE_PROFILES.md).
