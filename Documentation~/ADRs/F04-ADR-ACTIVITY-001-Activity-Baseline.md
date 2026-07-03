# F04 ADR ACTIVITY 001 - Activity Baseline

Status: Accepted

## Context

Activity represents a contextual playable step inside an active Route.

## Decision

Activity is a playable/contextual step inside Route.

`ActivityContentSet` and readiness are minimal core concepts.

Local Activity content is not canonical materialization.

## Consequences

Activity can express readiness and content participation without becoming a prefab/materialization system.

## Guardrails

- Do not treat Activity content as runtime materialization.
- Do not make Activity own Route lifecycle.
- Keep readiness explicit and diagnosable.
