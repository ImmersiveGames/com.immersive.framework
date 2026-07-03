# F01 ADR DIAG 001 - Framework Facts

Status: Accepted

## Context

Human-readable logs are not enough to validate framework behavior.

## Decision

Human logs and structured framework facts are distinct.

Smokes and validators must produce objective evidence.

## Consequences

Diagnostics can be asserted by tooling and reviewed by humans without relying on message wording.

## Guardrails

- Do not treat console text as the only validation surface.
- Keep structured facts stable enough for validators.
- Report required configuration failures explicitly.
