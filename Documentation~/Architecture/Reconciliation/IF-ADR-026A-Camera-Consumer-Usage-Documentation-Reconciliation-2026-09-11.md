# IF-ADR-026A — Camera Consumer Usage Documentation Reconciliation

Date: **2026-09-11**  
Status: **RECONCILED — documentation-only**

## Scope

This record reconciles the current consumer-facing Camera usage guide with the implemented
IF-ADR-026 Camera architecture on `master`.

No runtime code, QA code, scenes, assets or FIRSTGAME content are changed by this record.

Authoritative product guide after this reconciliation:

- [Camera Usage](../../Guides/Camera-Usage.md)

Normative architecture remains:

- [IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology](../ADRs/IF-ADR-026-Camera-Subjects-Assignment-and-Multi-Output-Topology.md)
- [IF-ADR-004D — Camera Default Output Presentation Authority](IF-ADR-004D-Camera-Default-Output-Presentation-Authority-2026-08-17.md)
- [IF-ADR-022 — Camera Rig Presentation Models](../ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md)

## Problem

`Camera-Usage.md` had accumulated correct newer IF-ADR-026 material alongside older
consumer statements from the pre-026 Camera model.

Two contradictions were material for new consumers:

```text
new/current statement
  Session supports 1..N explicitly authored Camera Outputs

older statement retained later in the same guide
  current implementation/validator requires exactly one persistent Output
```

and:

```text
new/current IF-ADR-026 model
  ordinary Player -> Camera Subject availability

older publisher list retained in the guide
  eligible Local Player Camera publication
```

The second statement no longer represents ordinary Player Camera participation after the
IF-ADR-026 removal of ordinary per-Player Camera request publication.

## Reconciled current model

The consumer-facing model is now documented as:

```text
Camera Subject
  -> Camera View / Assignment
  -> CameraRigComposer
  -> Camera Output
  -> Unity Camera + CinemachineBrain
```

The following rules are explicit:

1. Player participation contributes Camera Subject availability; it does not implicitly
   own a View, Rig, Output or viewport.
2. Player count does not determine Camera Output count.
3. The Session Camera topology supports `1..N` explicitly authored Outputs.
4. Every physical Output has an explicit `CameraOutputId`, Unity Camera,
   `CinemachineBrain` and Default `CameraRigComposer`.
5. `CameraViewOutputPolicyAuthoring` explicitly maps View to Output and normalized
   viewport.
6. The persistent Default Camera Rig is not a Camera Request and is not represented by
   `SessionCameraOverride`.
7. The current built-in scoped normal publishers documented for consumer use are
   Activity, Route and Session Camera Overrides.
8. Ordinary Local Player Camera request publication is not documented as a current
   publisher path.
9. `ActorCameraSubjectAuthoring` is the explicit surface for publishing an exact Actor
   Presentation child observation/mount Transform when the Actor root is not the intended
   observation pose.
10. Explicit authoring failures block; there is no root/name/hierarchy/global discovery
    fallback.

## Certification boundary

Documentation now distinguishes implementation support from recorded proof.

Current records remain:

```text
Historical Full Camera aggregate — 2026-08-15
  53/53
  valid for its historical boundary

IF-ADR-026 shared Camera runtime — 2026-09-09
  certified

Actor Presentation exact child observation Transform
  implemented on current master
  focused fresh Unity lifecycle proof pending

Split / fresh Full Camera aggregate / broader FIRSTGAME visual proof
  pending independent proof
```

Supporting `1..N` topology in architecture/runtime does not by itself relabel pending split
or consumer proof as certified.

## Documentation policy

`Guides/Camera-Usage.md` is a **current usage guide**. Historical superseded consumer
instructions should not be retained there merely for chronology.

Historical architecture, prior contracts and certification dates belong in ADRs,
Reconciliation records and the Framework Tracker.

FIRSTGAME remains a real-consumer proof surface. It does not define Framework Camera
architecture.