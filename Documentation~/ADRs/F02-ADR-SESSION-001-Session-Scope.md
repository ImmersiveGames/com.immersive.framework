# F02 ADR SESSION 001 - Session Scope

Status: Accepted

## Context

Session defines the application-level runtime scope above Route.

## Decision

Session is the scope above Route.

The runtime host owns app/session state and is not a public service locator.

`SessionContentSet` may exist without becoming a global manager.

## Consequences

Session can coordinate app-level lifecycle without exposing global mutable access.

## Guardrails

- Do not expose runtime host as service locator.
- Do not make Session content a generic global manager.
- Keep Route and Activity lifecycle below Session ownership.
