# F51B — PlayerView Camera Target Binding Adapter

Status: Accepted / package-first / QA-first.

## Objective

Introduce an explicit Unity adapter that associates the F51A `PlayerViewBindingSnapshot` with a concrete Unity `Transform` camera target/anchor.

This is the next step after logical `PlayerView` binding, but it is still not camera activation.

## Scope

F51B adds:

```text
PlayerViewBindingSnapshot
-> PlayerViewCameraTargetBindingAdapter
-> IPlayerViewCameraTargetBindingTarget
-> PlayerViewCameraTargetBindingSnapshot
```

The adapter validates that:

```text
PlayerView binding evidence exists
PlayerViewBehaviour evidence exists
PlayerSlotId matches
PlayerViewBehaviour has a Unity Transform view target
binding view target name matches the behaviour view target
camera-target binding target exists
```

## Out of scope

F51B does not:

```text
activate cameras
change Camera.main
change camera priority
drive Cinemachine
create CameraDirector
bind input/control
enable movement
spawn actors
create runtime lifecycle/coordinator
integrate FIRSTGAME
```

## Failure policy

Invalid or incomplete authoring fails explicitly with `PlayerViewCameraTargetBindingFailureKind`.
A clear request with no existing binding returns `NoOp` with `MissingExistingBinding`.

## Boundary

A successful F51B result may report:

```text
viewBinding='True'
cameraTargetBinding='True'
```

It must still report:

```text
controlBinding='False'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Validation

Technical validation is owned by QAFramework through `F51B_PLAYER_VIEW_CAMERA_TARGET_BINDING_QA`.
FIRSTGAME remains out of scope until QA is clean.
