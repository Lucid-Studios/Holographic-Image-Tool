# Data Classification Standard

## Purpose

This repository uses a simple Lucid Technologies data-classification model to support AI development, artifact governance, and public-safe handling.

## Classification Levels

### Public

Information approved for public repository visibility.

Examples:

- released source code
- public documentation
- public schemas
- sanitized example artifacts
- public issue and pull request discussion

Handling:

- may be committed to the repository
- should still avoid unnecessary sensitive metadata

### Internal

Operational or design information intended for Lucid Technologies collaborators but not necessarily harmful if disclosed.

Examples:

- roadmap drafts
- internal design discussion
- non-sensitive architecture notes

Handling:

- do not commit unless explicitly approved for repository publication
- keep clearly separate from restricted material

### Restricted

Sensitive technical, business, or participant-related information that must not be committed publicly.

Examples:

- credentials, tokens, private keys
- unpublished security details
- private datasets or participant information
- proprietary third-party material without publication rights

Handling:

- never commit to the public repository
- report accidental exposure immediately through the security path

### Cryptic

Governed references or payload-adjacent information that may exist in system workflows but must remain pointer-only in Prime-safe contexts.

Examples:

- protected artifact references
- role-gated payload locations
- restricted external storage identifiers
- continuity references that imply governed access

Handling:

- do not embed full cryptic payloads in public-safe artifacts
- use pointer-only references with explicit policy metadata
- review for boundary leakage before merge

## Holographic Data Tool Rules

- committed example artifacts must be Public only
- private signing keys are Restricted and must never be committed
- trust-envelope public keys may be committed only for sanitized examples
- Prime-safe outputs must never expose Restricted or full Cryptic content
- generated or transformed datasets must preserve provenance and declared classification

## Reviewer Checklist

- Is the classification explicitly known?
- Does the change move data from a higher class to a lower class?
- Are examples sanitized and safe for publication?
- Are cryptic references pointer-only?
- Were secrets or sensitive keys introduced anywhere in history, docs, tests, or examples?
