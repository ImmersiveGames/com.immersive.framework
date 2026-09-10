# Camera Usage

Status: **Current implementation guide — CAMERA-026-A through H implemented; Shared runtime certified**
Last updated: **2026-09-09**

The current runtime implementation supports **1..N explicitly authored Camera Outputs per
Session**. IF-ADR-026 separates Camera Subject, Assignment, Rig/Presentation and Output.
Shared composition, exact per-Output injection and explicit normalized viewport policy
are implemented. Shared runtime technical QA is certified as of 2026-09-09; Split,
Full Camera aggregate and FIRSTGAME visual proof remain pending.

Supported local Presentation Models are:

```text
Fixed
Follow
Mounted
Third Person
```

Multiple simultaneous Outputs are supported when each has a unique `CameraOutputId`,
Unity `Camera`, `CinemachineBrain` and Default Camera Rig. Add exactly one
`CameraViewOutputPolicyAuthoring` to Persistent Content and bind every Output explicitly.
For shared presentation use a full-screen viewport `(0,0,1,1)`. For a two-way horizontal
split use `(0,0,0.5,1)` for Output A and `(0.5,0,0.5,1)` for Output B. Disable
`PlayerInputManager.splitScreen`; Framework Camera composition owns these rectangles.

Technical certification before the 2026-08-17 Default-output cut:

```text
Full Camera QA
  53/53
  CAMERA QA CERTIFIED
```

The later Default-output authority cut is implemented on `master` and has real Sample 00
consumer proof. A new aggregate Camera QA run covering that cut has not been recorded.

## 1. Product model

```text
Unity Preset
  optional reusable values for CameraRigComposer

CameraRigComposer
  one local rig
  Presentation Model
  consumes resolved View/Subject assignment evidence for shared composition
  model-specific settings
  Cinemachine materialization
  materialization provenance
  diagnostics

CameraSharedComposition
  one persistent shared View and CameraRigComposer
  reconciles all current Camera Subjects without per-Player requests

Session / Route / Activity Camera Override bindings
  explicit scoped normal Camera publication

CameraOutputAuthoring
  persistent physical Unity Camera + CinemachineBrain
  explicit persistent Default Camera Rig

CameraOutputSession
  transactional logical/physical mutation
  Default presentation state
  independent force-default owners

CameraOutputSessionTopology
  Session-owned ordered collection of 1..N outputs
  exact CameraOutputId lookup and consumer injection
  deterministic snapshots and teardown

CameraViewOutputPolicyAuthoring
  explicit CameraViewId -> CameraOutputId bindings
  finite normalized viewport per physical Output
  deterministic shared or split-screen topology

CameraOutputContext
  admitted normal-request set + deterministic winner

Current shared-composition model
  Player / Actor contributes Camera Subject(s)
  Camera Assignment assigns 0..N Subjects to Views
  Camera Rig defines how resolved Subjects are observed
  Camera Output defines the physical destination
  each explicit output uses its own Default Camera Rig and arbitration context
```

`CameraRigRecipe` remains removed.

Unity Presets provide reusable Composer values without introducing another
Framework-owned defaults asset.

## 2. Presentation, request and output Default are separate

Keep these layers separate:

```text
Presentation Model
  how one local rig behaves

Camera Subject
  something that may be observed

Camera Assignment
  which Subjects a composed View observes

Camera Request
  when that rig participates in normal arbitration

Camera Output Default
  persistent fallback/system presentation owned by the output

Camera Output
  physical projection of Default or the current normal winner
```

CAMERA-026-A through H now implement Subject availability, logical View/Assignment,
shared presentation, explicit 1..N Outputs and View-to-Output viewport policy. Typed
requests remain a separate arbitration path for scoped overrides. A Player does not own
a View, Rig, Output or viewport merely by participating.

Changing:

```text
Follow -> Third Person
```

does not change arbitration policy or create a second physical output.

Likewise, assigning a rig as the persistent Default does not publish a Camera request.

## 3. Author one local Camera rig

Create one local GameObject with `CameraRigComposer`.

Choose:

```text
Presentation
```

Configure the relevant targets/settings, then use:

```text
Validate Configuration
Apply / Rebuild Rig
```

Apply/Rebuild materializes or repairs only the local Cinemachine rig.

It never creates:

```text
persistent Unity Camera
CinemachineBrain
AudioListener
CameraOutputAuthoring
```

The current accepted relationship is:

```text
one CameraRigComposer
  -> one local CinemachineCamera
```

If two independently arbitrated shots are required, author two separate rigs.

## 4. Fixed

Use Fixed for an authored static/local shot.

Typical uses:

```text
menu camera
room camera
static Activity camera
static Route camera
establishing shot
```

### Targets

```text
Tracking
  Not Used

Look At
  Not Used / Optional / Required
```

