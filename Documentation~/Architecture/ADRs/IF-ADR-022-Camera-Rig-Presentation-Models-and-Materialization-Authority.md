# IF-ADR-022 — Camera Rig Presentation Models and Materialization Authority

Status: **Accepted / Implemented / Technical QA Certified — FIRSTGAME promotion pending**  
Proposed: **2026-08-11**  
Accepted / technically certified: **2026-08-15**  
Type: architecture / product authoring / editor materialization  
Primary decision extended: IF-ADR-004 — Camera Requests and Output Authority  
Product-surface governance: IF-ADR-010 — Editor and Inspector Product Surface Authority  
Related Player spatial authority: IF-ADR-021 — Activity Player Actor Initial Placement Authority  
Source finding: pre-FIRSTGAME architecture review — R4 Camera Presentation Model beyond Follow  
Package implementation baseline: `b645f8db57673cbdc3531ce12b6d399225a4d0cb` (`ADR22`)  
Technical closure record: [Camera Presentation Technical Certification — 2026-08-15](../Reconciliation/IMMERSIVE-FRAMEWORK-CAMERA-PRESENTATION-TECHNICAL-CERTIFICATION-2026-08-15.md)

> This ADR expands the local Camera rig product surface beyond `Follow` without
> reopening Camera output authority, request arbitration, Session output
> lifetime or multi-output architecture.

## 1. Context

IF-ADR-004 already defines the Camera authority chain:

```text
Camera request source
  Session / Route / Activity / eligible Local Player
        ↓
typed CameraRequest + ownership/lifetime evidence
        ↓
ScopedCameraRequestPublisher
        ↓
CameraOutputSession
        ↓
CameraOutputContext
  admission + deterministic winner
        ↓
CameraOutputRigApplicator
  physical projection
        ↓
CameraOutputAuthoring
        ↓
one explicit Unity Camera + CinemachineBrain
```

The accepted product has one persistent Camera output per Session.

`CameraRigComposer` is deliberately **not** that authority.

It owns one concrete local rig configuration and materializes/reuses one local
`CinemachineCamera`.

Before this ADR the product surface supported only:

```text
CameraRigPresentationIntent
  Undefined
  Follow
```

The architectural gap was therefore local **presentation authoring and
materialization**, not Camera request/output authority.

## 2. Product problem

A real game needs more than one local camera presentation behavior.

Accepted first use cases include:

```text
static authored shot
generic follow camera
first-person / cockpit mount
third-person over-the-shoulder camera
```

Without a Framework product contract, consumers would have to:

```text
manually mutate the Cinemachine graph after Apply/Rebuild
bypass CameraRigComposer
create game-specific parallel camera authoring
treat raw Cinemachine components as the Framework product contract
```

That would fragment the product surface.

The solution must also avoid the opposite failure: turning the Framework into a
generic Cinemachine graph editor.

## 3. Decision — Camera authority remains unchanged

IF-ADR-022 does not change:

```text
CameraRequest
CameraOutputContext
winner arbitration
request precedence
request lifetime
CameraOutputSession
CameraOutputRigApplicator authority
CameraOutputAuthoring
one persistent Session output
transactional logical/physical mutation
rollback guarantees
```

A Presentation Model never decides which Camera wins.

A Presentation Model only determines how one local rig behaves when its
`CinemachineCamera` is selected by the existing request system.

## 4. One CameraRigComposer remains canonical

The Framework keeps one designer-facing Composer:

```text
CameraRigComposer
```

It does not introduce parallel authorities such as:

```text
FollowCameraComposer
ThirdPersonCameraComposer
FirstPersonCameraComposer
StaticCameraComposer
```

The single Composer exposes one explicit `Presentation` and only the fields that
have product meaning for that model.

## 5. Presentation Model is product intent

`CameraRigPresentationIntent` describes user-meaningful behavior.

```text
Presentation Model
        ↓
target contract
        ↓
position behavior
        ↓
rotation behavior
        ↓
model-specific settings
        ↓
technical Cinemachine materialization
```

The user chooses the behavior.

The Framework chooses the supported Cinemachine shape for that behavior.

The normal Inspector does not ask the user to assemble arbitrary Body/Aim
component classes.

## 6. Stable presentation identities

The accepted enum is:

```text
Undefined = 0
Follow = 10
Fixed = 20
Mounted = 30
ThirdPerson = 40
```

`Follow = 10` is frozen for serialized compatibility.

No migration may reinterpret existing `Follow` content as another model.

## 7. Accepted Presentation Model family

### 7.1 Fixed

Product intent:

