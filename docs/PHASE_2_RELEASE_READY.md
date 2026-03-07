# Phase 2 Release-Ready Criteria

This document defines the internal-first stop line for declaring the current Holographic Data Tool implementation operational at Phase 2.

## Supported Command Surface

The supported operator surface for this release target is:

- `New-HOPNG.ps1`
- `Test-HOPNG.ps1`
- `Show-HOPNG.ps1`
- `Merge-HOPNGLayers.ps1`
- `Compare-HOPNGSurfaces.ps1`

Later-phase wrappers remain intentionally reserved and are not part of the Phase 2 operational claim.

## Expected Exit Codes

- `0`: successful validation or lawful projection support
- validation error code from the core validator: malformed or trust-broken artifact input
- `24`: structurally incomplete derivation or comparison result
- `25`: flattened or unsupported derivation or comparison result

## Reference Artifact Sets

Committed examples should contain exactly:

- one clean Phase 1 reference set
- one clean Phase 2 reference set

Reference artifacts must be verification-safe:

- signed and validate cleanly
- include public verification material
- exclude reusable private signing keys

## Internal Smoke Path

Phase 2 release-ready verification must prove:

1. a new artifact can be created through the public PowerShell wrapper
2. the artifact validates through `Test-HOPNG.ps1`
3. the artifact can be inspected through `Show-HOPNG.ps1`
4. the Phase 2 reference artifact returns a lawful result through `Merge-HOPNGLayers.ps1`
5. the Phase 2 reference artifact compares against the Phase 1 reference artifact through `Compare-HOPNGSurfaces.ps1`

## Known Non-Goals

This release target does not include:

- event or phase slices
- phase policy or optical-channel semantics
- engram forms
- commitment or formation artifacts
- Sanctuary or OE runtime binding

## Acceptance Criteria

Phase 2 is release-ready when all of the following are true:

- repository docs match the actual CLI and wrapper behavior
- example artifacts are intentional, clean, and safe to keep committed
- local and CI build or test automation exist
- merge and comparison outcomes are documented
- the public PowerShell surface can demonstrate create, validate, inspect, merge, and compare behavior without hidden manual steps
