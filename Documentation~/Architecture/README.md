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

The implementation cut is intentionally product/editor-owned:

```text
Scene-Provided Local Player
  -> official explicit Create action
  -> canonical inspectable prefab/template
  -> coherent Local Player Editor naming family
  -> existing runtime authorities remain separate
```

Stable public C# type renames are not implicit in that product cut; IF-GOV-001 requires
an explicit migration decision for breaking changes to Stable consumer surfaces.

## Current affected ADR disposition

### Player

- IF-ADR-003 — Accepted / reconciled / Player QA recertified.
- IF-ADR-007 — Accepted / reconciled / Player readiness boundary recertified.
- IF-ADR-011 — Accepted / reconciled for Player readiness interaction.
- IF-ADR-012 — Accepted / reconciled / Player QA recertified.
- IF-ADR-015 — Accepted / reconciled / Public Surface certified.
- IF-ADR-016 — Accepted / reconciled / implementation certified.
- IF-ADR-019 — Accepted / reconciled / implementation recertified.
- IF-ADR-020 — Accepted / reconciled / implementation recertified.
- IF-ADR-021 — Accepted / reconciled / implementation certified.

### Camera

- IF-ADR-004 — Accepted / reconciled / Camera QA recertified.
- IF-ADR-010 — Accepted / reconciled for the implemented Camera Class C surface.
- IF-ADR-022 — Accepted / implemented / technical QA certified; FIRSTGAME C6 pending.

## Historical certification records

Dated certification/reconciliation records remain historical evidence.

Do not rewrite an older record to imply it tested a later contract.

Current revised authorities:

```text
Player revised physical lifetime
  2026-08-15 Player Physical Lifetime Recertification

Camera presentation expansion
  2026-08-15 Camera Presentation Technical Certification
```

## Current delivery state

See:

[Tracking/IF-TRACK-Framework.md](Tracking/IF-TRACK-Framework.md)

The Tracker is the mutable delivery summary. ADRs remain normative architecture.
