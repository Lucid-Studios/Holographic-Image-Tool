# Engineered Cognition Capture Bundle

This document defines the clean HDT-native handoff for the first real `.hopng` line backed by Engineered Cognition (`EC`) rather than a placeholder projection.

The goal is not to ask `EC` for full holographic cognition. The goal is to ask `EC` for one bounded capture package per cognitive event that HDT can validate, inspect, render deterministically, hash, and sign without counterfeiting finalization.

## Purpose

The first non-placeholder `.hopng` line should be built from a bounded capture bundle that:

- preserves event timing and slice provenance
- carries renderable state vectors rather than opaque claims
- assigns those states to declared universes and visible layers
- preserves protected-evidence custody
- remains support-only unless a later phase explicitly promotes stronger authority

This keeps HDT aligned with the current validator-first Phase 4 bridge while making room for a real image-generation path.

## Required Surfaces

For the first real `.hopng` line, `EC` should emit six surfaces:

1. timing and event slicing
2. per-slice renderable state vectors
3. universe and layer assignment
4. optical channel declaration
5. custody and evidence references
6. optional support-boundary posture for finalization-adjacent cases

## Current Model Mapping

The current HDT model already has a natural home for each surface:

- timing and slices:
  - `EventSliceSet`
  - `PhasePolicy`
- renderable state vectors:
  - `TemporalUniverseState` inside `EventSliceSet` and `PhaseSliceSet`
- universe and layer assignment:
  - `UniverseLayerSet`
  - `HopngLayerMap`
  - `DepthField`
- optical channel declaration:
  - `OpticalChannelsDefinition`
- custody and evidence references:
  - `ProtectedEvidenceRefs` in `ObservedSetHeader` and `EventSlice`
- support-boundary posture:
  - `PerspectivalEngramSupport`
  - `ParticipatoryEngramSupport`

## Minimal Contract

One bounded capture bundle per cognitive event is enough for the first non-placeholder line:

```json
{
  "artifactId": "ec-event-0001",
  "eventSliceSet": {},
  "phasePolicy": {},
  "universeLayerSet": {},
  "layerMap": {},
  "depthField": {},
  "opticalChannels": {},
  "projectionRules": {},
  "support": null
}
```

The first required payload should stay narrow:

- required slice scalars:
  - `pressure`
  - `drift`
  - `bloom`
- optional slice scalars:
  - `force`
  - `opacity`
  - `hue`
  - `saturation`
- required universes:
  - `visible_projection`
  - `cryptic_support`
- default custody posture:
  - `primeSafeInspectionMode = metadata_only`
  - `dataCustodyMode = protected_external`
- default support posture:
  - `supportOnly = true`
  - `phase5HandoffReady = false`

## Deterministic Render Policy Gap

The current model cleanly declares channels and projection intent, but it does not yet fully declare numeric state-to-pixel transfer behavior.

Today:

- `OpticalChannelsDefinition` declares channel identity, required/reserved posture, and canonical meaning
- `ProjectionRules` declares source universe, target projection role, mapping type, and precedence

That is enough for analytic declaration, but not enough for deterministic rendering by itself. The first non-placeholder image path therefore still needs one of these:

- a `renderPolicy` sidecar that carries deterministic transfer functions and composition rules
- an extension of `ProjectionRule` with explicit state-to-pixel mapping parameters

Until that surface exists, HDT can validate, compare, hash, and sign bounded capture bundles, but it cannot honestly claim to render a semantically complete engram image.

## Initial Rendering Boundary

The first renderer should remain conservative:

- deterministic and reproducible
- bounded to declared universes and channels
- driven only by captured slice values and declared policy
- free of hidden interpolation or synthetic event fabrication
- support-only when Phase 4 posture is present

That means the first renderer should treat the visible PNG as a lawful projection of captured analytic state, not as a complete holographic or identity-bearing truth surface.

## Recommended First Implementation Order

1. define the bounded `EC` capture bundle as the required HDT handoff
2. add the deterministic render-policy surface
3. replace the placeholder PNG with renderer output derived from the bundle
4. keep the rendered output inside the existing digest and signature workflow
5. preserve Phase 4 support-only posture until later promotion explicitly authorizes stronger claims

## Short Form

`EC` should emit time-bounded, evidence-custodied, renderable slice bundles with explicit universes, layers, and deterministic projection policy. It should not be asked to emit full holographic cognition or engram-finalized meaning yet.
