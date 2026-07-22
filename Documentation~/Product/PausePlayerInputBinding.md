# Pause PlayerInput Binding

`PausePlayerInputBinding` is the single-player, scene-local product surface for Pause.

## Authoring

Add it to the same GameObject as the gameplay `PlayerInput`, assign the `InputActionReference` for `Global/Pause`, and set the `Global` action map plus the configured gameplay action map. The default gameplay action-map name is `Player`; a project or QA fixture may use another configured name such as `Gameplay`. Use **Apply/Rebuild Technical Binding** to create or validate the co-located `UnityPlayerInputGateAdapter`.

Apply/Rebuild never removes adapters. It creates one when absent, reuses one compatible adapter, and blocks with a diagnostic for multiple or incompatible adapters.

## Runtime ownership

Scene Lifecycle provides the binding port for exactly the scene being composed. The binding registers one `PlayerInput`, receives an opaque token, applies `Global + configured gameplay action map`, and resolves the action by GUID in `PlayerInput.actions`. No asset-action name fallback is used.

`PauseRequestTrigger` in UIGlobal and the physical action both call the same product request port. With no active binding, requests report `BindingUnavailable` and do not change Pause, TimeScale, surface, or PlayerInput.

The current physical product posture applies `Global + configured gameplay action
map` while running and `Global` only while paused. `InputModeKind.PauseOverlay` remains the logical paused
posture; it does not imply a UI action map. A future interactive Pause UI may introduce
`Global + UI` only together with its own authorable actions, bindings, input module and
consumers.

On scene release, the lifecycle releases the exact token before unload. The runtime restores the original PlayerInput posture and releases the InputMode context. A normal request rollback restores the previous Pause snapshot; only lifecycle teardown has the explicit Running policy.

## Not part of this surface

Actor, Slot, Provisioning, PlayerInputManager, Camera, multiplayer pause policy,
and FIRSTGAME integration are not required by this P1 surface. The superseded
`PauseInputModeUnityPlayerInputRuntimeBridge` and
`PauseInputActionRuntimeBridgeTrigger` APIs were removed; there is no technical
compatibility or regression path parallel to the product binding.
