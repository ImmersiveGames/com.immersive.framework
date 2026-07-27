# Player Gameplay Camera Authoring

Status: Current  
Last updated: 2026-07-26

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

CameraOutputContext
  selects the active Camera request
```

`CameraRigRecipe` is removed. Unity Presets already provide reusable component
configuration without introducing a second framework-owned defaults asset.

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

A missing local Cinemachine Camera is always created. Existing local
Cinemachine components are reused and repaired idempotently.

This materialization behavior is fixed: the local Camera uses the technical
name `Cinemachine Camera`. Neither creation nor naming is a designer-editable
Composer policy.

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

There is no target fallback, Camera.main lookup, singleton or service locator.

## Migration

Remove all `CameraRigRecipe` assets from consumer projects. Existing serialized
`recipe` properties on `CameraRigComposer` are obsolete data and disappear when
the prefab or scene is saved with the new component shape.

For reusable Composer values, create a Unity Preset from a configured
`CameraRigComposer`.