### Materialization

```text
Position Control
  none

Rotation Control
  none
  or CinemachineHardLookAt
```

The `CinemachineCamera` Transform is the authored pose.

Apply/Rebuild preserves that pose.

## 5. Follow

Use Follow to keep an authored offset from a Tracking target.

### Targets

```text
Tracking
  Required

Look At
  Not Used / Optional / Required
```

### Settings

```text
Follow Offset
```

### Materialization

```text
CinemachineFollow
+
CinemachineHardLookAt when Look At participates
```

Existing serialized Follow rigs remain compatible because:

```text
CameraRigPresentationIntent.Follow = 10
```

is preserved.

## 6. Mounted

Use Mounted when an explicit Transform already represents the desired camera
mount pose.

Typical uses:

```text
first-person mount
cockpit
helmet camera
vehicle camera socket
gameplay-controlled camera mount
```

Example:

```text
Actor
  CameraMount
```

Supply `CameraMount` through the typed Camera target architecture.

The Framework does **not** find a child named `CameraMount`.

### Targets

```text
Tracking / Camera Mount
  Required

Separate Look At
  Not Used
```

### Settings

```text
Position Damping
Rotation Damping
```

### Materialization

```text
CinemachineHardLockToTarget
CinemachineRotateWithFollowTarget
```

Gameplay owns motion/rotation of the supplied mount.

Camera Presentation does not read Player input directly.

### Actor Presentation Camera Subject

When a prepared Player Actor must expose a child pivot instead of its Actor root, add
`ActorCameraSubjectAuthoring` to the **root of the Actor Presentation prefab**.

Inspector contract:

```text
Camera Subject
  Source = Actor Presentation
  Role = Observation / Camera Mount
  Transform = exact authored child/pivot Transform
```

The canonical first-person chain is:

```text
gameplay moves/rotates the observation mount
  -> ActorCameraSubjectAuthoring exposes that exact Transform
  -> prepared Actor occurrence publishes CameraSubject.Observation
  -> Camera Assignment assigns the Subject to a Camera View
  -> CameraRigComposer / Mounted consumes the resolved Transform
  -> Camera Output presents the rig
```

The component is optional for Actors whose root Transform is intentionally the observation
pose. Once the component is authored, its `Transform` is required. A missing reference,
component outside the Presentation root, foreign Transform or duplicate authored Subject
blocks publication with diagnostics; the runtime never falls back to the Actor root.

Actor replacement publishes a new occurrence identity and the replacement Presentation's
exact observation Transform. Leave releases the Subject and clears stale assignments while
the Camera View, rig and output retain their independent lifetimes.

## 7. Third Person

Use Third Person for an over-the-shoulder / third-person base presentation.

### Targets

```text
Tracking Pivot
  Required

Separate Look At
  Not Used in the first contract
```

The target may be a Player/Actor camera pivot rotated by gameplay.

### Settings

```text
Shoulder Offset
Vertical Arm Length
Camera Side
Camera Distance
Damping
```

### Materialization

```text
CinemachineThirdPersonFollow
```

The accepted first contract does not add a competing generic Aim stage.

## 8. Typed target sources

The current enum vocabulary includes:

```text
Explicit Transform
Player Composer
Player Slot
Route
Activity
Player Group
```

Only the explicit Transform authoring provider is implemented in the package. The other
kinds are extension vocabulary; notably, `PlayerGroup` does not yet provide a concrete
multi-target `ICameraTargetSource`. IF-ADR-026 requires a general Camera Subject/Subject
Set contract rather than treating `PlayerGroup` as the universal abstraction.

Required target resolution failures block.

Do not add consumer fallback through:

```text
Camera.main
GameObject.Find
object names
tags as authority
hierarchy guessing
first Player
nearest Actor
global registries
```

The persistent Default is also explicit authoring. The Framework does not discover a
Default rig by name, hierarchy, current Cinemachine state or request precedence.

## 9. Apply / Rebuild ownership safety

Apply/Rebuild is ownership-aware.

The Composer retains evidence for:

```text
materialized Presentation
CinemachineCamera
Framework-owned Position Control
Framework-owned Rotation Control
materialization revision
```

Only an exact previously recorded reference proves Framework ownership.

### External / Unknown

A pre-existing component with no Framework provenance is:

```text
ExternalOrUnknown
```

even when technically compatible.

Compatibility does not silently transfer ownership.

### Incompatible conflict

If an incompatible Body/Aim component is external or unknown:

```text
Apply / Rebuild
  -> Blocked
  -> diagnostic
  -> component preserved
```

The Framework does not delete it merely to make the selected model succeed.

## 10. Safe model switching

Model switching preflights Position and Rotation stages before mutation.

Example:

```text
Follow
  -> Third Person
  -> Follow
```

Expected:

