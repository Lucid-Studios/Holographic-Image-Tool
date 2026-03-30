# Development Path

This document keeps the Holographic Data Tool development path coherent by separating:

- the active implementation lane
- promoted doctrine that now governs execution boundaries
- deferred research lanes
- blocked later phases
- promotion rules for moving planning work into the main build

The goal is to let the repo keep advancing without accumulating planning drift, stale status claims, or doctrine that floats outside executable tool use.

## Active Main Lane

The current admitted main lane is:

- post-release maintenance of the approved Phase 3 single-clock temporal surface
- validator-first promotion prep for the deferred Phase 3 Milestone 4 lane

That lane currently includes:

- lawful single-artifact temporal rendering
- temporal state classification and derived-force reporting
- governed cross-artifact temporal comparison
- wrapper-backed repo checks and smoke paths
- committed public-safe temporal reference artifacts
- operator documentation, exit-code guidance, and release-surface alignment

Work belongs in the main lane when it is directly required to keep the approved Phase 3 operator surface more coherent, more deterministic, more verifiable, or more honestly documented, or when it is required to prepare Milestone 4 promotion without quietly turning research into carrier implementation.

## Promoted Doctrine

The following planning documents are mature enough to be treated as active build constraints rather than detached idea notes:

- [`OPERATOR_CONTINUITY_INSTRUCTIONS.md`](./OPERATOR_CONTINUITY_INSTRUCTIONS.md)
- [`HONEST_SUBSTRATE_PRINCIPLES.md`](./HONEST_SUBSTRATE_PRINCIPLES.md)
- [`PHASE_3_MILESTONE_4.md`](./PHASE_3_MILESTONE_4.md)
- [`HOGIF_BUILD_GOVERNANCE.md`](./HOGIF_BUILD_GOVERNANCE.md)
- [`HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md`](./HOGIF_ADMISSIBLE_IDENTITY_MORPHISMS.md)
- [`HOGIF_RESEARCH_TRACK.md`](./HOGIF_RESEARCH_TRACK.md)

These documents do not authorize immediate `.hogif` implementation. They do establish:

- what later work must preserve
- what kind of research is already coherent enough to constrain future implementation
- what must be validated before async or identity-bearing carrier work is allowed into the main lane

## Deferred Research Lane

The `.hogif` and heterochronous continuity track remains a research and validator-first lane, not an active carrier implementation lane.

That deferred lane currently includes:

- Milestone 4A: excursion doctrine and lifecycle vocabulary
- Milestone 4B: admissible identity morphism law
- Milestone 4C: async channel and sync-anchor semantics
- Milestone 4D: delta classes, witness tiers, and custody rules
- Milestone 4E: validator-first prototype and negative corpus
- Milestone 4F: carrier-boundary and packaging decision
- Milestone 4G: operator-surface prediction

Research work in this lane is allowed to refine doctrine, schemas, examples, and validator expectations. It is not allowed to quietly turn into renderer-first, container-first, or runtime-first implementation.

## Promotion Rules

Research or deferred work may move into the active build lane only when all of the following are true:

- the current active release gate is stable enough that the new work will not destabilize operator claims already in use
- doctrine terms are deterministic enough to support schema and validation, not just discussion
- at least one lawful example and one unlawful example exist for the promoted behavior
- Prime-safe posture remains explainable without hidden normalization or disclosure drift
- the new work can be expressed as validator-first or inspection-first progress before renderer or container complexity is introduced
- the repo docs can describe the maturity honestly without overstating implementation

If those conditions are not met, the work remains in the research lane even when the ideas are promising.

## Current Promotion Decisions

The following work is already promoted into the main lane:

- Phase 3 Milestone 2 temporal state maturity
- the first validator-first slice of Phase 3 Milestone 3 cross-artifact comparison
- wrapper-backed Phase 3 comparison smoke, failure smoke, and committed comparison artifacts
- operator cascade governance for bounded execution continuity
- the approved Phase 3 single-clock release baseline

The following work is explicitly not yet promoted:

- `.hogif` schema implementation
- `.hogif` carrier packaging
- async renderer or animation behavior
- Phase 4 engrammatic structures
- Phase 5 formation or commitment flows
- Phase 6 runtime OE or Sanctuary binding

## Full Path

The repo-wide development path now reads as:

1. keep Phase 1 and Phase 2 as stable, maintained baselines
2. maintain the approved Phase 3 single-clock operator surface and keep its release record honest
3. continue Phase 3 Milestone 4 only as doctrine, schema sketching, validator-first research, and reference-corpus maturation
4. promote heterochronous work into implementation only after the approved Phase 3 release baseline remains stable and Milestone 4 promotion rules are satisfied
5. begin Phase 4 engrammatic emergence only after Phase 3 semantics and identity-morphism dependencies are stable enough not to be reinvented midstream
6. begin Phase 5 only after engrammatic structures are lawful enough to witness review, commitment, and covenant-bearing state
7. begin Phase 6 only after commitment-bearing artifacts are stable enough for governed runtime participation

## No-Decay Rules

To keep the development path executable without error or decay:

- keep [`README.md`](../README.md), [`PHASE_ROADMAP.md`](./PHASE_ROADMAP.md), [`PHASE_BACKLOG.md`](./PHASE_BACKLOG.md), and [`holographic_data_tool_build_list.md`](../holographic_data_tool_build_list.md) aligned
- do not leave implemented work described as merely planned
- do not leave research work implied as already operational
- keep public wrapper smoke paths ahead of or equal to the public operator surface
- keep committed examples public-safe and free of reusable private keys
- prefer promotion through validation, reference artifacts, and inspection behavior rather than through prose alone
