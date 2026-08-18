# IF-TRACK — Immersive Framework

Status: **Active — Stage B baseline + proposed architecture expansion**  
Last updated: **2026-08-17**

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

Current package implementation merge reviewed for the 2026-08-17 Camera Default-output
tracker update:

```text
ImmersiveGames/com.immersive.framework
master
8591385d14b646b612b32defc7180e71f21a2beb
Merge branch 'camera/default-output-authority-cut'
```

That merge extends the accepted single-output Camera implementation with explicit
output-owned Default presentation. It does not retroactively change what earlier Stage A
or Camera QA baselines tested.

The historical package baseline approved for the original FIRSTGAME Stage B work on
accepted boundaries remains:

```text
ImmersiveGames/com.immersive.framework
7a20ec748e4e5f5f3764bdc34ee249c1fe1c1da6
fix(authoring): enforce validation governance semantics
```

Companion QA baseline recorded for that historical closure:

```text
rinnocenti/QAFramework
d65c5a7a637d4545e8b52b031614f879595335a3
qa: prove validation governance policy
```

Canonical closure record:

[Stage A Canonical Package Baseline Closure](../Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

Current Camera Default-output reconciliation:

[IF-ADR-004D — Camera Default Output Presentation Authority](../Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)

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
| [004](../ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | ACCEPTED / RECONCILED / IMPLEMENTED for single-output boundary; IF-ADR-004D Default-output cut merged | Full Camera 53/53 CERTIFIED for 2026-08-15 boundary; post-004D aggregate rerun not recorded | Sample 00 explicit Default-output + gameplay readiness proof PASS; broader Camera consumer proof remains separate. |
| [005](../ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Input Gate 9/9; Restart 8/8; Pause 27/27 | Stage A closed; Stage B may test authoring/usability. |
| [006](../ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: focused matrix 8/8; Progress 32/32; Terminal 34/34 | Prove real Loading/Transition authoring and diagnostics. |
| [007](../ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED: Foundation 18/18; Direct Policies 42/42 | Prove real readiness authoring, cover/wait/reveal behavior and diagnostics. |
| [008](../ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) | ACCEPTED / RECONCILED / IMPLEMENTED | No default technical gate | Current boundary closed; reopen only on concrete contract failure/change. |
| [009](../ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md) | ACCEPTED / RECONCILED / IMPLEMENTED | CERTIFIED | Current boundary closed. |
| [010](../ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) | ACCEPTED / IMPLEMENTED | No generic synthetic UX QA gate | Camera output Inspector now exposes required Default Camera Rig and validates missing Default explicitly. |
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
| [022](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) | ACCEPTED / IMPLEMENTED | CERTIFIED: Presentation 14/14; Full Camera aggregate 53/53 for 2026-08-15 boundary | C1-C5 closed; broader FIRSTGAME C6 promotion remains pending. |

> Camera status above is reconciled to the current Camera ADRs and 004D implementation.
> Non-Camera proposed/accepted status conflicts elsewhere in the documentation are not
> re-baselined by this scoped Camera tracker update.

## Proposed architecture expansion — pre-FIRSTGAME review

The pre-FIRSTGAME review originally recorded Player R6/R7/R8 and ADR-019 through
ADR-022 as proposed expansion. ADR-022 has since been accepted, implemented and
technically certified; its current status is the table above. The historical proposal
context below remains useful for the still-pending Player decisions.

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

### ADR-022 — Camera presentation models — accepted after proposal

ADR-004 Camera request/output authority remains the normative parent. ADR-022 keeps one
canonical `CameraRigComposer` and expands its local Presentation Model family to:

```text
Fixed
Follow
Mounted
Third Person
```

Apply/Rebuild is Editor-owned, explicit, idempotent and ownership-aware. One Composer
continues to materialize one local `CinemachineCamera`; unknown incompatible external
Cinemachine components block materialization instead of being destroyed.

ADR-022 C1-C5 are now implemented/certified. Multi-output/split-screen remains outside
the accepted product.

The later IF-ADR-004D cut changes only output Default presentation authority; it does not
change ADR-022 local presentation materialization.

## Stage A summary

For the currently accepted and certified technical boundaries:

```text
Package implementation:       APPROVED BASELINE + later accepted scoped cuts
Technical reconciliation:     CLOSED for recorded accepted boundaries
Reverse audit:                CLOSED
Open reverse-audit cuts:      NONE
Current generic Stage A task: NONE
```

The historical closure does not claim implementation or certification of:

```text
R6/R7/R8 proposed reconciliation deltas
ADR-019
ADR-020
ADR-021
```

ADR-022 is no longer part of that not-implemented list; it was accepted and certified on
2026-08-15. IF-ADR-004D is a later scoped Camera correction merged on 2026-08-17 and
consumer-proven in Sample 00, with focused post-cut Camera QA still unrecorded.

If proposed Player decisions are accepted, each becomes a new scoped technical/product
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

Primary areas identified for Stage B proof:

1. **Player** — ADR-003, ADR-012, ADR-015 and ADR-016: participation,
   Scene-/Manager-Provisioned flows, current provisioning commands and session profiles.
   Proposed targeted Join, explicit Actor Selection, cross-Activity Session lifetime,
   Leave and Initial Placement belong to the proposed expansion until accepted and
   implemented.
2. **Loading / Readiness** — ADR-006, ADR-007 and ADR-011: authoring sequence,
   cover/wait/reveal behavior, participant-aware progress and diagnostics.
3. **Camera** — ADR-004/004D explicit Default-output integration is now proven in Sample
   00. ADR-022 broader C6 still needs real use across Fixed/Follow/Mounted/Third Person and
   normal runtime override scenarios. Transition force-default remains package behavior
   without focused consumer proof in the Sample run because no Transition adapter was configured.
4. **Pause** — ADR-005: consumer authoring/usability rather than re-proving the already
   certified runtime contract.
5. **Audio** — ADR-013: real optional BGM integration and promotion evidence.
6. **Progression Save** — ADR-018: Built-in JSON persistence, close/reopen/load,
   Custom Provider replacement and explicit scoped runtime delivery usability.
7. **Editor/Product Surface** — ADR-010: feature-owned Inspector/discovery/workflow
   evidence gathered through the actual systems above.

### Sample 00 — gameplay input readiness authoring correction (2026-08-17)

Stage B audit of `Assets/_Sample/GettingStarted/MinimalGame` isolated the first
first-person gameplay-input failure to Activity authoring rather than locomotion or the
accepted Scene-Provided admission path.

Observed Sample state:

```text
SceneLocalPlayerAdmissionAuthoring.admissionTiming = 0
  -> OnActivityEnter

PlayerSession_MinimalGame.initialJoiningOpen = false
  -> does not block the dedicated Scene-Provided lifecycle admission path in the
     accepted executable baseline

Activity_MinimalGame.playerParticipationRequirementLevel = 30
  -> LogicalActorsPrepared

PlayerGameplayInputConsumerBinding
  HasCurrentGameplayBinding = false
  GameplayReady = false
  BindingRevision = 0
```

Accepted baseline behavior only enters the current gameplay admission/input/camera chain
when the Activity requires `GameplayReady` (`40`). `LogicalActorsPrepared` (`30`) may
therefore complete before any current gameplay consumer binding exists.

Disposition:

```text
FIRSTGAME integration defect
  Activity_MinimalGame requirement is weaker than the gameplay capability consumed

Minimal correction
  playerParticipationRequirementLevel: 30 -> 40
  LogicalActorsPrepared -> GameplayReady

Package regression
  NOT ESTABLISHED by the observed binding state

Locomotion / CharacterController / Move / Look
  no causal defect established before gameplay binding exists
```

The correction strengthens explicit Activity readiness intent. It does not add automatic
Join, change `initialJoiningOpen`, enable Manager Provisioning, add `PlayerInput` to the
Logical Actor, fabricate readiness or weaken ADR-007 fail-closed behavior.

### Sample 00 — Camera Default output authority correction (2026-08-17)

After gameplay admission progressed far enough to exercise Camera eligibility, the next
blocking defect was explicit and package-owned:

```text
CameraOutputSessionBinding
  Blocked
  Camera Output Session Binding requires an explicit Default Camera Rig.
```

The architecture review identified that the historical persistent Session camera had
been represented by a normal `SessionCameraOverrideBinding` request instead of an
output-owned Default presentation.

The accepted correction is IF-ADR-004D:

```text
CameraOutputSessionBinding
  explicit persistent Default Camera Rig

CameraOutputContext
  normal requests only

CameraOutputSession
  force-default -> Default
  normal winner -> winner
  no winner -> Default

SessionCameraOverrideBinding
  optional real Session override only
```

Package implementation:

```text
camera/default-output-authority-cut
688f34e23096c26d2f8e644a432094c64c117ac4

merged master
8591385d14b646b612b32defc7180e71f21a2beb
```

The output Inspector and authoring validator were included in the same product cut so a
required Default is visible in normal authoring and missing Default blocks explicitly.

Sample 00 was migrated by assigning its existing `Session Camera Rig` as the Default on
`CameraOutputSessionBinding`.

Terminal consumer evidence:

```text
CameraOutputSessionBinding
  status = Initialized
  output = camera.output.main
  defaultRig = Session Camera Rig

Activity
  readiness = Ready
  blockingIssues = 0

MinimalFirstPersonLocomotion
  READY
  hasBinding = true
  gameplayReady = true
  bindingRevision = 1
  LOOK_INPUT received
  MOVE_INPUT received
```

Disposition:

```text
Default-output authority
  IMPLEMENTED / SAMPLE 00 PROVEN

locomotion/input failure after Camera correction
  RESOLVED in observed run

Full Camera 53/53
  historical certification remains valid for 2026-08-15 boundary
  does not claim IF-ADR-004D coverage

post-004D focused/aggregate Camera QA
  NOT YET RECORDED

Transition force-default consumer proof
  NOT ESTABLISHED by this Sample run
  transitionAdapterCount = 0

Persistent Content package template artifact
  pre-004D source shape still requires refresh
```

This finding is recorded in:

[IF-ADR-004D — Camera Default Output Presentation Authority](../Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)

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

Session Player lifetime, Session Player Leave and Activity Initial Placement remain
explicitly tracked as proposed ADR-019 through ADR-021 in this tracker snapshot.
Expanded Camera presentation models are no longer future scope: ADR-022 is accepted and
implemented. Multi-output and additional presentation families remain future.

## Current architecture / governance records

- [IF-GOV-001 — API Maturity and Validation Governance](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
- [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) — Proposed in this tracker snapshot
- [IF-ADR-020 — Session Player Leave and Resource Release Authority](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) — Proposed in this tracker snapshot
- [IF-ADR-021 — Activity Player Actor Initial Placement Authority](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) — Proposed in this tracker snapshot
- [IF-ADR-022 — Camera Rig Presentation Models and Materialization Authority](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) — Accepted / Implemented / Technical QA Certified
- [RA-03 — Object Entry Ownership Reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-RA-03-OBJECT-ENTRY-OWNERSHIP-RECONCILIATION-2026-08-11.md)
- [RA-04 — Architecture Governance Hygiene](../Reconciliation/IMMERSIVE-FRAMEWORK-RA-04-ARCHITECTURE-GOVERNANCE-HYGIENE-2026-08-11.md)
- [Stage A Canonical Package Baseline Closure](../Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

## Current reconciliation records

- [ADR-001](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-001-RECONCILIATION-2026-08-10.md)
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
- [ADR-017](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-017-RECONCILIATION-2026-08-11.md)
- [ADR-018](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-RECONCILIATION-2026-08-11.md)
- [ADR-018-A Certification](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-A-CERTIFICATION-2026-08-11.md)
- [ADR-018-C Certification](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-C-CERTIFICATION-2026-08-11.md)

IF-ADR-004D is the current Camera reconciliation record for the explicit persistent
Default-output correction. Historical Camera certification records are retained without
claiming they tested the later cut.

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
- Dated QA records must never be rewritten to imply coverage of later package changes.
