# Phase 1 Artifact Model

Phase 1 defines a `.hopng` as a visible PNG projection plus deterministic sidecars. The PNG is the projection surface; the JSON files govern lawful structure, trust, and extension points.

## Implemented sidecars

- `*.hopng.json`: root manifest with file digests, sidecar references, visibility policy, and phase reservations
- `*.layer-map.json`: coordinate-bound layers with `x`, `y`, `z`, modality, and neutral plane
- `*.trust-envelope.json`: signer identity, key id, public key, signing scope, and signature pointer
- `*.transform-history.json`: transform provenance for the artifact
- `*.depth-field.json`: neutral plane and depth range declarations
- `*.hash.json`: manifest canonical hash and artifact-set digest
- `*.signature.json`: Ed25519 signature over the hash sidecar

## Validation rules

- all required files must exist beside the PNG projection
- manifest, layer map, trust envelope, transform history, and depth field must match supported schema/version pairs
- manifest digests must match the projection and governed sidecars
- hash sidecar must match the manifest canonical bytes and file digest set
- signature sidecar must verify against the public key in the trust envelope
- every layer must declare a coordinate frame and neutral plane
- Prime-safe projection must stay enabled in v1
- cryptic references may only be pointer URIs and require `crypticPointersAllowed=true`

## Prime-safe inspection

Prime-safe views expose:

- approved manifest metadata
- cryptic pointer summaries
- diagnostic results

Privileged views expose the full artifact set, trust material, and validation errors.