```text
authored local Camera rig pose
no procedural Tracking follow
optional target-oriented rotation
```

Primary uses:

```text
menu shot
room camera
static Activity camera
static Route camera
authored establishing shot
```

Target contract:

```text
Tracking / Follow
  Not Used

Look At
  Not Used / Optional / Required
```

Materialization:

```text
Position Control
  none

Rotation Control
  none when Look At is not used
  CinemachineHardLookAt when Look At participates
```

The `CinemachineCamera` Transform is authored by the consumer.

Apply/Rebuild preserves that pose.

Fixed is still a local rig participating in normal ADR-004 request arbitration.

### 7.2 Follow

Product intent:

```text
maintain an authored spatial relationship to a Tracking target
with optional target-oriented framing
```

Target contract:

```text
Tracking / Follow
  Required

Look At
  Not Used / Optional / Required
```

Materialization:

```text
Position Control
  CinemachineFollow

Rotation Control
  none when Look At does not participate
  CinemachineHardLookAt when Look At participates
```

`FollowOffset` belongs only to Follow.

A configured Look At target is not considered complete unless a supported
rotation stage actually consumes it.

### 7.3 Mounted

Product intent:

```text
attach camera position and rotation to one explicit Tracking target/mount
```

Primary uses:

```text
first-person camera mount
vehicle cockpit
head/helmet camera
camera socket controlled by gameplay
```

Target contract:

```text
Tracking
  Required

separate Look At
  Not Used in the accepted first contract
```

Materialization:

```text
Position Control
  CinemachineHardLockToTarget

Rotation Control
  CinemachineRotateWithFollowTarget
```

The consumer may author:

```text
Actor
  CameraMount
```

but the Framework consumes that Transform through the typed target contract. It
does not discover a child by name.

Camera does not own gameplay input that moves or rotates the mount.

### 7.4 Third Person

Product intent:

```text
third-person presentation around a rotating Tracking target
with explicit shoulder/arm/distance framing
```

Primary uses:

```text
third-person exploration
over-the-shoulder gameplay
third-person shooter base camera
```

Target contract:

```text
Tracking
  Required

separate Look At
  Not Used in the accepted first contract
```

Materialization:

```text
CinemachineThirdPersonFollow
```

No second generic Aim controller is added in the accepted first contract.

Accepted authored settings:

```text
Shoulder Offset
Vertical Arm Length
Camera Side
Camera Distance
Damping
```

The game may rotate an explicit Player/Actor camera pivot through its own
gameplay/input architecture. Camera Presentation does not read `PlayerInput`
directly.

## 8. Position and rotation are distinct technical stages

The architecture recognizes:

```text
Position Control
Rotation Control
```

A model may use:

```text
one Position Control
one Rotation Control
neither stage when authored Transform is intended behavior
```

but that choice is explicit.

Apply/Rebuild must not leave two competing Framework-owned controls for the same
pipeline stage.

## 9. Typed target resolution is retained

The existing target architecture remains:

```text
Explicit Transform
Player Composer
Player Slot
Route
Activity
Player Group
```

through typed `ICameraTargetSource` contracts.

IF-ADR-022 does not add:

```text
Camera.main
GameObject.Find
tag lookup
hierarchy-name lookup
first Player
nearest Actor
global target registry
```

Flow:

```text
CameraRigComposer intent
        ↓
resolve typed targets
        ↓
validate model target contract
        ↓
materialize selected presentation model
```

A materializer never performs its own scene/global target lookup.

## 10. Model-valid target requirements

The Inspector and validation derive valid target semantics from the selected
model.

Examples:

```text
Fixed
  Tracking is Not Used

Follow
  Tracking is Required
  Look At remains configurable

Mounted
  Tracking is Required
  separate Look At is Not Used

Third Person
  Tracking is Required
  separate Look At is Not Used
```

Existing serialized fields may remain for compatibility, but normal product UI
does not present nonsensical combinations.

## 11. Materialization authority

Apply/Rebuild dispatches explicitly by Presentation Model:

```text
CameraRigComposerApplyRebuild
        ↓
Presentation
        ↓
explicit materialization path
        ├─ Fixed
        ├─ Follow
        ├─ Mounted
        └─ Third Person
```

Implementation may use an explicit `switch`, small typed internal helpers and
typed materialization requests.

It must not use runtime reflection, discovery registries or service locators to
find model handlers.

Materialization remains Editor-owned.

Runtime does not depend on Editor assemblies.

## 12. One local CinemachineCamera per Composer

The canonical relationship is:

```text
one CameraRigComposer
  -> one concrete local rig
  -> one CinemachineCamera
```

