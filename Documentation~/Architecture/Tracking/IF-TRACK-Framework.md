# IF-TRACK — Immersive Framework

Status: **Active — Stage B baseline + ADR-019/ADR-020 closed + ADR-021/ADR-022 proposed**  
Last updated: **2026-08-13**

## Authority and status model

This is the single mutable summary of current delivery state. Its authority is below
accepted ADRs, governance records and current reconciliation/certification records, and
above historical audits, completion summaries and plans.

```text
Accepted ADRs   -> normative architecture
Proposed ADRs   -> pending architecture; not implementation/certification authority
Governance      -> cross-cutting compatibility/product policy
Reconciliation  -> current technical alignment and certification
Tracker         -> current mutable delivery state
FIRSTGAME       -> Stage B real-consumer evidence
Archive         -> historical, non-authoritative
```

A proposed ADR or draft reconciliation must not be reported as implemented/certified.
Conversely, once an accepted boundary has package implementation and evidence, the tracker
must not keep calling it Proposed merely because the last public repository documentation
predates the validated local cut.

## Current canonical baseline

Repository HEAD previously reviewed for the architecture expansion:

```text
ImmersiveGames/com.immersive.framework
7bfe77f8371338f1abbc4a1c2d9dd3fa42ce7e04
New ADrs
```

That HEAD added the architecture records used as the baseline for ADR-019 through ADR-022.
ADR-019 and ADR-020 package/QA work was subsequently validated as complete-file local
cuts. This tracker does not invent a later Git commit for those local validated changes.

The previously approved executable Stage A package baseline remains the historical
baseline for unrelated already-closed boundaries:

```text
ImmersiveGames/com.immersive.framework
7a20ec748e4e5f5f3764bdc34ee249c1fe1c1da6
fix(authoring): enforce validation governance semantics
```

Companion historical QA baseline:

```text
rinnocenti/QAFramework
d65c5a7a637d4545e8b52b031614f879595335a3
qa: prove validation governance policy
```

Canonical baseline closure:

