# 02 — Usage Map

Use this map to choose the public product surface before its technical materialization.

## Designer-first creation

| Task | Main flow | Advanced / Debug |
|---|---|---|
| Create Player | Optional `PlayerRecipe` -> `PlayerComposer` -> Validate -> Apply/Rebuild | Inspect generated declarations, bindings, anchors and diagnostics. |
| Create main gameplay camera | Optional `CameraRecipe` -> `CameraComposer` -> explicit `PlayerComposer` or transforms -> Validate -> Apply/Rebuild | Inspect Unity Camera, Cinemachine Camera, Brain and resolved targets. |
| Declare passive evidence | Use generated or explicit `PlayerSlotDeclaration`, `PlayerActorDeclaration`, `PlayerEntryBehaviour`, `PlayerViewBehaviour`, `PlayerControlBehaviour` only when technical evidence is needed. | These contracts do not execute gameplay. |
| Block PlayerInput by Gate | `UnityPlayerInputGateAdapter` with an explicit `PlayerInput` and validated action-map name. | Gate is not Player lifecycle or control authority. |
| Route/Activity camera output | Matching binding + explicit `FrameworkCinemachineCameraOutputSource`. | Apply-on-enter only; no automatic release/restore on exit. |

## Player boundary

`PlayerComposer` is the primary Player authoring surface. It is not a runtime manager and does not spawn, move, activate input or execute control binding.

`PlayerInput` and one-off movement scripts remain game-owned until the official PlayerControl runtime exists in P2.

## Camera boundary

`CameraComposer` is the primary gameplay-camera creation surface. Route/Activity bindings are technical lifecycle integration for a specific explicit output, not camera creation.

Activity policy:

- `UseOwn`: apply the Activity output.
- `UseRoute`: keep the Route output.
- invalid required output: blocked.
- invalid optional output: explicitly skipped.

Do not use `Camera.main`, object-name lookup, `SetActive`, `Camera.enabled` or a global camera manager as authority.

## Other common tasks

| Task | Use |
|---|---|
| Boot | `GameApplicationAsset` + Startup Route. |
| Switch Route | Framework Route request surface. |
| Pause | Pause request/input surface; do not mutate TimeScale from unrelated gameplay scripts. |
| Reset one object | `ObjectResetTrigger` with `ResetSubjectReference`. |
| Reset a scope | `ObjectResetGroupTrigger` + `ResetSelectionConfig`. |
| Restart Activity | `ActivityRestartTrigger`. |

See [Consumer Project Roles](03-Consumer-Project-Roles.md) and [Camera Product Usage](../Guides/Camera-Product-Usage.md).
