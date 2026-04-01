# Phase 4 Working-Intent Transitions

This document defines the stance transitions that govern Phase 4 working intent before the system reaches Phase 5 formation, review, commitment, or covenant state.

The purpose is to let engrammatic work mature in a lawful sequence without confusing:

- early identity-form exploration
- structured support evidence
- reviewable support
- restricted, deferred, or rejected support
- later formal acceptance or commitment

## Why This Exists

Phase 4 is the first phase where identity-bearing form begins to emerge.

That means the repo needs a transition grammar for working intent before it is allowed to reuse the stronger transition set that belongs to Phase 5:

- `observe`
- `review`
- `accept`
- `defer`
- `reject`
- `restrict`

Phase 4 should stop short of those later commitment-bearing semantics.

Its own stance ladder should therefore remain support-first and intent-first.

## Working-Intent Stance Set

The Phase 4 working-intent ladder is:

1. `working_intent`
2. `structured_intent`
3. `supported_intent`
4. `reviewable_support`
5. `restricted_support`
6. `deferred_support`
7. `rejected_support`

These are Phase 4 stances, not Phase 5 decisions.

## Stance Meanings

### `working_intent`

An artifact or claim is being explored as a possible perspectival or participatory engram-support form.

This stance means:

- the claim is bounded and explicit
- the work is exploratory
- no stronger identity or candidacy claim may be inferred

### `structured_intent`

The working intent has been typed into a clearer support shape.

This stance means:

- the claim declares whether it is perspectival or participatory support
- visible claim surfaces and protected support layers are distinguished
- provenance and constructor questions are explicit even if not yet sufficient

### `supported_intent`

The artifact now has enough support to function as evidence for a bounded Phase 4 claim.

This stance means:

- provenance is traceable
- constructor or coherence support is materially present
- the validator can distinguish the artifact from a merely decorative or unsupported surface

This still does not mean:

- candidacy is granted
- identity is admitted
- runtime authority exists

### `reviewable_support`

The artifact is strong enough to be handed toward later human review without pretending that review has already happened.

This is the strongest positive stance Phase 4 should ordinarily reach.

This stance means:

- support evidence is sufficiently coherent for human examination
- Prime-safe and privileged inspection both remain legible
- the artifact may be considered for later Phase 5 review workflows

### `restricted_support`

The artifact may carry meaningful support, but additional boundary conditions prevent free progression.

This stance means:

- visibility, custody, ambiguity, or dispute-risk conditions require restriction
- support is not discarded, but it is not broadly promotable
- additional witness, provenance, or policy clarification may be required

### `deferred_support`

The artifact is preserved as an unresolved support candidate rather than promoted or rejected.

This stance means:

- support is incomplete or not yet persuasive enough
- the artifact remains part of continuity history
- future refinement is possible without treating the current state as sufficient

### `rejected_support`

The support claim is judged insufficient, counterfeit, incoherent, or otherwise non-admissible as Phase 4 support evidence.

This stance means:

- the rejection should remain traceable
- the artifact may persist as a negative or boundary example
- rejection of support is not the same thing as deletion of history

## Allowed Transitions

The lawful transition graph for Phase 4 working intent is:

- `working_intent -> structured_intent`
- `working_intent -> deferred_support`
- `working_intent -> rejected_support`
- `structured_intent -> supported_intent`
- `structured_intent -> restricted_support`
- `structured_intent -> deferred_support`
- `structured_intent -> rejected_support`
- `supported_intent -> reviewable_support`
- `supported_intent -> restricted_support`
- `supported_intent -> deferred_support`
- `supported_intent -> rejected_support`
- `restricted_support -> supported_intent`
- `restricted_support -> deferred_support`
- `restricted_support -> rejected_support`
- `deferred_support -> structured_intent`
- `deferred_support -> supported_intent`
- `deferred_support -> rejected_support`
- `reviewable_support -> restricted_support`
- `reviewable_support -> deferred_support`
- `reviewable_support -> rejected_support`

The transition out of Phase 4 is not `accept`.

The lawful handoff is:

- `reviewable_support -> Phase 5 review lane`

## Transition Markers

The Phase 4 transition model now has an initial machine-checked marker set.

Currently machine-checked in the Phase 4 support sidecars:

- `intentClassification`
- `supportShape`
- `inspectionPosture`
- `phase5HandoffReady`
- `restrictionReason` when `workingIntentState = restricted_support`
- `deferReason` when `workingIntentState = deferred_support`
- `rejectionReason` when `workingIntentState = rejected_support`

Still planned as later validator targets:

- `provenance_basis`
- `constructor_support_status`

That means the Phase 4 working-intent ladder is no longer prose-only. Support validation, Prime-safe inspection, and support comparison already rely on a real transition-marker surface.

## Research Best-Practice Mapping

The stance ladder aligns with the local automation postures:

- `working_intent` maps naturally to `Initial`
- `structured_intent` and `supported_intent` map naturally to `Formal`
- `reviewable_support`, `restricted_support`, `deferred_support`, and `rejected_support` require `Closing`-grade evidence discipline
- any baseline that treats a Phase 4 support posture as approved for later adoption still requires `Approved` plus explicit HITL review

This keeps exploratory work, reproducible support, and reviewable support from being blended together.

The current committed lawful negative-state corpus for `restricted_support`, `deferred_support`, and `rejected_support` now gives that mapping stable artifact-backed reference cases rather than only doctrinal descriptions.

## Boundaries

Phase 4 working-intent transitions must not:

- imply candidacy approval
- imply identity admission
- imply covenant or commitment
- imply OE or Sanctuary runtime participation
- silently substitute for Phase 5 review and decision semantics

## Relationship To Later Phases

Phase 4 working-intent transitions prepare the artifact for later review.

Phase 5 is where the stronger decision grammar begins.

That means:

- Phase 4 may structure and strengthen support
- Phase 4 may restrict, defer, or reject support
- Phase 4 may hand support forward as reviewable
- Phase 4 must not yet accept, bind, or commit

## Relationship To Existing Docs

This transition model complements:

- [`PHASE_4_ENTRY_CRITERIA.md`](./PHASE_4_ENTRY_CRITERIA.md)
- [`PHASE_4_MILESTONE_1.md`](./PHASE_4_MILESTONE_1.md)
- [`CAPTURE_PROFILES.md`](./CAPTURE_PROFILES.md)
- [`PHASE_ROADMAP.md`](./PHASE_ROADMAP.md)

The intent is simple:

Phase 4 should have lawful becoming before it has formal decision.
