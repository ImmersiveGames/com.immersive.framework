# IF-ADR-022 — Camera Rig Presentation Models and Materialization Authority

Status: **Proposed**  
Date: 2026-08-11  
Type: architecture / product authoring / editor materialization  
Primary decision extended: IF-ADR-004 — Camera Requests and Output Authority  
Source finding: pre-FIRSTGAME architecture review — R4 Camera Presentation Model beyond Follow

> This ADR expands the local Camera rig product surface beyond the single current
> `Follow` presentation without reopening Camera output authority, request arbitration,
> Session output lifetime or multi-output architecture.

## Context

IF-ADR-004 already defines and certifies the Camera authority chain:

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
CameraOutputSessionBinding
        ↓
one explicit Unity Camera + CinemachineBrain
```

The accepted product has one persistent Camera output per Session.

The local `CameraRigComposer` is deliberately not that authority.

It owns one concrete local rig configuration and materializes one local
`CinemachineCamera`.

The current product surface is intentionally narrow:

```text
CameraRigPresentationIntent
  Undefined
  Follow
```

`CameraRigComposer.TryValidateForApply` rejects every presentation intent other than
`Follow`.

Its Editor Apply/Rebuild path currently materializes:

```text
CinemachineCamera
CinemachineFollow
Follow target
optional Look At target
Follow offset
```

The runtime output path does not depend on `CinemachineFollow`.

`CameraOutputRigApplicator` only requires the winning request to resolve to a
`CameraRigComposer` with one valid materialized `CinemachineCamera`, then enables that
camera and disables the previously applied one.

Therefore the architectural gap is local **presentation authoring and materialization**,
not Camera request/output authority.

## Product problem

A real game commonly needs more than one local camera presentation behavior.

Examples include:

```text
static authored shot
generic follow camera
first-person / cockpit mount
third-person over-the-shoulder camera
```

Today a user who needs those behaviors has three bad options:

```text
manually mutate the Cinemachine graph after Apply/Rebuild
bypass CameraRigComposer
create game-specific parallel camera authoring
treat Cinemachine components themselves as the Framework product contract
```

Those options undermine the canonical product surface.

At the same time, exposing every Cinemachine component and every possible combination as
Framework authoring would create a second problem:

```text
generic component graph editor
many invalid combinations
unclear target requirements
unclear ownership of generated components
destructive rebuild behavior
Framework API coupled directly to Cinemachine implementation details
```

R4 must expand capability without turning the Framework into a general Cinemachine
front-end.

## Decision

### 1. Camera authority remains unchanged

R4 does not change:

```text
CameraRequest
CameraOutputContext
winner arbitration
request priority
request lifetime
CameraOutputSession
CameraOutputRigApplicator authority
CameraOutputSessionBinding
one persistent Session output
transactional logical/physical mutation
rollback guarantees
```

A presentation model never decides which camera wins.

A presentation model only determines how one local rig behaves when its
`CinemachineCamera` is the selected rig.

### 2. One CameraRigComposer remains the canonical product surface

The Framework keeps one designer-facing Composer:

```text
CameraRigComposer
```

R4 must not introduce:

```text
FollowCameraComposer
ThirdPersonCameraComposer
FirstPersonCameraComposer
StaticCameraComposer
```

as parallel top-level product authorities.

The single Composer exposes one explicit **Presentation Model** and the fields relevant
to that model.

This preserves the ADR-004 rule that the Composer is the local rig intent/materialization
surface while keeping runtime output authority elsewhere.

### 3. Presentation Model is product intent, not a raw Cinemachine component choice

`CameraRigPresentationIntent` describes a user-meaningful behavior.

Conceptually:

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

The Framework chooses the supported technical Cinemachine shape for that behavior.

The Inspector must not ask the normal user to assemble arbitrary Position Control and
Rotation Control components manually.

### 4. A model owns a coherent camera pipeline

A supported Presentation Model defines at minimum:

```text
required target roles
optional target roles
position-control semantics
rotation-control semantics
model-specific authoring parameters
materialization rules
validation rules
debug evidence
```

The model is not complete merely because a Position Control component exists.

For example, a Follow rig with a Look At target must have a compatible rotation-control
behavior when the Framework claims that Look At participates.

The product surface must not publish configuration fields whose technical effect is
missing from Apply/Rebuild.

### 5. Position and rotation are distinct technical stages

The architecture explicitly recognizes the Cinemachine pipeline distinction:

```text
Position Control
Rotation Control
```

A model may use:

```text
one Position Control
one Rotation Control
neither stage when authored Transform is the intended behavior
```

but it must define that intentionally.

Apply/Rebuild must not accidentally leave two competing Framework-owned components for
the same pipeline stage.

## Accepted presentation model family

This ADR accepts the following product model family for the single-output Camera product.

New models may initially carry Experimental API status until their technical QA and
FIRSTGAME promotion gates are satisfied.

### 6. Fixed

Product intent:

```text
Fixed
  authored local Camera rig pose
  does not procedurally follow a Tracking target
