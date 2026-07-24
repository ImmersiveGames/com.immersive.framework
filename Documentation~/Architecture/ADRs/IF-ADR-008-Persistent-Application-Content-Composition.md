# IF-ADR-008 — Persistent Application Content Composition

Status: Accepted
Last updated: 2026-07-24
Supersedes: ad-hoc `UIGlobal` scene policy and path/name authoring
Superseded by: none

## Context

An Immersive game needs application-scoped content that survives Route and
Activity scene changes. The previous `UIGlobal` vocabulary described only part
of the actual composition:

```text
physical Camera output
Transition presentation
Loading presentation
optional Player provisioning
future global Audio
future application-scoped Lighting or Volume content
```

A dedicated Unity scene is the clearest authoring boundary because Camera
placement, Canvas anchors, Audio placement, Volumes, lighting and project-specific
persistent objects remain visible and editable together.

The scene asset is the authoring source, not the runtime lifetime authority.
Runtime loads it, retains its complete root hierarchies and unloads the source
scene.

## Decision

`GameApplicationAsset` is the single application-level authority for:

```text
Persistent Content
  Content Scene
```

The serialized group remains `PersistentContentComposition`.

The Content Scene is the complete concrete composition. `GameApplicationAsset`
does not separately declare Camera or presentation prefabs.

```text
PersistentContent.unity
  Camera output
  Presentation Canvas
  optional Player provisioning
  future Audio
  future Lighting or Volumes
  other explicitly persistent application content
```

The framework does not require:

```text
named roots
_Framework container
Composer
module markers
Recipe
prefab identity
```

## Scene Template

The package may distribute an official Persistent Content `SceneTemplateAsset`
and its source scene.

The Scene Template is an Editor creation surface:

```text
File
  New Scene
    Immersive Persistent Content
```

The created `.unity` scene becomes the application composition and is assigned to
`GameApplicationAsset`.

The Game Application never references the `SceneTemplateAsset` itself.

The template may use package prefabs, normal GameObjects or other Unity assets.
After scene creation, those objects are ordinary Unity authoring content.

## Prefabs

Prefabs remain optional building blocks inside the Content Scene.

Valid implementations may originate from:

```text
package minimum prefabs
Prefab Variants
studio packages
consumer-owned prefabs
manually authored scene objects
```

Validation proves the contracts present in the scene. It does not require that a
Camera, Canvas or adapter came from a specific prefab.

## Manual authoring

The developer uses the native Unity workflow:

```text
create a scene from the official Scene Template
or create an equivalent scene manually

edit hierarchy, positions, anchors and overrides
assign the created scene to GameApplication
enable it in the active Build Profile
run Validate Configuration
```

The framework validates and executes the composition. It does not create,
materialize, apply, rebuild, repair or silently replace content.

## Current required scene contracts

The playable-client baseline requires exactly:

```text
one Unity Camera
one CinemachineBrain
one CameraOutputSessionBinding
one SessionCameraOverrideBinding
```

`CameraOutputSessionBinding` must declare:

```text
explicit Output ID
explicit Unity Camera reference
explicit CinemachineBrain reference
Camera and Brain on the same physical output GameObject
```

Presentation requires:

```text
at least one Canvas
at least one ITransitionEffectAdapter
at least one ILoadingSurfaceAdapter
```

Additional authored objects are allowed.

Future Audio and Lighting contracts enter only after their ownership and runtime
authority are explicit.

## Runtime lifetime

Runtime loads the Content Scene additively, retains every complete root hierarchy
through Unity's persistent runtime lifetime and unloads the source scene.

Runtime preserves:

```text
parent-child hierarchy
RectTransform anchors
local transforms
internal references
Prefab instance relationships
visual and spatial composition
```

Runtime must not flatten hierarchy, discover modules by object name or silently
create missing required objects.

The current runtime resolves the build-loadable scene by its directly referenced
scene name. Editor validation therefore requires that name to be unique among
enabled Build Profile scenes.

## Validation

Validation is explicit, button-driven and non-mutating.

Inspector repaint does not open or inspect the Content Scene.

`Validate Configuration` checks:

```text
direct Content Scene reference
scene asset validity
Build Profile inclusion
unique enabled build scene name
Camera output component counts and bindings
Canvas availability
Transition adapter availability
Loading adapter availability
```

The validator opens the scene additively only when requested and closes it only
when the validator owns that temporary load.

`Model Readiness` delegates the same scene-contract proof to the canonical Game
Application validator instead of opening the scene a second time.

## Rejected scope

- Camera Output or Presentation Canvas prefab fields in `GameApplicationAsset`.
- Required prefab identity or Prefab Variant ancestry.
- `PersistentContentRecipe`.
- `PersistentContentComposer`.
- `PersistentContentSource`.
- Generic module lists.
- Automatic scene, prefab or asset creation.
- Apply/Rebuild over Persistent Content.
- Hidden repair or fallback objects.
- Scene Template references in runtime configuration.
- Silent fallback Route.
- Premature Audio, Lighting, headless or multi-output contracts.

## Consequences

The product has one concrete source of truth: the Content Scene.

Scene Templates provide a native reusable starting point without making template
or prefab origin part of runtime authority.

Projects remain free to replace, unpack, variant or manually author their scene
content as long as the required contracts remain valid.

## Current implementation coverage

`PERSISTENT-COMPOSITION-SCENE-ONLY-1` provides:

```text
scene-only PersistentContentComposition
Game Application Content Scene Inspector
explicit scene-content validation
runtime requirement reduced to the Content Scene
duplicate Model Readiness scene scan removed
documentation for the official Scene Template direction
```

The actual package `.unity` source scene and `.scenetemplate` asset must be authored
and saved through Unity in the next asset cut so Unity writes their canonical GUID
and dependency metadata.
