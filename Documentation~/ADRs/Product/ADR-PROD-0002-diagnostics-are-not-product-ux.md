# ADR-PROD-0002 — Diagnostics Are Not Product UX

Status: Accepted  
Date: 2026-07-09

## Context

The framework has strong diagnostics: validators, readiness checks, smokes, QA panels, logs, and repair proofs.

In practice, several workflows currently depend on these diagnostics as the primary way to understand or complete setup. FIRSTGAME exposed this clearly: player binding, camera setup, and repair workflows can be validated, but the user experience is still tool/validator-first instead of authoring-first.

Diagnostics prove correctness. They do not create a usable product surface.

## Decision

Diagnostics are support tooling, not the primary UX.

The primary user flow for a framework feature should be:

```text
Create / choose intent
Configure through Recipe/Profile/Template or Composer/Authoring
Apply / Rebuild materialization when needed
Use in Play Mode
Inspect Advanced/Debug diagnostics when needed
```

Validators, smokes, readiness checks, QA canvases, and repair proofs must not be the only evidence that a feature is product-ready.

## Rules

- A feature is not product-ready only because it passes smoke.
- A feature is not product-ready only because a validator can confirm the wiring.
- QA tools must remain available for regression and diagnostics.
- Designer-facing docs should explain creation and configuration before logs and smoke output.
- Advanced/Debug views may expose technical evidence, but the default surface should communicate domain intent.

## Consequences

- Future implementation plans must separate product acceptance from technical smoke acceptance.
- Menus that only validate, repair, or run smoke should not be presented as the main workflow for common users.
- Existing diagnostics can remain, but recurring features need product surfaces above them.
- QAFramework proves technical contracts; FIRSTGAME proves usability in a real game.

## Non-goals

- This ADR does not remove diagnostics.
- This ADR does not reduce fail-fast validation.
- This ADR does not prevent technical users from accessing full evidence in Advanced/Debug mode.

## Affected systems

Validators, QA tools, documentation, Player, Camera, Route/Activity, Reset, Content, and future product surfaces.
