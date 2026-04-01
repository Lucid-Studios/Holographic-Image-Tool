# HDT Automation Lane

This document defines the first end-to-end local automation lane for the Holographic Data Tool.

The goal is not to reproduce the full `OAN Tech Stack` conveyor. The goal is to give HDT one truthful local parent cycle that can:

- run the posture-aware repo checks
- reconcile root standing against `OAN Tech Stack` and `Documentation Repo`
- emit release-candidate receipts into a local `.audit/` tree
- keep live state and status surfaces current
- emit a local steward triad inside each release-candidate bundle
- emit lighter work reports on their own cadence
- emit operator-readable digests when a digest window is due or explicitly forced

The lane is manual-first and scheduler-ready.

It strengthens local artifact evidence, validation, inspection, and comparison without claiming wider-stack promotion or runtime authority.

## Three Layers

### 1. Policy Layer

The local contracts live under `build/`:

- `build/local-automation-cycle.json`
- `build/local-automation-tasking.json`
- `build/master-thread-orchestration.json`

Those files define:

- output roots
- live state paths
- advisory cadence fields
- root standing for admitted external roots
- current steward stage and canonical loop
- task map and task labels
- advisory branch and worktree expectations
- the bounded continuation statuses for the v1 lane

### 2. Execution Layer

The parent entrypoint is:

```powershell
.\scripts\Invoke-HdtAutomationCycle.ps1 -DevelopmentPosture Closing
```

That cycle:

1. loads the local automation contracts
2. reconciles root standing against the admitted external roots
3. reads the prior cycle state when available
4. runs `Invoke-HdtRepoChecks.ps1` as the mechanical child primitive
5. captures git branch and worktree metadata
6. writes a release-candidate bundle and steward triad
7. writes the live cycle state
8. emits a work report when forced or due
9. emits a digest when forced or due
10. writes tasking and orchestration status surfaces

The child primitive remains:

```powershell
.\scripts\Invoke-HdtRepoChecks.ps1 -DevelopmentPosture Closing
```

That script stays responsible for restore, build, tests, and smoke paths. The parent automation cycle is responsible for receipts, state surfaces, and operator-readable automation posture.

### 3. Visibility Layer

The live surfaces are:

- `.audit/state/local-automation-cycle.json`
- `.audit/state/local-automation-tasking-status.json`
- `.audit/state/local-automation-tasking-status.md`
- `.audit/state/master-thread-orchestration-status.json`
- `.audit/state/master-thread-orchestration-status.md`

The public visibility wrapper for those surfaces is:

```powershell
.\Show-HDTAutomationStatus.ps1 -View all
```

That wrapper can emit either:

- operator-readable status text
- combined JSON for the current cycle, tasking, and orchestration posture

The public receipt wrapper for the latest or requested emitted evidence is:

```powershell
.\Show-HDTAutomationReceipt.ps1 -View all
```

That wrapper can emit either:

- operator-readable receipt text
- combined JSON for the current release-candidate bundle and digest

The run receipts live under:

- `.audit/runs/release-candidates/<bundle-id>/`
- `.audit/runs/release-digests/<bundle-id>/`
- `.audit/runs/work-reports/<bundle-id>/`

Each release-candidate bundle contains:

- `build-evidence-manifest.json`
- `build-evidence-summary.md`
- `repo-checks-receipt.json`
- `git-worktree-receipt.json`
- `doping-header.json`
- `doping-header.md`
- `receipt.json`
- `receipt.md`
- `notice.json`
- `notice.md`

Each digest bundle contains:

- `release-candidate-digest.json`
- `release-candidate-digest.md`

Each work-report bundle contains:

- `work-report.json`
- `work-report.md`

## Status Classes

The v1 automation lane uses three status classes:

- `candidate-ready`
- `hitl-required`
- `blocked`

Meaning:

- `candidate-ready`: the local mechanical lane succeeded and may continue mechanically
- `hitl-required`: the requested posture succeeded mechanically, but explicit HITL adoption is still owed
- `blocked`: repo checks failed, the local contracts were unreadable, or the receipt/state write path could not remain coherent

`Approved` posture is intentionally mapped to `hitl-required`, not to self-granted approval.

## Advisory Orchestration

The v1 orchestration surface is status-only.

It records:

- current branch
- current worktree state
- required published branch
- required clean-worktree posture
- advisory later handoff eligibility

Those signals are advisory in the first HDT lane. They do not block a local cycle by themselves.

## Steward Loop

The HDT automation lane now records the current admitted steward shape:

- `S1 WitnessSteward`
- root standing reconciliation
- classify / judge / promote-when-lawful loop tracking
- triad emission on meaningful runs
- lighter work-report cadence distinct from the heavier digest cadence

See [`HDT_AUTOMATION_STEWARD_LOOP.md`](./HDT_AUTOMATION_STEWARD_LOOP.md) for the current admitted steward boundary and the explicit line between machine-enforced behavior and doctrine-first future control language.

## Interlaced Boundary

Inside the wider OAN split:

- `OAN Tech Stack` remains executable truth
- `Documentation Repo` remains stabilized doctrine and uptake truth
- `GNOMERONACORDE` remains first-run, certification, and pedagogy carriage
- `HDT` remains an optional governed `.hopng` validation, inspection, and evidence lane

That means this automation lane is real, but bounded.

It may strengthen:

- local artifact evidence
- repo-local validation receipts
- inspection and comparison legibility
- operator-readable state and digest surfaces

It may not claim:

- wider-stack promotion authority
- runtime identity authority
- mandatory `.hopng` dependence for the active executable lane
- branch or worktree truth override by local receipt alone

## Verification Surface

The cycle is verified in two ways:

- the child primitive still runs the actual build, test, and smoke graph
- the public parent lane now has dedicated manual smoke surfaces:
  - `scripts/Invoke-HdtAutomationCycleSmoke.ps1`
  - `scripts/Invoke-HdtAutomationCycleFailureSmoke.ps1`
- the public visibility layer now has a dedicated smoke surface:
  - `scripts/Invoke-HdtAutomationStatusSmoke.ps1`
- the public receipt layer now has a dedicated smoke surface:
  - `scripts/Invoke-HdtAutomationReceiptSmoke.ps1`
- dedicated automation-cycle tests verify success, blocked failure, and invalid audit-root fallback behavior without recursively calling `dotnet test` from inside `dotnet test`

This keeps the lane honest:

- repo checks remain the mechanical truth source
- the parent cycle remains the receipt and state source
- automation posture remains bounded by evidence rather than narrative
