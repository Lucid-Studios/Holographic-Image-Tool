# Local Automation Postures

This document refines the repo-local automation pipeline by adding a phased development-posture ladder for the Holographic Data Tool.

The goal is not to invent a new orchestration system. The goal is to make the existing local verification lane more honest about what kind of claim a given run can support.

The active local parent entrypoint is now [`Invoke-HdtAutomationCycle.ps1`](../scripts/Invoke-HdtAutomationCycle.ps1).

The mechanical child primitive remains [`Invoke-HdtRepoChecks.ps1`](../scripts/Invoke-HdtRepoChecks.ps1).

## Purpose

The posture ladder keeps local automation aligned with research best practices by distinguishing:

- exploratory work from reproducible work
- positive-path verification from adversarial closeout
- mechanical verification from human approval

This prevents the repo from treating every successful local run as if it carried the same evidentiary weight.

## Interlaced Stack Context

As of March 31, 2026, this posture ladder should be read alongside the broader OAN interlace described in `Documentation Repo/architecture/oan-tech-stack-build-interlace-summary.md`.

Inside that interlace:

- `OAN Tech Stack` owns executable present truth and active build readiness
- `Documentation Repo` owns stabilized doctrine, chapter uptake, and publication-facing interpretation
- `GNOMERONACORDE` carries first-run, certification, and pedagogy law in publication form
- `Holographic Data Tool` remains an optional governed `.hopng` validation and inspection lane

That means HDT automation is real, but bounded.

Its posture claims should support local artifact validation, inspection, comparison, and evidence hygiene without pretending to outrank the active executable truth in `OAN Tech Stack`.

The same rule applies to chapter-facing uptake. Chapter `5` through chapter `9` may shape build-facing interpretation through the `Documentation Repo` and `GNOMERONACORDE`, but they should return to build only as admitted contracts, readiness notes, packets, or implementation work.

`.hopng` remains optional for the wider stack right now. HDT automation may therefore strengthen evidence and legibility, but it must not be described as constitutive runtime authority unless the active build lane admits it explicitly.

## Posture Ladder

The current local automation pipeline recognizes four development postures:

1. `Initial`
2. `Formal`
3. `Closing`
4. `Approved`

These are development postures, not semantic roadmap phases. They describe the maturity of the current automation run and the strength of the claim that run can support.

## Initial

`Initial` is the exploratory posture.

It is appropriate when the team is:

- testing a bounded implementation idea
- falsifying a local hypothesis quickly
- iterating before representative corpora or failure cases are ready

The current repo-local automation expectation for `Initial` is:

- `dotnet restore`
- `dotnet build`
- `dotnet test`

Research best-practice correlation:

- keep the hypothesis bounded
- prefer quick falsification over early certainty
- record assumptions before broadening claims
- do not treat exploratory verification as release evidence

## Formal

`Formal` is the reproducible development posture.

It is appropriate when the team is:

- moving from exploratory implementation into repeatable evidence
- validating representative positive-path artifacts
- making public or operator-facing claims about the current tool surface

The current repo-local automation expectation for `Formal` is:

- everything in `Initial`
- Phase 2 smoke verification
- Phase 3 release smoke verification
- Phase 3 comparison smoke verification
- Phase 4 entry positive-path smoke verification
- Phase 4 support comparison positive-path smoke verification

Research best-practice correlation:

- use explicit protocol rather than ad hoc local checks
- test against representative reference corpora
- keep claims tied to reproducible evidence
- preserve a documented path that another operator can re-run

## Closing

`Closing` is the pre-promotion closeout posture.

It is appropriate when the team is:

- preparing a release candidate, promotion decision, or architecture checkpoint
- trying to prove the lane is not only working, but bounded correctly
- checking negative controls and failure-path behavior before claiming stability

The current repo-local automation expectation for `Closing` is:

- everything in `Formal`
- Phase 3 comparison failure smoke verification
- Phase 3 malformed or unsupported failure smoke verification
- Phase 4 entry failure smoke verification
- Phase 4 support comparison failure smoke verification

Research best-practice correlation:

- include disconfirming evidence, not only happy-path success
- run negative controls and adversarial boundary cases
- verify deterministic failure behavior where the contract demands it
- freeze the claim only after positive and negative-path evidence agree

## Approved

`Approved` is the operator-approved adoption posture.

It is appropriate when:

- the mechanical verification surface is complete
- the release or promotion record is coherent
- explicit HITL approval has been granted

The current repo-local automation expectation for `Approved` is:

- the full `Closing` verification chain
- explicit acknowledgment that automation verifies mechanics only
- separate human approval for release, promotion, or formal adoption

Research best-practice correlation:

- preserve reviewable receipts for the exact approved surface
- separate verification from adoption authority
- keep the approved corpus and documentation stable enough to cite
- do not allow automation success alone to imply governance approval

## Current Script Surface

The local parent cycle is:

```powershell
.\scripts\Invoke-HdtAutomationCycle.ps1 -DevelopmentPosture Closing
```

That parent script owns:

- release-candidate bundles
- digest bundles
- work-report bundles
- live `.audit` state
- tasking and orchestration status surfaces
- the primary automation visibility surface through [`Show-HDTAutomationStatus.ps1`](../Show-HDTAutomationStatus.ps1)
- the primary automation receipt surface through [`Show-HDTAutomationReceipt.ps1`](../Show-HDTAutomationReceipt.ps1)
- the steward triad inside each release-candidate bundle

The child mechanical verifier remains:

[`Invoke-HdtRepoChecks.ps1`](../scripts/Invoke-HdtRepoChecks.ps1), which now accepts:

```powershell
.\scripts\Invoke-HdtRepoChecks.ps1 -DevelopmentPosture Initial
.\scripts\Invoke-HdtRepoChecks.ps1 -DevelopmentPosture Formal
.\scripts\Invoke-HdtRepoChecks.ps1 -DevelopmentPosture Closing
.\scripts\Invoke-HdtRepoChecks.ps1 -DevelopmentPosture Approved
```

The default posture is `Closing`, which preserves the repo's existing full-check behavior.

If a run requests a posture but also tries to skip verification that posture requires, the script fails rather than silently downgrading the claim.

## Boundaries

This posture ladder does not:

- replace operator approval
- reinterpret the semantic roadmap
- turn Milestone 4 research into implementation authority
- grant permission to skip required release checks while still claiming a stronger posture

It does:

- give the local automation pipeline a truthful maturity ladder
- keep automation claims proportional to evidence strength
- align repo-local execution with research best practices
- keep HDT automation subordinate to the broader executable and doctrinal split when the wider OAN stack is present

## Relationship To Existing Governance

This posture model complements:

- [`HDT_AUTOMATION_LANE.md`](./HDT_AUTOMATION_LANE.md)
- [`OPERATOR_CONTINUITY_INSTRUCTIONS.md`](./OPERATOR_CONTINUITY_INSTRUCTIONS.md)
- [`DEVELOPMENT_PATH.md`](./DEVELOPMENT_PATH.md)
- [`PHASE_BACKLOG.md`](./PHASE_BACKLOG.md)
- [`PHASE_3_RELEASE_READY.md`](./PHASE_3_RELEASE_READY.md)

The operator cascade function still governs when work may continue without HITL interruption.

The automation posture ladder governs how strong a local verification claim is allowed to be.

The parent automation lane turns those posture claims into receipted `.audit` state, but it does not replace the child mechanical verification surface.
