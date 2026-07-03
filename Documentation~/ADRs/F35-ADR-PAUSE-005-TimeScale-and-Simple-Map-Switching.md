# F35-ADR-PAUSE-005 — Pause TimeScale and Simple PlayerInput Map Switching

Status: Accepted  
Phase: F35 / FIRSTGAME-2C — Pause Simulation Effect  
Type: Runtime / Authoring Boundary  
Last updated: 2026-07-03

## Context

FIRSTGAME-2 validated logical Pause, resident `UIGlobal` Pause presentation and Gate blockers. FIRSTGAME-2B validated `PauseInputActionTrigger` as the simple keyboard path for `Escape -> Toggle Pause`.

That state was not enough for a game designer expectation of Pause. It changed framework state, showed the surface and emitted blockers, but ordinary Unity simulation code, physics, `Update()` loops and time-driven gameplay would continue unless each consumer manually integrated with Gate.

For the first playable consumer flow, Pause must have one concrete simulation effect by default. Unity 6.x UI and many UI animations can still operate using unscaled time, so a basic `Time.timeScale = 0` pause is acceptable for the initial model.

## Decision

Pause now has four canonical effects in the preview model:

```text
PauseRuntime state
+ PauseGateBlockerPolicy snapshot
+ resident UIGlobal Pause surface
+ basic simulation pause via Time.timeScale
```

When a Pause request changes state from `Running` to `Paused`, the runtime captures the current running time scale and applies:

```text
Time.timeScale = 0
```

When a Resume/Toggle request changes state from `Paused` to `Running`, the runtime restores the captured running value instead of assuming `1`.

```text
Running timeScale 1.0 -> Pause -> 0 -> Resume -> 1.0
Running timeScale 0.5 -> Pause -> 0 -> Resume -> 0.5
```

`Time.timeScale` is therefore an accepted default simulation effect of Pause. It is not the complete identity of Pause; the authoritative Pause state remains `PauseRuntime`.

## Simple action map switching

`PauseInputActionTrigger` may directly switch the referenced Unity `PlayerInput` action map after a successful request:

```text
Paused  -> UI action map
Running -> Player/action gameplay map
```

This is intentionally a small authoring convenience for FIRSTGAME. It does not reintroduce the advanced bridge preflight path and does not require Player/Actor/InputTarget evidence.

The minimal flow is now:

```text
Escape
-> PauseInputActionTrigger
-> FrameworkRuntimeHost.RequestPause(Toggle)
-> PauseRuntime
-> Time.timeScale effect
-> PauseSurfaceRuntime
-> Gate snapshot
-> optional PlayerInput action map switch
```

## Accepted authoring model

A consumer scene can author Pause input in `FG_UIGlobal` as:

```text
FG_UIGlobal
  PauseInput
    FG_PlayerInput
      PlayerInput
    FG_PauseKeyboard
      PauseInputActionTrigger
```

Recommended fields:

```text
Player Input = FG_PlayerInput
Player Action Map Name = Player
UI Action Map Name = UI
Pause Action Name = Pause
Request Kind = Toggle
Switch Player Input Action Map = true
Gameplay Action Map Name = Player
Pause UI Action Map Name = UI
Reason = firstgame.pause.keyboard.toggle
```

The Input Actions asset should contain:

```text
Player/Pause = <Keyboard>/escape
UI/Pause     = <Keyboard>/escape
```

Same-frame dedupe remains required because both actions may observe the same physical key.

## Explicit non-goals

This ADR does not add:

- Pause as a Route.
- Pause as an Activity.
- Save/options menu behavior.
- A full input ownership model.
- PlayerInputManager join/spawn behavior.
- Per-system adapters for Animator, AudioSource, Rigidbody or AI.
- A service locator or global manager.

Those may be handled by later adapters if needed. The preview model only establishes the basic simulation effect and a simple UI action map handoff.

## Relationship to older Pause ADRs

- F20 remains the source for Pause state and Gate policy.
- F23 remains the source for resident Pause content/surface boundaries.
- F34 remains the source for the direct input trigger.
- F35 amends the earlier “no Time.timeScale as contract” language: `Time.timeScale` is accepted as the basic simulation effect, while the contract remains the Pause runtime state plus result snapshot.

## Validation evidence

FIRSTGAME preview.6 passed with observable runtime evidence:

```text
Pause Request completed.
kind='Toggle'
source='PauseInputActionTrigger'
status='Applied'
previousState='Running'
currentState='Paused'
timeScale='AppliedPausedTimeScale'
previousTimeScale='1'
targetTimeScale='0'
currentTimeScale='0'
capturedRunningTimeScale='1'
pauseSurfacePaused='True'

Pause Request completed.
kind='Toggle'
source='PauseInputActionTrigger'
status='Applied'
previousState='Paused'
currentState='Running'
timeScale='RestoredRunningTimeScale'
previousTimeScale='0'
targetTimeScale='1'
currentTimeScale='1'
capturedRunningTimeScale='1'
pauseSurfacePaused='False'
```

The input trigger also confirmed simple action map switching:

```text
Pause Input Action Trigger ready.
actionMapSwitching='True'
gameplayActionMap='Player'
pauseUiActionMap='UI'

Pause Input Action Trigger switched PlayerInput action map.
previousActionMap='UI'
targetActionMap='Player'
selectedActionMap='Player'
pauseState='Running'
```

## Documentation impact

The canonical Usage guide must describe Preview 6 Pause as:

```text
Logical Pause
+ TimeScale pause
+ Pause Surface
+ Gate Snapshot
+ optional PlayerInput action map switching
```

It must no longer say that `Time.timeScale` is outside the Pause model.
