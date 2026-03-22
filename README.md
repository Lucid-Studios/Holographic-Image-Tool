# Holographic Data Tool

The Holographic Data Tool (HDT) is a Windows-first CLI for creating, validating, and inspecting `.hopng` artifacts. Phase 1 is implemented end to end, Phase 2 is implemented as the operator-ready relational layer, and Phase 3 Milestone 1 is now implemented as the first single-artifact temporal layer: deterministic manifests, lawful sidecars, Ed25519 trust material, Prime-safe inspection, relational universes, governed merge derivation, artifact-to-artifact comparison, observed-set summaries, derived phase slices, and diagnostic temporal rendering.

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
.\New-HOPNGPhase3InvalidSample.ps1 --output-dir .\examples --name phase3-invalid-derived --json
.\Test-HOPNG.ps1 --path .\examples\phase2-sample.hopng.json --json
.\Show-HOPNG.ps1 --path .\examples\phase2-sample.hopng.json --view privileged --json
.\Merge-HOPNGLayers.ps1 --path .\examples\phase2-sample.hopng.json --json
.\Compare-HOPNGSurfaces.ps1 --left .\examples\phase2-sample.hopng.json --right .\examples\phase1-sample.hopng.json --json
.\Render-HOPNGPhaseStack.ps1 --path .\examples\phase3-sample.hopng.json --view prime --json
```

Later-phase commands remain reserved:

- `.\Invoke-HOPNGFormation.ps1`
- `.\Bind-HOPNGToOE.ps1`

`Render-HOPNGPhaseStack.ps1` requires an artifact that declares the Phase 3 temporal sidecars and now includes initial temporal state summaries backed by explicit `stateThresholds`, `phaseWindowDurationMs`, and `maxPhaseWindowSpanMs` in the phase policy.

The committed public-safe temporal reference sets live under `.\examples\phase3-sample.*` and `.\examples\phase3-invalid-derived.*`.

## Repo Checks

Repo-local verification helpers:

```powershell
.\scripts\Invoke-HdtRepoChecks.ps1
.\scripts\Invoke-Phase2ReleaseSmoke.ps1
.\scripts\Invoke-Phase3ReleaseSmoke.ps1
.\scripts\Invoke-Phase3FailureSmoke.ps1
```

## Governance

Lucid Technologies repository standards for AI governance, citizen science, data handling, and GitHub workflow are recorded in [docs/LUCID_TECHNOLOGIES_STANDARDS.md](docs/LUCID_TECHNOLOGIES_STANDARDS.md).

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
- `<name>.hash.json`
- `<name>.signature.json`
- `<name>.ed25519.public.key`

Committed reference artifacts omit private signing keys. Signing-key generation is a local operator action.

See [`examples/README.md`](examples/README.md) for the current committed reference sets, including the Phase 3 temporal sample.

See [docs/ARTIFACT_MODEL.md](docs/ARTIFACT_MODEL.md) for the artifact contract and [docs/PHASE_BACKLOG.md](docs/PHASE_BACKLOG.md) for the staged roadmap.

For the semantic progression of the system from trusted artifact to Sanctuary/OE runtime participant, see [docs/PHASE_ROADMAP.md](docs/PHASE_ROADMAP.md).

For the first concrete execution slice of lawful relationality, see [docs/PHASE_2_MILESTONE_1.md](docs/PHASE_2_MILESTONE_1.md).

For the next Phase 2 execution slice focused on governed projection derivation and merge behavior, see [docs/PHASE_2_MILESTONE_2.md](docs/PHASE_2_MILESTONE_2.md).

For the operator-facing hardening criteria for the current release target, see [docs/PHASE_2_RELEASE_READY.md](docs/PHASE_2_RELEASE_READY.md).

For the first temporal implementation slice, see [docs/PHASE_3_MILESTONE_1.md](docs/PHASE_3_MILESTONE_1.md).

For the remaining single-clock Phase 3 execution track, see [docs/PHASE_3_MILESTONE_2.md](docs/PHASE_3_MILESTONE_2.md), [docs/PHASE_3_MILESTONE_3.md](docs/PHASE_3_MILESTONE_3.md), and [docs/PHASE_3_RELEASE_READY.md](docs/PHASE_3_RELEASE_READY.md).

For the planned heterochronous channel extension that could later ground a `.hogif` carrier, see [docs/PHASE_3_MILESTONE_4.md](docs/PHASE_3_MILESTONE_4.md).

For the build-governance and research-planning documents behind that future `.hogif` track, see [docs/HOGIF_BUILD_GOVERNANCE.md](docs/HOGIF_BUILD_GOVERNANCE.md) and [docs/HOGIF_RESEARCH_TRACK.md](docs/HOGIF_RESEARCH_TRACK.md).

For the planned morphism law that governs which transformations may act on identity-bearing `.hogif` fragments, see [docs/HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md](docs/HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md).

For the broader doctrine tying identity continuity, telos, cryptographic authority, evidence carriers, and privacy posture together, see [docs/HONEST_SUBSTRATE_PRINCIPLES.md](docs/HONEST_SUBSTRATE_PRINCIPLES.md).

For typed evidentiary artifact classes and layer-carrier guidance, see [docs/CAPTURE_PROFILES.md](docs/CAPTURE_PROFILES.md).
