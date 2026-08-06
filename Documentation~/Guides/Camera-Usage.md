# Camera Usage

Status: Current  
Last updated: 2026-08-06

Single-output product scope. Split-screen and multiple simultaneous outputs are
out of scope for the current Stable Camera product API.

## Product model

```text
Unity Preset
  optional reusable values for CameraRigComposer

CameraRigComposer
  concrete rig authority
  targets
  requirements
  framing
  Cinemachine materialization

PlayerGameplayCameraAuthoring
  Player camera participation
  requiredness
  Camera Rig reference
  arbitration precedence

PlayerGameplayCameraEligibilityRuntimeContext
  validates the prepared Actor and resolved Composer evidence

CameraOutputSessionBinding
  persistent physical Camera + Brain composition

CameraOutputContext
  selects the active Camera request (Internal output authority)
```

`CameraRigRecipe` is removed. Unity Presets already provide reusable component
configuration without a second framework-owned defaults asset.

Bindings publish requests. Output authority arbitrates them. Apply / Rebuild
never creates a Unity Camera, Cinemachine Brain, AudioListener or Camera Output.

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

Configure the `CameraRigComposer`:

```text
Target Mode
  Explicit Transforms or Target Source Component

Follow Transform
Look At Transform
Follow Requirement
Look At Requirement
Follow Offset
```

Run:

```text
Validate Configuration
Apply / Rebuild Rig
```

A missing local Cinemachine Camera is always created. Existing local Cinemachine
components are reused and repaired idempotently.

This materialization behavior is fixed: the local Camera uses the technical name
`Cinemachine Camera`. Neither creation nor naming is a designer-editable Composer
policy.

Apply / Rebuild never creates:

```text
Unity Camera
CinemachineBrain
AudioListener
CameraOutputSessionBinding
```

Configure `PlayerGameplayCameraAuthoring`:

```text
Requiredness
Camera Rig
Precedence
```

Follow and Look At are not authored again. They are resolved exclusively from
the assigned `CameraRigComposer`.

## Persistent Camera Output Inspector

`CameraOutputSessionBinding` exposes Unity Camera and Cinemachine Brain as the
primary authoring fields. A new component receives a stable Output ID without
replacing IDs already authored.

`Advanced / Diagnostics` contains the stable identity, initialization and logging
settings, read-only runtime evidence and the last explicit validation report.

`Validate Configuration` checks the Output ID, both explicit component references
and the requirement that Unity Camera and Cinemachine Brain share the same
GameObject. Validation never creates, discovers or repairs components.

## Failure behavior

Authoring fails explicitly when:

```text
PlayerGameplayCameraAuthoring is outside a Player Actor
Camera Rig is missing
Camera Rig belongs to another Actor
target source is invalid
required Follow or Look At is missing
resolved target belongs to another hierarchy
Camera Rig configuration is invalid
```

There is no target fallback, `Camera.main` lookup, singleton or service locator.

## Migration

Remove all `CameraRigRecipe` assets from consumer projects. Existing serialized
`recipe` properties on `CameraRigComposer` are obsolete data and disappear when
the prefab or scene is saved with the new component shape.

For reusable Composer values, create a Unity Preset from a configured
`CameraRigComposer`.
