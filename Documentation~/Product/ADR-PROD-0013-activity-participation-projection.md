# ADR-PROD-0013 — Activity Player Participation Projection

Status: Accepted  
Date: 2026-07-13  
Amended: 2026-07-22 — Projection is Activity-owned serialized configuration, not a reusable Profile asset
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
Requirements Profile = reusable readiness policy
Activity Projection configuration = contextual Slot selection
PlayerSlotProfile = immutable Slot identity
Session state = mutable participation facts
```

A reusable Requirements Profile must not contain Activity-specific Slot identities.

## Decision

Every `ActivityAsset` owns one explicit Projection configuration and references one
mandatory Requirements Profile:

```text
Activity Participation Projection configuration
Player Participation Requirements Profile
```

Projection is not a `ScriptableObject`. It has no independent product identity, metadata or
cross-owner authority; it only configures the Activity that evaluates it. Keeping it as a
separate asset added navigation without adding a reusable product concept.

The serialized enum fields always hold a concrete value. The Requirements reference remains
mandatory. An Activity with no Players uses:

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

Projects the exact ordered `PlayerSlotProfile` references authored in the Activity.

Rules:

```text
At least one Profile is required.
References and PlayerSlotId values must be unique.
Zero Participant Policy must be Rejected.
Slot identity is never copied as inline text.
```

The explicit set is evaluated as authored. A Slot that has not joined is not silently removed from the set; the Requirements Profile can therefore reject missing Joined state.

## Zero-participant decision

Zero-participant behavior belongs to the Activity Projection configuration because it describes
the cardinality and validity of the selected contextual set.

The initial cut does not introduce a general minimum-player count. Evidence for `minimumCount`, teams, optional members or role topology is insufficient and those concerns must not be anticipated.

## Evaluation descriptor

The Activity produces an immutable `ActivityParticipationProjectionDescriptor` containing:

```text
Projection Mode
Zero Participant Policy
ordered explicit PlayerSlotProfile references
```

The descriptor carries no mutable Session state and performs no lookup. The scoped runtime authority and actual evaluation stage remain later P3 decisions.

## Product authoring flow

```text
Open Activity
-> choose No Slots, All Joined Slots or Explicit Slots
-> declare zero-participant behavior
-> add explicit PlayerSlotProfile references when applicable
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
Projection and Requirements remain separate decisions.
Projection is serialized directly in ActivityAsset.
Requirements remains a reusable Profile.
Every Activity makes both choices explicitly.
No-player Activities use NoSlots + None.
AllJoinedSlots has explicit zero-player behavior.
ExplicitSlots references PlayerSlotProfile assets, not copied identity strings.
Projection descriptors are deterministic and contain no mutable runtime state.
Invalid combinations fail visibly in authoring validation.
No Projection Profile asset or Projection template set is created.
```

## Amendment consequences

```text
Activity assets are the single authoring owner of contextual Slot projection.
PlayerSlotProfile references remain typed assets and are not copied as identity strings.
The immutable runtime descriptor and admission behavior are preserved.
Existing Activities migrate each referenced Projection Profile value to equivalent inline fields.
The removed Profile type has no compatibility layer or silent fallback.
```