A Presentation change does not create hidden parallel virtual cameras and switch
between them internally.

If a game needs two separately arbitrated shots, it authors two rigs and uses
the existing ADR-004 request system.

Ambiguous local `CinemachineCamera` candidates block materialization.

## 13. Durable materialization provenance

The Composer retains enough evidence to prove ownership:

```text
materialized Presentation
CinemachineCamera
Framework-owned Position Control
Framework-owned Rotation Control
materialization revision
last materialization result
```

Ownership is exact-reference based.

```text
exact previously recorded generated reference
  -> FrameworkOwned

pre-existing compatible component without proven provenance
  -> ExternalOrUnknown
```

Compatibility alone does not establish ownership.

No retroactive silent adoption is allowed.

## 14. Safe model switching

Switching models can require replacing the local Body/Aim pipeline.

Example:

```text
Follow
  CinemachineFollow

        ↓ Presentation changes

Third Person
  CinemachineThirdPersonFollow
```

The Framework may remove/replace only the old control whose exact reference is
proven Framework-owned.

An external/unknown incompatible control is preserved and blocks.

## 15. Preflight before mutation

Body and Aim stages are preflighted before any destructive reconciliation.

This is a transactional Editor rule:

```text
inspect Position stage
inspect Rotation stage
validate ownership/conflicts
        ↓
if any blocking conflict
  mutate nothing
        ↓
otherwise
  reconcile owned controls
  configure selected model
  commit evidence
```

A blocked model switch must not partially remove a previously valid
Framework-owned stage.

## 16. Idempotence

Repeated Apply/Rebuild with unchanged intent converges:

```text
Third Person
Apply
Apply
Apply

result:
  same CinemachineCamera
  one Third Person Body control
  no duplicate stage
```

Successful rebuild may advance materialization revision/evidence without
duplicating the technical pipeline.

## 17. External / unknown conflict policy

If an incompatible Body/Aim component is not proven Framework-owned:

```text
Apply/Rebuild
  -> Blocked
  -> identify stage/component
  -> report ExternalOrUnknown
  -> do not destroy component
```

This is a hard product invariant.

A compatible external control may remain usable under the selected model, but it
is not silently reclassified as Framework-owned.

## 18. Inspector and UX

`Presentation` is designer-editable:

```text
Fixed
Follow
Mounted
Third Person
```

The Inspector is model-specific.

### Fixed

```text
Presentation
Targets
  Look At requirement/source when used
Pose
  authored through CinemachineCamera Transform
Materialization
Validation
Advanced / Diagnostics
```

### Follow

```text
Presentation
Targets
  Tracking
  Look At
Follow Settings
  Follow Offset
Materialization
Validation
Advanced / Diagnostics
```

### Mounted

```text
Presentation
Targets
  Camera Mount / Tracking
Mounted Settings
  Position Damping
  Rotation Damping
Materialization
Validation
Advanced / Diagnostics
```

### Third Person

```text
Presentation
Targets
  Tracking Pivot
Third Person Settings
  Shoulder Offset
  Vertical Arm Length
  Camera Side
  Camera Distance
  Damping
Materialization
Validation
Advanced / Diagnostics
```

The normal Inspector must not become:

```text
Position Component Type
Rotation Component Type
Extension List
raw Cinemachine graph editor
```

## 19. Advanced / Diagnostics

The user can inspect:

```text
Presentation
materialized Presentation
CinemachineCamera
current Position Control
current Rotation Control
Framework-owned Position reference
Framework-owned Rotation reference
FrameworkOwned / ExternalOrUnknown classification
resolved targets
materialization revision
last result
blocking conflict
```

This technical evidence is secondary to normal authoring but not hidden.

## 20. Unity Presets remain reusable-value mechanism

IF-ADR-022 does not introduce a Camera Profile asset merely because multiple
models exist.

```text
CameraRigComposer
  concrete authoring instance

Unity Preset
  reusable authoring values when desired
```

A new Framework Recipe/Profile requires demonstrated product need that Unity
Presets cannot safely express.

## 21. Runtime behavior remains presentation-agnostic

A request for any accepted model uses the same Camera request contract.

```text
Fixed rig
Follow rig
Mounted rig
Third Person rig
        ↓
same CameraRequest / arbitration
```

`CameraOutputRigApplicator` resolves:

```text
winner.Rig.Composer
  -> composer.CinemachineCamera
```

It does not branch on Presentation Model.

## 22. Presentation does not own Camera request lifetime

A model does not publish/release its own Camera request automatically from
`Awake`, `Start` or `OnEnable`.

Request lifetime remains owned by existing Session / Route / Activity / Player
publishing surfaces.

