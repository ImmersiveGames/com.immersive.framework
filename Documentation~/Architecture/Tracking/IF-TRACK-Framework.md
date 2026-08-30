# IF-TRACK — Immersive Framework

Status: **Active — current implemented baseline + Stage B consumer evidence**  
Last updated: **2026-08-30**

## Authority and status model

```text
Accepted ADRs    -> normative architecture
Governance       -> cross-cutting compatibility/product policy
Reconciliation   -> current technical alignment/certification
Tracker          -> current mutable delivery state
FIRSTGAME        -> Stage B real-consumer evidence
Archive          -> historical/non-authoritative execution history
```

A dated certification remains evidence for the boundary it executed. Later cuts add evidence; they do not retroactively relabel historical matrices.

## Current Player state

The Player architecture is now reconciled through IF-ADR-023.

```text
Local Player Host
└── ActorMount
    └── PlayerActorRuntimeHost
        ├── PlayerActorDeclaration
        └── PresentationMount
            └── ActorProfile.PresentationPrefab
```

Current transaction split:

```text
Join
!= Actor Selection
!= Activity Actor Preparation
!= Physical Materialization
```

Current Player evidence:

```text
Historical Full Player          25/25 preserved
Player current aggregate        27/27 PASS
Manager functional Player QA    14/14 PASS
Pause/Input/Gate                 8/8 PASS
Route Spatial Entry             18/18 PASS
Activity Relocation             23/23 PASS
```

Current Player sample evidence from FIRSTGAME FG-ADR-002 Revision 4:

```text
Getting Started / Scene Player  PROVEN
Player Provisioning             PLAY MODE PROVEN
Character Selection             PLAY MODE PROVEN
Local Multiplayer               PLANNED / BLOCKED
```

Local Multiplayer remains blocked by public Slot/device/InputUser/control-scheme ownership/observation semantics. Arbitrary Actor Selection is delivered and is not a blocker.

## Current ADR status

| ADR | Architecture / package | Technical QA | Current disposition |
|---|---|---|---|
| 001 | ACCEPTED / RECONCILED / IMPLEMENTED | core evidence preserved; Editor startup isolation proven | current |
| 002 | ACCEPTED / RECONCILED / IMPLEMENTED | feature-owned | current |
| 003 | ACCEPTED / RECONCILED / IMPLEMENTED; ADR-023 structural reconciliation current | Player aggregate 27/27 + Manager functional 14/14 | current |
| 004 | ACCEPTED / RECONCILED / IMPLEMENTED | Camera 53/53 for certified boundary | current; broader consumer proof feature-owned |
| 005 | ACCEPTED / RECONCILED / IMPLEMENTED | Input Gate / Restart / Pause certified | current |
| 006 | ACCEPTED / RECONCILED / IMPLEMENTED | technical Transition/Loading certified | Game Flow consumer PASS |
| 007 | ACCEPTED / RECONCILED / IMPLEMENTED | readiness policies certified | Game Flow consumer PASS |
| 008 | ACCEPTED / RECONCILED / IMPLEMENTED | feature-owned | persistent composition consumer evidence present |
| 009 | ACCEPTED / RECONCILED / IMPLEMENTED | certified | current |
| 010 | ACCEPTED / IMPLEMENTED | feature-owned | current |
| 011 | ACCEPTED / RECONCILED / IMPLEMENTED | readiness/progress certified | consumer proof PASS |
| 012 | ACCEPTED / RECONCILED / IMPLEMENTED | Player aggregate 27/27 | current |
| 013 | ACCEPTED / EXPERIMENTAL / IMPLEMENTED | Audio/BGM certified | consumer gate PASS; maturity remains Experimental |
| 014 | ACCEPTED / IMPLEMENTED | certified | current |
| 015 | ACCEPTED / RECONCILED / IMPLEMENTED | public surface aggregate + Manager functional 14/14 | Observer + 8 explicit commands current |
| 016 | ACCEPTED / IMPLEMENTED | Player aggregate 27/27 | ResolveConfiguredDefault + LeaveUnresolved current |
| 017 | ACCEPTED / RECONCILED / IMPLEMENTED | frame-rate matrices certified | current |
| 018 | ACCEPTED / RECONCILED / IMPLEMENTED | persistence/backend certifications | FIRSTGAME usability proof remains feature-owned |
| 019 | ACCEPTED / RECONCILED / IMPLEMENTED | current aggregate + historical physical-lifetime certification | closed |
| 020 | ACCEPTED / RECONCILED / IMPLEMENTED | ADR020-H + aggregate + historical certification | closed |
| 021 | ACCEPTED / RECONCILED / IMPLEMENTED | Route 18/18 + Activity 23/23 + aggregate 27/27 | Model B current |
| 022 | ACCEPTED / IMPLEMENTED | presentation 14/14; Camera aggregate boundary certified | broader FIRSTGAME C6 remains separate |
| 023 | ACCEPTED / IMPLEMENTED / TECHNICAL QA CERTIFIED | Manager functional 14/14 + Pause/Input/Gate 8/8 | PlayerActorRuntimeHost + PresentationPrefab current; FIRSTGAME Player Provisioning/Character Selection proven |

