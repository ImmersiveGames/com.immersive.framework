# Immersive Framework Architecture Documentation

Last updated: **2026-08-26**

## Normative architecture

`ADRs/` contains accepted and proposed architecture decisions.

```text
Accepted
  -> normative architecture

Proposed
  -> pending architecture

Reopened
  -> architecture has been corrected/reconfirmed but implementation and/or
     prior certification must be reconciled before the boundary is closed again
```

## Current major technical closures

### Player Actor Selection public surface

Current public-surface certification authority:

[IF-ADR-015B — Player Actor Selection Public Surface Certification — 2026-08-26](Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)

The delivered Player Session public surface is:

```text
PlayerSessionObserver
  read-only scoped Session evidence

explicit commands
  Open Joining
  Close Joining
  Join
  Select Actor
  Select Default Actor
  Replace Actor Selection
  Clear Actor Selection
  Leave
```

Actor Selection remains Session-owned logical intent and does not become physical Actor hot-swap authority.

Current integrated certification:

```text
PLAYER CURRENT AGGREGATE COMPLETE
mandatoryContracts = 27
executedContracts = 27
passedContracts = 27
actor = PASS
publicSurface = PASS
```

The public arbitrary Actor-selection blocker for the Character Selection sample is closed. Exact-Slot public Join and the public Slot/device/input ownership contract remain future Player scope.

### Player physical lifetime

Current Player certification authority for the 2026-08-24 Model B/lifetime boundary:

[Player Current Aggregate Recertification — 2026-08-24](Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)

Historical physical-lifetime recertification:

[Player Physical Lifetime Recertification — 2026-08-15](Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)

Frozen model:

```text
Session
  owns admitted physical Player after successful admission
  retains physical preparation until Leave / Session termination

Activity
  owns contextual projection / activation / gameplay / camera / readiness
  does not own terminal physical Player lifetime
```

Current terminal certification:

```text
PLAYER CURRENT AGGREGATE COMPLETE
27/27
```

Historical Full Player `25/25` remains valid dated evidence for the 2026-08-15 boundary and is not relabeled as the current aggregate.

### Initial Placement discovery / scene authority

Current reconciliation authority:

[IF-ADR-021 — Player Authority and Initial Placement Reconciliation — 2026-08-23](Reconciliation/IF-ADR-021-Player-Authority-and-Initial-Placement-Reconciliation-2026-08-23.md)

Current certification:

[Player Current Aggregate Recertification — 2026-08-24](Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)

Frozen authority matrix:

```text
Session owns Player provisioning and admitted physical lifetime.
Route owns the Primary Scene, Route composition and baseline spatial-entry intent.
Activity owns contextual participation/readiness/representation, optional content and optional explicit relocation.
ActivityContentProfile remains optional.
```

IF-ADR-021 accepts Model B: Route owns baseline spatial entry for the current Route
occurrence; Activity owns only opt-in explicit contextual relocation. Route placement
is exact by `RouteId + PlayerSlotId`; Activity relocation is exact by
`ActivityId + PlayerSlotId`. The Primary Scene remains Route-owned.

Replacement implementation and QA are complete for the accepted Model B boundary:

```text
Route Spatial Entry      18/18 PASS
Activity Relocation      23/23 PASS
Full Player aggregate    27/27 PASS
```

Historical ADR-021 Initial Placement `9/9` remains evidence for the superseded
Activity-owned discovery model only.

### Camera Presentation / materialization

Current technical closure authority:

[Camera Presentation Technical Certification — 2026-08-15](Reconciliation/IMMERSIVE-FRAMEWORK-CAMERA-PRESENTATION-TECHNICAL-CERTIFICATION-2026-08-15.md)

Frozen model:

```text
IF-ADR-004
  owns Camera request/output authority

CameraRigComposer
  owns one local rig's Presentation/materialization

Presentation
  Fixed
  Follow
  Mounted
  Third Person

Materialization
  Editor-owned
  exact-reference ownership evidence
  external/unknown conflicts block
  preflight before mutation

CameraOutputRigApplicator
  remains presentation-agnostic
```

Terminal certification:

```text
CAMERA QA CERTIFIED
53/53
```

Breakdown:

```text
ADR-022 Presentation  14/14
C9R                   11/11
ADR-004B              18/18
ADR-004C              10/10
```

The `53/53` run predates the later Default-output authority correction below and
must not be read as certification of that later cut.

### Camera Default output presentation

Current reconciliation authority:

[IF-ADR-004D — Camera Default Output Presentation Authority — 2026-08-17](Reconciliation/IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)

Frozen model:

```text
CameraOutputSessionBinding
  owns one explicit persistent Default Camera Rig

CameraOutputContext
  normal admitted Camera requests only
  deterministic winner arbitration only

CameraOutputSession
  no normal winner -> Default
  normal winner -> winner
  force-default owner active -> Default

SessionCameraOverrideBinding
  optional real Session Camera request
  never the Default Camera
```

Transition presentation now forces/releases Default directly through the output
session rather than publishing a fake Session request or depending on one existing.
The force-default surface is owner-based and idempotent. The 2026-08-17 cut wires
Transition only; it does not introduce Pause-to-Camera authority.

Sample 00 real-consumer evidence after explicit Default authoring:

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

