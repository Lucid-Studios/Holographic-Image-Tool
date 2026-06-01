# Holographic Data Tool Build List

This document is the compact execution summary for the repo. It complements the semantic roadmap in [`docs/PHASE_ROADMAP.md`](./docs/PHASE_ROADMAP.md), the lane model in [`docs/DEVELOPMENT_PATH.md`](./docs/DEVELOPMENT_PATH.md), and the detailed backlog in [`docs/PHASE_BACKLOG.md`](./docs/PHASE_BACKLOG.md).

## Current Baseline

Implemented and maintained:

- Phase 1 trusted artifact foundation
- Phase 2 lawful relationality and governed projection support
- Phase 3 Milestone 1 temporal legality
- Phase 3 Milestone 2 temporal state maturity
- Phase 3 Milestone 3 governed cross-artifact comparison baseline

Current active gate:

- post-release maintenance of the approved Phase 3 single-clock temporal surface
- executable Phase 4 entry cascade through validator-first engram scaffolding
- manual-first, scheduler-ready HDT local automation lane for release-candidate receipts and live `.audit` state
- steward-form HDT local automation slice for root-standing reconciliation, triad emission, and lighter work reports

Current promotion-prep gate:

- validator-first promotion prep for Phase 3 Milestone 4 heterochronous research

Current wider-stack read:

- HDT contributes an optional governed artifact lane inside the broader `OAN Tech Stack` / `Documentation Repo` / `GNOMERONACORDE` split
- the current milestone map for that split is summarized in [`docs/INTERLACED_MILESTONE_MAPPING.md`](./docs/INTERLACED_MILESTONE_MAPPING.md)

## Active Tool Surface

Stable operator-facing wrappers now include:

- `New-HOPNG.ps1`
- `Test-HOPNG.ps1`
- `Show-HOPNG.ps1`
- `Merge-HOPNGLayers.ps1`
- `Compare-HOPNGSurfaces.ps1`
- `New-HOPNGPhase3Sample.ps1`
- `New-HOPNGPhase3PeerSample.ps1`
- `New-HOPNGPhase3DivergentPeerSample.ps1`
- `New-HOPNGPhase3IncompatibleBasisSample.ps1`
- `New-HOPNGPhase3InvalidSample.ps1`
- `New-HOPNGPhase4PerspectivalSample.ps1`
- `New-HOPNGPhase4PerspectivalPeerSample.ps1`
- `New-HOPNGPhase4RestrictedPerspectivalSample.ps1`
- `New-HOPNGPhase4DeferredPerspectivalSample.ps1`
- `New-HOPNGPhase4ParticipatorySample.ps1`
- `New-HOPNGPhase4ParticipatoryPeerSample.ps1`
- `New-HOPNGPhase4RejectedParticipatorySample.ps1`
- `New-HOPNGPhase4InvalidPerspectivalSample.ps1`
- `New-HOPNGPhase4InvalidParticipatorySample.ps1`
- `Render-HOPNGPhaseStack.ps1`
- `Compare-HOPNGPhaseStacks.ps1`
- `Compare-HOPNGEngramSupport.ps1`
- `Show-HDTAutomationStatus.ps1`
- `Show-HDTAutomationReceipt.ps1`

Repo-local verification paths now include:

- `scripts/Invoke-HdtAutomationCycle.ps1`
- `scripts/Invoke-HdtAutomationStatusSmoke.ps1`
- `scripts/Invoke-HdtAutomationReceiptSmoke.ps1`
- `scripts/Invoke-HdtAutomationCycleSmoke.ps1`
- `scripts/Invoke-HdtAutomationCycleFailureSmoke.ps1`
- `scripts/Invoke-HdtRepoChecks.ps1`
- `scripts/Invoke-Phase2ReleaseSmoke.ps1`
- `scripts/Invoke-Phase3ReleaseSmoke.ps1`
- `scripts/Invoke-Phase3ComparisonSmoke.ps1`
- `scripts/Invoke-Phase3ComparisonFailureSmoke.ps1`
- `scripts/Invoke-Phase3FailureSmoke.ps1`
- `scripts/Invoke-Phase4EntrySmoke.ps1`
- `scripts/Invoke-Phase4EntryFailureSmoke.ps1`
- `scripts/Invoke-Phase4SupportComparisonSmoke.ps1`
- `scripts/Invoke-Phase4SupportComparisonFailureSmoke.ps1`

