# Immersive Framework Architecture Documentation

Last updated: **2026-08-17**

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

FIRSTGAME C6 remains consumer proof, not technical package certification.

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
- IF-ADR-019 — Proposed / not implementation authority for the accepted Stage B baseline.
- IF-ADR-020 — Proposed / not implementation authority for the accepted Stage B baseline.
- IF-ADR-021 — Proposed / not implementation authority for the accepted Stage B baseline.

### Camera

- IF-ADR-004 — Accepted / reconciled / Camera QA recertified.
- IF-ADR-010 — Accepted / reconciled for the implemented Camera Class C surface.
- IF-ADR-022 — tracked according to the current Tracker; do not infer implementation authority from a proposed record alone.

## Historical certification records

Dated certification/reconciliation records remain historical evidence.

Do not rewrite an older record to imply it tested a later contract.

Current revised authorities and current proposed expansions must be interpreted through
the mutable Tracker rather than by commit chronology alone.

## Current delivery state

See:

[Tracking/IF-TRACK-Framework.md](Tracking/IF-TRACK-Framework.md)

The Tracker is the mutable delivery summary. ADRs remain normative architecture.
