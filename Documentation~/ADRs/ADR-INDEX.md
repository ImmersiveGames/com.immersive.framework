# Immersive Framework ADR Index

ADRs are decision history, not the current setup guide. Read [Current State](../Current/00-Current-State.md) and [Roadmap](../Current/01-Roadmap.md) first.

## Current product decisions

| ADR | Decision |
|---|---|
| [ADR-PROD-0001](Product/ADR-PROD-0001-product-surface-model.md) | Product surface model. |
| [ADR-PROD-0002](Product/ADR-PROD-0002-diagnostics-are-not-product-ux.md) | Diagnostics are not product UX. |
| [ADR-PROD-0003](Product/ADR-PROD-0003-domain-runtime-context-policy.md) | Scoped runtime context policy. |
| [ADR-PROD-0004](Product/ADR-PROD-0004-first-reference-product-surface.md) | First reference product surface. |
| [ADR-PROD-0006](Product/ADR-PROD-0006-camera-requests-output-contexts.md) | Camera requests, rig materialization and output-scoped runtime authority. |

## Superseded product decisions

| ADR | Superseded by | Historical contribution |
|---|---|---|
| [ADR-PROD-0005](Product/ADR-PROD-0005-camera-product-surface-cinemachine.md) | ADR-PROD-0006 | Established Cinemachine as mandatory and explicit target sources; its ownership and compatibility model is no longer current. |

## Historical groups

- F00–F28: framework baseline, lifecycle, diagnostics, identity, reset, transition, loading, pause, save and input boundaries.
- F34–F47: pause/input, reset/restart, runtime object and audio decisions.
- F49–F53: Player passive foundation, binding adapters, QA/consumer proofs and identity audit.
- F8R/F9R: runtime materialization and release policy.
- C1–C8: historical Cinemachine product exploration and local rig materialization. Ownership/activation decisions are superseded by ADR-PROD-0006.

All detailed ADR and note files remain in this directory for traceability. Their status and historical role are summarized in [ADR History](../History/020-ADR-History.md) and [Player Binding History](../History/070-Player-Binding-and-Composer-History.md).
