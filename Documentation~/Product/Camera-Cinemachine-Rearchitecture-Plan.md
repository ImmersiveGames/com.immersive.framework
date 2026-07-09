# Camera Cinemachine Re-architecture Plan

Status: Product/architecture plan
Package: `com.immersive.framework`
Surface: Camera Product Surface
Supersedes: `CameraComposer MVP-A — Rig/Bindings Foundation` as final product direction

## 1. Objective

Rebuild the camera lane as a real product surface instead of a technical collection of camera bindings.

The new direction is:

```text
Cinemachine-first Camera Product Surface
```

The framework should provide authorable camera intent, Cinemachine rig materialization, target-source contracts, runtime diagnostics, and clear future extension for single-player and multiplayer camera policies.

## 2. Problem

The current package has useful pieces, but they are fragmented:

```text
FrameworkCameraDirector
FrameworkRouteCameraBinding
FrameworkActivityCameraBinding
FrameworkCameraAnchorHost
PlayerViewCameraTargetBindingAdapter
PlayerViewCameraActivationAdapter
PlayerComposer CameraTarget / LookAtTarget
CameraComposer MVP-A
```

This does not yet answer the product question:

```text
How does a user create, configure, understand, and use a camera in a real game?
```

It also does not represent the difference between:

```text
single-player follow camera
route camera
activity camera
local multiplayer split-screen camera
shared group camera
spectator/debug camera
```

## 3. New product principle

Official camera authoring must be Cinemachine-based.

Pure `UnityEngine.Camera.enabled` activation is not enough because the framework needs:

```text
follow targets
look-at targets
priority
blending-ready camera changes
route/activity transitions
player-targeted cameras
future multiplayer camera modes
reusable camera recipes/templates
```

## 4. Target layers

### 4.1 Recipe / Profile / Template

Reusable camera intent.

Examples:

```text
Single Player Follow Camera Recipe
Top Down Player Camera Recipe
Route Overview Camera Recipe
Activity Cut Camera Recipe
Shared Group Camera Recipe (future)
Split Screen Local Player Camera Recipe (future)
```

### 4.2 Composer / Authoring Component

Designer-first component on the camera rig or scene root.

Responsibilities:

```text
select camera mode
select ownership/scope
select target source
link PlayerComposer when relevant
select or create Cinemachine rig
apply/rebuild materialization
show debug evidence
```

### 4.3 Materialization

Creates or repairs technical objects:

```text
Cinemachine Brain evidence
Cinemachine Camera / virtual camera equivalent
tracking target binding
look-at target binding
priority configuration
FrameworkCameraAnchorHost if still useful
_Framework/_Bindings technical evidence
```

### 4.4 Runtime Authority

Future typed runtime authority when coordination is required.

It must be:

```text
typed
scoped
explicit lifetime
not singleton
not service locator
not Camera.main lookup
```

### 4.5 Diagnostics

Diagnostics are required, but not the product UX.

Required evidence:

```text
resolved camera mode
resolved ownership/scope
resolved target source
resolved tracking target
resolved look-at target
Cinemachine rig object
priority/blend configuration
Apply/Rebuild counters
blocking issues
```

## 5. Ownership and mode taxonomy

The camera lane must define explicit modes before implementation expands.

Minimum taxonomy:

| Mode | Purpose | Initial status |
|---|---|---|
| `RouteCamera` | Camera owned by route presentation | Existing skeleton, needs Cinemachine rewrite |
| `ActivityCamera` | Camera owned by activity presentation | Existing skeleton, needs Cinemachine rewrite |
| `SinglePlayerFollowCamera` | Camera follows one authored player | First implementation target |
| `LocalPlayerCamera` | Per-local-player camera, split-screen capable | Future |
| `SharedPlayerGroupCamera` | One camera for group/multiple players | Future |
| `SpectatorOrDebugCamera` | Non-gameplay inspection/debug camera | Future |

## 6. Target source taxonomy

Camera target source must be explicit.

Minimum target sources:

| Source | Meaning | First implementation? |
|---|---|---|
| `ExplicitTransform` | Designer assigns tracking/look-at transforms directly | Yes |
| `PlayerComposer` | Camera consumes targets from a specific PlayerComposer | Yes |
| `PlayerSlot` | Camera resolves target by logical slot through runtime authority | Later |
| `Route` | Camera uses route-authored targets | Later |
| `Activity` | Camera uses activity-authored targets | Later |
| `PlayerGroup` | Camera computes/receives group target | Future |

For the first implementation, prefer:

```text
ExplicitTransform
PlayerComposer
```

Avoid runtime slot resolution until the proper runtime authority exists.

## 7. Required first implementation path

