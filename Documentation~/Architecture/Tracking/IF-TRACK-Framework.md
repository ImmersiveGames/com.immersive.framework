# IF-TRACK — Immersive Framework

Status: **Active — Stage B baseline + proposed architecture expansion**  
Last updated: **2026-08-24**

## Authority and status model

This is the single mutable summary of current delivery state. Its authority is below accepted ADRs, governance records and current reconciliation/certification records, and above historical audits, completion summaries and plans.

```text
Accepted ADRs   -> normative architecture
Proposed ADRs   -> pending architecture; not implementation/certification authority
Governance      -> cross-cutting compatibility/product policy
Reconciliation  -> current technical alignment and certification
Tracker         -> current mutable delivery state
FIRSTGAME       -> Stage B real-consumer evidence
Archive         -> historical, non-authoritative
```

A proposed ADR or draft must not be reported as implemented/certified until package and evidence exist. Experimental API status is maturity governance; it is not by itself an unresolved technical defect.

## Current canonical package state

Current Framework package implementation reviewed for this tracker update:

```text
ImmersiveGames/com.immersive.framework
master
1c422f7f22ec5d17a25e7caea8108eb5b0c08a4c
Audio Fix
```

This includes the BGM-CONTINUITY-1 Framework runtime cut. It is later than the historical Stage A baseline and later than the Camera Default-output implementation merge.

An additional Editor-only startup-isolation cut was implemented and proven locally on
2026-08-20 after the canonical package state above. No published commit SHA is recorded
for that cut in this documentation update, so it is tracked as a scoped local
implementation/evidence record below and does not replace the canonical package SHA.

Historical Stage A package baseline remains:

```text
ImmersiveGames/com.immersive.framework
7a20ec748e4e5f5f3764bdc34ee249c1fe1c1da6
fix(authoring): enforce validation governance semantics
```

Historical Camera Default-output implementation merge:

```text
8591385d14b646b612b32defc7180e71f21a2beb
Merge branch 'camera/default-output-authority-cut'
```

Later scoped cuts do not rewrite what earlier Stage A or Camera QA runs tested.

Current closure records:

- [Stage A Canonical Package Baseline Closure](../Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)
- [IF-ADR-001A — Editor Play Mode Startup Isolation](../Reconciliation/IF-ADR-001A-Editor-Play-Mode-Startup-Isolation-2026-08-20.md)
- [IF-ADR-004D — Camera Default Output Presentation Authority](../Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)
- [IF-ADR-013 — BGM Continuity Technical Certification](../Reconciliation/IF-ADR-013-BGM-Continuity-Technical-Certification-2026-08-19.md)
- [Stage B — Game Flow Sample Consumer Evidence](../Reconciliation/IF-STAGE-B-GAMEFLOW-SAMPLE-EVIDENCE-2026-08-21.md)
- [IF-ADR-021 — Player Authority and Initial Placement Reconciliation](../Reconciliation/IF-ADR-021-Player-Authority-and-Initial-Placement-Reconciliation-2026-08-23.md)
- [Player Current Aggregate Recertification — 2026-08-24](../Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)

## Reconciliation sequence

```text
Stage A / technical cut
  accepted contract -> package -> technical QA -> reconciliation/certification

Stage B / consumer proof
  accepted package boundary -> FIRSTGAME/Sample -> real integration -> usability/product proof
```

A real consumer can expose product/UX debt, an integration gap, future scope, or a reproducible technical regression. Consumer evidence does not retroactively rewrite historical technical certification.

## Current ADR status

