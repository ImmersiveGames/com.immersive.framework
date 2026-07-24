# IF-ADR-008 — Persistent Application Content Composition

Status: Accepted
Last updated: 2026-07-24
Supersedes: ad-hoc `UIGlobal` scene policy and path/name authoring
Superseded by: none

## Context

An Immersive game needs application-scoped content that survives Route and
Activity scene changes. The current implementation calls this content
`UIGlobal`, although the actual composition already includes more than UI:

```text
physical Camera output
Transition presentation
Loading presentation
optional Player provisioning
future global Audio
future application-scoped Lighting or Volume content
```

A dedicated Unity scene remains useful because it allows the developer to author
and inspect the concrete composition visually. Canvas anchors, Camera transforms,
Audio placement and future lighting content must remain normal Unity authoring.

The scene asset itself is not the runtime lifetime authority. The framework loads
it as a source container, retains its root object hierarchies and unloads the
source scene.

## Decision

`GameApplicationAsset` is the single authoring authority for one concrete
Persistent Content composition:

```text
Persistent Content
  Container Scene
  Camera Output Prefab
  Presentation Canvas Prefab
```

The serialized group is `PersistentContentComposition`.

### Container Scene

The Container Scene is stored as one direct Unity asset reference. The public
model does not serialize separate path and cached-name fields.

The scene is the visual composition boundary. Every authored root belongs to the
application-persistent content set.

```text
PersistentContent.unity
  Camera Output prefab instance
  Presentation Canvas prefab instance
  future Audio objects
  future Lighting or Volume objects
  other explicitly persistent application content
```

The framework does not require a named root, `_Framework` container, Composer or
module marker.

### Prefab references

`Camera Output Prefab` and `Presentation Canvas Prefab` identify the exact prefab
implementations expected in the Container Scene.

They may reference:

```text
package-provided minimum prefabs
Prefab Variants in the consumer project
studio package prefabs
consumer-owned prefabs
```

The framework uses these references for authoring validation. It does not
instantiate them automatically.

### Manual composition

The developer uses the normal Unity workflow:

```text
create or open the Container Scene
add the selected prefab instances
position and configure them visually
use Prefab Variants or normal overrides where appropriate
validate the Game Application
```

The framework validates and executes the composition. It does not create,
materialize, apply, rebuild, repair or silently replace it.

## Current required modules

The current playable-client baseline requires:

```text
exactly one selected Camera Output prefab instance
exactly one selected Presentation Canvas prefab instance
```

The Camera Output prefab must provide exactly:

```text
one Unity Camera
one CinemachineBrain
one CameraOutputSessionBinding
one SessionCameraOverrideBinding
```

The Presentation Canvas prefab must provide:

```text
one Canvas
at least one ITransitionEffectAdapter
at least one ILoadingSurfaceAdapter
```

The physical Camera requirement exists because the current product architecture
owns one application/session output. It is not a dependency of a Screen Space
Overlay Canvas.

## Runtime lifetime

Runtime loads the Container Scene additively from the direct scene reference,
collects its root objects, transfers each complete root hierarchy to Unity's
persistent runtime scene and unloads the source scene.

Runtime must preserve:

```text
parent-child hierarchy
RectTransform anchors
local transforms
internal object references
Prefab instance structure
visual and spatial composition
```

Runtime must not flatten the hierarchy, discover modules by object name or
silently create missing required objects.

The current runtime resolves the build-loadable scene by its direct reference
name. Editor validation therefore requires that name to be unique among enabled
Build Profile scenes.

## Validation

Validation is explicit and non-mutating.

It checks:

```text
Container Scene direct reference
Build Profile inclusion
unique enabled build scene name
Camera Output prefab contract
Presentation Canvas prefab contract
exact selected prefab instances in the Container Scene
unique physical Camera/session output bindings
Transition and Loading adapter availability
```

Additional authored objects are allowed. Future Audio and Lighting modules enter
only after their ownership contracts are defined.

## Rejected scope

- `PersistentContentRecipe` asset.
- `PersistentContentComposer` component.
- `PersistentContentSource` marker.
- Generic module enum or module list.
- `_Framework` materialization container.
- Automatic asset, scene or prefab creation.
- Apply/Rebuild over Persistent Content.
- Hidden repair or fallback objects.
- Separate serialized scene path and cached scene name.
- Silent fallback Route when a destination scene fails.
- Premature Audio, Lighting, headless or multi-output contracts.

## Framework Runtime Scene

A future internal Framework Runtime Scene may provide a valid active-scene context
during bootstrap and explicit gaps between Route Primary Scenes.

That scene is not part of this cut. It will be a technical safety context, not a
fallback Route and not a way to hide destination-load failure.

## Consequences

The product surface becomes smaller and uses native Unity authoring directly.
Prefab reuse remains available through package assets and Prefab Variants without
a second composition tool.

The Container Scene clearly communicates what will persist while runtime lifetime
remains separate from the source scene asset.

## Current implementation coverage

`PERSISTENT-COMPOSITION-1` introduces:

```text
PersistentContentComposition
GameApplication Persistent Content Inspector section
direct Container Scene reference
explicit prefab references
non-mutating prefab and scene validation
runtime retention of complete root hierarchies
updated Persistent Content diagnostics
```

Minimum package prefabs, Audio, Lighting and Framework Runtime Scene are subsequent
cuts.
