# F06 ADR SCENE 001 - Route Scene Composition

Status: Accepted

## Context

Route scene composition needs explicit planning and result reporting without becoming runtime materialization.

## Decision

Route scene composition uses plan/result.

Primary Scene and additional scenes have distinct ownership.

Additive scene support is not runtime materialization.

## Consequences

Scene composition can be ordered and diagnosed while keeping runtime object creation in a separate capability.

## Guardrails

- Do not treat additive scenes as runtime materialized objects.
- Do not merge Primary Scene ownership with additional scenes.
- Keep scene composition distinct from Content Anchor binding.
