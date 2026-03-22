# Phase 3 Milestone 4: Heterochronous Temporal Channel Model

Phase 3 Milestone 4 defines a planned extension track for temporal artifacts whose channels do not all evolve at the same cadence.

The purpose of this milestone is not to introduce a new public media container immediately. The purpose is to define the timing doctrine, channel ontology, and validation posture required before any later `.hogif` carrier, renderer, or encoder work is attempted.

This milestone treats the timing model as the invention. A future carrier expression such as `.hogif` is only the boundary where that doctrine becomes concrete.

## Intent

Milestone 4 extends the temporal system from single-clock lawful evidence stacking into governed heterochronous continuity.

It should answer questions like:

- how one artifact can preserve channels that update at different rates without collapsing them into one synthetic clock
- how asynchronous channels rejoin one lawful continuity object through explicit sync anchors
- how different classes of change can carry different admissibility status inside one artifact
- how Prime-safe and privileged inspection remain lawful when some channels are sparse, protected, derived, or witness-gated

The doctrinal center is:

> a continuity object composed of asynchronously updating channels under lawful sync anchors

## Relationship to Current Phase 3

Current Phase 3 remains the single-clock temporal layer for `.hopng`.

The currently implemented model still assumes:

- one observed-set basis
- one base raw cadence
- one event grouping basis
- one derived phase-window basis
- deterministic contiguous windows without interpolation

Milestone 4 does not reinterpret existing Phase 3 artifacts or weaken their current validation rules.

`.hopng` therefore remains:

- the crystallized posture artifact
- the current single-clock lawful evidence stack

Milestone 4 defines the asynchronous doctrine that must exist before any future heterochronous carrier is implemented.

Related planning documents:

- [`HONEST_SUBSTRATE_PRINCIPLES.md`](./HONEST_SUBSTRATE_PRINCIPLES.md)
- [`HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md`](./HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md)
- [`HOGIF_BUILD_GOVERNANCE.md`](./HOGIF_BUILD_GOVERNANCE.md)
- [`HOGIF_RESEARCH_TRACK.md`](./HOGIF_RESEARCH_TRACK.md)

## Scope

Milestone 4 includes:

- heterochronous channel ontology
- lawful sync-anchor semantics
- finite cadence modes for asynchronous channels
- delta classes with explicit admissibility meaning
- validation-before-rendering rules for async continuity
- Prime-safe and privileged inspection posture for asynchronous channel artifacts
- compatibility boundaries between current `.hopng` temporal semantics and any future heterochronous carrier

Milestone 4 does not include:

- immediate `.hogif` implementation as a public file type
- renderer-first or animation-first work
- arbitrary freeform timing semantics
- hidden normalization that erases cadence mismatch
- synthetic interpolation or silent backfilling
- OE, Sanctuary, engrammatic, covenant, or runtime semantics

## Channel Model

An asynchronous artifact should declare a master timing basis plus per-channel descriptors.

The master basis should remain explicit so all channel-local deltas can still be anchored into one governed history.

Each channel descriptor should declare:

- `channelType`
- `cadenceMode`
- `syncPolicy`
- `deltaEncoding`
- `encryptionScope`
- `witnessRequirement`

Additional descriptor metadata should remain explicit where needed, including:

- channel identity and semantic role
- whether the channel is observed, derived, operator-authored, or governance-authored
- custody posture and disclosure posture
- threshold policy when cadence is threshold-driven
- checkpoint policy when cadence is checkpoint-driven

## Cadence Modes

Milestone 4 should begin with a finite set of cadence modes rather than arbitrary timing freedom.

Initial cadence set:

- `fixed_rate`
- `event_rate`
- `threshold_rate`
- `checkpoint_rate`

The point of these modes is to preserve expressive power without turning temporal validation into undefined behavior.

## Sync Anchors

Asynchronous channels must still participate in one reconstructable continuity object.

Milestone 4 should therefore define lawful sync anchors that record:

