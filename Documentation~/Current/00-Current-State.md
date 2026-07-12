# 00 — Current State

Status: **canonical after C9 Camera closure and FIRSTGAME runtime/visual proof**  
Last reconciled: **2026-07-12**  
Decision: `ADR-PROD-0006-camera-requests-output-contexts.md`

For the active execution block, read `05-Execution-Status.md`.

## Camera product and authoring surface

```text
CameraRigRecipe
  reusable Cinemachine presentation intent

CameraRigComposer
  designer-facing rig instance
  Validate / Apply / Rebuild
  materializes one virtual CinemachineCamera and its local pipeline

CameraOutputSessionBinding
  explicit physical Unity Camera + CinemachineBrain output
  owns one scoped CameraOutputSession
```

`CameraRigComposer` does not create or own the physical output. It does not
select the active rig and does not operate gameplay.

`Follow Offset` is designer-authored through `CameraRigRecipe` and
`CameraRigComposer`. Apply/Rebuild writes it idempotently to the local
`CinemachineFollow`.

## Runtime authority

For one explicit `CameraOutputId`, the physical output creates:

```text
CameraOutputContext
  -> CameraOutputRigApplicator
  -> CameraOutputSession
```

`CameraOutputContext` is the sole winner-selection authority. It admits typed
requests, rejects invalid or ambiguous requests, selects the highest-precedence
request, captures diagnostics and restores the next valid request on release.

`CameraOutputSession` coordinates arbitration and presentation transactionally.
`CameraOutputRigApplicator` presents the selected materialized
`CinemachineCamera`; it does not choose the winner and does not toggle the
physical Unity Camera as policy.

## Persistent output and request sources

The canonical single-player output is session-owned and authored in
`UIGlobal`.

Framework Core injects that output into Route, Activity and Local Player
consumers. No consumer serializes a cross-scene output reference and no runtime
lookup by name, `Camera.main`, singleton or service locator is used.

| Source | Product/runtime boundary | Lifetime behavior | Default precedence |
|---|---|---|---:|
| Local Player | `LocalPlayerCameraRequestBinding` | publishes while explicitly eligible | 50 |
| Activity | `ActivityCameraOverrideBinding` | lifecycle entry makes it available; explicit request/release | 100 |
| Route | `RouteCameraOverrideBinding` | lifecycle entry makes it available; explicit request/release | 200 |
| Session | `SessionCameraOverrideBinding` in `UIGlobal` | transition-scoped explicit request/release | 300 |

Route and Activity lifecycle entry do **not** publish a camera request.
`RequestOverride()` publishes the temporary override and `ReleaseOverride()`
restores the next valid request.

The Session override is persistent but is not the normal gameplay winner. The
transition orchestrator requests it only while the transition surface covers
the scene change and releases it before destination content is revealed.

## Diagnostics and failure policy

The output binding and all request/override bindings retain Inspector status and
diagnostic text and emit `[FRAMEWORK_CAMERA]` evidence when diagnostics are
enabled.

Required invalid configuration fails explicitly. Release is idempotent.
Presentation failure is diagnostic and transactional. There is no silent
fallback, global camera manager, owner-controlled Cinemachine priority
competition or automatic physical output discovery.

## Closed evidence

### Package

- Typed request/output contracts are implemented.
- Single-output arbitration and restoration are implemented.
- Winner application is transactional.
- Route, Activity, Local Player and Session sources are implemented.
- Session-scoped output injection is implemented.
- Follow framing is designer-authored and materialized idempotently.
- Superseded camera Director, activation and automatic lifecycle-publication
  paths remain removed.

### QAFramework

- C9I lifecycle evidence closed eight cases.
- C9L Player arbitration evidence closed ten cases.
- C9O proved Activity teardown before Route unload.
- C9Q Follow Pipeline passed four cases:
  `camera-materialized`, `follow-pipeline-materialized`, `target-assigned`,
  `idempotent`.
- C9R Camera Override Authority passed eleven cases, including explicit
  request/release, precedence, restoration and lifecycle cleanup.

### FIRSTGAME

FIRSTGAME now proves the real consumer shape:

```text
one persistent camera.output.main in FG_UIGlobal
one physical Unity Camera
one CinemachineBrain
one SessionCameraOverrideBinding
Route / Activity / Local Player consumers injected at runtime
Player 50 < Activity 100 < Route 200 < Session 300
Session requested and released during Route transition
explicit Activity, Route and Session override/release restoration
```

The final manual visual inspection was accepted: the Player camera follows the
Player, the authored framings are distinguishable, and override/release changes
the effective camera as expected.

## Current limitations

- The supported product baseline is one explicit output.
- Multi-output authoring, split-screen setup and online local/remote eligibility
  remain future product work.
- `eligibleOnEnable` is a single-player convenience; a real locality authority
  must call `SetLocalPlayerEligible`.
- Session camera presentation is transition-scoped; it is not a gameplay camera
  manager.

## Current execution boundary

Camera C9 is closed.

The selected continuation is G1, redefined as a **consumer Route loop proof**.
The framework does not own objectives, win conditions, combat, mission state or
other gameplay rules.

A valid framework loop may be:

```text
Bootstrap
-> Menu Route
-> Gameplay Route
-> Ending Route or Menu Route
-> controlled return/re-entry
```

Gameplay objectives, interactions and resettable game state may be added by
FIRSTGAME, but they are not mandatory framework acceptance criteria.
