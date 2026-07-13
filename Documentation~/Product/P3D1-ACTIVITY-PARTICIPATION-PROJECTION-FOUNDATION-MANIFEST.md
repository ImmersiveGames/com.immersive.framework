# P3D.1 — Activity Participation Projection Foundation

## Objective

Close `DG-P3-01` and establish the reusable Activity Projection product contract without implementing Session state or Activity admission runtime.

## Type

Product contract + UX/product.

## Created

```text
Runtime/PlayerParticipation/Contracts/ActivityParticipationProjectionMode.cs
Runtime/PlayerParticipation/Contracts/ActivityParticipationZeroParticipantPolicy.cs
Runtime/PlayerParticipation/Contracts/ActivityParticipationProjectionDescriptor.cs
Runtime/PlayerParticipation/Authoring/ActivityParticipationProjectionProfile.cs
Editor/PlayerParticipation/ActivityParticipationProjectionAuthoringValidator.cs
Editor/PlayerParticipation/ActivityParticipationProjectionProfileEditor.cs
Documentation~/Product/ADR-PROD-0013-activity-participation-projection.md
Documentation~/Product/P3D1-ACTIVITY-PARTICIPATION-PROJECTION-FOUNDATION-MANIFEST.md
```

## Altered

```text
Runtime/Authoring/ActivityAsset.cs
Editor/Authoring/ActivityAssetEditor.cs
```

## Product surface

```text
Assets > Create > Immersive Framework > Player > Activity Participation Projection Profile
Activity Inspector > Player Participation
  Projection Profile
  Requirements Profile
  designer summary
  Advanced / Debug
  Authoring Validation
```

## Expected use

```text
Main Menu
  Projection: No Slots
  Requirements: None

Character Selection
  Projection: All Joined Slots / zero rejected
  Requirements: Joined Slots

Gameplay
  Projection: Explicit Slots or All Joined Slots
  Requirements: Gameplay Ready
```

## Validation

Blocking authoring failures include:

```text
missing Projection Profile
missing Requirements Profile
invalid enum value
NoSlots with explicit references
NoSlots with zero rejected
AllJoinedSlots with explicit references
ExplicitSlots without references
ExplicitSlots with zero allowed
null or duplicate explicit Profile
invalid or duplicate PlayerSlotId
NoSlots paired with non-None Requirements
```

## Out of scope

```text
Session Slot roster
runtime projection evaluation
join
Actor selection
Actor materialization
Activity admission gate
minimum player count beyond explicit zero policy
teams, roles, spectators or online topology
central Model Readiness integration
QAFramework smoke
FIRSTGAME integration
```

## Technical acceptance

```text
runtime code has no Editor dependency
Profiles contain no mutable Session state
Activity has no silent participation defaults
projection descriptor is deterministic authoring data
invalid combinations fail explicitly
```

## Product acceptance

```text
designer can understand who participates and what readiness is required
no-player Activity is explicit
Advanced / Debug exposes technical evidence without replacing designer-first editing
```

## Next cut

```text
P3D.2
  official Projection templates
  central Model Readiness integration
  QAFramework authoring smoke
```

## Suggested commit

```text
P3D.1 — add Activity participation projection authoring foundation
```
