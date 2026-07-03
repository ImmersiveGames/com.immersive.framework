# FIRSTGAME-2B — Pause Input Action Trigger

Status: Preview 5 usage note.

Use `PauseInputActionTrigger` for the first playable keyboard Pause flow.

This component is intentionally simpler than the advanced InputMode bridge path:

- subscribes to explicit Unity Input System actions;
- sends `PauseRequestKind.Toggle`, `Pause` or `Resume` to the current `FrameworkRuntimeHost`;
- does not switch action maps;
- does not require `UnityInputTargetDeclaration` roles;
- does not require `PlayerActorDeclaration`;
- does not require `PlayerInputManager`;
- does not spawn actors;
- does not own gameplay input.

## Recommended FIRSTGAME setup

In `FG_UIGlobal`:

```text
PauseInput
  FG_PlayerInput
    PlayerInput
  FG_PauseKeyboard
    PauseInputActionTrigger
```

`FG_PlayerInput`:

```text
PlayerInput.actions = FG_InputActions
```

`FG_InputActions`:

```text
Player/Pause = <Keyboard>/escape
UI/Pause     = <Keyboard>/escape
```

`FG_PauseKeyboard`:

```text
PauseInputActionTrigger
  Player Input = FG_PlayerInput
  Actions Asset = optional; empty uses PlayerInput.actions
  Player Action Map Name = Player
  UI Action Map Name = UI
  Pause Action Name = Pause
  Request Kind = Toggle
  Enable Resolved Actions On Enable = true
```

## Expected logs

When pressing Escape:

```text
Pause Input Action Trigger completed. action='Player/Pause' requestKind='Toggle' status='Applied' previousState='Running' currentState='Paused'
Pause Request completed. kind='Toggle' status='Applied' previousState='Running' currentState='Paused' pauseSurface='Succeeded'
```

Pressing Escape again:

```text
Pause Input Action Trigger completed. action='UI/Pause' requestKind='Toggle' status='Applied' previousState='Paused' currentState='Running'
Pause Request completed. kind='Toggle' status='Applied' previousState='Paused' currentState='Running' pauseSurface='Succeeded'
```

## Not for this cut

Do not use the advanced `PauseInputActionRuntimeBridgeTrigger` and `PauseInputModeUnityPlayerInputRuntimeBridge` for FIRSTGAME-2B unless the cut explicitly validates InputMode and action-map switching.
