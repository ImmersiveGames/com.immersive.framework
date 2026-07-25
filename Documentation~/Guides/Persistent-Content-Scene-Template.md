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
exactly one EventSystem
exactly one InputSystemUIInputModule
exactly one IPauseSurfaceAdapter

at least one Canvas
at least one ITransitionEffectAdapter
at least one ILoadingSurfaceAdapter
at least one PauseRequestTrigger
at least one authored Resume Button
```

The Camera bindings must contain their explicit IDs, output reference, rig and
target references required by the current validator.

The EventSystem and InputSystem UI module live on the same root GameObject. The UI
module references the Input System package's built-in `DefaultInputActions`, so the
template remains consumer-neutral while still providing Point, Left Click, Scroll,
Move, Submit and Cancel actions explicitly.

`SessionCameraOverrideBinding` intentionally does not reference a consumer
application asset. Session ownership is already explicit through its Scope ID,
which keeps the source scene reusable across projects.


## Persistent Pause presentation

The official source scene contains:

```text
Persistent Presentation
  GlobalCanvas
    PauseSurface
      Visual
        Pause Panel
          Pause Title
          Resume Button
```

The surface uses:

```text
UnityPauseSurfaceAdapter
  CanvasGroup
  explicit Surface Root
  initial Running presentation on Awake

Resume Button
  Button.OnClick
    PauseRequestTrigger.RequestResume
```

Authority remains separated:

```text
PausePlayerInputBinding
  reads the canonical Pause action from the admitted PlayerInput

PauseRuntime
  owns logical Running / Paused state

PauseSurfaceRuntime
  projects PauseSnapshot to persistent adapters

UnityPauseSurfaceAdapter
  mutates only the configured presentation hierarchy

PauseRequestTrigger
  submits an authored Resume request
```

The Pause surface does not read Escape, own `Time.timeScale`, select action maps,
resolve a Player or create UI content. Escape continues through the canonical
`PausePlayerInputBinding` attached to an officially admitted PlayerInput.

### Pause presentation does not provide Escape input

The Scene Template provides:

```text
Pause presentation
PauseSnapshot projection
Resume button
PauseRequestTrigger request surface
```

It does not provide a standalone keyboard listener or an input-only Player. For
Escape to work, the game must have an officially admitted local Player host:

```text
Local Player Host
  PlayerInput
  LocalPlayerHostAuthoring
  UnityPlayerInputGateAdapter
  PausePlayerInputBinding
```

The Player lifecycle associates that physical host with a logical `PlayerSlotId`
at runtime. The Player prefab does not serialize its own Slot identity. The
`Global` map is an action map inside the same PlayerInput action asset, not a
separate global Player.

```text
PlayerInput actions
  Global
    PauseToggle
      Escape
```

Without an admitted Player host, Escape does not submit Pause. Direct authored
buttons using `PauseRequestTrigger.RequestPause`, `RequestResume` or `TogglePause`
remain valid because they use the product request port rather than Player input.
A `PlayerInputManager` may provision the Player host, but the manager itself is
not the Escape listener.

The source surface starts hidden and non-interactive. When the Pause snapshot is
`Paused`, it becomes visible, interactable and raycast-blocking. When the snapshot
returns to `Running`, it hides and releases UI interaction.

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

```text
Editor/SceneTemplates/PersistentContent/
  PersistentContentTemplateSource.unity
  ImmersivePersistentContent.scenetemplate
```

The source scene is Editor-only package content. Consumer projects instantiate a
normal runtime `.unity` scene from the template.

The core source scene is render-pipeline-neutral. Render-pipeline-specific Camera
components belong to explicitly scoped template variants or consumer composition.

## Scene Template Pipeline

The official template uses:

```text
PersistentContentSceneTemplatePipeline
```

The pipeline is verification-only:

```text
BeforeTemplateInstantiation
  no mutation

AfterTemplateInstantiation
  validate the instantiated scene
  log PASS or explicit contract errors
```

It checks the same Camera, transition, loading, Pause presentation and EventSystem
contracts used by explicit Game Application validation. It also reports missing
scripts, verifies the authored Resume button binding and rejects a legacy
`StandaloneInputModule` in the persistent composition.

The pipeline never:

```text
creates GameObjects
repairs references
saves the new scene
assigns a GameApplication
adds the scene to the Build Profile
creates or clones assets
```

## Refreshing the official template

After editing the package source scene, run:

```text
Tools
  Immersive Framework
    Package Maintenance
      Refresh Persistent Content Template
```

This explicit package-maintenance action:

```text
validates the existing source scene
binds the existing pipeline script
synchronizes the required referenced Input System and Pause dependencies
saves the existing SceneTemplateAsset
```

It does not create any asset. The action must run from the editable framework
package repository; Git-installed read-only package copies are rejected explicitly.

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


## Creating the Scene Template asset

Select:

```text
Editor/SceneTemplates/PersistentContent/
  PersistentContentTemplateSource.unity
```

Use Unity's native command:

```text
Assets
  Create
    Scene Template From Scene
```

Save the generated asset beside the source scene as:

```text
ImmersivePersistentContent.scenetemplate
```

Configure:

```text
Title:
  Immersive Persistent Content

Description:
  Application-persistent Camera, Transition, Loading and Pause presentation
  composition for the Immersive Framework.

Pin in New Scene Dialog:
  enabled
```

Keep all dependencies referenced rather than cloned. The source scene depends on
framework runtime scripts and Cinemachine implementations that must continue
referencing their package assets.

After the source scene changes, use the explicit package-maintenance refresh action
so the existing `.scenetemplate` records the pipeline plus the referenced Input
System and Pause dependencies. Then create a new scene from the template, confirm the
Pause hierarchy is present and confirm the pipeline logs a passing instantiation
report.
