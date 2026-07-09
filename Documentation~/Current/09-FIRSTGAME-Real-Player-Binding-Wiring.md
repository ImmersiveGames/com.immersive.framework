# F53B — FIRSTGAME Real Player Binding Wiring Proof

## Objective

F53B proves that FIRSTGAME can consume the accepted PlayerControl / Unity PlayerInput chain on the real player object, without keeping temporary QA probes in the consumer project.

Canonical target for this cut:

```text
PlayerPrototype
  PlayerInput
  PlayerControlBindingTargetBehaviour
  UnityPlayerInputBridgeTargetBehaviour
  UnityPlayerInputActivationTargetBehaviour
```

## Scope

F53B is a consumer usability proof. It validates or applies authoring-only wiring in FIRSTGAME using the public framework components from F52A, F52B and F52C.

Allowed:

```text
validate the real PlayerPrototype
reuse the real PlayerInput
ensure framework binding target components on the real player object
configure expected PlayerSlotId = player.1
configure expected action map = Player
remove F53A preflight proof assets from FIRSTGAME when no longer needed
```

## Out of scope

F53B does not add new framework contracts and does not implement gameplay.

```text
no InputAction routing
no InputAction value reading
no movement creation
no CharacterController or Rigidbody integration
no gameplay command execution
no actor spawning
no runtime lifecycle/coordinator
no new test GameObject in FIRSTGAME
```

## Acceptance

The FIRSTGAME validation should produce:

```text
[F53B_FIRSTGAME_REAL_PLAYER_BINDING] status='Succeeded'
```

Required fields:

```text
playerObject='PlayerPrototype'
playerInput='True'
inputActions='True'
expectedGameplayActionMapFound='True'
playerControlBindingTarget='True'
unityPlayerInputBridgeTarget='True'
unityPlayerInputActivationTarget='True'
createdTestObject='False'
createdPlayerInput='False'
movement='False'
actorSpawning='False'
gameplayCommandExecution='False'
failureReason='None'
```

## Design note

The proof deliberately avoids another runtime probe component. FIRSTGAME should keep only canonical player wiring and editor-only validation helpers. The temporary F53A proof assets can be removed after F53B is accepted.
