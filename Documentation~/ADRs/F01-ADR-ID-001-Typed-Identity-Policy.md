# F01 ADR ID 001 - Typed Identity Policy

Status: Accepted

## Context

The framework needs stable identifiers across Session, Route, Activity, Local content, Runtime content and future consumers.

## Decision

Strings, paths and `GameObject.name` may appear in diagnostics.

They must not be the canonical functional key.

IDs must have an explicit domain.

## Consequences

Identity comparisons stay inside their own domain. Diagnostics can remain readable without becoming runtime behavior.

## Guardrails

- Do not parse strings to fabricate identity.
- Do not compare identities from different domains.
- Do not use scene path or hierarchy path as a functional key.