## 23. Presentation does not own Player lifecycle

Mounted/Third Person target sources may point at Player/Actor evidence.

That does not make Camera authoritative over:

```text
Join
Actor selection
Actor materialization
Initial Placement
Player readiness
Leave
```

A missing required target is a Camera configuration/readiness problem, not
permission to create a Player or Actor.

## 24. Relationship to IF-ADR-021 Initial Placement

Camera does not place the Actor.

Typical ordering:

```text
Actor representation
  materialized/adopted
        ↓
Initial Placement
        ↓
camera target evidence available
        ↓
Camera request may present the Actor
```

A Camera rig must not teleport an Actor to satisfy framing.

## 25. Validation

Generic validation covers:

```text
Presentation is supported
target source is typed when used
resolved targets satisfy model contract
one local CinemachineCamera is identified/materializable
materialization evidence belongs to this Composer
no ambiguous local CinemachineCamera candidates
no unknown incompatible component will be destroyed
numeric/model settings are valid
```

### Fixed

```text
Tracking not required
authored Camera pose valid
Look At requirement satisfied when Required
supported Aim can be materialized when Look At participates
```

### Follow

```text
Tracking present
CinemachineFollow exists/materializable
Follow Offset valid
Look At requirement satisfied
real rotation behavior exists when Look At participates
```

### Mounted

```text
Tracking/mount present
Hard Lock exists/materializable
Rotate With Follow Target exists/materializable
separate Look At not silently accepted
damping valid
```

### Third Person

```text
Tracking present
CinemachineThirdPersonFollow exists/materializable
settings valid
no conflicting unknown Body remains
no implicit Camera input source required
```

Unknown serialized Presentation intent fails explicitly.

There is no fallback to Follow.

## 26. Migration and compatibility

Existing Follow rigs remain Follow because `Follow = 10` is preserved.

Existing explicit/local `CinemachineCamera` references are reused where valid.

A legacy Follow rig that declared Look At but had no real rotation stage can be
repaired by Apply/Rebuild to add the accepted Framework-owned Look At behavior.

Pre-existing Cinemachine pipeline components without C2 provenance are
ExternalOrUnknown.

The first implementation intentionally blocks unknown conflicts rather than
guessing ownership.

## 27. Rejected behavior

Rejected:

- reopening request arbitration for each Presentation Model;
- creating another persistent Camera output for a local model;
- split-screen under IF-ADR-022;
- one top-level Composer class per model;
- exposing arbitrary Cinemachine component types as normal product intent;
- runtime reflection to discover presentation handlers;
- global materializer registry/service locator;
- Camera Presentation reading `PlayerInput` directly;
- Orbital/Free Look without input-authority design;
- `Camera.main` fallback;
- target lookup by name/tag/hierarchy guessing;
- fallback from unsupported model to Follow;
- stale Framework-owned controls after model switching;
- deleting external/unknown pipeline components;
- partial mutation before discovering another stage conflict;
- creating Unity Camera/CinemachineBrain/AudioListener from the Composer;
- treating a local CinemachineCamera as Session output authority;
- declaring Look At active without supported rotation behavior;
- adding a Camera Profile without demonstrated need.

## 28. Deliberately deferred work

```text
Orbital / Free Look
camera input-axis authority
recenter policy
Spline / Dolly
Group Framing product model
2D Framed Follow product model
camera shake/noise product authoring
Cinemachine impulse product authoring
Third Person Aim extension
advanced camera collision policy
Timeline/cinematic sequence authoring
advanced blend policy
multi-output
split-screen
per-Player physical output
XR camera authority
```

These require demonstrated product requirements and separate cuts.

## 29. Implementation closure

### C1 — Presentation contracts and Composer shape — CLOSED

Implemented:

```text
CameraRigPresentationIntent
  Undefined = 0
  Follow = 10
  Fixed = 20
  Mounted = 30
  ThirdPerson = 40

model-valid target semantics
model-specific serialized values
Follow serialized compatibility
```

### C2 — Safe materialization ownership — CLOSED

Implemented:

```text
durable exact-reference provenance
FrameworkOwned vs ExternalOrUnknown
materialized Presentation evidence
Position/Rotation ownership evidence
materialization revision
unknown conflict blocking
no retroactive adoption
```

### C3 — Model materializers — CLOSED

Implemented in accepted order:

```text
Follow completion/repair
Fixed
Mounted
Third Person
```

Technical shapes:

