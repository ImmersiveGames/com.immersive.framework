# C8A — Route/Activity Camera Cinemachine Rewrite Boundary Manifest

Status: C8A cleanup and demotion boundary
Package: `com.immersive.framework`

## Decision

`CameraComposer` is the official Camera Product Surface for authoring gameplay cameras. It is Cinemachine-first, uses explicit `PlayerComposer` target sources, and owns the Apply/Rebuild materialization path.

Route/Activity camera remains a technical lifecycle/binding integration surface for compatibility. It is not the primary designer workflow until a later C8B rewrite replaces its ownership and materialization model.

## Classification

| Item | Classification | Decision |
| --- | --- | --- |
| `CameraComposer` / `CameraRecipe` | `KEEP_OFFICIAL` | Primary authoring surface |
| `FrameworkCinemachineRigApplier` | `KEEP_OFFICIAL` | Typed Cinemachine adapter used by technical integrations |
| `FrameworkCameraDirector` | `KEEP_LEGACY_COMPATIBILITY` | Preserve for Route/Activity lifecycle regressions; not the product authoring path |
| `FrameworkRouteCameraBinding` | `KEEP_DIAGNOSTIC` | Preserve explicit Route lifecycle binding while C8B is deferred |
| `FrameworkActivityCameraBinding` | `KEEP_DIAGNOSTIC` | Preserve explicit Activity lifecycle binding while C8B is deferred |
| `FrameworkCameraAnchorHost` | `KEEP_LEGACY_COMPATIBILITY` | Preserve typed anchor evidence for existing bindings and FIRSTGAME legacy support |
| `PlayerViewCameraTargetBindingAdapter` | `KEEP_DIAGNOSTIC` | Older PlayerView contract; not Camera Product Surface |
| `PlayerViewCameraActivationAdapter` | `KEEP_LEGACY_COMPATIBILITY` | Retain only for compatibility regression; `Camera.enabled` is not official product behavior |
| Route/Activity QA scenes | `KEEP_DIAGNOSTIC` | Keep separate from Camera Product Surface QA |
| FIRSTGAME `FirstGameCameraComposerProof` | `KEEP_TEMPORARY_PROOF` | Accepted C6 consumer proof |
| FIRSTGAME `FirstGameCameraCutSetup` | `LEGACY_DIAGNOSTIC` | Preserve for legacy Route/Activity regression, demote in guidance |

## Explicit non-decisions

- No `CameraManager`, singleton, service locator or global runtime context was created.
- No `Camera.main` fallback or name/path identity lookup was introduced.
- No runtime reflection or Editor dependency was introduced.
- No Route/Activity runtime rewrite is included in C8A.

## C8B deferred work

1. Define the lifecycle ownership contract between Route/Activity and `CameraComposer`.
2. Replace director-selected active rig state with typed Cinemachine-aware lifecycle bindings.
3. Prove transitions, Activity retention and route fallback without raw `Camera.enabled` semantics.
4. Migrate QA and FIRSTGAME only after C8B has an explicit compatibility plan.

## Acceptance boundary

The package remains compile-compatible for existing technical integrations while its docs make the product boundary explicit: designers use `CameraComposer`; Route/Activity bindings are lifecycle diagnostics/compatibility until C8B.

## Suggested commit message

`Framework: demote legacy Route Activity camera path`