```

Primary use:

```text
menu shot
room camera
static Activity camera
static Route camera
authored establishing shot
```

Target contract:

```text
Follow / Tracking target
  Not Used

Look At target
  Optional or Required when authored aiming is desired
```

Technical intent:

```text
Position Control
  none — use authored CinemachineCamera Transform

Rotation Control
  none when Look At is Not Used
  supported look-at rotation behavior when Look At participates
```

The first implementation should use a Framework-supported Look At materialization rather
than leaving a configured Look At target with no rotation behavior.

Fixed does not mean persistent output.

It is still one local rig participating in normal request arbitration.

### 7. Follow

`Follow` remains the existing canonical model.

Product intent:

```text
maintain an authored spatial relationship to a Tracking target
with optional target-oriented framing
```

Target contract:

```text
Follow / Tracking target
  Required

Look At
  Optional / Required / Not Used according to Follow authoring
```

Technical intent:

```text
Position Control
  CinemachineFollow

Rotation Control
  none when Look At is Not Used
  supported look-at rotation behavior when Look At participates
```

The existing `FollowOffset` remains valid Follow-model authoring.

The existing serialized numeric value for `CameraRigPresentationIntent.Follow` must not
be changed.

### 8. Mounted

Product intent:

```text
attach the camera pose to one explicit Tracking target
```

Primary use:

```text
first-person camera mount
vehicle cockpit
head/helmet camera
camera socket controlled by gameplay
```

The Framework does not own the gameplay code that moves or rotates the mount.

The target itself is the camera-pose authority supplied by the game/Actor.

Target contract:

```text
Follow / Tracking target
  Required

separate Look At target
  Not Used in the first Mounted contract
```

Technical intent:

```text
Position Control
  Hard Lock to Tracking Target

Rotation Control
  match Tracking Target rotation
```

The consumer may author a dedicated camera mount Transform below an Actor.

Example:

```text
Player Actor
  CameraMount
```

The Framework follows that typed/explicit target.

It does not find a child named `CameraMount`.

Mounted is intentionally broader than naming the architecture `FirstPerson`.

A first-person recipe/preset can use Mounted without making first-person input,
head-bob, weapon camera, recoil or aiming part of Camera authority.

### 9. Third Person

Product intent:

```text
rigid third-person presentation around a rotating Tracking target,
with explicit shoulder/arm/distance framing
```

Primary use:

```text
third-person exploration
over-the-shoulder gameplay
third-person shooter base camera
```

Target contract:

```text
Follow / Tracking target
  Required

separate Look At target
  Not Used in the first Third Person contract
```

The Tracking target may be:

```text
Player camera target
Actor camera pivot
independent aim/orbit pivot controlled by gameplay
```

The Framework does not own input that rotates this target.

Technical intent:

```text
Position / presentation
  CinemachineThirdPersonFollow
```

The accepted first settings surface should include only the parameters needed to make the
model useful and understandable, for example:

```text
Shoulder Offset
Vertical Arm Length
Camera Side
Camera Distance
Damping
```

Collision settings may be exposed when they can be represented safely and validated
without turning the Inspector into a raw Cinemachine mirror.

The exact first subset is an implementation/product cut.

### 10. Presentation intent values are explicit and stable

A likely enum direction is:

```text
Undefined = 0
Follow = 10          // existing value preserved
Fixed = 20
Mounted = 30
ThirdPerson = 40
```

The exact names are frozen by acceptance of this ADR unless implementation review finds
a concrete API conflict before publication.

Numeric value `Follow = 10` is preserved for serialized compatibility.

## Deliberately deferred presentation models

### 11. Orbital / Free Look is not part of the first R4 contract

Cinemachine Orbital Follow can own input axes or be driven by an input-axis controller.

Adding it as a product model would force decisions about:

```text
camera input ownership
PlayerInput integration
input gating
pause behavior
per-Player control
recenter policy
device switching
```

Those are not presentation-only concerns.

Therefore:

```text
Orbital / Free Look
  Deferred
