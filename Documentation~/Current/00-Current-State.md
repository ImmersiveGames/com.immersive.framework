# 00 — Current State

Status: **canonical current state after Reset Reform preview.12**.

## Closed baseline

| Area | Current state |
|---|---|
| Bootstrap | `GameApplicationAsset` owns startup route and validation mode. |
| Route | Route lifecycle, scene composition, runtime route scope and content enter/exit are available. |
| Activity | Startup Activity, Activity clear/re-enter and Activity readiness are available. |
| UIGlobal | Resident global scene can host Transition, Loading and Pause adapters. |
| Transition | Route/Activity transition windows support Unity surface/effect adapters and Gate blocking. |
| Loading | Loading surface/progress evidence is available through UIGlobal adapters. |
| Pause | Logical Pause, Pause input action trigger, resident Pause surface and `Time.timeScale` support are available. |
| Gate | Gate blockers are available for input, interaction and gameplay action. |
| Unity Input | `UnityPlayerInputGateAdapter` can block a gameplay `PlayerInput` action map during Pause/Transition blockers. |
| Snapshot | Snapshot boundary exists as diagnostic/runtime state capture. |
| Preferences | Preferences boundary exists. |
| Progression Save | Adapter boundary exists; full save engine remains interchangeable/future. |
| Reset | Reset Reform is current: `ResetSubject`, `ResetParticipant`, `ResetRegistry`, `ResetSelectionConfig`, `ResetExecutor`. |
| Unity Reset | `UnityResetSubjectAdapter`, built-in participants and `IUnityResettable` gameplay component bridge are available. |
| Runtime prefab reset | Runtime id generation uses authored prefix + monotonic runtime counter. |
| Restart | `ActivityRestartTrigger` wraps reset + clear + re-enter inside one visual transition window. |
| FIRSTGAME | Reset Room and Activity Restart usage model passed with runtime objects. |
| Consumer project split | QA, FIRSTGAME and package roles are frozen in [`03-Consumer-Project-Roles.md`](03-Consumer-Project-Roles.md). |

## Closed Reset Reform sequence

| Cut | Result |
|---|---|
| preview.12A | Reset registry and subject/participant model. |
| preview.12B | Unity reset subject adapter and built-in participants. |
| preview.12C | Reset executor. |
| preview.12D | Object reset trigger rewrite. |
| preview.12E | Activity restart integration. |
| preview.12F | Runtime prefab reset smoke. |
| preview.12G | Old reset path cleanup and ADR supersession. |
| preview.12H | Activity restart visual ordering. |
| preview.12I | Reset subject adapter log noise cleanup. |
| preview.12J | `IUnityResettable` gameplay component bridge. |
| preview.12K | HTML guide advanced reset/programmatic usage. |

## Current reset usage model

```text
Scene object or prefab
  UnityResetSubjectAdapter
    Subject Id / Runtime Id Prefix
    Scope = Route / Activity / Runtime
    Include Unity Resettable Components = true
  UnityTransformResetParticipant              optional built-in
  UnityGameObjectActiveResetParticipant       optional built-in
  GameplayComponent : IUnityResettable        recommended gameplay-code path
```

## Current validation rule

Use both layers when relevant:

```text
1. Framework QA synthetic smoke proves framework behavior.
2. FIRSTGAME smoke proves real usage model.
```

For Reset Reform, both layers passed.

## Consumer project ownership

Current ownership rule:

```text
Framework package: contracts, runtime, editor tooling, validators, diagnostics and official docs.
QA Project: synthetic smokes, probes, artificial scenarios and negative cases.
FIRSTGAME: minimal real game usage proof.
```

Do not move canonical framework docs into consumer `Assets/` folders. Consumer projects may keep local READMEs only for project-specific operation.

## Consumer Project Separation

Status after the controlled reset cleanup sequence:

```text
POST-RESET-B1 — PASS
POST-RESET-B2 — PASS
POST-RESET-B3 — PASS
POST-RESET-B4 — PASS
POST-RESET-B5 — PASS
POST-RESET-B5A — PASS
POST-RESET-B6A — PASS
POST-RESET-B6B0 — PASS
POST-RESET-B6B — PASS
POST-RESET-B6F — PASS
```

Current separation rule:

```text
Framework package: canonical documentation and framework-owned code.
QA Project: Assets/ImmersiveFrameworkQA only for QA operation.
FIRSTGAME: Assets/_Project only for real minimal game usage.
```
