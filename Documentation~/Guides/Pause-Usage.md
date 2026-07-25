# Pause Usage

Status: Current
Last updated: 2026-07-25

## Responsibilities

```text
PauseRuntime
  owns logical Running / Paused state

PausePlayerInputBinding
  connects an official PlayerInput
  owns Global / Gameplay posture when present

PauseRequestTrigger
  exposes Pause / Resume / Toggle to UnityEvent and UI Button

UnityPauseSurfaceAdapter
  presents the current PauseSnapshot

SceneLifecycleRuntime
  injects and releases scene-scoped request bindings
```

No authored component searches for `FrameworkRuntimeHost`.

## Two supported request modes

### Physical Player input

`Escape` or Gamepad Start requires:

```text
Local Player Host
  PlayerInput
  UnityPlayerInputGateAdapter
  PausePlayerInputBinding
```

The configured `PlayerInput.actions` contains `Global/PauseToggle` and the
configured gameplay action map.

Result:

```text
productStatus = Applied
executionMode = PlayerInputTransaction
```

Pause, InputMode and action maps commit as one transaction.

### Authored button without Player input

A UI Button may call:

```text
PauseRequestTrigger.RequestPause
PauseRequestTrigger.RequestResume
PauseRequestTrigger.TogglePause
```

The Trigger requires an injected `IPauseProductRequestPort`, but it does not
require an active `PausePlayerInputBinding`.

Result:

```text
productStatus = AppliedWithoutPlayerInput
executionMode = ApplicationOnly
```

The framework applies logical Pause, `Time.timeScale` and the persistent Pause
surface. It does not create a Player and does not modify action maps.

This is explicit product behavior, not a silent fallback. If Player binding
evidence is failed or inconsistent, the request is rejected as
`BindingUnavailable`.

## Trigger locations

`PauseRequestTrigger` may be authored in:

```text
Persistent Content
Route primary/content scene
Activity content scene
```

Binding is automatic:

```text
Persistent Content
  bound during application boot

Route / Activity
  bound from exact SceneLifecycle roots
  released before the exact scene unloads
```

## Diagnostics

Every authored request emits a structured framework log.

Application-only success:

```text
[INFO][Immersive.Framework][PauseRequestTrigger]
Pause Request completed.
productStatus='AppliedWithoutPlayerInput'
executionMode='ApplicationOnly'
```

PlayerInput transaction success:

```text
productStatus='Applied'
executionMode='PlayerInputTransaction'
```

Distinguish:

```text
Pause product request port is not bound
  Trigger was not composed

AppliedWithoutPlayerInput
  Trigger was composed and logical Pause succeeded without Player input

BindingUnavailable
  Player binding evidence exists but is failed/inconsistent

Failed
  logical or physical application failed
```

## Persistent presentation

The application Persistent Content scene normally contains:

```text
GlobalCanvas
  PauseSurface
    UnityPauseSurfaceAdapter
    Visual
      Resume Button
        PauseRequestTrigger
```

The adapter only projects Pause state. It does not own Pause, input maps or
`Time.timeScale`.

## Manual validation

1. Enter gameplay with no `PausePlayerInputBinding`.
2. Confirm Route/Activity Trigger binding reports `Bound`.
3. Press the authored Pause button.
4. Confirm `AppliedWithoutPlayerInput`, paused TimeScale and visible surface.
5. Press Resume and confirm `Running`.
6. Repeat with an official Player binding.
7. Confirm `Applied` and `PlayerInputTransaction`.
8. Leave Route/Activity and confirm exact trigger release.
