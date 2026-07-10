# 02 — Usage Map

Use this map to choose the public product surface before its technical materialization.

For the current implementation block and next cut, read [Execution Status](05-Execution-Status.md).

## Designer-first creation

| Task | Main flow | Advanced / Debug |
|---|---|---|
| Create Player | Optional `PlayerRecipe` -> `PlayerComposer` -> configure Control -> Validate -> Apply/Rebuild | Inspect generated declarations, canonical Gate adapter and diagnostics. |
| Create main gameplay camera | Optional `CameraRecipe` -> `CameraComposer` -> explicit `PlayerComposer` or transforms -> Validate -> Apply/Rebuild | Inspect Unity Camera, Cinemachine Camera, Brain and resolved targets. |
| Build the current minimal loop | Compose the existing FIRSTGAME Route, Activity, Player, Camera, Pause, Reset and Activity Restart surfaces. | Use G1 diagnostics only to prove the integrated loop. |
| Declare passive evidence | Use generated or explicit `PlayerSlotDeclaration`, `PlayerActorDeclaration`, `PlayerEntryBehaviour`, `PlayerViewBehaviour`, `PlayerControlBehaviour` only when technical evidence is needed. | These contracts do not execute gameplay. |
| Block PlayerInput by Gate | `UnityPlayerInputGateAdapter` with explicit `PlayerInput` and validated action-map name. | Gate is availability, not movement or Player lifecycle authority. |
| Route/Activity camera output | Matching binding + explicit `FrameworkCinemachineCameraOutputSource`. | Apply-on-enter only; release/restore belongs to future C9. |

## Player boundary

`PlayerComposer` is the primary Player authoring surface. Its Control section owns explicit `PlayerInput`, gameplay action map, control target, requiredness and Gate participation.

`PlayerRecipe` stores reusable intent. It never stores concrete scene references.

Required control blocks validation when `PlayerInput`, its InputActionAsset, the configured action map or control target is missing.

The accepted runtime boundary is:

```text
Framework
  Player authoring
  PlayerSlot identity evidence
  PlayerInput reference
  Gate/Pause/Transition availability
  diagnostics

Consumer game
  action semantics
  action-value reading
  movement execution
  gameplay tuning
```

Do not add a runtime binding facade, Player manager or generic movement controller as a default path.

## Camera boundary

`CameraComposer` is the primary gameplay-camera creation surface. Route/Activity bindings are technical lifecycle integration for a specific explicit output, not camera creation.

Activity policy:

- `UseOwn`: apply the Activity output.
- `UseRoute`: keep the Route output.
- invalid required output: blocked.
- invalid optional output: explicitly skipped.

Do not use `Camera.main`, object-name lookup, `SetActive`, `Camera.enabled` or a global camera manager as authority.

## Current G1 task map

| Need | Existing surface |
|---|---|
| Enter gameplay | `GameApplicationAsset` + Route request. |
| Start gameplay state | Startup Activity. |
| Control Player | `PlayerComposer` + explicit `PlayerInput` + game-owned mover. |
| Follow Player | `CameraComposer`. |
| Pause and resume | Pause request/input surface. |
| Reset one object | `ObjectResetTrigger` + `ResetSubjectReference`. |
| Reset a scope | `ObjectResetGroupTrigger` + `ResetSelectionConfig`. |
| Restart Activity | `ActivityRestartTrigger`. |
| Prove return to initial state | G1 FIRSTGAME integration evidence; do not invent a new framework service. |

## Other common tasks

| Task | Use |
|---|---|
| Boot | `GameApplicationAsset` + Startup Route. |
| Switch Route | Framework Route request surface. |
| Pause | Pause request/input surface; do not mutate TimeScale from unrelated gameplay scripts. |
| Reset one object | `ObjectResetTrigger` with `ResetSubjectReference`. |
| Reset a scope | `ObjectResetGroupTrigger` + `ResetSelectionConfig`. |
| Restart Activity | `ActivityRestartTrigger`. |

See [Consumer Project Roles](03-Consumer-Project-Roles.md), [Roadmap](01-Roadmap.md), [Execution Status](05-Execution-Status.md) and [Camera Product Usage](../Guides/Camera-Product-Usage.md).
