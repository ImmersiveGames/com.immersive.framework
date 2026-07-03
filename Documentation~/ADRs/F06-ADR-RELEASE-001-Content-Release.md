# F06 ADR RELEASE 001 - Content Release

Status: Accepted

## Context

The framework must distinguish removing owned content from resetting active state.

## Decision

Release is different from Reset.

Release frees owned content.

Reset reconfigures active state without unloading or destroying by default.

## Consequences

Lifecycle exits can release ownership while future reset phases can preserve active objects when appropriate.

## Guardrails

- Do not use Reset as a synonym for Release.
- Do not destroy/unload by default during Reset.
- Release must follow explicit ownership.
