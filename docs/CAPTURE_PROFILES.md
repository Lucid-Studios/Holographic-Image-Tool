# HOPNG Capture Profiles

A `.hopng` capture profile defines what kind of claim an artifact may support, what evidence layers it may carry, and what custody posture governs its visibility.

Carrier choice is subordinate to evidentiary function, not the other way around.

## Purpose

Capture profiles define how `.hopng` should be typed when the artifact is being used as evidence rather than only as a structured carrier.

They exist so the system can distinguish:

- source evidence from derived projection
- Prime-safe visibility from protected or privileged custody
- constitutive witness from runtime evidence
- candidate support from governed or binding acts

Profiles do not replace the base `.hopng` artifact model. They constrain how that model is used for a specific evidentiary role.

## Doctrinal Rules

- format follows evidentiary role, not convenience
- protected payloads should be pointerized rather than embedded in visible layers
- visible layers are not assumed to carry the whole truth of the artifact
- `.hopng` evidence is not itself constitutive authority unless the profile and governing policy explicitly declare it
- carrier defaults are defaults, not eternal absolutes
- digest, signature, and custody rules still apply to all declared carrier layers

## Common Profile Fields

The following fields should exist once capture profiles become machine-readable:

- `profile_id`
- `profile_type`
- `evidence_class`
- `custody_posture`
- `prime_visibility_mode`
- `validation_targets[]`
- `dispute_risk_class`
- `sampling_policy`
- `required_layers[]`
- `optional_layers[]`
- `protected_pointer_requirements[]`
- `carrier_layers[]`

Recommended value families:

- `profile_type`
  - `cme_creation_capture`
  - `governing_traffic_evidence`
  - `engram_root_constructor_capture`
- `evidence_class`
  - `constitutive_witness`
  - `runtime_governance_evidence`
  - `engram_candidacy_evidence`
- `custody_posture`
  - `prime_safe`
  - `privileged`
  - `mixed_pointerized`
- `sampling_policy`
  - `single_witness`
  - `bounded_window`
  - `periodic_audit`
  - `full_event_set`
- `dispute_risk_class`
  - `low`
  - `medium`
  - `high`

## Default Carrier Guidance

Default carriers should remain subordinate to evidentiary role:

- default visible witness carrier: `PNG`
- default graph or binding carrier: `SVG`
- default high-depth scalar or audit field carrier: `TIFF`
- default multi-channel analytic or constructor field carrier: `OpenEXR`
- default protected payload carrier: pointerized sidecar reference rather than direct embed

Future implementations may admit alternative carriers when the evidentiary role is preserved and the manifest declares the format and custody rules explicitly.

## Profile 1: CME Creation Capture

### Purpose

Capture proof of role binding, naming, operator consent, and bonded legal custody during CME creation.

### Evidence Class

- `constitutive_witness`

### Custody Posture

- `mixed_pointerized`

### Validation Target

The artifact should support validation of:

- role binding
- naming act
- operator consent
- bonded custody posture
- issuer and operator attestation chain

### Required Layer Pattern

- `projection.visible/png`
  - Prime-safe visible witness surface
- `binding.graph/svg`
  - role names, binding edges, issuer, operator, and custody relation graph
- `consent.seal/png`
  - consent or approval witness seal
- `bond.terms.pointer`
  - protected reference to legal terms
- `identity.attest.pointer`
  - protected identity and operator attestation reference

### Invariants

- no creation capture may expose raw protected identity payloads in visible layers
- no creation capture may expose full bonded legal terms in public or Prime-safe layers
- the visible layer is witness material, not the complete legal record
- if `dispute_risk_class = high`, policy should require an additional witness artifact or equivalent secondary attestation path

## Profile 2: Governing CME Traffic Evidence

### Purpose

Capture runtime governance evidence for traffic monitored under bound CME use in GEL or cGEL, including policy application, threshold crossings, and operator intervention.

### Evidence Class

- `runtime_governance_evidence`

### Custody Posture

