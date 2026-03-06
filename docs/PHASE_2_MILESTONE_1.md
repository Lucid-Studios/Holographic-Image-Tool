# Phase 2 Milestone 1: Lawful Relationality Core

This milestone is the first executable slice of Phase 2. Its purpose is to make lawful relationality real without prematurely jumping into full rendering or merge orchestration.

The milestone should establish governed relational structure first, then comparison and merge behavior later.

## Milestone Intent

Prove that a Phase 1 `.hopng` can be extended into a lawful relational artifact with explicit universes, gluing relations, and projection rules that are machine-validatable.

This milestone does **not** aim to produce final rendered composite imagery. It aims to make relational structure explicit, testable, and counterfeit-resistant.

## Scope

### Add New Phase 2 Sidecars

Implement these schema assets and runtime models:

- `oan.hopng_universe_layer.v0.1.0.json`
- `oan.hopng_gluing_manifest.v0.1.0.json`
- `oan.hopng_projection_rules.v0.1.0.json`
- `oan.hopng_legibility_profile.v0.1.0.json`

### Extend Artifact Contracts

- allow a Phase 1 artifact to reference optional Phase 2 sidecars in the manifest
- preserve backward compatibility so Phase 1 artifacts without Phase 2 sidecars still validate cleanly
- require explicit schema name and version for every new sidecar

### Validation Additions

Add lawful relational validation rules for:

- every universe layer must declare:
  - `universeId`
  - `modality`
  - local `x`, `y`, `z` coordinate semantics
  - neutral plane `N`
  - mapping to a visible projection role
- gluing manifest must declare explicit relations between source and target universes
- projection rules must define how visible projection is derived from governed universes
- legibility profile must declare the minimum conditions for a surface to count as lawfully formed

### Comparison Foundations

Add the first counterfeit-resistance checks for relational artifacts:

- distinguish artifacts with valid gluing provenance from flattened or incomplete artifacts
- detect missing required relational sidecars
- detect projection rules that reference undeclared universes or layers
- detect gluing edges that point to nonexistent universe ids

### Command Surface

Upgrade or add only the minimum command behavior needed for Milestone 1:

- extend `Test-HOPNG` to validate Phase 2 sidecars when present
- extend `Show-HOPNG` privileged output to expose Phase 2 sidecar content when present
- keep `Merge-HOPNGLayers.ps1` as a reserved or shallow diagnostic command unless the merge behavior is fully backed by real relational rules

## Out of Scope

- final composite image rendering
- full layer merge execution
- performance optimization for large relational artifacts
- event slices, phase stacks, or temporal drift
- engrammatic identity forms
- Sanctuary or OE runtime binding

## Data and Schema Decisions

### Universe Layer

Represents one coordinate-bound universe participating in the artifact.

Minimum fields:

- `schema`
- `schemaVersion`
- `artifactId`
- `universeId`
- `modality`
- `neutralPlane`
- `coordinateFrame`
- `projectionRole`

### Gluing Manifest

Represents lawful relations between universes.

Minimum fields:

- `schema`
- `schemaVersion`
- `artifactId`
- `relations`

Each relation should minimally declare:

- source universe id
- target universe id
- relation type
- whether the relation is required for lawful formation

### Projection Rules

Represents how visible projection is derived from layered universes.

Minimum fields:

- `schema`
- `schemaVersion`
- `artifactId`
- `rules`

Each rule should minimally declare:

- source universe id
- target projection role
- mapping type
- precedence or ordering value

### Legibility Profile

Represents the minimum lawful conditions for a visible surface to remain intelligible and properly formed.

Minimum fields:

- `schema`
- `schemaVersion`
- `artifactId`
- `requiredUniverses`
- `requiredRelations`
- `projectionIntegrityRequired`

## Implementation Tasks

### Schemas and Models

- add Phase 2 schema definitions in `Hdt.Schemas`
- add embedded JSON schema assets
- add Phase 2 model records in `Hdt.Core`

### Artifact Layout and Loading

- extend manifest-sidecar handling to include optional Phase 2 files
- update loaders to hydrate Phase 2 sidecars when present
- preserve Phase 1 compatibility when absent

### Validation Engine

- add schema recognition for Phase 2 sidecars
- add relational validation rules
- add explicit error codes for:
  - missing universe references
  - invalid gluing relations
  - invalid projection rules
  - invalid legibility profile

### Inspection and Diagnostics

- extend privileged inspection view to show universe layers, gluing manifests, projection rules, and legibility profile
- extend diagnostics to emit counterfeit or flattening signals tied to broken relational structure

### Testing

Add tests for:

- valid Phase 2 artifact with all four sidecars
- Phase 1 artifact still validating without Phase 2 sidecars
- gluing manifest referencing a missing universe
- projection rule referencing a nonexistent projection role
- legibility profile failing when required universes are absent
- comparison or diagnostics marking flattened artifacts as relationally incomplete

## Acceptance Criteria

- a valid artifact can carry Phase 2 sidecars and pass validation
- a Phase 1-only artifact still passes Phase 1 validation unchanged
- relational validation fails with specific actionable error codes when universes, gluing rules, or projection rules are inconsistent
- privileged inspection surfaces the new relational structures clearly
- counterfeit-resistance improves by detecting missing or broken relational structure before any rendering logic exists

## Exit Condition

Milestone 1 is complete when lawful relational structure is explicit, machine-validated, inspectable, and backward-compatible with Phase 1 artifacts.

At that point, the project can move to a later Phase 2 slice focused on actual merge behavior or projection derivation without ambiguity about the governing relational contracts.