`Invoke-HdtAutomationCycle.ps1` is now the canonical local automation parent for HDT. It wraps `Invoke-HdtRepoChecks.ps1`, writes `.audit` receipts and digests, and keeps live cycle, tasking, and orchestration state current.

`Show-HDTAutomationStatus.ps1` is now the canonical local automation visibility wrapper for HDT. It reads the live `.audit` state and exposes the current cycle, tasking, and orchestration posture as either operator-readable text or combined JSON.

`Show-HDTAutomationReceipt.ps1` is now the canonical local automation receipt wrapper for HDT. It reads the latest or requested emitted release-candidate bundle and digest from `.audit` and exposes that evidence surface as either operator-readable text or combined JSON.

The parent automation lane now also emits a steward triad (`doping-header`, `receipt`, `notice`) plus lighter work-report bundles, while remaining bounded inside the HDT local lane.

`Invoke-HdtRepoChecks.ps1` remains the mechanical child primitive and supports `Initial`, `Formal`, `Closing`, and `Approved` development postures so local automation claims can scale with the maturity of the evidence being produced.

The dedicated parent-lane smoke pair stays separate from `Invoke-HdtRepoChecks.ps1` so the automation conveyor can be verified without recursive self-invocation.

The dedicated automation-status smoke stays separate from `Invoke-HdtRepoChecks.ps1` as well, because it verifies the public visibility layer over already-emitted `.audit` state rather than the mechanical child primitive itself.

The dedicated automation-receipt smoke stays separate for the same reason: it verifies the public emitted-evidence surface over `.audit` bundles rather than the mechanical child primitive itself.

## Main-Lane Priorities

The main lane should currently prioritize:

- preserving the approved Phase 3 single-clock release baseline
- preserving the executable Phase 4 entry bridge as support-only and validator-first
- preserving the new `.audit` automation lane as a truthful receipt surface rather than a counterfeit promotion surface
- preserving Phase 4 lawful and unlawful support corpora as committed public-safe references before stronger identity claims are allowed
- preserving Phase 4 support comparison as a wrapper-backed and corpus-backed surface for strengthened support, branch coherence, lawful negative support states, and counterfeit pressure
- keeping the working-intent stance ladder machine-checkable before Phase 5 review semantics begin
- defining the bounded Engineered Cognition capture-bundle and deterministic render-policy contract needed to replace placeholder PNG projections with lawful signed image output
- keeping the Sanctuary EC lab bridge readable from the GitHub repo so ChatGPT-app review can understand HDT as a holographic slice and inspection lane without local PC file access
- keeping HDT aligned with the wider OAN interlace so `OAN Tech Stack` remains executable truth, `Documentation Repo` remains doctrine truth, and `.hopng` stays optional and bounded until further promotion
- keeping temporal examples public-safe and deterministic
- expanding lawful and unlawful temporal comparison reference sets
- improving operator readability and explicit diagnostics without weakening validation posture
- keeping docs, wrappers, tests, and smoke paths aligned

## Promoted Planning Constraints

These planning items are now mature enough to constrain future work even though they are not yet active implementation targets:

- honest substrate principles
- operator cascade governance
- heterochronous timing doctrine
- admissible identity morphism law
- Sanctuary EC holographic slice and inspection bridge
- `.hogif` build governance and research sequence

They should shape boundaries and promotion decisions, but they should not be mistaken for carrier-ready implementation.

## Deferred Work

Keep these out of the active lane until the current promotion gate is complete:

- `.hogif` schema or package implementation
- async renderer or media behavior
- Phase 4 admission-bearing or runtime engrammatic structures
- Phase 5 formation or commitment flows
- Phase 6 OE or Sanctuary runtime binding

## No-Decay Conditions

- do not describe active implementation as merely planned
- do not describe research doctrine as already operational
- keep wrapper-backed smoke equal to the public tool surface
- keep committed examples free of reusable private keys
- promote new work through validation, reference artifacts, and inspection behavior before expanding claims
