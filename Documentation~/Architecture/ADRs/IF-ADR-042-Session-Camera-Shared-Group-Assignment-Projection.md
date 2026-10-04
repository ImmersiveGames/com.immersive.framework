# IF-ADR-042 — Session Camera Shared Group Assignment Projection

Status: **Accepted — source implementation complete; Unity validation pending**
Proposed: **2026-10-04**
Type: architecture / Session Camera / Subject projection / fallback
Depends on: **IF-ADR-038, IF-ADR-039**

## Context

`CameraOccurrenceMode.SharedGroup` already creates one Assignment occurrence per Output and retains explicit Player membership. Group Rig behavior is materialized by `CameraRigComposer`, but Session Camera Assignment authoring rejects it and the occurrence runtime does not project member Subjects into the authored `CinemachineTargetGroup`.

Local multiplayer needs one shared camera on an Output that frames the eligible Actor Subjects of P1/P2 as membership changes. The occurrence must remain stable across Join, Leave, Rejoin and Actor replacement. An empty group cannot provide the required Group framing and therefore uses the Output's Fallback Camera while preserving the Assignment and occurrence.

## Decision

`SessionCameraAssignmentAsset` accepts a Group Rig only with this Assignment contract:

```text
OccurrenceMode      = SharedGroup
MembershipPolicy    = ExplicitPlayerSlots
TargetPolicy        = MemberActorTargets
```

There is one occurrence per Assignment/Output for the Assignment lifetime. The occurrence replaces the Framework-owned `CinemachineTargetGroup` member array from the complete current eligible Subject set on each reconciliation; it never appends to the existing array.

For each eligible Subject, the projected member is:

```text
Target.Object = Subject.Observation
Target.Radius = Subject.FramingRadius > 0
              ? Subject.FramingRadius
              : CameraRigComposer.GroupMemberRadius
Target.Weight = CameraRigComposer.GroupMemberWeight
```

Repeated Observation Transforms are represented once. Group framing follows the materialized Target Group Transform; optional Group LookAt uses that same Transform when configured.

| Eligible Subjects | Target Group | Output state |
|---|---|---|
| 0 | Empty | Fallback covers Output; occurrence remains alive |
| 1 | One member | Fallback is released; occurrence is normal presentation |
| N | N unique Observation members | Fallback is released; occurrence is normal presentation |

Join, Leave, Rejoin, Actor replacement and eligibility changes update the current set on the same occurrence. Rejoin and Actor replacement use only their current exact occurrence/Subject evidence. When the last eligible member leaves, the Target Group is emptied and fallback covers the Output. A new eligible member releases fallback without recreating the occurrence.

The Group Camera Rig Composer materializes `CinemachineGroupFraming` enabled and projects `FramingSize`, `Damping`, `FovRange`, `DollyRange` and `OrthoSizeRange` from `GroupCameraRigBehaviorDefinition`. Apply/Rebuild refreshes these settings on the same Framework-owned component. This is authoring-time rig materialization; Session Camera runtime remains responsible only for current membership and Target Group projection.

## Authority and boundaries

- Camera Session remains the single writer of active Session Camera Assignment state and Output routing, as defined by IF-ADR-039.
- Membership and Subject projection mutate only the occurrence owned by that Session Assignment.
- Fallback remains Output coverage, not a competing Assignment or selection mechanism.
- Route and Activity do not own Camera Assignments, Requests or Outputs. Existing participation may affect Subject eligibility only.
- Individual Output topology, `PlayerInputManager` viewport geometry, `Camera.rect` and `Camera.pixelRect` are unchanged.
- No CameraRequest, Presentation ownership, Route/Activity Camera field or parallel arbitration authority is introduced.

## Validation

Editor test sources cover authoring acceptance/rejection, the zero-to-one-to-two-to-one-to-zero projection sequence, radius/weight projection, fallback transitions, stable occurrence identity, Rejoin, Actor Subject replacement and Group framing materialization/configuration.

Unity import/compile, Camera Editor test execution, prefab Apply/Rebuild and Local Multiplayer Play Mode validation remain required before this decision can be marked validated.