## Current Player scoped closure — IF-ADR-023 — 2026-08-29

Current architecture:

```text
Local Player Host composition
  -> reusable PlayerActorRuntimeHost

ActorProfile
  -> Actor-specific PresentationPrefab
```

Removed current authority:

```text
ActorProfile.LogicalActorHostPrefab
LogicalActorHost
SceneLogicalPlayerActorEvidence
HasLogicalActor
```

`LogicalActorsPrepared` remains semantic readiness terminology.

Current scoped-access reconciliation:

```text
Route scope     = Route lifecycle ownership
Activity scope  = Activity lifecycle ownership
scene location  != scope authority
```

Current teardown rule:

```text
consumer may die before persistent runtime owner
→ consumer-side binding releases on OnDestroy
→ later owner release tolerates destroyed Unity wrapper
→ diagnostics do not dereference destroyed object
```

Certification record:

[IF-ADR-023 Player Actor Runtime Technical Certification — 2026-08-29](../Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)

## Current Stage B / FIRSTGAME priorities

1. **Player** — Scene Player, Player Provisioning and Character Selection are proven. Local Multiplayer is next planned but blocked by the public Slot/device/input contract.
2. **Loading / Readiness** — positive Game Flow consumer lane proven; negative/terminal robustness remains QA-owned.
3. **Camera** — Default-output integration proven; broader ADR-022 consumer coverage remains feature-owned.
4. **Pause** — runtime certified; remaining work is consumer authoring/usability only.
5. **Audio** — BGM technical + consumer integration proven; API maturity promotion is separate.
6. **Progression Save** — real consumer persistence/usability proof remains.
7. **Editor/Product Surface** — continue feature-owned Inspector/workflow evidence under ADR-010.

## Future / deferred contracts

- exact-Slot public Join;
- public Slot/device/InputUser/control-scheme ownership observation;
- device disconnect/reconnect and reassignment semantics;
- heterogeneous per-Slot Host Provisioning;
- consumer-facing prepared physical Actor hot-swap;
- generic respawn/checkpoint/dynamic Spawn beyond ADR-021;
- additional Camera families / split-screen / multiple outputs;
- application-scoped stable-ID resolver;
- Session-scoped frame-rate override;
- persisted frame-rate preference integration;
- advanced BGM simultaneous-source/crossfade semantics unless separately accepted.

`PLAYER-COMMAND-SURFACE-READINESS / DEFERRED` remains a product-availability concern: valid authored commands may be runtime-unbound and must reject without fallback until live scoped access exists.

## Current architecture / reconciliation records

- [IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md)
- [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md)
- [IF-ADR-020 — Session Player Leave and Resource Release Authority](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md)
- [IF-ADR-021 — Route Spatial Entry and Activity Explicit Relocation](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md)
- [IF-ADR-022 — Camera Rig Presentation Models](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md)
- [IF-ADR-023 — Player Actor Runtime Host and Presentation Authority](../ADRs/IF-ADR-023-Player-Actor-Runtime-Host-and-Presentation-Authority.md)
- [Player Current Aggregate Recertification — 2026-08-24](../Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)
- [IF-ADR-015B — Player Actor Selection Public Surface Certification — 2026-08-26](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)
- [IF-ADR-023 Player Actor Runtime Technical Certification — 2026-08-29](../Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)

## Documentation maintenance

- accepted ADRs remain normative;
- reconciliation records hold dated technical evidence;
- this tracker holds current mutable state, not full execution history;
- FIRSTGAME evidence is consumer/product evidence;
- historical certification counts are preserved rather than rewritten to imply later coverage.
