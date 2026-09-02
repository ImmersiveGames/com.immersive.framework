# Immersive Framework Documentation

Last updated: **2026-09-02**

## Start here

- [Stage A canonical package baseline](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)
- [Current framework tracker](Architecture/Tracking/IF-TRACK-Framework.md)
- [Player Actor runtime and presentation authority — IF-ADR-023](Architecture/ADRs/IF-ADR-023-Player-Actor-Runtime-Host-and-Presentation-Authority.md)
- [Prepared Actor replacement public contract — IF-ADR-024](Architecture/ADRs/IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md)
- [Player Actor occurrence identity boundary — IF-ADR-023A — 2026-08-31](Architecture/Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)
- [Player Actor runtime technical certification — 2026-08-29](Architecture/Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)
- [Player Actor Selection public surface certification — 2026-08-26](Architecture/Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)
- [Player current aggregate recertification — 2026-08-24](Architecture/Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)
- [Player physical lifetime recertification — 2026-08-15](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)
- [Camera presentation technical certification — 2026-08-15](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-CAMERA-PRESENTATION-TECHNICAL-CERTIFICATION-2026-08-15.md)
- [Camera Default output presentation authority — 2026-08-17](Architecture/Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)
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

The historical Stage A package baseline remains the approved historical baseline
for already-closed Stage A boundaries.

Subsequent accepted architecture cuts extend that product without pretending the
historical Stage A commit tested later contracts.

```text
Historical Stage A package baseline
  ImmersiveGames/com.immersive.framework
  7a20ec748e4e5f5f3764bdc34ee249c1fe1c1da6

Current Camera Default-output implementation merge reviewed
  ImmersiveGames/com.immersive.framework
  master
  8591385d14b646b612b32defc7180e71f21a2beb
  Merge branch 'camera/default-output-authority-cut'

Reverse audit
  RA-01 through RA-04 CLOSED

Player Physical Lifetime Reconciliation
  CLOSED / RECERTIFIED 2026-08-15

Player Current Aggregate
  PLAYER CURRENT AGGREGATE COMPLETE
  27/27

Player Actor Selection public surface
  CLOSED / IMPLEMENTED / INTEGRATED QA CERTIFIED 2026-08-26
  Observer + 8 explicit command components
  Actor Lifecycle PASS
  Public Surface PASS
  Full Player 27/27 PASS
  Character Selection public-surface blocker CLOSED

Player Actor runtime and presentation
  ACCEPTED / Scene-Provided authored-composition implementation complete
  PlayerActorRuntimeHost + ActorProfile.PresentationPrefab
  authoring validation, transient resolution and runtime adoption implemented

Player Actor occurrence identity boundary
  RECONCILED / FIRSTGAME PLAY MODE PROVEN 2026-08-31
  PlayerActorDeclaration template ActorId = empty
  runtime occurrence ActorId established by physical preparation
  Scene-Provided LogicalActorsPrepared = READY / PASS
  Scene-Provided GameplayReady = READY / PASS

Historical Full Player QA
  PLAYER QA CERTIFIED
  25/25
  preserved for the 2026-08-15 boundary

ADR-019
  current aggregate PASS; see current Tracker / ADR authority

ADR-020
  current aggregate PASS; see current Tracker / ADR authority

ADR-021
  Model B IMPLEMENTED / CURRENT QA VERIFIED
  Route Spatial Entry 18/18 PASS
  Activity Relocation 23/23 PASS
  Full Player current aggregate 27/27 PASS
  historical Initial Placement 9/9 preserved for superseded boundary

ADR-004
  Accepted / Reconciled / Implemented
  IF-ADR-004D Default-output cut merged 2026-08-17
  Sample 00 Default-output consumer proof PASS

ADR-010
  Accepted / Camera product surface includes explicit required Default Camera Rig

ADR-022
  Accepted / Implemented / Technical QA Certified
  C1-C5 CLOSED
  broader FIRSTGAME C6 PENDING

Full Camera QA — historical 2026-08-15 boundary
  CAMERA QA CERTIFIED
  mandatoryCases = 53
  executedCases = 53
  passedCases = 53

Post-004D Camera QA
  new focused/aggregate run NOT RECORDED

Active consumer phase
  Getting Started Scene Player framework lifecycle is proven through GameplayReady
  remaining Getting Started work = game-owned Presentation/gameplay completeness
  Player Provisioning and Character Selection are proven
  Local Multiplayer remains blocked by public Slot/device/input contract
  ADR-022 broader Camera C6 promotion
```

The `53/53` Camera aggregate predates IF-ADR-004D. It remains valid historical
certification for the boundary it executed, but it is not relabeled as proof of the later
Default-output or force-default implementation.

The public arbitrary Actor-selection surface is now delivered through explicit Player
Session commands. Exact-Slot public Join and public Slot/device/InputUser/control-scheme
ownership observation are not implied by that closure. Manager-Provisioned prepared-
Actor replacement is accepted but unimplemented under IF-ADR-024; Scene-Provided
replacement remains outside its V1 scope.

`PlayerActorDeclaration.ActorId` is a runtime physical occurrence identity. Reusable Player Actor templates keep the authored occurrence ID empty; the physical preparation owner establishes the typed runtime identity before downstream consumers may require it. This post-certification boundary is documented by IF-ADR-023A.

`GameplayReady` proves the current contextual gameplay projection over retained prepared Session Players. It does not by itself certify game-owned locomotion, camera composition, concrete gameplay input consumers or Presentation completeness.

The canonical mutable status is recorded in the
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

ADRs decide.

Governance records define cross-cutting policy without creating feature
authority.

