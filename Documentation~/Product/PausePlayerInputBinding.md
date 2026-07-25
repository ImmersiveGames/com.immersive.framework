# Pause PlayerInput Binding

`PausePlayerInputBinding` is the single-player, scene-local product surface for
physical Pause input and PlayerInput posture.

## Authoring

Add it to the same GameObject as the gameplay `PlayerInput`, assign the
`InputActionReference` for `Global/Pause`, and set the `Global` and gameplay
action maps. Use **Apply/Rebuild Technical Binding** to create or validate the
co-located `UnityPlayerInputGateAdapter`.

Apply/Rebuild never removes adapters. It creates one when absent, reuses one
compatible adapter, and blocks with a diagnostic for multiple or incompatible
adapters.

## Runtime ownership

Scene Lifecycle provides the binding port for exactly the scene being composed.
The binding registers one `PlayerInput`, receives an opaque token, applies
`Global + gameplay`, and resolves the action by GUID in `PlayerInput.actions`.
No asset-action name fallback is used.

Physical Pause input requires this active binding:

```text
Escape / Gamepad Start
  -> PausePlayerInputBinding
  -> PauseProductBindingRuntimeContext
  -> logical Pause + InputMode transaction
```

Authored `PauseRequestTrigger` buttons use the same product request port but do
not require a physical PlayerInput binding:

```text
PauseRequestTrigger
  -> logical Pause, TimeScale and Pause Surface

with active PlayerInput binding
  -> status Applied
  -> InputMode/action maps are transacted

without active PlayerInput binding
  -> status AppliedWithoutPlayerInput
  -> executionMode ApplicationOnly
  -> no action maps are modified
```

This is an explicit execution mode, not a silent fallback. Failed or inconsistent
PlayerInput binding evidence remains a blocking `BindingUnavailable` result.

On scene release, the lifecycle releases the exact token before unload. The
runtime restores the original PlayerInput posture and releases the InputMode
context. A normal request rollback restores the previous Pause snapshot; only
lifecycle teardown has the explicit Running policy.

## Not part of this surface

Actor replacement, multiplayer Pause policy and automatic creation of a Player
are not owned by this component.
