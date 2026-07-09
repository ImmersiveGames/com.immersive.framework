# ADR-PROD-0005 — Camera Product Surface requires Cinemachine

Status: Accepted
Date: 2026-07-09
Package: `com.immersive.framework`
Area: Camera Product Surface

## Context

The current camera implementation in the package is split across technical lanes:

```text
Route / Activity camera skeleton
PlayerView camera binding / activation chain
PlayerComposer camera anchors
experimental CameraComposer MVP-A foundation
```

This is not sufficient for the intended product direction.

The framework is not trying to merely enable or disable Unity cameras. The intended product is an authorable camera system for real games: follow cameras, look-at targets, route/activity camera changes, player-targeted cameras, transitions, and future local multiplayer or group cameras.

A pure `UnityEngine.Camera.enabled` pipeline cannot be the official final product shape. It can remain as low-level diagnostic or legacy evidence, but it must not define the Camera Product Surface.

## Decision

Camera Product Surface will be restructured around Cinemachine as an official package requirement.

The framework camera product model becomes:

```text
Camera Recipe / Profile / Template
  reusable intent

Camera Composer / Authoring Component
  designer-first surface on a camera rig or scene prefab

Cinemachine Rig Materialization
  Cinemachine Brain / Cinemachine Camera / target bindings / priority / blending-ready data

Camera Target Source Contracts
  explicit source of tracking/look-at targets: PlayerComposer, Route, Activity, Group, or explicit Transform

Scoped Camera Runtime Authority
  future typed authority for active camera state when runtime coordination is required

Diagnostics
  Apply/Rebuild reports, validation, debug evidence, QA smokes
```

Cinemachine is mandatory for the official Camera Product Surface. The framework should not try to maintain a parallel pure-camera product path.

## Consequences

### Required changes

- Add `com.unity.cinemachine` as a package dependency.
- Add Cinemachine assembly references where required.
- Replace the current `CameraComposer MVP-A` direction with a Cinemachine-first composer.
- Define explicit camera ownership/scope contracts before broad implementation.
- Define explicit target-source contracts before player integration.
- Separate single-player camera behavior from local multiplayer camera behavior.
- Treat legacy pure camera activation as technical evidence, not product UX.

### Preserved components

These may remain useful, but their role changes:

```text
FrameworkCameraAnchorHost
  survives as target evidence or target provider if compatible.

FrameworkCameraDirector
  must be audited and likely refactored into Cinemachine-aware coordination or replaced.

FrameworkRouteCameraBinding / FrameworkActivityCameraBinding
  can remain lifecycle entry points, but should feed Cinemachine rig state rather than raw camera enable/disable.

PlayerViewCameraTargetBindingAdapter
  remains a technical adapter, not final product UX.

PlayerViewCameraActivationAdapter
  becomes legacy/diagnostic/compatibility path, not the preferred product path.
```

### Rejected direction

The following is no longer acceptable as final product direction:

```text
CameraComposer -> enable/disable explicit Unity Camera
```

That shape does not satisfy real camera authoring, target-driven gameplay cameras, transitions, blending, or future multiplayer camera needs.

## Ownership model to define

The next camera architecture must distinguish at least:

```text
RouteCamera
ActivityCamera
SinglePlayerFollowCamera
LocalPlayerCamera
SharedPlayerGroupCamera
SpectatorOrDebugCamera
```

The current `FrameworkCameraScope` and `FrameworkCameraRigRole` only represent DefaultFallback, Route, Activity, and RetainedActivity. That is insufficient for the product model.

## Single-player policy

The first implementation target should be single-player follow/look-at camera because it matches the current FIRSTGAME validation path.

Expected first real flow:

```text
PlayerComposer
  materializes or exposes CameraTarget / LookAtTarget

CameraComposer
  references PlayerComposer explicitly
  resolves tracking/look-at targets through typed source policy
  materializes Cinemachine rig/bindings
  applies priority/blend-ready configuration
```

No lookup by name or hierarchy path should be used as functional identity.

## Multiplayer policy

Local multiplayer must not be inferred from single-player behavior.

Future multiplayer camera modes require separate contracts:

```text
per-local-player camera
split-screen camera ownership
shared group target camera
spectator/debug camera
player join/leave camera rebinding
```

These should be planned after the single-player Cinemachine path is stable.

## Migration guidance

If `CameraComposer MVP-A` was applied locally, treat it as experimental and superseded.

Recommended handling:

```text
Do not validate Camera Product Surface against MVP-A.
Do not create QA smokes for MVP-A.
Do not prove FIRSTGAME on MVP-A.
Use MVP-A only as temporary reference for editor reporting/idempotency patterns if useful.
```

## Acceptance criteria for the restructured camera lane

Technical acceptance:

```text
Cinemachine dependency declared
assemblies compile
no runtime Editor dependency
no Camera.main fallback
no service locator / singleton manager
explicit target-source failure diagnostics
idempotent Apply/Rebuild
clear logs and debug evidence
```

Product acceptance:

```text
designer can create a camera rig from product surface
designer can link it to a PlayerComposer explicitly
Apply/Rebuild materializes Cinemachine rig/bindings
Inspector shows designer intent first
Advanced/Debug shows technical evidence
FIRSTGAME camera consumes real PlayerPrototype targets
single-player path is clear and reusable
multiplayer is not accidentally implied
```

## Next cut

Recommended next cut:

```text
C1 — Camera Cinemachine Rebuild Plan
```

Then:

```text
C2 — Cinemachine package dependency and assembly boundary
C3 — Camera ownership / target-source contracts
C4 — Cinemachine rig materialization utility
C5 — CameraComposer SinglePlayer MVP
C6 — FIRSTGAME proof
C7 — QA technical regressions
```
