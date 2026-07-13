# ADR-PROD-0013 — Activity Player Participation Projection

Status: Accepted  
Date: 2026-07-13  
Package: `com.immersive.framework`  
Area: Activity Authoring / Player Participation Projection  
Related: `ADR-PROD-0007`, `ADR-PROD-0009`, `ADR-PROD-0011`, `ADR-PROD-0012`, `P3-PLAYER-PARTICIPATION-MATERIALIZATION-IMPLEMENTATION-PLAN`

## Context

`PlayerParticipationRequirementsProfile` defines the progressive readiness level required by an Activity, but it does not select which Session Slots are evaluated.

The P3 plan assigns this decision to `DG-P3-01` and requires a minimum product shape supporting:

```text
all currently Joined Slots
or
an explicit Activity subset
```

The decision must preserve:

```text
Requirements Profile = readiness policy
Projection Profile = contextual Slot selection
PlayerSlotProfile = immutable Slot identity
Session state = mutable participation facts
```

A reusable Requirements Profile must not contain Activity-specific Slot identities.

## Decision

Every `ActivityAsset` references two explicit and mandatory Profiles:

```text
Activity Participation Projection Profile
Player Participation Requirements Profile
```

`null` is invalid for either reference. An Activity with no Players uses explicit assets:

```text
Projection: No Slots
Requirements: None
```

### Initial projection modes

```text
NoSlots
AllJoinedSlots
ExplicitSlots
```

#### NoSlots

Projects no Session Slots.

Rules:

```text
Explicit Slots must be empty.
Zero Participant Policy must be Allowed.
A non-None Requirements Profile is contradictory and invalid.
```

#### AllJoinedSlots

Projects every currently Joined Session Slot.

Deterministic order is the canonical ordered `PlayerSlotProfile[]` configuration in `GameApplicationAsset`, not `PlayerInput.playerIndex`, device order, hierarchy order or join callback order.

Zero-participant behavior is explicit:

```text
Allowed
or
Rejected
```

#### ExplicitSlots

Projects the exact ordered `PlayerSlotProfile` references authored in the Projection Profile.

Rules:

```text
At least one Profile is required.
References and PlayerSlotId values must be unique.
Zero Participant Policy must be Rejected.
Slot identity is never copied as inline text.
```

The explicit set is evaluated as authored. A Slot that has not joined is not silently removed from the set; the Requirements Profile can therefore reject missing Joined state.

## Zero-participant decision

Zero-participant behavior belongs to the Projection Profile because it describes the cardinality and validity of the selected contextual set.

The initial cut does not introduce a general minimum-player count. Evidence for `minimumCount`, teams, optional members or role topology is insufficient and those concerns must not be anticipated.

## Evaluation descriptor

The Profile produces an immutable `ActivityParticipationProjectionDescriptor` containing:

```text
Projection Mode
Zero Participant Policy
ordered explicit PlayerSlotProfile references
```

The descriptor carries no mutable Session state and performs no lookup. The scoped runtime authority and actual evaluation stage remain later P3 decisions.

## Product authoring flow

```text
Create Projection Profile
-> choose No Slots, All Joined Slots or Explicit Slots
-> declare zero-participant behavior
-> add explicit PlayerSlotProfile references when applicable

Open Activity
-> assign Projection Profile
-> assign Requirements Profile
-> inspect designer summary
-> use Advanced / Debug for technical evidence
```

## Guardrails

```text
Do not put Slot identities inside Requirements Profiles.
Do not use null as No Slots, All Joined Slots or None.
Do not derive PlayerSlotId from PlayerInput.playerIndex.
Do not store Joined or readiness state in Profiles.
Do not create a runtime manager in P3D.
Do not evaluate Activity admission in P3D.
Do not add teams, roles, spectators or online topology without a later decision.
```

## Acceptance

```text
Projection and Requirements remain separate reusable Profiles.
Every Activity makes both choices explicitly.
No-player Activities use NoSlots + None.
AllJoinedSlots has explicit zero-player behavior.
ExplicitSlots references PlayerSlotProfile assets, not copied identity strings.
Projection descriptors are deterministic and contain no mutable runtime state.
Invalid combinations fail visibly in authoring validation.
```