```

until a real game requirement defines the required input authority.

### 12. Spline / Dolly is separate future product work

Spline camera behavior introduces:

```text
Spline ownership
position units
automatic/manual dolly policy
target-to-spline interaction
Activity/Route authoring workflow
```

It is not added merely because Cinemachine provides a component.

### 13. Position Composer / Group Framing are not exposed as raw model names yet

The Framework should add product behavior when there is a demonstrated use case, for
example:

```text
2D Framed Follow
Group Framing
```

rather than exposing every Cinemachine class as an enum value.

### 14. Camera shake, impulse, noise and aim extensions remain orthogonal

Effects such as:

```text
noise
impulse
Third Person Aim
post-processing/volume settings
```

are modifiers/extensions, not primary Presentation Models.

They require separate authoring decisions when promoted.

R4 does not bundle them into the base model taxonomy.

### 15. Multi-output and split-screen remain deferred

R4 must not be used to reopen:

```text
multiple Unity Camera outputs
split-screen
per-Player physical output
multiple CinemachineBrains
output channels as Framework product authority
```

Those remain a separate future contract.

## Target resolution

### 16. Existing typed target-source architecture is retained

The current target source boundary remains useful:

```text
Explicit Transform
Player Composer
Player Slot
Route
Activity
Player Group
```

through typed `ICameraTargetSource` implementations.

R4 does not add:

```text
Camera.main
GameObject.Find
tag lookup
hierarchy-name lookup
first Player
nearest Actor
global target registry
```

### 17. Each model derives valid target requirements

The Inspector must stop presenting target requirement combinations that are nonsensical
for the selected model.

Examples:

```text
Fixed
  Follow requirement is not editable as Required

Mounted
  Follow/Tracking target is always Required
  separate Look At is not part of first contract

Third Person
  Tracking target is always Required
  separate Look At is not part of first contract
```

`Follow` retains the existing configurable Look At requirement.

Existing serialized target requirement fields may be preserved for backward
compatibility, but the product UI should present only model-valid controls.

### 18. Target resolution remains separate from model materialization

The flow is:

```text
CameraRigComposer intent
        ↓
resolve typed targets
        ↓
validate model target contract
        ↓
materialize selected presentation model
```

A materializer must not perform its own scene lookup.

## Materialization authority

### 19. Apply/Rebuild dispatches by explicit presentation model

The current Apply/Rebuild path is specialized to Follow even though the materializer has
a generic-looking name.

R4 changes the conceptual structure to:

```text
CameraRigComposerApplyRebuild
        ↓
Presentation Model
        ↓
explicit typed materialization path
        ├─ Fixed
        ├─ Follow
        ├─ Mounted
        └─ Third Person
```

The dispatch must be explicit.

Acceptable implementation approaches include:

```text
switch on CameraRigPresentationIntent
small internal model-specific editor materializers
typed materialization request structures
```

The implementation must not use runtime reflection or an implicit service registry to
discover camera model handlers.

### 20. Materialization remains Editor-owned

Creation/repair/removal of the technical Cinemachine pipeline remains Editor tooling.

Runtime must not depend on Editor.

The runtime `CameraRigComposer` stores authoring intent and materialized references
required by the accepted product surface.

### 21. One local CinemachineCamera remains canonical per Composer

Each Composer materializes/reuses one local `CinemachineCamera`.

A presentation change does not create multiple hidden virtual cameras and switch between
them internally.

If a game wants two separately arbitrated shots, it authors two rigs and publishes
requests according to ADR-004.

This keeps:

```text
one Composer
  -> one concrete local rig
  -> one CinemachineCamera
```

### 22. Presentation switching must be idempotent

Running Apply/Rebuild repeatedly with unchanged configuration must not continually add,
duplicate or reorder technical components.

Example:

```text
Third Person
Apply
Apply
Apply

result:
  one CinemachineCamera
  one Framework-owned Third Person position behavior
  no duplicate position-control components
