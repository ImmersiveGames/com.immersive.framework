# Camera Usage

Status: **Current**  
Last updated: **2026-08-10**

The current Stable Camera product is **single-output**. Split-screen and multiple
simultaneous physical outputs are out of scope.

## Product model

```text
Unity Preset
  optional reusable values for CameraRigComposer

CameraRigComposer
  local rig intent
  targets / requirements / framing
  Cinemachine materialization

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
  internal admitted-request set + deterministic winner
```

`CameraRigRecipe` is removed. Unity Presets provide reusable component values
without another framework-owned defaults asset.

## Author one Player Camera

Inside the Logical Player Actor hierarchy:

```text
Actor
  PlayerActorDeclaration
  PlayerGameplayCameraAuthoring

  Anchors
    CameraTarget
    LookAtTarget

  Player Camera Rig
    CameraRigComposer
```

Configure `CameraRigComposer` with explicit target source, Follow/Look At
requirements and local framing, then run:

```text
Validate Configuration
Apply / Rebuild Rig
```

Apply/Rebuild materializes or repairs only the local Cinemachine rig. It never
creates:

```text
persistent Unity Camera
CinemachineBrain
AudioListener
CameraOutputSessionBinding
```

Configure `PlayerGameplayCameraAuthoring` with Requiredness, Camera Rig and
precedence. Follow/Look At are resolved from the assigned Composer rather than
authored twice.

## Persistent Camera output

`CameraOutputSessionBinding` is the explicit persistent physical output authoring
surface. It references one Unity Camera and one CinemachineBrain on the same
GameObject and owns a stable Output ID.

`Validate Configuration` checks those references and never discovers or repairs
them through fallback lookup.

The current application composition must contain exactly one persistent Camera
output.

## Scoped Camera overrides

Supported scoped publishers include:

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

Higher precedence wins. Equal precedence requires distinct deterministic
Tie-Breaker IDs; timing is never used as hidden priority.

## Lifecycle and abnormal component loss

Route and Activity have **logical owner lifetime** controlled by their canonical
Game Flow lifecycle. Their Camera binding component has a separate publication
lifetime.

If a Route/Activity Camera binding is disabled or destroyed unexpectedly while
its request is published, the binding releases that publication so no orphaned
request remains. A temporary component disable does not synthesize a Route or
Activity exit.

Re-enabling a binding does **not** silently publish another request. Publication
remains explicit while the logical owner is valid.

Session differs because `SessionCameraOverrideBinding` itself owns Session
availability; disable/destroy ends that owner scope and releases its request.

Repeated release/cleanup is idempotent.

## Failure behavior

Authoring/runtime blocks explicitly when mandatory Camera evidence is invalid,
including:

```text
Camera Rig missing or owned by another Actor
invalid target source
required Follow/Look At missing
invalid Camera Rig configuration
wrong OutputId
duplicate RequestId
ambiguous equal-precedence tie-break evidence
missing Unity Camera / CinemachineBrain
invalid persistent single-output composition
```

Physical apply failure does not silently commit logical winner state. The output
session rolls back the mutation; rollback failure is reported explicitly.

There is no target fallback, `Camera.main` authority, singleton, service locator,
name lookup or hierarchy guessing.

## Diagnostics

Advanced / Diagnostics may expose:

```text
Output ID
Request ID
Owner / Lifetime
Precedence / Tie-Breaker
Admitted request set
Current winner
Physical apply result
Rollback attempt/result
Blocking issue code/message
```

Use these diagnostics to inspect the explicit authority chain rather than adding
local Camera-selection logic in a consumer project.

## Migration

Remove legacy `CameraRigRecipe` assets. Obsolete serialized recipe data disappears
when prefabs/scenes are saved with the current component shape.

For reusable Composer values, create a Unity Preset from a configured
`CameraRigComposer`.
