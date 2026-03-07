# Phase 3 Milestone 2: Temporal State Maturity

Phase 3 Milestone 2 deepens the single-artifact temporal system established in Milestone 1. The artifact already preserves observed sets, grouped event slices, derived phase slices, and first-order drift and topology diagnostics. Milestone 2 adds stronger temporal state interpretation without crossing into engram, formation, OE, Sanctuary runtime, or aesthetic-rendering semantics.

The purpose of this milestone is to make the temporal layer operationally meaningful rather than merely structurally valid.

## Intent

Milestone 2 turns Phase 3 from basic temporal legality into stronger temporal interpretation.

It should answer questions like:

- is the artifact temporally stable, rising in strain, drifting, or approaching rupture risk
- how does drift behave across longer horizons rather than just adjacent slices
- what derived directionality emerges from pressure and topology change
- how should duration windows and comparison windows be governed without introducing interpolation

## Scope

Milestone 2 includes:

- duration-window and timestamp-span policy support
- richer temporal-state classification
- derived `force` semantics
- deeper drift analysis across widened horizons
- stronger `render-phase-stack` diagnostic output
- clearer temporal exit codes and operator guidance

Milestone 2 does not include:

- cross-artifact temporal comparison
- engram semantics
- formation or covenant logic
- OE or Sanctuary runtime behavior
- synthetic interpolation
- PNG phase rendering

## Data and Policy Extensions

`oan.hopng_phase_policy` should be extended to support:

- fixed raw-count windows
- duration-based windows
- timestamp-span validation rules
- horizon-classification policy
- optional state-threshold policy for drift, pressure, bloom, and derived force

Milestone 2 should still reject synthetic interpolation. Any windowing change must operate only over explicitly present slices.

## Derived Force Semantics

`force` should remain derived rather than hand-authored.

Initial Milestone 2 meaning:

- `force` = derived directional change tendency inferred from pressure, drift, and relation-graph change across the current comparison horizon

Expected behavior:

- higher pressure without topology change may indicate constrained buildup
- higher drift with widening topology change may indicate propagating transition
- derived force should remain explainable through its contributing metrics

Milestone 2 should record derivation provenance rather than treating force as opaque output.

## Temporal State Classes

Milestone 2 should classify slices or windows into operator-usable states.

Initial class set:

- `Stable`
- `RisingPressure`
- `Drifting`
- `Propagating`
- `RuptureRisk`
- `StructurallyIncomplete`

The exact thresholds should be policy-driven and deterministic.

## Command Surface

`Render-HOPNGPhaseStack.ps1` remains the public operator command, but Milestone 2 should extend its output with:

- state classification
- widened-horizon summaries
- derived force summaries
- duration-window basis when used
- deterministic exit codes for meaningful temporal-state outcomes

No new public temporal command is required in this milestone.

## Validation Rules

Milestone 2 must enforce:

- duration windows align with explicit timestamps
- timestamp spans do not exceed policy bounds
- derived force is reproducible from stored inputs and policy
- temporal state classification is deterministic
- no interpolation or synthetic slice generation occurs

## Acceptance Criteria

- one artifact can be classified into meaningful temporal states using explicit policy
- widened drift analysis works across declared horizons without ambiguity
- derived force is reproducible and explainable
- duration windows remain lawful and non-synthetic
- Prime-safe temporal output remains metadata-first and does not expose protected payloads

## Non-Goals

- comparing temporal stacks across different artifacts
- projecting temporal states into engram identity structures
- covenant or runtime consequences of temporal state
- aesthetic or image-native phase synthesis
