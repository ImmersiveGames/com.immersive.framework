# Immersive Framework

`com.immersive.framework` is the official Unity package for building an
Immersive Games application around explicit application, Session, Route and
Activity lifecycles.

**Official Stable baseline approved:** `1.1.0` (publication pending). The
current source package metadata is `1.1.0-preview.9`; the latest published
preview remains `1.1.0-preview.8`. Versions before 1.1.0, including 1.0.2 and
`1.1.0-preview.*`, are retained as historical prototyping and validation
records and carry no compatibility or generic migration obligation to the new
baseline. From 1.1.0 forward, guarantees follow SemVer and the individual
Stable/Experimental/Internal classification; see [IF-ADR-047](Documentation~/Architecture/ADRs/IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md)
and [IF-GOV-001](Documentation~/Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md).
Release readiness and remaining gates are recorded in the
[framework tracker](Documentation~/Architecture/Tracking/IF-TRACK-Framework.md).

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

After the 1.1.0 release is published, configure OpenUPM once in
`Packages/manifest.json`, using the `com.immersive` scope, then declare the
Framework package:

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

The current preview source declares Foundation `0.2.2`, Logging `0.2.3`,
Cinemachine `3.1.7` and Input System `1.19.0`; confirm the dependency graph
against the immutable 1.1.0 candidate before release. Do not install 1.1.0
until its publication and exact package contents have been verified. Git
consumers must declare custom Git dependencies themselves because Unity cannot
resolve their semantic versions from Git alone. Earlier package releases
remain in the [changelog](CHANGELOG.md) as history; they are not migration
baselines for 1.1.0.

The currently published preview is `1.1.0-preview.8`. Its API and serialized
authoring are not Stable compatibility commitments. See the [preview release](https://github.com/ImmersiveGames/com.immersive.framework/releases/tag/v1.1.0-preview.8)
for historical installation details.

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
