# Phase 2 Milestone 2: Governed Projection Derivation

This milestone is the second executable slice of Phase 2. Milestone 1 made lawful relational structure explicit. Milestone 2 uses that structure to derive a governed projection result and expose a real merge/comparison path.

The emphasis here is still lawful relationality, not event time. The system should be able to prove how a visible surface is formed from declared universes and gluing relations without yet introducing Phase 3 temporal slices.

## Milestone Intent

Prove that a relational `.hopng` can move from declared structure to derived projection behavior.

At the end of this milestone, the tool should be able to:

- evaluate projection rules in order
- produce a deterministic merge or derivation result
- expose whether a visible surface is lawfully formed from the declared relational graph
- compare a governed projection result against a flattened or incomplete artifact

## Scope

### Projection Derivation Engine

Add a Phase 2 projection derivation service that:

- consumes universe layers, gluing relations, projection rules, and legibility profile
- evaluates projection rules by explicit precedence
- derives a governed projection plan or merge trace
- produces deterministic output for the same artifact input

This service does not need full image synthesis beyond the minimum required to prove lawful derivation.

### Merge Command Upgrade

Upgrade `Merge-HOPNGLayers.ps1` and the underlying CLI command from a reserved placeholder to a Phase 2 diagnostic merge tool.

Minimum command behaviors:

- validate relational sidecars before merge
- emit a merge plan or projection trace
- optionally emit a shallow merged artifact summary
- fail with explicit errors when lawful derivation conditions are not met

### Projection Comparison

Add the first real comparison path for formed vs flattened artifacts:

- compare artifacts with lawful relational provenance against artifacts missing required relations or projection rules
- identify whether the final surface is supported by governed derivation or merely resembles it superficially
- emit actionable diagnostics for missing derivation support

### Legibility Enforcement

Use the legibility profile as a runtime gate:

- required universes must participate in derivation
- required relations must be present in the merge graph
- projection-integrity requirements must be met before merge is considered lawful

## Out of Scope

- event slices or temporal drift logic
- optical phase channels as active merge data
- participatory or perspectival engram forms
- commitment or covenant state
- Sanctuary or OE runtime participation

## Implementation Tasks

### Core Services

- add a projection derivation or merge-planning service in `Hdt.Core`
- add a merge result model with:
  - ordered rule application trace
  - participating universes
  - participating relations
  - legibility pass/fail state
  - formed-vs-incomplete classification

### Validation and Diagnostics

- extend diagnostics to distinguish:
  - relationally valid but under-specified projection
  - invalid projection derivation
  - flattened projection lacking lawful provenance
- add explicit validation or diagnostic messages when required universes or relations are not used by the derivation result

### CLI and PowerShell Surface

- upgrade the `merge-layers` CLI command from "not implemented" to a real Phase 2 command
- expose the same behavior through `Merge-HOPNGLayers.ps1`
- support JSON output and human-readable output

### Comparison Path

- add a comparison service that can classify:
  - lawfully formed projection
  - structurally incomplete projection
  - flattened or counterfeit-like artifact lacking derivation support

## Test Plan

Add tests for:

- valid relational artifact producing a deterministic merge trace
- projection rules applied in precedence order
- merge failing when a required universe is omitted from derivation
- merge failing when a required relation is absent
- comparison classifying a relational artifact as formed
- comparison classifying a Phase 1-only or incomplete artifact as flattened or unsupported in a Phase 2 comparison context
- CLI merge output returning non-zero exit codes when derivation is unlawful

## Acceptance Criteria

- a valid Phase 2 artifact can produce a deterministic governed projection trace
- `Merge-HOPNGLayers` becomes operational as a diagnostic or shallow merge tool
- legibility profile materially participates in pass/fail derivation outcomes
- the tool can distinguish lawfully formed projection support from flattened or incomplete surfaces
- no Phase 3 temporal semantics are required to complete the milestone

## Exit Condition

Milestone 2 is complete when Phase 2 artifacts can move from declared lawful relation to governed projection derivation and comparison.

At that point, Phase 2 will have proven both:

- relational structure exists lawfully
- relational structure can actually govern a visible formation result

That creates a stable handoff into Phase 3 event and tensor temporality.
