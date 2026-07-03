# F10 ADR PAUSE 001 - Pause as Consumer

Status: Accepted / Historical

## Context

Pause crosses lifecycle, input and content visibility concerns, but it is not a core owner.

This ADR remains a histórical F10 decision. It is not the current implementation plan for Pause.

## Decision

Pause is a consumer of lifecycle, input and Content Anchor.

Pause does not own Route or Activity.

Pause does not define core Content Anchor.

Current operational planning moved to:

```text
F20-ADR-PAUSE-002-Pause-State-and-Gate.md
F23-ADR-PAUSE-003-Pause-Content-Overlay-Input-Boundary.md
```

The current plan treats Pause as state + Gate blocker first, then content/overlay/input as consumers.

## Consequences

Pause behavior can be layered on top of core contracts without redefining the framework.

## Guardrails

- Do not let Pause own Route or Activity state.
- Do not define anchor semantics inside Pause.
- Keep pause-specific behavior in consumer space.
- Do not treat Pause as Activity.
- Do not treat `Time.timeScale` as the canonical Pause contract.
