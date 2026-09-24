# Immersive Framework

`com.immersive.framework` is the official Unity package for building an
Immersive Games application around explicit application, Session, Route and
Activity lifecycles.

Current version: `1.0.1` (stable package release).

The package provides runtime authorities, designer-facing authoring surfaces,
Editor workflows, diagnostics and validation. It consumes the technical
`com.immersive.foundation` and `com.immersive.logging` packages instead of
reimplementing their primitives.

## Requirements

- Unity `6000.5.0f1` or newer in the supported `6000.5` line;
- `com.immersive.foundation` `0.2.0`;
- `com.immersive.logging` `0.2.1`;
- Cinemachine `3.1.0`;
- Input System `1.19.0`.

There is no support or validation matrix for earlier Unity versions.

## Installation

Configure the source that resolves the Immersive technical packages, then add
the framework through Unity Package Manager with the stable Git tag:

```text
https://github.com/ImmersiveGames/com.immersive.framework.git#v1.0.1
```

Equivalent `Packages/manifest.json` entry:

```json
{
  "dependencies": {
    "com.immersive.framework": "https://github.com/ImmersiveGames/com.immersive.framework.git#v1.0.1"
  }
}
```

Pin the tag in projects and release manifests. Do not depend on `master` for a
reproducible game setup.

## Getting started

1. Create a `GameApplicationAsset` from the Immersive Framework asset menu.
2. Configure the application policies and create the required `RouteAsset` and
   `ActivityAsset` definitions.
3. Create the application-persistent scene through `File > New Scene > Immersive
   Persistent Content`.
4. Save that scene as a game-owned `.unity` asset and assign it to
   `GameApplicationAsset > Persistent Content > Content Scene`.
5. Use the Game Application Inspector action to add or enable the scene in the
   active Build Profile Scene List.
6. Author each feature through its component, asset, Project Settings, Template
   or Composer surface, then validate it from the owning Inspector.
7. Enter Play Mode and inspect runtime evidence separately from authoring
   validation.

Required configuration fails explicitly. The framework does not silently repair
missing dependencies, discover a runtime host through global lookup or fabricate
identity from object names.

See [Framework Usage](Documentation~/Guides/Framework-Usage.md) for the complete
workflow.

## Product surfaces

| Area | Primary authoring surface | Runtime responsibility |
|---|---|---|
| Application | `GameApplicationAsset`, Project Settings and Persistent Content Scene Template | bootstrap, persistent content, Session creation and scoped framework runtime |
| Game Flow | `RouteAsset`, `ActivityAsset`, request triggers and content profiles | Route/Activity transitions, content contribution, visibility and lifecycle |
| Readiness and Loading | readiness participants, loading policies and loading surface adapters | commit/readiness gates, progress, interruption and terminal failure evidence |
| Player | `PlayerSessionProfile`, Slot/Actor profiles, Local Player and Scene-Provided authoring | Join/Leave, Actor selection, physical preparation, relocation and scoped observation |
| Camera | Session Outputs, Camera Presentations, `CameraRigComposer` and Camera Subjects | request arbitration, output transactions, presentation lifecycle and subject framing |
| Input and Pause | input-mode policy, `PlayerPauseInput`, pause triggers and presentation adapters | transactional input state, pause ownership and resident/activity presentation |
| Transition | transition policies and explicit effect adapters | transition planning, gating, effects and continuity |
| Reset | reset subjects/participants, Object Reset, Cycle Reset and Activity Restart triggers | scoped reset registration and explicit execution |
| Progression Save | `ProgressionSaveProfile` and backend adapter contract | save orchestration with built-in JSON or a game/third-party backend |
| Audio | Route/Activity BGM authoring and `FrameworkBgmDirector` | optional integration with `com.immersive.audio` |
| Scene integration | Scene Lifecycle Events and explicit Unity adapters | scoped load/unload callbacks without a parallel lifecycle authority |
| Diagnostics | validation modes, focused diagnostics and runtime evidence | projects existing authority; never creates a hidden command path |

### Application and Game Flow

The canonical ownership chain is:

```text
GameApplicationAsset
  -> bootstrap
  -> Persistent Content
  -> internal FrameworkRuntimeHost
  -> Session
  -> Route lifecycle
  -> Activity lifecycle
  -> scoped feature contexts
```

`FrameworkRuntimeHost` is the internal composition root. It intentionally has no
static current-host registry or service-locator API. Route and Activity
definitions own their content, readiness, participation, transition and
presentation intent.

### Player and local multiplayer

Player participation separates logical intent from physical ownership:

```text
Join
!= Actor Selection
!= Activity Actor Preparation
!= Physical Materialization
!= Prepared Actor Replacement
```

The product surface includes:

- `PlayerSessionObserver` for scoped read-only Session evidence;
- explicit Open Joining, Close Joining, Join, Select Actor, Default Actor
  Selection, Replace Actor Selection, Clear Actor Selection and Leave commands;
- Manager-Provisioned and Scene-Provided local Player workflows;
- Route Spatial Entry and explicit Activity relocation;
- explicit, Experimental device/InputUser/control-scheme ownership evidence for
  the implemented local multiplayer boundary; exact-Slot Join remains deferred;
