# IF-ADR-042 — Session Camera Shared Group Assignment Projection

Status: **Accepted — QA certification PASS (2026-10-07)**
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

The dedicated QA fixture `QA-IF-ADR-042` was executed in Unity on 2026-10-07 and certified the public SharedGroup contract with terminal:

```text
status='Passed'
verdict='PASS'
cases='7/7'
cleanup='BaselineRestored'
```

The certified sequence covers baseline zero, `0 -> P1`, `P1 -> P1+P2`, P1 Actor/Subject replacement and restoration, `P1+P2 -> P2`, `P2 -> 0`, and `0 -> P1` rejoin. It verifies stable Assignment/Output/occurrence identity, fallback transitions, unique current Target Group membership, radius/weight projection, contextual Actor/Subject convergence, and deterministic cleanup.

As a regression check, `QA-IF-ADR-043` also passed `7/7` in the same validation session with `framesWithoutCamera='0'` and `cleanup='BaselineRestored'`, providing evidence that the corrected IF-ADR-042 fixture did not regress the related individual physical-participation camera contract.

Framework Editor test sources cover authoring acceptance/rejection, the zero-to-one-to-two-to-one-to-zero projection sequence, radius/weight projection, fallback transitions, stable occurrence identity, Rejoin, Actor Subject replacement and Group framing materialization/configuration. Those Editor tests were not executed as part of this QA certification.

IF-ADR-042 is therefore technically validated by its dedicated Unity QA. Broader IF-ADR-038 certification gates remain independently tracked and are not closed by this result.
