# F35-ADR-PAUSE-005 — Pause TimeScale and Simple PlayerInput Map Switching

Status: Accepted / Amended by F36-ADR-PAUSE-006  
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

## Global Pause input correction

F36 amends the input authoring part of this ADR. The minimal flow is now:

```text
Global/Pause
-> PauseInputActionTrigger
-> FrameworkRuntimeHost.RequestPause(Toggle)
-> PauseRuntime
-> Time.timeScale effect
-> PauseSurfaceRuntime
-> Gate snapshot
```

`PauseInputActionTrigger` may still directly switch a referenced Unity `PlayerInput` action map after a successful request, but this is optional and disabled by default. Enable it only when that `PlayerInput` is a gameplay input lane and the EventSystem/UI input lane is independent.

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

Recommended fields after F36:

```text
Player Input = optional FG_PlayerInput or explicit Actions Asset
Pause Action Map Name = Global
Pause Action Name = Pause
Request Kind = Toggle
Switch Player Input Action Map = false
Reason = firstgame.pause.keyboard.toggle
```

The Input Actions asset should contain:

```text
Global/Pause = <Keyboard>/escape
```

Do not use `Player/Pause + UI/Pause` as the canonical path. Same-frame dedupe is defensive only.

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

The preview.6 input trigger also confirmed simple action map switching, but F36 later amended the canonical input track to use `Global/Pause`:

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


## F36 amendment

F36 keeps the TimeScale decision accepted and changes the input lane decision:

- Canonical Pause input is one action: `Global/Pause`.
- Duplicate `Player/Pause` and `UI/Pause` are no longer the normal authoring path.
- Direct PlayerInput map switching is optional, disabled by default, and must not be used as a UI action-map keep-alive workaround.
