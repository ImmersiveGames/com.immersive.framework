# Immersive Framework ADR Index

ADRs are decision history, not the current setup guide. Read [Current State](../Current/00-Current-State.md), [Roadmap](../Current/01-Roadmap.md) and [Execution Status](../Current/05-Execution-Status.md) first.

## Current product decisions

| ADR | Decision |
|---|---|
| [ADR-PROD-0001](Product/ADR-PROD-0001-product-surface-model.md) | Product surface model. |
| [ADR-PROD-0002](Product/ADR-PROD-0002-diagnostics-are-not-product-ux.md) | Diagnostics are not product UX. |
| [ADR-PROD-0003](Product/ADR-PROD-0003-domain-runtime-context-policy.md) | Scoped runtime context policy. |
| [ADR-PROD-0006](Product/ADR-PROD-0006-camera-requests-output-contexts.md) | Camera requests, typed target sources, rig materialization and output-scoped runtime authority. |
| [ADR-PROD-0007](Product/ADR-PROD-0007-player-participation-composition-and-contextual-materialization.md) | Player participation composition and contextual Actor materialization. |
| [ADR-PROD-0008](Product/ADR-PROD-0008-actor-profile-logical-host-and-presentation-materialization.md) | Actor Profile, Logical Actor Host and Presentation separation. |
| [ADR-PROD-0009](Product/ADR-PROD-0009-immutable-product-profiles-and-runtime-state.md) | Immutable product Profiles and runtime-state separation. |
| [ADR-PROD-0010](Product/ADR-PROD-0010-manual-local-player-join-and-player-input-manager-authority.md) | Manual local Player join and PlayerInputManager provisioning authority. |
| [ADR-PROD-0011](Product/ADR-PROD-0011-ordered-local-player-slot-allocation-and-visual-identity.md) | Ordered local Player Slot allocation and visual identity. |
| [ADR-PROD-0012](Product/ADR-PROD-0012-activity-player-participation-requirements-profiles.md) | Activity Player Participation Requirements Profiles. |
| [ADR-PROD-0013](Product/ADR-PROD-0013-scene-local-player-admission.md) | Admission of an explicitly authored scene-existing local Player Host. |
| [ADR-PROD-0014](Product/ADR-PROD-0014-activity-transition-authority-readiness-and-finalization.md) | Activity transition authority, readiness, commit and previous-Activity finalization contract. |
| [ADR-PROD-0015](Product/ADR-PROD-0015-pause-activity-binding-intent.md) | Activity-owned Pause intent with session-owned runtime and future activity-scoped registration. |

## Superseded product decisions

| ADR | Superseded by | Historical contribution |
|---|---|---|
| [ADR-PROD-0004](Product/ADR-PROD-0004-first-reference-product-surface.md) | P3 Canonical Player Lane and ADR-PROD-0013 | Established Player as the first product-surface proof, but its Player Recipe/Composer shape is not current Player implementation guidance. |
| [ADR-PROD-0005](Product/ADR-PROD-0005-camera-product-surface-cinemachine.md) | ADR-PROD-0006 | Established Cinemachine as mandatory and explicit target sources; its ownership and compatibility model is no longer current. |

## Historical groups

- F00–F28: framework baseline, lifecycle, diagnostics, identity, reset, transition, loading, pause, save and input boundaries.
- F34–F47: pause/input, reset/restart, runtime object and audio decisions.
- F49–F53: Player passive foundation, binding adapters, QA/consumer proofs and identity audit.
- F8R/F9R: runtime materialization and release policy.
- C1–C8: historical Cinemachine product exploration and local rig materialization. Ownership/activation decisions are superseded by ADR-PROD-0006.

All detailed ADR and note files remain in this directory for traceability. Their status and historical role are summarized in [ADR History](../History/020-ADR-History.md) and [Player Binding History](../History/070-Player-Binding-and-Composer-History.md).
