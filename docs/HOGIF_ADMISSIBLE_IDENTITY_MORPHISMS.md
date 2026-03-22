# HOGIF Admissible Identity Morphisms

This document defines the planned morphism law for future `.hogif` continuity artifacts.

It exists to answer one build-critical question:

> what kinds of transformations can act on an identity-bearing fragment without breaking re-admittable continuity

For `.hogif`, identity is already treated as a bounded and referencable object. The problem is therefore not re-containerizing identity. The problem is governing the transformations that act on it.

## Purpose

This planning document gives the project a stable way to talk about:

- lawful transformations over identity-bearing fragments
- partial reversibility and return conditions
- telos-constrained admissibility
- the boundary between deformation, excursion, and identity break

It is a planning-stage doctrine. It does not yet define a finalized schema or runtime implementation.

## Core Invariant

An identity-bearing fragment remains re-admittable when its transformation history is:

- continuous
- witnessed
- policy-evaluable
- admissible under telos

Form may change. Scope may change. Ownership status may change.

Continuity fails only when the transformation path becomes untraceable, incoherent, or inadmissible.

## Working Definition

An admissible identity morphism is:

- a transformation over an identity-bearing fragment or delta
- with declared preconditions and postconditions
- whose continuity requirements are explicit
- whose reversibility class is declared
- whose readmission posture is policy-evaluable

## Morphism Registry Concept

`.hogif` planning should assume a morphism registry rather than implicit transformation behavior.

Each registered morphism should eventually declare:

- `morphismId`
- `morphismClass`
- `description`
- `reversibilityClass`
- `fromLifecycleState`
- `toLifecycleState`
- `allowedTransitContexts`
- `continuityRequirements`
- `witnessRequirements`
- `telosPolicy`
- `readmissionPolicy`
- `identityRiskClass`

No transformation that materially changes identity posture should be treated as lawful unless it belongs to a declared morphism class.

## Initial Morphism Classes

### `isomorphic`

Meaning:

- the fragment is re-expressed without changing its continuity posture

Examples:

- channel translation
- representation change
- lawful projection into another decode surface

Expected properties:

- continuity remains internal or directly mappable
- reversibility is high
- readmission is trivial because identity scope did not materially break

### `deformative_admissible`

Meaning:

- the fragment changes form, compression level, abstraction level, or interpretation while remaining policy-trackable

Examples:

- summarization
- structured compression
- inference with provenance
- abstraction into a derived channel

Expected properties:

- continuity remains provable
- reversibility may be partial
- readmission requires provenance and class-aware telos review

### `excursive`

Meaning:

- the fragment leaves identity scope, persists in a transit context, and may later return for evaluation

Examples:

- external processing
- foreign-context traversal
- delegated or quarantined transformation

Expected properties:

- departure must be explicit
- transit context must be declared
- transformation while external must be witnessed
- readmission is never automatic

### `identity_breaking`

Meaning:

- the transformation destroys, obscures, or invalidates continuity

Examples:

- untraceable mutation
- incoherent recombination
- adversarial corruption
- lineage loss

Expected properties:

- telos rejects re-admission
- validator treats the path as discontinuous
- the fragment may remain in continuity history as rejected, but not as restored identity

## Reversibility Classes

The project should distinguish morphism class from reversibility class.

Initial reversibility classes:

- `fully_reversible`
- `partially_reversible`
- `reconstructable_with_witness`
- `non_reversible_but_traceable`
- `identity_breaking`

This matters because two transformations may both be admissible while having very different return conditions.

## Geometry Intuition

Planning should treat identity space as a governed manifold of allowable paths rather than a flat set of static values.

Under that intuition:

- lawful morphisms follow admissible paths
- excursions leave identity scope but retain a return mapping
- identity-breaking transforms leave the admissible region entirely

This is a planning metaphor, not a requirement to implement advanced mathematics directly.

## Required Planning Questions

Before schema work begins, the project should answer:

- which morphism classes are allowed in Prime-safe artifacts
- which morphism classes require privileged or cryptic witness material
- whether every excursion is a morphism or whether some are higher-order sequences of morphisms
- how morphism class and delta class interact
- whether sync anchors admit transformed fragments or only record their reconciliation
- what evidence is sufficient for partial reversibility

## Validator Implications

The future validator should eventually be able to ask:

- was the transformation a member of the declared admissible morphism set
- did it begin from a lawful lifecycle state
- did it end in a lawful lifecycle state
- were the required witnesses present
- did the transformation preserve continuity to the degree promised by its reversibility class
- is readmission allowed for this morphism class under current telos policy

This is stronger than validating markers alone. It validates the lawfulness of the transformation class itself.

## Planning Dependencies

This morphism law should be treated as:

- a Phase 3 Milestone 4 planning dependency
- a precondition for `.hogif` schema design
- a precondition for `.hogif` validator rules
- a precondition for Phase 4 identity-bearing artifact work

Phase 4 should inherit a stable law of admissible identity transformation rather than inventing one ad hoc.

## Planned Deliverables

Research should eventually produce:

- a named morphism registry draft
- at least one positive example for each admissible morphism class
- at least one negative example for `identity_breaking`
- a reversibility decision table
- a telos-readmission matrix by morphism class

## Explicit Non-Goals

- replacing lifecycle states with abstract theory
- treating any reversible transform as automatically admissible
- assuming all excursions are identity-preserving
- collapsing morphism law into renderer behavior
