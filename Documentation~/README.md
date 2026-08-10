# Immersive Framework Documentation

## Start here

- [Current framework tracker](Architecture/Tracking/IF-TRACK-Framework.md)
- [Architecture documentation map](Architecture/README.md)
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
- [Game Flow - player-independent navigation](Guides/Game-Flow-Player-Independent-Navigation.md)
- [Application frame rate](Guides/Application-Frame-Rate-Usage.md)

## Documentation authority

```text
Architecture/ADRs/             normative architecture decisions
Architecture/Reconciliation/   current technical reconciliation and certification
Architecture/Tracking/         current mutable framework status
Guides/                        current product usage
Architecture/Archive/          historical records; not current authority
```

ADRs decide. Reconciliation records describe current alignment. The Tracker
summarizes current delivery state. Archive records preserve history without
acting as current product truth.

## Canonical decisions

| ADR | Title | Status |
|---|---|---|
| [001](Architecture/ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | Core lifecycle and runtime authority | Accepted |
| [002](Architecture/ADRs/IF-ADR-002-Product-Authoring-Model.md) | Product authoring model | Accepted |
| [003](Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | Player participation and Actor lifecycle | Accepted |
| [004](Architecture/ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | Camera requests and output authority | Accepted |
| [005](Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | Input, Pause, Gate and Reset | Accepted |
| [006-016](Architecture/ADRs/) | Remaining framework boundaries | See individual ADRs |

## Current reconciliation records

- [ADR-001 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-001-RECONCILIATION-2026-08-10.md)
- [ADR-002 and ADR-009 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-002-009-RECONCILIATION-2026-08-10.md)
- [ADR-003 and ADR-012 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-003-012-RECONCILIATION-2026-08-10.md)
- [ADR-004 Camera reconciliation](Architecture/Reconciliation/IF-ADR-004A-Camera-Authority-Normative-Reconciliation-2026-08-10.md)
- [ADR-005 reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-005-RECONCILIATION-2026-08-10.md)
