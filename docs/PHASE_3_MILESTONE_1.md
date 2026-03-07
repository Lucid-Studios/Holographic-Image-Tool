# Phase 3 Milestone 1: Single-Artifact Temporal Semantics

Phase 3 Milestone 1 adds the first temporal layer to `.hopng` without introducing engrams, formation state, OE behavior, Sanctuary runtime behavior, cross-artifact temporal comparison, interpolation, or aesthetic rendering.

The milestone is analytic-first and custody-safe. A `.hopng` may carry lawful temporal summaries, provenance, slice digests, and protected evidence references without embedding raw protected telemetry.

## Temporal Notation

- `DeltaObs`: total observed-set duration
- `delta`: base raw slice cadence
- `N`: total raw slice count in the observed set
- `n`: ordinal index within the stored slice family
- `h`: delta horizon in raw-slice units
- `hDelta = h x delta`

## Implemented Sidecars

- `oan.hopng_event_slice.v0.1.0.json`
- `oan.hopng_phase_slice.v0.1.0.json`
- `oan.hopng_phase_policy.v0.1.0.json`
- `oan.hopng_optical_channels.v0.1.0.json`

Each Phase 3 schema remains an aggregate sidecar:

- one file per schema
- manifest references plus file digests
- no per-slice files in Milestone 1

## Observed Set Model

`*.event-slices.json` contains:

- `observedSet`
- `slices[]`

The `observedSet` block defines the raw temporal basis and includes:

- `observedSetId`
- `artifactId`
- `observedDurationMs`
- `baseSliceCadenceMs`
- `rawSliceCount`
- `observedEventCount`
- `eventGroupingMode`
- `eventGroupingSizeRawSlices`
- `protectedEvidenceRefs[]`
- `primeSafeInspectionMode`
- `dataCustodyMode`

Event slices are grouped evidence windows rather than the raw observed stream itself.

## Event Slice Definition

Each event slice includes:

- `eventSliceId`
- `artifactId`
- `n`
- `timestampStartUtc`
- `timestampEndUtc`
- `rawStartN`
- `rawEndN`
- `rawSliceSpan`
- `observedEventCount`
- `universeStates`
- `protectedEvidenceRefs[]`
- `sliceDigest`

Required canonical channels in every participating universe state:

- `pressure`
- `drift`
- `bloom`

Reserved channels:

- `force`
- `opacity`
- `hue`
- `saturation`

## Phase Slice Definition

`*.phase-slices.json` contains derived temporal interpretation layers built from contiguous event windows.

Each phase slice includes:

- `phaseSliceId`
- `artifactId`
- `n`
- `timestampStartUtc`
- `timestampEndUtc`
- `sourceEventSliceIds`
- `sourceRawStartN`
- `sourceRawEndN`
- `deltaHorizon`
- `deltaHorizonMs`
- `universeStates`
- `sliceDigest`

## Phase Policy

`*.phase-policy.json` defines:

- `rawCadenceMs`
- `eventGroupingMode`
- `eventGroupingSizeRawSlices`
- `phaseWindowMode`
- `phaseWindowSizeEventSlices`
- `comparisonHorizonRawSlices`
- `aggregationPolicies`
- `primeSafeInspectionMode`
- `privilegedInspectionMode`

Milestone 1 defaults:

- `eventGroupingMode = fixed_raw_count`
- `phaseWindowMode = fixed_event_count`
- aggregation modes limited to `latest`, `mean`, and `delta`

## Optical Channel Semantics

Channels are dual-use, but their analytic meaning is canonical.

- `pressure`: local coherence strain or torsion tension near threshold or break
- `drift`: accumulated deviation from prior stable or expected trajectory
- `bloom`: coherent spread or intensification across neighboring slices
- `force`: reserved or derived only in Milestone 1
- `opacity`: reserved as a derived render aid only
- `hue`, `saturation`: reserved visual channels only

## Validation and Derivation Rules

Milestone 1 enforces:

- observed-set duration, cadence, and raw-slice count consistency
- strict `n` ordering within event and phase slice families
- contiguous raw ranges for event slices
- contiguous event windows for phase slices
- exact source-event and raw-range provenance in derived phase slices
- required channel coverage for every participating universe state
- Phase 2 universe references for every temporal universe state
- aggregation policies limited to `latest`, `mean`, and `delta`
- stable per-slice digests
- no interpolation or synthetic slices

Phase derivation is deterministic and single-artifact only.

## Comparison Behavior

Milestone 1 temporal comparison remains inside one artifact.

Primary metric:

- drift across slices

Secondary metric:

- relation-graph topology change

Topology change means:

- universe participation changes
- gluing participation changes
- projection-rule participation changes

Milestone 1 does not implement:

- cross-artifact temporal comparison
- spatial surface topology comparison
- force-distribution comparison as a primary authored channel

## Custody and Prime-Safe Rules

Raw protected telemetry may remain outside the artifact under protected custody.

The `.hopng` may instead carry:

- lawful summaries
- provenance
- slice digests
- protected evidence references

Prime-safe temporal inspection exposes:

- slice ids
- `n`
- timestamps
- raw-range summaries
- aggregate flags
- summary diagnostics

Prime-safe inspection does not expose raw temporal channel payloads.

## Command Surface

`Render-HOPNGPhaseStack.ps1` is live in Milestone 1.

Inputs:

- `--path <manifest-or-png>`
- optional `--view prime|privileged`
- optional `--h <raw-slice-horizon>`

Outputs:

- JSON
- simple console summary

The result includes:

- observed-set duration
- base raw cadence
- raw slice count
- observed event count
- event slice count
- phase slice count
- grouping summary
- horizon summary
- required-channel coverage
- drift flags
- topology-change flags
- view mode indicator

No PNG output is produced in Milestone 1.

## Explicit Non-Goals

- engram semantics
- formation or covenant logic
- OE or Sanctuary runtime behavior
- cross-artifact temporal comparison
- interpolation
- synthetic slices
- aesthetic image synthesis