- the global cycle or continuity index at which reconciliation occurs
- the channels participating in the anchor
- the scope of the admitted change set
- the witness or evidence requirements attached to that anchor
- any protected or cryptic references needed to substantiate the anchor

Sync anchors are the place where independent channel deltas are admitted back into one lawful record.

## Delta Classes

Milestone 4 should distinguish kinds of change rather than treating all deltas as equal-status truth.

Initial delta classes:

- `observational`
- `inferred`
- `operator_confirmed`
- `governance_certified`
- `cryptically_sealed`

These classes should affect admissibility, inspection posture, and witness requirements, but must not introduce hidden authority escalation.

## Excursion and Re-Admission

Milestone 4 should explicitly support the possibility that a tracked fragment or delta may:

- begin inside identity scope
- leave identity scope lawfully
- remain tracked while external
- undergo witnessed transformation while external
- return for readmission under telos evaluation

This means continuity is not defined by constant inclusion alone.

It is defined by the ability to preserve boundary crossings with lawful memory.

Minimum transition markers should therefore include:

- departure markers
- externalization markers
- transformation markers
- readmission requests
- telos decisions

No async carrier should silently collapse a lawful excursion into ordinary uninterrupted ownership.

## Admissible Identity Morphisms

Milestone 4 should explicitly define the lawful morphism set acting on identity-bearing fragments.

That morphism law should answer:

- which transformations preserve direct continuity
- which transformations remain deformative but re-admittable
- which transformations count as excursions
- which transformations are identity-breaking

Initial morphism classes should include:

- `isomorphic`
- `deformative_admissible`
- `excursive`
- `identity_breaking`

Milestone 4 should treat this morphism law as a precondition for schema and validator work rather than as a later refinement.

## Validation Rules

Milestone 4 must enforce:

- deterministic ordering within each channel
- explicit cadence declaration for every channel
- explicit sync-anchor participation rules
- no silent normalization that hides cadence mismatch
- no interpolation or synthetic backfill across missing channel updates
- explicit distinction between observed, derived, operator-confirmed, and governance-certified deltas
- stable digest behavior for channel-local deltas and sync anchors
- Prime-safe views that remain metadata-first even when privileged payloads are richer
- lawful custody boundaries for protected, sealed, or cryptic channel content

Validation must happen before any rendering, animation, or surface reconstruction is considered valid.

## Carrier Boundary

If a future `.hogif` is introduced, it should be treated as the carrier expression of this milestone rather than as the doctrine itself.

In that framing:

- `.hopng` remains the crystallized posture artifact
- current Phase 3 remains single-clock lawful evidence stacking
- `.hogif` becomes a future continuity carrier for asynchronously updating channels under lawful sync anchors

This keeps the model honest: the hard problem is the ontology of time, not the file extension.

## Preconditions

Milestone 4 should not begin implementation work until the current single-clock Phase 3 contracts are stable enough to serve as a baseline.

That means:

- Milestone 1 slice legality must be stable
- Milestone 2 state semantics must be stable enough not to shift underneath async design work
- Milestone 3 basis-alignment semantics must be stable enough to inform cross-channel reconciliation rules
- admissible identity morphism classes must be stable enough to govern validator and readmission design

Milestone 4 is a planned extension track. It does not redefine the current single-clock Phase 3 release gate.

## Acceptance Criteria

- the repo contains an explicit doctrine for heterochronous channels before any carrier implementation begins
- cadence modes, sync anchors, and delta classes are finite, explicit, and deterministic
- async validation rules are defined before any renderer or encoder work
- the relationship between `.hopng` and a future `.hogif` is documented without ambiguity
- Prime-safe and custody-safe boundaries remain explicit under asynchronous timing

## Non-Goals

- casual animation support
- media-first framing of the extension
- arbitrary per-channel timing grammars
- weakening current `.hopng` trust or temporal guarantees
- retrofitting existing single-clock artifacts as if they were already heterochronous
