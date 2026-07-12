# ADR-PROD-0006 — Camera requests and output-scoped runtime authority

Status: Accepted  
Date: 2026-07-10  
Package: `com.immersive.framework`  
Area: Camera Product Surface / Runtime Authority  
Supersedes: `ADR-PROD-0005`, C3–C8 camera ownership and activation decisions

## Context

The current camera lane correctly established Cinemachine as the presentation engine and `PlayerComposer` as an explicit source of follow/look-at targets. However, the implemented product shape still conflates:

```text
target source
rig authoring/materialization
runtime camera selection
output/viewport ownership
```

A local `CameraComposer` with a Cinemachine Camera and editable priority is sufficient for a single-rig proof, but it is not sufficient for the intended framework behavior:

```text
Route may present a camera.
Activity may temporarily present another camera.
A local Player may assume camera control when gameplay becomes available.
Activity or cutscene may override Player presentation and later release it.
Split-screen requires one independent camera authority per viewport.
Online play may create camera authority only for local players.
Spectator output may use a separate camera authority.
```

Cinemachine already owns camera presentation mechanics:

```text
tracking
look-at
framing
damping
position and rotation algorithms
priority/channel integration
brain evaluation
blending
```

The framework must not recreate these mechanics. The missing framework responsibility is explicit policy for which camera request controls each output at a given time.

The previous model also retained or documented compatibility paths for `FrameworkCameraDirector`, Route/Activity camera bindings, PlayerView camera activation and raw Unity Camera enable/disable behavior. This project has no production compatibility requirement. Keeping those paths allows the superseded architecture to return and creates competing runtime authorities.

## Decision

The Camera Product Surface is restructured around five independent layers:

```text
Camera Target Source
  typed provider of FollowTarget and LookAtTarget

Camera Rig Recipe
  reusable Cinemachine behavior intent

Camera Rig Composer
  authoring and idempotent Cinemachine materialization
  no runtime winner-selection authority

Camera Request
  explicit request for a rig on a specific output
  declares owner, lifetime, targets and policy

Camera Output Context
  scoped runtime authority for one output/viewport
  arbitrates requests and applies the winning rig through Cinemachine
```

Cinemachine remains mandatory and is the only official camera presentation engine.

## Canonical model

### 1. Camera target sources

A target source provides target evidence only.

Examples:

```text
PlayerComposer
Boss or Actor target provider
Explicit transform target provider
Shared player-group target provider
```

A target source:

```text
may expose FollowTarget
may expose LookAtTarget
must fail explicitly when required targets are missing
must not select or activate a camera
must not mutate Cinemachine priority
```

`PlayerComposer` remains a valid explicit target source. It does not own a Cinemachine Camera.

### 2. Camera rig recipe

`CameraRigRecipe` defines reusable presentation intent, including as applicable:

```text
Cinemachine position algorithm
Cinemachine rotation algorithm
offset and distance
damping
lens
target requirements
blend hints
output compatibility
```

A Recipe contains no concrete scene Player reference and no runtime request owner.

### 3. Camera rig composer

`CameraRigComposer` is the designer-facing instance authoring surface.

It:

```text
selects a CameraRigRecipe
references or receives an explicit target source
materializes a local Cinemachine rig idempotently
exposes designer intent first
exposes technical evidence under Advanced/Debug
```

It does not:

```text
decide whether its rig is currently active
arbitrate against Route, Activity, Player or cutscene
directly own output lifetime
act as a global manager
```

### 4. Camera requests

Route, Activity, Player and other owners publish explicit requests.

A request must identify:

```text
CameraOutputId
request owner
request lifetime
CameraRigRecipe or materialized rig
target source
precedence/arbitration policy
release condition
diagnostic source and reason
```

Requests are typed and scoped. No request is discovered through object name, hierarchy path or `Camera.main`.

Representative lifetimes:

```text
Route
Activity
LocalPlayerEligibility
ExplicitOperation
SpectatorSession
```

Representative owners:

```text
Route
Activity
LocalPlayer
Cutscene
Pause or modal presentation
Spectator
Debug
```

Owners request and release camera intent. They do not directly toggle Unity Camera components or mutate Cinemachine priority as independent authorities.

### 5. Camera output contexts

`CameraOutputContext` is the only runtime authority that selects a camera for one output.

An output corresponds to a concrete presentation destination such as:

```text
main single-player output
local-player viewport 1
local-player viewport 2
spectator output
debug output
```

Each output context owns or references:

```text
one Unity Camera
one CinemachineBrain
viewport/output configuration
registered camera requests
current winning request
transition/blend application state
diagnostics
```

The context:

```text
admits and releases requests
selects the winner through explicit policy
applies the winning rig to Cinemachine
restores the next valid request when an override is released
fails explicitly when required output or rig state is invalid
```

There is no application-global `CameraManager`. Multiple output contexts may coexist, each with explicit lifetime and ownership.

## Expected flows

### Single-player startup

```text
Route enters
-> Route publishes Route Camera Request
-> Main CameraOutputContext selects Route request
-> Cinemachine presents Route rig

Activity enters
-> Activity may publish Activity Camera Request
-> output context applies Activity rig when policy wins

Player becomes locally controllable
-> Player publisher submits Player Camera Request
-> output context selects Player request
-> Cinemachine blends to Player rig
```

### Temporary Activity override

```text
Player Camera Request remains registered
-> Activity publishes Puzzle or Cutscene request
-> Activity request wins
-> Cinemachine presents Activity rig
-> Activity releases request
-> output context restores Player request
-> Cinemachine blends back
```

### Split-screen

