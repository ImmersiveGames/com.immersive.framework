# C2 — Cinemachine package dependency and assembly boundary

Status: implementation delta
Package: `com.immersive.framework`
Surface: Camera Product Surface
Date: 2026-07-09

## Objective

Add Cinemachine as an official package dependency and open the minimal runtime/editor assembly boundary required by the Camera Product Surface rearchitecture.

This cut intentionally does not implement CameraComposer, CameraRecipe, rig materialization, FIRSTGAME integration, or QA smokes.

## Scope

```text
package.json dependency
runtime asmdef reference
editor asmdef reference
minimal compile-only Cinemachine boundary
```

## Out of scope

```text
CameraComposer
CameraRecipe
Cinemachine rig creation
Cinemachine rig repair
PlayerComposer target consumption
Route/Activity camera rewrite
FIRSTGAME integration
QAFramework smoke
multiplayer camera design
```

## Files changed

```text
Packages/com.immersive.framework/package.json
Packages/com.immersive.framework/Runtime/Immersive.Framework.Runtime.asmdef
Packages/com.immersive.framework/Editor/Immersive.Framework.Editor.asmdef
```

## Files created

```text
Packages/com.immersive.framework/Runtime/Camera/Cinemachine.meta
Packages/com.immersive.framework/Runtime/Camera/Cinemachine/FrameworkCinemachinePackageBoundary.cs
Packages/com.immersive.framework/Runtime/Camera/Cinemachine/FrameworkCinemachinePackageBoundary.cs.meta
Packages/com.immersive.framework/Documentation~/Product/C2-CINEMACHINE-PACKAGE-BOUNDARY-MANIFEST.md
Packages/com.immersive.framework/Documentation~/Product/C2-CINEMACHINE-PACKAGE-BOUNDARY-MANIFEST.md.meta
```

## Files removed

```text
none
```

## Product surface affected

```text
CameraRecipe / CameraComposer
```

## Expected usage flow

No user-facing Unity flow is added in this cut.

This cut only makes Cinemachine available to the package so the following cuts can safely introduce camera contracts, materialization, and authoring.

## Technical smoke expected

```text
Unity resolves com.unity.cinemachine
Immersive.Framework.Runtime compiles
Immersive.Framework.Editor compiles
FrameworkCinemachinePackageBoundary compiles against Unity.Cinemachine.CinemachineBrain
no runtime dependency on UnityEditor
no CameraComposer product code introduced
```

## Technical acceptance criteria

```text
Cinemachine dependency declared
Unity.Cinemachine assembly referenced by Runtime and Editor assemblies
minimal compile proof exists
no runtime Editor dependency
no singleton
no service locator
no Camera.main fallback
no target resolution behavior yet
```

## Product acceptance criteria

```text
Camera Product Surface is ready for Cinemachine-first implementation
no legacy pure Camera.enabled path is promoted
no designer-facing camera workflow is introduced prematurely
```

## Architectural gain

This cut converts the Cinemachine decision from documentation-only into a package boundary while avoiding premature CameraComposer implementation.

## Usability gain

Future CameraComposer work can now target the official Cinemachine path instead of continuing the legacy pure Unity camera activation flow.

## Risks

```text
Cinemachine package version may need adjustment if the consuming Unity project resolves a different registry version.
Unity.Cinemachine assembly reference must match the installed Cinemachine major version.
```

## Suggested commit message

```text
Build: add Cinemachine package boundary
```
