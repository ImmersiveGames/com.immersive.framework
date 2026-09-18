# IF-TRACK — Immersive Framework

Status: **Active — current implemented baseline + Stage B consumer evidence; CAMERA-028 A/B certified, C focused behavior PASS, D implemented/tested/integrated with 33/33 functional PASS; final ADR-028 validation and consumer closure remain open**

Last updated: **2026-09-17**

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

The Player target architecture is reconciled through IF-ADR-023 / IF-ADR-023A for
Actor composition and occurrence identity, plus IF-ADR-024 for Manager-Provisioned
prepared physical Actor replacement. Scene-Provided authoring validation, transient
resolution and runtime adoption remain implemented; derived evidence and Player
Apply / Rebuild are removed.

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
!= Prepared Actor Replacement
```

Current prepared Actor replacement public boundary:

```text
IPlayerSessionScopedAccess.RequestReplacePreparedActor(...)
  Manager-Provisioned V1 only
  same Player Slot / Host / PlayerInput / Session / Activity occurrence
  Actor A Prepared + GameplayReady
    -> release A contextual gameplay
    -> release A occupancy by exact preparation ownership
    -> replace A -> B
    -> establish B gameplay
    -> reconcile readiness
```

Current Player Actor identity boundary:

```text
AUTHORED / UNPREPARED
  PlayerActorDeclaration.actorId = empty

→ physical preparation establishes runtime occurrence identity

IDENTITY ESTABLISHED / PREPARING
  typed PlayerActorDeclaration.ActorId is valid

→ commit

PREPARED / COMMITTED
  physical preparation evidence retained
```

`PlayerActorDeclaration.ActorId` is runtime occurrence identity. It is not a persistent prefab/template identity. Ordinary persistent `ActorDeclaration` identity rules remain separate.

Current Player evidence:

```text
Historical Full Player          25/25 preserved
Player current aggregate        27/27 PASS
Manager functional Player QA    14/14 historical/current earlier boundary
ADR-024 Full Player QA          16/16 PASS
Pause/Input/Gate                 8/8 PASS
Route Spatial Entry             18/18 PASS
Activity Relocation             23/23 PASS
Scene-Provided occurrence ID    FIRSTGAME Play Mode PASS
```

The ADR-024 `16/16` run is the current integrated proof for the Manager-Provisioned
prepared Actor replacement boundary. The older `14/14` Manager functional and
`27/27` aggregate records remain dated evidence for the earlier boundaries they
executed and are not relabeled as ADR-024 proof.

Current Player sample evidence from FIRSTGAME FG-ADR-002 Revision 4 plus the 2026-08-31 Scene-Provided reconciliation run:

```text
Getting Started / Scene Player  PROVEN
  LogicalActorsPrepared         READY / PASS
  GameplayReady                 READY / PASS