```

### 23. Model switching must be safe and ownership-aware

Switching:

```text
Follow
  -> Third Person
```

requires changing the generated technical position pipeline.

The Framework must not delete arbitrary consumer-authored Cinemachine components merely
because they conflict with the selected model.

The materialization layer therefore needs explicit evidence of which technical
components it owns.

Conceptually:

```text
CameraRigComposer
  materialization evidence
    CinemachineCamera
    materialized Presentation Model
    Framework-owned Position Control
    Framework-owned Rotation Control
    materialization revision/version
```

The exact evidence type is an implementation detail.

### 24. Framework-owned technical pipeline may be replaced

When ownership evidence proves a technical component was generated/materialized by this
Composer, Apply/Rebuild may replace that component when the selected model changes.

Example:

```text
owned CinemachineFollow
  removed/replaced
owned CinemachineThirdPersonFollow
  created
```

This is legitimate idempotent materialization.

### 25. Unknown incompatible components block instead of being destroyed

If the local CinemachineCamera contains an incompatible Position/Rotation Control
component that is not proven Framework-owned:

```text
Apply/Rebuild
  -> blocked
  -> diagnostic identifies component and stage
```

It must not silently delete or overwrite the component.

This preserves non-destructive Editor tooling.

### 26. Materialization evidence is visible in Advanced/Debug

The user should be able to inspect:

```text
Presentation Model
CinemachineCamera
Position Control
Rotation Control
Framework-owned / external evidence
resolved targets
last materialization result
blocking conflict
```

The technical graph may be secondary to the designer experience, but it cannot be
irretrievably hidden.

## Complete model semantics

### 27. Fixed model

Expected technical result:

```text
CameraRigComposer
  CinemachineCamera
    Position Control: none
    Rotation Control:
      none
      or supported Look At rotation behavior
```

The CinemachineCamera Transform is authored by the consumer.

Apply/Rebuild must preserve its authored pose.

It must not reset Transform to zero on every rebuild merely because the Camera is a
technical child.

If Apply/Rebuild creates the CinemachineCamera for a new Fixed rig, the creation workflow
must give the designer a clear initial pose and permit normal scene editing afterward.

### 28. Follow model

Expected technical result:

```text
CameraRigComposer
  CinemachineCamera
    CinemachineFollow
    compatible rotation behavior when Look At participates
```

`FollowOffset` belongs only to Follow.

A configured Look At target must not be treated as meaningful evidence if no supported
rotation stage consumes it.

This closes the gap between product intent and technical materialization.

### 29. Mounted model

Expected technical result:

```text
CameraRigComposer
  CinemachineCamera
    hard-lock position behavior
    match-tracking-target rotation behavior
```

The mount Transform is supplied by the target source.

The Framework does not add camera-look input.

Typical Actor authoring:

```text
Actor
  CameraTarget / CameraMount
```

A Player/Actor camera target source may resolve that explicit Transform through the
existing typed camera target contract.

### 30. Third Person model

Expected technical result:

```text
CameraRigComposer
  CinemachineCamera
    CinemachineThirdPersonFollow
```

The component itself derives camera position/orientation from the rotating Tracking
target.

The Framework must not add a second generic rotation controller that conflicts with the
Third Person Follow model's intended orientation semantics unless a later contract
explicitly requires it.

Gameplay input rotates the supplied tracking target/pivot through the game's existing
input/gameplay architecture.

Camera Presentation does not read `PlayerInput` directly.

## Inspector and UX

### 31. Presentation selection becomes designer-editable

The current Inspector disables the Presentation field because only Follow exists.

After R4 implementation:

```text
Presentation
  Fixed
  Follow
  Mounted
  Third Person
```

is the primary designer choice.

### 32. Inspector is model-specific

The default Inspector should show only relevant fields.

Example:

```text
Presentation: Follow

Targets
  Target Mode
  Follow Transform / Target Source
  Look At

Follow Settings
  Follow Offset
  ...

Materialization
  Apply / Rebuild Rig

Validation
  Configuration Status

Advanced / Diagnostics
```

Third Person example:

```text
Presentation: Third Person

Targets
  Target Mode
  Tracking Target / Target Source

Third Person Settings
  Shoulder Offset
  Vertical Arm Length
  Camera Side
  Camera Distance
  Damping
  optional supported collision settings

