# Immersive Framework Documentation

Entry point for `com.immersive.framework` documentation.

Use this folder as a **small current surface** plus a **numbered history**. Do not treat every historical file as active implementation guidance.

## Read in this order

| Order | Document | Purpose |
|---:|---|---|
| 1 | [`Current/00-Current-State.md`](Current/00-Current-State.md) | What the package supports now. |
| 2 | [`Guides/Usage/index.html`](Guides/Usage/index.html) | Current user-facing HTML guide opened from Project Settings. |
| 3 | [`Current/01-Roadmap.md`](Current/01-Roadmap.md) | Selected current roadmap and next candidate lanes. |
| 4 | [`Current/02-Usage-Map.md`](Current/02-Usage-Map.md) | Which runtime surface to use for common game tasks. |
| 5 | [`History/000-INDEX.md`](History/000-INDEX.md) | Numbered historical navigation. |
| 6 | [`ADRs/ADR-INDEX.md`](ADRs/ADR-INDEX.md) | Detailed decision archive. |

## Documentation policy

| Area | Policy |
|---|---|
| Current docs | Short, maintained, canonical. |
| HTML guide | Stable path: `Documentation~/Guides/Usage/index.html`. Project Settings depends on this path. |
| ADRs | Keep detailed historical decisions. They are not the quick-start path. |
| History | Keep consolidated phase/guide/roadmap history in numbered files. |
| Planning drafts | Avoid long-lived active drafts. Promote decisions to `Current/` or archive to `History/`. |
| Old phase guides | Do not keep one file per old cut unless it is still directly useful. Summarize in numbered history. |

## Current model summary

The current framework baseline includes:

```text
Application boot
Route lifecycle
Activity lifecycle
Route/Activity scene composition
UIGlobal resident surfaces
Transition / Loading / Pause surfaces
Gate and Unity PlayerInput gate adapter
Snapshot / Preferences / Progression Save boundaries
ResetSubject / ResetParticipant / ResetRegistry / ResetExecutor
ObjectResetTrigger / ObjectResetGroupTrigger / ActivityRestartTrigger
UnityResetSubjectAdapter
UnityResetParticipantBehaviour
IUnityResettable gameplay component bridge
FIRSTGAME reset usage proof
```

## Reset model summary

Canonical reset path:

```text
UnityResetSubjectAdapter
  -> registers ResetSubject
  -> discovers UnityResetParticipantBehaviour
  -> discovers IUnityResettable gameplay components
  -> registers ResetParticipants
ResetExecutor
  -> executes selected subjects/participants
```

Game code should not access `ResetRegistry` directly. Use authored triggers, `UnityResetSubjectAdapter`, `UnityResetParticipantBehaviour`, or gameplay components implementing `IUnityResettable`.

## Stable public guide path

Do not rename this path unless editor settings are changed in the same cut:

```text
Documentation~/Guides/Usage/index.html
```
