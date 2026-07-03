# F03 ADR ROUTE 001 - Route Baseline

Status: Accepted

## Context

Route is the navigable lifecycle unit below Session.

## Decision

Route has identity, Primary Scene, `RouteContentSet` and local lifecycle.

Route is not camera, audio or player.

Route switch must exit the current Route before entering the next Route.

## Consequences

Route ownership is explicit and route changes can be diagnosed as ordered lifecycle transitions.

## Guardrails

- Do not attach gameplay consumers directly to Route identity.
- Do not enter a new Route before exiting the previous Route.
- Keep Primary Scene and additional content ownership distinct.
