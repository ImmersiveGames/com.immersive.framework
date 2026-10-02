# Immersive Framework

`com.immersive.framework` is the official Unity package for building an
Immersive Games application around explicit application, Session, Route and
Activity lifecycles.

Latest published preview: `1.1.0-preview.2`. Prepared, unpublished candidate:
`1.1.0-preview.3` (Reset consumer validation is complete; Camera/Player
validation remains pending). Latest stable package release: `1.0.2`. The earlier
`1.0.3-preview.1` preparation was not published and `1.1.0-preview.1` failed
archive verification before distribution.

The package provides runtime authorities, designer-facing authoring surfaces,
Editor workflows, diagnostics and validation. It consumes the technical
`com.immersive.foundation` and `com.immersive.logging` packages instead of
reimplementing their primitives.

## Requirements

- Unity `6000.5.0f1` or newer in the supported `6000.5` line;
- `com.immersive.foundation` `0.2.2`;
- `com.immersive.logging` `0.2.3`;
- Cinemachine `3.1.7`;
- Input System `1.19.0`.

There is no support or validation matrix for earlier Unity versions.

## Installation

Configure OpenUPM once in `Packages/manifest.json`, using the `com.immersive`
scope, then declare only the Framework package:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.immersive"]
    }
  ],
  "dependencies": {
    "com.immersive.framework": "1.0.2"
  }
}
```

The stable `1.0.2` release resolves Foundation `0.2.1` and Logging `0.2.2`.
The preview candidate in this checkout targets Foundation `0.2.2` and Logging
`0.2.3`. As a Git fallback for the stable release, use
`https://github.com/ImmersiveGames/com.immersive.framework.git#v1.0.2`; Git
consumers must declare the custom Git dependencies themselves because Unity
cannot resolve their semantic versions from Git alone.

The latest published preview is `1.1.0-preview.2`; use that version through
OpenUPM or the Git tag
`https://github.com/ImmersiveGames/com.immersive.framework.git#v1.1.0-preview.2`.
Candidate `1.1.0-preview.3` is prepared in this source tree but is not available
from OpenUPM or a release tag yet. It consolidates the Reset authoring and
runtime cut; preview APIs and serialized authoring may still change.

## Getting started

Start with the [Framework Getting Started guide](Documentation~/Guides/Getting-Started.md)
for the consumer setup path, then use the [documentation index](Documentation~/README.md)
to find the canonical guide for each domain.

## Product surfaces

| Area | Primary authoring surface | Runtime responsibility |
|---|---|---|
| Application | `GameApplicationAsset`, Project Settings and Persistent Content Scene Template | bootstrap, persistent content, Session creation and scoped framework runtime |
| Game Flow | `RouteAsset`, `ActivityAsset`, request triggers and content profiles | Route/Activity transitions, content contribution, visibility and lifecycle |
| Readiness and Loading | readiness participants, loading policies and loading surface adapters | commit/readiness gates, progress, interruption and terminal failure evidence |
| Player | `PlayerSessionProfile`, Slot/Actor profiles, Local Player and Scene-Provided authoring | Join/Leave, Actor selection, physical preparation, relocation and scoped observation |
| Camera | Session Camera Assignments, Camera Definitions, Outputs and `CameraRigComposer` | Assignment/Occurrence lifetime, membership and Subject resolution, transactional replacement and Output/Fallback application |
| Input and Pause | input-mode policy, `PlayerPauseInput`, pause triggers and presentation adapters | transactional input state, pause ownership and resident/activity presentation |
| Transition | transition policies and explicit effect adapters | transition planning, gating, effects and continuity |
| Reset | reset subjects/participants, Object Reset, Cycle Reset and Activity Restart triggers | scoped reset registration and explicit execution |
| Progression Save | `ProgressionSaveProfile` and backend adapter contract | save orchestration with built-in JSON or a game/third-party backend |
| Audio | Route/Activity BGM authoring and `FrameworkBgmDirector` | optional integration with `com.immersive.audio` |
| Scene integration | Scene Lifecycle Events and explicit Unity adapters | scoped load/unload callbacks without a parallel lifecycle authority |
| Diagnostics | validation modes, focused diagnostics and runtime evidence | projects existing authority; never creates a hidden command path |

### Application and Game Flow

The application owns Session setup; Routes own navigation scopes and Activities
own contextual gameplay. See the [Game Flow guide](Documentation~/Guides/Game-Flow.md)
for their transitions and lifetime rules.

### Player and local multiplayer

Use the [Player Usage guide](Documentation~/Guides/Player-Usage.md) for
Scene-Provided and Manager-Provisioned paths, Actor ownership, and maturity limits.
See [Activity Readiness](Documentation~/Guides/Activity-Readiness.md) for readiness.

### Camera

For the current Session Assignment model and its migration/validation status, see
[Camera Usage](Documentation~/Guides/Camera-Usage.md) and the
[curated Public API Reference](Documentation~/API/Public-API.md#camera).

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
  IF-ADR-038 Camera cuts have source coverage; Unity validation is pending;
- the Camera legacy-removal aggregate Unity recertification remains pending;
- Experimental Reset surfaces retain their declared Experimental API status;
- real-consumer proof remains required where listed by the current tracker.

Always validate package import/compile and the relevant Play Mode or smoke lanes
in the consuming Unity project before promoting a game build.

## Documentation

- [Documentation index](Documentation~/README.md) — canonical usage guide map, API reference and architecture navigation.
- [Getting Started](Documentation~/Guides/Getting-Started.md)
- [Curated Public API Reference](Documentation~/API/Public-API.md)
- [Current Framework tracker](Documentation~/Architecture/Tracking/IF-TRACK-Framework.md)
- [Changelog](CHANGELOG.md)

QAFramework owns synthetic technical validation. FIRSTGAME and consumer samples
own real-game integration and usability proof. Consumer assets and the legacy
Base/NewScripts architecture do not belong in this package.

## License

Licensed under the [MIT License](LICENSE.md).
