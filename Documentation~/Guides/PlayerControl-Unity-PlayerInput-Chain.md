# PlayerControl Unity PlayerInput Chain Guide

Status: **guide for the F52A-F52C chain**.

Use this guide when you need to understand what the current PlayerControl input binding stack does and what it deliberately does not do.

## Quick reading

The current chain has three steps:

```text
1. Bind PlayerControl evidence.
2. Bridge that binding to an explicit Unity PlayerInput.
3. Switch one explicit PlayerInput to a configured action map.
```

It is not a movement system. It is not a gameplay input router. It is a small, explicit adapter chain that can be validated and cleared.

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

The report should be ready for control binding before the PlayerControl binding adapter is used.

## Step 1 — PlayerControl binding

Runtime boundary:

```text
PlayerControlBindingAdapter
  -> IPlayerControlBindingTarget
  -> PlayerControlBindingSnapshot
```

This stores evidence that an active, validated `PlayerControl` is selected for a `PlayerSlot`.

Expected success flags:

```text
viewBinding='False'
controlBinding='True'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Step 2 — Unity PlayerInput bridge

Runtime boundary:

```text
UnityPlayerInputBridgeAdapter
  -> IUnityPlayerInputBridgeTarget
  -> UnityPlayerInputBridgeSnapshot
  -> explicit UnityEngine.InputSystem.PlayerInput
```

This stores evidence that the selected PlayerControl is associated with one explicit Unity `PlayerInput`. It does not enable the `PlayerInput`, switch action maps or read input values.

Expected success flags:

```text
controlBinding='True'
unityPlayerInputBridge='True'
inputActivation='False'
movement='False'
actorSpawning='False'
cameraActivation='False'
```

## Step 3 — Unity PlayerInput activation

Runtime boundary:

```text
UnityPlayerInputActivationAdapter
  -> IUnityPlayerInputActivationTarget
  -> UnityPlayerInputActivationSnapshot
  -> explicit PlayerInput.SwitchCurrentActionMap(actionMapName)
```

This switches one explicitly configured Unity `PlayerInput` to a configured action map and stores the previous action map for clear/restore.

Expected success flags:

```text
controlBinding='True'
unityPlayerInputBridge='True'
inputActivation='True'
movement='False'
actorSpawning='False'
cameraActivation='False'
```

## Clearing

Clear operations are explicit and local to each adapter:

```text
PlayerControlBindingAdapter.Clear(...)
UnityPlayerInputBridgeAdapter.Clear(...)
UnityPlayerInputActivationAdapter.Clear(...)
```

Clear without an existing binding, bridge or activation should return `NoOp` with a diagnostic reason.

## What not to expect

Do not expect the current chain to handle:

```text
InputAction routing
reading Vector2 movement
movement controller enable/disable
CharacterController
Rigidbody
gameplay commands
actor spawning
automatic route/activity binding
FIRSTGAME setup
```

Those require later explicit cuts and must not be smuggled into the PlayerControl input boundary.
