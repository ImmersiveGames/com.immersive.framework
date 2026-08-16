# Immersive Framework

`com.immersive.framework` is the official Unity package for framework runtime,
authoring, diagnostics and validation.

Current version: `1.0.0-preview.17`.

## Supported Unity version

```text
Unity 6000.5.0f1 is the official minimum version.
There is no support or test matrix for earlier Unity versions.
```

## Product surfaces

```text
GameApplicationAsset -> bootstrap -> scoped Framework runtime
PlayerSessionProfile -> Supported Slots / Joining / Host Provisioning / Actor Resolution
LocalPlayerProvisioningAuthoring -> Manager-Provisioned local Player join
SceneLocalPlayerAdmissionAuthoring -> Scene-Provided local Player admission
CameraRigComposer -> Validate / Apply/Rebuild (Unity Preset optional)
FrameworkBgmDirector -> Route/Activity BGM bindings -> Immersive Audio
PausePlayerInputBinding -> InputMode transaction -> PlayerInput state writer
Reset authoring -> explicit runtime ports -> ResetRegistry / ResetExecutor
SceneLifecycleEvents -> SceneLifecycleRuntime callbacks -> explicit UnityEvents
```

`FrameworkRuntimeHost` is an internal application/session composition root. It
does not expose a static current-host registry or service-locator API. Required
runtime dependencies are supplied through typed bindings and fail explicitly
when unavailable.

## Persistent Content Scene Template

The package includes an official Editor Scene Template for the application-persistent
content scene:

```text
File
  -> New Scene
  -> Immersive Persistent Content
```

Use the template as a starting point, save the result as a concrete `.unity` scene
owned by the game, and assign that scene to:

```text
GameApplicationAsset
  -> Persistent Content
  -> Content Scene
```

The `GameApplicationAsset` Inspector also provides explicit authoring actions to
open the assigned scene and add or enable it in the active Build Profile Scene List.

The Scene Template is an Editor authoring aid only. The template asset is not a
runtime authority, and the framework does not silently create, repair, save, assign
or add consumer scenes to a build.

See [Framework usage](Documentation~/Guides/Framework-Usage.md) for the complete
Persistent Content workflow.

## Player technical QA status

The current accepted Player model was technically certified by the canonical
QAFramework Player orchestrator on 2026-08-09:

```text
Player Session                         PASS
Scene-Provided                        PASS
Manager-Provisioned                   PASS
Actor lifecycle                       PASS
Public Player Surface                 PASS
Activity Participation integration    PASS

PLAYER QA CERTIFIED
```

This certification covers the current `Supported Slots` Session model; it does
not restore the removed Capacity, separate provisioning Profile or per-Slot
Host Provisioning override model. FIRSTGAME remains the real-consumer/product
usability proof.

## Documentation

- [Documentation index](Documentation~/README.md)
- [Current tracker](Documentation~/Architecture/Tracking/IF-TRACK-Framework.md)
- [ADR completion summary](Documentation~/Architecture/IMMERSIVE-FRAMEWORK-ADR-COMPLETION-SUMMARY-2026-08-08.md)
- [Player QA certification](Documentation~/Architecture/IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md)
- [Framework usage](Documentation~/Guides/Framework-Usage.md)
- [Player usage](Documentation~/Guides/Player-Usage.md)
- [Activity readiness](Documentation~/Guides/Activity-Readiness.md)
- [Pause usage](Documentation~/Guides/Pause-Usage.md)
- [Camera usage](Documentation~/Guides/Camera-Usage.md)
- [Reset usage](Documentation~/Guides/Reset-Usage.md)
- [Scene lifecycle events](Documentation~/Guides/Scene-Lifecycle-Events.md)

QAFramework owns synthetic technical validation. FIRSTGAME owns real-game
integration proof. Consumer assets and the old Base/NewScripts architecture do
not belong in this package.
