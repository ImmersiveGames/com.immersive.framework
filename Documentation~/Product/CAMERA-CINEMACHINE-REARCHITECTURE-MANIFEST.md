> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# CAMERA-CINEMACHINE-REARCHITECTURE-MANIFEST

Status: documentation / architecture direction
Package: `com.immersive.framework`

## Objective

Freeze the decision that the official Camera Product Surface must be Cinemachine-first and that the previous pure-camera MVP direction is superseded.

## Files created

```text
Packages/com.immersive.framework/Documentation~/ADRs/Product/ADR-PROD-0005-camera-product-surface-cinemachine.md
Packages/com.immersive.framework/Documentation~/Product/Camera-Cinemachine-Rearchitecture-Plan.md
Packages/com.immersive.framework/Documentation~/Product/CAMERA-CINEMACHINE-REARCHITECTURE-MANIFEST.md
```

## Files changed

```text
none
```

## Files removed

```text
none
```

## Product surface affected

```text
Camera Recipe / Camera Composer
```

## Decision

Camera Product Surface is no longer allowed to evolve as a pure `UnityEngine.Camera.enabled` workflow.

The official direction is:

```text
Cinemachine-first camera authoring
explicit target-source contracts
single-player first implementation
multiplayer planned separately
```

## Superseded work

```text
CameraComposer MVP-A — Rig/Bindings Foundation
```

MVP-A may be used as reference for editor/idempotency patterns only. It should not be treated as final Camera Product Surface and should not receive QA/FIRSTGAME proof.

## Out of scope

```text
no C# code
no package.json dependency change
no asmdef change
no FIRSTGAME change
no QAFramework change
no Cinemachine API implementation
```

## Next expected cut

```text
C2 — Cinemachine package dependency and assembly boundary
```

## Acceptance criteria

```text
docs compile as markdown
ADR decision is explicit
Cinemachine mandatory direction is recorded
single-player and multiplayer camera paths are separated
legacy pure-camera activation is demoted from product path
```

## Suggested commit message

```text
Docs: reframe camera product surface around Cinemachine
```