Materialization
Validation
Advanced / Diagnostics
```

Mounted example:

```text
Presentation: Mounted

Targets
  Camera Mount / Target Source

Mounted Settings
  only settings that have product meaning

Materialization
Validation
Advanced / Diagnostics
```

Fixed example:

```text
Presentation: Fixed

Pose
  edit rig/camera Transform in Scene
Look At
  optional

Materialization
Validation
Advanced / Diagnostics
```

### 33. Do not expose generic Cinemachine graph authoring by default

The default Inspector must not become:

```text
Position Component Type
Rotation Component Type
Extension List
raw component properties
```

That belongs to advanced technical inspection or direct Cinemachine editing outside the
canonical Framework product surface.

### 34. Unity Presets remain the reusable-value mechanism for this product surface

R4 does not introduce a separate Camera Profile asset merely because multiple models
exist.

The accepted ADR-004 posture remains:

```text
CameraRigComposer
  concrete authoring instance

Unity Preset
  reusable authoring values when desired
```

A Framework Camera Recipe/Profile should only be introduced later if real product
friction demonstrates capabilities that Unity Presets cannot express safely.

## Runtime behavior

### 35. Runtime arbitration is presentation-agnostic

A request for:

```text
Fixed rig
Follow rig
Mounted rig
Third Person rig
```

uses the same Camera request contract.

The winner algorithm does not inspect Presentation Model to grant special priority.

### 36. CameraOutputRigApplicator remains presentation-agnostic

The runtime applicator continues to resolve:

```text
winner.Rig.Composer
  -> composer.CinemachineCamera
```

and applies that camera to the single output.

No model-specific branches should be added there unless a concrete future runtime
requirement proves they are necessary.

### 37. Presentation model does not own camera lifetime

A model does not publish/release its own request automatically from `Awake`, `Start` or
`OnEnable`.

Request lifetime remains owned by the existing Session/Route/Activity/Player publishing
surfaces.

### 38. Presentation model does not own Player lifecycle

Mounted/Third Person target sources may point at Player/Actor evidence.

That does not make Camera Presentation authoritative over:

```text
Join
Actor selection
Actor materialization
Initial Placement
Player readiness
Leave
```

A missing required target is an explicit camera configuration/runtime readiness problem,
not permission for Camera to create a Player or Actor.

## Relationship to Initial Placement

### 39. Camera does not place the Actor

IF-ADR-021 owns initial Player/Actor spatial placement when accepted.

R4 only consumes the resulting Actor/camera target after the relevant representation
exists.

The flow may be:

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

A Camera rig must not teleport an Actor to satisfy its framing requirements.

## Validation

### 40. Generic Composer validation

At minimum validate:

```text
Presentation Model is supported
target source component is typed when used
resolved targets satisfy the selected model contract
one local CinemachineCamera is identified/materializable
materialization evidence belongs to this Composer
no ambiguous local CinemachineCamera candidates
no unknown incompatible pipeline component will be destroyed
```

### 41. Fixed validation

Validate:

```text
Follow target is not required
authored Camera pose is valid
Look At requirement is satisfied when configured as Required
Look At rotation behavior can be materialized when Look At participates
```

### 42. Follow validation

Validate:

```text
Tracking/Follow target is present
Follow position behavior exists/materializable
Follow settings are valid
Look At target requirement is satisfied
Look At has a compatible rotation behavior when it participates
```

### 43. Mounted validation

Validate:

```text
Tracking target/mount is present
hard-lock position behavior exists/materializable
matching rotation behavior exists/materializable
separate Look At is not silently accepted in the first contract
```

### 44. Third Person validation

Validate:

```text
Tracking target is present
Third Person Follow behavior exists/materializable
model settings are within valid ranges
no conflicting Framework/external Position Control remains
no implicit input source is required by Camera Presentation
```

### 45. Unsupported intent fails explicitly

An unknown serialized Presentation Model:

```text
does not fallback to Follow
does not leave the old materialization active as if successful
```

Validation/Apply reports an unsupported model.

This follows the Framework no-silent-fallback rule.

## Migration and compatibility

### 46. Existing Follow rigs remain valid

An existing `CameraRigComposer` serialized as:

```text
presentationIntent = Follow
```

continues to mean Follow.

No migration should change its model to another value.

### 47. Existing local CinemachineCamera references are preserved where valid

Apply/Rebuild should reuse the existing explicit/local CinemachineCamera when it satisfies
the Composer ownership rules.

R4 does not require recreating every existing Camera rig.

### 48. Current Follow materialization may require repair to become model-complete

If an existing Follow rig has:

```text
Look At participates
but no compatible Rotation Control exists
```

the new model-complete Apply/Rebuild may add the accepted Framework-owned rotation
behavior.

That is an intentional repair of the declared product configuration.

It must be diagnostic and idempotent.

### 49. Existing consumer-authored Cinemachine components are not silently claimed

A component that predates R4 and has no ownership evidence is external/unknown until the
Framework can prove otherwise.

Migration tooling may offer an explicit adoption path later.

The first R4 implementation may block and explain the conflict rather than guessing
ownership.

## Diagnostics

### 50. Designer-level diagnostic

Example:

```text
CAMERA RIG

