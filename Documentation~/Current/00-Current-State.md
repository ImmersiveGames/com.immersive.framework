# 00 — Current State

Status: **canonical Camera state after C9N; QA Player and FIRSTGAME validation remain open**
Decision: `ADR-PROD-0006-camera-requests-output-contexts.md`

## Product and authoring surface

```text
CameraRigRecipe
  -> reusable Cinemachine presentation intent
CameraRigComposer
  -> designer-facing rig instance; Validate / Apply / Rebuild
  -> materializes one virtual CinemachineCamera only
CameraOutputSessionBinding
  -> explicit Unity Camera + CinemachineBrain physical output
```

`CameraRigComposer` does not create a Unity Camera, `CinemachineBrain`,
`AudioListener`, or `CameraOutputSessionBinding`. The Composer Inspector exposes
designer intent first and technical materialization plus diagnostics under
Advanced/Debug.

## Runtime authority

For one explicit `CameraOutputId`, `CameraOutputSessionBinding` creates one
scoped chain:

```text
CameraOutputContext -> CameraOutputRigApplicator -> CameraOutputSession
```

`CameraOutputContext` is the sole winner-selection authority. It admits typed
`CameraRequest` values, rejects invalid/output-mismatched/ambiguous requests,
selects the highest precedence request (ordinal tie-breaker for equal
precedence), captures a snapshot, and restores the next winner on release.
`CameraOutputSession` applies each accepted mutation transactionally; failed
presentation is rolled back explicitly. `CameraOutputRigApplicator` only
enables/disables the winning materialized `CinemachineCamera`; it does not
select winners or toggle the physical Unity Camera.

## Request sources, precedence and lifetime

| Source | Boundary | Owner / lifetime |
|---|---|---|
| Route | `RouteCameraRequestBinding` on `RouteContentBehaviour` callbacks | `Route` / `Route` |
| Activity | `ActivityCameraRequestBinding` on `ActivityContentBehaviour` callbacks | `Activity` / `Activity` |
| Local Player | `LocalPlayerCameraRequestBinding.SetLocalPlayerEligible(bool)` | `LocalPlayer` / `LocalPlayerEligibility` |

All requests require explicit output, request id, scope/owner, rig, target,
precedence and deterministic tie-breaker. Route and Activity bind the assigned
lifecycle asset by reference; names are diagnostic-only. A Local Player uses
`PlayerComposer`'s explicit Player Slot, CameraTarget and LookAtTarget; it
never discovers locality through a name, tag, `PlayerInput` index or lookup.

Higher precedence wins; equal precedence requires distinct tie-breakers.
Release is idempotent and restores the next admitted request. No owner controls
`CinemachineCamera` priority as independent arbitration.

## Diagnostics and failure policy

`CameraOutputSessionBinding` and all three request bindings retain Inspector
status and diagnostic text and emit `[FRAMEWORK_CAMERA]` diagnostics when
enabled. Missing configuration, lifecycle mismatch, arbitration ambiguity and
transaction rollback fail explicitly. There is no `Camera.main` fallback,
global camera manager, service locator or automatic output discovery.

## Evidence state

QAFramework contains C9C–C9G technical fixtures, the C9I canonical
Route/Activity fixture, the C9L Route → Local Player → Activity fixture, and
the post-C9N teardown fixture. C9I records eight closed lifecycle cases. The
teardown evidence records Activity release before content disable, Route
release, and QA Hub return with `blockingIssues='0'`. C9L passed its ten-case
Player arbitration smoke, including the explicit invalid Player block,
precedence/restoration chain and Hub return with `blockingIssues='0'`. A hygiene
rerun remains pending only to confirm removal of unrelated C9O/C9M logs.

FIRSTGAME serializes one `camera.output.main` output, three virtual rigs, and
Route, Activity and Local Player bindings in `FG_Gameplay.unity`. Its C9M
validation report is still unfilled: this is verified configuration, not a
recorded FIRSTGAME Play Mode closure after C9N.

## Current limitations

- No multi-output registry, output creation wizard, split-screen setup or
  online local/remote-player authority exists.
- `eligibleOnEnable` is a single-player convenience policy; real eligibility
  authority must call `SetLocalPlayerEligible`.
- C9M FIRSTGAME manual validation must be recorded after C9N. See
  `Camera-Delivery-Reconciliation.md`.
