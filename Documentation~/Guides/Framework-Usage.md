# Framework Usage

Status: Current
Last updated: 2026-07-24

## Create and author

1. Create a `GameApplicationAsset`.
2. Assign the ordered Player Slot configuration and explicit product policies
   needed by the game.
3. Create `RouteAsset` and `ActivityAsset` assets for application flow.
4. Configure Route and Activity content, participation, transition and gate
   policies.
5. Create and assign the Persistent Content Scene.
6. Use the package bootstrap surface to start the application.

Required configuration is not repaired through hidden lookup. Missing contracts
produce diagnostics and block the owning operation.

## Persistent Content

The Game Application declares:

```text
Content Scene
```

The scene is the complete visual composition authority.

```text
PersistentContent.unity
  physical Camera output
  Presentation Canvas
  Transition surface
  Loading surface
  optional Player provisioning
  future Audio
  future Lighting or Volumes
```

Prefabs and Prefab Variants may be used inside the scene but are not separately
declared by the Game Application.

### Create the scene

Preferred product flow:

```text
File
  New Scene
    Immersive Persistent Content
```

The official Scene Template provides a minimum starting scene. Assign the created
`.unity` scene to the Game Application, not the Scene Template asset.

Until the official template asset is added, create an equivalent dedicated scene
manually.

### Minimal Persistent Content contract

The Content Scene requires exactly one:

```text
CameraOutputSessionBinding
```

The output binding requires an explicit Output ID and explicit references to its
physical Camera and Brain.

`SessionCameraOverrideBinding` is optional: author zero or one when the
Persistent Content needs to publish a Session-scoped Camera request. Player,
Activity and Route Camera publication use the mandatory output without creating
an implicit Session request.

Transition, Loading and Pause presentation are optional modules. With no
Transition or Loading adapters, their runtimes remain explicit NoOp; no surface,
binding or visual fallback is created.

### Optional presentation modules

Canvas, Transition, Loading and Pause authoring are optional. Add each module
only when the application needs its explicit presentation behavior; their
absence does not block Persistent Content boot.

Game-specific artwork, hierarchy and layout remain consumer-owned.

### Validate

Press:

```text
Validate Configuration
```

Only that explicit action opens and inspects the Content Scene.

Validation checks the scene and contracts present in it. It does not require a
specific prefab, create missing objects or repair configuration.

### Runtime behavior

The framework loads the Content Scene, retains each complete root hierarchy and
unloads the source scene.

```text
content persists
source scene does not
```

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

The host is a composition root, not a public service locator.

## Apply and rebuild

Persistent Content does not expose Apply/Rebuild because its concrete composition
is authored directly through Unity.

Other product surfaces may expose Apply/Rebuild only when they own derived
technical materialization.

## Diagnose

- Run explicit Inspector validation.
- Inspect the stored report under Advanced / Diagnostics.
- Fix the owner that failed to supply a required dependency.
- Do not add name lookup, scene search, fallback objects or static access.

## Manual validation

After applying this cut:

1. Compile the package in Unity 6.5.
2. Open the Game Application Inspector.
3. Confirm Persistent Content exposes only `Content Scene`.
4. Assign a dedicated scene and enable it in the active Build Profile.
5. Run `Validate Configuration`.
6. Confirm no scene inspection occurs merely by repainting the Inspector.
7. Confirm validation accepts manually authored objects and does not require
   prefab instances.
8. Exercise Play Mode and verify the complete scene root hierarchies persist.
