# IF-ADR-042 — Session Camera Shared Group Assignment Projection

Status: **Accepted — SharedGroup consumer manual validation PASS; Framework Unity validation pending**
Proposed: **2026-10-04**
Type: architecture / Session Camera / Subject projection / fallback
Depends on: **IF-ADR-038, IF-ADR-039**

## Context

Shared Group Assignment projection requires one stable occurrence per Assignment/Output, explicit Player membership, current Actor Subject projection, Group framing, and Output fallback while the group is empty. IF-ADR-042 defines this contract for the Local Multiplayer P1/P2 consumer.

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

Local Multiplayer manual Unity consumer validation was reported PASS on 2026-10-06 for:

```text
1 Player follows translation without orbiting around the Actor
2 Players are framed together
separating Players uses dolly/FOV to keep both visible
rotating an Actor in place does not move its Subject
```

Framework Editor test sources cover authoring acceptance/rejection, the zero-to-one-to-two-to-one-to-zero projection sequence, radius/weight projection, fallback transitions, stable occurrence identity, Rejoin, Actor Subject replacement and Group framing materialization/configuration. They have not been executed as part of this consumer validation.

Framework Unity import/compile, Camera Editor test execution, prefab Apply/Rebuild validation, and the remaining IF-ADR-038 QA/certification gates remain pending. Consumer PASS does not close those gates.
