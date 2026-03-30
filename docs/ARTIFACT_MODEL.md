# HOPNG Artifact Model

Phase 1 defines a `.hopng` as a visible PNG projection plus deterministic sidecars. Phase 2 extends that carrier with lawful relational structure and governed projection support. Phase 3 Milestone 1 extends it further with single-artifact temporal summaries, derived phase slices, and custody-safe diagnostic rendering. The PNG remains the projection surface; the JSON files govern lawful structure, trust, relation, comparison, and temporal provenance.

## Implemented sidecars

- `*.hopng.json`: root manifest with file digests, sidecar references, visibility policy, and phase reservations
- `*.layer-map.json`: coordinate-bound layers with `x`, `y`, `z`, modality, and neutral plane
- `*.trust-envelope.json`: signer identity, key id, public key, signing scope, and signature pointer
- `*.transform-history.json`: transform provenance for the artifact
- `*.depth-field.json`: neutral plane and depth range declarations
- `*.hash.json`: manifest canonical hash and artifact-set digest
- `*.signature.json`: Ed25519 signature over the hash sidecar

## Phase 2 relational sidecars

- `*.universe-layer.json`: declared universes with modality, coordinate frame, neutral plane, and projection role
- `*.gluing-manifest.json`: explicit inter-universe relations and formation-required links
- `*.projection-rules.json`: ordered rules that map source universes to visible projection roles
- `*.legibility-profile.json`: required universes, required relations, and projection-integrity expectations

## Phase 3 temporal sidecars

- `*.event-slices.json`: observed-set header plus grouped event slices with raw-range provenance, protected evidence references, and per-universe `pressure`, `drift`, and `bloom`
- `*.phase-slices.json`: derived phase slices built from contiguous event windows with exact source-event and raw-range provenance
- `*.phase-policy.json`: raw cadence, event grouping, phase window mode, explicit phase-window duration and max span, legacy raw comparison horizon, explicit comparison-horizon declarations, aggregation, explicit temporal state thresholds, and inspection policy
- `*.optical-channels.json`: channel catalog declaring required and reserved temporal channels and their canonical analytic meaning

## Validation rules

- all required files must exist beside the PNG projection
- manifest, layer map, trust envelope, transform history, and depth field must match supported schema/version pairs
- manifest digests must match the projection and governed sidecars
- hash sidecar must match the manifest canonical bytes and file digest set
- signature sidecar must verify against the public key in the trust envelope
- every layer must declare a coordinate frame and neutral plane
- Prime-safe projection must stay enabled in v1
- cryptic references may only be pointer URIs and require `crypticPointersAllowed=true`

When Phase 2 sidecars are declared:

- every referenced relational sidecar must exist and have a manifest digest entry
- universes must be unique and coordinate-complete
- gluing relations must reference declared universes
- projection rules must target known projection roles
- legibility requirements must reference known universes and relations
- governed derivation must be able to distinguish:
  - `LawfullyFormed`
  - `StructurallyIncomplete`
  - `FlattenedOrUnsupported`

When Phase 3 sidecars are declared:

- `observedDurationMs`, `baseSliceCadenceMs`, and `rawSliceCount` must agree
- event and phase slices must be strictly ordered by `n`
- event slices must use contiguous raw ranges only
- phase slices must reference existing event slices and contiguous event windows only
- every participating universe id must exist in the Phase 2 universe declarations
- required temporal channels `pressure`, `drift`, and `bloom` must exist for every participating universe state
- aggregation policies are limited to `latest`, `mean`, and `delta`
- phase-window duration and max span must declare a lawful time basis for each derived phase slice
- comparison horizons must be explicit, deterministic, and align their primary horizon with the stored raw-slice horizon basis
- state-threshold policy must declare deterministic threshold and derived-force weight values
- slice digests must be stable and unique within each slice family
- Prime-safe views must not expose raw temporal payloads
- no interpolation or synthetic slices are allowed in Milestone 1

## Governed derivation and comparison

`merge-layers` derives an ordered projection trace from the Phase 2 sidecars. It evaluates projection rules by precedence, records participating universes and relations, and applies the legibility profile as a runtime gate.

`compare-surfaces` compares two artifacts by projection-formation class rather than image appearance alone. A lawful artifact can therefore be distinguished from a flattened or incomplete surface even when the visible projection might look similar.

`render-phase-stack` derives and validates a single-artifact temporal stack from the Phase 3 sidecars. It reports observed-set duration, raw cadence, event and phase slice counts, explicit timestamp spans, required-channel coverage, explicit comparison-horizon summaries, drift flags, relation-graph topology changes, and temporal state summaries backed by the explicit phase-policy thresholds and horizon basis.

`compare-phase-stacks` compares two lawful Phase 3 artifacts under explicit basis alignment. It reports basis compatibility, final state compatibility, drift and derived-force deltas, topology delta count, state-rank delta, classification reason, and deterministic classification into `Convergent`, `Delayed`, `Divergent`, `Incompatible`, or `FlattenedOrUnsupported`. The comparison remains policy-first: there is no hidden normalization, interpolation, or image-native rendering step.

## Prime-safe inspection

Prime-safe views expose:

- approved manifest metadata
- cryptic pointer summaries
- diagnostic results
- temporal slice metadata and summary flags when Phase 3 is present

Privileged views expose the full artifact set, trust material, and validation errors.
