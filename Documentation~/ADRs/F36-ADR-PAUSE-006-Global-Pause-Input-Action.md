# F36-ADR-PAUSE-006 — Global Pause Input Action

Status: Accepted  
Phase: F36 / FIRSTGAME-2D — Pause Input Track Correction  
Type: Runtime / Authoring Boundary  
Last updated: 2026-07-03

## Context

F35 accepted basic simulation Pause through `Time.timeScale = 0` and allowed `PauseInputActionTrigger` to listen to `Player/Pause` and `UI/Pause`, with same-frame dedupe preventing double toggles.

That authoring shape passed the first Pause smoke, but it exposed a design problem during FIRSTGAME-3: after Resume, UI buttons could stop responding when the same `PlayerInput` action-map switch disabled the UI action map used by the EventSystem path.

A workaround that keeps the UI action map enabled while `PlayerInput.currentActionMap` returns to `Player` would create duplicated input lanes and hidden state:

```text
PlayerInput.currentActionMap = Player
UI action map also kept enabled manually
```

That is not the canonical framework shape.

## Decision

Pause input must use one global/system command track:

```text
Global/Pause
-> PauseInputActionTrigger
-> FrameworkRuntimeHost.RequestPause(Toggle)
-> PauseRuntime
-> Time.timeScale effect
-> PauseSurfaceRuntime
-> Gate snapshot
```

`PauseInputActionTrigger` resolves exactly one configured Pause action. The default authoring is:

```text
Pause Action Map Name = Global
Pause Action Name = Pause
Request Kind = Toggle
```

The trigger no longer uses the old normal path:

```text
Player/Pause + UI/Pause + dedupe_same_frame
```

Same-frame dedupe may remain as a defensive guard, but it is not expected as the normal success path.

## PlayerInput action-map switching

Direct PlayerInput action-map switching remains optional and explicit:

```text
Switch Player Input Action Map = false by default
```

It may be enabled only when the referenced `PlayerInput` is the gameplay-owned input lane and the EventSystem/UI input lane is independent.

It must not be used as a hidden way to keep UI buttons alive. UI click/point/submit belongs to the EventSystem/UI input setup, not to a Pause trigger workaround.

## Accepted authoring model

Recommended Input Actions asset:

```text
Global
  Pause = <Keyboard>/escape

Player
  Move
  Interact
  ...

UI
  Point
  Click
  Navigate
  Submit
  Cancel
```

Recommended `PauseInputActionTrigger` setup:

```text
Player Input = optional PlayerInput that references the same InputActionAsset
Actions Asset = optional explicit InputActionAsset
Pause Action Map Name = Global
Pause Action Name = Pause
Request Kind = Toggle
Switch Player Input Action Map = false
Reason = firstgame.pause.keyboard.toggle
```

If controller-only Pause overlay navigation later requires gameplay input switching, enable:

```text
Switch Player Input Action Map = true
Gameplay Action Map Name = Player
Pause UI Action Map Name = UI
```

only when UI/EventSystem actions are not dependent on that same PlayerInput map lifecycle.

## Rejected

- Canonicalizing `Keep UI Action Map Enabled` as a Pause trigger feature.
- Using `Player/Pause` and `UI/Pause` as the normal Pause command path.
- Treating same-frame dedupe as the expected normal Pause path.
- Hiding EventSystem/UI input liveness inside the Pause trigger.
- Using the advanced bridge for simple `Escape -> Toggle Pause`.

## Consequences

Accepted:

- Pause has a single input command track.
- The Pause trigger is easier to reason about.
- UI buttons after Resume are not dependent on a duplicate enabled action map workaround.
- PlayerInput switching remains available, but only as an explicit gameplay-lane side effect.

Required migration for FIRSTGAME:

```text
1. Add Global/Pause to InputSystem_Actions.
2. Configure PauseInputActionTrigger to Pause Action Map Name = Global.
3. Remove/ignore Player/Pause and UI/Pause as the canonical Pause trigger path.
4. Set Switch Player Input Action Map = false unless the UI/EventSystem lane is independent.
```

## Validation evidence expected

Ready log:

```text
Pause Input Action Trigger ready.
pauseAction='Global/Pause'
pauseActionMap='Global'
actionMapSwitching='False'
```

Pause log:

```text
Pause Input Action Trigger completed.
action='Global/Pause'
requestKind='Toggle'
status='Applied'
currentState='Paused'
actionMapSwitchStatus='Disabled'
```

There should be no normal `dedupe_same_frame` line for the same Escape press.
