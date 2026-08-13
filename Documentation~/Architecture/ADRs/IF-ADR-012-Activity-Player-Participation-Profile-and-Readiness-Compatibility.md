# IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility

Status: **Accepted**  
Last updated: 2026-08-13  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-010, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020  
Current reconciliation: [ADR-003 / ADR-012 technical reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-003-012-RECONCILIATION-2026-08-10.md)  
ADR-020 follow-up: [ADR-020 reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md)

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Activities need reusable Player participation intent that can express projected
Slots, readiness requirements and compatibility without duplicating Session rules
inside each scene.

## Decision

Activity Player participation is authored through the approved Activity/Route
policy surface and resolves into one normalized effective policy with provenance.

Runtime consumes explicit Slot/Player/Actor evidence and publishes requested and
effective state plus diagnostic reasons. Invalid or incompatible states fail
explicitly.

Activity participation does not own or silently mutate Player Session
configuration.

## Session boundary

```text
PlayerSessionProfile
  owns Supported Slots
  owns Initial Joining
  owns Session Host Provisioning
  owns Actor Resolution

Activity Player policy
  projects/qualifies current Session Slots
  defines participation/readiness intent
  does not replace Session provisioning
  does not create Capacity
```

## Session lifetime and Activity representation boundary

IF-ADR-019 is authoritative for the lifetime consequence of Activity participation.
IF-ADR-020 is authoritative when one exact joined Session Player occurrence explicitly
Leaves.

Activity policy projects/qualifies the current Session; it does not create or destroy
Session membership. Therefore:

```text
Joined Session Player
+ excluded by current Activity policy
  -> Slot remains Joined
  -> valid Session Actor selection remains current
  -> Activity representation may be Absent

Joined Session Player
+ included by current Activity policy
  -> current Activity evaluates the required representation/readiness evidence
```

Activity exit is contextual release, not Session Player Leave. A later Activity can
project the same Joined Logical Player into a new physical occurrence without performing
another Join.

Successful IF-ADR-020 Leave is different: the Session occurrence ends and current
Activity evidence for that occurrence is retired. The Activity then reconciles from the
new Session truth instead of preserving stale readiness.

### Readiness requirement compatibility

The effective Player requirement determines whether a physical Activity representation
is required:

```text
None
JoinedSlots
SelectedActors
  -> Session-level evidence
  -> no physical Activity representation prerequisite

LogicalActorsPrepared
GameplayReady
  -> current Activity representation required
  -> absence cannot be converted to Ready
```

This boundary applies equally to immediate entry and deferred/reconciled readiness after
a required Slot joins later.

### Leave-driven readiness reconciliation

A successful Session Leave invalidates readiness contribution tied to the departed
occurrence. Activity requirement never grants authority to keep that Player joined or to
fabricate Ready evidence after Leave.

For the explicitly certified boundary:

```text
Participation selection  ExplicitSlots
Requirement              GameplayReady
Zero-participant policy  Rejected
Required Player leaves   successfully
```

reconciliation is:

```text
authored Slot projection remains present
current Player occurrence is absent
contribution becomes WaitingForJoin / Preparing
Activity Ready = false
```

The Slot is not removed from the Activity projection merely because Session occupancy
became vacant. This rule is scoped to the declared explicit-slot policy above; other
participation policies continue to follow their own accepted projection semantics.

The Framework must not respond to Leave by:

```text
keeping stale Ready evidence
weakening Required to Optional
automatically joining a replacement Player
silently selecting another Slot
removing an authored Explicit Slot projection to make readiness pass
```

## Observation boundary

Readiness and participation observations may retain baseline or historical summaries for
diagnostics. Presence of a summary object is not by itself proof of current Activity
authority.

Current readiness decisions must use the operational state/correlation for the current
Session Player and Activity occurrence. Released, `NotAdmitted`, `NotEvaluated`, vacant or
otherwise baseline evidence must not be reinterpreted as live gameplay/readiness
authority.

## Constraints

- One normalized effective participation policy is runtime input.
- Provenance and requested/effective differences remain diagnosable.
- Invalid compatibility never falls back silently.
- Activity/GameFlow tests may consume a stable Player fixture but cannot become
  Player Session configuration authority.
- Participation policy does not become a Host provisioning mode.
- Session Leave remains Session authority; Activity readiness only reconciles its
  consequences.
- Readiness evidence is occurrence/revision-correlated and cannot survive as current
  authority after its Player occurrence leaves.

## FIRSTGAME boundary

Real-product integration should prove participation against the same official
package contracts after the underlying Player provisioning mode is integrated.
Any UX friction observed during that proof is qualitative and does not change the
participation contract's functional completion arithmetic.
