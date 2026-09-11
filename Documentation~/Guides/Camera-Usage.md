# Camera Usage

Status: **Current implementation guide — IF-ADR-026 CAMERA-026-A through H implemented**  
Last updated: **2026-09-11**

This guide describes the current Camera product surface on `master`.

The canonical Camera model separates **Subject**, **Assignment**, **Rig / Presentation**
and **Output**:

```text
Camera Subject
  -> Camera View / Assignment
  -> CameraRigComposer
  -> Camera Output
  -> Unity Camera + CinemachineBrain
```

The current runtime supports **1..N explicitly authored Camera Outputs per Session**.
Output topology is explicit and independent from Player count.

Shared Camera runtime technical QA is certified as of 2026-09-09. Exact Actor
Presentation child-Transform publication is implemented; its focused fresh Unity proof
remains pending. Split runtime/full aggregate recertification and broader FIRSTGAME visual
proof also remain pending.

See:

- [IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology](../Architecture/ADRs/IF-ADR-026-Camera-Subjects-Assignment-and-Multi-Output-Topology.md)
- [IF-ADR-026 Shared Camera Technical Certification](../Architecture/Reconciliation/IF-ADR-026-SHARED-CAMERA-TECHNICAL-CERTIFICATION-2026-09-09.md)
- [IF-ADR-004D — Camera Default Output Presentation Authority](../Architecture/Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)
- [IF-ADR-022 — Camera Rig Presentation Models](../Architecture/ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md)

---

## 1. Product model

Keep these authorities separate:

```text
Camera Subject
  something that may be observed

Camera Assignment
  which Subject(s) a logical Camera View observes

Camera Rig / Presentation
  how those resolved Subject(s) are framed

Camera Request
  when a non-default rig participates in normal arbitration

Camera Output Default
  persistent fallback/system presentation owned by an Output

Camera Output
  physical projection through one Unity Camera + CinemachineBrain
```

A Player does **not** own a Camera View, Camera Rig, Camera Output or viewport merely by
participating in gameplay.

Player count does **not** imply Output count.

```text
2 Players != 2 Cameras
4 Players != 4 Camera Outputs
```

A shared multiplayer camera, one camera per viewport, spectator cameras and debug cameras
are all composition decisions expressed explicitly by Camera topology and policy.

---

## 2. Minimal persistent Camera composition

A normal persistent Camera composition contains:

```text
Persistent Content
  Camera Output A
    Unity Camera
    CinemachineBrain
    CameraOutputAuthoring
      Output ID
      Default Camera Rig

  Default Camera Rig
    CameraRigComposer

  Shared Camera Composition
    CameraSharedComposition

  Camera View Output Policy
    CameraViewOutputPolicyAuthoring
```

For more than one physical Output, author additional explicit Outputs. Every Output must
have its own unique `CameraOutputId`, Unity `Camera`, `CinemachineBrain` and explicit
Default Camera Rig.

Add exactly one `CameraViewOutputPolicyAuthoring` to the persistent composition and bind
each logical View explicitly to an Output and normalized viewport.

Typical full-screen binding:

```text
View -> Output A
Viewport = (0, 0, 1, 1)
```

Typical two-way horizontal split:

```text
View A -> Output A
Viewport = (0,   0, 0.5, 1)

View B -> Output B
Viewport = (0.5, 0, 0.5, 1)
```

When Framework Camera composition owns the viewports, disable
`PlayerInputManager.splitScreen`. Player participation must not create or resize Camera
Outputs implicitly.

---

## 3. Author one Camera Rig

Create a local GameObject and add:

```text
Immersive Framework / Camera / Camera Rig Composer
```

Choose a Presentation Model, configure its settings, then use the Inspector actions:

```text
Validate Configuration
Apply / Rebuild Rig
```

`Apply / Rebuild Rig` materializes or repairs only the local Cinemachine rig.

It never creates:

```text
Unity Camera
CinemachineBrain
AudioListener
CameraOutputAuthoring
```