```text
same local CinemachineCamera
one valid Body stage
correct Aim stage for selected model
old Framework-owned incompatible control removed
external controls never destroyed
no duplicate pipeline
```

If another stage contains an external conflict, the switch blocks before
partially removing the existing valid Framework-owned pipeline.

## 11. Persistent Camera output

`CameraOutputAuthoring` is the explicit persistent physical output authoring
surface.

It requires:

```text
one stable Output ID
one Unity Camera
one CinemachineBrain
one Default Camera Rig (CameraRigComposer)
```

The Unity Camera and Cinemachine Brain belong to the same physical output GameObject.
The Default rig is an explicit persistent `CameraRigComposer` reference and may live
elsewhere in the same persistent composition.

The normal Inspector exposes:

```text
Output Components
  Unity Camera
  Cinemachine Brain
  Default Camera Rig
```

Use `Validate Configuration` after assigning these references. Missing Default is a
blocking authoring issue and also fails explicitly at runtime:

```text
Camera Output Session Binding requires an explicit Default Camera Rig.
```

There is no automatic discovery or synthetic fallback.

The current implementation and validator require exactly one persistent Camera output.
The accepted target composition contains `1..N` explicit outputs; duplicate
`CameraOutputId` or ambiguous bindings remain invalid. Player count never creates
outputs implicitly.

### Output selection

The physical output uses this order:

```text
force-default presentation active
  -> Default Camera Rig

otherwise normal Camera request winner exists
  -> winner rig

otherwise
  -> Default Camera Rig
```

Normal absence of a winner does not clear the output. Physical `Clear()` is reserved for
true teardown.

## 12. Scoped Camera overrides

Supported normal publishers:

```text
Session Camera Override
Route Camera Override
Activity Camera Override
eligible Local Player Camera publication
```

The Local Player publisher is a current specialized/legacy policy, not the canonical
target model for ordinary Player participation. Under IF-ADR-026, ordinary Players
contribute Subjects while an Activity/Route/Session composition may select the shared
View.

Current precedence convention:

```text
Local Player   50
Activity      100
Route         200
Session       300
```

Higher precedence wins.

Equal precedence requires distinct deterministic Tie-Breaker IDs.

Timing is never hidden priority.

Presentation Model does not affect precedence.

### Session Camera Override is not Default

`SessionCameraOverride` remains a valid optional **normal Session request**.

Use it only when the game actually needs a Session-scoped request to compete in the
normal arbitration ladder.

Do not use it to represent the persistent Default. The Default belongs to
`CameraOutputAuthoring` and has no precedence or tie-break identity.

Removing or omitting a Session override does not remove the output Default.

## 13. System force-default presentation

System presentation may temporarily force the output Default without publishing a
normal Camera request.

`CameraOutputSession` owns independent idempotent force-default owners. A caller releases
only its own ownership, so overlapping system presentation cannot accidentally clear
another caller's force-default state.

The current implementation wires this behavior for Transition through
`SessionCameraTransitionOrchestrator`, which receives `CameraOutputAuthoring`
directly.

This cut does **not** create a Pause-to-Camera authority. Do not infer unwired system
presentation from the existence of the generic owner mechanism.

## 14. Ordinary Player Camera participation

Ordinary Player gameplay contributes Camera Subject availability from the prepared
physical Actor. It does not own a Camera rig or publish a Camera request:

```text
Actor
  PlayerActorDeclaration
  Camera Subject availability

independently:

CameraSharedComposition
  Camera View
  CameraRigComposer
  existing CinemachineCamera
  CameraOutputAuthoring Default presentation
```

`PlayerGameplayCameraAuthoring` and Player Camera eligibility/request evidence are
removed. Gameplay admission requires occupancy plus input only. The shared Composer
stays active because it is the output's explicit Default Camera Rig; Subject membership
updates only its targets. Ordinary Player join, leave, rollback and rejoin keep the
ordinary Local Player Camera request count at zero.

Camera does not own Player Join, Actor creation, Initial Placement or Leave.

Joining/leaving adds/removes Subject availability and assignments without intrinsically
creating/destroying the shared Rig or Output. Explicit Route, Activity, Session,
Cutscene, Modal Presentation, Spectator and Debug requests remain valid arbitration.

## 15. Lifecycle and abnormal component loss

Route and Activity have logical owner lifetime controlled by Game Flow.

Their Camera binding component has a separate publication lifetime.

Unexpected disable/destroy of a published Route/Activity binding releases only
that publication so no orphaned request remains.

It does not synthesize Route/Activity exit.

Re-enable does not silently publish another request.

Session override differs because `SessionCameraOverride` itself owns its normal
Session-request availability.

The output Default has a different lifetime: it belongs to the persistent
`CameraOutputAuthoring` / `CameraOutputSession`.

Repeated cleanup is idempotent.

