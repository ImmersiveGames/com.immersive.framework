# IF-TRACK — Immersive Framework

Status: **Active — stable OpenUPM release 1.0.2; preview 1.1.0-preview.5 published; Reset and IF-ADR-044 consumer proof PASS; IF-ADR-039 command boundary consumer-validated; IF-ADR-041 Route-scoped Activity observation validated; broader Camera/Player validation tracked separately**

Last updated: **2026-10-09**

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

## Public package distribution

The public OpenUPM graph is published:

| Package | Version | GitHub Release | OpenUPM |
|---|---:|---|---|
| `com.immersive.foundation` | `0.2.1` | [v0.2.1](https://github.com/ImmersiveGames/com.immersive.foundation/releases/tag/v0.2.1) | [package](https://openupm.com/packages/com.immersive.foundation/) |
| `com.immersive.logging` | `0.2.2` | [v0.2.2](https://github.com/ImmersiveGames/com.immersive.logging/releases/tag/v0.2.2) | [package](https://openupm.com/packages/com.immersive.logging/) |
| `com.immersive.pooling` | `0.2.1` | [v0.2.1](https://github.com/ImmersiveGames/com.immersive.pooling/releases/tag/v0.2.1) | [package](https://openupm.com/packages/com.immersive.pooling/) |
| `com.immersive.audio` | `0.2.2` | [v0.2.2](https://github.com/ImmersiveGames/com.immersive.audio/releases/tag/v0.2.2) | [package](https://openupm.com/packages/com.immersive.audio/) |
| `com.immersive.framework` | `1.0.2` | [v1.0.2](https://github.com/ImmersiveGames/com.immersive.framework/releases/tag/v1.0.2) | [package](https://openupm.com/packages/com.immersive.framework/) |

Preview **`1.1.0-preview.5`** is published to GitHub Releases and OpenUPM. Its
signed archive is attached to [the GitHub Release](https://github.com/ImmersiveGames/com.immersive.framework/releases/tag/v1.1.0-preview.5),
and OpenUPM resolves `.5` as the current preview. It depends on Foundation
`0.2.2`, Logging `0.2.3`, Cinemachine `3.1.7` and Input System `1.19.0`. The
`v1.1.0-preview.1` tag
stopped at signed-archive verification; no GitHub Release or OpenUPM publication
was created for it. Package import/compile and broader Camera/Player validation
remain pending.

Registry dependency graph:

```text
com.immersive.framework 1.0.2
├── com.immersive.foundation 0.2.1
└── com.immersive.logging 0.2.2

com.immersive.audio 0.2.2
└── com.immersive.pooling 0.2.1
```

Distribution status: **PUBLISHED / PENDING UNITY COMPILE/IMPORT VALIDATION**.

## Current Reset integration QA evidence

On 2026-10-06 the current RESET-035-B extensions were executed in Unity Play Mode. `QA-NEW-002` passed the controlled Activity readiness/rollback and owner-release path with `BaselineRestored`; `QA-NEW-003` passed Route A → B → new A with owner survival, exact lifecycle observations and `BaselineRestored`. Both ended with empty `firstDivergence` and `cleanupIssue`. This closes the integration execution gate represented by those two fixtures without relabeling older historical certifications.

## Current Player state

The Player target architecture is defined by IF-ADR-038 for Actor composition,
physical/spatial authority and Camera Subject identity, plus IF-ADR-024 for
Manager-Provisioned prepared Actor replacement. Scene-Provided authoring validation, transient
resolution and runtime adoption remain implemented; derived evidence and Player
Apply / Rebuild are removed.

```text
Local Player Host
└── ActorMount
    └── PlayerActorRuntimeHost
        ├── PlayerActorDeclaration
        ├── Actor occurrence root / physical state
        ├── ActorCameraSubjectAuthoring / ObservationTransform
        └── optional subordinate visual content
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

Foundational Player sample evidence from FIRSTGAME FG-ADR-002 Revision 4 and the 2026-08-31 Scene-Provided reconciliation run:

```text
Getting Started / Scene Player  PROVEN
  LogicalActorsPrepared         READY / PASS
  GameplayReady                 READY / PASS
Player Provisioning             PLAY MODE PROVEN
```

Current sample migration and Play Mode evidence:

```text
Character Selection             IMPLEMENTED / INTEGRATED / MANUAL PLAY MODE PASS
Local Multiplayer               IMPLEMENTED / INTEGRATED / MANUAL PLAY MODE PASS
Character Selection Split Screen IMPLEMENTED / INTEGRATED / MANUAL PLAY MODE PASS
```

`GameplayReady` proves the current contextual gameplay projection over retained prepared Session Players. It does not by itself certify game-owned locomotion, camera composition, concrete gameplay input consumers or Presentation completeness.

## Current ADR status

| ADR | Architecture / package | Technical QA | Current disposition |
|---|---|---|---|
| 001 | ACCEPTED / RECONCILED / IMPLEMENTED | core evidence preserved; Editor startup isolation proven | current |
| 002 | ACCEPTED / RECONCILED / IMPLEMENTED | feature-owned | current |
| 003 | ACCEPTED / RECONCILED / IMPLEMENTED; ADR-023 structural reconciliation current | Player aggregate 27/27 + Manager functional 14/14 | current |
| 005 | ACCEPTED / RECONCILED / IMPLEMENTED | Input Gate / Restart / Pause certified | current |
| 006 | ACCEPTED / RECONCILED / IMPLEMENTED | technical Transition/Loading certified | Game Flow consumer PASS |
| 007 | ACCEPTED / RECONCILED / IMPLEMENTED | readiness policies certified | Game Flow consumer PASS |
| 008 | ACCEPTED / RECONCILED / IMPLEMENTED | feature-owned | persistent composition consumer evidence present |
| 009 | ACCEPTED / RECONCILED / IMPLEMENTED / TECHNICAL QA CERTIFIED | Contribution 3/3 + Visibility 2/2 + lifecycle 16/16 | current post-split contract |
| 010 | ACCEPTED / IMPLEMENTED | feature-owned | current |
| 011 | ACCEPTED / RECONCILED / IMPLEMENTED | readiness/progress certified | consumer proof PASS |
| 012 | ACCEPTED / RECONCILED / IMPLEMENTED | Player aggregate 27/27 | current |
| 013 | ACCEPTED / EXPERIMENTAL / IMPLEMENTED / VALIDATED | QA-NEW-005 12/12 PASS post-IF-ADR-040; historical 44/44 retained | consumer gate PASS; maturity remains Experimental |
| 014 | ACCEPTED / IMPLEMENTED | certified | current |
| 015 | ACCEPTED / RECONCILED / IMPLEMENTED | public surface aggregate + Manager functional 14/14 | Observer + 8 explicit commands current |
| 016 | ACCEPTED / IMPLEMENTED | Player aggregate 27/27 | ResolveConfiguredDefault + LeaveUnresolved current |
| 017 | ACCEPTED / RECONCILED / IMPLEMENTED | frame-rate matrices certified | current |
| 018 | ACCEPTED / RECONCILED / IMPLEMENTED | persistence/backend certifications | FIRSTGAME usability proof remains feature-owned |
| 019 | ACCEPTED / RECONCILED / IMPLEMENTED | current aggregate + historical physical-lifetime certification | closed |
| 020 | ACCEPTED / RECONCILED / IMPLEMENTED | ADR020-H + aggregate + historical certification | closed |
| 021 | ACCEPTED / RECONCILED / IMPLEMENTED | Route 18/18 + Activity 23/23 + aggregate 27/27 | Model B current |
| 023 | SUPERSEDED by IF-ADR-038; dated certification retained as evidence | Historical Manager/Scene-Provided evidence only | Optional visual-content lifecycle remains; Actor root owns spatial state |
| 024 | ACCEPTED / RECONCILED / IMPLEMENTED — Manager-Provisioned V1 | Full Player QA 16/16 PASS including positive `actor-replace` | public `RequestReplacePreparedActor(...)` current; Scene-Provided prepared physical replacement deferred |
| 025 | ACCEPTED / IMPLEMENTATION STATUS OWNED BY PLAYER TRACK | feature-owned | Camera remains outside the Player input contract |
| 032 | SUPERSEDED / HISTORICAL | prior Camera QA/certifications remain historical only | superseded by IF-ADR-038 |
| 038 | ACCEPTED; current Assignment/Output model implemented | Framework EditMode 175/175; Camera Editor 78/78 PASS; QA-NEW-004 Session Camera 9/9 PASS; IF-ADR-039 GameFlow command consumer Play Mode PASS | broader IF-ADR-038 recertification remains separately scoped |
| 039 | ACCEPTED / IMPLEMENTED / TESTED / INTEGRATED / VALIDATED | Framework EditMode 169/169; RouteLifecycle 6/6; Camera 74/74; Route-scoped GameFlow Play Mode PASS | command boundary and migrated consumer proof closed |
| 041 | ACCEPTED / IMPLEMENTED / TESTED / INTEGRATED / VALIDATED | Framework EditMode 169/169; RouteLifecycle observer 6/6; GameFlow `Hub -> A -> B -> A -> C -> B -> Hub` Play Mode PASS | one Route-scoped adapter in `SCN_GameFlow_Basic`; ActivityFlow remains authority |
| 042 | ACCEPTED / IMPLEMENTED / QA CERTIFIED | Dedicated QAFramework Play Mode certification PASS 7/7 on 2026-10-07 with `BaselineRestored`; Local Multiplayer manual consumer validation PASS 2026-10-06; Camera Editor 78/78 PASS (test sources, execution scope recorded separately) | focused SharedGroup gate closed; broader IF-ADR-038 recertification remains open |
| 043 | ACCEPTED / IMPLEMENTED / TESTED / INTEGRATED / VALIDATED | Individual Output participation manually validated; Framework EditMode 175/175, Camera 78/78, PlayerCameraOutputIntegration 3/3, SessionCameraAssignmentRuntime 37/37; focused QAFramework 7/7 PASS on 2026-10-07 with 10/10 monitored frames covered and `BaselineRestored` | focused physical-participation gate closed; broader IF-ADR-038 recertification remains separate |
| 044 | ACCEPTED / IMPLEMENTED / INTEGRATED / VALIDATED | Public occurrence-scoped block/release token, Session-lifetime composition, canonical input adapter projection and Leave cleanup; QAFramework IF-ADR-044 **8/8 PASS** with `BaselineRestored`; `PlayerGameplayAvailabilityBlockTrigger` manually validated in Local Multiplayer on 2026-10-08 for P1/P2 Block/Release, independent ownership, teardown cleanup and Pause composition | current; gameplay turn/active-player cycling remains consumer-owned and outside Framework scope |
| 045 | ACCEPTED / IMPLEMENTATION PENDING | Contract accepted 2026-10-09: path-first identity; `ContainerScene` Stable through 1.x as non-authoritative legacy snapshot; Single Load terminal failure states make no false rollback guarantee; IF-ADR-040 boundary preserved | source-level mismatch confirmed; consumer WebGL incident remains unreproduced; baseline Framework `fca1dd130bbd9a953526c6b7b21fedead4ab1e07`, QAFramework `55c30d477c58432ffc3a6dcf3194020620e82070`; implementation route: [IF-PLAN-ADR-045 v1](../Plans/IF-PLAN-ADR-045-RUNTIME-SAFE-SCENE-REFERENCES.v1.md) |

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
  -> Actor occurrence selection/configuration (implemented; optional visual content is subordinate)

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

## Current Camera and Actor occurrence target — IF-ADR-038 — 2026-09-30

~~~text
GameApplication startup -> Session Camera Assignment -> Occurrence -> Membership / Subject -> Output
Empty SharedGroup -> Output fallback
~~~

Session Camera authority explicitly activates Assignments. Route and Activity do not
own Assignment state, membership, or Output routing; Join/Leave changes membership
only. SharedGroup retains one occurrence per Assignment/Output and reconciles its
current eligible Actor Subjects into the TargetGroup. The Actor occurrence root owns
Player physical/spatial state and pose; its explicit ObservationTransform owns Camera
observation, and Actor replacement updates Subject evidence while preserving Player
and Camera Occurrence identity. Output, fallback, and PlayerInputManager physical
split-layout ownership remain.

The Player runtime ownership cut and the Session Camera runtime path are present in
source. `FrameworkRuntimeHost` materializes Outputs, creates Assignment occurrences,
integrates Player membership and routes transition coverage through Output fallback.
The assignment runtime supports Session-scoped, Individual and Shared Group modes,
membership/Subject reconciliation, and transactional replacement.

| Cut | Source status | Repository evidence | Remaining gate |
|---|---|---|---|
| CAMERA-038-A — Actor occurrence authority | Implemented in source | Actor-root ownership, optional visual content and explicit Subject authoring; Editor coverage exists | SceneProvided and ManagerProvisioned Unity revalidation |
| CAMERA-038-B — Definition / Assignment / Occurrence | Implemented in source | Definition, Assignment, mode-specific identity and identity/validation coverage exist | Unity import/compile and runtime certification |
| CAMERA-038-C — Outputs / Fallback | Implemented in source | Per-Output state, Fallback coverage and same-occurrence recovery have Editor coverage | Unity runtime certification across multiple Outputs |
| CAMERA-038-D — Zero-Player Session Camera | Targetless/fixed path implemented / validated; explicit Session/world target deferred | QA-NEW-004 current Session-scoped, Player-free continuity certification PASS 9/9 with `BaselineRestored`; FirstGame uses `NoSubject`; same Output/Assignment survives Route/Activity transitions | targetless cut closed; implement explicit world target only for a concrete consumer; broader IF-ADR-038 recertification remains separate |
| CAMERA-038-E — Membership / Subjects | Implemented in source | Current Player/Actor occurrence reconciliation and Subject updates have Editor coverage | Unity Join/Leave/replacement evidence |
| CAMERA-038-F — Individual per Player | Implemented / Integrated / Validated | Manual P1/P2 validation plus Framework EditMode 175/175 / Camera 78/78; IF-ADR-043 focused QAFramework 7/7 PASS proves `0 -> P1 -> P1+P2 -> P2 -> 0 -> P1 -> P1+P2`, exact `PlayerInput.camera`, stable Assignment/Output identity and zero frames without physical coverage | cut closed; broader IF-ADR-038 recertification remains separate |
| CAMERA-038-G — Shared group | Implemented / QA certified; Local Multiplayer consumer validation PASS 2026-10-06 | IF-ADR-042 dedicated QAFramework Play Mode 7/7 PASS on 2026-10-07 with `BaselineRestored`; consumer verified 1-player follow without orbit, 2-player framing, dolly/FOV on separation, and stable Subject under Actor rotation; Camera Editor 78/78 PASS (test suite scope remains distinct from QAFramework result) | focused SharedGroup gate closed; broader IF-ADR-038 recertification and transactional Assignment failure paths remain |
| CAMERA-038-H — Transactional Assignment change | Implemented in source | Candidate replacement, rollback and Output preservation have Editor coverage | Unity failure-path and multi-Output evidence |
| CAMERA-038-I — Remove Presentation / Request / Game Flow ownership | Package code removed | Old Presentation/Request symbols are absent from Runtime/Editor | Migrate and revalidate remaining serialized QA/consumer assets |
| CAMERA-038-J — Authoring / samples / assets | Partial | Getting Started, Local Multiplayer SharedGroup, Character Selection Fixed Follow and Character Selection Multiplayer Split Screen are migrated; the Character Selection samples and Local Multiplayer have manual Play Mode PASS | Migrate remaining consumer/QA assets and complete authoring validation |
| CAMERA-038-K — QA / regressions / documentation cleanup | Partial | Framework EditMode 175/175 and Camera Editor 78/78 PASS; QA-NEW-004 Play Mode 9/9 PASS; IF-ADR-042 SharedGroup 7/7 and IF-ADR-043 physical-participation 7/7 PASS; sample docs record current manual Play Mode evidence | focused SharedGroup and individual-participation QA gates closed; transactional Assignment failure paths and broader IF-ADR-038 recertification remain |

Current Framework EditMode execution is **175/175 PASS**, including Camera Editor
**78/78 PASS**. Manual Play Mode passed for Local Multiplayer SharedGroup,
Character Selection Fixed Follow, Character Selection Multiplayer Split Screen,
and IF-ADR-043 Output participation. QA-NEW-004 now provides current Play Mode
certification for the zero-Player Session-scoped Camera cut at **9/9 PASS** with
`BaselineRestored`. Focused SharedGroup QA is closed. Framework Editor tests cover invalid-candidate preservation/rollback, but Unity validation of transactional Assignment failure paths and broader
IF-ADR-038 recertification, remain pending. Historical Camera certifications stay dated evidence for their exact
former boundaries.

Previous IF-ADR-032 and IF-ADR-029/030 certifications remain historical evidence
for the exact former boundaries; they do not certify IF-ADR-038.

Historical records:

- [IF-ADR-029/030 Camera Composition and Framing Technical Certification — 2026-09-21](../Reconciliation/IF-ADR-029-030-CAMERA-COMPOSITION-FRAMING-TECHNICAL-CERTIFICATION-2026-09-21.md)
- [IF-ADR-030 Local Multiplayer Consumer Unity Proof — 2026-09-20](../Reconciliation/IF-ADR-030-LOCAL-MULTIPLAYER-CONSUMER-UNITY-PROOF-2026-09-20.md)
- [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)

## Current Stage B / FIRSTGAME priorities

1. **Player** — Scene Player physical/contextual lifecycle has historical proof through `GameplayReady`; Actor occurrence now owns physical/spatial state and replacement pose. Character Selection and Character Selection Multiplayer Split Screen migration/Play Mode are complete; broader Unity recertification and remaining sample migrations are pending.
2. **Loading / Readiness** — positive Game Flow consumer lane proven; negative/terminal robustness remains QA-owned.
3. **Camera** — IF-ADR-038 is Accepted. Assignment/Occurrence/Fallback runtime cuts A-H remain present; CAMERA-038-L derives `PlayerInput.camera` topology from Individual Assignments. Local Multiplayer SharedGroup, Character Selection Fixed Follow, and Character Selection Multiplayer Split Screen are migrated and have manual Play Mode PASS. IF-ADR-043 physical Output participation passed manual 0/1/2/Leave/Rejoin validation and current automated regressions. Framework EditMode is 175/175 PASS, Camera Editor 78/78 PASS, and QA-NEW-004 Session Camera continuity is 9/9 PASS. Focused SharedGroup QA is closed; transactional Assignment failure paths and broader IF-ADR-038 recertification remain pending.
4. **Pause** — runtime certified; remaining work is consumer authoring/usability only.
5. **Audio** — IF-ADR-040 Audio composition migration validated: EditMode 5/5 + QA-NEW-005 12/12 with `BaselineRestored`; historical 44/44 remains pre-migration evidence. Consumer integration is proven; API maturity promotion is separate.
6. **Progression Save** — real consumer persistence/usability proof remains.
7. **Editor/Product Surface** — continue feature-owned Inspector/workflow evidence under ADR-010.

## Future / deferred contracts

- exact-Slot public Join;
- public Slot/device/InputUser/control-scheme ownership observation;
- device disconnect/reconnect and reassignment semantics;
- heterogeneous per-Slot Host Provisioning;
- Scene-Provided prepared Actor replacement, pending an explicit physical-ownership contract;
- generic respawn/checkpoint/dynamic Spawn beyond ADR-021;
- application-scoped stable-ID resolver;
- Session-scoped frame-rate override;
- persisted frame-rate preference integration;
- advanced BGM simultaneous-source/crossfade semantics unless separately accepted.

`PLAYER-COMMAND-SURFACE-READINESS / DEFERRED` remains a product-availability concern: valid authored commands may be runtime-unbound and must reject without fallback until live scoped access exists.

## Current architecture / reconciliation records

- [IF-ADR-038 — Session Player Camera Assignments and Occurrence Lifecycle](../ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md)
- [IF-ADR-043 — Individual Player Camera Output Physical Participation](../ADRs/IF-ADR-043-Individual-Player-Camera-Output-Physical-Participation.md)
- [IF-ADR-044 — Consumer-Controlled Player Runtime Gameplay Availability](../ADRs/IF-ADR-044-Consumer-Controlled-Player-Runtime-Gameplay-Availability.md)
- [IF-ADR-045 — Persistent Content Scene Reference Must Survive Player Builds](../ADRs/IF-ADR-045-Persistent-Content-Scene-Reference-Player-Builds.md)
- [IF-ADR-041 — Route-Scoped Activity Transition Observation](../ADRs/IF-ADR-041-Route-Scoped-Activity-Transition-Observation.md)

- [IF-ADR-009 — Activity Local Visibility Rules](../ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md)
- [IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md)
- [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md)
- [IF-ADR-020 — Session Player Leave and Resource Release Authority](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md)
- [IF-ADR-021 — Route Spatial Entry and Activity Explicit Relocation](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md)
- [IF-ADR-029/030 Camera Composition and Framing Technical Certification — 2026-09-21](../Reconciliation/IF-ADR-029-030-CAMERA-COMPOSITION-FRAMING-TECHNICAL-CERTIFICATION-2026-09-21.md)
- [IF-ADR-030 Local Multiplayer Consumer Unity Proof — 2026-09-20](../Reconciliation/IF-ADR-030-LOCAL-MULTIPLAYER-CONSUMER-UNITY-PROOF-2026-09-20.md)
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

## IF-PLAYER-MATURITY-004 — Public contract maturity

Stable, bounded contracts: PlayerGameplayAvailabilityBlockTrigger, PlayerGameplayAvailabilityBlockToken, PlayerGameplayAvailabilityBlockResult, PlayerGameplayAvailabilityBlockStatus; ISessionCameraAssignmentCommandPort and ISessionCameraAssignmentCommandConsumer.

Still Experimental: IPlayerSessionScopedAccess as a whole, Manager-Provisioned/Session profiles and Actor profiles, SessionCameraAssignmentCommandTrigger, SessionCameraAssignmentAsset, CameraOutputDefinition, and ActorCameraSubjectAuthoring. Availability methods remain callable through the Experimental scoped-access interface. Stable Camera command contracts accept Experimental Assignment assets; their status does not extend to those dependencies.

Evidence boundary: IF-ADR-044 QA 8/8 (BaselineRestored) plus FirstGame manual P1/P2 ownership, teardown and Pause composition; IF-ADR-039 Framework Editor/GameFlow evidence for Activate/Replace/Clear, including invalid-candidate state preservation in Editor tests. These results cover recorded scenarios, not Transition composition for Availability or every Unity Assignment failure topology. SharedGroup IF-ADR-042 QA 7/7 is closed; broader IF-ADR-038 recertification remains open.

Historical QA/consumer runs in QAFramework and FirstGame resolved Framework through a local file: dependency and did not record an immutable Framework SHA. This does not invalidate results for tested behavior. Future certification records must include the exact resolved Framework revision.