This is Stage B consumer evidence. A new aggregate Camera QA run covering 004D has
not been recorded.

## Current product-authoring decisions

### Scene-Provided Local Player

FIRSTGAME Sample 00 established a concrete Player product-composition gap: a
Scene-Provided Local Player could appear correctly authored while still omit the Unity
Input Gate endpoint required to reach `GameplayReady`.

Current product decision:

[Scene-Provided Local Player Product Composition — 2026-08-17](Reconciliation/IMMERSIVE-FRAMEWORK-SCENE-PROVIDED-LOCAL-PLAYER-PRODUCT-COMPOSITION-2026-08-17.md)

Current implementation/evidence:

```text
Package
  5c9dab5661c95cf712d8cfce124a5d730d0dd1f1
  -> canonical Create Scene-Provided Local Player action implemented

FIRSTGAME
  facb6e2d9b763b7200e670a029c06100505d7c06
  -> Scene-Provided Local Player prefab created
  -> Scene-Provided Logical Player prefab kept as separate ActorProfile authority
  -> scene composes the Logical Player under the Local Player ActorMount
```

Current product split:

```text
Scene-Provided Local Player
  technical Host product
  PlayerInput
  LocalPlayerHostAuthoring
  SceneLocalPlayerAdmissionAuthoring
  UnityPlayerInputGateAdapter
  ActorMount

Scene-Provided Logical Player
  consumer/example ActorProfile.LogicalActorHostPrefab
  gameplay representation
  separate asset authority
```

The official Create action owns deterministic technical composition only. Player Slot,
Actor Profile, `InputActionAsset`, Gameplay Action Map and the exact Logical Player
remain explicit consumer intent.

A neutral inspectable package prefab/template for the technical Local Player shape is
still a product artifact to add. It must match the Create action and must not embed
project-specific Slot / Actor / Input defaults.

Stable public C# type renames are not implicit in this product cut; IF-GOV-001 requires
an explicit migration decision for breaking changes to Stable consumer surfaces.

### Player Session public commands and observation

IF-ADR-015 now defines the implemented public consumer model:

```text
PlayerSessionObserver
  = read

8 explicit Player Session command components
  = request/change
```

Arbitrary Actor Selection is delivered through explicit Select / Default / Replace /
Clear commands. `PlayerSessionProfile.ActorResolution = LeaveUnresolved` is a valid
initial policy for a flow where Join precedes Character Selection.

Do not use Actor-selection commands as a bypass around Actor preparation/materialization,
and do not interpret `Replace Actor Selection` as physical hot-swap.

## Current affected ADR disposition

### Player

- IF-ADR-003 — Accepted / reconciled / implemented; arbitrary Actor-selection lifecycle delivered and current aggregate PASS; physical hot-swap remains future.
- IF-ADR-007 — Accepted baseline / readiness boundary implemented.
- IF-ADR-011 — Accepted baseline for participant-aware readiness/loading interaction.
- IF-ADR-012 — Accepted baseline / implemented; current aggregate PASS.
- IF-ADR-015 — Accepted / reconciled / implemented; Observer + eight explicit commands including Actor Select / Default / Replace / Clear; current aggregate PASS.
- IF-ADR-016 — Accepted / implemented; `ResolveConfiguredDefault` and `LeaveUnresolved` current; `LeaveUnresolved` now has a delivered explicit Actor-selection continuation path.
- IF-ADR-019 — Accepted / reconciled / implemented; current Full Player aggregate 27/27 PASS; historical 25/25 recertification preserved.
- IF-ADR-020 — Accepted / reconciled / implemented; current Full Player aggregate 27/27 PASS; historical 25/25 recertification preserved.
- IF-ADR-021 — Accepted / reconciled / implemented / current QA verified; Route Spatial Entry 18/18, Activity Relocation 23/23 and Full Player aggregate 27/27 PASS.

### Camera

- IF-ADR-004 — Accepted / reconciled / implemented; 004D is the current Default-output presentation correction.
- IF-ADR-004D — Implemented on `master`; Sample 00 consumer proof PASS; focused post-cut Camera QA not yet recorded.
- IF-ADR-010 — Accepted / reconciled for the implemented Camera Class C surface, including explicit required Default authoring in the output Inspector.
- IF-ADR-022 — Accepted / implemented / technical QA certified for local presentation models; broader FIRSTGAME C6 promotion remains separate.

## Historical certification records

Dated certification/reconciliation records remain historical evidence.

Do not rewrite an older record to imply it tested a later contract.

The Full Player `25/25` certification remains the 2026-08-15 historical boundary. The
2026-08-24 current aggregate is the Model B/lifetime `27/27` reconciliation record. The
2026-08-26 IF-ADR-015B record closes the later public Actor-selection extension with a
fresh integrated `27/27` rerun. The historical ADR-021 Initial Placement `9/9` remains
tied to the superseded Activity-owned discovery model.

The package-local Actor-selection Unity Test Framework Editor tests are not claimed as
executed by the integrated QA record unless a separate result is recorded.

## Current delivery state

See:

[Tracking/IF-TRACK-Framework.md](Tracking/IF-TRACK-Framework.md)

The Tracker is the mutable delivery summary. ADRs remain normative architecture.