Presentation       Third Person
Target Source      Player Slot
Tracking Target    Player1/CameraPivot
Status             Ready
Materialization    Applied
```

### 51. Advanced materialization diagnostic

Example:

```text
CAMERA RIG MATERIALIZATION

Composer            Player Gameplay Camera
Presentation        Third Person
Cinemachine Camera  Cinemachine Camera
Position Control    CinemachineThirdPersonFollow
Rotation Control    Model-owned by Third Person semantics
Tracking Target     Player1/CameraPivot
Look At Target      Not Used
Ownership           Framework materialized
Status              Applied
```

Conflict example:

```text
CAMERA RIG MATERIALIZATION

Presentation        Mounted
Status              Blocked
Conflict            CinemachineOrbitalFollow
Ownership           External / Unknown
Diagnostic          Mounted requires one supported Position Control.
                    The conflicting external component was not removed.
```

### 52. Output diagnostics remain separate

The Composer may report:

```text
rig configuration
targets
materialization
```

while Camera Output diagnostics report:

```text
published requests
winner
previous winner
physical applied CinemachineCamera
transaction status
```

The UI/debug story should make those layers distinguishable.

## Rejected behavior

- Reopening Camera request arbitration for each Presentation Model.
- Creating a second Camera output for Third Person or Mounted.
- Adding split-screen under R4.
- One specialized Composer class per presentation model.
- Exposing arbitrary Cinemachine Position/Rotation component types as the normal product
  contract.
- Runtime reflection to discover presentation handlers.
- Global registry of Camera model materializers.
- Camera Presentation reading `PlayerInput` directly to control Third Person rotation.
- Orbital/Free Look without an explicit input-authority decision.
- `Camera.main` fallback.
- target lookup by object name/tag.
- fallback from unknown model to Follow.
- leaving stale Framework-owned Position Control components after a model switch.
- silently deleting external/unknown Cinemachine pipeline components.
- creating a Unity Camera, CinemachineBrain or AudioListener from CameraRigComposer.
- treating a local CinemachineCamera as Session output authority.
- materializing a Look At target without any supported rotation semantics while claiming
  the model is fully configured.
- adding a CameraProfile asset without demonstrated product need.
- turning R4 into camera shake, aim, impulse, spline, split-screen or cinematic sequencing.

## Deferred

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
camera collision advanced policy beyond the accepted first Third Person surface
Timeline/cinematic sequence authoring
blend policy authoring beyond current output behavior
multi-output
split-screen
per-Player physical output
XR camera authority
```

These require demonstrated product requirements and separate cuts.

## Consequences

### Positive

The Framework gains real gameplay camera variety while preserving the already-certified
Camera authority chain.

A developer can use one consistent workflow:

```text
add CameraRigComposer
choose Presentation
configure relevant intent
Apply / Rebuild
Validate
publish through existing Session/Route/Activity/Player surface
```

The Inspector becomes more useful without becoming a raw Cinemachine graph editor.

First-person/cockpit behavior can be expressed through `Mounted`.

Third-person gameplay gains an explicit supported model.

Static menu/Activity/Route shots gain `Fixed`.

Existing Follow remains compatible.

### Architectural gain

The architecture separates:

```text
Camera authority
  who wins and owns output

Camera request lifetime
  when a rig participates

Camera target source
  what the rig tracks

Camera presentation model
  how the rig behaves

Cinemachine materialization
  which technical components realize that behavior
```

