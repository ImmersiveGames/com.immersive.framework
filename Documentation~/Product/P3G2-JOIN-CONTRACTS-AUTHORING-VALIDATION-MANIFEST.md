
# P3G.2 — Local Player Join Contracts and Authoring Validation

## Objective

Introduce the passive typed contracts and designer-facing declaration required before the framework calls `PlayerInputManager.JoinPlayer`.

## Type

Runtime contracts + authoring component + Editor validation.

## Product surface

```text
Add Component
  Immersive Framework
    Player
      Local Player Provisioning Authoring
```

The component references the one Session-authorized `PlayerInputManager`. It performs no gameplay side effect.

## Created

```text
Runtime/PlayerParticipation/Contracts/LocalPlayerJoinOperationId.cs
Runtime/PlayerParticipation/Contracts/LocalPlayerJoinRequest.cs
Runtime/PlayerParticipation/Contracts/LocalPlayerJoinStatus.cs
Runtime/PlayerParticipation/Contracts/LocalPlayerJoinCallbackConfirmation.cs
Runtime/PlayerParticipation/Contracts/LocalPlayerJoinResult.cs
Runtime/PlayerParticipation/Authoring/LocalPlayerProvisioningAuthoring.cs
Editor/PlayerParticipation/LocalPlayerProvisioningValidator.cs
Editor/PlayerParticipation/LocalPlayerProvisioningAuthoringEditor.cs
```

## Validation

Required:

```text
explicit PlayerInputManager reference
Join Players Manually behavior
Player Prefab assigned
Player Prefab contains PlayerInput
Game Application contains Local Player Slots
configured Slot count does not exceed a positive maxPlayerCount
```

Validation is Editor-only and non-mutating. No manager, prefab or Slot configuration is repaired automatically.

## Contract boundary

The ordinary request contains only:

```text
Source
Reason
optional InputDevice hint
optional control scheme hint
```

The caller does not select Slot, playerIndex, split-screen index, Session identity or ActorProfile.

`LocalPlayerJoinResult.PlayerActorDeclaration` carries the existing specialized declaration as Unity `Component` evidence. P3G does not redeclare Actor identity or introduce a second declaration type.

## Out of scope

```text
PlayerInputManager.JoinPlayer invocation
pending operation runtime
callback correlation
Slot reservation/commit/rollback orchestration
runtime host injection
Player leave
Actor selection
Activity admission
FIRSTGAME integration
```

## Expected QA

```text
[P3G2_JOIN_CONTRACT_AUTHORING_SMOKE] status='Passed'
```

## Suggested commit

```text
P3G.2 — add local Player join contracts and provisioning authoring
```
