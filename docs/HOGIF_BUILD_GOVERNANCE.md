# HOGIF Build Governance

This document defines the build-governance posture for any future `.hogif` implementation.

`.hogif` is not governed as a casual animation format. It is governed as a continuity carrier for asynchronously updating channels whose deltas may leave identity scope, remain tracked while external, and later be lawfully re-admitted with their excursion history intact.

The governing principle is:

> continuity must remember boundary crossings, not merely preserve internal state

## Purpose

This governance exists to prevent two failure modes:

- silent mutation inside identity scope
- silent loss or return across identity boundaries

The build should therefore preserve explicit, witnessed transformation across boundaries rather than pretending that all continuity is constant inclusion.

## Core Posture

For `.hogif`, identity is not defined as uninterrupted possession.

Identity is defined as:

- lawful admission
- lawful release
- tracked excursion
- evaluated transformation
- lawful re-admission or lawful rejection

Something may leave identity scope without leaving continuity scope.

## Scope Boundaries

This governance applies to:

- asynchronous channel descriptors
- channel-local deltas
- sync anchors
- excursion markers
- telos or admissibility decisions
- Prime-safe and privileged inspection behavior
- custody and witness requirements for re-entry

This governance does not by itself define:

- the final `.hogif` binary or package layout
- rendering rules
- aesthetic animation behavior
- OE, Sanctuary, covenant, or runtime action semantics

Related planning documents:

- [`HONEST_SUBSTRATE_PRINCIPLES.md`](./HONEST_SUBSTRATE_PRINCIPLES.md)
- [`HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md`](./HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md)
- [`HOGIF_RESEARCH_TRACK.md`](./HOGIF_RESEARCH_TRACK.md)

## Governing Terms

- `identity_scope`: the set of deltas or fragments currently admitted as internal to the artifact's active continuity state
- `continuity_scope`: the larger tracked history that includes admitted, released, external, transformed, and re-entry decisions
- `excursion`: a lawful departure from identity scope that remains tracked in continuity scope
- `transit_context`: the declared external or transitional context in which an excursion persists
- `readmission`: a governed attempt to return a tracked excursion to identity scope
- `telos_gate`: the explicit evaluation step that determines whether re-entry is admissible
- `witness_anchor`: the evidence-bearing marker that substantiates a transition or decision

## Lifecycle States

Every excursion-capable delta or fragment should move through explicit lifecycle states.

Minimum state set:

- `admitted_internal`
- `released_external`
- `tracked_in_transit`
- `transformed_external`
- `submitted_for_readmission`
- `readmitted`
- `rejected`
- `quarantined`

These states are not interchangeable. A readmitted fragment is not the same thing as one that never left. The artifact must preserve that history.

## Required Transition Markers

No boundary crossing should occur without a marker.

Minimum marker set:

- `departure_marker`: records that a fragment left identity scope
- `externalization_marker`: records the transit context or external class
- `transformation_marker`: records what materially changed while external
- `sync_anchor`: records how asynchronous channels rejoin one lawful continuity point
- `readmission_request`: records that re-entry is being requested
- `telos_decision`: records whether re-entry was admitted, rejected, or quarantined

Each marker should carry stable identifiers, timestamps, actor or process provenance where relevant, and evidence references sufficient for later inspection.

## Telos Governance

Re-entry is not automatic.

Every readmission path should answer:

- was departure lawful
- was the transit context declared
- was transformation witnessed or otherwise justified
- is continuity still provable
- is re-entry admissible under current policy

The telos gate should produce only explicit outcomes:

- `Admit`
- `Reject`
- `Quarantine`
- `Defer`

No implementation should silently coerce a failed excursion back into identity scope.

## Admissible Identity Morphisms

Markers alone are not sufficient.

The build must also govern the class of transformation acting on an identity-bearing fragment.

Every material identity-affecting transformation should be treated as a member of an admissible morphism set.

Initial morphism classes:

- `isomorphic`
- `deformative_admissible`
- `excursive`
- `identity_breaking`

Each morphism should eventually declare:

- its morphism class
- its reversibility class
- allowed source and destination lifecycle states
- continuity requirements
- witness requirements
- telos and readmission policy

No transformation should be treated as lawful merely because transition markers exist. The transformation itself must belong to a lawful morphism class.

## Channel Governance

Every `.hogif` channel should declare:

- its channel type
- its cadence mode
- its sync policy
- its delta encoding
- its encryption scope
- its witness requirement
- whether it may host excursion-capable deltas

If a channel allows excursion-capable deltas, it must also declare:

- valid transit contexts
- readmission requirements
- whether transformations are allowed while external
- whether governance certification is required before re-entry

## Delta Governance

Every delta should preserve both timing class and truth-status class.

Minimum delta classes:

- `observational`
- `inferred`
- `operator_confirmed`
- `governance_certified`
- `cryptically_sealed`

If a delta leaves identity scope, its class may change only through an explicit transformation marker and witness anchor.

## Build Rules

The `.hogif` build must follow these rules:

- schema before container
- validation before rendering
- inspection before automation
- no silent exit from identity scope
- no silent return to identity scope
- no interpolation or synthetic continuity across missing excursion markers
- no identity reassignment without an explicit telos decision
- no downgrade of Prime-safe posture because async state is harder to explain
- no loss of digestibility or provenance to simplify carrier implementation

## Validator Requirements

Before any `.hogif` renderer or writer is considered lawful, validation must be able to prove:

- every material identity-affecting transformation belongs to a declared admissible morphism class
- every excursion has a departure marker
- every external phase has a declared transit context
- every material transformation has a transformation marker
- every re-entry attempt has a telos decision
- morphism reversibility claims match the available evidence and continuity record
- sync anchors reconcile asynchronous channels without hidden normalization
- digests remain stable across channel-local deltas and transition markers
- Prime-safe output omits restricted payloads while still exposing continuity facts

## Inspection Requirements

Prime-safe inspection should expose:

- lifecycle state
- marker identifiers
- timestamps
- delta class
- transit context class
- readmission status
- validation findings

Privileged inspection may additionally expose:

- payload-level evidence
- transformation details
- witness material
- protected or cryptic references when authorized

Prime-safe inspection must never hide the fact that a fragment left and returned.

## Reference Artifact Requirements

Research and implementation work should maintain explicit reference cases for:

- lawful excursion and lawful readmission
- lawful excursion and permanent rejection
- lawful excursion and quarantine
- malformed silent transformation
- malformed silent return
- incompatible sync-anchor reconstruction

No `.hogif` implementation should be treated as credible without these cases.

## Build Lanes

The work should proceed in this order:

- doctrine and vocabulary
- admissible identity morphism law
- channel and transition schemas
- validator rules and negative cases
- Prime-safe and privileged inspection models
- reference artifacts and research corpus
- carrier packaging and encoder work
- rendering and operator tooling

This order is mandatory because `.hogif` is a governed continuity object first and a carrier second.

## Merge Gates

A `.hogif`-related change should not merge unless it:

- names the lifecycle states or markers it affects
- preserves or improves provenance
- adds or updates validation when semantics change
- updates Prime-safe behavior when disclosure posture changes
- includes reference examples or tests for new transition behavior
- does not overstate implementation maturity

## Explicit Non-Goals

- treating `.hogif` as merely animated `.hopng`
- equating continuity with uninterrupted ownership
- allowing undeclared transformation classes to act on identity-bearing fragments
- allowing hidden mutation during transit
- allowing re-entry without evidence
- using renderer behavior as a substitute for governance
