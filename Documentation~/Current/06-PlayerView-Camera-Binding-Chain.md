# 06 — PlayerView Camera Binding Chain

Status: **current after F51C**.

This document consolidates the minimum PlayerView camera chain created after the passive Player binding foundation and authoring validation sequence.

## Scope

The current PlayerView camera chain is explicit, incremental and adapter-based:

```text
PlayerBindingAuthoringValidationReport
  -> PlayerBindingReadinessSummary
  -> PlayerViewBindingAdapter
  -> PlayerViewCameraTargetBindingAdapter
  -> PlayerViewCameraActivationAdapter
```

The chain is intentionally small. It does not create a runtime lifecycle, a camera director, priority arbitration, Cinemachine integration or FIRSTGAME-specific setup.

## Implemented chain

| Cut | Implemented boundary | Result |
|---|---|---|
| F51A | `PlayerViewBindingAdapter` | Stores validated PlayerView binding evidence on an explicit target. |
| F51B | `PlayerViewCameraTargetBindingAdapter` | Converts PlayerView binding evidence into Unity camera target evidence. |
| F51C | `PlayerViewCameraActivationAdapter` | Enables/disables an explicit Unity `Camera` from validated camera-target evidence. |

## Runtime shape

```text
PlayerViewBindingAdapter
  -> IPlayerViewBindingTarget
  -> PlayerViewBindingSnapshot

PlayerViewCameraTargetBindingAdapter
  -> IPlayerViewCameraTargetBindingTarget
  -> PlayerViewCameraTargetBindingSnapshot
  -> Transform viewTarget

PlayerViewCameraActivationAdapter
  -> IPlayerViewCameraActivationTarget
  -> PlayerViewCameraActivationSnapshot
  -> explicit Unity Camera.enabled
```

## Required upstream evidence

A successful activation depends on prior authoring and binding evidence:

```text
PlayerSlotDeclaration
PlayerSlotOccupancy
ActorReadinessBehaviour
PlayerEntryBehaviour
PlayerViewBehaviour
PlayerControlBehaviour
PlayerBindingReadinessSummary
PlayerViewBindingSnapshot
PlayerViewCameraTargetBindingSnapshot
explicit Unity Camera
```

The authoring validator remains the recommended gate before attempting runtime binding.

## Successful state

After F51C, a successful camera activation may report:

```text
viewBinding='True'
cameraTargetBinding='True'
cameraActivation='True'
```

The following remain outside this chain and must stay false:

```text
controlBinding='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Clear semantics

Each adapter owns its own explicit clear operation:

| Adapter | Clear result |
|---|---|
| `PlayerViewBindingAdapter.Clear(...)` | Removes PlayerView binding evidence from the binding target. |
| `PlayerViewCameraTargetBindingAdapter.Clear(...)` | Removes camera target evidence from the camera target binding target. |
| `PlayerViewCameraActivationAdapter.Clear(...)` | Clears camera activation and restores the explicit camera's previous enabled state. |

Clear without an existing binding or activation is an explicit `NoOp`, not a silent success.

## Failure semantics

F51A-F51C use explicit result objects and failure kinds. Missing readiness, missing target, inactive PlayerView, missing camera-target binding, missing activation target and missing explicit camera must produce diagnostic failures.

No adapter should infer missing required references from global state, `Camera.main`, scene searches or hidden service locators.

## Not implemented here

The following are intentionally not part of the current chain:

```text
Cinemachine
CameraDirector
Camera.main lookup
camera priority or arbitration
multi-camera ownership
runtime lifecycle/coordinator
route/activity automatic binding
control binding
input activation
movement enable/disable
actor spawning
FIRSTGAME integration
```

## Next technical lane

The recommended next technical lane is PlayerControl binding:

```text
F52A — PlayerControl Binding Adapter Contract
```

Reason: PlayerView now has a minimal end-to-end chain from authoring evidence to explicit Unity camera activation. The equivalent incremental chain is still missing for control/input.
