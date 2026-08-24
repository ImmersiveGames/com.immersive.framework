# Immersive Framework Architecture Documentation

Last updated: **2026-08-23**

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

### Player physical lifetime

Current closure authority:

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

Terminal certification:

```text
PLAYER QA CERTIFIED
25/25
```

### Initial Placement discovery / scene authority

Current reconciliation authority:

[IF-ADR-021 — Player Authority and Initial Placement Reconciliation — 2026-08-23](Reconciliation/IF-ADR-021-Player-Authority-and-Initial-Placement-Reconciliation-2026-08-23.md)

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
`ActivityId + PlayerSlotId`. The Primary Scene remains Route-owned. Historical
implementation/QA is preserved, while the reconciled spatial delta remains pending
runtime implementation and replacement QA.

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

## Current affected ADR disposition

### Player

- IF-ADR-003 — Accepted baseline / reconciled / implemented; later R6/R7/R8 expansion remains separately tracked.
- IF-ADR-007 — Accepted baseline / readiness boundary implemented.
- IF-ADR-011 — Accepted baseline for participant-aware readiness/loading interaction.
- IF-ADR-012 — Accepted baseline / implemented.
- IF-ADR-015 — Accepted baseline / current public consumer surface implemented.
- IF-ADR-016 — Accepted baseline / current Session initial configuration implemented.
- IF-ADR-019 — Accepted / reconciled / implemented / QA recertified.
- IF-ADR-020 — Accepted / reconciled / implemented / QA recertified.
- IF-ADR-021 — Accepted / reconciled; Route lifecycle cut implemented; Activity relocation and replacement QA pending.

### Camera

- IF-ADR-004 — Accepted / reconciled / implemented; 004D is the current Default-output presentation correction.
- IF-ADR-004D — Implemented on `master`; Sample 00 consumer proof PASS; focused post-cut Camera QA not yet recorded.
- IF-ADR-010 — Accepted / reconciled for the implemented Camera Class C surface, including explicit required Default authoring in the output Inspector.
- IF-ADR-022 — Accepted / implemented / technical QA certified for local presentation models; broader FIRSTGAME C6 promotion remains separate.

## Historical certification records

Dated certification/reconciliation records remain historical evidence.

Do not rewrite an older record to imply it tested a later contract.

Current revised authorities and current proposed expansions must be interpreted through
the mutable Tracker rather than by commit chronology alone.

## Current delivery state

See:

[Tracking/IF-TRACK-Framework.md](Tracking/IF-TRACK-Framework.md)

The Tracker is the mutable delivery summary. ADRs remain normative architecture.
