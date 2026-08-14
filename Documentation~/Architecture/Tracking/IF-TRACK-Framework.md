# IF-TRACK — Immersive Framework

Status: **Active — Stage B baseline + ADR-019/ADR-020/ADR-021 closed + ADR-022 proposed**  
Last updated: **2026-08-14**

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

Repository HEAD verified for this tracker correction:

```text
ImmersiveGames/com.immersive.framework
661968297a0436c5bcafaa197b86bc486fc7ed4d
ADR21Build
```

Companion QA HEAD containing the executed ADR-021 regression:

```text
rinnocenti/QAFramework
6dfa338461bcd1ece251e8598047d52dfcc085f6
ADR21
```

These HEADs preserve the already closed ADR-019 and ADR-020 boundaries and add the ADR-021 package/QA cut.

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
| [003](../ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | ACCEPTED baseline / RECONCILED / IMPLEMENTED; R6/R7/R8 draft portions remain separately tracked | CERTIFIED baseline; draft delta not implied by ADR-020 | IF-ADR-020 Leave reconciliation applied without promoting unrelated R6/R7/R8 draft scope. |
| [004](../ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED for single-output boundary | CERTIFIED | Complete real-consumer Camera proof; multi-output remains future scope. |
| [005](../ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Input Gate 9/9; Restart 8/8; Pause 27/27 | Stage A closed; Stage B may test authoring/usability. |
| [006](../ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: focused matrix 8/8; Progress 32/32; Terminal 34/34 | Prove real Loading/Transition authoring and diagnostics. |
| [007](../ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Foundation 18/18; Direct Policies 42/42 | Prove real readiness authoring, cover/wait/reveal behavior and diagnostics. |
| [008](../ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) | ACCEPTED / RECONCILED / IMPLEMENTED | No default technical gate | Current boundary closed; reopen only on concrete contract failure/change. |
| [009](../ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED | Current boundary closed. |
| [010](../ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) | ACCEPTED / IMPLEMENTED | No generic synthetic UX QA gate | Feature-owned adoption evidence continues in real product flows. |
| [011](../ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Progress 32/32; Terminal 34/34; Route 25/25; App 20/20 | Prove real participant-aware Loading progress/usability where used. |
| [012](../ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED baseline + ADR-020 readiness follow-up | Explicit Slot Leave reconciliation preserves projection and invalidates stale Ready. FIRSTGAME participation proof pending. |
| [013](../ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | ACCEPTED / Experimental / IMPLEMENTED | CERTIFIED: Audio 26/26; ADR-013A 11/11 | FIRSTGAME real-consumer proof is the promotion gate. |
| [014](../ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | ACCEPTED / IMPLEMENTED | CERTIFIED | Current boundary closed and already consumer-proven. |
| [015](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | ACCEPTED baseline / IMPLEMENTED; IF-ADR-020 Leave command reconciled; R6/R7/R8 draft portions separately tracked | Existing baseline certified; ADR020-H public Leave 26/26 | `Request Leave` is accepted scoped consumer intent; no direct Slot mutation/global control plane. |
| [016](../ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | ACCEPTED / IMPLEMENTED; IF-ADR-020 Joining/Leave reconciliation applied | Existing baseline certified; ADR020-H covers Joining Closed + rejoin semantics | Joining controls admission only; Leave does not reapply initial population. |
| [017](../ADRs/IF-ADR-017-Application-Frame-Rate-Project-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Edit 13/13; Target 13/13; VSync 13/13; Defaults 13/13 | Stage A closed; no mandatory FIRSTGAME gate for current project-level boundary. |
| [018](../ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | ACCEPTED / RECONCILED; A Stable, B JSON certified, C composition implemented | CERTIFIED: backend conformance; JSON 18/18; composition 12/12 | ADR018-D FIRSTGAME real-consumer persistence/backend usability proof. |
| [019](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: ADR19 matrix 5/5; Scene-Provided transition 28/28; Full Player QA | Session-scoped lifetime / Activity representation boundary technically closed. FIRSTGAME proof pending. |
| [020](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED — focused Manager-Provisioned public Leave: ADR020-H 26/26 | Architecture/package closed. Dedicated Scene-Provided **Session Leave** regression is not separately evidenced in the ADR-020 record. FIRSTGAME proof pending. |
| [021](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) | ACCEPTED / IMPLEMENTED | CERTIFIED: ADR-021 Initial Placement 10/10 | Stage A technical boundary closed. FIRSTGAME remains Stage B product/usability evidence, not a prerequisite for technical closure. |
| [022](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) | PROPOSED / NOT IMPLEMENTED | NOT STARTED | Extends CameraRigComposer presentation authoring/materialization while preserving ADR-004 single-output request/output authority. |

## Architecture expansion — ADR-019, ADR-020 and ADR-021 accepted; ADR-022 proposed

ADR-019 and ADR-020 were closed before ADR-021. ADR-021 builds on those accepted boundaries; none of their acceptance, implementation or QA evidence is reopened by this closure.

### Player R6 / R7 / R8 reconciliation draft

ADR-003, ADR-015 and ADR-016 retain separately tracked draft portions for Host Provisioning, targeted Slot Assignment and explicit Actor Selection. IF-ADR-020 closure does not automatically certify or promote those unrelated draft deltas.

### ADR-019 — Session Player lifetime — accepted / implemented / certified

Canonical lifetime:

```text
Session
  Joined Logical Player              -> Session-scoped
  Manager-Provisioned Local Host      -> Session-owned after successful Join

Activity
  Player/Actor representation         -> contextual occurrence
  readiness / Camera / bindings       -> Activity-local

Scene-Provided Host + Actor
  physical ownership                  -> consumer scene
  Session association                 -> contextual bind/adopt/reprojection
```

Technical certification on 2026-08-12 proves Session-only readiness separation, occupied Slot preservation, Manager-Provisioned Host survival across Activity exit, Session termination release and Scene-Provided reprojection without re-Join.

Detailed evidence: `../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md`.

### ADR-020 — Session Player Leave — accepted / implemented / reconciled

Canonical individual terminal transaction:

```text
validate exact Slot + current occurrence
  -> stage Leaving
  -> release current Activity representation if present
  -> release provisioning-specific Session resources
  -> clear occurrence-owned Session associations
  -> terminal commit
  -> Slot Vacant / Available
```

Focused public Manager-Provisioned QA: **ADR020-H 26/26 PASS**. The architecture/package boundary is closed; dedicated Scene-Provided Session Leave remains not separately evidenced.

Detailed evidence: `../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md`.

### ADR-021 — Activity Initial Placement — accepted / implemented / certified

Accepted placement authority is Activity-scoped and explicitly Slot-addressed:

```text
Activity
  Player1 -> Placement Anchor A
  Player2 -> Placement Anchor B
```

Manager-Provisioned contextual Actors use `ActivityPlayerInitialPlacementAuthoring` and place `PlayerActorMaterializationHandle.LogicalActorHost` after materialization and before activation/promotion. Scene-Provided explicitly chooses `PreserveAuthoredPose` or `ApplyActivityPlacement`. Discovery is restricted to canonical `ActivityOwnedScenes`; placement writes position + rotation only, does not reparent, does not apply scale and has no fallback.

Technical closure evidence (2026-08-14):

```text
Package HEAD: 661968297a0436c5bcafaa197b86bc486fc7ed4d (ADR21Build)
QA HEAD:      6dfa338461bcd1ece251e8598047d52dfcc085f6 (ADR21)
[QA_ADR021_INITIAL_PLACEMENT] status='Passed' verdict='ADR-021 INITIAL PLACEMENT VERIFIED' cases='10/10'
```

Placement evidence is Activity-owner + occurrence scoped. Candidate/handoff and Scene-Provided adoption pass the same gate before promotion/adoption. Required placement failure remains a preparation/readiness blocker.

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

## Stage A / scoped technical summary

For accepted/certified boundaries:

```text
Historical Stage A baseline       APPROVED
Reverse audit                     CLOSED
ADR-019 scoped extension          CLOSED / CERTIFIED
ADR-020 scoped extension          CLOSED for architecture/implementation
ADR-020 Manager public QA         CERTIFIED 26/26
ADR-021 scoped extension          CLOSED / CERTIFIED 10/10
Open generic reverse-audit cuts   NONE
```

This closure does not claim implementation/certification of:

```text
unproven R6/R7/R8 draft deltas
ADR-022
dedicated Scene-Provided Session Leave QA
FIRSTGAME Stage B proof for ADR-019/020/021
```

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

1. **Player** — ADR-003, ADR-012, ADR-015, ADR-016, ADR-019, ADR-020 and ADR-021: current provisioning/participation, Session lifetime, explicit Leave, Activity Initial Placement, diagnostics and real rejoin/placement usability. ADR-022 remains unrelated Camera scope.
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

Resolved:

```text
Session Player lifetime     -> accepted IF-ADR-019
Session Player Leave        -> accepted IF-ADR-020
Activity Initial Placement  -> accepted IF-ADR-021
```

Proposed:

```text
expanded Camera presentation models -> IF-ADR-022
```

## Current architecture / governance records

- [IF-GOV-001 — API Maturity and Validation Governance](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
- [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) — Accepted / Reconciled / Implemented / QA Certified
- [IF-ADR-020 — Session Player Leave and Resource Release Authority](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) — Accepted / Reconciled / Implemented; focused Manager QA 26/26
- [IF-ADR-021 — Activity Player Actor Initial Placement Authority](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) — Accepted / Implemented / QA Certified (10/10)
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
- [ADR-019](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md)
- [ADR-020](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md)

ADR-019 and ADR-020 retain their existing reconciliation records. ADR-021 technical closure is recorded in its accepted ADR and focused Unity QA evidence; no separate reconciliation record is added by this documentation-only closure. ADR-022 remains without reconciliation/certification.

## Documentation maintenance

- Accepted ADRs remain normative and concise.
- Proposed ADRs remain explicitly marked Proposed until acceptance; accepted ADRs must not be downgraded by stale tracker wording.
- Governance records hold cross-cutting compatibility/product policy.
- Reconciliation records hold technical alignment and certification evidence.
- This tracker stays concise and current; detailed execution history belongs in
  reconciliation or archive records.
- FIRSTGAME evidence must be recorded as Stage B product/consumer evidence rather than
  merged back into historical Stage A audit prose.
