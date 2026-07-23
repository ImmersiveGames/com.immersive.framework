# Immersive Framework Documentation

This package keeps one compact documentation topology:

```text
Architecture/ADRs      accepted decisions and explicit pending decisions
Architecture/Plans     immutable approved execution route
Architecture/Tracking  the only mutable status board
Guides                 current product usage
```

## Start here

- [Current framework tracker](Architecture/Tracking/IF-TRACK-Framework.md)
- [Immutable evolution plan](Architecture/Plans/IF-PLAN-Framework-Evolution.v1.md)
- [Framework usage](Guides/Framework-Usage.md)
- [Player usage](Guides/Player-Usage.md)
- [Camera usage](Guides/Camera-Usage.md)
- [Audio usage](Guides/Audio-Usage.md)
- [Logging usage](Guides/Logging-Usage.md)
- [Reset usage](Guides/Reset-Usage.md)

## Canonical decisions

- [Core lifecycle and runtime authority](Architecture/ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md)
- [Product authoring model](Architecture/ADRs/IF-ADR-002-Product-Authoring-Model.md)
- [Player participation and Actor lifecycle](Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md)
- [Camera requests and output authority](Architecture/ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md)
- [Input, Pause, Gate and Reset](Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md)
- [Loading, transition, persistence and diagnostics](Architecture/ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md)
- [Optional Audio BGM adapter](Architecture/ADRs/IF-ADR-007-Optional-Audio-BGM-Adapter.md)

ADRs decide. The plan defines the stable route. The tracker records progress.
Git history retains superseded audits, manifests, closeouts and micro-cut notes.