| ADR | Architecture / package | Technical QA | Stage B / current disposition |
|---|---|---|---|
| [001](../ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED; 001A Editor startup isolation implemented locally | Existing core certification preserved; 001A is scoped consumer/Play Mode regression evidence, not new broad QA certification | Both Editor startup policies proven: `FrameworkStartup` uses neutral bootstrap and prevents the reproduced EventSystem/listener contamination; `CurrentSceneOnly` executes the current scene with Framework boot explicitly skipped. |
| [002](../ADRs/IF-ADR-002-Product-Authoring-Model.md) | ACCEPTED / RECONCILED / IMPLEMENTED | Feature-owned evidence | Stage A closed; product proof remains feature-owned. |
| [003](../ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | ACCEPTED baseline / RECONCILED / IMPLEMENTED; R6/R7/R8 draft pending | CERTIFIED baseline; current Full Player aggregate 27/27 PASS | Existing Player proof remains valid; proposed deltas are not delivered baseline behavior. |
| [004](../ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED; 004D Default-output cut merged | Full Camera 53/53 CERTIFIED for 2026-08-15 boundary | Sample 00 Default-output + gameplay readiness proof PASS; broader Camera consumer proof remains separate. |
| [005](../ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | ACCEPTED / RECONCILED / IMPLEMENTED | Input Gate 9/9; Restart 8/8; Pause 27/27 CERTIFIED | Stage A closed; Stage B may test authoring/usability. |
| [006](../ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | ACCEPTED / RECONCILED / IMPLEMENTED | Focused 8/8; Progress 32/32; Terminal 34/34 CERTIFIED | **GAME FLOW CONSUMER PASS** — Game Flow Sample proves real persistent Transition/Loading authoring, Route Fade cover/reveal, Activity Fade/Seamless/FadeWithLoading selection and successful readiness-governed covered waiting. Negative/terminal robustness remains technical QA-owned and is not a FIRSTGAME/Sample completion gate. |
| [007](../ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | ACCEPTED / RECONCILED / IMPLEMENTED | Foundation 18/18; Direct Policies 42/42 CERTIFIED | **GAME FLOW CONSUMER PASS** — Game Flow Readiness Showcase proves valid `ObserveOnly`, `WaitVisible` and `WaitCovered` authoring, reveal behavior, capability blocking until Ready, release and fresh reentry. Failure/interruption/recovery semantics remain technical QA-owned. |
| [008](../ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) | ACCEPTED / RECONCILED / IMPLEMENTED | No generic default gate | Persistent content remains explicit Game Application composition; Game Flow Sample now adds real consumer evidence for explicitly composed optional Transition + Loading adapters. 001A prevents unrelated Editor-open scenes from becoming persistent-composition sources under `FrameworkStartup`. |
| [009](../ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED | Current boundary closed; Game Flow Sample adds positive A/B visibility plus content-less Activity C negative-isolation consumer evidence. |
| [010](../ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) | ACCEPTED / IMPLEMENTED; Editor Play Mode startup surface reconciled with 001A | Feature-owned | Project Settings now has a deterministic technical consequence: `FrameworkStartup` -> neutral bootstrap; `CurrentSceneOnly` -> current scene/no framework startup. |
| [011](../ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md) | ACCEPTED / RECONCILED / IMPLEMENTED | Progress 32/32; Terminal 34/34; Route 25/25; App 20/20 CERTIFIED | **CORE CONSUMER PROOF PASS** — Game Flow Readiness Showcase exercises determinate `ActivityReadiness` Loading progress with one Required participant, terminal Ready, content release and fresh reentry. Broader technical QA remains certification authority for the full matrix, including negative paths. |
| [012](../ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED; current Full Player aggregate 27/27 PASS | FIRSTGAME participation proof required. |
| [013](../ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | ACCEPTED / EXPERIMENTAL / IMPLEMENTED — IF-ADR-013A + BGM-CONTINUITY-1 + BGM-ROUTE-POLICY-1 | CERTIFIED: Audio 30/30 = Core 7/7 + Framework BGM 14/14 + ADR-013A 5/5 + physical continuity 4/4; real Framework Route A->B continuity PASS | **FIRSTGAME/SAMPLE CONSUMER GATE PASS** — Game Flow Sample proves Play, no-request Preserve, owner-exit preservation and explicit Silence across transient Route/Activity scenes. API remains Experimental pending an explicit product-maturity promotion cut. |
| [014](../ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | ACCEPTED / IMPLEMENTED | CERTIFIED | Current boundary closed and consumer-proven. |
| [015](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | ACCEPTED baseline / IMPLEMENTED; R6/R7/R8 draft pending | CERTIFIED baseline; current Full Player aggregate 27/27 PASS | Targeted Join / Actor Selection draft deltas are not current delivered baseline. |
| [016](../ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | ACCEPTED baseline / IMPLEMENTED; R6/R7/R8 draft pending | CERTIFIED baseline; current Full Player aggregate 27/27 PASS | Scene-/Manager-Provisioned baseline remains valid. |
| [017](../ADRs/IF-ADR-017-Application-Frame-Rate-Project-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | Edit 13/13; Target 13/13; VSync 13/13; Defaults 13/13 CERTIFIED | Stage A closed. |
| [018](../ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | ACCEPTED / RECONCILED; A Stable, B JSON certified, C composition implemented | backend conformance; JSON 18/18; composition 12/12 CERTIFIED | FIRSTGAME persistence/backend usability proof remains. |
| [019](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | Full Player current aggregate 27/27 PASS; historical 25/25 recertification preserved | Closed for Session physical Player lifetime and contextual representation authority. |
| [020](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | ADR020-H 26/26; Full Player current aggregate 27/27 PASS; historical 25/25 recertification preserved | Closed for Session Player Leave and resource release authority. |
| [021](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) | ACCEPTED / RECONCILED — Model B; Route Spatial Entry + Activity Explicit Relocation IMPLEMENTED / CURRENT QA VERIFIED | Route Spatial Entry 18/18 PASS; Activity Relocation 23/23 PASS; Full Player current aggregate 27/27 PASS; historical Initial Placement 9/9 and Full Player 25/25 preserved | Route baseline entry and optional Activity relocation are separate lifecycle-correct pipelines; replacement QA closed. |
| [022](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) | ACCEPTED / IMPLEMENTED | Presentation 14/14; Full Camera 53/53 for 2026-08-15 boundary | C1-C5 closed; broader FIRSTGAME C6 remains pending. |

## Current scoped closures

### Player current aggregate — IF-ADR-019 / 020 / 021 — 2026-08-24

Current technical Player certification:

```text
PLAYER CURRENT AGGREGATE COMPLETE
mandatoryContracts=27
executedContracts=27
passedContracts=27
```

Focused Model B evidence:

```text
Route Spatial Entry      18/18 PASS
Activity Relocation      23/23 PASS
```

The historical Full Player `25/25` and ADR-021 Initial Placement `9/9` remain dated evidence for their earlier boundaries; neither is relabeled as current Model B proof.

Bootstrap composition now preserves the required authority order:

```text
Player Session core
  -> Game Flow / Route lifecycle authority
  -> Player Actor Preparation lifecycle attachment
  -> Route startup
```

The QA Play Mode synchronization sweep aligned observation timing with canonical Host readiness. It did not alter runtime authority or Player semantics.

Certification record:

[Player Current Aggregate Recertification — 2026-08-24](../Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)

### Editor Play Mode startup isolation — IF-ADR-001A — 2026-08-20

Accepted lifecycle rule:

```text
Editor authoring never becomes runtime authority.
```

Scoped Editor realization:

```text
FrameworkStartup
  -> package-owned empty FrameworkPlayModeBootstrap
  -> FrameworkRuntimeHost
  -> Startup Route Primary Scene
  -> Persistent Content
  -> normal Route / Activity composition

CurrentSceneOnly
  -> no Framework Play Mode start-scene override
  -> current Editor scene executes intentionally
  -> Framework startup skipped
```

Regression cause:

```text
previous AfterSceneLoad bootstrap timing
  -> Editor-open scene Awake / OnEnable could run first
  -> side effects could occur
  -> objects could escape through DontDestroyOnLoad
  -> later Single scene load could not undo those effects
```

Observed `FrameworkStartup` evidence:

```text
SceneReleasing
  scene='FrameworkPlayModeBootstrap'
  reason='single-scene-replacement'

Startup Route Primary Scene
  scene='MinimalGame_Gameplay'
  alreadyLoaded='False'
  loadMode='Single'
  loaded='True'

Boot
  succeeded
  activityReadiness='Ready'
  blockingIssues='0'
```

The path was then rerun with the Editor scene that had previously reproduced duplicate
`EventSystem` / listener contamination. The neutral-bootstrap sequence remained intact
and the previous symptom was absent.

The explicit `CurrentSceneOnly` counter-mode was also exercised. Observed evidence:

```text
[INFO][Immersive.Framework][ImmersiveFrameworkBootstrap]
Boot skipped. editorPlayModeStartup='CurrentSceneOnly'
```

This confirms that the current Editor scene remains the intentional execution target in
that mode while Framework application startup is skipped.

Evidence classification:

```text
implementation                 local package cut
FrameworkStartup path          PROVEN in consumer Play Mode
CurrentSceneOnly path          PROVEN in consumer Play Mode
regression reproduction        CLOSED
dedicated automated QA         not added / not required for this cut
published package SHA          not recorded yet
```

Reconciliation record:

[IF-ADR-001A — Editor Play Mode Startup Isolation](../Reconciliation/IF-ADR-001A-Editor-Play-Mode-Startup-Isolation-2026-08-20.md)

### Camera Default output — IF-ADR-004D

Current accepted Camera output contract:

```text
CameraOutputSessionBinding
  explicit persistent Default Camera Rig

CameraOutputContext
  normal request arbitration only

CameraOutputSession
  force-default -> Default
  normal winner -> winner
  no winner -> Default

SessionCameraOverrideBinding
  optional real Session override only
```

Sample 00 proved explicit Default output + gameplay readiness. The earlier Camera 53/53 aggregate predates 004D and remains historical evidence for the boundary it executed.

### Game Flow Sample Stage B consumer evidence — updated 2026-08-22

Current consumer topology:

```text
Game Flow HUB
  Route_Hub
  Activity = None
  BGM = explicit Silence

Basic Flow
  Route_BasicFlow
  Startup Activity = A
  Activity A / B = Seamless
  Activity C = Fade + no ActivityContentProfile

Readiness Showcase
  Route_ReadinessShowcase
  Startup Activity = C neutral baseline
  D = Wait Visible + Fade With Loading
  E = Wait Covered + Fade With Loading
  D / E share ActivityContentReadiness
  one Required ActivityReadinessParticipant
```

Persistent Content explicitly composes:

```text
UnityFadeCurtainEffectAdapter
UnityLoadingSurfaceAdapter
```

Observed real-consumer Basic Flow proof:

```text
Persistent Content
  transitionAdapterCount=1
  loadingAdapterCount=1

HUB -> Basic Flow
  Route transition = SucceededWithUnitySurface
  effect = Fade
  Loading = SucceededWithUnitySurface
  Activity A = Ready
  blockingIssues=0

A <-> B
  Seamless
  Activity scene composition/release succeeds
  visual Transition/Loading skipped by authored policy

A/B -> C
  Fade = SucceededWithUnitySurface
  Loading skipped by Fade policy
  previous Activity scene released
  C scene composition = NotRequested
  C = Active + Ready
  A/B local content hidden
  blockingIssues=0
```

Observed Readiness Showcase proof:

```text
HUB -> Readiness Showcase
  Startup Activity = C
  C scene composition = NotRequested
  C = Ready
  blockingIssues=0

C -> D
  Wait Visible
  shared readiness scene loaded fresh
  target commits NotReady then settles Ready
  Fade With Loading presentation succeeds
  blockingIssues=0

D -> C
  SCN_GameFlow_Content_Readiness unloaded
  activityScenesReleased=1
  C = Ready

C -> E
  Wait Covered
  shared readiness scene loaded fresh
  target commits NotReady then settles Ready
  loadingProgressMode=Determinate
  loadingProgressPhase=ActivityReadiness
  Required completed=1 total=1 pending=0
  blockingIssues=0

D -> C -> D
E -> C -> E
  repeatable with fresh Activity content / readiness occurrence
```

The Readiness menu uses `ActivityContentBinding` only for sample presentation: C exposes D/E requests; D/E expose a return-to-C request. All transitions remain normal `ActivityRequestTrigger` / Game Flow requests.

BGM consumer proof in the Basic Flow topology:

```text
HUB Silence
  -> A Play(BGM_Floresta)
  -> B Play(BGM_Gelo)

A -> C
  No Request / owner exit -> Preserve BGM_Floresta

B -> C
  No Request / owner exit -> Preserve BGM_Gelo

Basic Flow -> HUB
  explicit Silence -> provider-confirmed release
```

Disposition:

```text
IF-ADR-006 Transition/Loading + readiness success path     GAME FLOW CONSUMER PASS
IF-ADR-007 Wait Visible / Wait Covered success path        GAME FLOW CONSUMER PASS
IF-ADR-008 optional persistent presentation composition    CONSUMER EVIDENCE ADDED
IF-ADR-009 positive/negative local visibility              CONSUMER EVIDENCE ADDED
IF-ADR-011 participant-aware readiness progress            CORE CONSUMER PROOF PASS
IF-ADR-013 real-consumer BGM integration gate              PASS
```

The intended positive Game Flow consumer lane is closed. Negative/invalid/interrupted/terminal readiness robustness remains QA-owned and is not a pending FIRSTGAME/Sample scenario.

Detailed record:

[IF Stage B — Game Flow Sample Consumer Evidence](../Reconciliation/IF-STAGE-B-GAMEFLOW-SAMPLE-EVIDENCE-2026-08-21.md)

### Audio BGM continuity — IF-ADR-013 / BGM-CONTINUITY-1 — 2026-08-19

Canonical contract:

```text
No Request  -> Preserve / NoChange
Play(cue)   -> explicit provider apply/transition
Silence     -> explicit provider release to silence
Owner exit  -> Preserve / NoChange
```

Architecture:

```text
Framework Persistent Content
└─ AudioRuntimeHost + FrameworkBgmDirector
        ↑ runtime injection
        │
Transient Route/Activity BGM bindings
```

Automated certification:

```text
Core Audio         7/7 PASS
Framework BGM     14/14 PASS
ADR-013A            5/5 PASS
Audio continuity    4/4 PASS
TOTAL              30/30 PASS
FAILED               0
```

Physical continuity proves same-cue no restart, controlled different-cue transition, completed new-cue playback, and explicit fade-to-silence.

Real Framework lifecycle proof:

```text
QA Hub
  -> QA_Audio / Route A
  -> Own Activity BGM
  -> Route A exit / scene unload
  -> QA_AudioRouteB / Route B
  -> Startup Activity = Retain Previous / NoRequest
  -> Activity Ready / blockingIssues=0
  -> BGM remained playing
```

Setup was run twice with the same persistent authority/defaults/no-request topology.

Stage B Game Flow Sample then proved the supported consumer intent path with explicit Play on A/B, no-request preservation through content-less Activity C, owner-exit preservation across Activity scene release, and explicit Route Silence on return to HUB.

Disposition:

```text
technical BGM continuity     CLOSED / CERTIFIED
FIRSTGAME / Sample gate      PROVEN
ADR-013 maturity             EXPERIMENTAL pending explicit promotion cut
```

Route and Startup Activity BGM intent ordering is now runtime-resolved: the Activity lifecycle closes the pending Route intent only after normal Activity BGM publication. No Route authoring reference or synthetic Activity binding is required.

Certification record:

[IF-ADR-013 — BGM Continuity Technical Certification](../Reconciliation/IF-ADR-013-BGM-Continuity-Technical-Certification-2026-08-19.md)

Consumer evidence record:

[IF Stage B — Game Flow Sample Consumer Evidence](../Reconciliation/IF-STAGE-B-GAMEFLOW-SAMPLE-EVIDENCE-2026-08-21.md)

## Stage A summary

For currently accepted/certified technical boundaries:

```text
Package implementation:   approved historical baseline + later accepted scoped cuts
Technical reconciliation: closed for recorded accepted boundaries
Reverse audit:            closed
Generic Stage A task:     none
```

The historical Stage A closure does not claim implementation/certification of later
accepted cuts. IF-ADR-021 Model B is now separately implemented and certified by the
2026-08-24 Player Current Aggregate Recertification. ADR-019 and ADR-020 retain their
historical 2026-08-15 recertification and are also covered by the current 27/27
aggregate.

A new accepted contract or reproducible regression may open a new scoped technical cut without invalidating unrelated historical certification.

IF-ADR-001A is such a scoped regression correction. Its consumer Play Mode evidence
does not rewrite prior ADR-001 certification and does not claim a new broad automated
certification.

## Active work — Stage B / FIRSTGAME

Stage B is the real-consumer lane for accepted package boundaries.

1. **Player** — current technical Player boundary is certified by the `27/27` aggregate, including Scene-/Manager-Provisioned flows, Actor Lifecycle, public surface, Leave and IF-ADR-021 Model B spatial semantics. Remaining Player work in Stage B is consumer/sample integration and usability proof; proposed future deltas remain separate until accepted.
2. **Loading / Readiness** — **Game Flow consumer proof CLOSED** for valid product paths: Route Fade + Loading, Activity `FadeWithLoading`, `ObserveOnly`, `WaitVisible`, `WaitCovered`, one-Required participant-aware determinate progress, content release and fresh reentry are proven. Negative/invalid/interrupted/terminal failure semantics remain technical QA/certification and do not block FIRSTGAME.
3. **Camera** — 004D Default-output integration is proven in Sample 00; broader ADR-022 C6 remains pending.
4. **Pause** — consumer authoring/usability only; runtime contract is certified.
5. **Audio** — BGM-CONTINUITY-1 technical runtime/QA and Game Flow Sample real-consumer integration are closed. Remaining ADR-013 work, if desired, is an explicit API maturity promotion decision rather than another FIRSTGAME proof gate.
6. **Progression Save** — real Built-in JSON and Custom Provider usability/persistence proof.
7. **Editor/Product Surface** — IF-ADR-001A is closed for both startup policies: `FrameworkStartup` isolation and `CurrentSceneOnly` boot-skip behavior are proven. Continue feature-owned Inspector/discovery/workflow evidence.

## Reopen conditions for a closed technical boundary

Reopen only for at least one of:

- reproducible regression against an accepted contract;
- documented contradiction between package behavior and normative docs;
- accepted contract/architecture change;
- newly accepted scope extending the boundary.

Do not reopen solely because a consumer exposes weak UX, because an API remains Experimental, or because a proposed ADR has not yet been accepted.

## Future / deferred contracts

These remain outside the current accepted baseline unless separately accepted:

- device disconnect/reconnect, reassignment and network reconnection semantics;
- heterogeneous per-Slot Host Provisioning;
- consumer-facing physical Actor hot-swap/replacement while represented;
- generic respawn/checkpoint/dynamic Spawn beyond the accepted ADR-021 spatial contract;
- Orbital / Free Look input ownership and additional camera product models;
- split-screen / multiple Camera outputs;
- exceptional post-commit compensation beyond accepted boundaries;
- application-scoped stable-ID resolver;
- Session-scoped Frame Rate override;
- persisted Frame Rate preference integration after relevant persistence/preferences architecture;
- simultaneous dual-source BGM crossfade and AudioMixer binding unless separately accepted.

ADR-022 presentation models are not future scope; Fixed/Follow/Mounted/Third Person are accepted/implemented. Multi-output/additional families remain future.

## Current architecture / governance records

- [IF-GOV-001 — API Maturity and Validation Governance](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
- [IF-ADR-013 — Optional Audio BGM Adapter](../ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) — Accepted / Experimental / technically certified / consumer gate proven
- [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) — Accepted / reconciled / implemented / current aggregate PASS
- [IF-ADR-020 — Session Player Leave and Resource Release Authority](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) — Accepted / reconciled / implemented / current aggregate PASS
- [IF-ADR-021 — Route Spatial Entry and Activity Explicit Relocation](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) — Accepted / reconciled / implemented / current QA verified
- [IF-ADR-022 — Camera Rig Presentation Models and Materialization Authority](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) — Accepted / Implemented / Technical QA Certified

## Current reconciliation records

- [ADR-001](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-001-RECONCILIATION-2026-08-10.md)
- [ADR-001A — Editor Play Mode Startup Isolation](../Reconciliation/IF-ADR-001A-Editor-Play-Mode-Startup-Isolation-2026-08-20.md)
- [ADR-002](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-002-RECONCILIATION-2026-08-10.md)
- [ADR-002 and ADR-009](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-002-009-RECONCILIATION-2026-08-10.md)
- [ADR-003 and ADR-012](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-003-012-RECONCILIATION-2026-08-10.md)
- [ADR-004 Camera — 004A](../Reconciliation/IF-ADR-004A-Camera-Authority-Normative-Reconciliation-2026-08-10.md)
- [ADR-004 Camera — 004D Default Output](../Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)
- [ADR-005](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-005-RECONCILIATION-2026-08-10.md)
- [ADR-006](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-006-RECONCILIATION-2026-08-10.md)
- [ADR-007](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-007-RECONCILIATION-2026-08-11.md)
- [ADR-008](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-008-RECONCILIATION-2026-08-10.md)
- [ADR-011](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-011-RECONCILIATION-2026-08-11.md)
- [ADR-013 BGM Continuity Technical Certification](../Reconciliation/IF-ADR-013-BGM-Continuity-Technical-Certification-2026-08-19.md)
- [Stage B — Game Flow Sample Consumer Evidence](../Reconciliation/IF-STAGE-B-GAMEFLOW-SAMPLE-EVIDENCE-2026-08-21.md)
- [ADR-017](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-017-RECONCILIATION-2026-08-11.md)
- [ADR-018](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-RECONCILIATION-2026-08-11.md)
- [ADR-018-A Certification](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-A-CERTIFICATION-2026-08-11.md)
- [ADR-018-C Certification](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-C-CERTIFICATION-2026-08-11.md)
- [Player Current Aggregate Recertification](../Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)
- [RA-03 — Object Entry Ownership Reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-RA-03-OBJECT-ENTRY-OWNERSHIP-RECONCILIATION-2026-08-11.md)
- [RA-04 — Architecture Governance Hygiene](../Reconciliation/IMMERSIVE-FRAMEWORK-RA-04-ARCHITECTURE-GOVERNANCE-HYGIENE-2026-08-11.md)
- [Stage A Canonical Package Baseline Closure](../Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

## Documentation maintenance

- Accepted ADRs remain normative.
- Proposed ADRs remain explicitly Proposed until acceptance.
- Governance records hold cross-cutting compatibility/product policy.
- Reconciliation records hold technical alignment/certification evidence.
- This tracker holds current mutable delivery state; detailed execution history belongs in reconciliation/archive records.
- FIRSTGAME evidence is Stage B product/consumer evidence, not rewritten historical Stage A evidence.
- Dated QA records must never be rewritten to imply coverage of later package changes.
