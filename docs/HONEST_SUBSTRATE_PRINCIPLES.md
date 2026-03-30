# Honest Substrate Principles

This document defines the foundational principles for a maximally honest computational substrate in the Holographic Data Tool.

The goal is not total restriction. The goal is legible, accountable, and admissible system behavior across identity, evidence, transformation, and access control.

## Purpose

The system should strengthen its evidentiary chain through lawful handling of sensitive data without making that data itself the cryptographic secret.

This requires three concerns to remain distinct:

- privacy and legal controls define who may access, see, or use data
- cryptography enforces boundaries even when context leaks
- provenance and witness history record what happened and why it was admissible

Together, these produce an honest substrate.

## Core Doctrine

### Identity as Continuity

Identity is not static state.

Identity is:

- an append-only lineage
- composed of admissible transformations
- evaluated under governance constraints

Identity persists through lawful becoming, not through immutability.

### Telos as Admissibility

Telos is not a goal function.

Telos is the policy-evaluation layer that determines:

- whether a transformation may be incorporated
- whether continuity is preserved
- whether re-admission is lawful after excursion

Telos should be derived from:

- policy
- lineage
- operator context
- governance rules

### Transitional Identity

Identity is not required to remain continuously internal.

The system explicitly supports:

- departure from identity scope
- external transformation in a foreign or transitional context
- governed re-entry

All such transitions must be:

- witnessed
- recorded
- evaluated

Identity is preserved through tracked excursion, not forced containment.

### Admissible Identity Morphisms

Transformations over identity-bearing fragments should be classified as:

- `isomorphic`
- `deformative_admissible`
- `excursive`
- `identity_breaking`

Only admissible morphisms may extend identity lineage.

See [`HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md`](./HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md).

## Cryptic Substrate Principles

### Separation of Concerns

The system distinguishes:

- cryptographic authority
- governance context
- private data

Cryptographic authority includes:

- keys
- signatures
- attestation

Governance context includes:

- policy
- operator role
- lawful use conditions

Private data includes:

- sensitive contextual evidence

Private data is not the cryptographic secret. Cryptographic authority must enforce boundaries independently of whether private context is known.

### Root of Trust

A first-boot governance ceremony should establish:

- root key material
- device attestation baseline
- policy inheritance context

From this root:

- subordinate keys and capabilities are derived
- actions remain attributable and auditable

### Key Hierarchy

Keys should be structured by scope:

- root governance key
- device attestation key
- operator authorization key
- channel or artifact keys
- witness or audit signing keys

All keys should be:

- scoped
- revocable
- bound to lawful context

### Attested State

Sensitive operations should require:

- verified device state
- verified policy version
- verified operator authorization

Possession of data is insufficient without attested context.

## Evidence and Media Carriers

### `.hopng`

A `.hopng` is the crystallized posture artifact.

It carries:

- a visible preview or projection layer
- structured sidecars
- provenance metadata
- signatures and trust references
- policy-governed visibility boundaries

It represents a single admissible posture of identity or system state.

### `.hogif`

A future `.hogif` is the heterochronous temporal continuity carrier.

It should carry:

- multiple channels
- independent cadence modes
- classified deltas
- lawful sync anchors

It represents a continuity object across time rather than a single snapshot.

### Delta Classification

Deltas should remain typed by truth-status:

- `observational`
- `inferred`
- `operator_confirmed`
- `governance_certified`
- `cryptically_sealed`

Not all changes carry equal evidentiary weight.

## Privacy and Legal Posture

### Lawful Data Handling

The system should enforce:

- data minimization
- purpose limitation
- consent-aware access
- retention boundaries
- reviewable access policy

### Private Data as Context

Private or protected data:

- strengthens admissibility context
- improves evidence interpretation

Private or protected data must not:

- function as the cryptographic secret
- define authority on its own

### Auditability

Meaningful actions should remain:

- attributable
- timestamped
- policy-evaluable
- reviewable by human-in-the-loop governance where required

### Operator Continuity

Normal execution should proceed under bounded cascade authorization within the currently admitted build lane.

Pause and request HITL intervention only when:

- work would cross into a new architectural domain or research claim
- governance, security, cryptic-layer semantics, or admissibility rules would change
- verification fails or remains ambiguous inside current standing
- an irreversible destructive action is required
- operator preference is required beyond established doctrine
- promotion, commitment, release, or formal adoption requires operator review

Otherwise, continuity of execution should be preserved so the system can advance the active milestone with lawful structure, traceable reasoning, verification receipts, and bounded scope intact.

## System Guarantees

The system should be designed so that there is:

- no silent mutation of identity
- no authority without context
- no evidence without provenance
- no transformation without classification
- no re-entry without evaluation

## Summary Principle

The system maintains lawful continuity by binding identity, transformation, and evidence to cryptographic authority, governance policy, and witnessed history.

This creates a substrate where:

- identity can evolve without dissolving
- evidence can accumulate without corruption
- authority can operate without opacity

## Repository Context

This doctrine complements, but does not replace, the repository governance recorded in:

- [`LUCID_TECHNOLOGIES_STANDARDS.md`](./LUCID_TECHNOLOGIES_STANDARDS.md)
- [`OPERATOR_CONTINUITY_INSTRUCTIONS.md`](./OPERATOR_CONTINUITY_INSTRUCTIONS.md)
- [`PHASE_3_MILESTONE_4.md`](./PHASE_3_MILESTONE_4.md)
- [`HOGIF_BUILD_GOVERNANCE.md`](./HOGIF_BUILD_GOVERNANCE.md)

## Closing Statement

This substrate does not attempt to prevent all change.

It exists to ensure that meaningful change remains visible, classifiable, and admissible within a lawful continuity of identity.
