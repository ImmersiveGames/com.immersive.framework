# 00 — Current State

Status: **canonical current state after F49M Player Passive Binding Foundation consolidation**.

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
| Player identity | `PlayerSlot`, `ActorId`, Actor readiness and `PlayerEntry` passive evidence are available. |
| Player topology | `PlayerTopologyValidator` validates slots, occupancies and entries. |
| Player view | `PlayerView` and `PlayerViewTopologyValidator` validate passive view evidence. |
| Player control | `PlayerControl` and `PlayerControlTopologyValidator` validate passive control evidence. |
| Player binding readiness | `PlayerBindingReadinessSummarizer` aggregates topology readiness for future binding. |
| Player binding diagnostics | `PlayerBindingDiagnosticReporter` produces human-readable passive diagnostics. |
| Consumer project split | QA, FIRSTGAME and package roles are frozen in [`03-Consumer-Project-Roles.md`](03-Consumer-Project-Roles.md). |

## Closed F49 passive player sequence

| Cut | Result |
|---|---|
| F49A | PASS |
| F49B | PASS |
| F49B-QA | PASS |
| F49C | PASS |
| F49C-QA | PASS |
| F49D | PASS |
| F49D-QA | PASS |
| F49E | PASS |
| F49E-QA | PASS |
| F49F | PASS |
| F49F-QA | PASS |
| F49G | PASS |
| F49G-QA | PASS |
| F49H | PASS |
| F49H-QA | PASS |
| F49I | PASS |
| F49I-QA | PASS |
| F49J | PASS |
| F49J-QA | PASS |
| F49K | PASS |
| F49K-QA | PASS |
| F49L | PASS |
| F49L-QA | PASS |
| F49M | Documentation-only consolidation |

## Current player passive boundary

The F49 player foundation does not execute binding:

```text
viewBinding = false
controlBinding = false
cameraActivation = false
inputActivation = false
movement = false
actorSpawning = false
```

Future behavior that changes this boundary requires a new explicit implementation cut.

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

For Reset Reform, both layers passed. For F49 passive player binding foundation, package + QA have passed and FIRSTGAME remains intentionally out of scope until a real binding implementation exists.

## Consumer project ownership

Current ownership rule:

```text
Framework package: contracts, runtime, editor tooling, validators, diagnostics and official docs.
QA Project: synthetic smokes, probes, artificial scenarios and negative cases.
FIRSTGAME: minimal real game usage proof.
```

Do not move canonical framework docs into consumer `Assets/` folders. Consumer projects may keep local READMEs only for project-specific operation.