- `mixed_pointerized`

### Validation Target

The artifact should support validation of:

- governance trigger conditions
- monitored event window
- rule or policy boundary application
- operator override or escalation
- evidence basis for SelfGEL, cSelfGEL, GEL, or cGEL substrate review

### Required Window Fields

Every governing-traffic artifact should declare:

- event window start
- event window end
- trigger class
- sample policy class
- whether the artifact is:
  - spot sampled
  - threshold triggered
  - periodic audit
  - full bounded set

### Required Layer Pattern

- `traffic.summary/png`
  - Prime-safe operational witness surface
- `traffic.field/tiff`
  - high-depth audit field for monitored substrate conditions
- `governance.phase/exr`
  - privileged multi-channel phase, pressure, drift, and analytic state field
- `decision.boundary/svg`
  - rule boundaries, thresholds, and intervention geometry

### Sampling Guidance

Evidence `.hopng`s are typically required when:

- threshold crossings occur
- policy conflicts are detected
- operator override happens
- anomalous drift or rupture-risk conditions appear
- scheduled audit windows require preserved evidence

Illustrative policy classes:

- low-risk routine traffic: one artifact per audit window
- medium-risk governed traffic: three to five artifacts across the bounded event window
- high-risk or contested governance: full bounded event set rather than spot samples

## Profile 3: Engram Root / Constructor Capture

### Purpose

Support a cognitive claim about an engram root and constructor form before it becomes a GEL or cGEL candidate.

### Evidence Class

- `engram_candidacy_evidence`

### Custody Posture

- `mixed_pointerized`

### Doctrinal Boundary

An Engram Root / Constructor `.hopng` is evidentiary support for candidacy, not candidacy itself.

### Validation Target

The artifact should support validation of:

- root-form coherence
- constructor-form traceability
- provenance from observed basis to claimed candidate structure
- drift stability across slices or windows
- reproducibility of the constructor claim under the recorded conditions

### Required Layer Pattern

- `engram.projection/png`
  - visible claim surface
- `engram.root.vector/svg`
  - symbolic or graph form of the claimed root structure
- `engram.constructor.field/exr`
  - privileged multi-channel constructor-state field
- `engram.coherence.field/tiff`
  - high-depth coherence, strain, or support field
- `engram.provenance.pointer`
  - protected provenance and source-observation references

### Invariants

- the visible claim layer must not be treated as sufficient proof by itself
- provenance must remain inspectable even when protected evidence is pointerized
- a candidate-support `.hopng` must not be interpreted as a binding GEL or cGEL act without an additional governing process

## Sampling and Dispute-Risk Policy

Sampling policy and dispute-risk class should be explicit rather than implied.

Recommended use:

- `single_witness`
  - low-volume, low-dispute constitutive acts with strong attestation
- `bounded_window`
  - runtime events where a lawful event window is more important than isolated snapshots
- `periodic_audit`
  - scheduled evidence preservation for ongoing governance monitoring
- `full_event_set`
  - contested, high-risk, or legally sensitive conditions where spot samples are insufficient

Dispute risk should drive stricter evidentiary burden:

- `low`
  - single signed witness may suffice
- `medium`
  - secondary witness, operator confirmation, or fuller provenance may be required
- `high`
  - redundant witness paths, stricter custody, and fuller event-set preservation should be expected

## Non-Goals

This document does not yet define:

- a committed schema for machine-readable capture profiles
- final format exclusivity rules for every possible carrier
- engram candidacy approval logic
- GEL or cGEL admission rules
- Sanctuary or OE runtime authority
- legal policy beyond the artifact-side evidence posture

## Design Consequence

Capture profiles imply a future `.hopng` artifact-profile layer where the manifest can declare the evidentiary role of the artifact itself.

That future profile layer should eventually govern:

- what claim class the artifact supports
- which layer carriers are lawful for that class
- what custody posture applies
- what sampling and dispute policy applies
- what validation burden must be met before the artifact can support stronger claims
