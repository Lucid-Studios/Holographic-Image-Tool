# Contributing

## Lucid Technologies Contribution Standard

This repository follows:

- U.S.-aligned AI risk management and trustworthy development practices
- citizen science and scientific integrity expectations for transparency, consent, provenance, and data quality
- responsible data lifecycle handling and documentation
- GitHub community health and pull request hygiene best practices

This repository is not a substitute for legal review. Where a contribution raises regulatory, privacy, export control, or human-subject concerns, escalate to maintainers before merge.

## Before You Start

- read [`README.md`](../README.md)
- read [`docs/ARTIFACT_MODEL.md`](../docs/ARTIFACT_MODEL.md)
- read [`docs/LUCID_TECHNOLOGIES_STANDARDS.md`](../docs/LUCID_TECHNOLOGIES_STANDARDS.md)
- read [`docs/LICENSING_POLICY.md`](../docs/LICENSING_POLICY.md)
- read [`docs/DCO_CLA_POLICY.md`](../docs/DCO_CLA_POLICY.md)
- read [`docs/AI_USAGE_DISCLOSURE.md`](../docs/AI_USAGE_DISCLOSURE.md)
- read [`docs/DATA_CLASSIFICATION.md`](../docs/DATA_CLASSIFICATION.md)
- check open issues before starting new work

## Contribution Rules

- keep changes scoped to one problem or capability
- preserve deterministic artifact behavior and trust validation
- add or update tests for behavioral changes
- do not commit secrets, private keys, tokens, or restricted datasets
- do not weaken Prime-safe visibility rules without explicit maintainer approval
- document any external standard, policy, or schema dependency introduced by the change
- classify new data, artifacts, or examples before submitting them
- disclose material AI assistance in the pull request
- sign commits off with `git commit -s` unless maintainers explicitly waive the requirement

## Git and Pull Requests

- branch from `main`
- use focused commits with explicit intent
- sign commits with DCO-style sign-off by default
- include architecture and testing context in commit and PR descriptions
- link related issues or explain why none exists
- keep pull requests reviewable; split unrelated work
- expect CLA escalation only when maintainers request it for ownership or IP-risk reasons

## Commit Guidance

Use structured commit messages:

```text
<type>(<component>): <short summary>

Context:
Explain the problem or goal addressed by this change.

Implementation:
Describe what was added, modified, or removed.

Architecture Layer:
Specify the affected subsystem.

Testing:
Describe how the change was verified.

Impact:
Note runtime effects, configuration changes, or migration requirements.
```

## Testing

Run the relevant checks before opening a pull request:

```powershell
dotnet build .\HolographicDataTool.sln
dotnet test .\Hdt.Tests\Hdt.Tests.csproj
```

If a contribution affects artifact integrity, also validate a sample artifact:

```powershell
.\Test-HOPNG.ps1 --path .\examples\phase1-sample.hopng.json --json
```

## AI, Data, and Citizen Science Expectations

- identify data origin and provenance
- distinguish observation, inference, and generated content
- avoid unsupported claims of compliance or certification
- minimize collection and retention of sensitive data
- state participant, contributor, or subject assumptions where relevant
- flag contributions that may materially affect safety, misuse resistance, or scientific validity
- mark committed material as Public, Internal, Restricted, or Cryptic in review discussion where relevant
- keep Restricted data out of the repository and keep Cryptic data pointer-only in public-safe artifacts
