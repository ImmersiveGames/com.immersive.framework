# 07 — PlayerControl Unity PlayerInput Chain

Status: **current after F52C**.

This document consolidates the PlayerControl Unity PlayerInput chain created after the PlayerView camera binding chain.

## Scope

The current PlayerControl input chain is explicit, incremental and adapter-based:

```text
PlayerBindingAuthoringValidationReport
  -> PlayerBindingReadinessSummary
  -> PlayerControlBindingAdapter
  -> UnityPlayerInputBridgeAdapter
  -> UnityPlayerInputActivationAdapter
```

The chain is intentionally small. It does not create movement, gameplay command execution, input action routing, a runtime lifecycle, actor spawning or FIRSTGAME-specific setup.

## Implemented chain

| Cut | Implemented boundary | Result |
|---|---|---|
| F52A | `PlayerControlBindingAdapter` | Stores validated PlayerControl binding evidence on an explicit target. |
| F52B | `UnityPlayerInputBridgeAdapter` | Converts PlayerControl binding evidence into explicit Unity `PlayerInput` bridge evidence. |
| F52C | `UnityPlayerInputActivationAdapter` | Switches an explicit Unity `PlayerInput` to a configured action map and restores the previous map on clear. |

## Runtime shape

```text
PlayerControlBindingAdapter
  -> IPlayerControlBindingTarget
  -> PlayerControlBindingSnapshot

UnityPlayerInputBridgeAdapter
  -> IUnityPlayerInputBridgeTarget
  -> UnityPlayerInputBridgeSnapshot
  -> explicit UnityEngine.InputSystem.PlayerInput

UnityPlayerInputActivationAdapter
  -> IUnityPlayerInputActivationTarget
  -> UnityPlayerInputActivationSnapshot
  -> explicit PlayerInput action-map switch/restore
```

## Required upstream evidence

A successful input activation depends on prior authoring and binding evidence:

```text
PlayerSlotDeclaration
PlayerSlotOccupancy
ActorReadinessBehaviour
PlayerEntryBehaviour
PlayerViewBehaviour
PlayerControlBehaviour
PlayerBindingReadinessSummary
PlayerControlBindingSnapshot
UnityPlayerInputBridgeSnapshot
explicit Unity PlayerInput
configured action map name
configured action map present in PlayerInput.actions
```

The authoring validator remains the recommended gate before attempting runtime binding.

## Successful state

After F52C, a successful Unity PlayerInput activation may report:

```text
controlBinding='True'
unityPlayerInputBridge='True'
inputActivation='True'
```

The following remain outside this chain and must stay false:

```text
viewBinding='False'
cameraActivation='False'
movement='False'
actorSpawning='False'
```

## Clear semantics

Each adapter owns its own explicit clear operation:

| Adapter | Clear result |
|---|---|
| `PlayerControlBindingAdapter.Clear(...)` | Removes PlayerControl binding evidence from the binding target. |
| `UnityPlayerInputBridgeAdapter.Clear(...)` | Removes Unity PlayerInput bridge evidence from the bridge target. |
| `UnityPlayerInputActivationAdapter.Clear(...)` | Clears input activation and restores the previous explicit `PlayerInput.currentActionMap` when one was captured. |

Clear without an existing binding, bridge or activation is an explicit `NoOp`, not a silent success.

## Failure semantics

F52A-F52C use explicit result objects and failure kinds. Missing readiness, missing PlayerControl binding, missing bridge target, missing PlayerInput, missing action map name, missing configured action map and PlayerSlot mismatches must produce diagnostic failures.

No adapter should infer missing required references from global state, scene searches, hidden service locators or implicit lifecycle managers.

## Not implemented here

The following are intentionally not part of the current chain:

```text
InputAction routing
InputAction value reading
movement enable/disable
CharacterController integration
Rigidbody integration
gameplay command execution
actor spawning
runtime lifecycle/coordinator
route/activity automatic binding
camera activation
FIRSTGAME integration
```

## Next lane

The recommended next lane is a FIRSTGAME usability proof for the accepted PlayerView and PlayerControl input chains, only after QA remains clean.

```text
F53 — FIRSTGAME PlayerView / PlayerControl usability proof
```

That lane must remain an integration/usability proof. It must not introduce new framework contracts before QA proves them first.
