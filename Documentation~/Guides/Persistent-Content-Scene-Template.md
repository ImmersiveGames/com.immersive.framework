# Persistent Content Scene Template

Status: Current
Last updated: 2026-08-16

## Purpose

Persistent Content uses Unity Scene Templates as an explicit Editor authoring
surface for application-persistent composition.

The current package ships a **minimal baseline template**. Additional templates
covering optional persistent presentation modules such as Pause, Loading and
Transition are expected future product work; they are not part of the current
baseline and are not required by the Persistent Content contract.

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

## Current template baseline

The current official template is intentionally minimal.

```text
Persistent Camera
├── Camera Output
│   ├── Camera
│   ├── CinemachineBrain
│   ├── CameraOutputSessionBinding
│   └── SessionCameraOverrideBinding
├── Session Camera Target
└── Session Camera Rig
    ├── CinemachineCamera
    ├── CinemachineFollow
    ├── CinemachineRotationComposer
    └── CameraRigComposer

EventSystem
├── EventSystem
└── InputSystemUIInputModule
```

The canonical persistent Camera Output ID is:

```text
camera.output.main
```

The current baseline does **not** include:

```text
Global Canvas
Transition surface
Loading surface
Pause surface
other game-specific persistent presentation
```

Their absence does not make the template incomplete. Those systems remain
optional composition and explicit NoOp where the corresponding product contract
allows it.

## Minimum current source-scene contracts

The minimal template requires:

```text
exactly one CameraOutputSessionBinding
exactly one EventSystem
exactly one InputSystemUIInputModule
zero or one SessionCameraOverrideBinding
```

The Camera Output contains its explicit Output ID and references to the physical
Unity Camera and Cinemachine Brain.

`SessionCameraOverrideBinding` remains optional. Omit it when Persistent Content
does not need a Session-scoped Camera request. Player, Activity and Route Camera
publication continue to use the explicit output without requiring an implicit
Session request.

When authored, `SessionCameraOverrideBinding` intentionally does not reference a
consumer application asset. Session ownership is explicit through its Scope ID,
which keeps the template reusable across projects.

The EventSystem and Input System UI module live on the same root GameObject.

## Source-scene workflow

1. Maintain the physical package source scene for the desired template.
2. Validate the contracts owned by that template.
3. Create or refresh the `SceneTemplateAsset` through explicit Editor tooling.
4. Consumer projects create their own Persistent Content `.unity` scene from the
   template.
5. The consumer saves and owns that concrete scene.
6. Assign the concrete scene to `GameApplicationAsset > Persistent Content > Content Scene`.
7. Add or enable that scene explicitly in the active Build Profile Scene List.
8. Run explicit validation and Play Mode integration proof as required by the
   consumer project.

The Scene Template itself is never a runtime reference from `GameApplicationAsset`.

## Planned template family

The minimal template is the baseline, not the intended maximum product surface.

Future authoring work may add dedicated Persistent Content template variants for
commonly reused optional modules, including:

```text
Pause presentation
Loading presentation
Transition presentation
combined persistent presentation compositions
other reusable persistent framework modules when a concrete product need exists
```

The exact variant names, combinations and delivery order are **not frozen** by
this guide.

The important architectural rule is that future variants extend authoring
convenience without changing runtime authority or making optional modules
mandatory.

A future variant must preserve:

```text
Scene Template
  Editor-only reusable creation surface

Created .unity scene
  consumer-owned runtime composition

GameApplicationAsset
  references the concrete scene only

Template pipeline
  verifies the contracts owned by that variant
  does not silently materialize or repair consumer content
```

A more complete template therefore does not supersede the minimal template.
Consumers should be able to choose the smallest authored composition that matches
their game.

## Future optional presentation templates

Pause, Loading and Transition already have framework runtime/product contracts,
but inclusion of their persistent visual composition in Scene Templates is a
separate Editor authoring concern.

When those template variants are implemented, each one should explicitly define:

```text
owned hierarchy
required adapters/bindings
required references
validation invariants
consumer-neutral dependencies
what remains optional
```

They must not introduce:

```text
silent scene repair
implicit GameApplication assignment
implicit Build Profile mutation
runtime lookup by hierarchy/name
hidden gameplay intent
mandatory presentation for games that do not use it
```

The current minimal template must not be expanded opportunistically merely because
those modules exist. New variants should be introduced as deliberate product cuts.

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

The template should be produced only after the source scene compiles and passes
its explicit validation.

## Package asset layout

Current baseline:

```text
Editor/SceneTemplates/PersistentContent/
  PersistentContentTemplateSource.unity
  ImmersivePersistentContent.scenetemplate
  PersistentContentSceneTemplatePipeline.cs
```

The source scene is Editor-only package content. Consumer projects instantiate a
normal runtime `.unity` scene from the template.

Future template variants should remain under an explicit Persistent Content
Scene Template product surface rather than being generated by runtime assets.

The core source scene is render-pipeline-neutral. Render-pipeline-specific Camera
components belong to explicitly scoped template variants or consumer composition.

## Scene Template pipeline

The official baseline uses:

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

For the current minimal template it validates the Camera and EventSystem contracts
owned by that baseline.

Future template variants may validate additional contracts only when those
contracts are actually authored by the selected variant.

The pipeline never:

```text
creates GameObjects
repairs references
saves the new scene
assigns a GameApplication
adds the scene to the Build Profile
creates or clones assets
```

## Runtime evidence

The current minimal template has been instantiated into a concrete consumer
Persistent Content scene and exercised in Play Mode.

Observed integration evidence includes successful framework boot with the
persistent composition materialized for application lifetime, including the
Persistent Camera structure and EventSystem. In the current implementation those
objects were observed under Unity's `DontDestroyOnLoad` scene.

`DontDestroyOnLoad` is implementation evidence, not the authoring authority of the
Scene Template. The architectural contract is application-persistent lifetime and
scoped runtime authority.

## Validation

Validation remains button-driven:

```text
Inspector repaint
  no scene opening
  no component scan

Validate Configuration
  open scene additively when required
  inspect contracts
  close only when validator owns the load
```

Validation reports invalid authored state. It does not silently repair it.

## Product direction summary

```text
CURRENT
  Minimal Persistent Content template
    Camera / Session Camera structure
    EventSystem

PLANNED, NOT YET IMPLEMENTED AS TEMPLATE VARIANTS
  Pause composition
  Loading composition
  Transition composition
  useful combined variants

ALWAYS
  concrete .unity scene is the game product
  template is Editor-only authoring convenience
  optional modules remain optional
  pipeline verifies and does not materialize/repair
```
