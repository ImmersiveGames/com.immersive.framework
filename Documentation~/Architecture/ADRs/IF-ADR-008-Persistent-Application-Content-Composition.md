# IF-ADR-008 — Persistent Application Content Composition

Status: Accepted
Last updated: 2026-07-25
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
Pause presentation
optional Manager-Provisioned Logical Player setup
optional Session-Persistent Logical Player source
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
does not separately declare Camera, presentation or Logical Player prefabs.

```text
PersistentContent.unity
  Camera output
  Presentation Canvas
    Transition surface
    Loading surface
    Pause surface
  optional Manager-Provisioned Logical Player setup
  optional Session-Persistent Logical Player source
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

## Logical Player boundary

Persistent Content may participate in the Player domain in two distinct ways.

### Manager-Provisioned setup

Persistent Content may contain the explicit technical composition used by
`PlayerInputManager` to create Local Player Hosts after an authorized join request.

That setup does not itself represent the admitted Logical Player. The framework
reserves a Slot, provisions the host and admits the resulting Logical Player into
`PlayerParticipationRuntimeContext`.

### Session-Persistent Logical Player source

Persistent Content may provide a Logical Player that exists at Game
Application/Session scope without belonging to a Route or Activity.

```text
Persistent Content physical composition
  -> Session-Persistent Logical Player source
  -> PlayerParticipationRuntimeContext
  -> typed PlayerSlotId
```

The persistent scene may provide:

```text
Logical Player only
Logical Player + Local Player Host
Logical Player + Actor
Logical Player + Actor materialization/presentation
```

Logical Player, Actor and materialization remain separate contracts. The framework
must compose missing parts and adopt valid provided parts without duplication.

The Content Scene owns the authored physical composition. It is not the logical
participation authority. `PlayerParticipationRuntimeContext` remains the single
Session authority.

Route and Activity may project and consume the Session-Persistent Logical Player,
but they do not own its identity or Session lifetime.

A Scene-Provided Logical Player authored inside a Route or Activity scene remains a
different source defined by `IF-ADR-003`; it is not reclassified as Session
Persistent merely because its object survives temporarily.

The Session-Persistent source is accepted architecture but is not implemented by
the current runtime.

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

Reusable template components must not serialize references to consumer-specific
assets. Session identity is expressed through explicit scoped IDs and runtime
contracts.

### Asset creation boundary

Frozen rule:

```text
Assets do not create other assets.
```

`GameApplicationAsset`, `RouteAsset`, `ActivityAsset`, Recipes, Profiles and
other authoring assets may reference and validate dependencies, but they do not
create scenes, prefabs, templates or sibling assets.

Product assets may expose navigation and validation actions only. Reusable asset
creation belongs to Unity-native creation surfaces, package menus or manually
authored package assets, never to another asset Inspector.

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
Camera, Canvas, adapter or Logical Player source came from a specific prefab.

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
materialize, apply, rebuild, repair or silently replace Persistent Content scene
objects.

Feature-specific runtime modules may still prepare missing contextual Actor or
gameplay content after a Logical Player source has been admitted. That operation is
not Apply/Rebuild over Persistent Content.

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

Presentation and UI input require:

```text
at least one Canvas
at least one ITransitionEffectAdapter
at least one ILoadingSurfaceAdapter
exactly one IPauseSurfaceAdapter
at least one PauseRequestTrigger
at least one authored Resume button
exactly one EventSystem
exactly one InputSystemUIInputModule
```

The EventSystem and Input System UI module are explicit scene content. The module
uses referenced package UI actions and is not created or repaired by a pipeline.

Pause presentation is application-scoped content. The surface projects the logical
`PauseSnapshot`; it does not own Pause state, input, Gate evaluation or
`Time.timeScale`. The persistent scene supplies an authored Resume button through
`PauseRequestTrigger.RequestResume`.

Escape is not supplied by the Persistent Content presentation. It requires an
officially admitted local Logical Player with a compatible Local Player Host
containing:

```text
PlayerInput
LocalPlayerHostAuthoring
UnityPlayerInputGateAdapter
PausePlayerInputBinding
```

The physical host is associated with a logical `PlayerSlotId` by the official
Player lifecycle; a generic host prefab does not serialize a Slot identity.
`Global` is an action map on that PlayerInput, not a separate global Player.

A `PlayerInputManager` in Persistent Content may provision hosts, but it does not by
itself create Logical Player authority, capture Escape or submit Pause. The
framework does not create a fake or duplicate input-only Logical Player when no
admitted Logical Player exists.

Player provisioning and Session-Persistent Logical Player sources remain optional
Persistent Content contracts. They are validated only when their matching product
surface is configured.

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

Physical persistence of a scene object does not automatically admit a Logical
Player. Logical admission must occur through the explicit Player participation
contract.

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
Pause adapter availability
Pause request trigger availability
authored Resume button binding
configured optional Player source contracts
```

