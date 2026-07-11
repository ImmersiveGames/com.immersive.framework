> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# C4 — Cinemachine Rig Materialization Utility Manifest

Status: implementation / editor tooling
Date: 2026-07-09
Package: `com.immersive.framework`
Surface: `Camera Recipe / Camera Composer`

## Objective

Create the first idempotent editor-only materialization utility for the official Cinemachine-first Camera Product Surface.

This cut prepares the technical layer that later `CameraComposer` will call from its Inspector `Apply/Rebuild` flow.

## Files created

```text
Packages/com.immersive.framework/Editor/Camera.meta
Packages/com.immersive.framework/Editor/Camera/Cinemachine.meta
Packages/com.immersive.framework/Editor/Camera/Cinemachine/CinemachineRigMaterializationRequest.cs
Packages/com.immersive.framework/Editor/Camera/Cinemachine/CinemachineRigMaterializationRequest.cs.meta
Packages/com.immersive.framework/Editor/Camera/Cinemachine/CinemachineRigMaterializationEvidence.cs
Packages/com.immersive.framework/Editor/Camera/Cinemachine/CinemachineRigMaterializationEvidence.cs.meta
Packages/com.immersive.framework/Editor/Camera/Cinemachine/CinemachineRigMaterializationReport.cs
Packages/com.immersive.framework/Editor/Camera/Cinemachine/CinemachineRigMaterializationReport.cs.meta
Packages/com.immersive.framework/Editor/Camera/Cinemachine/CinemachineRigMaterializer.cs
Packages/com.immersive.framework/Editor/Camera/Cinemachine/CinemachineRigMaterializer.cs.meta
Packages/com.immersive.framework/Documentation~/Product/C4-CINEMACHINE-RIG-MATERIALIZATION-UTILITY-MANIFEST.md
Packages/com.immersive.framework/Documentation~/Product/C4-CINEMACHINE-RIG-MATERIALIZATION-UTILITY-MANIFEST.md.meta
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

## Flow of use enabled later

This cut does not expose a designer-facing CameraComposer yet.

It creates the technical apply layer that a future Composer can call:

```text
CameraComposer Inspector
  -> resolve explicit targets
  -> CinemachineRigMaterializer.ApplyOrRebuild(request)
  -> report created/repaired/alreadyValid/skipped/blocked
  -> show evidence in Advanced/Debug
```

## Materialization behavior

The utility can:

```text
create or reuse a local Unity Camera under the supplied rig root
add or reuse CinemachineBrain on that Unity Camera
create or reuse a local CinemachineCamera under the supplied rig root
assign Follow target
assign LookAt target
assign Priority
return evidence and diagnostic breakdown
```

## Architectural constraints

```text
Editor-only implementation.
No runtime authority created.
No CameraManager.
No singleton.
No service locator.
No Camera.main fallback.
No global scene lookup.
No name/path functional identity.
Only the supplied rig root is searched for local technical evidence.
```

Object names are used only for created child labels, not as identity for resolving gameplay targets.

## Out of scope

```text
CameraComposer
CameraRecipe ScriptableObject
PlayerComposer target resolver
Route/Activity Cinemachine rewrite
FIRSTGAME integration
QAFramework smoke
multiplayer/split-screen
runtime CameraContext or CameraSession
```

## Technical acceptance criteria

This cut is PASS if:

```text
Unity compiles.
Editor assembly compiles against Unity.Cinemachine.
Repeated ApplyOrRebuild does not duplicate Unity Camera, CinemachineBrain or CinemachineCamera under the same rig root.
Missing required Follow target returns blocked report.
Missing required LookAt target returns blocked report when required.
Follow target is assigned explicitly.
LookAt target is assigned explicitly.
Priority is assigned explicitly.
No runtime Editor dependency is introduced.
No Camera.main usage is introduced.
```

## Product acceptance criteria

```text
CameraComposer can later use a single utility for Apply/Rebuild.
Future Inspector can expose clear debug evidence.
Designer-facing Camera flow remains pending until C5.
Technical binding is no longer raw Camera.enabled activation.
```

## Suggested smoke after applying

Create a temporary scene object manually or through a future local editor test:

```text
Camera Rig
  PlayerCameraTarget
  PlayerLookAtTarget
```

Call:

```text
CinemachineRigMaterializer.ApplyOrRebuild(new CinemachineRigMaterializationRequest
{
    RigRoot = cameraRig.transform,
    FollowTarget = playerCameraTarget,
    LookAtTarget = playerLookAtTarget,
    Priority = 10,
});
```

Expected first run:

```text
created > 0
blocked = 0
```

Expected second run:

```text
created = 0
blocked = 0
alreadyValid increases
```

## Next expected cut

```text
C5 — CameraComposer SinglePlayer MVP
```

## Suggested commit message

```text
Editor: add Cinemachine rig materialization utility
```
