# Camera Usage

Status: **Current implementation guide — IF-ADR-026 CAMERA-026-A through H technically certified; IF-ADR-027 accepted with CAMERA-027-A through D implemented, CAMERA-027-E deferred and CAMERA-027-F next**
Last updated: **2026-09-12**

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

Current technical closure is the 2026-09-12 Full Camera certification:

```text
CAMERA QA CERTIFIED
mandatory cases = 39/39
ADR-026 phases = 2/2
certified dimensions = 9/9
```

The run includes exact Actor Presentation child-Transform publication/consumption,
shared Camera membership/replacement/leave/rejoin, explicit split/multi-output isolation,
View→Output viewport topology, generic arbitration and negative validation. Remaining work
is **consumer proof and migration** in official Samples/FIRSTGAME under CAMERA-027-F.

See:

- [IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology](../Architecture/ADRs/IF-ADR-026-Camera-Subjects-Assignment-and-Multi-Output-Topology.md)
- [IF-ADR-027 — Camera Authoring Definitions and Composition Authority](../Architecture/ADRs/IF-ADR-027-Camera-Authoring-Definitions-and-Composition-Authority.md)
- [Camera Full Technical Certification — 2026-09-12](../Architecture/Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)
- [IF-ADR-026 Shared Camera Technical Certification — 2026-09-09](../Architecture/Reconciliation/IF-ADR-026-SHARED-CAMERA-TECHNICAL-CERTIFICATION-2026-09-09.md)
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
are composition decisions expressed explicitly by Camera topology and policy.

---

## 2. Canonical definition-backed authoring

Normal Camera authoring uses typed definitions rather than copied stable-ID text.

The normal designer-facing concepts are:

```text
Camera Subject
Camera View Definition
Camera Rig Behavior Definition
Camera Output Definition
Viewport / View→Output association
```

Stable `CameraViewId`, `CameraOutputId`, assignment context/owner IDs and technical
`CameraViewOutputBinding` objects remain runtime/diagnostic evidence. They are not the
canonical linking mechanism in normal Inspectors.

Normative shorthand:

> **Binding is runtime topology; association is authoring intent.**

Do not restore a workflow based on copying IDs between Inspectors.

---

## 3. Minimal persistent Camera composition

A normal persistent Camera composition contains:

```text
Persistent Content
  Camera Output A
    Unity Camera
    CinemachineBrain
    CameraOutputAuthoring
      Output Definition
      Default Camera Rig

  Default Camera Rig
    CameraRigComposer
      Behavior Definition

  Shared Camera Composition
    CameraSharedComposition
      View Definition
      Subject Policy
      Output Definition
      Viewport
```

The normal one View / one Output association is authored on `CameraSharedComposition`.
Viewport defaults to normalized fullscreen `(0, 0, 1, 1)`. The Framework projects that
association into the Session `CameraViewOutputTopology`.

A separate `CameraViewOutputPolicyAuthoring` is **not required** for this simple case.

For more than one physical Output, author additional explicit Outputs. Every Output must
have its own unique Output Definition, Unity `Camera`, `CinemachineBrain` and explicit
Default Camera Rig. Every active physical Output must be covered by exactly one admitted
View→Output association.

Typical full-screen association:

```text
Shared Camera Composition
  View Definition   = Gameplay View
  Output Definition = Main Output
  Viewport          = (0, 0, 1, 1)
```

Typical two-way horizontal split may use two Shared Camera Composition associations or
the advanced policy surface:

```text
View A -> Output A
Viewport = (0,   0, 0.5, 1)

View B -> Output B
Viewport = (0.5, 0, 0.5, 1)
```

When Framework Camera composition owns the viewports, disable
`PlayerInputManager.splitScreen`. Automatic PlayerInput split-screen is rejected by
Framework authoring validation for this topology.

---

## 4. Author one Camera Rig

Create a local GameObject and add:

```text
Immersive Framework / Camera / Camera Rig Composer
```

Create a typed reusable Camera Rig Behavior asset from:

```text
Assets / Create / Immersive Framework / Camera / Rig Behaviors
  Fixed
  Follow
  Mounted
  Third Person
```

Configure the model-specific settings on that asset, assign it to the Composer's
`Behavior / Definition` field, then use:

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

The relationship is:

```text
one CameraRigBehaviorDefinition
  -> one or more CameraRigComposer instances
  -> one local CinemachineCamera per Composer
```

If two shots must be independently arbitrated, author two separate rigs.

Supported Presentation Models:

```text
Fixed
Follow
Mounted
Third Person
```

Changing the Behavior definition changes presentation behavior. It does not create a new
physical Output, select Players or change request precedence.

---

## 5. Fixed

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

## 6. Follow

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

Shared Follow can frame the current assigned Subject set without producing one rig or
request per Player. Multi-Subject Follow uses Framework-owned group projection and group
framing; group membership is derived from the explicit View assignment, not Player count.

---

## 7. Mounted

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

This exact child-Transform/Mounted replacement/leave/rejoin boundary is included in the
2026-09-12 Full Camera certification.

---

## 8. Third Person

Use **Third Person** for an over-the-shoulder / third-person base presentation.

