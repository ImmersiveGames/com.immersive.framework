# F05 ADR LOCAL 001 - Local Identity and Contribution

Status: Accepted

## Context

Local contributions need stable identity and explicit requiredness inside the framework.

## Decision

`LocalContribution` depends on explicit `LocalContentIdentity`.

Do not use `GameObject.name`, scene path or hierarchy path as a functional key.

Required/Optional must be explicit.

## Consequences

Local content can be validated without relying on fragile scene naming.

## Guardrails

- Do not infer local identity from hierarchy.
- Do not use scene path as ownership.
- Required contributions must fail explicitly when missing.