- Manager-Provisioned prepared Actor replacement while preserving the owning
  Player Slot, Host, PlayerInput, Session and Activity occurrence.

See [Player Usage](Documentation~/Guides/Player-Usage.md) and
[Activity Readiness](Documentation~/Guides/Activity-Readiness.md).

### Camera

IF-ADR-032 is the current Camera architecture:

```text
Session
  -> physical Camera Outputs + Defaults

Session / Route / Activity
  -> Camera Presentation definitions
  -> runtime Presentation occurrences
  -> CameraRequest
  -> CameraOutputSession
  -> Camera Output
```

Camera capacity belongs to the Session. Presentation intent belongs to Session,
Route or Activity. `CameraRigComposer` materializes reusable rigs explicitly;
Camera Subjects provide live observable evidence. Player count does not create
Outputs implicitly, and `PlayerInputManager` remains the physical split-layout
writer.

The legacy shared-composition surface has been removed. Aggregate Unity
recertification after that removal is still pending; focused IF-ADR-032 evidence
is recorded in the tracker.

See [Camera Usage](Documentation~/Guides/Camera-Usage.md).

### Persistence, reset and optional integrations

Progression Save supports a built-in transactional JSON backend and an explicit
backend adapter contract for game-owned or third-party persistence. There is no
silent backend fallback.

Reset authoring covers direct Object Reset, grouped reset, Route/Activity cycle
reset and Activity restart over scoped reset registration/execution.

The Audio module is optional and integrates Route/Activity BGM intent with
`com.immersive.audio`. Logging guidance uses the separate
`com.immersive.logging` package.

## Persistent Content Scene Template

Create the official template through:

```text
File
  -> New Scene
  -> Immersive Persistent Content
```

The template is an Editor authoring aid, not runtime authority. The package does
not silently create, repair, save, assign or add consumer scenes to a build.
Under IF-ADR-032, Persistent Content does not own gameplay Camera topology.

See [Persistent Content Scene Template](Documentation~/Guides/Persistent-Content-Scene-Template.md).

## API maturity

The package version is stable, but maturity is declared per surface with
`FrameworkApiStatusAttribute`:

- `Stable`: supported consumer contract; breaking changes require an explicit
  architecture and migration decision;
- `Experimental`: usable for controlled development, without compatibility
  guarantees;
- `Internal`: framework implementation detail;
- `DevelopmentTooling`: Editor, QA or diagnostic tooling rather than game API;
- `Deferred` and `Removed`: outside the active consumer baseline.

Do not treat a stable package version as promotion of every Experimental type.
See [API Maturity and Validation Governance](Documentation~/Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md).

## Validation status

The repository contains package-local Unity Test Framework assemblies and the
documentation records focused QAFramework and FIRSTGAME evidence. Certification
is scoped and dated: an older passing matrix is not evidence for later cuts.

At this release boundary:

- Player, Game Flow, Pause/Input, Activity content/visibility and the focused
  IF-ADR-032 Camera cuts have recorded technical or consumer evidence;
- the Camera legacy-removal aggregate Unity recertification remains pending;
- Experimental Reset surfaces retain their declared Experimental API status;
- real-consumer proof remains required where listed by the current tracker.

Always validate package import/compile and the relevant Play Mode or smoke lanes
in the consuming Unity project before promoting a game build.

## Documentation

- [Documentation index](Documentation~/README.md)
- [Current Framework tracker](Documentation~/Architecture/Tracking/IF-TRACK-Framework.md)
- [Architecture map](Documentation~/Architecture/README.md)
- [Framework Usage](Documentation~/Guides/Framework-Usage.md)
- [Editor Authoring Standard](Documentation~/Guides/Editor-Authoring-Standard.md)
- [Player Usage](Documentation~/Guides/Player-Usage.md)
- [Camera Usage](Documentation~/Guides/Camera-Usage.md)
- [Activity Readiness](Documentation~/Guides/Activity-Readiness.md)
- [Pause Usage](Documentation~/Guides/Pause-Usage.md)
- [Reset Usage](Documentation~/Guides/Reset-Usage.md)
- [Progression Save Authoring](Documentation~/Guides/Progression-Save-Authoring.md)
- [Progression Save backend contract](Documentation~/Guides/Progression-Save-Backend-Adapter-Contract.md)
- [Built-in JSON backend](Documentation~/Guides/Progression-Save-Built-In-Json-Backend.md)
- [Audio Usage](Documentation~/Guides/Audio-Usage.md)
- [Logging Usage](Documentation~/Guides/Logging-Usage.md)
- [Application Frame Rate](Documentation~/Guides/Application-Frame-Rate-Usage.md)
- [Scene Lifecycle Events](Documentation~/Guides/Scene-Lifecycle-Events.md)
- [Changelog](CHANGELOG.md)

QAFramework owns synthetic technical validation. FIRSTGAME and consumer samples
own real-game integration and usability proof. Consumer assets and the legacy
Base/NewScripts architecture do not belong in this package.
