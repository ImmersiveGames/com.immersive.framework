# F00 ADR BL 001 - Baseline Reconciliation

Status: Accepted

## Context

The framework needed a corrected baseline after older docs mixed lifecycle architecture with gameplay-specific systems.

## Decision

`com.immersive.framework` is a lifecycle, content and contribution framework.

It is not a camera, audio, actor or gameplay framework. The old CameraFlow direction must not dictate Framework Core.

Contradictory code or documentation must be corrected instead of preserved as competing guidance.

## Consequences

Framework Core remains focused on stable lifecycle and content contracts. Gameplay-specific capabilities are consumers opened only when the canonical plan allows them.

## Guardrails

- Do not use old CameraFlow as a core blueprint.
- Do not preserve contradictory docs for history.
- Keep the real historical summary in the canonical roadmap.
