# Example Artifacts

`phase1-sample.*` is the clean reference Phase 1 artifact set.

`phase2-sample.*` is the clean reference Phase 2 artifact set used for merge and comparison verification.

`phase3-sample.*` is the clean reference Phase 3 temporal artifact set used for validation, inspection, and phase-stack rendering verification, including explicit comparison horizons for widened-horizon diagnostics.

`phase3-peer-sample.*` is the clean reference Phase 3 temporal comparison peer used for lawful cross-artifact basis alignment and delayed-comparison verification.

`phase3-divergent-peer.*` is the clean reference Phase 3 temporal comparison peer used for lawful cross-artifact basis alignment and divergent-comparison verification.

`phase3-incompatible-basis.*` is the clean reference Phase 3 temporal artifact set used for lawful but basis-incompatible comparison verification.

`phase3-invalid-derived.*` is the signed malformed Phase 3 reference set used for failure-path verification of deterministic phase derivation.

`phase4-perspectival-sample.*` is the clean reference Phase 4 perspectival-support artifact set used for lawful entry validation, Prime-safe inspection, and inherited temporal rendering verification.

`phase4-perspectival-peer.*` is the clean reference Phase 4 perspectival-support comparison peer used for lawful strengthened-support verification.

`phase4-restricted-perspectival.*` is the clean reference Phase 4 perspectival-support artifact set used for lawful restricted-support verification, Prime-safe state-reason inspection, and machine-checked transition-marker verification.

`phase4-deferred-perspectival.*` is the clean reference Phase 4 perspectival-support artifact set used for lawful deferred-support verification, Prime-safe state-reason inspection, and machine-checked transition-marker verification.

`phase4-participatory-sample.*` is the clean reference Phase 4 participatory-support artifact set used for lawful entry validation, Prime-safe inspection, and inherited temporal rendering verification.

`phase4-participatory-peer.*` is the clean reference Phase 4 participatory-support comparison peer used for lawful branch-coherence verification.

`phase4-rejected-participatory.*` is the clean reference Phase 4 participatory-support artifact set used for lawful rejected-support verification, Prime-safe state-reason inspection, and machine-checked transition-marker verification.

`phase4-invalid-perspectival.*` is the signed malformed Phase 4 reference set used for failure-path verification of unsupported perspectival overclaim.

`phase4-invalid-participatory.*` is the signed malformed Phase 4 reference set used for failure-path verification of unsupported participatory branch or handoff claims.

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
.\Compare-HOPNGPhaseStacks.ps1 --left .\examples\phase3-sample.hopng.json --right .\examples\phase3-peer-sample.hopng.json --view prime --json
.\Compare-HOPNGPhaseStacks.ps1 --left .\examples\phase3-sample.hopng.json --right .\examples\phase3-divergent-peer.hopng.json --view prime --json
.\Compare-HOPNGPhaseStacks.ps1 --left .\examples\phase3-sample.hopng.json --right .\examples\phase3-incompatible-basis.hopng.json --view prime --json
.\Test-HOPNG.ps1 --path .\examples\phase3-invalid-derived.hopng.json --json
.\Render-HOPNGPhaseStack.ps1 --path .\examples\phase3-invalid-derived.hopng.json --view prime --json
.\Test-HOPNG.ps1 --path .\examples\phase4-perspectival-sample.hopng.json --json
.\Show-HOPNG.ps1 --path .\examples\phase4-perspectival-sample.hopng.json --view prime --json
.\Compare-HOPNGEngramSupport.ps1 --left .\examples\phase4-perspectival-sample.hopng.json --right .\examples\phase4-perspectival-peer.hopng.json --view prime --json
.\Compare-HOPNGEngramSupport.ps1 --left .\examples\phase4-perspectival-sample.hopng.json --right .\examples\phase4-restricted-perspectival.hopng.json --view prime --json
.\Compare-HOPNGEngramSupport.ps1 --left .\examples\phase4-perspectival-sample.hopng.json --right .\examples\phase4-deferred-perspectival.hopng.json --view prime --json
.\Test-HOPNG.ps1 --path .\examples\phase4-participatory-sample.hopng.json --json
.\Show-HOPNG.ps1 --path .\examples\phase4-participatory-sample.hopng.json --view prime --json
.\Compare-HOPNGEngramSupport.ps1 --left .\examples\phase4-participatory-sample.hopng.json --right .\examples\phase4-participatory-peer.hopng.json --view prime --json
.\Compare-HOPNGEngramSupport.ps1 --left .\examples\phase4-participatory-sample.hopng.json --right .\examples\phase4-rejected-participatory.hopng.json --view prime --json
.\Test-HOPNG.ps1 --path .\examples\phase4-invalid-perspectival.hopng.json --json
.\Test-HOPNG.ps1 --path .\examples\phase4-invalid-participatory.hopng.json --json
```