Player Provisioning             PLAY MODE PROVEN
Character Selection             PLAY MODE PROVEN
Local Multiplayer               PLANNED / BLOCKED
```

`GameplayReady` proves the current contextual gameplay projection over retained prepared Session Players. It does not by itself certify game-owned locomotion, camera composition, concrete gameplay input consumers or Presentation completeness.

Local Multiplayer remains blocked by public Slot/device/InputUser/control-scheme ownership/observation semantics. Arbitrary Actor Selection and Manager-Provisioned prepared Actor replacement are delivered and are not blockers.

## Current ADR status

| ADR | Architecture / package | Technical QA | Current disposition |
|---|---|---|---|
| 001 | ACCEPTED / RECONCILED / IMPLEMENTED | core evidence preserved; Editor startup isolation proven | current |
| 002 | ACCEPTED / RECONCILED / IMPLEMENTED | feature-owned | current |
| 003 | ACCEPTED / RECONCILED / IMPLEMENTED; ADR-023 structural reconciliation current | Player aggregate 27/27 + Manager functional 14/14 | current |
| 004 | ACCEPTED / REOPENED BY 026/028 for evolved output topology/layout; request arbitration and Default semantics preserved | historical + corrected Camera regression evidence retained | request/output core retained; ADR-028 final validation/consumer closure pending |
| 005 | ACCEPTED / RECONCILED / IMPLEMENTED | Input Gate / Restart / Pause certified | current |
| 006 | ACCEPTED / RECONCILED / IMPLEMENTED | technical Transition/Loading certified | Game Flow consumer PASS |
| 007 | ACCEPTED / RECONCILED / IMPLEMENTED | readiness policies certified | Game Flow consumer PASS |
| 008 | ACCEPTED / RECONCILED / IMPLEMENTED | feature-owned | persistent composition consumer evidence present |
| 009 | ACCEPTED / RECONCILED / IMPLEMENTED / TECHNICAL QA CERTIFIED | Contribution 3/3 + Visibility 2/2 + lifecycle 16/16 | current post-split contract |
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
| 022 | PRESENTATION FAMILY ACCEPTED / IMPLEMENTED; target selection replaced by 026 and reusable authoring refined by 027 | presentation 14/14 historical; retained regressions in corrected Full Camera | presentation/materialization boundary preserved |
| 023 | ACCEPTED / authored composition implementation complete; ADR-023A occurrence identity boundary current | Manager functional 14/14 + Pause/Input/Gate 8/8 + FIRSTGAME Scene-Provided readiness PASS | Physical Scene-Provided validation/resolution/adoption is canonical; derived evidence, runtime evidence validation, Player Apply/Rebuild and obsolete evidence type removed |
| 024 | ACCEPTED / RECONCILED / IMPLEMENTED — Manager-Provisioned V1 | Full Player QA 16/16 PASS including positive `actor-replace` | public `RequestReplacePreparedActor(...)` current; Scene-Provided prepared physical replacement deferred |
| 025 | ACCEPTED / IMPLEMENTATION STATUS OWNED BY PLAYER TRACK | feature-owned | Camera remains outside the Player input contract |
| 026 | REOPENED — A..G retained; H superseded; H2 certified; I certified 2026-09-16 | corrected Full Camera 39/39 + ADR-026 2/2 + Player→Camera integration PASS | viewport-free topology and Player→Camera Subject integration current |
| 027 | REOPENED — A/B/C retained; D2 certified; E deferred; F final closure waits corrected boundary | D2 corrected logical association certified 2026-09-14 | authoring viewport removed; final consumer closure pending |
| 028 | ACCEPTED / IMPLEMENTED — A/B certified; C focused behavior PASS; D implemented/tested/integrated | CAMERA-028-D 33/33 focused functional PASS; adjacent regressions PASS | VALIDATION OPEN — read-only preflight + cleanup/reentrancy proof pending |

## Current Activity content / visibility closure — IF-ADR-009 — 2026-08-30

Current architecture:

```text
ActivityContentContribution
  -> Activity ownership
  -> Local Content Id
  -> Required / Optional
  -> Activity content lifecycle

ActivityVisibilityRule
  -> presentation only
  -> no ownership
  -> no Requiredness
  -> no Activity content lifecycle authority
```

Current post-split QA:

```text
Contribution Authority     3/3  PASS
Visibility Isolation       2/2  PASS
Lifecycle regression      16/16 PASS
------------------------------------
Current post-split evidence 21/21 PASS
```

The lifecycle regression explicitly proves that Visibility membership does not broaden
Contribution ownership. Presentation may change with zero Contribution callbacks, and
Contribution lifecycle may exit while presentation remains visible for another listed
Activity.

The historical ADR-009 `46`-case certification remains dated evidence for the earlier
combined boundary only.

Certification record:

[IF-ADR-009 Contribution / Visibility Technical Certification — 2026-08-30](../Reconciliation/IF-ADR-009-CONTRIBUTION-VISIBILITY-TECHNICAL-CERTIFICATION-2026-08-30.md)

## Current Player scoped closure — IF-ADR-023 / IF-ADR-023A / IF-ADR-024 — 2026-09-02

Current architecture:

```text
Local Player Host composition
  -> reusable PlayerActorRuntimeHost

