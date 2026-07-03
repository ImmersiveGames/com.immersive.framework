# F10 ADR INPUT 001 - Input Ownership

Status: Accepted

## Context

Input is needed by gameplay but must not own framework lifecycle.

## Decision

Input is an intermediate consumer.

`InputMode` must be a typed contract.

Action map string is not a functional key.

## Consequences

Input can follow Session/Route/Activity state without becoming a lifecycle owner.

## Guardrails

- Do not key behavior on action map strings.
- Do not let Input own Route or Activity lifecycle.
- Keep Input behind typed mode contracts.
