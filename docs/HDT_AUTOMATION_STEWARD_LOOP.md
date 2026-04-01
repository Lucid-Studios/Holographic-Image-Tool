# HDT Automation Steward Loop

This document records the current steward-form execution model for the Holographic Data Tool local automation lane.

It adapts the wider bounded automation steward pattern into a form the HDT lane can actually enforce today.

The goal is not to pretend the full OAN-wide control language is already machine-complete here.

The goal is to make the current HDT lane more honest by explicitly naming:

- root standing
- the current steward stage
- the reconcile -> compile -> classify -> judge -> promote loop
- the emitted triad
- the lighter work-report cadence
- the current contract barriers

## Root Standing

The HDT steward loop now reads the following roots explicitly on each automation run:

- `D:\OAN Tech Stack` as executable standing and target-environment truth
- `D:\Documentation Repo` as lawful doctrine and first documentation-form compile surface
- the local HDT repo as the admitted lane write root
- the local HDT `.audit` tree as the admitted receipt and status surface

This does not make HDT sovereign over those roots.

It means the local automation lane now reconciles those roots deliberately instead of acting as if only its own repo exists.

## Current Steward Stage

The current admitted stage is:

- `S1 WitnessSteward`

That is the highest lawful stage the HDT local lane can honestly claim right now.

It may:

- reconcile standing
- compile bounded local work
- classify the current lane posture
- judge whether the lane is `candidate-ready`, `hitl-required`, or `blocked`
- emit receipts, notices, and work reports

It may not:

- self-authorize final promotion into wider-stack executable truth
- self-authorize release or publication adoption
- widen governance authority by repetition alone

## Canonical Loop

The current machine-expressed loop is:

1. reconcile root standing
2. compile
3. classify
4. judge
5. promote when lawful
6. emit receipts and notices
7. continue unless a stop condition is reached

In the current HDT implementation, the strongest machine support exists for:

- root reconciliation
- bounded compile and verification
- classification and judgment of lane status
- receipted emission of local evidence

The broader OAN control-language ladder remains richer than the current HDT-local machine model.

## Triad

Each meaningful HDT automation run now emits a local triad inside the release-candidate bundle:

- `doping-header`
  what lawfully conditions the run before work begins
- `receipt`
  what happened, what passed, and what standing resulted
- `notice`
  what downstream lanes should assume, prepare for, defer, or escalate

These are emitted in both JSON and Markdown form.

They do not replace the existing manifest or summary.

They complement them by making the steward interpretation explicit.

## Cadence

The HDT local lane now distinguishes:

- `1 hour` work-report cadence
- `6 hour` recommended release-candidate cadence
- `24 hour` mandatory HITL digest cadence

The lane remains manual-first and scheduler-ready.

That means:

- the cadences are now encoded and receipted
- lighter work reports are emitted as a distinct surface
- full unattended scheduling is still outside this repo's current machine authority

## Current Machine-Enforced Surfaces

The current HDT automation lane now machine-emits:

- live cycle state
- live tasking state
- live orchestration state
- release-candidate bundle
- digest bundle
- work-report bundle
- triad artifacts inside the release-candidate bundle

The current public operator wrappers are:

- `Show-HDTAutomationStatus.ps1`
- `Show-HDTAutomationReceipt.ps1`

The current public smoke surfaces are:

- `Invoke-HdtAutomationCycleSmoke.ps1`
- `Invoke-HdtAutomationCycleFailureSmoke.ps1`
- `Invoke-HdtAutomationStatusSmoke.ps1`
- `Invoke-HdtAutomationReceiptSmoke.ps1`

## Current Contract Barriers

The current local barriers remain:

- governance widening is HITL-governed
- final build admission remains outside the HDT local lane
- writes remain bounded to admitted roots
- wider-stack executable authority remains rooted in `OAN Tech Stack`

If one of those barriers is reached materially, the lane should pause for HITL rather than continuing under local repetition alone.

## Honest Boundary

What is real now:

- root standing is reconciled and recorded
- steward stage is recorded
- triad artifacts are emitted
- work reports are emitted
- the HDT lane can now speak in a more lawful steward form

What is not yet fully machine-complete here:

- the full admit / hold / narrow / defer / refuse / return ladder as typed object law
- automatic scheduler enforcement of hourly or 6-hour cadence
- wider-stack promotion authority
- final build admission