## 16. Failure behavior

Authoring/runtime blocks explicitly when mandatory Camera evidence is invalid.

Examples:

```text
unsupported Presentation
required target missing
invalid target source
ambiguous local CinemachineCamera candidates
invalid model settings
unknown incompatible Body/Aim component
wrong OutputId
duplicate RequestId
ambiguous equal-precedence tie-break
missing Unity Camera / CinemachineBrain
missing Default Camera Rig
duplicate persistent output
physical apply failure
rollback failure
```

An unknown Presentation never falls back to Follow.

A missing Default never becomes an implicit Session request.

A local materialization failure does not create/alter persistent output
authority.

## 17. Diagnostics

### Composer Advanced / Diagnostics

May expose:

```text
Presentation
materialized Presentation
CinemachineCamera
current Body
current Aim
Framework-owned Position reference
Framework-owned Rotation reference
ownership classification
resolved targets
materialization revision
last result
blocking issue
```

### Output diagnostics

May expose:

```text
Output ID
Default Camera Rig
force-default state / owners
Request ID
Owner / Lifetime
Precedence / Tie-Breaker
admitted normal request set
current normal winner
selected physical presentation
physical apply result
rollback evidence
```

Use the appropriate layer rather than adding local Camera-selection logic.

## 18. Reusable authoring

For reusable Composer values, create a Unity Preset from a configured
`CameraRigComposer`.

Do not create a new Framework Camera Profile merely for symmetry.

## 19. Persistent Content migration

Persistent Content scenes authored before the 2026-08-17 Default-output cut must assign
their intended persistent Default rig explicitly.

Typical migration:

```text
Camera Output
  CameraOutputAuthoring
    Default Camera Rig -> existing persistent Session Camera Rig
```

Then save the consumer scene, close/reopen it and verify the reference persists before
running Play Mode.

`SessionCameraOverride` may stay only if it represents a real Session override.
Do not keep it merely to emulate Default behavior.

The package `PersistentContentTemplateSource.unity` present at the implementation merge
still uses the pre-cut serialized output shape and must be refreshed before that template
artifact is treated as conformant for new 004D consumer scenes.

## 20. Technical certification and Sample 00 evidence

Historical aggregate result dated 2026-08-15:

```text
ADR-022 Presentation Models    14/14
C9R canonical authority        11/11
ADR-004B negative integrity    18/18
ADR-004C owner lifetime        10/10
                              -----
Full Camera                     53/53
```

Terminal:

```text
CAMERA QA CERTIFIED
```

That run predates the Default-output cut and is not relabeled as post-cut QA.

Implementation merge for the Default-output cut:

```text
master
8591385d14b646b612b32defc7180e71f21a2beb
```

Sample 00 consumer proof after assigning `Session Camera Rig` as the explicit Default:

```text
CameraOutputAuthoring
  Initialized
  defaultRig = Session Camera Rig

Activity
  Ready
  blockingIssues = 0

MinimalFirstPersonLocomotion
  READY
  hasBinding = true
  gameplayReady = true
  LOOK_INPUT received
  MOVE_INPUT received
```

The Sample had no configured Transition adapter, so this proof validates explicit Default
output authoring and gameplay-readiness integration, not Transition force-default runtime
behavior.

See [IF-ADR-004D](../Architecture/Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md).

## 21. FIRSTGAME boundary

Sample 00 now provides real-consumer proof for:

```text
persistent output Default authoring
Camera output initialization
prepared Player Camera Subject availability
Activity readiness
input consumer binding
Move / Look consumption
```

Broader ADR-022 C6 remains the consumer proof for:

```text
Fixed in a real Route/Activity
Follow gameplay
Mounted gameplay/cockpit or first-person mount
Third Person gameplay
runtime request overrides between separate rigs
broken-configuration diagnostics across the presentation family
```

Consumer friction may justify later product refinement. It does not by itself
invalidate historical technical certification.

## 22. Implemented topology versus deferred presentation features

Implemented through CAMERA-026-H:

```text
Camera Assignment / Subject Set assignment
shared multi-target / Group Framing projection
multi-output
explicit shared and split-screen viewport topology
```

Secondary-view product presets such as minimap and picture-in-picture remain deferred,
although the View-to-multiple-Outputs cardinality is supported by the binding contract.

Not part of the current accepted presentation family:

```text
Orbital / Free Look
camera input-axis authority / recenter
Spline / Dolly
2D Framed Follow
noise / shake / impulse product authoring
Third Person Aim
advanced collision policy
Timeline/cinematic sequencing
advanced blend policy
per-player physical output
XR Camera authority
```

These presentation capabilities require separate product decisions. The implemented
IF-ADR-026 Shared topology/assignment boundary is technically certified. Split runtime,
the Full Camera aggregate and FIRSTGAME certification remain pending.
