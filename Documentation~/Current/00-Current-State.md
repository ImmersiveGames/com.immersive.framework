# 00 — Current State

Status: **canonical current state after C8B6 Camera Product Documentation consolidation**.

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
| FIRSTGAME | Reset Room, Activity Restart and CameraComposer usage proofs passed. |
| Player identity | `PlayerSlot`, `ActorId`, Actor readiness and `PlayerEntry` passive evidence are available. |
| Player topology | `PlayerTopologyValidator` validates slots, occupancies and entries. |
| Player view | `PlayerView` and `PlayerViewTopologyValidator` validate passive view evidence. |
| Player control | `PlayerControl` and `PlayerControlTopologyValidator` validate passive control evidence. |
| Player binding readiness | `PlayerBindingReadinessSummarizer` aggregates topology readiness for future binding. |
| Player binding diagnostics | `PlayerBindingDiagnosticReporter` produces human-readable passive diagnostics. |
| Camera product | `CameraComposer` + optional `CameraRecipe` is the designer-first single-player camera authoring surface. |
| Camera targets | `CameraComposer` resolves explicit `PlayerComposer` targets or explicit transforms; no scene/name lookup. |
| Route/Activity camera | Bindings consume only explicit `FrameworkCinemachineCameraOutputSource` outputs. No legacy director/fallback exists. |
| Consumer project split | QA, FIRSTGAME and package roles are frozen in [`03-Consumer-Project-Roles.md`](03-Consumer-Project-Roles.md). |

## Current camera model

```text
Gameplay camera:
PlayerComposer -> CameraComposer -> Validate -> Apply/Rebuild

Lifecycle-specific output:
Route/Activity binding -> explicit Cinemachine output source -> output applier
```

Current boundary:

```text
single-player CameraComposer MVP
explicit references only
idempotent editor materialization
no Camera.main
no global CameraManager
no legacy FrameworkCameraDirector
no automatic output release on lifecycle exit yet
```

See [`../Guides/Camera-Product-Usage.md`](../Guides/Camera-Product-Usage.md).

## Current player passive boundary

The passive player foundation does not execute binding by itself:

```text
viewBinding = false
controlBinding = false
cameraActivation = false
inputActivation = false
movement = false
actorSpawning = false
```

`CameraComposer` is a separate explicit product surface. `PlayerViewBehaviour` remains passive evidence and does not become camera authority.

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

For the camera product, package + QA prove the technical contracts, while FIRSTGAME proves `PlayerComposer -> CameraComposer` as the current real-game usage path.

## Consumer project ownership

Current ownership rule:

```text
Framework package: contracts, runtime, editor tooling, validators, diagnostics and official docs.
QA Project: synthetic smokes, probes, artificial scenarios and negative cases.
FIRSTGAME: minimal real game usage proof.
```

Do not move canonical framework docs into consumer `Assets/` folders. Consumer projects may keep local READMEs only for project-specific operation.
