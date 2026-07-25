# Pause Usage

Status: Current  
Last updated: 2026-07-25

## Responsibilities

```text
PauseRuntime
  owns logical Running / Paused state

PausePlayerInputBinding
  connects the officially admitted PlayerInput
  owns Global / Gameplay input posture through the product runtime

PauseRequestTrigger
  exposes Pause / Resume / Toggle to UnityEvent and UI Button

UnityPauseSurfaceAdapter
  presents the current PauseSnapshot

SceneLifecycleRuntime
  injects and releases scene-scoped request bindings
```

No authored component searches for `FrameworkRuntimeHost`.

## Required Player composition

The current Pause product requires one active official Player binding:

```text
Local Player Host
  PlayerInput
  UnityPlayerInputGateAdapter
  PausePlayerInputBinding
```

The Player host is admitted through the canonical Player/Activity lifecycle.
`PlayerSlotId` is runtime evidence and is not fixed in the prefab.

The configured `PlayerInput.actions` must contain:

```text
Global
  PauseToggle
    Keyboard / Escape
    Gamepad / Start

configured gameplay action map
```

`Global` is an action map of the Player, not a second or global Player.

## Persistent presentation

The application Persistent Content scene contains the reusable presentation:

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

## Authored request triggers

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
  bound when SceneLifecycle reports the exact scene roots as available
  released before that exact scene unloads
```

The Trigger never needs a serialized runtime reference and must not call a
singleton or scene search.

## Buttons

Configure a Unity UI Button persistent call to one of:

```text
PauseRequestTrigger.RequestPause
PauseRequestTrigger.RequestResume
PauseRequestTrigger.TogglePause
```

A button does not require an input action to be pressed. It still uses the same
Pause product runtime as `Escape`, so the current product requires an active
official `PausePlayerInputBinding`.

Two distinct diagnostics matter:

```text
"Pause product request port is not bound."
  the Trigger was not composed by Persistent Content or SceneLifecycle

"no active PlayerInput binding is available"
  the Trigger has the request port, but no official PausePlayerInputBinding
  is active
```

## Runtime flow

```text
Escape / Gamepad Start
  -> PausePlayerInputBinding
  -> PauseProductBindingRuntimeContext

or

UI Button
  -> PauseRequestTrigger
  -> injected IPauseProductRequestPort
  -> PauseProductBindingRuntimeContext

then

PauseRuntime
  -> PauseSnapshot
  -> PauseSurfaceRuntime
  -> UnityPauseSurfaceAdapter
```

## Rejected compositions

```text
PauseRequestTrigger
  -> FindObjectOfType<FrameworkRuntimeHost>

PauseRequestTrigger
  -> FrameworkRuntimeHost.Instance

PauseRequestTrigger
  -> global service locator

duplicate PlayerInput created only to listen for Escape
```

## Manual validation

1. Enter an Activity with one officially admitted local Player.
2. Confirm `PausePlayerInputBinding.BindingStatus` is `Bound`.
3. Confirm Route and Activity `PauseRequestTrigger.ProductRequestBindingStatus`
   are `Bound`.
4. Call Pause from the Route trigger.
5. Confirm logical state is `Paused`, only `Global` remains enabled and the
   persistent surface appears.
6. Call Resume from the Activity or persistent trigger.
7. Leave the Activity and Route.
8. Confirm scene release completes without foreign/stale binding diagnostics.
