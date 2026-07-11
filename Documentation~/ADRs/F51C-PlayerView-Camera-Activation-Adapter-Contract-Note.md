> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# F51C — PlayerView Camera Activation Adapter Contract

Status: Accepted / implementation cut.

## Objective

Add the first explicit Unity camera activation adapter for the PlayerView binding lane.

F51C consumes the F51B camera-target binding evidence and activates exactly one explicit `UnityEngine.Camera` supplied by an authored target.

```text
PlayerViewBindingSnapshot
  -> PlayerViewCameraTargetBindingSnapshot
  -> PlayerViewCameraActivationAdapter
  -> IPlayerViewCameraActivationTarget
  -> PlayerViewCameraActivationSnapshot
```

## Scope

- Add camera activation result/status/failure primitives.
- Add `PlayerViewCameraActivationSnapshot`.
- Add `IPlayerViewCameraActivationTarget`.
- Add `PlayerViewCameraActivationTargetBehaviour`.
- Add `PlayerViewCameraActivationAdapter`.
- Validate explicit failure/no-op cases in QA.

## Out of scope

- Cinemachine.
- CameraDirector.
- Camera priority arbitration.
- `Camera.main` lookup.
- Multi-camera selection policy.
- Runtime lifecycle/coordinator.
- PlayerInput/input activation.
- Control binding.
- Movement enable/disable.
- Actor spawning.
- FIRSTGAME integration.

## Boundary rule

F51C may report:

```text
viewBinding='True'
cameraTargetBinding='True'
cameraActivation='True'
```

F51C must still report:

```text
controlBinding='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Architectural gain

The framework now has a minimal, testable path from PlayerView readiness to an explicitly activated Unity camera without hiding global orchestration inside the adapter.

This deliberately stops before priority, Cinemachine and lifecycle so that those systems can be introduced as separate explicit cuts.

## Expected smoke

```text
[F51C_PLAYER_VIEW_CAMERA_ACTIVATION_QA] status='Succeeded'
```
