# Holographic Data Tool

The Holographic Data Tool (HDT) is a Windows-first CLI for creating, validating, and inspecting `.hopng` artifacts. Phase 1 is implemented end to end: deterministic manifests, lawful sidecars, Ed25519 trust material, Prime-safe inspection, and adapter contracts for later storage and OE integration.

## Projects

- `Hdt.Core`: artifact models, canonical JSON, hashing, signing, validation, diagnostics
- `Hdt.Schemas`: embedded phase schema assets and schema registry
- `Hdt.Cli`: command surface and reserved later-phase stubs
- `Hdt.Adapters`: storage and OE adapter contracts plus test doubles
- `Hdt.Tests`: unit and CLI integration coverage

## Commands

```powershell
.\New-HOPNG.ps1 --output-dir .\examples --name sample --signer "Local Tester" --key-id "dev-key"
.\Test-HOPNG.ps1 --path .\examples\sample.hopng.json --json
.\Show-HOPNG.ps1 --path .\examples\sample.hopng.json --view prime --json
```

Reserved phase commands are already exposed and return deterministic "not implemented" messages:

- `.\Merge-HOPNGLayers.ps1`
- `.\Render-HOPNGPhaseStack.ps1`
- `.\Compare-HOPNGSurfaces.ps1`
- `.\Invoke-HOPNGFormation.ps1`
- `.\Bind-HOPNGToOE.ps1`

## Artifact Layout

A v1 artifact is a loose-sidecar set stored in one directory:

- `<name>.png`
- `<name>.hopng.json`
- `<name>.layer-map.json`
- `<name>.trust-envelope.json`
- `<name>.transform-history.json`
- `<name>.depth-field.json`
- `<name>.hash.json`
- `<name>.signature.json`
- `<name>.ed25519.private.key`
- `<name>.ed25519.public.key`

See [docs/ARTIFACT_MODEL.md](/D:/Holographic%20Data%20Tool/docs/ARTIFACT_MODEL.md) for the Phase 1 contract and [docs/PHASE_BACKLOG.md](/D:/Holographic%20Data%20Tool/docs/PHASE_BACKLOG.md) for the staged roadmap.
