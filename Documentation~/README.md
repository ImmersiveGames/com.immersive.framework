# Immersive Framework Documentation

This package keeps one documentation topology:

```text
Guides/                    current product usage
Architecture/ADRs/         accepted and proposed decisions (unique numbers)
Architecture/Tracking/     the only mutable status board
Architecture/Archive/      historic plans, audits and fix notes
Architecture/Plans/        no active plan file; open work lives on the tracker
```

## Start here

- [Current framework tracker](Architecture/Tracking/IF-TRACK-Framework.md)
- [Framework usage](Guides/Framework-Usage.md)
- [Player usage](Guides/Player-Usage.md)
- [Activity readiness](Guides/Activity-Readiness.md)
- [Pause usage](Guides/Pause-Usage.md)
- [Camera usage](Guides/Camera-Usage.md)
- [Reset usage](Guides/Reset-Usage.md)
- [Persistent Content Scene Template](Guides/Persistent-Content-Scene-Template.md)
- [Audio usage](Guides/Audio-Usage.md)
- [Logging usage](Guides/Logging-Usage.md)
- [Scene lifecycle events](Guides/Scene-Lifecycle-Events.md)
- [Game Flow — player-independent navigation](Guides/Game-Flow-Player-Independent-Navigation.md)
- [Application frame rate](Guides/Application-Frame-Rate-Usage.md)
- [Editor authoring standard](Guides/Editor-Authoring-Standard.md)

## Canonical decisions

| ADR | Title | Status |
|---|---|---|
| [001](Architecture/ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | Core lifecycle and runtime authority | Accepted |
| [002](Architecture/ADRs/IF-ADR-002-Product-Authoring-Model.md) | Product authoring model | Accepted |
| [003](Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | Player participation and Actor lifecycle | Accepted |
| [004](Architecture/ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | Camera requests and output authority | Accepted |
| [005](Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | Input, Pause, Gate and Reset | Accepted |
| [006](Architecture/ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | Loading, transition, persistence and diagnostics | Accepted |
| [007](Architecture/ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | Activity entry readiness and reveal gating | Accepted (implemented) |
| [008](Architecture/ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) | Persistent Application Content composition | Accepted |
| [009](Architecture/ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md) | Activity local visibility rules | Accepted |
| [010](Architecture/ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) | Editor and Inspector product surface authority | Proposed |
| [011](Architecture/ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md) | Participant-aware Activity readiness Loading progress | Accepted (implemented) |
| [012](Architecture/ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md) | Activity Player participation profile and readiness compatibility | Proposed (not shipped) |
| [013](Architecture/ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | Optional Audio BGM adapter | Accepted |
| [014](Architecture/ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | Authored definition and stable identity authority | Proposed |

ADRs decide. The tracker records progress and implementation confirmation. Git
history and [Architecture/Archive](Architecture/Archive/README.md) retain
superseded plans, audits and micro-cut notes.

## Foundation / advanced (code present; dedicated guide deferred)

```text
ProgressionSave / snapshot / preferences foundations
ObjectEntry declarations
ActivityLocalVisibilityAdapter
```

See the tracker for status. Do not treat Archive content as current product truth.
