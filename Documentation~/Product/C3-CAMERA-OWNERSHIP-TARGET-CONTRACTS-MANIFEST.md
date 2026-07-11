> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# C3 — Camera Ownership / Target Source Contracts Manifest

Status: runtime contracts delta
Date: 2026-07-09
Package: `com.immersive.framework`
Surface: `Camera Recipe / Camera Composer`

## Objective

Create the minimum camera product vocabulary before implementing `CameraComposer`.

This cut formalizes camera mode, ownership scope, target-source kind, resolved target output and failure/result primitives. It intentionally does not materialize Cinemachine rigs yet.

## Files created

```text
Runtime/Camera/Product/CameraMode.cs
Runtime/Camera/Product/CameraMode.cs.meta
Runtime/Camera/Product/CameraOwnershipScope.cs
Runtime/Camera/Product/CameraOwnershipScope.cs.meta
Runtime/Camera/Product/CameraTargetSourceKind.cs
Runtime/Camera/Product/CameraTargetSourceKind.cs.meta
Runtime/Camera/Product/CameraTargetRequirement.cs
Runtime/Camera/Product/CameraTargetRequirement.cs.meta
Runtime/Camera/Product/CameraOperationStatus.cs
Runtime/Camera/Product/CameraOperationStatus.cs.meta
Runtime/Camera/Product/CameraIssueSeverity.cs
Runtime/Camera/Product/CameraIssueSeverity.cs.meta
Runtime/Camera/Product/CameraIssue.cs
Runtime/Camera/Product/CameraIssue.cs.meta
Runtime/Camera/Product/CameraTargetSourceDescriptor.cs
Runtime/Camera/Product/CameraTargetSourceDescriptor.cs.meta
Runtime/Camera/Product/CameraResolvedTargets.cs
Runtime/Camera/Product/CameraResolvedTargets.cs.meta
Runtime/Camera/Product/CameraTargetResolveResult.cs
Runtime/Camera/Product/CameraTargetResolveResult.cs.meta
Runtime/Camera/Product/ICameraTargetSource.cs
Runtime/Camera/Product/ICameraTargetSource.cs.meta
Runtime/Camera/Product/CameraProductIntent.cs
Runtime/Camera/Product/CameraProductIntent.cs.meta
Documentation~/Product/C3-CAMERA-OWNERSHIP-TARGET-CONTRACTS-MANIFEST.md
Documentation~/Product/C3-CAMERA-OWNERSHIP-TARGET-CONTRACTS-MANIFEST.md.meta
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

## Scope

```text
CameraMode
CameraOwnershipScope
CameraTargetSourceKind
CameraTargetRequirement
CameraResolvedTargets
CameraTargetSourceDescriptor
CameraTargetResolveResult
CameraIssue
ICameraTargetSource
CameraProductIntent
```

## Out of scope

```text
CameraComposer
CameraRecipe ScriptableObject
Cinemachine rig creation
Cinemachine Brain creation
Cinemachine Camera creation
Route/Activity camera rewrite
FIRSTGAME
QAFramework
asmdef changes
package.json changes
runtime camera authority
CameraManager
```

## Decisions

- `SinglePlayerFollowCamera` is a distinct mode, not an implicit Route or Activity camera.
- `LocalPlayerCamera` and `SharedPlayerGroupCamera` are named now but not implemented.
- `ExplicitTransform` and `PlayerComposer` are the first supported target-source shapes for the upcoming MVP.
- `PlayerSlot`, `Route`, `Activity` and `PlayerGroup` are contract names for later work only.
- Target resolution returns explicit `CameraResolvedTargets` and a diagnostic result.
- Required missing targets block the operation.
- Optional missing look-at target succeeds with warning.
- Contracts do not use `Camera.main`, hierarchy path or object name fallback.

## Flow of use expected after this cut

No new Unity authoring flow is introduced yet.

The intended next flow is:

```text
CameraComposer
  owns CameraProductIntent
  references PlayerComposer explicitly
  resolves CameraResolvedTargets through explicit source policy
  passes targets to Cinemachine materialization in C4/C5
```

## Technical acceptance criteria

```text
Unity compiles.
No runtime Editor dependency is introduced.
No Cinemachine API is required by these contracts.
No singleton, service locator or CameraManager is introduced.
No Camera.main fallback is introduced.
No name/path lookup is introduced as functional identity.
Existing Route/Activity camera classes remain untouched.
Existing PlayerComposer behavior remains untouched.
```

## Product acceptance criteria

```text
Camera modes are named in product language.
Single-player and multiplayer camera modes are separated.
Target source is explicit.
Future CameraComposer can report resolved follow/look-at targets.
Future diagnostics can distinguish warnings from blocking issues.
```

## Architectural gain

This cut prevents `CameraComposer` from being implemented as another setup helper. It creates a small product contract layer that later authoring, materialization and QA can depend on.

## Usability gain

The designer-facing camera surface can now be built around clear language: camera mode, ownership, target source, follow target, look-at target and priority.

## Next expected cut

```text
C4 — Cinemachine rig materialization utility
```

## Suggested commit message

```text
Runtime: add camera ownership and target source contracts
```
