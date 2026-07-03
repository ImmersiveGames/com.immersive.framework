# F08 ADR RUNTIME 001 - Runtime Materialization

Status: Accepted

## Context

The framework needs logical ownership for content created during runtime.

## Decision

Runtime materialization creates runtime content with request/result/handle.

Runtime Root is not Content Anchor.

Materialization does not belong to Actor, Projectile or Pool.

## Consequences

Runtime content can be tracked by ownership and release contracts before gameplay consumers exist.

## Guardrails

- Do not merge Runtime Root with Content Anchor.
- Do not let Actor, Projectile or Pool own materialization.
- Keep physical Unity execution behind adapter contracts.
