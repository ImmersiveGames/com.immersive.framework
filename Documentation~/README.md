# Immersive Framework Documentation

Last updated: **2026-08-23**

## Start here

- [Stage A canonical package baseline](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)
- [Current framework tracker](Architecture/Tracking/IF-TRACK-Framework.md)
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

Full Player QA
  PLAYER QA CERTIFIED
  25/25

ADR-019
  see current Tracker / ADR authority

ADR-020
  see current Tracker / ADR authority

ADR-021
  see current Tracker / ADR authority

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
  Player real-game validation when scheduled
  ADR-013 Audio FIRSTGAME promotion
  ADR-022 broader Camera C6 promotion
```

The `53/53` Camera aggregate predates IF-ADR-004D. It remains valid historical
certification for the boundary it executed, but it is not relabeled as proof of the later
Default-output or force-default implementation.

Sample 00 provides real-consumer evidence for the 004D Default authoring path:

```text
CameraOutputSessionBinding
  Initialized
  defaultRig = Session Camera Rig

Activity
  Ready
  blockingIssues = 0

MinimalFirstPersonLocomotion
  READY
  gameplayReady = true
  Move / Look consumed
```

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
| [003](Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | Player participation and Actor lifecycle | Accepted / Reconciled |
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
| [015](Architecture/ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | Player provisioning commands and consumer observation surface | Accepted / Reconciled |
| [016](Architecture/ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | Player session initial configuration and provisioning profiles | Accepted / Reconciled |
| [017](Architecture/ADRs/IF-ADR-017-Application-Frame-Rate-Project-Authority.md) | Application frame-rate project authority | Accepted |
| [018](Architecture/ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | Progression Save backend independence and persistence boundaries | Accepted |
| [019](Architecture/ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) | Session Player lifetime and Activity representation authority | Accepted / Reconciled / Implemented / QA Recertified |
| [020](Architecture/ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) | Session Player Leave and resource release authority | Accepted / Reconciled / Implemented / QA Recertified |
| [021](Architecture/ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) | Route spatial entry and Activity explicit relocation | Accepted / Reconciled — Route Spatial Entry + Activity Explicit Relocation implemented; QA pending |
| [022](Architecture/ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) | Camera Rig presentation models and materialization authority | Accepted / Technical QA Certified |

## Current reconciliation / closure records

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
- [ADR-021 Player authority and Initial Placement reconciliation](Architecture/Reconciliation/IF-ADR-021-Player-Authority-and-Initial-Placement-Reconciliation-2026-08-23.md)
- [RA-03 Object Entry ownership](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-RA-03-OBJECT-ENTRY-OWNERSHIP-RECONCILIATION-2026-08-11.md)
- [RA-04 Architecture Governance Hygiene](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-RA-04-ARCHITECTURE-GOVERNANCE-HYGIENE-2026-08-11.md)
- [Stage A Canonical Package Baseline Closure](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

## Current certification scopes

### Player

The current Player boundary is certified as one terminal matrix rather than
disconnected local fixes.

```text
PLAYER QA CERTIFIED
25/25
```

### Camera

The 2026-08-15 Camera aggregate combines presentation materialization and
non-regression of the request/output authority that existed at that time.

```text
CAMERA QA CERTIFIED
53/53
```

The aggregate proves:

```text
Presentation Models
safe materialization ownership
switching / idempotence
external conflict protection
no output-authority mutation under the tested boundary
canonical request lifecycle
negative transactional integrity
owner lifetime integrity
```

IF-ADR-004D is a later accepted implementation/reconciliation cut. Its current evidence
is package merge + Sample 00 consumer proof, not a rewritten claim about the earlier
`53/53` run.
