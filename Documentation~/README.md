# Immersive Framework Documentation

Last updated: **2026-08-13**

## Start here

- [Stage A canonical package baseline](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)
- [Current framework tracker](Architecture/Tracking/IF-TRACK-Framework.md)
- [Architecture documentation map](Architecture/README.md)
- [API maturity and validation governance](Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
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

## Current program state

The historical Stage A package baseline remains the approved baseline for already-closed
boundaries. Subsequent Player architecture cuts ADR-019 and ADR-020 were reconciled as
separate scoped technical extensions and do not invent a replacement baseline commit.

```text
Historical Stage A package baseline
  ImmersiveGames/com.immersive.framework
  7a20ec748e4e5f5f3764bdc34ee249c1fe1c1da6

Reverse audit
  RA-01 through RA-04 CLOSED

ADR-019
  Accepted / Reconciled / Implemented / QA Certified

ADR-020
  Accepted / Reconciled / Implemented
  Focused Manager-Provisioned public Leave QA: 26/26

ADR-021 / ADR-022
  Proposed

Active product phase
  FIRSTGAME / Stage B real-consumer validation on accepted boundaries
```

The canonical mutable status and certification scope are recorded in the
[Framework Tracker](Architecture/Tracking/IF-TRACK-Framework.md).

## Documentation authority

```text
Architecture/ADRs/             normative architecture decisions
Architecture/Governance/       cross-cutting compatibility/product policy
Architecture/Reconciliation/   current technical reconciliation/certification
Architecture/Tracking/         current mutable framework status
Guides/                        current product usage
Architecture/Archive/          historical records; not current authority
```

ADRs decide. Governance records define cross-cutting policy without creating feature
authority. Reconciliation records describe current alignment and certification. The
Tracker summarizes current delivery state. Archive records preserve history without
acting as current product truth.

## Canonical decisions

| ADR | Title | Status |
|---|---|---|
| [001](Architecture/ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | Core lifecycle and runtime authority | Accepted |
| [002](Architecture/ADRs/IF-ADR-002-Product-Authoring-Model.md) | Product authoring model | Accepted |
| [003](Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | Player participation and Actor lifecycle | Accepted |
| [004](Architecture/ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | Camera requests and output authority | Accepted |
| [005](Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | Input, Pause, Gate and Reset | Accepted |
| [006](Architecture/ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | Loading, transition, persistence and diagnostics | Accepted |
| [007](Architecture/ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | Activity entry readiness and reveal gating | Accepted |
| [008](Architecture/ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) | Persistent application content composition | Accepted |
| [009](Architecture/ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md) | Activity-local visibility rules | Accepted |
| [010](Architecture/ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) | Editor and Inspector product surface authority | Accepted |
| [011](Architecture/ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md) | Participant-aware readiness/loading progress | Accepted |
| [012](Architecture/ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md) | Activity Player participation profile and readiness compatibility | Accepted |
| [013](Architecture/ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | Optional Audio/BGM adapter | Accepted |
| [014](Architecture/ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | Authored definition and stable identity authority | Accepted |
| [015](Architecture/ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | Player provisioning commands and consumer observation surface | Accepted |
| [016](Architecture/ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | Player session initial configuration and provisioning profiles | Accepted |
| [017](Architecture/ADRs/IF-ADR-017-Application-Frame-Rate-Project-Authority.md) | Application frame-rate project authority | Accepted |
| [018](Architecture/ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | Progression Save backend independence and persistence boundaries | Accepted |
| [019](Architecture/ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) | Session Player lifetime and Activity representation authority | Accepted |
| [020](Architecture/ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) | Session Player Leave and resource release authority | Accepted |
| [021](Architecture/ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) | Activity Player Actor initial placement authority | Proposed |
| [022](Architecture/ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) | Camera Rig presentation models and materialization authority | Proposed |

## Current reconciliation / closure records

- [ADR-001 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-001-RECONCILIATION-2026-08-10.md)
- [ADR-002 and ADR-009 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-002-009-RECONCILIATION-2026-08-10.md)
- [ADR-003 and ADR-012 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-003-012-RECONCILIATION-2026-08-10.md)
- [ADR-004 Camera reconciliation](Architecture/Reconciliation/IF-ADR-004A-Camera-Authority-Normative-Reconciliation-2026-08-10.md)
- [ADR-005 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-005-RECONCILIATION-2026-08-10.md)
- [ADR-006 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-006-RECONCILIATION-2026-08-10.md)
- [ADR-007 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-007-RECONCILIATION-2026-08-11.md)
- [ADR-008 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-008-RECONCILIATION-2026-08-10.md)
- [ADR-011 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-011-RECONCILIATION-2026-08-11.md)
- [ADR-017 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-017-RECONCILIATION-2026-08-11.md)
- [ADR-018 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-RECONCILIATION-2026-08-11.md)
- [ADR-019 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md)
- [ADR-020 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md)
- [RA-03 Object Entry ownership](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-RA-03-OBJECT-ENTRY-OWNERSHIP-RECONCILIATION-2026-08-11.md)
- [RA-04 Architecture Governance Hygiene](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-RA-04-ARCHITECTURE-GOVERNANCE-HYGIENE-2026-08-11.md)
- [Stage A Canonical Package Baseline Closure](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

## Certification-scope note for ADR-020

ADR-020 architecture and package implementation are closed. Focused public
Manager-Provisioned Leave is technically certified at 26/26. This documentation set does
not claim a dedicated Scene-Provided **Session Leave** regression terminal that has not
been separately evidenced. See the ADR-020 reconciliation and tracker for the exact scope.
