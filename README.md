# Immersive Framework

`com.immersive.framework` is the official Unity package for building an
Immersive Games application around explicit application, Session, Route and
Activity lifecycles.

`1.1.0` is the first official Stable baseline. Compatibility follows SemVer
for contracts explicitly classified Stable; a Stable package version does
not promote Experimental or Internal surfaces. Versions before 1.1.0,
including 1.0.2 and `1.1.0-preview.*`, are historical records and carry no
generic migration obligation to this baseline. See
[IF-ADR-047](Documentation~/Architecture/ADRs/IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md)
and [IF-GOV-001](Documentation~/Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md).
Release evidence and any remaining gates are recorded in the
[framework tracker](Documentation~/Architecture/Tracking/IF-TRACK-Framework.md).

The package provides runtime authorities, designer-facing authoring surfaces,
Editor workflows, diagnostics and validation. It consumes the technical
`com.immersive.foundation` and `com.immersive.logging` packages instead of
reimplementing their primitives.

## Requirements

- Stable Framework `1.1.0` uses Unity `6000.5.0f1` or newer in the supported
  `6000.5` line, Foundation `0.2.2`, Logging `0.2.3`, Cinemachine `3.1.7`,
  and Input System `1.19.0`.
- Framework preview `1.2.0-preview.4` declares
  Composition `0.1.0-preview.3`, Foundation `0.2.3`, Logging `0.2.4`,
  Cinemachine `3.1.7`, and Input System `1.19.0`. Preview APIs remain
  Experimental unless explicitly classified otherwise.

There is no support or validation matrix for earlier Unity versions.

## Installation

Configure OpenUPM once in `Packages/manifest.json`, using the `com.immersive`
scope, then declare the Framework package:

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
    "com.immersive.framework": "1.1.0"
  }
}
```

This manifest value deliberately keeps Framework on Stable `1.1.0`. Package
Manager will not replace it automatically with a preview release or a newer
Git branch. To use preview `1.2.0-preview.4`, set the dependency to that exact
version through OpenUPM; the stable value above remains unchanged until the
project explicitly opts into the preview channel.

The Framework preview release is [`1.2.0-preview.4`](https://github.com/ImmersiveGames/com.immersive.framework/releases/tag/v1.2.0-preview.4), available through [OpenUPM](https://openupm.com/packages/com.immersive.framework/). It includes Actor Profile part assets and migration, Actor Groups, scoped Actor occurrence queries, Player Actor composition contributions, and Object Entry occurrence queries. It depends on Composition `0.1.0-preview.3`, Foundation `0.2.3` and Logging `0.2.4`. The preview does not promote Experimental contracts to Stable.
Earlier package releases remain in the [changelog](CHANGELOG.md) as history;
they are not migration baselines for `1.1.0`.

For Framework development from Git, Unity cannot resolve custom-package SemVer
dependencies to Git packages. Add Framework and each custom package it
depends on as direct Git dependencies in the project's existing
`Packages/manifest.json`; the current candidate graph includes:

```json
{
  "dependencies": {
    "com.immersive.framework": "https://github.com/ImmersiveGames/com.immersive.framework",
    "com.immersive.composition": "https://github.com/ImmersiveGames/com.immersive.composition.git",
    "com.immersive.foundation": "https://github.com/ImmersiveGames/com.immersive.foundation.git",
    "com.immersive.logging": "https://github.com/ImmersiveGames/com.immersive.logging.git"
  }
}
```

These URLs select the repositories' default branches because no tag/ref is
specified. `Packages/packages-lock.json` still pins the resolved commit hashes;
Unity does not continuously follow branch updates. Refresh a package through
Package Manager to resolve a newer commit. Do not hand-edit the lockfile.

For a registry installation, declare Framework only and let it resolve
Composition transitively. The Framework package declares Composition's minimum
SemVer, so do not add a second Composition source in that setup:

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
    "com.immersive.framework": "1.2.0-preview.4"
  }
}
```

`Packages/packages-lock.json` records the exact resolved graph; update it
through Package Manager, not by hand. The Project Owner's QAFramework EditMode
result and FirstGame / PEGA Actor Profile migrations are recorded in the
[Framework tracker](Documentation~/Architecture/Tracking/IF-TRACK-Framework.md#if-adr-050-d08-closeout--consumer-actor-profile-migration--2026-10-18).
For the new Base-part and runtime contribution model, see the
[Actor Composition Usage guide](Documentation~/Guides/Actor-Composition-Usage.md).

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
| Input and Pause | `PlayerPauseInput`, pause triggers and presentation adapters | transactional input state, pause ownership and resident/activity presentation |
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

Package version and API maturity are separate. Maturity is declared per
surface with `FrameworkApiStatusAttribute`:

- `Stable`: supported consumer contract; breaking changes require an explicit
  architecture and migration decision;
- `Experimental`: usable for controlled development, without compatibility
  guarantees;
- `Internal`: framework implementation detail;
- `DevelopmentTooling`: Editor, QA or diagnostic tooling rather than game API;
- `Deferred` and `Removed`: outside the active consumer baseline.

Do not treat a stable package version as promotion of every Experimental type.
See [API Maturity and Validation Governance](Documentation~/Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md).
Compatibility begins at the approved 1.1.0 baseline; earlier releases do not
create a migration obligation to it. The release decision and SemVer rules are
in [IF-ADR-047](Documentation~/Architecture/ADRs/IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md).

## Validation status

The repository contains package-local Unity Test Framework assemblies and the
documentation records focused QAFramework and FIRSTGAME evidence. Certification
is scoped and dated: an older passing matrix is not evidence for later cuts.

Validation is boundary-specific. The current tracker records completed Player,
Game Flow, Pause/Input, Reset and focused Camera evidence separately from the
remaining gates. For example, IF-ADR-042 SharedGroup has focused QA and consumer
evidence, IF-ADR-043 physical Output participation has focused QA and consumer
evidence, while broader IF-ADR-038 recertification remains open. IF-ADR-044 has
runtime QA evidence and a Local Multiplayer manual consumer validation; package
import/compile and other gate combinations remain separate. See the tracker for
the dated evidence and exact scope.

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