ActorProfile
  -> Actor-specific PresentationPrefab

PlayerActorDeclaration
  -> authored occurrence ID empty
  -> runtime occurrence identity established by physical preparation
```

Removed current authority:

```text
ActorProfile.LogicalActorHostPrefab
LogicalActorHost
SceneLogicalPlayerActorEvidence
HasLogicalActor
persistent authored PlayerActorDeclaration occurrence IDs
```

`LogicalActorsPrepared` remains semantic readiness terminology.

Current occurrence-identity invariant:

```text
before physical preparation boundary
  typed Player Actor occurrence ActorId unavailable

after identity establishment boundary
  typed PlayerActorDeclaration.ActorId valid

after preparation commit
  retained physical evidence is authoritative
```

Current Scene-Provided Play Mode proof:

```text
LogicalActorsPrepared
  Activity readiness = Ready
  projected = 1
  selected = 1
  prepared = 1
  failed = 0

GameplayReady
  Activity readiness = Ready
  projected = 1
  selected = 1
  prepared = 1
  failed = 0
```

Current Manager-Provisioned prepared replacement invariant:

```text
A Prepared + GameplayReady
  -> release contextual gameplay A
  -> release occupancy A by exact preparation ownership
  -> physical replacement A -> B
  -> canonical gameplay projection B
  -> same Activity occurrence readiness
```

Recoverable pre-commit failures restore A through canonical gameplay projection.
Post-commit gameplay failure keeps B authoritative and reports the committed
degraded state. `Slot.Revision` is mutable revision evidence and is not treated as
immutable Player occurrence identity.

Current ADR-024 integrated proof:

```text
[QA_PLAYER_FULL]
status = Passed
verdict = PLAYER QA CERTIFIED
cases = 16/16
```

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

Certification and reconciliation records:

- [IF-ADR-023 Player Actor Runtime Technical Certification — 2026-08-29](../Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)
- [IF-ADR-023A Player Actor Occurrence Identity Boundary — 2026-08-31](../Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)
- [IF-ADR-024 Prepared Actor Replacement Technical Certification — 2026-09-02](../Reconciliation/IF-ADR-024-PREPARED-ACTOR-REPLACEMENT-TECHNICAL-CERTIFICATION-2026-09-02.md)

## Current Camera architecture — IF-ADR-026 / IF-ADR-027 / IF-ADR-028 — 2026-09-17

Current normative model:

```text
Camera Subject
  -> observable evidence

Camera Assignment
  -> 0..N Subjects per logical View

Camera Rig / Presentation
  -> observes resolved Subjects

Camera Output
  -> explicit physical Camera capacity

View→Output association
  -> logical View identity + Output identity only

Output Presentation / Layout
  -> separate authority for viewport / display / RenderTexture / PiP
