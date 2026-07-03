# Immersive Framework

`com.immersive.framework` is the Unity package for Immersive Framework runtime, authoring, diagnostics and QA surfaces.

Package version: `1.0.0-preview.1`

Source repository: `https://github.com/ImmersiveGames/com.immersive.framework`

## Install from Git

Install private Immersive package dependencies in the consumer project's `Packages/manifest.json` before installing the framework package:

```json
{
  "dependencies": {
    "com.immersive.foundation": "https://github.com/ImmersiveGames/com.immersive.foundation.git#v1.0.0-preview.1",
    "com.immersive.logging": "https://github.com/ImmersiveGames/com.immersive.logging.git#v1.0.0-preview.1",
    "com.immersive.framework": "https://github.com/ImmersiveGames/com.immersive.framework.git#v1.0.0-preview.1"
  }
}
```

Unity package manifests do not support Git URLs as transitive dependencies inside `package.json`. `com.immersive.framework/package.json` therefore declares package/version dependencies only. If `com.immersive.foundation` and `com.immersive.logging` are not available from a scoped registry, the consumer project must install those sibling packages directly by Git URL.

Do not use unpinned branch URLs for reproducible setup. Use tag `v1.0.0-preview.1` after the tag exists and after clean consumer validation has passed.

## Documentation

Start here:

- [Package documentation](Documentation~/README.md)
- [Git package install](Documentation~/Git-Package-Install.md)
- [Setup](Documentation~/Setup.md)
- [Authoring](Documentation~/Authoring.md)

The package documentation is for setup, authoring, runtime surfaces, QA smokes and troubleshooting. Project-side architecture tracking, closeouts, audits and consolidation roadmaps are not package usage documentation.

## Current public surface

- Game Application authoring through `GameApplicationAsset`.
- Route and Activity baseline through `RouteAsset`, `ActivityAsset`, route lifecycle and activity flow runtime surfaces.
- Optional app/session scoped `UIGlobal` scene for shared visual surfaces.
- Loading surface through `UnityLoadingSurfaceAdapter`.
- Transition surface through transition orchestration and `UnityFadeCurtainEffectAdapter`.
- Pause surface through resident `UIGlobal` presentation with `UnityPauseResidentSurfaceAdapter`.
- Pause input through `PauseInputActionRuntimeBridgeTrigger` and `PauseInputModeUnityPlayerInputRuntimeBridge`.
- RuntimeContent and ContentAnchor logical runtime, Unity materialization adapters, materialization bridges and composite release helpers.
- QA Canvas and smoke runners for package validation.

## Package boundaries

- Framework contracts and framework-specific Unity adapters live in this package.
- Technical primitives are consumed from `com.immersive.foundation`, `com.immersive.logging` and `com.immersive.pooling` instead of being reimplemented here.
- Runtime, Editor, package metadata and Unity assets are not documented as historical cuts. Use the package docs for the current supported state.

## Readiness status

FIRSTGAME remains deferred. After installing the package, configure the minimum Model 1.0 authoring assets and run Project Settings > Immersive Framework > Model Readiness > `Run Model Readiness Check`.