This prevents future camera features from accumulating inside one procedural
`CinemachineFollow` materializer.

### Product cost

The package needs:

```text
model-specific Composer settings
model-specific Inspector sections
typed Editor materialization dispatch
safe ownership evidence
model-specific validators
additional QA
FIRSTGAME camera samples
```

The current Follow materialization needs a compatibility/repair pass for complete
Look At semantics.

## Required architecture reconciliation after acceptance

This draft intentionally does not edit IF-ADR-004 yet.

After acceptance:

```text
IF-ADR-004
  replace "Follow is the only accepted presentation capability"
  with reference to IF-ADR-022
  preserve all output/request authority rules
  keep multi-output explicitly future

IF-ADR-010
  register model-specific CameraRigComposer Inspector behavior
  and safe Apply/Rebuild ownership evidence

Architecture tracking
  close R4 as an accepted presentation-model expansion
```

No existing accepted ADR should be changed until this draft is reviewed.

## Expected implementation cuts

### C1 — Presentation contracts and Composer shape

Objective:

```text
extend CameraRigPresentationIntent
define model-valid target semantics
add serialized model-specific authoring values
preserve Follow compatibility
```

Type:

```text
technical + product authoring
```

Out of scope:

```text
runtime Camera output changes
multi-output
orbital input
```

### C2 — Safe materialization ownership

Objective:

```text
introduce explicit evidence for Framework-owned Camera rig pipeline components
allow idempotent replacement of owned components
block on unknown incompatible components
```

Type:

```text
Editor tooling / technical materialization
```

### C3 — Model materializers

Implement in narrow order:

```text
Follow completion/repair
Fixed
Mounted
Third Person
```

Each model gets explicit technical materialization and validation.

No generic reflection registry.

### C4 — Inspector / UX

Objective:

```text
designer-first presentation selector
model-specific fields
Apply/Rebuild
Validate
Advanced/Diagnostics
```

### C5 — QA

Preserve existing ADR-004 technical suites and add presentation-specific proof.

At minimum:

```text
Follow existing compatibility
Follow Look At complete materialization
Fixed
Mounted
Third Person
switch Follow -> Third Person -> Follow
idempotent rebuild
unknown conflicting component blocks
no external component deletion
no output authority regression
no fallback from unsupported model
```

### C6 — FIRSTGAME

Create real consumer examples showing at least:

```text
Fixed Activity/Route camera
Follow gameplay camera
Mounted first-person/cockpit-style camera
Third Person gameplay camera
runtime override between separate rigs through existing request system
broken configuration diagnostics
```

FIRSTGAME should prove that a user can understand why changing Presentation changes local
rig behavior without changing Camera output authority.

## Technical acceptance

```text
compiles on Unity 6.5 / Cinemachine package used by the Framework
Follow serialized compatibility preserved
one Composer remains canonical
one local CinemachineCamera per Composer
output authority unchanged
request arbitration unchanged
materialization dispatch explicit
no runtime Editor dependency
no runtime reflection required
Apply/Rebuild idempotent
model switching removes/replaces only Framework-owned technical components
unknown external conflicts block explicitly
no silent fallback to Follow
target requirements are model-valid
Look At has real rotation semantics when declared active
existing ADR-004 QA remains green
new presentation QA passes
```

## Product acceptance

```text
user can choose Presentation in CameraRigComposer
Inspector only shows meaningful fields for selected model
user can Apply/Rebuild safely
user can Validate before Play Mode
user can identify generated Position/Rotation controls in Advanced/Debug
Fixed is understandable
Follow remains understandable
Mounted clearly supports first-person/cockpit camera mounts without owning input
Third Person clearly exposes shoulder/arm/distance intent
user can switch models without manually reconstructing Cinemachine components
external conflicting components are not destroyed
short documentation explains Presentation vs Camera Output authority
FIRSTGAME proves at least two gameplay-relevant models in real use
```

## Suggested commits

Architecture:

```text
docs(architecture): define camera rig presentation models
```

Future implementation should be split, for example:

```text
feat(camera): add presentation model contracts
feat(camera-authoring): add safe model materialization evidence
feat(camera-authoring): materialize fixed and mounted camera rigs
feat(camera-authoring): materialize third person camera rigs
qa(camera): prove presentation model materialization
```
