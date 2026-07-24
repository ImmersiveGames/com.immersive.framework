# Persistent Content Scene Template

Status: Current
Last updated: 2026-07-24

## Authority

```text
Physical source scene
  authored and validated in Unity

Scene Template
  reusable Editor creation surface

Created game scene
  concrete application composition

GameApplicationAsset
  reference, navigation and validation only
```

The Game Application never creates scene objects, scenes, prefabs or templates.

## Frozen rule

```text
Assets do not create other assets.
```

This applies to:

```text
GameApplicationAsset
RouteAsset
ActivityAsset
Recipes
Profiles
Templates referenced by other assets
```

Asset Inspectors may:

```text
edit references
open referenced assets
select referenced assets
run explicit validation
show stored diagnostics
```

They may not:

```text
create sibling assets
create scene content
materialize another asset
repair a referenced asset automatically
save another asset silently
```

## Source-scene workflow

1. Author the physical Persistent Content scene.
2. Add the required Camera and presentation contracts directly in that scene.
3. Save the scene.
4. Assign it temporarily to the Game Application.
5. Run `Validate Configuration`.
6. Correct all blocking issues.
7. Create a `SceneTemplateAsset` from the validated physical scene through Unity's
   native Scene Template workflow.
8. Keep the source scene and template in the official package.
9. Consumer projects create their own Persistent Content scene from that template.
10. The consumer assigns the created `.unity` scene to its Game Application.

## Minimum current source-scene contracts

```text
exactly one Unity Camera
exactly one CinemachineBrain
exactly one CameraOutputSessionBinding
exactly one SessionCameraOverrideBinding

at least one Canvas
at least one ITransitionEffectAdapter
at least one ILoadingSurfaceAdapter
```

The Camera bindings must contain their explicit IDs, output reference, rig,
application owner and target references required by the current validator.

## Scene Template boundary

The template is not runtime authority.

```text
SceneTemplateAsset
  Editor-only reusable source

GameApplicationAsset
  never references it

created .unity scene
  runtime-loadable concrete composition
```

The template must be produced only after the source scene compiles and passes
explicit validation.

## Package asset layout

Recommended final layout:

```text
Samples~ or Templates/
  PersistentContent/
    PersistentContentTemplateSource.unity
    ImmersivePersistentContent.scenetemplate
```

The exact package folder should be chosen when the physical scene and its metadata
are available.

## Validation

Validation remains button-driven:

```text
Inspector repaint
  no scene opening
  no component scan

Validate Configuration
  open scene additively
  inspect contracts
  close only when validator owns the load
```