Reconciliation records describe current alignment and certification.

The Tracker summarizes mutable delivery state.

Archive records preserve history without acting as current product truth.

## Canonical decisions

| ADR | Title | Status |
|---|---|---|
| [001](Architecture/ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | Core lifecycle and runtime authority | Accepted |
| [002](Architecture/ADRs/IF-ADR-002-Product-Authoring-Model.md) | Product authoring model | Accepted |
| [003](Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | Player participation and Actor lifecycle | Accepted / Reconciled / Current QA PASS |
| [004](Architecture/ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | Camera requests and output authority | Accepted / Reconciled / 004D implemented |
| [005](Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | Input, Pause, Gate and Reset | Accepted |
| [006](Architecture/ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | Loading, transition, persistence and diagnostics | Accepted |
| [007](Architecture/ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | Activity entry readiness and reveal gating | Accepted / Reconciled |
| [008](Architecture/ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) | Persistent application content composition | Accepted |
| [009](Architecture/ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md) | Activity-local visibility rules | Accepted |
| [010](Architecture/ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) | Editor and Inspector product surface authority | Accepted / Camera Reconciled |
| [011](Architecture/ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md) | Participant-aware readiness/loading progress | Accepted |
| [012](Architecture/ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md) | Activity Player participation profile and readiness compatibility | Accepted / Reconciled |
| [013](Architecture/ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | Optional Audio/BGM adapter | Accepted / Experimental |
| [014](Architecture/ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | Authored definition and stable identity authority | Accepted |
| [015](Architecture/ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | Player provisioning commands and consumer observation surface | Accepted / Reconciled / Actor Selection delivered |
| [016](Architecture/ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | Player session initial configuration and provisioning profiles | Accepted / Reconciled / Current QA PASS |
| [017](Architecture/ADRs/IF-ADR-017-Application-Frame-Rate-Project-Authority.md) | Application frame-rate project authority | Accepted |
| [018](Architecture/ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | Progression Save backend independence and persistence boundaries | Accepted |
| [019](Architecture/ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) | Session Player lifetime and Activity representation authority | Accepted / Reconciled / Implemented / Current Aggregate PASS |
| [020](Architecture/ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) | Session Player Leave and resource release authority | Accepted / Reconciled / Implemented / Current Aggregate PASS |
| [021](Architecture/ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) | Route spatial entry and Activity explicit relocation | Accepted / Reconciled / Implemented / Current QA Verified |
| [022](Architecture/ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) | Camera Rig presentation models and materialization authority | Accepted / Technical QA Certified |
| [023](Architecture/ADRs/IF-ADR-023-Player-Actor-Runtime-Host-and-Presentation-Authority.md) | Player Actor runtime Host and presentation authority | Accepted / Scene-Provided authored-composition implementation complete / 023A identity boundary reconciled |

## Current reconciliation / closure records

- [Player Actor Occurrence Identity Boundary — 2026-08-31](Architecture/Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)
- [Player Actor Runtime Technical Certification — 2026-08-29](Architecture/Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)
- [Player Actor Selection Public Surface Certification — 2026-08-26](Architecture/Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)
- [Player Current Aggregate Recertification — 2026-08-24](Architecture/Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)
- [Player Physical Lifetime Recertification — 2026-08-15](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)
- [Camera Presentation Technical Certification — 2026-08-15](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-CAMERA-PRESENTATION-TECHNICAL-CERTIFICATION-2026-08-15.md)
- [Camera Default Output Presentation Authority — 2026-08-17](Architecture/Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)
- [Player Physical Lifetime Reopen — 2026-08-14](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)
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
- [ADR-019 historical reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md)
- [ADR-020 historical reconciliation](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md)
- [ADR-021 pre-certification reconciliation — 2026-08-23 (historical)](Architecture/Reconciliation/IF-ADR-021-Player-Authority-and-Initial-Placement-Reconciliation-2026-08-23.md)
- [RA-03 Object Entry ownership](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-RA-03-OBJECT-ENTRY-OWNERSHIP-RECONCILIATION-2026-08-11.md)
- [RA-04 Architecture Governance Hygiene](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-RA-04-ARCHITECTURE-GOVERNANCE-HYGIENE-2026-08-11.md)
- [Stage A Canonical Package Baseline Closure](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

## Current certification scopes

### Player

The current Player boundary is certified as one terminal matrix rather than disconnected local fixes.

```text
PLAYER CURRENT AGGREGATE COMPLETE
mandatoryContracts=27
executedContracts=27
passedContracts=27
```

The 2026-08-26 rerun additionally closes the delivered arbitrary Actor-selection public surface:

```text
Actor Lifecycle = PASS
Public Surface  = PASS
```

Focused IF-ADR-021 Model B evidence:

```text
Route Spatial Entry      18/18 PASS
Activity Relocation      23/23 PASS
```

Post-certification Scene-Provided occurrence-identity evidence:

```text
LogicalActorsPrepared = READY / PASS
GameplayReady         = READY / PASS
projected             = 1
selected              = 1
prepared              = 1
failed                = 0
```

Historical evidence remains preserved without being relabeled:

```text
Full Player 2026-08-15       25/25
ADR-021 Initial Placement     9/9
```

Package-local Actor-selection Unity Test Framework Editor tests are not claimed as executed by this integrated QA result unless separately recorded.

### Camera

The 2026-08-15 Camera aggregate combines presentation materialization and
non-regression of the request/output authority that existed at that time.

```text
CAMERA QA CERTIFIED
53/53
```

IF-ADR-004D is a later accepted implementation/reconciliation cut. Its current evidence
is package merge + Sample 00 consumer proof, not a rewritten claim about the earlier
`53/53` run.
