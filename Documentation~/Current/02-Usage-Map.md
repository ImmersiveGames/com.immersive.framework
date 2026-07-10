# 02 — Usage Map

Use this map to choose the public product surface before its technical materialization.

## Designer-first creation

| Task | Main flow | Advanced / Debug |
|---|---|---|
| Create Player | Optional `PlayerRecipe` -> `PlayerComposer` -> configure Control -> Validate -> Apply/Rebuild | Inspect generated declarations, canonical bindings, Gate adapter and diagnostics. |
| Create main gameplay camera | Optional `CameraRecipe` -> `CameraComposer` -> explicit `PlayerComposer` or transforms -> Validate -> Apply/Rebuild | Inspect Unity Camera, Cinemachine Camera, Brain and resolved targets. |
| Declare passive evidence | Use generated or explicit `PlayerSlotDeclaration`, `PlayerActorDeclaration`, `PlayerEntryBehaviour`, `PlayerViewBehaviour`, `PlayerControlBehaviour` only when technical evidence is needed. | These contracts do not execute gameplay. |
| Block PlayerInput by Gate | `UnityPlayerInputGateAdapter` with an explicit `PlayerInput` and validated action-map name. | Gate is not Player lifecycle or control authority. |
| Route/Activity camera output | Matching binding + explicit `FrameworkCinemachineCameraOutputSource`. | Apply-on-enter only; no automatic release/restore on exit. |

## Player boundary

`PlayerComposer` is the primary Player authoring surface. Its Control section owns `Control Enabled`, explicit `PlayerInput`, gameplay action map, explicit control target, `BindOnEnable`, requiredness and Gate participation. Apply/Rebuild materializes authoring evidence only; it does not bind at runtime.

`PlayerRecipe` stores only reusable control intent. It never stores `PlayerInput`, scene transforms or PlayerSlot/Actor declarations. Applying Recipe defaults preserves concrete Composer references.

Required control blocks validation when `PlayerInput`, its InputActionAsset, the configured action map or the control target is missing. Duplicate PlayerSlot/Actor owners or F52 targets outside `Player/_Framework/_Bindings` block Validate and Apply without automatic deletion.

`PlayerInput` remains the typed Unity reference and movement scripts remain game-owned. Runtime bind/unbind and scoped authority remain deferred to P2C-P2E.

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
