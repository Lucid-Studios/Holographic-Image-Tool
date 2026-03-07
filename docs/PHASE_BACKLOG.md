# Holographic Data Tool Phase Backlog

This document is the execution-oriented companion to [`PHASE_ROADMAP.md`](./PHASE_ROADMAP.md). The roadmap defines what each phase means. This backlog defines what remains to be built in practical terms.

## Phase 1: Trusted Artifact Existence

### Status

Implemented.

### Completed Foundation

- artifact skeleton creation
- deterministic canonical JSON output
- sidecar packaging and digesting
- Ed25519 signing and verification
- lawful structure validation
- Prime-safe and privileged inspection
- PowerShell wrappers for the CLI
- adapter contracts and test doubles for storage and OE bindings
- reserved schema names for later semantic phases

### Remaining Phase 1 Cleanup

- normalize older project draft documents that still contain mojibake or outdated framing
- tighten example artifact hygiene so only intentional public-safe examples remain committed
- expand documentation where Phase 1 behavior and governance policy overlap

## Phase 2: Lawful Relationality

### Status

Implemented, entering release hardening.

### Goal

Move from isolated trusted artifacts to governed relation between coordinate-bound universes and artifacts.

### Backlog

- implement `oan.hopng_universe_layer.v0.1.0.json`
- implement `oan.hopng_gluing_manifest.v0.1.0.json`
- implement `oan.hopng_projection_rules.v0.1.0.json`
- implement `oan.hopng_legibility_profile.v0.1.0.json`
- extend the schema registry and validation engine to recognize and validate Phase 2 sidecars
- add lawful gluing checks before any rendering or merge pipeline
- add counterfeit-vs-formed comparison foundations tied to gluing provenance
- add Phase 2 CLI or reserved-command upgrades only when the underlying contracts are real

### Planned First Slice

See [`PHASE_2_MILESTONE_1.md`](./PHASE_2_MILESTONE_1.md).

### Planned Second Slice

See [`PHASE_2_MILESTONE_2.md`](./PHASE_2_MILESTONE_2.md).

### Current Hardening Work

- reconcile docs and operator guidance with the now-live `merge-layers` and `compare-surfaces` commands
- keep only clean Phase 1 and Phase 2 reference artifact sets under `examples/`
- remove committed private signing keys from reference artifacts
- add repo-local build and test automation
- add one internal smoke path that proves create, validate, inspect, merge, and compare behavior through the public PowerShell surface

### Release Gate

See [`PHASE_2_RELEASE_READY.md`](./PHASE_2_RELEASE_READY.md).

## Phase 3: Event and Tensor Temporality

### Status

Milestone 1 implemented; later temporal expansion remains planned.

### Goal

Preserve evented change and tensor-native deltas rather than static lawful structure alone.

### Backlog

- harden the Phase 3 Milestone 1 operator surface and reference artifacts
- add broader temporal examples once a public-safe Phase 3 example set is selected
- extend drift and topology diagnostics into richer temporal comparison after single-artifact semantics stabilize
- implement duration-window and timestamp-span temporal policy support
- implement derived `force` semantics and temporal state classification
- implement governed cross-artifact temporal comparison after single-artifact semantics stabilize
- harden the full Phase 3 operator surface for release readiness

### Implemented Milestone 1

- implemented `oan.hopng_event_slice.v0.1.0.json`
- implemented `oan.hopng_phase_slice.v0.1.0.json`
- implemented `oan.hopng_phase_policy.v0.1.0.json`
- implemented `oan.hopng_optical_channels.v0.1.0.json`
- added observed-set validation for duration, cadence, and raw-slice count
- added grouped event-slice and derived phase-slice validation
- added deterministic phase derivation using contiguous event windows
- added drift and relation-graph topology diagnostics inside one artifact
- upgraded `render-phase-stack` to a live diagnostic command
- preserved Prime-safe metadata-only temporal inspection

### Current Boundaries

- single-artifact temporal semantics only
- analytic-first and custody-safe
- no interpolation or synthetic slices
- no cross-artifact temporal comparison
- no engram, formation, OE, or Sanctuary runtime behavior
- no aesthetic image synthesis

### Milestone Definition

See [`PHASE_3_MILESTONE_1.md`](./PHASE_3_MILESTONE_1.md).

### Planned Milestone 2

See [`PHASE_3_MILESTONE_2.md`](./PHASE_3_MILESTONE_2.md).

### Planned Milestone 3

See [`PHASE_3_MILESTONE_3.md`](./PHASE_3_MILESTONE_3.md).

### Release Gate

See [`PHASE_3_RELEASE_READY.md`](./PHASE_3_RELEASE_READY.md).

## Phase 4: Engrammatic Emergence

### Status

Planned after event and phase semantics are stable.

### Goal

Allow identity-bearing and perspective-bearing artifact forms to emerge on top of relational and temporal structure.

### Backlog

- implement perspectival engram schema
- implement participatory engram schema
- implement nearby or Peral universe set representation
- implement modal transform representation
- add compact family and branching-form persistence
- add engram comparison and inspection workflows

## Phase 5: Formation, Commitment, and Covenant State

### Status

Planned after engrammatic form is stable enough to witness review and commitment.

### Goal

Allow `.hopng` artifacts to witness lawful formation, review, acceptance, and commitment-bearing state.

### Backlog

- implement formation contract schema
- implement identity review delta
- implement certification acceptance record
- implement module self base set
- implement commitment projection
- add first-run formation workflows and comparison tooling
- preserve observe / review / accept / defer / reject / restrict state transitions as lawful artifact deltas

## Phase 6: Sanctuary and OE Runtime Life

### Status

Planned after commitment-bearing artifacts are structurally reliable.

### Goal

Bind lawful artifacts into Sanctuary and OE runtime life as governed witnesses.

### Backlog

- implement role glyphic act schema
- implement OE chain boundary schema
- implement role header and endcap structures
- replace placeholder storage or OE adapters with authorized runtime integrations
- add governed continuity binding workflows
- preserve Prime/Cryptic and trust-envelope guarantees under runtime binding

## Cross-Phase Rules

- do not weaken trust or provenance to accelerate later semantic phases
- add schemas before behaviors that depend on them
- add validation before automation that assumes lawful formation
- prefer explicit sidecars and comparison logic over hidden or inferred semantics
- treat Prime-safe and Cryptic boundaries as invariant across all phases