```

Output registration and View participation are distinct:

```text
Session available Outputs       -> 1..N
current View→Output bindings     -> 0..N subset
```

An available Output without a current View binding is valid. Player count does not determine Output count or binding count.

Current Camera authoring:

```text
Camera Subject
Camera View Definition
Camera Rig Behavior Definition
Camera Output Definition
logical View→Output association
```

Physical presentation remains externally owned. CAMERA-028-C code reconciliation is implemented and has focused preservation evidence; CAMERA-028-D integrates explicit Player Slot → Camera Output identity with `PlayerInput.camera` while Unity `PlayerInputManager` remains the split-screen layout writer.

The designer does not normally transport `CameraViewId`, `CameraOutputId`, Assignment Context ID, Assignment Owner ID or technical binding text between official Camera Inspectors. Typed definition references remain authoring authority; stable IDs remain runtime/diagnostic projections.

The viewport-bearing Camera association is now removed from the active Camera topology/authoring contract.

Historical integrated evidence:

```text
[QA_CAMERA_FULL]
2026-09-12
39/39 PASS
ADR-026 phases 2/2
9/9 dimensions
```

This remains valid evidence for the earlier viewport-bearing boundary and is not relabeled.

Current corrected evidence:

```text
Persistent Camera Presentation Composition regression  12/12 PASS
invalid-viewport                                       SupersededByCAMERA028B
CAMERA-028-A Partial                                   8/8 PASS
outputParticipation                                    PASS
Full Camera established cases                          39/39 PASS
ADR-026 phases                                         2/2 PASS
current dimensions                                     8/8 PASS
viewOutputAssociation                                  PASS
viewportSplitTopology                                  REMOVED
Shared baseline restore                                PASS
```

Current closure disposition:

```text
CAMERA-026-A..G    IMPLEMENTED / RETAINED
CAMERA-026-H       SUPERSEDED
CAMERA-026-H2      IMPLEMENTED / TECHNICALLY CERTIFIED — revalidated 2026-09-14
CAMERA-026-I       IMPLEMENTED / TECHNICALLY CERTIFIED — 2026-09-16
CAMERA-027-A..C    IMPLEMENTED / RETAINED
CAMERA-027-D2      IMPLEMENTED / TECHNICALLY CERTIFIED — 2026-09-14
CAMERA-027-E       DEFERRED / OPTIONAL
CAMERA-027-F       FINAL CONSUMER CLOSURE PENDING
CAMERA-028-A       IMPLEMENTED / TECHNICALLY CERTIFIED — 8/8 PASS
CAMERA-028-B       IMPLEMENTED / TECHNICALLY CERTIFIED — 2026-09-14
CAMERA-028-C       IMPLEMENTED; FOCUSED BEHAVIOR PASS — 2026-09-17
CAMERA-028-D       IMPLEMENTED / TESTED / INTEGRATED; 33/33 FUNCTIONAL PASS — 2026-09-17
ADR-028 validation OPEN — read-only preflight + cleanup/reentrancy proof pending
```

IF-ADR-028 implementation and focused integration work are present. The remaining technical gate is validation of clean-state/reentrancy: read-only preflight before repair, QA-owned device cleanup before terminal PASS, post-cleanup verification, and a second run without reparative preparation.

Historical certification record:

[Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)

Current reconciliation record:

[Camera Output Participation and Layout Authority Reconciliation — 2026-09-12](../Reconciliation/IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)

## Current Stage B / FIRSTGAME priorities

1. **Player** — Scene Player physical/contextual lifecycle is proven through `GameplayReady`; Player Provisioning and Character Selection are proven. Manager-Provisioned prepared Actor replacement is technically certified in QA. Remaining Getting Started work is game-owned Presentation/gameplay completeness, not Framework Player readiness. Local Multiplayer remains blocked by the public Slot/device/input contract.
2. **Loading / Readiness** — positive Game Flow consumer lane proven; negative/terminal robustness remains QA-owned.
3. **Camera** — CAMERA-026-H2/I, CAMERA-027-D2 and CAMERA-028-A/B are technically certified; CAMERA-028-C has focused behavior PASS and CAMERA-028-D is implemented/tested/integrated with 33/33 focused functional PASS. Do **not** close CAMERA-027-F before ADR-028 clean-state/cleanup/reentrancy validation is proven. No new Framework physical-layout authority is required.
4. **Pause** — runtime certified; remaining work is consumer authoring/usability only.
5. **Audio** — BGM technical + consumer integration proven; API maturity promotion is separate.
6. **Progression Save** — real consumer persistence/usability proof remains.
7. **Editor/Product Surface** — continue feature-owned Inspector/workflow evidence under ADR-010.

## Future / deferred contracts

- exact-Slot public Join;
- public Slot/device/InputUser/control-scheme ownership observation;
- device disconnect/reconnect and reassignment semantics;
- heterogeneous per-Slot Host Provisioning;
- Scene-Provided prepared Actor replacement, pending an explicit physical-ownership contract;
- generic respawn/checkpoint/dynamic Spawn beyond ADR-021;
- ADR-028 validation-harness clean-start / cleanup / reentrancy closure;
- CAMERA-028-D final validation after read-only preflight and device-cleanup ordering are proven;
- CAMERA-027-F final official Samples/FIRSTGAME Camera migration after corrected technical closure;
- CAMERA-027-E optional reusable Camera Composition definition, deferred until consumer evidence justifies a grouped asset;
- application-scoped stable-ID resolver;
- Session-scoped frame-rate override;
- persisted frame-rate preference integration;
- advanced BGM simultaneous-source/crossfade semantics unless separately accepted.

`PLAYER-COMMAND-SURFACE-READINESS / DEFERRED` remains a product-availability concern: valid authored commands may be runtime-unbound and must reject without fallback until live scoped access exists.

## Current architecture / reconciliation records

- [IF-ADR-009 — Activity Local Visibility Rules](../ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md)
- [IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md)
- [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md)
- [IF-ADR-020 — Session Player Leave and Resource Release Authority](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md)
- [IF-ADR-021 — Route Spatial Entry and Activity Explicit Relocation](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md)
- [IF-ADR-022 — Camera Rig Presentation Models](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md)
- [IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology](../ADRs/IF-ADR-026-Camera-Subjects-Assignment-and-Multi-Output-Topology.md)
- [IF-ADR-027 — Camera Authoring Definitions and Composition Authority](../ADRs/IF-ADR-027-Camera-Authoring-Definitions-and-Composition-Authority.md)
- [IF-ADR-028 — Camera Output Participation and Presentation Layout Authority](../ADRs/IF-ADR-028-Camera-Output-Participation-and-Presentation-Layout-Authority.md)
- [Camera Output Participation and Layout Authority Reconciliation — 2026-09-12](../Reconciliation/IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)
- [IF-ADR-028 Focused Physical Presentation / PlayerInput Validation — 2026-09-17](../Reconciliation/IF-ADR-028-FOCUSED-VALIDATION-2026-09-17.md)
- [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)
- [IF-ADR-026 Shared Camera Technical Certification — 2026-09-09](../Reconciliation/IF-ADR-026-SHARED-CAMERA-TECHNICAL-CERTIFICATION-2026-09-09.md)
- [IF-ADR-023 — Player Actor Runtime Host and Presentation Authority](../ADRs/IF-ADR-023-Player-Actor-Runtime-Host-and-Presentation-Authority.md)
- [IF-ADR-024 — Prepared Actor Replacement Public Contract](../ADRs/IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md)
- [IF-ADR-009 Contribution / Visibility Technical Certification — 2026-08-30](../Reconciliation/IF-ADR-009-CONTRIBUTION-VISIBILITY-TECHNICAL-CERTIFICATION-2026-08-30.md)
- [Player Current Aggregate Recertification — 2026-08-24](../Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)
- [IF-ADR-015B — Player Actor Selection Public Surface Certification — 2026-08-26](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)
- [IF-ADR-023 Player Actor Runtime Technical Certification — 2026-08-29](../Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)
- [IF-ADR-023A Player Actor Occurrence Identity Boundary — 2026-08-31](../Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)
- [IF-ADR-024 Prepared Actor Replacement Technical Certification — 2026-09-02](../Reconciliation/IF-ADR-024-PREPARED-ACTOR-REPLACEMENT-TECHNICAL-CERTIFICATION-2026-09-02.md)

## Documentation maintenance

- accepted ADRs remain normative;
- reconciliation records hold dated technical evidence;
- this tracker holds current mutable state, not full execution history;
- FIRSTGAME evidence is consumer/product evidence;
- historical certification counts are preserved rather than rewritten to imply later coverage.