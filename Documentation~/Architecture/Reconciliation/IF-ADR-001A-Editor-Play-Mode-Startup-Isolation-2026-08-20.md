# IF-ADR-001A — Editor Play Mode Startup Isolation

Status: **IMPLEMENTED / BOTH EDITOR STARTUP MODES PROVEN — not a standalone QA certification**  
Date: **2026-08-20**  
Normative owner: [IF-ADR-001 — Core Lifecycle and Runtime Authority](../ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md)  
Related decisions: IF-ADR-008, IF-ADR-010

## Purpose

Record the scoped correction for a Unity Editor Play Mode regression in which a scene
that happened to be open for authoring could execute before normal Framework startup and
therefore contaminate the later application composition.

This record does not create a new lifecycle authority.

It makes an existing IF-ADR-001 rule operational:

```text
Editor authoring never becomes runtime authority.
```

## Reproduced failure

Before the correction, `ImmersiveFrameworkBootstrap` entered through an
`AfterSceneLoad` runtime initialization path.

That allowed the scene currently open in the Unity Editor to execute its ordinary Play
lifecycle first.

The failure window was:

```text
Editor-open authoring scene
  -> Play
  -> Awake / OnEnable
  -> possible EventSystem/listener/runtime side effects
  -> possible DontDestroyOnLoad escape
  -> Framework bootstrap
  -> Startup Route Primary Scene loaded Single
```

`LoadSceneMode.Single` correctly replaced ordinary loaded scene content later, but it
could not undo side effects that had already happened, and it could not remove objects
that had already escaped through `DontDestroyOnLoad`.

The concrete consumer symptom included an authoring scene capable of reproducing
duplicate `EventSystem` / listener contamination.

## Architectural correction

For:

```text
Editor Play Mode Startup = FrameworkStartup
```

Play Mode now starts from a package-owned neutral scene:

```text
Packages/com.immersive.framework/
  Editor/PlayMode/
    FrameworkPlayModeBootstrap.unity
```

The scene is intentionally empty.

It contains no:

```text
Camera
EventSystem
MonoBehaviour
GameApplication
FrameworkRuntimeHost
UIGlobal
Player
gameplay
persistent composition
```

Unity Editor startup policy points Play Mode at this scene before runtime bootstrap.

Canonical sequence:

```text
Editor-open authoring scenes
  -> excluded from FrameworkStartup Play entry

FrameworkPlayModeBootstrap
  -> neutral first scene

ImmersiveFrameworkBootstrap
  -> FrameworkRuntimeHost

FrameworkRuntimeHost
  -> Startup Route Primary Scene through SceneLifecycle

Game Application
  -> Persistent Content
  -> Route / Activity composition
```

The authoring scene therefore never receives Play lifecycle in `FrameworkStartup` and
cannot become an accidental persistent-composition source.

## Editor implementation

The scoped Editor implementation consists of:

```text
Editor/PlayMode/
  FrameworkEditorPlayModeStartupController.cs
  FrameworkPlayModeBootstrap.unity

Editor/Settings/
  ImmersiveFrameworkSettingsProvider.cs
```

`FrameworkEditorPlayModeStartupController` reconciles the project setting with Unity
Editor Play Mode start-scene state.

Expected mapping:

```text
FrameworkStartup
  -> EditorSceneManager.playModeStartScene = FrameworkPlayModeBootstrap

CurrentSceneOnly
  -> EditorSceneManager.playModeStartScene = null
```

Synchronization occurs after Editor/domain initialization, after relevant project/Undo
changes, through Project Settings, and immediately before entering Play Mode.

## Failure policy

A missing neutral bootstrap scene is a blocking configuration/infrastructure failure for
`FrameworkStartup`.

Required behavior:

```text
bootstrap available
  -> use it

bootstrap missing
  -> report explicit error
  -> cancel Play entry

bootstrap missing
  -> NEVER run current Editor scene as fallback
```

This preserves the existing no-silent-fallback rule.

## Runtime/build boundary

