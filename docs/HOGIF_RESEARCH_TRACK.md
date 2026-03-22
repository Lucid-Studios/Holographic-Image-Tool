# HOGIF Research Track

This document predicts the research sequence for filling out the future `.hogif` continuity model under Phase 3 Milestone 4.

The goal is to give researchers and implementers a concrete set of questions, deliverables, and stop lines before public carrier work begins.

## Research Posture

Research should treat `.hogif` as:

- a heterochronous continuity carrier
- an excursion-aware identity system
- a validation-first artifact family

Research should not treat `.hogif` as:

- an animation patch
- a renderer-first effort
- a freeform timing sandbox

## Predicted Milestone Sequence

### Milestone 4A: Excursion Doctrine and Identity Lifecycle

Primary question:

- what does it mean for a delta or fragment to leave identity scope without leaving continuity scope

Expected deliverables:

- a stable excursion and re-admission vocabulary
- lifecycle-state definitions
- transition-marker definitions
- initial failure taxonomy for silent exit, silent mutation, and silent return

Research signals of completion:

- the team can distinguish `admitted`, `tracked_external`, `readmitted`, `rejected`, and `quarantined` without ambiguity
- telos evaluation can be described without reference to a specific carrier format

### Milestone 4B: Admissible Identity Morphisms

Primary question:

- what transformation classes can act on identity-bearing fragments without breaking re-admittable continuity

Expected deliverables:

- a named admissible morphism registry draft
- initial morphism classes
- reversibility classes
- a telos-readmission matrix by morphism class

Research signals of completion:

- the team can distinguish `isomorphic`, `deformative_admissible`, `excursive`, and `identity_breaking` behavior without ambiguity
- validator requirements can reference transformation classes, not just marker presence

### Milestone 4C: Async Channel and Sync-Anchor Semantics

Primary question:

- how do channels updating at different cadences remain one lawful continuity object

Expected deliverables:

- per-channel descriptor draft
- finite cadence-mode definitions
- sync-anchor schema sketch
- channel-compatibility and cadence-mismatch taxonomy

Research signals of completion:

- at least one lawful example exists for each cadence mode
- sync anchors can be explained without hidden normalization

### Milestone 4D: Delta Classes, Witness Tiers, and Custody Rules

Primary question:

- how should different truth-status classes behave across excursion, transformation, and re-entry

Expected deliverables:

- stable delta-class definitions
- witness requirement tiers
- custody posture matrix for Prime-safe, privileged, protected, and cryptic material
- rules for class change during transit

Research signals of completion:

- the team can explain why observational, inferred, operator-confirmed, and governance-certified deltas are not interchangeable
- Prime-safe posture remains viable under async continuity

### Milestone 4E: Validator-First Continuity Prototype

Primary question:

- what can be validated before any renderer or binary carrier exists

Expected deliverables:

- JSON example corpus
- malformed-case corpus
- validator rule inventory
- expected diagnostics for excursion and re-entry failures

Research signals of completion:

- a validator can detect silent return, missing transition markers, illegal re-entry, and broken sync-anchor reconstruction
- lawful and unlawful examples are clearly separable

### Milestone 4F: Carrier Boundary and Packaging Decision

Primary question:

- what should `.hogif` actually package once the doctrine is stable

Expected deliverables:

- packaging options analysis
- sidecar-set versus bundled-carrier comparison
- minimum viable carrier contract
- non-goals for first public `.hogif`

Research signals of completion:

- the team can justify why `.hogif` exists as a carrier and not merely as more `.hopng` sidecars
- packaging choice does not weaken provenance or inspection behavior

### Milestone 4G: Operator Surface Prediction

Primary question:

- what should the first operator-facing `.hogif` workflows be

Expected deliverables:

- predicted command surface
- inspection-mode definitions
- reference operator tasks
- testing and smoke-path outline

Predicted first commands:

- `Test-HOGIF.ps1`
- `Show-HOGIF.ps1`
- `Compare-HOGIFContinuity.ps1`

Research signals of completion:

- operator tasks are meaningful without requiring a renderer
- Prime-safe and privileged behavior are clearly distinct

## Predicted Research Dependencies

The likely dependency chain is:

- 4A before 4B because lifecycle semantics must exist before morphism law can govern transformation
- 4B before 4C because async channel semantics should inherit a stable morphism law
- 4C before 4E because validation depends on cadence and sync-anchor rules
- 4D before 4E because witness and custody posture affect what validation can require
- 4E before 4F because the carrier should package validated semantics rather than invent them
- 4F before 4G because the command surface depends on the carrier boundary

## Predicted Reference Corpus

Research should aim to produce:

- one lawful single-excursion example
- one lawful deformative-but-readmittable example
- one lawful multi-channel asynchronous excursion example
- one lawful readmission example with full transit history
- one rejected re-entry example
- one quarantined re-entry example
- one identity-breaking transformation example
- one sync-anchor mismatch example
- one Prime-safe inspection example
- one privileged inspection example

## Predicted Failure Modes

Research should assume these will become first-order failure classes:

- `SilentExit`
- `SilentTransformation`
- `SilentReturn`
- `UnwitnessedTransit`
- `IllegalReadmission`
- `UndeclaredMorphism`
- `FalseReversibilityClaim`
- `CadenceMismatch`
- `SyncAnchorBreak`
- `CustodyLeak`
- `FlattenedContinuity`

## Research Stop Lines

Research should stop and revisit doctrine if:

- lifecycle states cannot be made deterministic
- admissible morphism classes cannot be distinguished cleanly enough to govern validation
- sync anchors require hidden normalization to appear coherent
- Prime-safe inspection cannot explain excursion state without leaking restricted content
- carrier packaging pressures the team to weaken provenance or telos gating

## Predicted Implementation Horizon

If research proceeds cleanly, the likely implementation order after the research track is:

- schema drafts
- validator prototype
- inspection prototype
- reference artifact publication
- carrier writer and reader
- comparison tooling
- optional rendering experiments

That prediction assumes `.hogif` remains a governed continuity carrier first and a media surface second.
