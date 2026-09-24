# Pause Usage

Status: Current  
Last updated: 2026-08-06

## Responsibilities

```text
PauseRuntime
  owns logical Running / Paused state

PlayerPauseInput
  single-player physical Pause action and lifecycle registration

UnityPlayerInputGateAdapter
  explicit PlayerInput and Gameplay Action Map authority
  physical writer adapter for the Framework Gate

PauseRequestTrigger
  exposes Pause / Resume / Toggle to UnityEvent and UI Button

UnityPauseSurfaceAdapter
  presents the current PauseSnapshot

SceneLifecycleRuntime
  injects and releases scene-scoped request bindings
```

No authored component searches for `FrameworkRuntimeHost`. Actor replacement,
multiplayer Pause policy and automatic Player creation are not owned by these
surfaces.

## Two supported request modes

### Physical Player input

`Escape` or Gamepad Start requires an active binding on the admitted Local
Player Host:

```text
PlayerInput
LocalPlayerHostAuthoring
PlayerPauseInput
UnityPlayerInputGateAdapter
```

Physical path:

```text
Escape / Gamepad Start
  -> PlayerPauseInput
  -> PauseProductBindingRuntimeContext
  -> logical Pause + InputMode transaction
```

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
require an active `PlayerPauseInput`.

Result:

```text
productStatus = AppliedWithoutPlayerInput
executionMode = ApplicationOnly
```

The framework applies logical Pause, `Time.timeScale` and the persistent Pause
surface. It does not create a Player and does not modify action maps.

This is explicit product behavior, not a silent fallback. Failed or inconsistent
Player binding evidence is rejected as `BindingUnavailable`.

## Authoring PlayerPauseInput

Add `PlayerPauseInput` and exactly one `UnityPlayerInputGateAdapter` to the
same GameObject. The Gate Adapter is the only authoring authority for the
gameplay `PlayerInput` and Gameplay Action Map.

### References

```text
Pause Action
  InputActionReference
  resolved by action GUID

Global Action Map
  derived from Pause Action.actionMap
  not separately typed by the designer

Player Input and Gameplay Action Map
  authored on UnityPlayerInputGateAdapter
  Gameplay map stores InputActionAsset + Action Map GUID
```

Runtime never falls back to Action Map names. The selected Gameplay map is
resolved by GUID against the exact `PlayerInput.actions` instance, including
PlayerInput-owned action-asset copies. A cached map name exists only for
Inspector display and diagnostics.

### Authoring flow

1. Add exactly one `UnityPlayerInputGateAdapter` to the Local Player Host.
2. Assign its exact `PlayerInput` and Gameplay Action Map.
3. Add `PlayerPauseInput` to that same GameObject.
4. Assign the Pause `InputActionReference` (for example `Global/Pause`).

`PlayerPauseInput` shows the Gate-owned target and Gameplay map read-only in
its Inspector. It neither creates nor overwrites a Gate Adapter.

Technical commands and verbose runtime evidence live in the collapsed
`Advanced / Debug` foldout.

### Failure behavior

The composition fails explicitly when:

```text
Gate Adapter is missing or duplicated
Gate Adapter PlayerInput or actions are missing
Pause Action is missing
Pause Action GUID is absent from PlayerInput.actions
Gate Adapter Gameplay Action Map reference is missing or invalid
Gameplay map GUID is absent from the Gate Adapter PlayerInput.actions
Global and Gameplay resolve to the same map
```

No runtime map-name fallback, hierarchy search, singleton or service locator is
used.

### Legacy migration

Legacy Gameplay map migration remains owned by `UnityPlayerInputGateAdapter`.
`PlayerPauseInput` no longer reads legacy PlayerInput or map fields; the Global
map is always derived from the assigned Pause Action.

## Runtime ownership

Scene Lifecycle provides the binding port for exactly the scene being composed.
The binding registers one `PlayerInput`, receives an opaque token, applies
`Global + gameplay`, and resolves the action by GUID in `PlayerInput.actions`.

On scene release, the lifecycle releases the exact token before unload. The
runtime restores the original PlayerInput posture and releases the InputMode
context. A normal request rollback restores the previous Pause snapshot; only
lifecycle teardown has the explicit Running policy.

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

1. Enter gameplay with no `PlayerPauseInput`.
2. Confirm Route/Activity Trigger binding reports `Bound`.
3. Press the authored Pause button.
4. Confirm `AppliedWithoutPlayerInput`, paused TimeScale and visible surface.
5. Press Resume and confirm `Running`.
6. Repeat with an official Player binding.
7. Confirm `Applied` and `PlayerInputTransaction`.
8. Leave Route/Activity and confirm exact trigger release.
