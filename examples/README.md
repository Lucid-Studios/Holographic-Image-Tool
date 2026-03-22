# Example Artifacts

`phase1-sample.*` is the clean reference Phase 1 artifact set.

`phase2-sample.*` is the clean reference Phase 2 artifact set used for merge and comparison verification.

`phase3-sample.*` is the clean reference Phase 3 Milestone 1 temporal artifact set used for validation, inspection, and phase-stack rendering verification.

`phase3-invalid-derived.*` is the signed malformed Phase 3 reference set used for failure-path verification of deterministic phase derivation.

Committed example artifacts are verification-safe:

- they validate cleanly
- they keep public verification material
- they do not keep reusable private signing keys

Any other example or scratch artifacts should be generated locally and not kept committed as reference material.

Useful verification commands:

```powershell
.\Test-HOPNG.ps1 --path .\examples\phase3-sample.hopng.json --json
.\Show-HOPNG.ps1 --path .\examples\phase3-sample.hopng.json --view prime --json
.\Render-HOPNGPhaseStack.ps1 --path .\examples\phase3-sample.hopng.json --view prime --json
.\Test-HOPNG.ps1 --path .\examples\phase3-invalid-derived.hopng.json --json
.\Render-HOPNGPhaseStack.ps1 --path .\examples\phase3-invalid-derived.hopng.json --view prime --json
```