The validator opens the scene additively only when requested and closes it only
when the validator owns that temporary load.

`Model Readiness` delegates the same scene-contract proof to the canonical Game
Application validator instead of opening the scene a second time.

The exact Session-Persistent Logical Player validation contract remains pending
until its official authoring component exists.

## Rejected scope

- Camera Output or Presentation Canvas prefab fields in `GameApplicationAsset`.
- Logical Player prefab fields in `GameApplicationAsset`.
- Required prefab identity or Prefab Variant ancestry.
- `PersistentContentRecipe`.
- `PersistentContentComposer`.
- `PersistentContentSource`.
- Generic module lists.
- Automatic scene, prefab or asset creation.
- Asset Inspectors that create sibling assets or scene content.
- Apply/Rebuild over Persistent Content.
- Hidden repair or fallback objects.
- Standalone or duplicate PlayerInput created only to capture application Pause.
- A second Logical Player participation runtime or parallel Slot authority.
- Treating a persistent scene object as an admitted Logical Player without explicit admission.
- Treating a `Global` action map as a global Player authority.
- Scene Template references in runtime configuration.
- Silent fallback Route.
- Premature Audio, Lighting, headless or multi-output contracts.

## Scene Template Pipeline policy

The official Persistent Content template may use an `ISceneTemplatePipeline` only
for non-mutating verification after instantiation.

Allowed:

```text
inspect the newly instantiated scene
validate required contracts
report missing scripts and invalid references
emit diagnostic PASS or ERROR evidence
```

Rejected:

```text
create or delete scene objects
repair references
save the consumer scene
assign consumer assets
edit Build Profile configuration
create or clone assets
```

Template source edits are propagated through an explicit package-maintenance action
that updates the existing SceneTemplateAsset pipeline reference and synchronizes
the required referenced Input System and Pause dependencies.
This action is not part of the GameApplication Inspector or consumer authoring flow.

## Consequences

The product has one concrete source of truth for persistent physical composition:
the Content Scene.

Scene Templates provide a native reusable starting point without making template
or prefab origin part of runtime authority.

Projects remain free to replace, unpack, variant or manually author their scene
content as long as the required contracts remain valid.

Persistent physical composition and Session Logical Player authority remain
separate. A Session-Persistent source can survive Route/Activity changes without
creating a parallel Player participation model.

## Current implementation coverage

The current implementation provides:

```text
scene-only PersistentContentComposition
Game Application Content Scene Inspector
explicit scene-content validation
persistent Camera, Transition, Loading and Pause presentation
runtime requirement reduced to the Content Scene
duplicate Model Readiness scene scan removed
optional Manager-Provisioned setup registration
documentation for the official Scene Template direction
```

The official package source scene is stored under
`Editor/SceneTemplates/PersistentContent`.

The `.scenetemplate` asset is authored from that source through Unity's native
Scene Template workflow, preserving canonical GUID and dependency metadata.

The Session-Persistent Logical Player source, its authoring surface and its
validation/runtime admission contract are not yet implemented.

## Pending decisions

- Official authoring component for Session-Persistent Logical Player.
- Exact admission request/result and physical-ownership evidence.
- Whether the official Persistent Content template includes an optional sample
  source or leaves it entirely consumer-authored.
