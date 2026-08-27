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
PlayerSessionObserver -> scoped read-only Player Session evidence
explicit Player Session commands -> Open / Close / Join / Actor Selection / Leave
LocalPlayerProvisioningAuthoring -> Manager-Provisioned local Player authority
SceneLocalPlayerAdmissionAuthoring -> Scene-Provided local Player admission
CameraRigComposer -> Validate / Apply/Rebuild (Unity Preset optional)
FrameworkBgmDirector -> Route/Activity BGM bindings -> Immersive Audio
PlayerPauseInput -> InputMode transaction -> PlayerInput state writer
Reset authoring -> explicit runtime ports -> ResetRegistry / ResetExecutor
SceneLifecycleEvents -> SceneLifecycleRuntime callbacks -> explicit UnityEvents
```

The explicit Player Session command family currently contains:

```text
PlayerSessionOpenJoiningCommandTrigger
PlayerSessionCloseJoiningCommandTrigger
PlayerSessionJoinCommandTrigger
PlayerSessionSelectActorCommandTrigger
PlayerSessionDefaultActorSelectionCommandTrigger
PlayerSessionReplaceActorSelectionCommandTrigger
PlayerSessionClearActorSelectionCommandTrigger
PlayerSessionLeaveCommandTrigger
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

The current integrated Player boundary is certified by the QAFramework Full Player orchestrator:

```text
PLAYER CURRENT AGGREGATE COMPLETE
mandatoryContracts = 27
executedContracts = 27
passedContracts = 27

serialization                      PASS
session                            PASS
routeSpatialEntry                  PASS
activityRelocation                 PASS
sceneProvided                      PASS
sceneProvidedLeave                 PASS
sceneProvidedNoActivityLeave       PASS
sceneProvidedNoActivityTermination PASS
managerProvisioned                 PASS
managerNoActivity                  PASS
managerSessionTermination          PASS
actor                              PASS
publicSurface                      PASS
leave                              PASS
failedFirstSceneAdoption           PASS
failedContextualReprojection       PASS
noPhysicalHandoff                  PASS
```

The 2026-08-26 rerun closes the public arbitrary Actor-selection surface through explicit Select / Default / Replace / Clear commands. Actor selection remains Session-owned logical intent; those commands do not grant physical Actor hot-swap authority.

Historical `25/25` Player certification remains dated evidence for its earlier boundary and is not relabeled as coverage of the later Actor-selection command surface.

The package-local Actor-selection Unity Test Framework Editor tests are a separate evidence lane and are not claimed as executed by this integrated QA result unless separately recorded.

Current remaining Player product gaps include exact-Slot public Join, the public Slot/device/InputUser/control-scheme ownership/observation contract required for canonical Local Multiplayer, and the deferred command-availability/readiness product surface.

## Documentation

- [Documentation index](Documentation~/README.md)
- [Current tracker](Documentation~/Architecture/Tracking/IF-TRACK-Framework.md)
- [Player Actor Selection public surface certification](Documentation~/Architecture/Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)
- [Player current aggregate recertification](Documentation~/Architecture/Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)
- [Framework usage](Documentation~/Guides/Framework-Usage.md)
- [Player usage](Documentation~/Guides/Player-Usage.md)
- [Activity readiness](Documentation~/Guides/Activity-Readiness.md)
- [Pause usage](Documentation~/Guides/Pause-Usage.md)
- [Camera usage](Documentation~/Guides/Camera-Usage.md)
- [Reset usage](Documentation~/Guides/Reset-Usage.md)
- [Scene lifecycle events](Documentation~/Guides/Scene-Lifecycle-Events.md)

QAFramework owns synthetic technical validation. FIRSTGAME/Samples own real-consumer integration and product usability proof. Consumer assets and the old Base/NewScripts architecture do not belong in this package.
