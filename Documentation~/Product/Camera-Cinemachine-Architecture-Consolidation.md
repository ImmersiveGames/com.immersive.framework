# Camera Cinemachine Architecture Consolidation

Status: C1 architecture consolidation
Package: `com.immersive.framework`
Surface: `Camera Recipe / Camera Composer`
Date: 2026-07-09

## Objective

Consolidate the Camera Product Surface re-architecture before any implementation cut.

This closes the product direction that official camera authoring in the framework must be:

```text
Cinemachine-first
explicitly target-driven
composer-authored
idempotently materialized
diagnostic-first for failures
single-player first, multiplayer planned separately
```

## Decision summary

The official Camera Product Surface is not a pure `UnityEngine.Camera.enabled` workflow.

The package must evolve toward:

```text
Camera Recipe / Profile / Template
  reusable camera intent

Camera Composer / Authoring Component
  designer-first editing surface

Cinemachine Rig Materialization
  Cinemachine Brain / Cinemachine Camera / Follow / LookAt / Priority

Camera Target Source Contracts
  explicit sources such as PlayerComposer or ExplicitTransform

Scoped Runtime Authority
  only when runtime camera coordination has clear ownership and lifetime

Diagnostics
  validation, reports, logs, Advanced/Debug evidence, QA smokes
```

## Canonical supporting documents

```text
Documentation~/ADRs/Product/ADR-PROD-0005-camera-product-surface-cinemachine.md
Documentation~/Product/Camera-Architecture-Audit.md
Documentation~/Product/Camera-Cinemachine-Rearchitecture-Plan.md
```

## Current camera lanes classification

### Preserved / candidate for reuse

```text
PlayerComposer CameraTarget / LookAtTarget
  Official first source for SinglePlayerFollowCamera.

FrameworkCameraAnchorHost
  May remain as anchor evidence or target-provider evidence if compatible.

FrameworkRouteCameraBinding / FrameworkActivityCameraBinding
  May remain lifecycle entry points, but must feed Cinemachine-aware camera state instead of raw camera enable/disable.

FrameworkCameraDirector
  Must be audited. It can be refactored into Cinemachine-aware coordination or replaced if the product model requires it.
```

### Demoted / legacy technical path

```text
PlayerViewCameraTargetBindingAdapter
  Technical adapter, not final product UX.

PlayerViewCameraActivationAdapter
  Legacy/diagnostic/compatibility path. It must not define the official Camera Product Surface.

Pure UnityEngine.Camera enabled/disabled flow
  Diagnostic evidence only, not final camera authoring.
```

### Superseded

```text
CameraComposer MVP-A — Rig/Bindings Foundation
```

MVP-A may be referenced only for editor reporting or idempotency patterns. It must not receive QA/FIRSTGAME proof and must not be treated as the official Camera Product Surface.

## Required minimum contracts before CameraComposer

The next implementation path must define small runtime contracts before adding the authoring surface:

```text
CameraMode
CameraOwnershipScope
CameraTargetSourceKind
CameraTargetSourceDescriptor
CameraResolvedTargets
CameraTargetResolveResult
CameraApplyIssue / CameraApplyResult
```

The first supported source kinds should be:

```text
ExplicitTransform
PlayerComposer
```

Future source kinds should remain planned, not implemented implicitly:

```text
PlayerSlot
Route
Activity
PlayerGroup
```

## First implementation target

The first product implementation should be:

```text
SinglePlayerFollowCamera
```

Expected usage flow:

```text
1. Designer creates or selects a Camera Rig.
2. Designer adds CameraComposer.
3. Designer selects SinglePlayerFollowCamera.
4. Designer links a PlayerComposer explicitly.
5. CameraComposer resolves CameraTarget / LookAtTarget from PlayerComposer.
6. Apply/Rebuild creates or repairs the Cinemachine rig.
7. Advanced/Debug shows target, rig and priority evidence.
```

## Explicitly rejected behavior

```text
Camera.main fallback
FindObjectOfType fallback
hierarchy path/name as functional identity
global CameraManager singleton
service locator
silent fallback to arbitrary camera
implicit player creation
single-player MVP implying multiplayer support
```

## Next cuts

```text
C2 — Cinemachine package dependency and assembly boundary
C3 — Camera ownership / target-source contracts
C4 — Cinemachine rig materialization utility
C5 — CameraComposer SinglePlayer MVP
C6 — FIRSTGAME proof
C7 — QA technical coverage
C8 — Route/Activity Cinemachine rewrite
C9 — Multiplayer camera design
```

## Acceptance criteria for C1

```text
Cinemachine mandatory direction is explicit.
CameraComposer MVP-A is marked as superseded.
Pure UnityEngine.Camera activation is demoted from product path.
Single-player and multiplayer camera paths are separated.
CameraComposer implementation is intentionally deferred until contracts and Cinemachine boundary exist.
No runtime/editor code is introduced.
No asmdef or package dependency is changed in this cut.
```

## Suggested commit message

```text
Docs: consolidate Cinemachine camera product architecture
```