The current relationship is:

```text
one CameraRigComposer
  -> one local CinemachineCamera
```

If two shots must be independently arbitrated, author two separate rigs.

Supported local Presentation Models are:

```text
Fixed
Follow
Mounted
Third Person
```

Changing the Presentation Model changes how the rig behaves. It does not create a new
physical Output and does not change request precedence.

---

## 4. Fixed

Use **Fixed** for an authored static/local shot.

Typical uses:

```text
menu camera
room camera
static Activity camera
static Route camera
establishing shot
```

Targets:

```text
Tracking
  Not Used

Look At
  Not Used / Optional / Required
```

Materialization:

```text
Position Control
  none

Rotation Control
  none
  or CinemachineHardLookAt
```

The `CinemachineCamera` Transform is the authored pose. `Apply / Rebuild Rig` preserves
that pose.

---

## 5. Follow

Use **Follow** to keep an authored offset from a Tracking target.

Targets:

```text
Tracking
  Required

Look At
  Not Used / Optional / Required
```

Primary setting:

```text
Follow Offset
```

Materialization:

```text
CinemachineFollow
+
CinemachineHardLookAt when Look At participates
```

The current Composer also owns the shared-Follow settings used when one View resolves
multiple Subjects.

Shared Follow can frame the current assigned Subject set without producing one rig or
request per Player.

---

## 6. Mounted

Use **Mounted** when an explicit Transform already represents the desired Camera mount
pose.

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
Actor Presentation
  CameraMount
```

Targets:

```text
Tracking / Camera Mount
  Required

Separate Look At
  Not Used
```

Settings:

```text
Position Damping
Rotation Damping
```

Materialization:

```text
CinemachineHardLockToTarget
CinemachineRotateWithFollowTarget
```

Gameplay owns motion and rotation of the supplied mount. Camera Presentation does not
read Player input directly.

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

Canonical first-person chain:

```text
gameplay moves/rotates the observation mount
  -> ActorCameraSubjectAuthoring exposes that exact Transform
  -> prepared Actor occurrence publishes CameraSubject.Observation
  -> Camera Assignment assigns the Subject to a Camera View
  -> CameraRigComposer / Mounted consumes the resolved Transform
  -> Camera Output presents the rig
```

The component is optional when the Actor root Transform is intentionally the observation
pose.

Once `ActorCameraSubjectAuthoring` is authored, its explicit Transform is mandatory. A
missing reference, component outside the Presentation root, foreign Transform or duplicate
authored Subject blocks publication with diagnostics. The runtime does not fall back to
the Actor root.

Actor replacement publishes a new occurrence identity and the replacement Presentation's
exact observation Transform. Leave releases the Subject and clears stale assignments while
the Camera View, rig and Output retain their independent lifetimes.

---

## 7. Third Person

Use **Third Person** for an over-the-shoulder / third-person base presentation.

Targets:

```text
Tracking Pivot
  Required

Separate Look At
  Not Used in the current first contract
```

The target may be a Player/Actor Camera pivot rotated by gameplay.

Settings:

```text
Shoulder Offset
Vertical Arm Length
Camera Side
Camera Distance
Damping
```

Materialization:

```text
CinemachineThirdPersonFollow
```

The current contract does not add a competing generic Aim stage.

---

## 8. Ordinary Player Camera participation

Ordinary Player gameplay contributes **Camera Subject availability** from the prepared
physical Actor.

It does not own a Camera Rig and does not publish an ordinary Local Player Camera request.

Canonical relationship:

```text
Prepared Player Actor
  -> Camera Subject availability

independently

CameraSharedComposition
  -> Camera View / Assignment
  -> CameraRigComposer
  -> CameraOutputAuthoring
