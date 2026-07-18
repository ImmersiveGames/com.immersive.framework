# F38-ADR-INPUT-001 — Unity PlayerInput Gate Adapter

Status: Accepted / preview.9

## Context

Preview 8 introduced Transition Gate. Route, Activity and ActivityClear transitions can now expose Gate blockers for lifecycle requests, input acceptance, interaction acceptance and gameplay actions.

That closes the logical Gate layer, but Unity gameplay scripts that read `InputAction` values directly do not automatically obey that Gate. In the First Game prototype, `FirstGamePlayerMover` is intentionally consumer-side and temporary, so it should not be made framework-aware just to prove Gate behavior.

## Decision

Add an opt-in Unity Input System adapter:

```text
UnityPlayerInputGateAdapter
```

The adapter observes the current framework Gate snapshot and suppresses a configured gameplay `PlayerInput` lane while relevant blockers are active.

Default behavior:

```text
Block On Input Acceptance = true
Block On Gameplay Action = true
Block Mode = Disable Action Map
Gameplay Action Map Name = Player
```

When the Gate is blocked, the adapter disables the configured action map. When the Gate releases, it restores only the state it changed.

## Boundaries

The adapter does not:

- spawn players;
- own `PlayerInputManager`;
- create Player/Actor lifecycle;
- read movement commands;
- replace InputMode;
- use `Time.timeScale`;
- make Pause use `Player/Pause + UI/Pause` again.

Current Pause input uses the canonical Pause/InputMode bridge described by
`ADR-INPUT-0001`, `ADR-INPUT-0002` and `ADR-INPUT-0003`; this historical ADR
does not define a Pause authoring flow.

## First Game setup

```text
PlayerPrototype
  PlayerInput
  FirstGamePlayerMover
  UnityPlayerInputGateAdapter
    Player Input = PlayerPrototype/PlayerInput
    Gameplay Action Map Name = Player
    Block On Input Acceptance = true
    Block On Gameplay Action = true
    Block Mode = Disable Action Map
```

## Consequences

This lets a temporary consumer mover obey Pause and Transition Gate without coupling the mover to framework internals. It is a bridge, not the future official movement system.

A later Player/Actor/Movement cut may replace this with a more mature capability participant or input ownership model.