The first implementation should be:

```text
C5 — CameraComposer SinglePlayer MVP
```

Flow:

```text
1. Designer creates or selects CameraRig.
2. Designer adds CameraComposer.
3. Designer chooses SinglePlayerFollowCamera mode.
4. Designer links PlayerComposer explicitly.
5. CameraComposer resolves CameraTarget / LookAtTarget from PlayerComposer.
6. Apply/Rebuild creates or repairs Cinemachine camera rig.
7. Debug shows resolved player, tracking target, look-at target and Cinemachine evidence.
```

This implementation should prove the product relationship:

```text
PlayerComposer owns player intent and anchors.
CameraComposer consumes player anchors and owns camera intent.
Cinemachine rig executes camera presentation.
```

## 8. Current code interpretation

### Keep / reuse

```text
FrameworkCameraAnchorHost
  can stay as anchor evidence if useful.

FrameworkCameraDirector
  can be refactored into Cinemachine-aware camera coordinator.

FrameworkRouteCameraBinding / FrameworkActivityCameraBinding
  can remain lifecycle entry points after Cinemachine rewrite.

PlayerComposer cameraTarget/lookAtTarget
  should feed CameraComposer via explicit PlayerComposer reference.
```

### Supersede / demote

```text
PlayerViewCameraActivationAdapter
  pure Camera.enabled activation; not product path.

CameraComposer MVP-A
  useful editor/idempotency reference, but not final product architecture.

Pure Unity Camera rig creation
  not enough for official camera product.
```

### Missing

```text
Cinemachine dependency
Cinemachine assembly references
Camera ownership taxonomy
Camera target-source contracts
PlayerComposer target consumption API
single-player camera recipe/template
multiplayer camera plan
FIRSTGAME proof with real PlayerPrototype camera
```

## 9. Concrete cut sequence

### C1 — Camera Cinemachine Rebuild Plan

Documentation/ADR cut. Accepts Cinemachine as mandatory and freezes the new direction.

### C2 — Cinemachine package dependency and assembly boundary

Package cut.

Scope:

```text
package.json dependency
asmdef references
minimal compile-only Cinemachine boundary
no product implementation yet
```

Acceptance:

```text
Unity compiles with Cinemachine required
runtime does not depend on Editor
no FIRSTGAME changes
```

### C3 — Camera ownership / target-source contracts

Runtime contracts cut.

Scope:

```text
CameraOwnershipScope
CameraMode
CameraTargetSourceKind
CameraTargetSourceDescriptor
CameraResolvedTargets
failure/result primitives
```

No Cinemachine rig generation yet.

### C4 — Cinemachine rig materialization utility

Editor tooling cut.

Scope:

```text
create/repair Cinemachine camera object
assign follow/look-at targets through Cinemachine API
configure priority
report created/repaired/alreadyValid/skipped/blocked
```

### C5 — CameraComposer SinglePlayer MVP

Product surface cut.

Scope:

```text
CameraRecipe
CameraComposer
Apply Recipe Defaults
Validate
Apply/Rebuild
PlayerComposer explicit source
Cinemachine rig materialization
Debug evidence
```

### C6 — FIRSTGAME proof

Integration proof.

Scope:

```text
FIRSTGAME camera consumes PlayerPrototype targets
old local helper/setup is no longer the main flow
real gameplay camera remains usable
```

### C7 — QA technical coverage

Regression cut.

Scope:

```text
single-player camera apply/rebuild
missing PlayerComposer failure
missing tracking target failure
idempotency
Cinemachine dependency compile proof
no duplicate rig components
```

### C8 — Multiplayer camera design

Plan before implementation.

Scope:

```text
local player camera
split-screen
shared group camera
join/leave rebinding
spectator/debug mode
```

## 10. What to do with the current CameraComposer MVP-A delta

If it has not been applied:

```text
do not apply as final product cut
```

If it has already been applied:

```text
mark as experimental/superseded
remove or replace during C5
avoid QA/FIRSTGAME validation against it
```

## 11. Acceptance criteria for final camera product surface

Technical:

```text
Cinemachine required and compiling
explicit target-source contracts
no Camera.main fallback
no global manager/singleton/service locator
no silent fallback
clear failure diagnostics
idempotent Apply/Rebuild
runtime/editor boundaries clean
```

Product:

```text
designer can create a camera from menu/template
designer can link camera to PlayerComposer
camera follows real player target via Cinemachine
look-at is authorable
priority is visible
technical bindings are debuggable but not primary UX
FIRSTGAME proves real usage
multiplayer is not accidentally implied by single-player MVP
```

## 12. Suggested commit message

```text
Docs: reframe camera product surface around Cinemachine
```
