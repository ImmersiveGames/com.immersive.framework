# F52B — Unity PlayerInput Bridge Contract Note

Status: Proposed / Experimental implementation cut.

## Objective

Introduce an explicit bridge between already-applied `PlayerControlBindingSnapshot` evidence and a configured Unity `PlayerInput`.

```text
PlayerControlBindingSnapshot
-> UnityPlayerInputBridgeAdapter
-> IUnityPlayerInputBridgeTarget
-> UnityPlayerInputBridgeSnapshot
```

## Scope

F52B creates bridge evidence only:

```text
controlBinding = true
unityPlayerInputBridge = true
inputActivation = false
movement = false
actorSpawning = false
```

## Out of scope

F52B does not:

```text
enable PlayerInput
switch action maps
route InputActions
enable movement
execute gameplay commands
spawn actors
own runtime lifecycle
integrate FIRSTGAME
```

## Boundary

`UnityPlayerInputBridgeTargetBehaviour` requires an explicit `PlayerInput` reference and an expected `PlayerSlotId`.
It does not search globally, use a singleton, use a service locator or infer ownership from `PlayerInput.playerIndex`.

## QA

The QA smoke validates success, missing control binding target, missing control binding, missing bridge target, missing `PlayerInput`, slot mismatch, clear no-op, clear after bridge and passive boundary.
