# Immersive Framework

`com.immersive.framework` is the official Unity package for framework runtime,
authoring, diagnostics and validation.

Current version: `1.0.0-preview.17`.

## Product surfaces

```text
GameApplicationAsset -> bootstrap -> scoped Framework runtime
LocalPlayerProvisioningAuthoring -> manual local Player join
SceneLocalPlayerAdmissionAuthoring -> scene-owned local Player admission
CameraRigRecipe -> CameraRigComposer -> Validate / Apply/Rebuild
FrameworkBgmDirector -> Route/Activity BGM bindings -> Immersive Audio
PausePlayerInputBinding -> InputMode transaction -> PlayerInput state writer
Reset authoring -> explicit runtime ports -> ResetRegistry / ResetExecutor
```

`FrameworkRuntimeHost` is an internal application/session composition root. It
does not expose a static current-host registry or service-locator API. Required
runtime dependencies are supplied through typed bindings and fail explicitly
when unavailable.

## Documentation

- [Documentation index](Documentation~/README.md)
- [Current tracker](Documentation~/Architecture/Tracking/IF-TRACK-Framework.md)
- [Framework usage](Documentation~/Guides/Framework-Usage.md)
- [Player usage](Documentation~/Guides/Player-Usage.md)
- [Camera usage](Documentation~/Guides/Camera-Usage.md)
- [Audio usage](Documentation~/Guides/Audio-Usage.md)
- [Reset usage](Documentation~/Guides/Reset-Usage.md)

QAFramework owns synthetic technical validation. FIRSTGAME owns real-game
integration proof. Consumer assets and the old Base/NewScripts architecture do
not belong in this package.
