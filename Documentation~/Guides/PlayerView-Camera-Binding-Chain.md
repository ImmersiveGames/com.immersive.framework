# PlayerView Camera Binding Chain Guide

Status: **guide for the F51A-F51C chain**.

Use this guide when you need to understand what the current PlayerView camera binding stack does and what it deliberately does not do.

## Quick reading

The current chain has three steps:

```text
1. Bind PlayerView evidence.
2. Bind Unity camera target evidence.
3. Activate one explicit Unity Camera.
```

It is not a camera system yet. It is a small, explicit adapter chain that can be validated and cleared.

## Step 0 — Validate authoring first

Before attempting binding, run the Player Binding authoring validation tooling:

```text
Immersive Framework > Player Binding > Authoring Validation
```

Expected minimum authoring chain:

```text
PlayerSlotDeclaration
PlayerSlotOccupancy
ActorReadinessBehaviour
PlayerEntryBehaviour
PlayerViewBehaviour
PlayerControlBehaviour
```

The report should be ready for view binding before the PlayerView binding adapter is used.

## Step 1 — PlayerView binding

Runtime boundary:

```text
PlayerViewBindingAdapter
  -> IPlayerViewBindingTarget
  -> PlayerViewBindingSnapshot
```

This stores evidence that an active, validated `PlayerView` is selected for a `PlayerSlot`.

Expected success flags:

```text
viewBinding='True'
controlBinding='False'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Step 2 — Camera target binding

Runtime boundary:

```text
PlayerViewCameraTargetBindingAdapter
  -> IPlayerViewCameraTargetBindingTarget
  -> PlayerViewCameraTargetBindingSnapshot
  -> Transform viewTarget
```

This converts PlayerView binding evidence into Unity target evidence. The target is a `Transform`, not an active camera pipeline.

Expected success flags:

```text
viewBinding='True'
cameraTargetBinding='True'
controlBinding='False'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Step 3 — Camera activation

Runtime boundary:

```text
PlayerViewCameraActivationAdapter
  -> IPlayerViewCameraActivationTarget
  -> PlayerViewCameraActivationSnapshot
  -> explicit Unity Camera.enabled
```

This activates one explicitly configured Unity `Camera`. It does not look up `Camera.main`, does not choose between multiple cameras and does not apply priority.

Expected success flags:

```text
viewBinding='True'
cameraTargetBinding='True'
cameraActivation='True'
controlBinding='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Clearing

Clear operations are explicit and local to each adapter:

```text
PlayerViewBindingAdapter.Clear(...)
PlayerViewCameraTargetBindingAdapter.Clear(...)
PlayerViewCameraActivationAdapter.Clear(...)
```

Clear without an existing binding or activation should return `NoOp` with a diagnostic reason.

## What not to expect

Do not expect the current chain to handle:

```text
Cinemachine
CameraDirector
camera priority
camera blending
Camera.main
automatic route/activity binding
input routing
movement enable/disable
FIRSTGAME setup
```

Those require later cuts.
