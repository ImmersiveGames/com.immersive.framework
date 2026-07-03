# F09 ADR ANCHOR 002 - Content Anchor Binding

Status: Accepted

## Context

Declared anchors and runtime content need an explicit binding relationship.

## Decision

Binding connects `RuntimeContent` to `ContentAnchor`.

Consumers do not instantiate directly without passing through the contract.

Binding and release have explicit order.

## Consequences

Consumers can request placement through framework contracts without owning lifecycle or release order.

## Guardrails

- Do not instantiate directly from a consumer as canonical flow.
- Do not release before binding ownership is resolved.
- Keep binding logical unless a Unity adapter is explicitly in scope.
