> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# C1 — Camera Cinemachine Architecture Consolidation Manifest

Status: documentation / architecture consolidation
Package: `com.immersive.framework`
Surface: `Camera Recipe / Camera Composer`
Date: 2026-07-09

## Objective

Consolidate the Camera Product Surface direction before implementation.

The official Camera Product Surface is now Cinemachine-first, target-driven and composer-authored. Pure `UnityEngine.Camera.enabled` activation is demoted to legacy/diagnostic/compatibility evidence.

## Files created

```text
Packages/com.immersive.framework/Documentation~/ADRs/Product/ADR-PROD-0005-camera-product-surface-cinemachine.md
Packages/com.immersive.framework/Documentation~/Product/Camera-Architecture-Audit.md
Packages/com.immersive.framework/Documentation~/Product/CAMERA-ARCHITECTURE-AUDIT-MANIFEST.md
Packages/com.immersive.framework/Documentation~/Product/Camera-Cinemachine-Rearchitecture-Plan.md
Packages/com.immersive.framework/Documentation~/Product/CAMERA-CINEMACHINE-REARCHITECTURE-MANIFEST.md
Packages/com.immersive.framework/Documentation~/Product/Camera-Cinemachine-Architecture-Consolidation.md
Packages/com.immersive.framework/Documentation~/Product/C1-CAMERA-CINEMACHINE-ARCHITECTURE-CONSOLIDATION-MANIFEST.md
```

## Files changed

```text
none
```

## Files removed

```text
none
```

## Superseded work

```text
CameraComposer MVP-A — Rig/Bindings Foundation
camera_composer_mvp_delta.zip, if present locally
```

Superseded work must not be used as final Camera Product Surface, must not receive QA proof and must not receive FIRSTGAME proof.

## Product surface affected

```text
Camera Recipe / Camera Composer
```

## Flow of use expected after this cut

No Unity authoring flow is introduced in this cut.

This cut only prepares the official direction for later product implementation:

```text
PlayerComposer exposes CameraTarget / LookAtTarget.
CameraComposer will explicitly consume PlayerComposer targets.
Cinemachine rig materialization will execute the camera presentation.
```

## Out of scope

```text
C# runtime code
C# editor code
package.json dependency
asmdef changes
Cinemachine API implementation
CameraComposer implementation
QAFramework changes
FIRSTGAME changes
```

## Technical acceptance criteria

```text
No runtime code added.
No editor code added.
No asmdef or package dependency changed.
Cinemachine mandatory direction documented.
Legacy Camera.enabled path demoted from product path.
No singleton / service locator / Camera.main direction introduced.
```

## Product acceptance criteria

```text
Camera product surface target shape is clear.
Single-player and multiplayer camera paths are separated.
CameraComposer MVP-A is explicitly superseded.
First implementation target is SinglePlayerFollowCamera.
PlayerComposer anchors are the first official target source.
```

## Architectural gain

Prevents the framework from freezing an incomplete CameraComposer API around technical bindings or raw camera activation.

## Usability gain

Keeps the intended user flow centered on authorable camera intent, explicit player targets and Cinemachine rig materialization.

## Next expected cut

```text
C2 — Cinemachine package dependency and assembly boundary
```

## Suggested commit message

```text
Docs: consolidate Cinemachine camera product architecture
```
