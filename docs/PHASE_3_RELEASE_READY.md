# Phase 3 Release Ready

This document defines the stop line for declaring Phase 3 operationally ready before the project advances into Phase 4.

Phase 3 is only release-ready when temporal semantics are not just implemented, but also documented, testable, operator-safe, and stable enough that later identity-bearing work does not need to reinterpret them.

## Current Status

The single-clock Phase 3 release baseline was operator-approved on March 30, 2026.

This document now serves as both:

- the release stop line for the single-clock temporal surface
- the approved release record for that baseline

The approved Phase 3 release scope includes:

- single-artifact temporal rendering and deterministic state classification
- explicit duration-window, max-span, threshold, and comparison-horizon policy
- governed cross-artifact comparison with `Convergent`, `Delayed`, `Divergent`, `Incompatible`, and `FlattenedOrUnsupported` outcomes
- human-readable and JSON operator surfaces with deterministic exit codes
- committed public-safe reference artifacts and wrapper-backed smoke verification

## Interlaced Build Meaning

Inside the wider OAN stack, this approved Phase 3 baseline should currently be read as an optional governed artifact evidence surface.

That means it is mature enough to support:

- local `.hopng` validation and inspection
- temporal evidence comparison
- optional supplementary readiness or packet evidence when admitted

It does not yet mean:

- mandatory runtime dependence in `OAN Tech Stack`
- constitutive authority over executable truth
- automatic promotion of `.hopng` into wider-stack runtime law

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

Planned Milestone 4 heterochronous channel work is intentionally outside this approved single-clock release baseline.

## Required Operator Surface

By release-ready, the public temporal operator surface should include:

- `Render-HOPNGPhaseStack.ps1`
- `Compare-HOPNGPhaseStacks.ps1`

The public surface should provide:

- human-readable output
- JSON output
- deterministic exit codes
- Prime-safe and privileged modes

Human-readable temporal output should remain operator-meaningful without forcing JSON mode. `Render-HOPNGPhaseStack.ps1` should expose final state posture, horizon summaries, and issue counts. `Compare-HOPNGPhaseStacks.ps1` should expose basis alignment, final state posture, state-rank delta, classification reason, comparable slice count, and basis or signal summaries.

## Reference Artifacts

Release-ready requires intentional public-safe reference artifacts for:

- one valid Milestone 1 temporal artifact
- one valid Milestone 2 temporal-state artifact
- one valid cross-artifact comparison pair
- one lawful divergent comparison peer
- one lawful but basis-incompatible comparison artifact
- one malformed or unsupported temporal artifact for failure-path verification

No committed private signing keys should exist in reference artifacts.

The current hardening baseline already keeps `examples/phase3-sample.*` as the public-safe Milestone 2 temporal reference set, `examples/phase3-peer-sample.*` as the lawful delayed comparison peer, `examples/phase3-divergent-peer.*` as the lawful divergent comparison peer, `examples/phase3-incompatible-basis.*` as the lawful incompatible-basis comparison artifact, and `examples/phase3-invalid-derived.*` as the signed malformed temporal reference set.

## Verification Gates

Required verification:

- `dotnet build HolographicDataTool.sln`
- `dotnet test Hdt.Tests\Hdt.Tests.csproj`
- public wrapper smoke path for:
  - temporal render
  - temporal state classification
  - cross-artifact temporal comparison
  - cross-artifact basis-incompatibility failure
  - cross-artifact flattened-or-unsupported failure
- documented Prime-safe verification path

The current repo-local smoke path should remain wrapper-backed and artifact-oriented, rather than relying only on filtered test execution. Failure-path verification should also remain artifact-backed so deterministic temporal contract failures stay reproducible.

The wrapper-backed smoke path should verify both JSON and human-readable operator output for the public temporal commands, so release readiness does not silently validate only the machine-readable branch.

For local automation, the minimum truthful verification posture for this release surface is `Closing`. `Approved` may use the same full mechanical verification chain, but still requires explicit HITL approval before release or adoption is claimed.

## Documentation Gates

Release-ready documentation must include:

- updated `README.md`
- updated `ARTIFACT_MODEL.md`
- updated `PHASE_BACKLOG.md`
- updated `PHASE_ROADMAP.md`
- milestone docs for Milestones 1 through 3
- operator-facing notes on temporal exit codes and view modes

The current exit-code contract for the public temporal surface is:

- `Render-HOPNGPhaseStack.ps1`: `0` lawful temporal derivation, `24` structurally incomplete temporal derivation, `25` unsupported temporal surface
- `Compare-HOPNGPhaseStacks.ps1`: `0` aligned comparison result, `24` basis-incompatible comparison, `25` flattened, unsupported, or invalid temporal comparison surface

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

The bounded bridge that begins after this stop line is recorded in [`PHASE_4_ENTRY_CRITERIA.md`](./PHASE_4_ENTRY_CRITERIA.md).
