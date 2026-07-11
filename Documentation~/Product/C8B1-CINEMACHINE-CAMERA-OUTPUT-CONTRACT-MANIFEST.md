> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# C8B1 — Cinemachine Camera Output Contract Manifest

Status: ready for Unity validation after Foundation normalization cut
Package: `com.immersive.framework`

## Objective

Add a small technical contract for explicit materialized Cinemachine camera output without changing Route/Activity lifecycle runtime.

## Scope

- Add `CinemachineCameraOutput` as explicit technical output evidence.
- Add `CinemachineCameraOutputDiagnostic` for structured blocked/skipped/succeeded diagnostics.
- Add `FrameworkCinemachineOutputApplier` to validate/apply output priority and targets.
- Add `CameraComposerCinemachineOutputExtensions` as a bridge from the official Product Surface to the technical output.

## Out of scope

- No Route/Activity runtime rewrite.
- No `FrameworkCameraDirector` rewrite.
- No `FrameworkRouteCameraBinding` or `FrameworkActivityCameraBinding` rewrite.
- No QA scene changes.
- No FIRSTGAME changes.
- No legacy removal.
- No `CameraManager`, singleton, service locator, `Camera.main` or global lookup.

## Files created

```text
Runtime/Camera/Cinemachine/CinemachineCameraOutput.cs
Runtime/Camera/Cinemachine/CinemachineCameraOutputDiagnostic.cs
Runtime/Camera/Cinemachine/FrameworkCinemachineOutputApplier.cs
Runtime/Camera/Cinemachine/CameraComposerCinemachineOutputExtensions.cs
Documentation~/Product/C8B1-CINEMACHINE-CAMERA-OUTPUT-CONTRACT-MANIFEST.md
```

## Files altered

```text
Runtime/Camera/Cinemachine/CinemachineCameraOutput.cs
Runtime/Camera/Cinemachine/CinemachineCameraOutputDiagnostic.cs
Runtime/Camera/Cinemachine/CameraComposerCinemachineOutputExtensions.cs
Runtime/Camera/Cinemachine/Immersive.Framework.Camera.Cinemachine.asmdef
Runtime/Common/FrameworkStringExtensions.cs
```

The C8B1 contracts consume `Immersive.Foundation.Common.FoundationStringExtensions` explicitly. No C8B1 contract owns a private normalizer. The pre-existing `FrameworkStringExtensions` facade remains temporarily for older Framework consumers and is outside this bounded migration.

Pre-existing domain-local `Normalize(...)` wrappers remain outside this cut in lifecycle, transition, loading, content, snapshot, pause, save and related modules. They require a separate bounded migration because their fallback semantics and ownership must be reviewed per module.

## Files removed

```text
none
```

## Public APIs added

```text
Immersive.Framework.Camera.Cinemachine.CinemachineCameraOutput
Immersive.Framework.Camera.Cinemachine.CinemachineCameraOutputDiagnostic
Immersive.Framework.Camera.Cinemachine.CinemachineCameraOutputDiagnosticStatus
Immersive.Framework.Camera.Cinemachine.FrameworkCinemachineOutputApplier
Immersive.Framework.Camera.Cinemachine.CameraComposerCinemachineOutputExtensions.TryCreateCinemachineOutput(...)
```

## Hotfix notes

The first C8B1 draft used an unavailable Framework-local normalization path. The shared primitive now lives in Foundation, and the Cinemachine assembly references Foundation directly. No local C8B1 normalizer remains.

## Product surface affected

`CameraComposer` remains the designer-facing Product Surface. The new output contract is technical evidence for later Route/Activity integration and does not create lifecycle authority.

## Expected technical flow

```text
CameraComposer Apply/Rebuild materializes Cinemachine rig.
CameraComposerCinemachineOutputExtensions creates explicit output evidence.
FrameworkCinemachineOutputApplier validates/applies priority and targets.
Future Route/Activity cuts consume the output explicitly.
```

## Technical acceptance criteria

```text
Package compiles.
Runtime does not depend on Editor.
No Camera.main.
No FindObjectOfType.
No singleton/manager.
No fallback silently creates camera output.
C7 QA regression remains PASS.
C6 FIRSTGAME CameraComposer proof remains PASS.
```

## Product acceptance criteria

```text
Designer still uses CameraComposer.
Route/Activity remains technical integration.
Legacy camera path remains diagnostic/compatibility.
No new Product Surface competes with CameraComposer.
```

## Architectural gain

The framework now has an explicit technical output object that later Route/Activity camera integration can consume without binding directly to the authoring component or reviving raw camera activation semantics.

## Usability gain

No visible workflow changes yet. The benefit is reduced risk for C8B2/C8B3 because the runtime integration can target explicit output evidence instead of guessing from scene state.

## Suggested commit message

```text
Framework: add Cinemachine camera output contract
```