```text
Follow
  CinemachineFollow
  + CinemachineHardLookAt when Look At participates

Fixed
  no Body
  + optional CinemachineHardLookAt
  authored pose preserved

Mounted
  CinemachineHardLockToTarget
  + CinemachineRotateWithFollowTarget

Third Person
  CinemachineThirdPersonFollow
  no extra generic Aim
```

Model switching preflights Body + Aim before mutation.

### C4 — Inspector / UX — CLOSED

Implemented:

```text
designer-editable Presentation
model-specific targets/settings
Apply/Rebuild
Validation
Advanced / Diagnostics
ownership/provenance visibility
```

### C5 — Technical QA — CLOSED / CERTIFIED

Presentation-specific QA:

```text
14/14 PASS
```

Supporting legacy Follow smoke:

```text
6/6 PASS
```

Full Camera certification:

```text
C9R             11/11
ADR-004B        18/18
ADR-004C        10/10
aggregate        53/53
CAMERA QA CERTIFIED
```

### C6 — FIRSTGAME — PENDING CONSUMER PROOF

FIRSTGAME should demonstrate real consumer use of at least:

```text
Fixed Activity/Route Camera
Follow gameplay Camera
Mounted first-person/cockpit-style Camera
Third Person gameplay Camera
runtime override between separate rigs
broken-configuration diagnostics
```

C6 is not additional package implementation unless consumer validation exposes a
concrete defect.

## 30. Technical certification evidence

Terminal result:

```text
[QA_CAMERA_FULL]
status='Completed'
verdict='CAMERA QA CERTIFIED'
adr022Presentation='PASS'
canonicalAuthority='PASS'
adr004NegativeIntegrity='PASS'
adr004OwnerLifetime='PASS'
mandatoryCases='53'
executedCases='53'
passedCases='53'
```

Presentation QA terminal:

```text
[QA][ADR022 Presentation Models]
PASS
cases='14/14'
```

Canonical authority:

```text
[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]
status='Passed'
phase='canonical-override-fixture'
cases='11'
```

Negative integrity:

```text
[QA_CAMERA_ADR004B]
status='Passed'
cases='18/18'
failed='0'
blocked='0'
verdict='ADR-004B CAMERA NEGATIVE INTEGRITY CERTIFIED'
```

Owner lifetime:

```text
[QA_CAMERA_ADR004C]
status='Passed'
cases='10/10'
failed='0'
verdict='ADR-004C CAMERA OWNER LIFETIME INTEGRITY CERTIFIED'
```

## 31. Non-blocking QA fixture hygiene

The certified run emitted three Unity warnings:

```text
The referenced script (Unknown) on this Behaviour is missing!
```

during C9R teardown.

They did not produce `Failed` or `Blocked`, did not prevent Route cleanup, and
the complete Camera matrix finished `53/53`.

Classification:

```text
QA fixture authoring hygiene
not package behavior failure
not ADR-022 technical failure
not certification blocker
```

The QA fixture should still be cleaned so future logs are noise-free.

## 32. Source-control traceability

Package implementation is present on:

```text
ImmersiveGames/com.immersive.framework
master
b645f8db57673cbdc3531ce12b6d399225a4d0cb
commit: ADR22
```

The 53/53 certification was executed with the active QA working tree containing
the ADR-022 presentation smoke, C9R installer reconciliation and Full Camera
orchestration.

At documentation time the remote QA branch still points to its pre-C5 baseline.

Synchronizing those QA changes is source-control traceability work; it does not
reopen the successful technical certification.

## 33. Product maturity / promotion boundary

Architecture is accepted.

C1-C5 package/editor/QA work is complete.

The new presentation family has technical certification.

FIRSTGAME C6 remains the real-consumer promotion gate for practical ergonomics
and gameplay integration.

A FIRSTGAME issue should reopen package architecture only if it demonstrates a
real contract or product-surface defect.

## 34. Required reconciliation — completed

Acceptance requires:

```text
IF-ADR-004
  recognize IF-ADR-022 presentation family
  preserve request/output authority

IF-ADR-010
  register Camera model-specific Class C Inspector/materialization
  preserve ownership-safe Apply/Rebuild rules

Architecture tracking
  close R4 technical implementation
  retain C6 FIRSTGAME consumer proof
```

Those documentation reconciliations are part of the 2026-08-15 technical
closure.

## 35. Current disposition

```text
Architecture
  ACCEPTED

Package C1
  CLOSED

Package / Editor C2
  CLOSED

Package / Editor C3
  CLOSED

Package / Editor C4
  CLOSED

Technical QA C5
  CAMERA QA CERTIFIED
  53/53

FIRSTGAME C6
  PENDING CONSUMER PROOF

Package implementation blocker
  NONE

Technical Camera certification blocker
  NONE
```
