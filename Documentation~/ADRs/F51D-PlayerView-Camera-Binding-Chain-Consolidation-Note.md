# F51D — PlayerView Camera Binding Chain Consolidation

Status: **Accepted / Documentation-only**

## Objective

Consolidate the PlayerView camera binding chain after F51A-F51C and freeze its current semantics before moving to PlayerControl binding or more advanced camera systems.

## Scope

This cut documents:

```text
PlayerView binding
Camera target binding
Camera activation
failure semantics
clear/no-op semantics
explicit boundary exclusions
next technical lane
```

## Out of scope

This cut does not add runtime code, editor code, QA scenes or new behavior.

It does not introduce:

```text
Cinemachine
CameraDirector
camera priority
multi-camera arbitration
automatic route/activity lifecycle
control binding
input activation
movement
actor spawning
FIRSTGAME integration
```

## Decision

The current PlayerView camera chain is accepted as a minimal explicit adapter stack:

```text
F51A — PlayerViewBindingAdapter
F51B — PlayerViewCameraTargetBindingAdapter
F51C — PlayerViewCameraActivationAdapter
```

Each adapter owns one step and one clear operation. Each step must return an explicit result object and must not hide missing required state through global lookup or implicit fallback.

## Accepted flags

A successful F51C activation may report:

```text
viewBinding='True'
cameraTargetBinding='True'
cameraActivation='True'
```

The following must remain outside this lane:

```text
controlBinding='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Architectural gain

The framework now has a minimal camera chain that is:

```text
explicit
incremental
testable
clearable
free of global managers
free of Camera.main lookup
free of Cinemachine assumptions
```

This allows the next technical lane to focus on PlayerControl binding without mixing camera activation, input activation and movement in one cut.

## Acceptance evidence

F51A, F51B and F51C are expected to remain the technical proof for this consolidation. F51D itself is documentation-only.

## Next lane

Recommended next cut:

```text
F52A — PlayerControl Binding Adapter Contract
```
