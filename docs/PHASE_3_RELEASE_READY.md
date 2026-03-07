# Phase 3 Release Ready

This document defines the stop line for declaring Phase 3 operationally ready before the project advances into Phase 4.

Phase 3 is only release-ready when temporal semantics are not just implemented, but also documented, testable, operator-safe, and stable enough that later identity-bearing work does not need to reinterpret them.

## Release Target

Phase 3 release-ready means:

- single-artifact temporal semantics are complete and stable
- cross-artifact temporal comparison is available under explicit basis rules
- operator documentation matches implementation
- Prime-safe temporal handling is verified
- reference artifacts and smoke paths exist

## Required Milestones

Phase 3 release-ready assumes completion of:

- [`PHASE_3_MILESTONE_1.md`](./PHASE_3_MILESTONE_1.md)
- [`PHASE_3_MILESTONE_2.md`](./PHASE_3_MILESTONE_2.md)
- [`PHASE_3_MILESTONE_3.md`](./PHASE_3_MILESTONE_3.md)

## Required Operator Surface

By release-ready, the public temporal operator surface should include:

- `Render-HOPNGPhaseStack.ps1`
- a governed cross-artifact temporal comparison command

The public surface should provide:

- human-readable output
- JSON output
- deterministic exit codes
- Prime-safe and privileged modes

## Reference Artifacts

Release-ready requires intentional public-safe reference artifacts for:

- one valid Milestone 1 temporal artifact
- one valid Milestone 2 temporal-state artifact
- one valid cross-artifact comparison pair
- one malformed or unsupported temporal artifact for failure-path verification

No committed private signing keys should exist in reference artifacts.

## Verification Gates

Required verification:

- `dotnet build HolographicDataTool.sln`
- `dotnet test Hdt.Tests\Hdt.Tests.csproj`
- public wrapper smoke path for:
  - temporal render
  - temporal state classification
  - cross-artifact temporal comparison
- documented Prime-safe verification path

## Documentation Gates

Release-ready documentation must include:

- updated `README.md`
- updated `ARTIFACT_MODEL.md`
- updated `PHASE_BACKLOG.md`
- updated `PHASE_ROADMAP.md`
- milestone docs for Milestones 1 through 3
- operator-facing notes on temporal exit codes and view modes

## Safety Gates

Release-ready must preserve:

- Prime-safe temporal metadata posture
- protected-evidence custody separation
- deterministic slice and bundle integrity
- explicit basis alignment for cross-artifact comparison
- no hidden interpolation or synthetic temporal invention

## Explicit Stop Line Before Phase 4

Do not begin engram implementation until:

- Phase 3 policies are stable
- temporal state classes are stable
- cross-artifact comparison semantics are stable
- temporal docs and reference artifacts are clean enough that later identity semantics do not have to redefine them
