# Immersive Framework Documentation

This package uses one documentation authority model:

```text
Guides/                    current product usage
Architecture/ADRs/         normative architecture decisions
Architecture/Tracking/     mutable cross-framework status board
Architecture/Archive/      historic plans, audits and fix notes
Architecture/Plans/        index only; active work belongs on the tracker
```

## Evidence model

Functional technical claims are grounded in package implementation and objective
QA evidence. FIRSTGAME remains the real-consumer integration/UX surface when
applicable and is tracked separately from deterministic QA contracts.

## Start here

- [Current framework tracker](Architecture/Tracking/IF-TRACK-Framework.md)
- [Framework usage](Guides/Framework-Usage.md)
- [Editor authoring standard](Guides/Editor-Authoring-Standard.md)
- [Player usage](Guides/Player-Usage.md)
- [Activity readiness](Guides/Activity-Readiness.md)
- [Camera usage](Guides/Camera-Usage.md)
- [Pause usage](Guides/Pause-Usage.md)
- [Reset usage](Guides/Reset-Usage.md)
- [Persistent Content Scene Template](Guides/Persistent-Content-Scene-Template.md)
- [Audio usage](Guides/Audio-Usage.md)
- [Logging usage](Guides/Logging-Usage.md)
- [Scene lifecycle events](Guides/Scene-Lifecycle-Events.md)
- [Game Flow — player-independent navigation](Guides/Game-Flow-Player-Independent-Navigation.md)
- [Application frame rate](Guides/Application-Frame-Rate-Usage.md)

## Canonical decisions

| ADR | Title | Status |
|---|---|---|
| [001](Architecture/ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | Core lifecycle and runtime authority | Accepted |
| [002](Architecture/ADRs/IF-ADR-002-Product-Authoring-Model.md) | Product authoring model | Accepted |
| [003](Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | Player participation and Actor lifecycle | Accepted |
| [004](Architecture/ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | Camera requests and output authority | **Accepted / technically certified current single-output boundary** |
| [005](Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | Input, Pause, Gate and Reset | Accepted |
| [006](Architecture/ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | Loading, transition, persistence and diagnostics | Accepted |
| [007](Architecture/ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | Activity entry readiness and reveal gating | Accepted |
| [008](Architecture/ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) | Persistent Application Content composition | Accepted |
| [009](Architecture/ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md) | Activity local visibility rules | Accepted |
| [010](Architecture/ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) | Editor and Inspector product surface authority | Accepted |
| [011](Architecture/ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md) | Participant-aware Activity readiness Loading progress | Accepted |
| [012](Architecture/ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md) | Activity Player participation and readiness compatibility | Accepted |
| [013](Architecture/ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | Optional Audio BGM adapter | Accepted / Experimental |
| [014](Architecture/ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | Authored definition and stable identity authority | Accepted |
| [015](Architecture/ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | Player provisioning commands and observation surface | Accepted |
| [016](Architecture/ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | Player Session initial configuration | Accepted |

### Camera sub-decisions

- [IF-ADR-004A — Camera Authority Normative Reconciliation](Architecture/ADRs/IF-ADR-004A-Camera-Authority-Normative-Reconciliation-2026-08-10.md) — **Closed**
- [IF-ADR-004B — Camera Negative Integrity Certification](Architecture/ADRs/IF-ADR-004B-Camera-Negative-Integrity-Certification-2026-08-10.md) — **Certified 18/18**
- [IF-ADR-004C — Camera Owner Lifetime Integrity](Architecture/ADRs/IF-ADR-004C-Camera-Owner-Lifetime-Integrity-2026-08-10.md) — **Accepted / Implemented / Certified 10/10**

## Current evidence records

- [Camera QA certification — 2026-08-10](Architecture/IMMERSIVE-FRAMEWORK-CAMERA-QA-CERTIFICATION-2026-08-10.md)
- [Camera audit and post-audit closure — 2026-08-10](Architecture/IMMERSIVE-FRAMEWORK-ADR-004-CAMERA-AUDIT-2026-08-10.md)
- [Player QA certification — 2026-08-09](Architecture/IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md)
- [Player serialization migration integrity — 2026-08-09](Architecture/IMMERSIVE-FRAMEWORK-PLAYER-SERIALIZATION-MIGRATION-INTEGRITY-2026-08-09.md)

ADRs decide. The Tracker records mutable implementation/integration status.
Evidence records preserve point-in-time technical proof. Guides teach current
usage. Archive content is historical and must not be treated as current product
truth.
