# Immersive Framework Documentation

Last updated: **2026-09-12**

This directory contains the current product documentation for Immersive Framework.

## Authority model

```text
Architecture/ADRs/
  -> normative architecture decisions

Architecture/Governance/
  -> cross-cutting compatibility / product policy

Architecture/Reconciliation/
  -> dated technical reconciliation and certification evidence

Architecture/Tracking/
  -> current mutable delivery state

Guides/
  -> current product usage and authoring guidance

Architecture/Archive/
  -> historical/non-authoritative execution history
```

ADRs decide architecture. Reconciliation records preserve what was actually implemented or tested at a point in time. The Tracker is the canonical mutable status summary and must not be replaced by stale duplicated status text in this README.

## Start here

### Current status

- [Current Framework Tracker](Architecture/Tracking/IF-TRACK-Framework.md)
- [Architecture documentation map](Architecture/README.md)
- [API maturity and validation governance](Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

### Camera — current normative baseline

- [IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology](Architecture/ADRs/IF-ADR-026-Camera-Subjects-Assignment-and-Multi-Output-Topology.md)
- [IF-ADR-027 — Camera Authoring Definitions and Composition Authority](Architecture/ADRs/IF-ADR-027-Camera-Authoring-Definitions-and-Composition-Authority.md)
- [IF-ADR-028 — Camera Output Participation and Presentation Layout Authority](Architecture/ADRs/IF-ADR-028-Camera-Output-Participation-and-Presentation-Layout-Authority.md)
- [Camera Output Participation and Layout Authority Reconciliation — 2026-09-12](Architecture/Reconciliation/IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)
- [Camera Usage](Guides/Camera-Usage.md)

Historical Camera evidence:

- [Camera Full Technical Certification — 2026-09-12](Architecture/Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)
- [IF-ADR-026 Shared Camera Technical Certification — 2026-09-09](Architecture/Reconciliation/IF-ADR-026-SHARED-CAMERA-TECHNICAL-CERTIFICATION-2026-09-09.md)
- [Camera Presentation Technical Certification — 2026-08-15](Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-CAMERA-PRESENTATION-TECHNICAL-CERTIFICATION-2026-08-15.md)
- [Camera Default Output Presentation Authority — 2026-08-17](Architecture/Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)

### Player — current normative baseline

- [IF-ADR-023 — Player Actor Runtime Host and Presentation Authority](Architecture/ADRs/IF-ADR-023-Player-Actor-Runtime-Host-and-Presentation-Authority.md)
- [IF-ADR-023A — Player Actor Occurrence Identity Boundary — 2026-08-31](Architecture/Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)
- [IF-ADR-024 — Prepared Actor Replacement Public Contract](Architecture/ADRs/IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md)
- [IF-ADR-024 — Prepared Actor Replacement Technical Certification — 2026-09-02](Architecture/Reconciliation/IF-ADR-024-PREPARED-ACTOR-REPLACEMENT-TECHNICAL-CERTIFICATION-2026-09-02.md)
- [IF-ADR-015B — Player Actor Selection Public Surface Certification — 2026-08-26](Architecture/Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)
- [Player Current Aggregate Recertification — 2026-08-24](Architecture/Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)
- [Player Usage](Guides/Player-Usage.md)

### Other current guides

- [Framework Usage](Guides/Framework-Usage.md)
- [Editor Authoring Standard](Guides/Editor-Authoring-Standard.md)
- [Activity Readiness](Guides/Activity-Readiness.md)
- [Pause Usage](Guides/Pause-Usage.md)
- [Reset Usage](Guides/Reset-Usage.md)
- [Persistent Content Scene Template](Guides/Persistent-Content-Scene-Template.md)
- [Audio Usage](Guides/Audio-Usage.md)
- [Logging Usage](Guides/Logging-Usage.md)
- [Scene Lifecycle Events](Guides/Scene-Lifecycle-Events.md)
- [Game Flow — Player-Independent Navigation](Guides/Game-Flow-Player-Independent-Navigation.md)
- [Application Frame Rate](Guides/Application-Frame-Rate-Usage.md)

## Current program state

Detailed mutable status lives in the [Framework Tracker](Architecture/Tracking/IF-TRACK-Framework.md). The summary below only records the major current boundaries.

### Player

Current Player architecture includes:

```text
Player Actor Runtime Host / Presentation authority
runtime Player Actor occurrence identity established by physical preparation
explicit Player Session observation + command surface
Manager-Provisioned prepared Actor replacement V1
Route Spatial Entry + Activity explicit relocation
```

Current important evidence includes:

```text
Player current aggregate                  27/27 PASS
ADR-024 Full Player QA                    16/16 PASS
Pause/Input/Gate                           8/8 PASS
Route Spatial Entry                       18/18 PASS
Activity Relocation                       23/23 PASS
Scene-Provided occurrence/readiness       FIRSTGAME Play Mode PASS
```

Scene-Provided prepared physical Actor replacement remains outside the current ADR-024 V1 boundary.

### Camera

The Camera architecture was reconciled again on **2026-09-12** after the first multi-output/authoring implementation exposed an ownership problem.

Current normative separation:

```text
Camera Subject
  -> observable evidence

Camera Assignment
  -> Subjects assigned to a logical View

Camera Rig / Presentation
  -> how resolved Subjects are observed

Camera Output
  -> explicit physical Camera capacity

View→Output association
  -> logical View identity + Output identity

Output Presentation / Layout
  -> separate viewport / display / RenderTexture / PiP authority
```

Current cardinality rule:

```text
Session available Outputs       -> 1..N
current View→Output associations -> 0..N subset
```

An available Output does not have to participate in the current View topology. Player count does not define Output count, active association count or screen layout.

The previous viewport-bearing implementation remains in code until the pending implementation cuts are completed. It is no longer normative architecture.

Current implementation plan:

```text
CAMERA-026-H2  IMPLEMENTED / TECHNICALLY CERTIFIED — partial Output participation
CAMERA-026-I   Player→Camera Subject integration boundary
CAMERA-027-D2  logical View→Output authoring without viewport
CAMERA-028-A   IMPLEMENTED / TECHNICALLY CERTIFIED — available Output vs active participation
CAMERA-028-B   remove viewport from Camera topology
CAMERA-028-C   explicit Output Presentation / Layout authority
CAMERA-028-D   PlayerInputManager layout integration
```

The 2026-09-12 Full Camera QA result remains valid historical evidence:

```text
39/39 PASS
ADR-026 phases 2/2
9/9 dimensions
```

It certified the previous viewport-bearing contract. It is **not** current certification of the corrected IF-ADR-026/027/028 boundary. Corrected implementation and recertification are pending.

Request arbitration, output-owned Default Rig semantics, force-default ownership, typed View/Output/Rig Behavior definitions and `CameraRigComposer` materialization authority remain preserved.

### Activity content / visibility

Activity content Contribution and presentation Visibility remain separate authorities. Current post-split evidence is:

```text
Contribution Authority     3/3 PASS
Visibility Isolation       2/2 PASS
Lifecycle regression      16/16 PASS
```

See the Tracker and IF-ADR-009 reconciliation records for the current boundary.

## Canonical decisions

| ADR | Title | Current disposition |
|---|---|---|
| [001](Architecture/ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | Core lifecycle and runtime authority | Accepted / Reconciled / Implemented |
| [002](Architecture/ADRs/IF-ADR-002-Product-Authoring-Model.md) | Product authoring model | Accepted / Reconciled / Implemented |
| [003](Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | Player participation and Actor lifecycle | Accepted / Reconciled / Implemented |
| [004](Architecture/ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | Camera requests and output authority | Request/Default core preserved; evolved topology/layout governed by 026/028 |
| [005](Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | Input, Pause, Gate and Reset | Accepted / Reconciled / Implemented |
| [006](Architecture/ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | Loading, transition, persistence and diagnostics | Accepted / Reconciled / Implemented |
| [007](Architecture/ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | Activity entry readiness and reveal gating | Accepted / Reconciled |
| [008](Architecture/ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) | Persistent application content composition | Accepted / Implemented |
| [009](Architecture/ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md) | Activity-local visibility rules | Accepted / Reconciled / Implemented / Current QA certified |
| [010](Architecture/ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) | Editor and Inspector product surface authority | Accepted; Camera surface reconciliation pending 027-D2/028 |
| [011](Architecture/ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md) | Participant-aware readiness/loading progress | Accepted / Reconciled |
| [012](Architecture/ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md) | Activity Player participation profile and readiness compatibility | Accepted / Reconciled / Implemented |
| [013](Architecture/ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | Optional Audio/BGM adapter | Accepted / Experimental |
| [014](Architecture/ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | Authored definition and stable identity authority | Accepted / Implemented |
| [015](Architecture/ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | Player provisioning commands and consumer observation surface | Accepted / Reconciled / Implemented |
| [016](Architecture/ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | Player Session initial configuration and provisioning profiles | Accepted / Implemented |
| [017](Architecture/ADRs/IF-ADR-017-Application-Frame-Rate-Project-Authority.md) | Application frame-rate project authority | Accepted / Reconciled / Implemented |
| [018](Architecture/ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | Progression Save backend independence and persistence boundaries | Accepted / Reconciled / Implemented |
| [019](Architecture/ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) | Session Player lifetime and Activity representation authority | Accepted / Reconciled / Implemented |
| [020](Architecture/ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) | Session Player Leave and resource release authority | Accepted / Reconciled / Implemented |
| [021](Architecture/ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) | Route Spatial Entry and Activity explicit relocation | Accepted / Reconciled / Implemented |
| [022](Architecture/ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) | Camera Rig presentation models and materialization authority | Accepted / Implemented; preserved by 026/027/028 |
| [023](Architecture/ADRs/IF-ADR-023-Player-Actor-Runtime-Host-and-Presentation-Authority.md) | Player Actor Runtime Host and Presentation authority | Accepted / Implemented; occurrence identity reconciled by 023A |
| [024](Architecture/ADRs/IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md) | Prepared Actor replacement public contract | Accepted / Reconciled / Manager-Provisioned V1 implemented and certified |
| [025](Architecture/ADRs/IF-ADR-025-Local-Player-Input-Ownership-and-Device-Association.md) | Local Player input ownership and device association | Accepted / Implemented |
| [026](Architecture/ADRs/IF-ADR-026-Camera-Subjects-Assignment-and-Multi-Output-Topology.md) | Camera Subjects, Assignment and multi-output topology | **Reopened** — A..G retained; H2 certified; I/layout reconciliation pending |
| [027](Architecture/ADRs/IF-ADR-027-Camera-Authoring-Definitions-and-Composition-Authority.md) | Camera authoring definitions and composition authority | **Reopened** — A/B/C retained; D2 pending; E deferred; F final closure pending |
| [028](Architecture/ADRs/IF-ADR-028-Camera-Output-Participation-and-Presentation-Layout-Authority.md) | Camera Output participation and Presentation Layout authority | **Accepted architecture / partial implementation** — 028-A certified; B/C/D pending |

## Current reconciliation / certification records

Key current or recent records:

- [Camera Output Participation and Layout Authority Reconciliation — 2026-09-12](Architecture/Reconciliation/IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)
- [Camera Full Technical Certification — 2026-09-12](Architecture/Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md) — historical boundary after IF-ADR-028 reconciliation
- [IF-ADR-026 Shared Camera Technical Certification — 2026-09-09](Architecture/Reconciliation/IF-ADR-026-SHARED-CAMERA-TECHNICAL-CERTIFICATION-2026-09-09.md)
- [IF-ADR-024 Prepared Actor Replacement Technical Certification — 2026-09-02](Architecture/Reconciliation/IF-ADR-024-PREPARED-ACTOR-REPLACEMENT-TECHNICAL-CERTIFICATION-2026-09-02.md)
- [IF-ADR-023A Player Actor Occurrence Identity Boundary — 2026-08-31](Architecture/Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)
- [IF-ADR-023 Player Actor Runtime Technical Certification — 2026-08-29](Architecture/Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)
- [IF-ADR-015B Player Actor Selection Public Surface Certification — 2026-08-26](Architecture/Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)
- [Player Current Aggregate Recertification — 2026-08-24](Architecture/Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)
- [IF-ADR-009 Contribution / Visibility Technical Certification — 2026-08-30](Architecture/Reconciliation/IF-ADR-009-CONTRIBUTION-VISIBILITY-TECHNICAL-CERTIFICATION-2026-08-30.md)

Older dated records remain evidence for the exact boundaries they executed. Do not rewrite an older certification to imply it tested a later contract.

## Documentation maintenance rule

When architecture changes after certification:

```text
1. preserve the dated certification result;
2. mark its scope historical if the certified contract is superseded;
3. update/reopen the affected ADRs;
4. add a reconciliation record;
5. update the Tracker and current usage guide;
6. implement the corrected boundary;
7. recertify with a new dated result.
```

Do not keep two current authorities for the same concern.
