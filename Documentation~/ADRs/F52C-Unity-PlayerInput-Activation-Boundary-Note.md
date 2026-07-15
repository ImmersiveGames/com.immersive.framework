# F52C — Unity PlayerInput Activation / Action Map Boundary

Status: **Implemented as explicit QA-first contract cut**.

## Decision

F52C may activate input only by switching one explicitly configured `UnityEngine.InputSystem.PlayerInput` action map from existing F52B bridge evidence.

```text
UnityPlayerInputBridgeSnapshot
-> UnityPlayerInputActivationAdapter
-> IUnityPlayerInputActivationTarget
-> UnityPlayerInputActivationSnapshot
```

## Boundary

F52C may set diagnostic evidence equivalent to:

```text
controlBinding = true
unityPlayerInputBridge = true
inputActivation = true
```

It must keep these false:

```text
movement = false
actorSpawning = false
gameplayCommandExecution = false
cameraActivation = false
```

## Explicit non-goals

F52C does not route `InputAction` callbacks, read action values, enable movement, bind a movement controller, execute gameplay commands, spawn actors, own runtime lifecycle, or integrate FIRSTGAME.

## Clear semantics

Clear restores the previous action map when the activation snapshot captured one, then clears only F52C activation evidence. F52B bridge evidence may remain available until the bridge is cleared explicitly.
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