Targets:

```text
Tracking Pivot
  Required

Separate Look At
  Not Used in the current first contract
```

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

## 9. Ordinary Player Camera participation

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

Critical invariant:

```text
Player != Camera
Player != Camera View
Player != Camera Output
```

---

## 10. Shared Camera composition

`CameraSharedComposition` is the explicit authoring/runtime composition surface for one
shared logical Camera View.

Normal authoring owns/references:

```text
View Definition
Subject selection policy
Output Definition
Viewport
```

From those explicit consumer choices the Framework projects technical evidence including:

```text
CameraViewId
CameraOutputId
Assignment context identity
Assignment owner identity
CameraViewOutputBinding
```

Those projected values may be inspected in Advanced / Debug. They are not parallel normal
authoring authority.

The composition consumes Camera Subject availability and reconciles View-to-Subject
assignment as membership changes.

Its physical presentation is the explicitly bound Output's Default
`CameraRigComposer` unless normal request arbitration selects an override.

Conceptually:

```text
available Subjects
  -> CameraSharedComposition
  -> logical View assignments
  -> bound Output.DefaultCameraRig
  -> Camera Output
```

Shared composition does not manufacture per-Player requests.

---

## 11. Camera View Output Policy

`CameraViewOutputPolicyAuthoring` is the **advanced explicit multi-binding surface**.

Use it when several View → Output → viewport relations need to be authored together, for
example split, spectator or multi-display layouts. Do not use it merely because runtime
has a binding object.

Normal simple case:

```text
Shared Camera Composition
  View Definition
  Subject Policy
  Output Definition
  Viewport
```

Advanced bindings still use typed definition references:

```text
View Definition
  -> Output Definition
  -> normalized viewport
```

Persistent Content may include **0 or 1** advanced policy. More than one policy is
ambiguous and blocked. Simple associations and advanced bindings are aggregated with
**no precedence**. Two associations targeting the same Output are a conflict.

Every binding is explicit. The Framework does not infer the physical destination from:

```text
Player index
join order
object name
hierarchy
active Camera
```

The Session requires every active physical Output to be bound exactly once.

---

## 12. Persistent Camera Output

`CameraOutputAuthoring` is the explicit persistent physical Output surface.

Normal authoring requires:

```text
Output Definition
Unity Camera
CinemachineBrain
Default Camera Rig (CameraRigComposer)
```

The Output Definition projects the stable `CameraOutputId`; consumers do not normally
copy that ID between authoring surfaces.

The Unity Camera and `CinemachineBrain` must live on the same GameObject.
The Default Camera Rig is an explicit reference. There is no automatic discovery and no
synthetic fallback.

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

The Default is persistent Output authority, not a Camera Request.

---

## 13. Explicit 1..N Output topology

The current architecture/runtime supports **1..N explicitly authored Outputs per
Session**.

Every Output owns an independent physical destination:

```text
CameraOutputId (projected from Output Definition)
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

Duplicate Output identity or ambiguous View-to-Output bindings are invalid.

The 2026-09-12 Full Camera certification proves the current two-Output split fixture,
left/right viewport topology, Output isolation, missing-Output rejection and rejection of
automatic `PlayerInputManager` split-screen while Framework Camera composition is active.

---

## 14. Scoped Camera overrides

The current built-in scoped normal Camera publishers are:

```text
Activity Camera Override
Route Camera Override
Session Camera Override
```

Current built-in precedence convention:

```text
Activity   100
Route      200
Session    300
```

Higher precedence wins. Equal precedence requires deterministic distinct tie-break
identity. Timing is never hidden priority. Presentation Model does not affect precedence.

Ordinary Player participation does **not** publish the old Local Player Camera request.
Players normally contribute Subjects instead.

### Session Camera Override is not Default

`SessionCameraOverride` is an optional normal Session-scoped request.

```text
CameraOutputAuthoring.DefaultCameraRig
  !=
SessionCameraOverride
```

The Default has no precedence and no request tie-break identity. Removing a Session
override does not remove the Output Default.

---

## 15. System force-default presentation

System presentation may temporarily force an Output back to its Default Camera Rig without
publishing a normal Camera request.

`CameraOutputSession` owns independent idempotent force-default owners. A caller releases
only its own ownership, so overlapping system presentation cannot accidentally clear
another owner's state.

The current Framework wires this behavior for Transition through
`SessionCameraTransitionOrchestrator`.

---

## 16. No implicit target or output discovery

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

The persistent Default is explicit authoring. The Framework does not discover a Default
rig by name, hierarchy, current Cinemachine state or request precedence.

A simplified definition-backed authoring surface may derive technical facts only from
explicit choices already made by the consumer.

---

## 17. Apply / Rebuild ownership safety

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
A pre-existing compatible component without Framework provenance remains
`ExternalOrUnknown`.

If an incompatible Body/Aim component is external or unknown, Apply/Rebuild blocks and
preserves that external component.

Model switching preflights affected pipeline state before destructive mutation.

---

## 18. Lifecycle

Route and Activity Camera override bindings have scoped logical ownership controlled by
Game Flow and a separate component publication lifetime.

Unexpected disable/destroy of a published Route/Activity binding releases only that Camera
publication. It does not synthesize Route/Activity exit. Repeated cleanup is idempotent.

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

## 19. Failure behavior

Mandatory Camera evidence fails explicitly.

Examples:

```text
unsupported Presentation
required target missing
invalid ActorCameraSubjectAuthoring reference
ambiguous local CinemachineCamera candidates
invalid behavior/model settings
unknown incompatible Body/Aim component
missing/invalid/duplicate View or Output Definition identity
invalid View-to-Output association
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
A missing definition does not silently fall back to old raw authored ID text.