```

The previous ordinary per-Player Camera request path is not the canonical current model.
`PlayerGameplayCameraAuthoring` and Player Camera eligibility/request evidence are removed.
Gameplay admission requires Player occupancy/input according to the Player contract, not a
Player-owned Camera request.

Camera does not own:

```text
Player Join
Actor creation
Initial Placement
Player Leave
```

Join/leave changes Subject availability and View assignments. It does not intrinsically
create/destroy the shared Rig or physical Output.

This is a critical IF-ADR-026 invariant:

```text
Player != Camera
Player != Camera View
Player != Camera Output
```

---

## 9. Shared Camera composition

`CameraSharedComposition` is the explicit runtime authority for one shared logical Camera
View.

It owns/references:

```text
View ID
Assignment context
Assignment owner
Subject selection policy
Target Output ID
```

It consumes Camera Subject availability and reconciles View-to-Subject assignment as
membership changes.

Its physical presentation is the explicitly bound Output's Default `CameraRigComposer`.

Conceptually:

```text
available Subjects
  -> CameraSharedComposition
  -> logical View assignments
  -> bound Output.DefaultCameraRig
  -> Camera Output
```

Shared composition does not manufacture per-Player requests.

A Player joining or leaving changes the Subject set. The View/Rig/Output have independent
lifetimes.

---

## 10. Camera View Output Policy

`CameraViewOutputPolicyAuthoring` defines the explicit mapping:

```text
CameraViewId
  -> CameraOutputId
  -> normalized viewport
```

Every binding is explicit. The Framework does not infer the physical destination from:

```text
Player index
join order
object name
hierarchy
active Camera
```

A policy requires at least one valid binding. Duplicate/ambiguous topology is invalid.

This is the authority used for both full-screen and multi-viewport composition.

---

## 11. Persistent Camera Output

`CameraOutputAuthoring` is the explicit persistent physical Output surface.

Each Output requires:

```text
stable CameraOutputId
Unity Camera
CinemachineBrain
Default Camera Rig (CameraRigComposer)
```

The Unity Camera and `CinemachineBrain` must live on the same GameObject.

The Default Camera Rig is an explicit reference. It may live elsewhere in the same
persistent composition.

There is no automatic discovery and no synthetic fallback.

Use the Inspector validation after assigning all references.

### Output selection

Each physical Output follows this order:

```text
force-default presentation active
  -> Default Camera Rig

otherwise normal Camera request winner exists
  -> winner rig

otherwise
  -> Default Camera Rig
```

Normal absence of a request winner does not clear the physical Output.

The Default is persistent Output authority, not a Camera Request.

---

## 12. Explicit 1..N Output topology

The current architecture/runtime supports **1..N explicitly authored Outputs per
Session**.

Every Output owns an independent physical destination:

```text
CameraOutputId
Unity Camera
CinemachineBrain
Default Camera Rig
normal arbitration context
```

Output count is an authored display/composition decision.

Examples:

```text
1 shared gameplay Camera
  -> 1 Output

4 Players sharing one framed View
  -> still 1 Output

2 explicit split-screen Views
  -> 2 Outputs

1 gameplay View + 1 spectator/display View
  -> 2 Outputs
```

Duplicate `CameraOutputId` or ambiguous View-to-Output bindings are invalid.

The explicit topology is implemented. Current shared runtime certification remains the
recorded baseline; split/full aggregate and broader consumer visual certification remain
pending and must not be inferred merely from architecture support.

---

## 13. Scoped Camera overrides

The current built-in scoped normal Camera publishers are:

```text
Activity Camera Override
Route Camera Override
Session Camera Override
```

Explicit Cutscene, Modal Presentation, Spectator and Debug policies may participate through
the Camera request contract when their consumer implementation is wired accordingly.

Ordinary Player participation does **not** publish the old Local Player Camera request.
Players normally contribute Subjects instead.

Current built-in precedence convention:

```text
Activity   100
Route      200
Session    300
```

Higher precedence wins.

Equal precedence requires deterministic distinct tie-break identity.

Timing is never hidden priority.

Presentation Model does not affect precedence.

### Session Camera Override is not Default

`SessionCameraOverride` is an optional normal Session-scoped request.

Use it only when the game needs a Session-scoped shot to compete in normal request
arbitration.

Do not use it to represent the persistent Default.

```text
CameraOutputAuthoring.DefaultCameraRig
  !=
