# P3G.3 — Local Player Provisioning Bridge and Synthetic QA Manifest

## Type

Runtime integration and technical QA.

## Objective

Connect the Session Player participation context to one explicit manual local Player provisioner while preserving Unity `PlayerInputManager` as the sole physical creator of local Player hosts.

## Implemented flow

```text
LocalPlayerJoinRequest
-> validate backend and technical ceiling
-> reserve first Available configured Slot
-> create PendingLocalPlayerJoin
-> call PlayerInputManager.JoinPlayer
-> correlate direct return and joined callback
-> validate PlayerInput
-> validate PlayerActorDeclaration and prefab evidence
-> mark reserved Slot Joined
```

Every failure after reservation calls `TryReleaseReservation`. `LocalPlayerJoinResult` preserves the typed original status, commit evidence when present and the rollback result; `FailedRollback` cannot hide the original failure.

## Callback policy

```text
callback before JoinPlayer return
  correlated immediately by reference

no callback at return boundary
  admission may complete from the direct non-null result
  confirmation remains Pending
  a later callback updates bridge confirmation evidence

callback for another PlayerInput
  rejected as correlation divergence

callback without an authorized operation
  rejected and the Unity host is not admitted
```

No arbitrary frame wait, coroutine or sibling `OnEnable` ordering is used.

## Product evidence

A successful result directly associates:

```text
PlayerSlotRuntimeSnapshot
PlayerInput
PlayerActorDeclaration
Unity playerIndex diagnostics
```

This is Session join evidence. Actor selection, Actor-specific composition, occupancy, camera and gameplay readiness remain outside P3G.3.

## Files

```text
Runtime/PlayerParticipation/Contracts/ILocalPlayerProvisioningBackend.cs
Runtime/PlayerParticipation/Contracts/LocalPlayerJoinResult.cs
Runtime/PlayerParticipation/Runtime/UnityLocalPlayerProvisioningBackend.cs
Runtime/PlayerParticipation/Runtime/PendingLocalPlayerJoin.cs
Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningBridge.cs
Editor/PlayerParticipation/LocalPlayerProvisioningValidator.cs
```

## Out of scope

```text
FrameworkRuntimeHost composition of the bridge
automatic product join requests
ActorProfile selection
Actor composition/materialization
PlayerSlot occupancy
camera/input gameplay activation
leave/disconnect/reconnect
FIRSTGAME integration
```

## Next cut

P3G.4 composes one bridge per Session, injects the authored manager, provides an explicit runtime request entry point and proves real Play Mode joins in QA.
