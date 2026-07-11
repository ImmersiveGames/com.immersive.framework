> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# C8B4A — Route/Activity Cinemachine Output Bridge Manifest

Status: implementation cut; Unity compile/import/smoke validation pending.
Package: `com.immersive.framework`

## Objective

Connect Route/Activity camera bindings to the explicit `CinemachineCameraOutput` contract without replacing `FrameworkCameraDirector`.

## Scope

- Add `FrameworkCinemachineCameraOutputSource` with explicit serialized Cinemachine references.
- Apply and diagnose configured Route output on Route enter.
- Apply and diagnose configured Activity output on Activity enter when policy owns the Activity camera.
- Preserve existing Route/Activity binding and director behavior.
- Keep `CameraComposer` as the Product Surface and Cinemachine output as technical materialization.

## Out of scope

- No `FrameworkCameraDirector` rewrite or replacement.
- No complete Route/Activity precedence migration.
- No legacy removal, `GameObject.SetActive` removal or `Camera.enabled` migration.
- No scene, asset, QAFramework or FIRSTGAME changes.
- No output priority clear on Activity exit; this is deferred to C8B4B/C8B5 because prior priority ownership is not modeled yet.
- No CameraManager, singleton, service locator, `Camera.main` or global functional lookup.

## Files created

```text
Runtime/Camera/Cinemachine/FrameworkCinemachineCameraOutputSource.cs
Runtime/Camera/Cinemachine/FrameworkCinemachineCameraOutputSource.cs.meta
Runtime/Camera/Cinemachine/FrameworkRouteCameraBinding.cs
Runtime/Camera/Cinemachine/FrameworkRouteCameraBinding.cs.meta
Runtime/Camera/Cinemachine/FrameworkActivityCameraBinding.cs
Runtime/Camera/Cinemachine/FrameworkActivityCameraBinding.cs.meta
Documentation~/Product/C8B4A-ROUTE-ACTIVITY-CINEMACHINE-OUTPUT-BRIDGE-MANIFEST.md
```

## Files altered

The Route/Activity binding sources were relocated from `Runtime/Camera/Unity/` to the existing Cinemachine adapter assembly so they can consume the C8B1 output contract without creating an assembly cycle. Their Unity GUIDs were preserved.

## Files removed

```text
Runtime/Camera/Unity/FrameworkRouteCameraBinding.cs
Runtime/Camera/Unity/FrameworkRouteCameraBinding.cs.meta
Runtime/Camera/Unity/FrameworkActivityCameraBinding.cs
Runtime/Camera/Unity/FrameworkActivityCameraBinding.cs.meta
```

## Product surface affected

`CameraComposer` remains the designer-facing surface. Route/Activity bindings gain an optional advanced technical reference to `FrameworkCinemachineCameraOutputSource`; no competing authoring flow is introduced.

## Expected flow

```text
Route enter
  → legacy FrameworkCameraDirector route path remains active
  → optional explicit Route output source validates/applies Cinemachine output
  → applied/skipped/blocked diagnostic is logged

Activity enter
  → legacy FrameworkCameraDirector Activity path remains active
  → UseRoute records use-route and does not apply an Activity override
  → own Activity policy validates/applies optional explicit Cinemachine output
```

Activity exit still uses the existing director clear/retention behavior. The explicit output is not reset in this cut; C8B4B/C8B5 must prove and model that ownership before changing priority.

## Smoke técnico esperado

- C8B2 Cinemachine Output Applier Smoke: PASS.
- C8B3 Route Activity Cinemachine Output Binding Smoke: PASS.
- C7 Camera Product Surface Regression Smoke: PASS.
- Package compile/import: PASS after Unity validation.

## Technical acceptance criteria

- Explicit source exposes camera, brain, targets, priority, required, output ID and display name.
- Required missing camera/brain produces Blocked diagnostics; optional missing output produces Skipped diagnostics.
- Route and Activity logs expose contextual bridge codes.
- No global camera lookup, fallback selection or new lifecycle authority is introduced.
- Existing legacy bindings and `FrameworkCameraDirector` remain available.

## Product acceptance criteria

- `CameraComposer` remains the main workflow.
- Route/Activity remains a technical integration boundary.
- Designer does not receive a competing camera authoring flow.
- Legacy remains available as compatibility.
- The new path starts a Cinemachine-aware migration without breaking the real consumer.

## Architectural gain

Route/Activity now consume explicit technical output evidence through the Cinemachine adapter assembly instead of inferring cameras from scene state. Assembly ownership stays acyclic and lifecycle authority remains in the existing bindings/director.

## Usability gain

Advanced users can assign an explicit output source and receive actionable applied/skipped/blocked diagnostics while existing Product Surface setup remains unchanged.

## Explicit status

C8B4A does not remove legacy. C8B4A does not complete Route/Activity migration. C8B4A adds an explicit Cinemachine output bridge. C8B4B/C8B5 must prove real integration in QA before FIRSTGAME.

## Suggested commit message

```text
Framework: add Route Activity Cinemachine output bridge
```