SessionCameraOverride
```

The Default has no precedence and no request tie-break identity.

Removing a Session override does not remove the Output Default.

---

## 14. System force-default presentation

System presentation may temporarily force an Output back to its Default Camera Rig without
publishing a normal Camera request.

`CameraOutputSession` owns independent idempotent force-default owners. A caller releases
only its own ownership, so overlapping system presentation cannot accidentally clear
another owner's state.

The current Framework wires this behavior for Transition through
`SessionCameraTransitionOrchestrator`.

Do not infer unrelated authority from the generic mechanism. In particular, the existence
of force-default ownership does not itself create a Pause-to-Camera policy.

---

## 15. Typed target sources

The target-source vocabulary currently includes:

```text
Explicit Transform
Player Composer
Player Slot
Route
Activity
Player Group
```

Only the explicit Transform authoring provider is currently implemented as a concrete
package authoring source. The other values are extension vocabulary and must not be treated
as implicit discovery mechanisms.

In particular, `PlayerGroup` is not a universal multi-target abstraction.

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

---

## 16. Apply / Rebuild ownership safety

`CameraRigComposer` materialization is ownership-aware.

The Composer keeps durable evidence for:

```text
materialized Presentation
CinemachineCamera
Framework-owned Position Control
Framework-owned Rotation Control
shared Follow materialization where applicable
materialization revision
```

Only an exact previously recorded reference proves Framework ownership.

A pre-existing compatible component without Framework provenance remains:

```text
ExternalOrUnknown
```

Compatibility does not silently transfer ownership.

If an incompatible Body/Aim component is external or unknown:

```text
Apply / Rebuild Rig
  -> Blocked
  -> diagnostic
  -> external component preserved
```

The Framework does not delete unknown external components merely to make a selected model
succeed.

### Safe model switching

Model switching preflights the affected pipeline before destructive mutation.

Example:

```text
Follow
  -> Third Person
  -> Follow
```

Expected result:

```text
same local CinemachineCamera
one valid Position/Body stage
correct Rotation/Aim stage for selected model
old Framework-owned incompatible control removed
external controls preserved
no duplicate Framework pipeline
```

If another stage contains an external conflict, switching blocks instead of partially
tearing down the existing valid Framework-owned rig.

---

## 17. Lifecycle

Route and Activity Camera override bindings have scoped logical ownership controlled by
Game Flow and a separate component publication lifetime.

Unexpected disable/destroy of a published Route/Activity binding releases only that Camera
publication. It does not synthesize Route/Activity exit.

Re-enable does not silently manufacture a second request.

`SessionCameraOverride` differs because it owns Session-request availability directly.

The Output Default has a different lifetime again: it belongs to the persistent
`CameraOutputAuthoring` / `CameraOutputSession`.

Repeated cleanup is idempotent.

For ordinary Player Subjects:

```text
Join / preparation
  -> Subject becomes available

Actor replacement
  -> old occurrence Subject replaced by new occurrence Subject

Leave
  -> Subject released
  -> stale assignments removed
