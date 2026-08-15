# Camera Usage

Status: **Current / ADR-022 Technical QA Certified**  
Last updated: **2026-08-15**

The current Camera product is **single-output**.

Supported local Presentation Models are:

```text
Fixed
Follow
Mounted
Third Person
```

Split-screen and multiple simultaneous physical outputs remain out of scope.

Technical certification:

```text
Full Camera QA
  53/53
  CAMERA QA CERTIFIED
```

FIRSTGAME consumer proof for the expanded presentation family remains a separate
promotion step.

## 1. Product model

```text
Unity Preset
  optional reusable values for CameraRigComposer

CameraRigComposer
  one local rig
  Presentation Model
  typed targets / model-valid requirements
  model-specific settings
  Cinemachine materialization
  materialization provenance
  diagnostics

PlayerGameplayCameraAuthoring
  Player Camera participation
  requiredness
  Camera Rig reference
  arbitration precedence

Session / Route / Activity Camera Override bindings
  explicit scoped Camera publication

CameraOutputSessionBinding
  persistent physical Unity Camera + CinemachineBrain

CameraOutputSession
  transactional logical/physical mutation

CameraOutputContext
  admitted-request set + deterministic winner
```

`CameraRigRecipe` remains removed.

Unity Presets provide reusable Composer values without introducing another
Framework-owned defaults asset.

## 2. Presentation is not Camera Output

Keep these layers separate:

```text
Presentation Model
  how one local rig behaves

Camera Request
  when that rig participates

Camera Output
  which admitted rig wins and is physically projected
```

Changing:

```text
Follow -> Third Person
```

does not change arbitration policy or create a second physical output.

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
CameraOutputSessionBinding
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

The supported typed target architecture includes:

```text
Explicit Transform
Player Composer
Player Slot
Route
Activity
Player Group
```

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

`CameraOutputSessionBinding` is the explicit persistent physical output authoring
surface.

It references:

```text
one Unity Camera
one CinemachineBrain
same physical output GameObject
one stable Output ID
```

`Validate Configuration` checks those explicit references.

The current application composition must contain exactly one persistent Camera
output.

Duplicate persistent outputs are invalid.

## 12. Scoped Camera overrides

Supported publishers:

```text
Session Camera Override
Route Camera Override
Activity Camera Override
eligible Local Player Camera publication
```

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

## 13. Player Camera authoring

Inside the Logical Player Actor hierarchy a game may use:

```text
Actor
  PlayerActorDeclaration
  PlayerGameplayCameraAuthoring

  Anchors
    CameraTarget
    LookAtTarget / CameraPivot / CameraMount

  Player Camera Rig
    CameraRigComposer
```

The exact target Transform depends on Presentation.

Configure `PlayerGameplayCameraAuthoring` with Requiredness, Camera Rig and
precedence.

Target resolution belongs to the assigned Composer/typed source and is not
authored twice as an implicit output override.

Camera does not own Player Join, Actor creation, Initial Placement or Leave.

## 14. Lifecycle and abnormal component loss

Route and Activity have logical owner lifetime controlled by Game Flow.

Their Camera binding component has a separate publication lifetime.

Unexpected disable/destroy of a published Route/Activity binding releases only
that publication so no orphaned request remains.

It does not synthesize Route/Activity exit.

Re-enable does not silently publish another request.

Session differs because `SessionCameraOverrideBinding` itself owns Session
availability.

Repeated cleanup is idempotent.

## 15. Failure behavior

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
duplicate persistent output
physical apply failure
rollback failure
```

An unknown Presentation never falls back to Follow.

A local materialization failure does not create/alter persistent output
authority.

## 16. Diagnostics

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
Request ID
Owner / Lifetime
Precedence / Tie-Breaker
admitted request set
current winner
physical apply result
rollback evidence
```

Use the appropriate layer rather than adding local Camera-selection logic.

## 17. Reusable authoring

For reusable Composer values, create a Unity Preset from a configured
`CameraRigComposer`.

Do not create a new Framework Camera Profile merely for symmetry.

## 18. Deliberately deferred Camera features

Not part of the current accepted presentation family:

```text
Orbital / Free Look
camera input-axis authority / recenter
Spline / Dolly
Group Framing
2D Framed Follow
noise / shake / impulse product authoring
Third Person Aim
advanced collision policy
Timeline/cinematic sequencing
advanced blend policy
multi-output
split-screen
per-player physical output
XR Camera authority
```

These require separate product requirements.

## 19. Technical certification

Current aggregate result:

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

The existing Follow pipeline also passes its supporting `6/6` smoke.

## 20. FIRSTGAME boundary

The package/editor Camera boundary is technically complete for IF-ADR-022 C1-C5.

FIRSTGAME C6 remains the consumer proof for:

```text
Fixed in a real Route/Activity
Follow gameplay
Mounted gameplay/cockpit or first-person mount
Third Person gameplay
runtime request overrides between rigs
understanding broken configuration diagnostics
```

Consumer friction may justify later product refinement. It does not by itself
invalidate the current technical certification.
