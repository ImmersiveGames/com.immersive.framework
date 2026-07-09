# 08 — FIRSTGAME Player Binding Usability Proof

Status: **F53A preflight with F53A-B1 strictness patch**.

This document defines the first FIRSTGAME usability proof after the validated PlayerView and PlayerControl input chains.

## Purpose

F53 validates that the framework contracts accepted in QA can be consumed by a real project without creating new framework contracts in the consumer.

The proof starts with a small FIRSTGAME-side preflight probe. It is intentionally not a new runtime lifecycle, binding coordinator, gameplay controller, movement adapter or input-command router.

## Accepted upstream evidence

F53 starts after these package + QA results are clean:

```text
F51A — PlayerView binding adapter
F51B — PlayerView camera target binding adapter
F51C — PlayerView camera activation adapter
F51D — PlayerView camera binding chain consolidation
F52A — PlayerControl binding adapter
F52B — Unity PlayerInput bridge
F52C — Unity PlayerInput action-map activation boundary
F52D — PlayerControl Unity PlayerInput chain consolidation
```

## F53A scope

F53A is a FIRSTGAME preflight proof:

```text
FIRSTGAME project imports framework package
FIRSTGAME project imports Unity Input System
FIRSTGAME can compile against F51/F52 public components
FIRSTGAME can create explicit proof GameObjects/components
FIRSTGAME can reference the real scene PlayerInput
FIRSTGAME can verify the expected gameplay action map exists
FIRSTGAME can verify the non-goals remain non-goals
```

## F53A-B1 strictness

F53A-B1 tightens acceptance:

```text
expectedGameplayActionMap='True' is required for status='Succeeded'
```

The FIRSTGAME preflight creator must not create a temporary duplicate `PlayerInput`, because that can compete with the real player input device pairing. It must reuse an existing scene `PlayerInput` and fail explicitly if the required action map cannot be found.

## F53A non-goals

F53A must not implement:

```text
new framework contracts
new package runtime
new QA synthetic scenarios
movement
CharacterController integration
Rigidbody integration
InputAction value reading
InputAction-to-command routing
gameplay command execution
actor spawning
runtime lifecycle/coordinator
route/activity automatic binding
production scene migration
```

## Proof shape

The FIRSTGAME delta provides a small local proof namespace:

```text
Assets/_Project/Scripts/FrameworkProof/
Assets/_Project/Scripts/Editor/FrameworkProof/
Assets/_Project/Documentation/
```

It must stay local to FIRSTGAME and must not be copied into the framework package as runtime behavior.

## Acceptance

F53A is accepted when FIRSTGAME:

```text
compiles after the delta
can run the editor preflight menu
logs F53A_FIRSTGAME_PLAYER_BINDING_PREFLIGHT status='Succeeded'
logs playerInput='True'
logs inputActions='True'
logs expectedGameplayActionMap='True'
keeps movement='False'
keeps actorSpawning='False'
keeps gameplayCommandExecution='False'
```

## Next after F53A

Only after F53A passes with strict action-map validation should a later F53B attempt a real scene/prefab wiring proof in FIRSTGAME.

F53B still must not introduce new framework contracts in the consumer.
