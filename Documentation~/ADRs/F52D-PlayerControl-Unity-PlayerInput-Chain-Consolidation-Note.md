# F52D — PlayerControl Unity PlayerInput Chain Consolidation

Status: **Accepted / Documentation-only**

## Objective

Consolidate the PlayerControl Unity PlayerInput chain after F52A-F52C and freeze its current semantics before any FIRSTGAME usability proof or later gameplay-facing input work.

## Scope

This cut documents:

```text
PlayerControl binding
Unity PlayerInput bridge
Unity PlayerInput action-map activation
failure semantics
clear/no-op semantics
explicit boundary exclusions
next lane
```

## Out of scope

This cut does not add runtime code, editor code, QA scenes or new behavior.

It does not introduce:

```text
InputAction routing
InputAction value reading
movement
CharacterController
Rigidbody
gameplay command execution
actor spawning
automatic route/activity lifecycle
camera activation
FIRSTGAME integration
```

## Decision

The current PlayerControl Unity PlayerInput chain is accepted as a minimal explicit adapter stack:

```text
F52A — PlayerControlBindingAdapter
F52B — UnityPlayerInputBridgeAdapter
F52C — UnityPlayerInputActivationAdapter
```

Each adapter owns one step and one clear operation. Each step must return an explicit result object and must not hide missing required state through global lookup, scene search or implicit fallback.

## Accepted flags

A successful F52C activation may report:

```text
controlBinding='True'
unityPlayerInputBridge='True'
inputActivation='True'
```

The following must remain outside this lane:

```text
viewBinding='False'
cameraActivation='False'
movement='False'
actorSpawning='False'
```

## Architectural gain

The framework now has a minimal PlayerControl input chain that is:

```text
explicit
incremental
testable
clearable
free of movement assumptions
free of gameplay command execution
free of hidden PlayerInput discovery
```

This allows a FIRSTGAME usability proof without mixing input ownership, movement and gameplay execution in one framework cut.

## Acceptance evidence

F52A, F52B and F52C are expected to remain the technical proof for this consolidation. F52D itself is documentation-only.

## Next lane

Recommended next lane:

```text
F53 — FIRSTGAME PlayerView / PlayerControl usability proof
```

The FIRSTGAME lane must prove usability of the existing contracts only. New framework contracts must still be created and validated in package + QA before they are used in FIRSTGAME.
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