No runtime lifecycle redesign is required.

The correction does not change:

```text
FrameworkRuntimeHost ownership
SceneLifecycleRuntime ownership
Startup Route semantics
Persistent Content ownership
Route / Activity authority
Player/runtime build startup
```

The neutral scene and controller are Editor-only infrastructure.

## Product-surface boundary

Project Settings remains the explicit consumer-facing authority:

```text
Project Settings
  -> Immersive Framework
      -> Editor Play Mode
          -> Startup
              FrameworkStartup
              CurrentSceneOnly
```

IF-ADR-010 owns this product surface.

IF-ADR-001 owns the lifecycle/isolation semantics behind `FrameworkStartup`.

## Persistent Content boundary

IF-ADR-008 remains unchanged in authority:

```text
Game Application
  -> explicit Persistent Content
      -> application-persistent composition
```

An unrelated scene merely being open in the Editor is not a valid source of application
persistent content.

This is particularly important for persistent infrastructure such as:

```text
Camera Output
Session Camera structure
EventSystem
InputSystemUIInputModule
listeners / adapters
```

## Observed validation evidence

The corrected `FrameworkStartup` path was exercised in Play Mode.

Observed startup evidence included:

```text
SceneReleasing
  scene='FrameworkPlayModeBootstrap'
  reason='single-scene-replacement'
```

followed by:

```text
Startup Route Primary Scene
  scene='MinimalGame_Gameplay'
  alreadyLoaded='False'
  loadMode='Single'
  loaded='True'
```

and terminal application evidence:

```text
Boot succeeded
activityReadiness='Ready'
blockingIssues='0'
```

A second run was performed while the Editor had open the scene that had previously
reproduced `EventSystem` / duplicated-listener contamination.

The same neutral-bootstrap sequence was observed and the previous duplication symptom
did not appear.

This is direct consumer/Play Mode regression evidence for the `FrameworkStartup`
isolation path.

## Evidence classification

This record must not be described as a new broad QAFramework certification.

Current evidence classification:

```text
architecture contract        RECONCILED
Editor implementation        IMPLEMENTED LOCALLY
FrameworkStartup path        PROVEN IN PLAY MODE
CurrentSceneOnly path        PROVEN IN PLAY MODE
previous duplication symptom ABSENT IN REPRODUCED PROBLEM SCENE
regression reproduction      CLOSED
dedicated automated QA       NOT ADDED / NOT REQUIRED FOR THIS CUT
```

The existing historical ADR-001 certifications remain evidence for the boundaries they
actually executed. This scoped Editor correction does not retroactively change those
historical test claims.

## CurrentSceneOnly counter-mode

The explicit counter-mode was also exercised successfully.

Expected contract:

```text
CurrentSceneOnly
  -> current Editor scene intentionally executes
  -> Framework Play Mode start-scene override is absent
  -> Framework startup is skipped
```

Observed runtime evidence:

```text
[INFO][Immersive.Framework][ImmersiveFrameworkBootstrap]
Boot skipped. editorPlayModeStartup='CurrentSceneOnly'
```

This proves that the Editor policy remains intentionally bifurcated:

```text
FrameworkStartup
  -> neutral package bootstrap
  -> Framework application startup

CurrentSceneOnly
  -> current authoring scene
  -> Framework bootstrap skipped
```

The counter-mode is therefore proven for this scoped correction.

## Disposition

The scoped Editor startup regression is closed for both supported startup policies:

```text
FrameworkStartup    PROVEN
CurrentSceneOnly    PROVEN
regression          CLOSED
```

No IF-ADR-023 is required.

The correction belongs under IF-ADR-001 because it enforces an existing lifecycle
boundary instead of introducing a new authority.

Final ownership map:

```text
IF-ADR-001
  normative Editor/runtime startup isolation

IF-ADR-001A
  scoped implementation + regression evidence

IF-ADR-010
  Project Settings / Editor product surface

IF-ADR-008
  explicit application-persistent composition boundary

IF-TRACK
  mutable current status
```
