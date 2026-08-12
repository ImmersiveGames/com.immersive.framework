# IF-TRACK — Immersive Framework

Status: **Active — Stage B baseline + proposed architecture expansion**  
Last updated: **2026-08-12**

## Authority and status model

This is the single mutable summary of current delivery state. Its authority is
below accepted ADRs, governance records and current reconciliation/certification
records, and above historical audits, completion summaries and plans.

```text
Accepted ADRs   -> normative architecture
Proposed ADRs   -> pending architecture; not implementation/certification authority
Governance      -> cross-cutting compatibility/product policy
Reconciliation  -> current technical alignment and certification
Tracker         -> current mutable delivery state
FIRSTGAME       -> Stage B real-consumer evidence
Archive         -> historical, non-authoritative
```

A proposed ADR or proposed reconciliation draft may define the intended next boundary,
but it must not be reported as implemented, certified or consumer-proven until the
corresponding package, QA and integration evidence exists.

## Current canonical baseline

Repository HEAD reviewed for this tracker update:

```text
ImmersiveGames/com.immersive.framework
7bfe77f8371338f1abbc4a1c2d9dd3fa42ce7e04
New ADrs
```

That HEAD adds proposed architecture records and proposed reconciliation deltas. It does
not supersede the currently approved executable Stage A package baseline below.

The current package baseline approved for FIRSTGAME Stage B work on accepted boundaries
remains:

```text
ImmersiveGames/com.immersive.framework
7a20ec748e4e5f5f3764bdc34ee249c1fe1c1da6
fix(authoring): enforce validation governance semantics
```

Companion QA baseline:

```text
rinnocenti/QAFramework
d65c5a7a637d4545e8b52b031614f879595335a3
qa: prove validation governance policy
```

Canonical closure record:

