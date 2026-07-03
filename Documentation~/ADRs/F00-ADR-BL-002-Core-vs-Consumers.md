# F00 ADR BL 002 - Core vs Consumers

Status: Accepted

## Context

The framework requires a strict split between framework ownership and game-facing consumers.

## Decision

Core defines owners, identity, lifecycle, content, contribution, reset, release and diagnostics.

Consumers use the core. Consumers do not discover the world by themselves and do not own lifecycle.

## Consequences

Consumers can be added without redefining core contracts. Framework behavior remains testable through explicit ownership and diagnostics.

## Guardrails

- Do not let consumers create implicit lifecycle.
- Do not bypass core identity or ownership.
- Do not hide discovery behind service-locator behavior.
