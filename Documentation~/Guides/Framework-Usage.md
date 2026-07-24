# Framework Usage

Status: Current
Last updated: 2026-07-24

## Create and author

1. Create a `GameApplicationAsset`.
2. Assign the ordered Player Slot configuration and explicit product policies
   needed by the game.
3. Create `RouteAsset` and `ActivityAsset` assets for the application flow.
4. Configure startup Route, primary/additive scenes, Activity participation,
   transition and gate policies.
5. Configure the Game Application Persistent Content composition.
6. Use the package bootstrap surface to start the application.

Required configuration is not repaired by hidden lookup. Missing bindings return
typed diagnostics and block the owning operation.

## Persistent Content composition

The Game Application declares:

```text
Container Scene
Camera Output Prefab
Presentation Canvas Prefab
```

The scene is composed manually through normal Unity authoring:

1. Create or open a dedicated Container Scene.
2. Add exactly one instance of the selected Camera Output prefab.
3. Add exactly one instance of the selected Presentation Canvas prefab.
4. Position and configure the hierarchy visually.
5. Add the Container Scene to the active Build Profile.
6. Assign the scene and exact prefab assets in the Game Application.
7. Press `Validate Configuration`.

The framework does not create or repair this scene. Use package minimum prefabs,
Prefab Variants or consumer-owned prefab implementations.

### Camera Output contract

The selected prefab must contain exactly:

```text
Unity Camera
CinemachineBrain
CameraOutputSessionBinding
SessionCameraOverrideBinding
```

The binding must explicitly reference the same physical Camera and Brain and use
an explicit Output ID.

### Presentation Canvas contract

The selected prefab must contain:

```text
one Canvas
at least one Transition adapter
at least one Loading adapter
```

Game-specific artwork and layout remain consumer-owned.

### Runtime behavior

The framework loads the Container Scene, retains each complete authored root
hierarchy and unloads the source scene. The objects persist; the source scene does
not.

## Runtime model

```text
GameApplicationAsset
-> bootstrap
-> Persistent Content load and retention
-> internal FrameworkRuntimeHost
-> Session
-> Route lifecycle
-> Activity lifecycle
-> scoped content and feature modules
```

The host is a composition root, not a public service locator. Runtime components
receive narrow feature ports through bootstrap or lifecycle composition.

## Apply and rebuild

Only product surfaces with derived technical materialization expose Apply/Rebuild.
Camera Rig authoring is one example.

Persistent Content does not expose Apply/Rebuild because its concrete scene and
prefab instances are authored directly through Unity.

## Diagnose

- Start with Inspector validation and Advanced/Diagnostics evidence.
- Use typed result/status/issue fields rather than parsing log text.
- Inspect the current Route, Activity, readiness and feature snapshots.
- Fix the owner that failed to supply a required dependency.
- Do not add name lookup, scene search, fallback objects or static access.

## Real-game boundary

The framework owns lifecycle, scoped content, feature authority and diagnostics.
The game owns objectives, interactions, win/loss rules and content. QAFramework
proves synthetic behavior; FIRSTGAME proves that the official package surface is
usable without consumer facades.

## Manual validation

After runtime or serialized changes:

1. Import and compile the package in Unity.
2. Import and compile QAFramework.
3. Run only the focused QA suites for the affected owner.
4. Exercise startup, Route/Activity exit and re-entry.
5. Verify diagnostics contain no missing binding or retained-scope error.
6. Validate FIRSTGAME when the changed surface is product-facing.