---

## 20. Diagnostics

Advanced/diagnostic state may expose:

```text
stable View / Output IDs
Assignment context / owner identity
technical View→Output binding
materialization provenance
resolved Subjects
materialization revision
current request winner
physical presentation
rollback evidence
```

Advanced / Debug is evidence, not a second authored authority.

---

## 21. Reusable authoring

Use typed Camera Rig Behavior definition assets for reusable Presentation intent and
tuning. The Behavior asset contains no scene objects, Subjects, Outputs or runtime
Assignment state.

Use typed Camera View and Output definitions wherever the same logical authored identity
must be referenced across official Camera surfaces.

`CameraRigComposer` remains concrete local materialization authority.
`CameraOutputAuthoring` remains concrete physical Output authority.

CAMERA-027-E, a grouped reusable Camera Composition definition, is **deferred**. Do not
invent such an asset for Samples unless repeated real consumer authoring demonstrates that
the group itself is meaningful reusable intent.

---

## 22. Current certification and consumer evidence

### Historical presentation-model aggregate — 2026-08-15

```text
Full Camera QA
53/53
CAMERA QA CERTIFIED
```

That aggregate remains historical evidence for the boundary it executed.

### Focused Shared Camera certification — 2026-09-09

The focused IF-ADR-026 Shared Camera runtime proof remains valid dated evidence for its
boundary.

### Full current Camera certification — 2026-09-12

Current technical authority:

[Camera Full Technical Certification — 2026-09-12](../Architecture/Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)

```text
[QA_CAMERA_FULL]
status='Completed'
verdict='CAMERA QA CERTIFIED'
mandatoryEstablishedCases='39'
executedEstablishedCases='39'
passedEstablishedCases='39'
adr026Phases='2/2'
dimensions='9/9'
missing='<none>'
```

The same run proves:

```text
exact Actor Presentation child observation Transform  PASS
Mounted exact-Transform consumption                  PASS
stale Actor occurrence removal                       PASS
ordinary per-Player Camera requests = 0             PASS
shared membership/replacement/leave/rejoin           PASS
explicit multi-output                               PASS
Output isolation                                     PASS
View→Output binding                                  PASS
left/right split viewport topology                   PASS
missing Output rejection                             PASS
automatic PlayerInput split-screen rejection         PASS
generic arbitration                                 PASS
negative validation                                  PASS
```

The canonical baseline was restored after the certified run.

### Remaining consumer proof — CAMERA-027-F

Technical Camera certification is closed for the current architecture. Remaining work is
real-consumer/product proof:

```text
migrate official Samples/FIRSTGAME to definition-backed authoring
prove normal one View / one Output workflow
prove shared Follow consumer behavior
prove representative split-screen consumer behavior
remove stale normal-authoring dual-authority paths
```

FIRSTGAME and Samples are consumer proof surfaces. They do not define or reopen Camera
architecture by themselves.

---

## 23. Sample migration checklist

Before Play Mode, verify:

```text
[ ] Every physical Camera Output references a unique Output Definition
[ ] Unity Camera and CinemachineBrain are explicitly assigned
[ ] Unity Camera and CinemachineBrain are on the same GameObject
[ ] Every Output has an explicit Default Camera Rig
[ ] Every CameraRigComposer references the intended Behavior Definition
[ ] Every CameraRigComposer validates
[ ] Apply / Rebuild Rig succeeds for every authored rig
[ ] CameraSharedComposition has View Definition, Subject Policy, Output Definition and Viewport
[ ] Simple View → Output associations are complete, or one advanced Camera View Output Policy covers the remaining Outputs
[ ] CameraViewOutputPolicyAuthoring is used only as the advanced multi-binding surface
[ ] Viewports are finite normalized rectangles
[ ] Ordinary Players are treated as Camera Subjects, not implicit Camera owners
[ ] ActorCameraSubjectAuthoring is used when an exact child observation pivot is required
[ ] No View ID / Output ID / Assignment Context ID / Assignment Owner ID is copied manually in the normal workflow
[ ] No Camera.main / Find / name / hierarchy fallback was added
[ ] Session/Route/Activity overrides exist only when a real scoped override is required
[ ] Default Camera Rig is not modeled as a Session override
[ ] PlayerInputManager automatic split-screen is disabled when Framework Camera topology owns viewports
```

For the next workstream, treat this guide plus IF-ADR-026 and accepted IF-ADR-027 as the
canonical baseline. The next named cut is **CAMERA-027-F — Consumer migration and stale
surface removal**.