```text
Local Player 1
-> request targets Output 1

Local Player 2
-> request targets Output 2

Output 1 and Output 2
-> independent CameraOutputContexts
-> independent Unity Cameras and CinemachineBrains
```

### Online multiplayer

```text
local player
-> may publish a camera request to a local output

remote player
-> does not automatically create local camera authority
-> may still be used as an explicit spectator or target source
```

## Precedence policy

The exact arbitration algorithm is implemented in a later technical cut, but the following rules are frozen:

```text
precedence belongs to CameraOutputContext policy
request owners do not compete by mutating Cinemachine priority directly
request lifetime and release are explicit
ties are invalid unless a deterministic tie-breaker is declared
release restores the next valid request
missing mandatory request data blocks explicitly
```

Cinemachine priority or channel values may be generated as technical materialization details after arbitration. They are not the source of framework authority.

## Destructive removal policy

The following architecture is superseded and must be physically removed from the package, QA and consumer guidance:

```text
FrameworkCameraDirector
FrameworkRouteCameraBinding
FrameworkActivityCameraBinding
PlayerViewCameraTargetBindingAdapter
PlayerViewCameraActivationAdapter
raw Camera.enabled selection as camera policy
direct independent priority competition between owners
PlayerView as camera authority
compatibility facades, aliases or obsolete wrappers for those paths
```

`FrameworkCameraAnchorHost` must also be removed if it exists only for the superseded architecture. A real non-Player target-provider requirement must be expressed through a new typed target source contract, not by preserving the old type.

`FrameworkCinemachineRigApplier` may remain only if it is a neutral typed Cinemachine adapter with no dependency on the old Director/binding model and no winner-selection authority. Otherwise it must be replaced.

No `[Obsolete]` compatibility bridge, migration facade, legacy menu or retained QA smoke is allowed. Historical documentation may describe that the old model existed, but no current guide may teach or recommend it.

## Superseded contracts

The following concepts are not valid as runtime authority:

```text
CameraMode as winner selection
CameraOwnershipScope as runtime arbitration
CameraProductIntent.Priority as direct cross-owner competition
one CameraComposer equals one active camera
```

A camera mode may remain only as descriptive rig intent. Ownership and lifetime must be represented by explicit request and output contracts.

## Preserved decisions

The following decisions from the prior camera work remain valid:

```text
Cinemachine is mandatory.
The framework does not reimplement Follow, LookAt, damping or blending.
PlayerComposer may expose CameraTarget and LookAtTarget.
Target resolution is explicit and typed.
Required missing targets block explicitly.
No Camera.main fallback.
No functional object-name or hierarchy-path lookup.
No global singleton or service locator.
Apply/Rebuild is idempotent.
Designer-first Inspector with Advanced/Debug evidence.
```

## Product authoring direction

The expected product surface becomes:

```text
Create menu or wizard
-> CameraRigRecipe
-> CameraRigComposer
-> Apply/Rebuild Cinemachine rig
-> select explicit target source
-> publish request through Route, Activity, Player or another owner
-> CameraOutputContext diagnostics show registered requests and winner
```

Diagnostics are supporting evidence. They are not the main authoring flow.

## Implementation sequence

```text
C9A — Camera architecture ADR and documentation reset
C9B — Destructive removal of superseded camera architecture
C9C — Camera request and output contracts
C9D — Single-output CameraOutputContext runtime
C9E — Cinemachine winning-request application
C9F — Route, Activity and Player request publishers
C9G — QA request arbitration and restoration
C9H — FIRSTGAME manual integration proof
```

## Technical acceptance criteria

```text
The package compiles after each implementation cut.
No runtime Editor dependency.
No old camera Director/binding/activation path remains.
No fallback silently creates or selects an output.
No owner directly acts as camera-selection authority.
One output has exactly one scoped CameraOutputContext authority.
Cinemachine executes presentation.
Request admission, winner selection and release are diagnostic.
```

## Product acceptance criteria

```text
A designer can create a reusable rig intent.
A designer can materialize a Cinemachine rig.
Route, Activity and Player can request cameras without directly controlling Cinemachine.
An Activity override can release and restore the Player request.
Single-player is usable.
Split-screen and online shapes are not contradicted by the contracts.
Advanced/Debug shows output, requests, winner, targets and rig evidence.
```

## Consequences

## Session output and explicit override correction

The main single-player output is session-owned and authored in `UIGlobal`, not
inside a Route scene. Route, Activity and local Player request sources receive
that output through explicit Framework Core injection; no scene reference,
singleton or service locator is used.

Route and Activity lifecycle entry only makes an override available. It never
publishes a request. Their explicit request/release API is the only authority
for temporary overrides. The fixed precedence is:

```text
Local Player 50 < Activity 100 < Route 200 < Session 300
```

The Session camera is persistent but is not a gameplay winner. The transition
orchestrator requests it only after the fade/loading cover has settled and
releases it before the cover opens over the destination content.

### Positive

```text
Route, Activity and Player no longer compete as camera authorities.
Cinemachine remains responsible for presentation quality.
Split-screen is modeled as multiple outputs rather than special-case priority.
Online remote players do not accidentally create local cameras.
Request release and previous-camera restoration become explicit.
The superseded architecture cannot return through compatibility code.
```

### Cost

```text
Current C3–C8 camera code and QA require destructive cleanup.
CameraComposer/CameraRecipe naming and responsibilities may change.
FIRSTGAME camera integration must be rebuilt after package and QA contracts are ready.
```

## Suggested commit message

```text
Docs: freeze camera requests and output contexts architecture
```
