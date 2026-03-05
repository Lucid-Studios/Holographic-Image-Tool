# Security Policy

## Supported Scope

Security review applies to:

- artifact canonicalization and digesting
- signature generation and verification
- trust envelope handling
- visibility boundary enforcement
- CLI input handling
- schema loading and artifact parsing
- example artifacts and documentation that might expose sensitive material

## Reporting a Vulnerability

Do not open public issues for:

- credential exposure
- private key exposure
- signature bypass or trust envelope weaknesses
- data leakage across Prime/Cryptic boundaries
- artifact tampering paths
- unsafe AI misuse enablement discovered in repository logic or documentation

Report privately to maintainers through GitHub security reporting if enabled for the repository. If GitHub security reporting is not enabled, contact maintainers directly before public disclosure.

## Lucid Technologies Security Expectations

- do not commit private keys, tokens, or restricted data
- treat provenance, integrity, and authenticity failures as security issues
- treat AI misuse pathways and unsafe data disclosure as security issues
- prefer coordinated disclosure and reproducible reports

## What to Include

- affected files, commands, or flows
- clear reproduction steps
- impact summary
- whether the issue affects confidentiality, integrity, availability, provenance, or misuse risk
- whether the issue is already exploited or only theoretical