[Stage A Canonical Package Baseline Closure](../Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

## Reconciliation sequence

```text
Stage A — technical reconciliation
  accepted ADR -> package -> technical QA -> reconciliation/certification

Stage B — real consumer proof
  accepted package boundary -> FIRSTGAME -> real integration -> usability/product proof
```

Stage B evidence does not reopen a closed Stage A technical boundary by default.
It can identify a product/UX issue, a real-integration gap, future scope, or a
reproducible technical regression.

A newly proposed ADR is tracked separately from the closed accepted baseline. Acceptance
of new architecture creates a new technical implementation/reconciliation cut without
retroactively invalidating the already certified baseline.

## Current ADR status

| ADR | Architecture / package | Technical QA | Stage B / current disposition |
|---|---|---|---|
| [001](../ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED | Core lifecycle proven; current Stage A boundary closed. |
| [002](../ADRs/IF-ADR-002-Product-Authoring-Model.md) | ACCEPTED / RECONCILED / IMPLEMENTED | No generic cross-cutting QA gate | Stage A closed; product proof remains feature-owned. |
| [003](../ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | ACCEPTED baseline / RECONCILED / IMPLEMENTED; R6/R7/R8 reconciliation DRAFT pending | CERTIFIED baseline; draft delta not certified | Existing Player proof remains required. Targeted Slot Join and explicit Actor Selection are proposed extensions, not delivered baseline behavior. |
| [004](../ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED for single-output boundary | CERTIFIED | Complete real-consumer Camera proof; multi-output remains future scope. |
| [005](../ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Input Gate 9/9; Restart 8/8; Pause 27/27 | Stage A closed; Stage B may test authoring/usability. |
| [006](../ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: focused matrix 8/8; Progress 32/32; Terminal 34/34 | Prove real Loading/Transition authoring and diagnostics. |
| [007](../ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Foundation 18/18; Direct Policies 42/42 | Prove real readiness authoring, cover/wait/reveal behavior and diagnostics. |
| [008](../ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) | ACCEPTED / RECONCILED / IMPLEMENTED | No default technical gate | Current boundary closed; reopen only on concrete contract failure/change. |
| [009](../ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED | Current boundary closed. |
| [010](../ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) | ACCEPTED / IMPLEMENTED | No generic synthetic UX QA gate | Feature-owned adoption evidence continues in real product flows. |
| [011](../ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Progress 32/32; Terminal 34/34; Route 25/25; App 20/20 | Prove real participant-aware Loading progress/usability where used. |
| [012](../ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED | FIRSTGAME participation proof required. |
| [013](../ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | ACCEPTED / Experimental / IMPLEMENTED | CERTIFIED: Audio 26/26; ADR-013A 11/11 | FIRSTGAME real-consumer proof is the promotion gate. |
| [014](../ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | ACCEPTED / IMPLEMENTED | CERTIFIED | Current boundary closed and already consumer-proven. |
| [015](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | ACCEPTED baseline / IMPLEMENTED; R6/R7/R8 reconciliation DRAFT pending | CERTIFIED baseline; draft delta not certified | Existing command surface is consumer-usable; targeted Join and explicit Actor Selection remain proposed until implemented and proved. |
| [016](../ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | ACCEPTED baseline / IMPLEMENTED; R6/R7/R8 reconciliation DRAFT pending | CERTIFIED baseline; draft delta not certified | Scene-/Manager-Provisioned baseline remains valid; Host Provisioning, Slot Assignment and Actor Selection stay separate decisions. |
| [017](../ADRs/IF-ADR-017-Application-Frame-Rate-Project-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Edit 13/13; Target 13/13; VSync 13/13; Defaults 13/13 | Stage A closed; no mandatory FIRSTGAME gate for current project-level boundary. |
| [018](../ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | ACCEPTED / RECONCILED; A Stable, B JSON certified, C composition implemented | CERTIFIED: backend conformance; JSON 18/18; composition 12/12 | ADR018-D FIRSTGAME real-consumer persistence/backend usability proof. |
| [019](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) | PROPOSED / NOT IMPLEMENTED as complete decision | NOT STARTED for proposed boundary | Defines Session-scoped Logical Player lifetime, Activity-scoped representation and provisioning-specific physical lifetimes. Must be accepted before implementation/certification is reported. |
| [020](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) | PROPOSED / NOT IMPLEMENTED | NOT STARTED | Defines explicit occurrence-aware Session Player Leave and staged resource release. Depends on the lifetime boundary introduced by proposed ADR-019. |
| [021](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) | PROPOSED / NOT IMPLEMENTED | NOT STARTED | Defines Activity-scoped Initial Placement, explicit Slot-to-Anchor intent and no-fallback placement before readiness. |
| [022](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) | PROPOSED / NOT IMPLEMENTED | NOT STARTED | Extends CameraRigComposer presentation authoring/materialization while preserving ADR-004 single-output request/output authority. |

## Proposed architecture expansion — pre-FIRSTGAME review

Repository HEAD `7bfe77f8371338f1abbc4a1c2d9dd3fa42ce7e04` records the current proposed
architecture expansion discovered before continuing normal FIRSTGAME proof.

### Player R6 / R7 / R8 reconciliation draft

ADR-003, ADR-015 and ADR-016 now carry a proposed reconciliation draft with these
separations:

```text
Host Provisioning
  Session-wide technical Host provisioning decision

Slot Assignment
  which configured Player Slot a joining Player occupies

Actor Selection
  which ActorProfile is selected for one Joined Player Slot
```

The draft accepts explicit intent for:

```text
Request Join
  first eligible vacant Supported Slot

Request Join To Slot
  exact requested Supported Slot
  no fallback to another Slot

Request Default Actor Selection
Request Actor Selection
  exact Joined Slot + ActorProfile
```

The draft explicitly does **not** open heterogeneous/per-Slot Host Provisioning and does
not combine Slot targeting with Actor selection into one implicit Join transaction.

These deltas are not part of the currently certified baseline until implementation and
focused QA exist.

### ADR-019 — Session Player lifetime

Proposed canonical lifetime:

```text
Session
  Joined Logical Player              -> Session-scoped
  Manager-Provisioned Local Host      -> Session-owned after successful Join

Activity
  Player/Actor representation         -> contextual occurrence
  readiness / camera / bindings       -> Activity-local

Scene-Provided Host + Actor
  physical ownership                  -> consumer scene
  Session association                 -> contextual bind/adopt/reprojection
```

A joined Logical Player persists across Activity changes. Activity exit is not Player
Leave, and a later Activity representation for the same Session Player is not a second
Join. No authored `Session Persistent` switch is introduced.

### ADR-020 — Session Player Leave

Proposed Leave is one explicit Session mutation targeting an exact current Player
occurrence. The release transaction is staged:

```text
validate target + occurrence
  -> stage Leaving
  -> release current Activity representation, if present
  -> release provisioning-specific Session resources
  -> clear Session Player associations
  -> commit Slot Vacant
```

Manager-Provisioned Hosts are released through provisioning authority. Scene-Provided
physical objects remain consumer-owned. A stale Leave must never remove a later Player
occurrence that reused the same Slot.

### ADR-021 — Activity Initial Placement

Proposed placement authority is Activity-scoped and explicitly Slot-addressed:

```text
Activity
  Player1 -> Placement Anchor A
  Player2 -> Placement Anchor B
```

Manager-Provisioned contextual Actors require Activity placement when configured for
that flow. Scene-Provided representations explicitly choose between:

```text
Preserve Authored Pose
Apply Activity Placement
```

Missing required placement is a preparation/readiness failure. There is no fallback to
world origin, Host pose, previous Activity pose, prefab pose or another Slot's anchor.

### ADR-022 — Camera presentation models

ADR-004 Camera request/output authority remains unchanged. The proposed extension keeps
one canonical `CameraRigComposer` and expands its local Presentation Model family to:

```text
Fixed
Follow
Mounted
Third Person
```

Apply/Rebuild remains Editor-owned, explicit, idempotent and ownership-aware. One
Composer continues to materialize one local `CinemachineCamera`; unknown incompatible
external Cinemachine components block materialization instead of being destroyed.

Multi-output/split-screen remains outside this proposal.

## Stage A summary

For the currently accepted and certified technical boundaries:

```text
Package implementation:       APPROVED BASELINE
Technical reconciliation:     CLOSED for current accepted boundaries
Reverse audit:                CLOSED
Open reverse-audit cuts:      NONE
Current generic Stage A task: NONE
```

The closure above does not claim implementation or certification of:

```text
R6/R7/R8 proposed reconciliation deltas
ADR-019
ADR-020
ADR-021
ADR-022
```

If those proposed decisions are accepted, each becomes a new scoped technical/product
cut with its own package implementation and QA evidence. Their existence does not reopen
unrelated already-certified Stage A boundaries.

ADR-010 feature-owned adoption evidence and all listed FIRSTGAME work are not a reason
to keep the generic Stage A audit open.

## Reverse-audit disposition

```text
RA-CUT-01  Application Frame Rate / ADR-017       CLOSED / CERTIFIED
RA-CUT-02  Persistence / ADR-018                  CLOSED FOR STAGE A
RA-CUT-03  Object Entry Ownership Reconciliation CLOSED / DOC RECONCILIATION
RA-CUT-04  Architecture Governance Hygiene       CLOSED / CERTIFIED
```

RA-04 focused QA terminal evidence:

```text
[RA04_QA_VALIDATION_GOVERNANCE]
status='Passed'
cases='17'
unknownKnown='False'
unknownWarningsAsErrors='True'
```

RA-03 Object Entry handoff disposition:

```text
ObjectEntryId / declaration metadata
  -> ADR-014 stable identity / passive metadata

ObjectEntryRuntimeContextSnapshot
  -> derivative projection of ADR-001 lifecycle authority

Reset consumption
  -> ADR-005 downstream consumer only

ObjectEntryRequest / ObjectEntryResult
  -> RETAINED AS EXPERIMENTAL under IF-GOV-001

new Object Entry lifecycle authority
  -> NONE

new ADR
  -> NOT REQUIRED
```

Experimental status is governed maturity, not an unresolved Stage A gap.

## Active work — Stage B / FIRSTGAME

Stage B remains the real-consumer proof lane for the **currently accepted package
baseline**. Proposed ADRs and proposed reconciliation deltas must not be treated as
already available FIRSTGAME capabilities.

Primary areas already identified for Stage B proof on the accepted baseline:

1. **Player** — ADR-003, ADR-012, ADR-015 and ADR-016: participation,
   Scene-/Manager-Provisioned flows, current provisioning commands and session profiles.
   Proposed targeted Join, explicit Actor Selection, cross-Activity Session lifetime,
   Leave and Initial Placement belong to the proposed expansion until accepted and
   implemented.
2. **Loading / Readiness** — ADR-006, ADR-007 and ADR-011: authoring sequence,
   cover/wait/reveal behavior, participant-aware progress and diagnostics.
3. **Camera** — ADR-004: real single-output consumer integration and usability. Proposed
   ADR-022 presentation models are not part of the certified baseline yet.
4. **Pause** — ADR-005: consumer authoring/usability rather than re-proving the already
   certified runtime contract.
5. **Audio** — ADR-013: real optional BGM integration and promotion evidence.
6. **Progression Save** — ADR-018: Built-in JSON persistence, close/reopen/load,
   Custom Provider replacement and explicit scoped runtime delivery usability.
7. **Editor/Product Surface** — ADR-010: feature-owned Inspector/discovery/workflow
   evidence gathered through the actual systems above.

## Classification of FIRSTGAME findings

```text
Product / UX finding
  -> authoring/discovery/Inspector/template/diagnostic problem

Real integration finding
  -> technical parts work, but an official product surface is missing or awkward

Technical regression
  -> accepted package contract is reproducibly broken

Future scope
  -> desired capability is outside the current accepted boundary
```

Only a reproducible technical regression or accepted contract change automatically
reopens a closed Stage A technical boundary.

## Reopen conditions for Stage A

Reopen a closed boundary only when at least one occurs:

- reproducible regression against an accepted contract;
- documented contradiction between current package behavior and normative docs;
- accepted contract/architecture change;
- newly accepted scope that extends the boundary.

Do not reopen Stage A solely because a real consumer exposes weak UX, because an
Experimental API still exists, or because a proposed ADR has not yet been accepted.

## Future contracts

The following remain future/deferred contracts, not gaps in the current accepted
baseline:

- device disconnect/reconnect, reassignment and network reconnection semantics;
- heterogeneous per-Slot Host Provisioning;
- consumer-facing physical Actor hot-swap/replacement while represented;
- generic respawn, checkpoint and dynamic Spawn policy beyond ADR-021 Initial Placement;
- Orbital / Free Look input ownership and Spline/Dolly camera product models;
- split-screen and multiple Camera outputs;
- exceptional post-commit compensation beyond the current accepted boundary;
- application-scoped stable-ID resolver;
- Session-scoped Frame Rate override;
- persisted Frame Rate preference integration after the relevant
  Persistence/Preferences architecture is accepted.

Session Player lifetime, Session Player Leave, Activity Initial Placement and expanded
Camera presentation models are no longer unnamed future bullets; they are explicitly
tracked as proposed ADR-019 through ADR-022 above.

## Current architecture / governance records

- [IF-GOV-001 — API Maturity and Validation Governance](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
- [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) — Proposed
- [IF-ADR-020 — Session Player Leave and Resource Release Authority](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) — Proposed
- [IF-ADR-021 — Activity Player Actor Initial Placement Authority](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) — Proposed
- [IF-ADR-022 — Camera Rig Presentation Models and Materialization Authority](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) — Proposed
- [RA-03 — Object Entry Ownership Reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-RA-03-OBJECT-ENTRY-OWNERSHIP-RECONCILIATION-2026-08-11.md)
- [RA-04 — Architecture Governance Hygiene](../Reconciliation/IMMERSIVE-FRAMEWORK-RA-04-ARCHITECTURE-GOVERNANCE-HYGIENE-2026-08-11.md)
- [Stage A Canonical Package Baseline Closure](../Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

## Current reconciliation records

- [ADR-001](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-001-RECONCILIATION-2026-08-10.md)
- [ADR-002](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-002-RECONCILIATION-2026-08-10.md)
- [ADR-002 and ADR-009](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-002-009-RECONCILIATION-2026-08-10.md)
- [ADR-003 and ADR-012](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-003-012-RECONCILIATION-2026-08-10.md)
- [ADR-004 Camera](../Reconciliation/IF-ADR-004A-Camera-Authority-Normative-Reconciliation-2026-08-10.md)
- [ADR-005](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-005-RECONCILIATION-2026-08-10.md)
- [ADR-006](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-006-RECONCILIATION-2026-08-10.md)
- [ADR-007](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-007-RECONCILIATION-2026-08-11.md)
- [ADR-008](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-008-RECONCILIATION-2026-08-10.md)
- [ADR-011](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-011-RECONCILIATION-2026-08-11.md)
- [ADR-017](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-017-RECONCILIATION-2026-08-11.md)
- [ADR-018](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-RECONCILIATION-2026-08-11.md)
- [ADR-018-A Certification](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-A-CERTIFICATION-2026-08-11.md)
- [ADR-018-C Certification](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-C-CERTIFICATION-2026-08-11.md)

There is no reconciliation/certification record yet for ADR-019 through ADR-022.

## Documentation maintenance

- Accepted ADRs remain normative and concise.
- Proposed ADRs remain explicitly marked Proposed until acceptance; the tracker must not
  promote them through wording alone.
- Governance records hold cross-cutting compatibility/product policy.
- Reconciliation records hold technical alignment and certification evidence.
- This tracker stays concise and current; detailed execution history belongs in
  reconciliation or archive records.
- FIRSTGAME evidence must be recorded as Stage B product/consumer evidence rather than
  merged back into historical Stage A audit prose.
