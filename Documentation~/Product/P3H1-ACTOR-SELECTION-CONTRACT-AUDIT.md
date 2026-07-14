# P3H.1 — Actor Selection Contract Audit

Status: decision gate complete  
Type: architecture and product contract documentation  
Date: 2026-07-13

## Objective

Freeze the Actor-selection authority and transaction boundary after P3G real local Player join, without implementing ActorProfile runtime assets, Session selection state, logical Actor materialization, Presentation, or FIRSTGAME UX.

## Baseline inspected

```text
ADR-PROD-0008
  ActorProfile is the canonical selectable Actor identity.

ADR-PROD-0009
  Profiles are immutable; current selection belongs to scoped runtime state.

ADR-PROD-0011
  Slot allocation and Actor selection are separate.
  A Joined Slot without ActorProfile remains Joined.

PlayerParticipationRuntimeContext
  already owns ordered Session Slot allocation state.

PlayerSlotRuntimeSnapshot
  currently exposes Slot identity/allocation but no Actor selection.

LocalPlayerJoinResult
  currently preserves join, Slot, PlayerInput and PlayerActorDeclaration evidence.

PlayerParticipationRequirementLevel.SelectedActors
  already defines the next progressive readiness level.
```

Repository baseline:

```text
f59e005908baf9270e9f54f8142e2c7e8b2f40c1
P3G.4 — integrate real local Player joins with the runtime host
```

## Current gaps

```text
no canonical ActorProfile runtime asset implementation
no ActorProfileId typed value
no explicit Session Actor-selection policy
no selected Actor evidence in PlayerSlotRuntimeSnapshot
no select/replace/clear operation
no duplicate-selection enforcement
no stale-selection revision handling
SelectedActors readiness has no Session selection source yet
```

## Frozen decisions

### Authority

```text
PlayerParticipationRuntimeContext
  owns current ActorProfile selection per Player Slot for the Session
```

Not authorities:

```text
PlayerSlotProfile
ActorProfile
LocalPlayerJoinResult
PlayerInputManager
PlayerInput
PlayerActorDeclaration
Activity runtime
UI selection screen
```

### Timing

```text
join completes first
selection occurs after Slot is Joined
selection may remain absent
selection is required only when a product flow or Activity policy requires it
```

### Join boundary

```text
LocalPlayerJoinRequest does not gain ActorProfile.
LocalPlayerJoinResult remains historical join evidence.
Later current state is read from the Session Slot snapshot.
```

### Runtime state

```text
SelectedActorProfile
SelectionRevision
SelectionSource
SelectionReason
```

are mutable Session Slot state and do not mutate Profile assets.

### Selection targeting

```text
PlayerSlotId + ActorProfile reference
```

Never:

```text
Unity playerIndex
GameObject name
prefab name
copied ActorProfileId string
```

### Policy

Introduce explicit immutable `PlayerActorSelectionPolicyProfile`.

Initial policies:

```text
AllowDuplicates
UniqueAcrossJoinedSlots
```

Null is not an implicit policy.

### Defaults

`PlayerSlotProfile` may reference an optional `DefaultActorProfile`, but applying it is an explicit canonical selection operation after join and must obey policy.

### Replacement

Select, replace and clear are allowed while no logical Actor is prepared. Once a logical host exists, selection changes are rejected until a later explicit Actor replacement transaction exists.

### Readiness

`SelectedActors` checks that every projected Slot has a valid selected ActorProfile. It does not imply logical host, ActorId, Presentation, occupancy, input or camera readiness.

## Product surface affected

Future product surface:

```text
Create > Immersive Framework > Actor > Actor Profile
Create > Immersive Framework > Player > Actor Selection Policy Profile

Player Slot Profile
  Default Actor Profile optional

Session/player selection surface
  current Slot
  selected Actor
  select / replace / clear
  policy diagnostics
```

P3H.1 itself adds documentation only.

## Expected user flow

```text
configure ordered PlayerSlotProfiles
configure explicit Actor selection policy
create ActorProfiles
optionally assign Slot defaults

runtime:
  Player joins
  Slot identity becomes available
  Player selects ActorProfile or applies default
  Session snapshot records selection
  Activity SelectedActors readiness becomes satisfied
```

## Out of scope

```text
C# ActorProfile implementation
selection runtime implementation
Editor inspectors and templates
QA smoke
logical Actor materialization
PlayerInputManager prefab reconciliation
ActorId
Presentation / Skin
FIRSTGAME selection UI
```

## Files

Created:

```text
Documentation~/Product/ADR-PROD-0013-session-actor-profile-selection-and-slot-binding.md
Documentation~/Product/P3H1-ACTOR-SELECTION-CONTRACT-AUDIT.md
```

Associated Unity `.meta` files are included.

No runtime, Editor or QA file is changed.

## Technical acceptance

```text
selection authority is unambiguous
join and selection transactions remain separate
Profile immutability is preserved
Slot and Actor identities remain separate
policy and stale-request behavior are explicit
materialization is not anticipated inside selection
```

## Product acceptance

```text
joined Players may select later
single-player defaults have a defined future location
simultaneous selection is supported
unique-character products have an explicit policy path
Activity readiness can require selection independently from materialization
```

## Next cut

```text
P3H.2 — Actor Profile and Selection Policy Authoring Foundation
```

P3H.2 should implement only:

```text
ActorProfileId
ActorProfile immutable asset
PlayerActorSelectionPolicyProfile
optional PlayerSlotProfile.DefaultActorProfile
Editor inspectors/validators/templates
QA authoring smoke
```

It must not implement Session selection state or logical Actor materialization.

## Suggested commit message

```text
P3H.1 — freeze Session Actor Profile selection contracts
```
