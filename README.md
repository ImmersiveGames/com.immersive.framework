# Immersive Framework

`com.immersive.framework` is the official Unity package for framework runtime, authoring, diagnostics and validation.

Current version: `1.0.0-preview.14`.

## Current product surfaces

```text
PlayerRecipe (optional) -> PlayerComposer -> Validate -> Apply/Rebuild
PlayerComposer -> CameraComposer -> Validate -> Apply/Rebuild
```

`CameraComposer` materializes the main Unity/Cinemachine camera rig from explicit references. Route/Activity camera bindings are lifecycle-specific explicit-output integration and currently apply on enter without automatic release/restoration on exit.

`PlayerViewBehaviour` and the passive Player foundation remain technical evidence. A complete PlayerControl runtime, input activation, movement and spawning are not automatic.

## Next active lane

`P2 — Player Control Product`.

## Start reading

- [Documentation index](Documentation~/README.md)
- [HTML Usage Guide](Documentation~/Guides/Usage/index.html)
- [Current State](Documentation~/Current/00-Current-State.md)
- [Roadmap](Documentation~/Current/01-Roadmap.md)
- [Camera Product Usage](Documentation~/Guides/Camera-Product-Usage.md)

QAFramework owns synthetic technical validation. FIRSTGAME owns minimal real-game proof. Do not copy consumer assets or old project architecture into this package.
