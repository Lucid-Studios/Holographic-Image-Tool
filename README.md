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
.\Test-HOPNG.ps1 --path .\examples\phase2-sample.hopng.json --json
.\Show-HOPNG.ps1 --path .\examples\phase2-sample.hopng.json --view privileged --json
.\Merge-HOPNGLayers.ps1 --path .\examples\phase2-sample.hopng.json --json
.\Compare-HOPNGSurfaces.ps1 --left .\examples\phase2-sample.hopng.json --right .\examples\phase1-sample.hopng.json --json
.\Render-HOPNGPhaseStack.ps1 --path .\artifacts\temporal-sample.hopng.json --view prime --json
```

Later-phase commands remain reserved:

- `.\Invoke-HOPNGFormation.ps1`
- `.\Bind-HOPNGToOE.ps1`

`Render-HOPNGPhaseStack.ps1` requires an artifact that declares the Phase 3 temporal sidecars.

## Governance

Lucid Technologies repository standards for AI governance, citizen science, data handling, and GitHub workflow are recorded in [docs/LUCID_TECHNOLOGIES_STANDARDS.md](/D:/Holographic%20Data%20Tool/docs/LUCID_TECHNOLOGIES_STANDARDS.md).

Community and contribution guidance lives in:

- [.github/CONTRIBUTING.md](/D:/Holographic%20Data%20Tool/.github/CONTRIBUTING.md)
- [.github/SECURITY.md](/D:/Holographic%20Data%20Tool/.github/SECURITY.md)
- [.github/CODE_OF_CONDUCT.md](/D:/Holographic%20Data%20Tool/.github/CODE_OF_CONDUCT.md)
- [docs/LICENSING_POLICY.md](/D:/Holographic%20Data%20Tool/docs/LICENSING_POLICY.md)
- [docs/DCO_CLA_POLICY.md](/D:/Holographic%20Data%20Tool/docs/DCO_CLA_POLICY.md)
- [docs/AI_USAGE_DISCLOSURE.md](/D:/Holographic%20Data%20Tool/docs/AI_USAGE_DISCLOSURE.md)
- [docs/DATA_CLASSIFICATION.md](/D:/Holographic%20Data%20Tool/docs/DATA_CLASSIFICATION.md)

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

See [docs/ARTIFACT_MODEL.md](/D:/Holographic%20Data%20Tool/docs/ARTIFACT_MODEL.md) for the artifact contract and [docs/PHASE_BACKLOG.md](/D:/Holographic%20Data%20Tool/docs/PHASE_BACKLOG.md) for the staged roadmap.

For the semantic progression of the system from trusted artifact to Sanctuary/OE runtime participant, see [docs/PHASE_ROADMAP.md](/D:/Holographic%20Data%20Tool/docs/PHASE_ROADMAP.md).

For the first concrete execution slice of lawful relationality, see [docs/PHASE_2_MILESTONE_1.md](/D:/Holographic%20Data%20Tool/docs/PHASE_2_MILESTONE_1.md).

For the next Phase 2 execution slice focused on governed projection derivation and merge behavior, see [docs/PHASE_2_MILESTONE_2.md](/D:/Holographic%20Data%20Tool/docs/PHASE_2_MILESTONE_2.md).

For the operator-facing hardening criteria for the current release target, see [docs/PHASE_2_RELEASE_READY.md](/D:/Holographic%20Data%20Tool/docs/PHASE_2_RELEASE_READY.md).

For the first temporal implementation slice, see [docs/PHASE_3_MILESTONE_1.md](/D:/Holographic%20Data%20Tool/docs/PHASE_3_MILESTONE_1.md).

For the remaining Phase 3 execution track, see [docs/PHASE_3_MILESTONE_2.md](/D:/Holographic%20Data%20Tool/docs/PHASE_3_MILESTONE_2.md), [docs/PHASE_3_MILESTONE_3.md](/D:/Holographic%20Data%20Tool/docs/PHASE_3_MILESTONE_3.md), and [docs/PHASE_3_RELEASE_READY.md](/D:/Holographic%20Data%20Tool/docs/PHASE_3_RELEASE_READY.md).