[Stage A Canonical Package Baseline Closure](../Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

## Reconciliation sequence

```text
Stage A / scoped technical reconciliation
  accepted ADR -> package -> technical QA -> reconciliation/certification

Stage B
  accepted package boundary -> FIRSTGAME -> real integration -> usability/product proof
```

Stage B evidence does not reopen a closed technical boundary by default. It may identify a
product/UX finding, real-integration gap, future scope or reproducible technical
regression.

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
| [013](../ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | ACCEPTED / Experimental / IMPLEMENTED | CERTIFIED: Audio 26/26; ADR-013A 11/11 | FIRSTGAME real-consumer proof is promotion gate. |
| [014](../ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | ACCEPTED / IMPLEMENTED | CERTIFIED | Current boundary closed and already consumer-proven. |
| [015](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | ACCEPTED baseline / IMPLEMENTED; IF-ADR-020 Leave command reconciled; R6/R7/R8 draft portions separately tracked | Existing baseline certified; ADR020-H public Leave 26/26 | `Request Leave` is accepted scoped consumer intent; no direct Slot mutation/global control plane. |
| [016](../ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | ACCEPTED / IMPLEMENTED; IF-ADR-020 Joining/Leave reconciliation applied | Existing baseline certified; ADR020-H covers Joining Closed + rejoin semantics | Joining controls admission only; Leave does not reapply initial population. |
| [017](../ADRs/IF-ADR-017-Application-Frame-Rate-Project-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Edit 13/13; Target 13/13; VSync 13/13; Defaults 13/13 | Stage A closed. |
| [018](../ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | ACCEPTED / RECONCILED; A Stable, B JSON certified, C composition implemented | CERTIFIED: backend conformance; JSON 18/18; composition 12/12 | ADR018-D FIRSTGAME real-consumer persistence/backend usability proof. |
| [019](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: ADR19 matrix 5/5; Scene-Provided transition 28/28; Full Player QA | Session-scoped lifetime / Activity representation boundary technically closed. FIRSTGAME proof pending. |
| [020](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED — focused Manager-Provisioned public Leave: ADR020-H 26/26 | Architecture/package closed. Dedicated Scene-Provided **Session Leave** regression is not separately evidenced in the ADR-020 record. FIRSTGAME proof pending. |
| [021](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) | PROPOSED / NOT IMPLEMENTED | NOT STARTED | Defines Activity-scoped Initial Placement. Related ADR-019/020 references are accepted facts; ADR-021 itself remains proposed. |
| [022](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) | PROPOSED / NOT IMPLEMENTED | NOT STARTED | Extends CameraRigComposer presentation authoring/materialization while preserving ADR-004 single-output authority. |

## Architecture expansion — ADR-019 and ADR-020 accepted; ADR-021/ADR-022 proposed

Repository HEAD `7bfe77f8371338f1abbc4a1c2d9dd3fa42ce7e04` records the architecture
expansion discovered before continuing normal FIRSTGAME proof.

```text
ADR-019
  accepted / implemented / QA certified on 2026-08-12

ADR-020
  accepted / implemented / reconciled on 2026-08-13
  focused Manager-Provisioned public QA certified 26/26

ADR-021
  proposed

ADR-022
  proposed
```

### Player R6 / R7 / R8 reconciliation draft

ADR-003, ADR-015 and ADR-016 retain separately tracked draft portions for:

```text
Host Provisioning
  Session-wide technical Host provisioning decision

Slot Assignment
  which configured Player Slot a joining Player occupies

Actor Selection
  which ActorProfile is selected for one Joined Player Slot
```

IF-ADR-020 closure does not automatically certify or promote those unrelated draft deltas.

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

Technical certification on 2026-08-12 proves Session-only readiness separation, occupied
Slot preservation, Manager-Provisioned Host survival across Activity exit, Session
termination release and Scene-Provided reprojection without re-Join.

Detailed evidence:
`../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md`.

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

Focused public Manager-Provisioned QA proves:

```text
ADR020-H
status Passed
cases 26/26
```

Certified proof categories:

```text
PublicLeave
ManagerProvisioned
JoiningClosed
TerminalAvailable
ResourceRelease
ReadinessInvalidation
Rejoin
StaleOccurrence
NoActivityLeave
```

Key reconciliation outcomes:

- Unity physical Host destruction is asserted after canonical settle, not weakened.
- `ExplicitSlots + GameplayReady + Rejected` returns to `WaitingForJoin` after Leave.
- retained baseline summaries are not treated as current authority.
- Leave with no current Activity representation creates no fake representation.
- stale Leave cannot affect a new occurrence that reused the Slot.

Detailed evidence:
`../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md`.

### ADR-020 certification scope

The architecture includes Scene-Provided ownership semantics. ADR-019 already certifies
Scene-Provided contextual reprojection/ownership, but contextual release is intentionally
not Session Leave. No dedicated Scene-Provided **Session Leave** terminal regression is
recorded in the ADR-020 closure.

Therefore the current certification label remains:

```text
Focused Manager-Provisioned public Leave — Certified
Dedicated Scene-Provided Session Leave — Not separately evidenced
```

This does not reopen the accepted architecture/package implementation boundary.

### ADR-021 — Activity Initial Placement

ADR-021 remains Proposed. It defines Activity-scoped Slot-to-Anchor placement intent and
no-fallback placement before readiness. IF-ADR-019 and IF-ADR-020 are now accepted related
decisions; their accepted status does not promote ADR-021.

### ADR-022 — Camera presentation models

ADR-022 remains Proposed. ADR-004 Camera request/output authority remains unchanged.

## Stage A / scoped technical summary

For accepted/certified boundaries:

```text
Historical Stage A baseline       APPROVED
Reverse audit                     CLOSED
ADR-019 scoped extension          CLOSED / CERTIFIED
ADR-020 scoped extension          CLOSED for architecture/implementation
ADR-020 Manager public QA         CERTIFIED 26/26
Open generic reverse-audit cuts   NONE
```

This closure does not claim implementation/certification of:

```text
unproven R6/R7/R8 draft deltas
ADR-021
ADR-022
dedicated Scene-Provided Session Leave QA
FIRSTGAME Stage B proof for ADR-019/020
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

RA-03 Object Entry handoff remains:

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
```

## Active work — Stage B / FIRSTGAME

Stage B remains the real-consumer proof lane for **accepted package boundaries**.
Proposed ADRs must not be treated as already available product capability.

Primary areas:

1. **Player** — ADR-003, ADR-012, ADR-015, ADR-016, ADR-019 and ADR-020: current
   provisioning/participation, Session lifetime, normal consumer Leave, diagnostics and
   real rejoin behavior. ADR-021 Initial Placement remains proposed.
2. **Loading / Readiness** — ADR-006, ADR-007, ADR-011: authoring sequence,
   cover/wait/reveal behavior, participant-aware progress and diagnostics.
3. **Camera** — ADR-004 accepted single-output integration. ADR-022 remains proposed.
4. **Pause** — ADR-005 consumer authoring/usability.
5. **Audio** — ADR-013 real optional BGM integration/promotion evidence.
6. **Progression Save** — ADR-018 Built-in JSON persistence, close/reopen/load, Custom
   Provider replacement and scoped delivery usability.
7. **Editor/Product Surface** — ADR-010 feature-owned Inspector/discovery/workflow evidence.

## Classification of FIRSTGAME findings

```text
Product / UX finding
  -> authoring/discovery/Inspector/template/diagnostic problem

Real integration finding
  -> technical pieces work but official product surface is missing/awkward

Technical regression
  -> accepted package contract is reproducibly broken

Future scope
  -> desired capability is outside current accepted boundary
```

Only a reproducible technical regression or accepted contract change automatically
reopens a closed technical boundary.

## Reopen conditions

Reopen a closed boundary only when at least one occurs:

- reproducible regression against accepted contract;
- documented contradiction between current package behavior and normative docs;
- accepted contract/architecture change;
- newly accepted scope extending the boundary.

Do not reopen solely because Stage B exposes UX friction or because an Experimental API
has not been promoted.

## Future / separate contracts

Still future/deferred:

- device disconnect/reconnect, reassignment and network reconnection semantics;
- heterogeneous per-Slot Host Provisioning;
- consumer-facing physical Actor hot-swap/replacement while represented;
- ADR-021 Initial Placement plus generic respawn/checkpoint/dynamic Spawn beyond it;
- Orbital / Free Look input ownership and Spline/Dolly camera product models;
- split-screen / multiple Camera outputs;
- exceptional post-commit compensation beyond accepted Leave boundary;
- application-scoped stable-ID resolver;
- Session-scoped Frame Rate override;
- persisted Frame Rate preference integration after relevant persistence/preferences architecture.

Resolved:

```text
Session Player lifetime   -> accepted IF-ADR-019
Session Player Leave      -> accepted IF-ADR-020
```

Proposed:

```text
Activity Initial Placement          -> IF-ADR-021
expanded Camera presentation models -> IF-ADR-022
```

## Current architecture / governance records

- [IF-GOV-001 — API Maturity and Validation Governance](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
- [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) — Accepted / Reconciled / Implemented / QA Certified
- [IF-ADR-020 — Session Player Leave and Resource Release Authority](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) — Accepted / Reconciled / Implemented; focused Manager QA 26/26
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
- [ADR-019](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md)
- [ADR-020](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md)

There is no reconciliation/certification record yet for ADR-021 or ADR-022.

## Documentation maintenance

- Accepted ADRs remain normative and concise.
- Proposed ADRs remain explicitly Proposed until acceptance.
- Governance records hold cross-cutting compatibility/product policy.
- Reconciliation records hold technical alignment/certification evidence.
- Tracker stays current; detailed execution history belongs in reconciliation/archive.
- Historical records are not silently rewritten; later closures are appended as dated
  follow-ups where necessary.
- FIRSTGAME evidence remains Stage B product/consumer evidence rather than historical
  Stage A audit prose.