```

The shared View, Rig and Output remain independently owned.

---

## 18. Failure behavior

Mandatory Camera evidence fails explicitly.

Examples:

```text
unsupported Presentation
required target missing
invalid target source
invalid ActorCameraSubjectAuthoring reference
ambiguous local CinemachineCamera candidates
invalid model settings
unknown incompatible Body/Aim component
invalid or duplicate CameraOutputId
invalid View-to-Output binding
duplicate RequestId
ambiguous equal-precedence tie-break
missing Unity Camera
missing CinemachineBrain
Unity Camera / Brain on different GameObjects
missing Default Camera Rig
physical apply failure
rollback failure
```

An unknown Presentation never falls back to Follow.

A missing explicit Actor observation Transform never falls back to the Actor root.

A missing Output Default never becomes an implicit Session request.

A local rig materialization failure does not create or alter persistent Output authority.

---

## 19. Diagnostics

### CameraRigComposer

Advanced/diagnostic state may expose:

```text
Presentation
materialized Presentation
CinemachineCamera
current Position/Body control
current Rotation/Aim control
Framework-owned references
ownership classification
resolved targets
materialization revision
last result
blocking issue
```

### CameraOutputAuthoring / CameraOutputSession

Output diagnostics may expose:

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

Use the appropriate Camera layer rather than adding local Camera-selection logic in a
consumer.

---

## 20. Reusable authoring

For reusable `CameraRigComposer` values, create a Unity Preset from a configured Composer.

Do not create a separate Framework Camera Profile merely for symmetry.

---

## 21. Persistent Content migration

Persistent Content authored before the explicit Default-output cut must assign the intended
persistent Default rig explicitly.

Typical migration:

```text
Camera Output
  CameraOutputAuthoring
    Default Camera Rig -> existing persistent CameraRigComposer
```

Then save the consumer scene, close/reopen it and verify that the explicit reference
persists before Play Mode validation.

`SessionCameraOverride` may remain only when it represents a real Session-scoped override.
Do not keep it merely to emulate Default behavior.

For IF-ADR-026 migration, also remove assumptions that ordinary Players publish one Camera
request each. Players normally contribute Camera Subjects and the shared composition owns
the View assignment.

---

## 22. Current certification and consumer evidence

Recorded technical evidence must be read by boundary/date.

### Historical presentation-model aggregate — 2026-08-15

```text
Full Camera QA
  53/53
  CAMERA QA CERTIFIED
```

That aggregate predates later Default-output and IF-ADR-026 topology cuts. It remains valid
historical evidence for the boundary it executed and must not be relabeled as proof of
later behavior.

### Shared Camera runtime — 2026-09-09

IF-ADR-026 shared Camera runtime technical proof is recorded as certified.

It covers the shared Subject / View / Assignment / composition runtime boundary represented
by that certification record.

### Exact Actor Presentation observation Transform

`ActorCameraSubjectAuthoring` support for an exact child observation/mount Transform is
implemented on current `master`. Focused fresh Unity lifecycle proof for the added boundary
remains pending according to the current framework tracker.

### Existing real-consumer proof

Sample 00 provides consumer evidence for:

```text
explicit persistent Default Camera Rig authoring
Camera Output initialization
prepared Player Camera Subject availability
Activity readiness integration
Move / Look input consumption
```

This does not substitute for broader visual proof of every Camera Presentation and
multi-Output topology.

### Remaining consumer proof

Still pending as independent proof boundaries:

```text
split runtime / viewport consumer proof
fresh Full Camera aggregate for the post-026 state
broader FIRSTGAME visual proof
```

FIRSTGAME is a consumer proof surface. It does not define Camera architecture.

---

## 23. Consumer checklist

Before Play Mode, verify:

```text
[ ] Every physical Camera Output has a unique CameraOutputId
[ ] Unity Camera and CinemachineBrain are explicitly assigned
[ ] Unity Camera and CinemachineBrain are on the same GameObject
[ ] Every Output has an explicit Default Camera Rig
[ ] Every CameraRigComposer validates
[ ] Apply / Rebuild Rig succeeds for every authored rig
[ ] CameraSharedComposition has explicit View / assignment / Output identity
[ ] CameraViewOutputPolicyAuthoring binds every intended View explicitly
[ ] Viewports are finite normalized rectangles
[ ] Ordinary Players are treated as Camera Subjects, not implicit Camera owners
[ ] ActorCameraSubjectAuthoring is used when an exact child observation pivot is required
[ ] No Camera.main / Find / name / hierarchy fallback was added
[ ] Session/Route/Activity overrides exist only when a real scoped override is required
[ ] Default Camera Rig is not modeled as a Session override
```

The consumer should configure the Camera product surface explicitly and let the Framework
own Camera arbitration, shared assignment and Output projection.