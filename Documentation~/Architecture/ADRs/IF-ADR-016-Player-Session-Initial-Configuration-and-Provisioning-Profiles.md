# IF-ADR-016 — Player Session Initial Configuration

Status: **Accepted**  
Last updated: 2026-08-12  
Proposed reconciliation draft: **2026-08-11 — R6 / R7 / R8**  
Supersedes: former separate provisioning-Profile, Capacity and per-Slot override model  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-012, IF-ADR-015, IF-ADR-019

> **Draft note:** this file is a proposed reconciliation of the accepted ADR after
> the R6/R7/R8 architecture review. It has not been applied to the repository yet.
> It preserves Session-wide Host Provisioning and clarifies that targeted Slot
> Join and explicit Actor Selection are orthogonal runtime operations.

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Player Session needs one authorable source for initial intent without turning
Profiles into live Session state. The former model split intent across multiple
Profiles, introduced a second Capacity limit and allowed per-Slot Host
Provisioning overrides.

The R6/R7/R8 review confirms that three concerns remain separate:

```text
Host Provisioning
  initial Session-wide technical Host policy

Slot allocation / assignment
  runtime decision about which Supported Slot a Player occupies

Actor selection
  runtime intent selecting ActorProfile for one Joined Slot
```

Different Slot or Actor choices do not require per-Slot Host Provisioning.

## Decision

`PlayerSessionProfile` is the only Profile required to configure initial Player
Session intent.

```text
PlayerSessionProfile
├── Supported Slots
├── Initial Joining
├── Host Provisioning
│   ├── Scene Provided
│   └── Manager Provisioned
└── Actor Resolution
    ├── Resolve Configured Default
    └── Leave Unresolved
```

An application may provide a default Profile. An explicit creation-time Profile
replaces that default completely:

```text
no field merge
invalid explicit source does not fall back
```

A composition that does not enable Player Session is valid absence. A composition
that enables it without a valid Profile fails explicitly.

## Supported Slots and Joining

`Supported Slots` is the complete structural Slot universe and authored
untargeted-Join order.

### Untargeted Join

Normal untargeted Join remains:

```text
Joining Open
+ first eligible vacant Supported Slot in authored order
```

No available Slot produces explicit rejection.

### Targeted Join

The consumer may explicitly request one exact Supported Slot.

```text
Joining Open
+ requested Supported Slot exists
+ requested Slot is vacant/eligible
+ provisioning is compatible
  -> reserve/admit that exact Slot
```

Failure of the requested Slot is explicit.

```text
requested Player2 unavailable

-> reject
-> do not allocate Player1
-> do not allocate Player3
```

Targeted Join is not a generic allocation strategy.

The Framework therefore supports exactly two bounded intents:

```text
untargeted
  first eligible vacant Supported Slot in authored order

targeted
  one exact requested Supported Slot
```

No weighted/random/priority/role-based/custom allocation policy is introduced.

The consumer expresses Slot intent but the created Session remains the only Slot
reservation/assignment authority.

There is no independent Initial/Current/Dynamic Capacity and no runtime
SetCapacity/SetDynamicCapacity command.

## Host Provisioning

Host Provisioning is one Session decision:

```text
Scene Provided
or
Manager Provisioned
```

It applies uniformly to all Supported Slots.

Mixed/per-Slot provisioning is not part of the current model.

The R6 review explicitly confirms:

```text
Player1 -> Actor A
Player2 -> Actor B

does not imply

Player1 -> Host Provisioning A
Player2 -> Host Provisioning B
```

Likewise, Targeted Join does not make Host Provisioning a per-Slot decision.

Mixed/per-Slot Host Provisioning remains deferred until a concrete game
requirement demonstrates truly different Host ownership/provisioning semantics
between Slots.

## Actor Resolution

Actor Resolution remains independent from Host Provisioning and Slot allocation:

```text
Resolve Configured Default
or
Leave Unresolved
```

Configured default reuse references existing Slot/Actor definitions. It does not
duplicate Actor data into another schema.

### Resolve Configured Default

The configured default is Actor-selection intent for that Slot.

It is applied through the canonical Actor-selection operation after the Slot is
Joined.

It is not encoded into Host Provisioning or Join.

### Leave Unresolved

`Leave Unresolved` deliberately permits:

```text
Player joins
Slot becomes Joined
Selected Actor remains unresolved
        ↓
consumer later requests explicit Actor selection
```

This supports games that choose an Actor after admission without requiring the
Framework to own a character-selection UI or roster system.

### Explicit Actor Selection

A bounded explicit selection command may target one exact Joined Slot with one
`ActorProfile`.

That operation changes live Session state, not this Profile.

Therefore:

```text
PlayerSessionProfile
  never becomes mutable current Actor state
```

Actor selection remains separate from physical Actor preparation/materialization.

A currently prepared Actor cannot be silently changed by direct selection.
Physical Actor hot-swap requires a separate accepted product operation.

## Runtime authority

The Profile resolves once at Session creation into immutable effective
configuration evidence. The created Session then owns mutable runtime state.

```text
PlayerSessionProfile
  -> resolve once
  -> immutable effective configuration
  -> Session runtime authority
```

Mutable runtime decisions include:

```text
Joining state
Slot occupancy/assignment
selected Actor per Joined Slot
selection revisions
```

Later Profile edits, Route changes or Activity changes do not silently reapply
initial configuration.

Targeted Join and explicit Actor Selection operate on the live Session and never
mutate/reapply `PlayerSessionProfile`.

IF-ADR-019 also makes Session persistence runtime semantics rather than initial authored
policy:

```text
Joined Logical Player
  persists for the Session

Activity participation/representation
  may appear, disappear and reproject without reapplying PlayerSessionProfile
```

No `Persistent Player` field or per-Player persistence mode is added to
`PlayerSessionProfile`.

Provisioning-specific physical lifetime remains derived from the accepted Host
Provisioning mode:

```text
Manager Provisioned
  admitted technical Host/PlayerInput becomes Session-owned

Scene Provided
  physical Host/Actor remains consumer-scene-owned
  Session association is contextual and may be reprojected in a later Activity
```

## Manager-Provisioned Input System bridge

For Manager-Provisioned Player:

```text
serialized PlayerInputManager player limit
=
PlayerSessionProfile.SupportedSlotCount
```

This is derived technical configuration, not domain Capacity. Runtime fails
explicitly on divergence; it does not reinterpret the Session.

Framework `PlayerSlotId` remains distinct from Unity Input System
`PlayerInput.playerIndex`.

A targeted Framework Slot request must not force the domain Slot identity to equal
Unity's player index.

Scene-Provided does not use this bridge as a provisioning requirement and may
continue to author exact Slot identity directly.

## Rejected scope

- separate provisioning Profile asset;
- Capacity fields/commands;
- per-Slot Host Provisioning overrides;
- interpreting per-Slot Actor choice as per-Slot Host Provisioning;
- live Profile-to-Session synchronization;
- parallel Slot schema;
- generic allocation strategy beyond bounded untargeted/targeted Join;
- targeted Join fallback to another Slot;
- combined Slot + Actor + materialization request;
- generic character-selection flow, roster, unlock or store system;
- consumer Actor preparation/materialization authority;
- direct selection hot-swapping a prepared Actor;
- per-Player or per-Profile physical persistence mode; canonical Joined Player Session lifetime is defined by IF-ADR-019.

## Integration boundary

Package/QA/FIRSTGAME integration state is tracked outside the ADR. Real
FIRSTGAME integration is part of proving the supported feature in a real product.

R6 requires no implementation while per-Slot Host Provisioning remains deferred.

R7/R8 implementation should prove:

```text
untargeted Join remains compatible
targeted exact-Slot Join works without fallback
explicit Actor selection works for a Joined unresolved Slot
configured default selection remains available
Actor selection and Host Provisioning remain independent
prepared Actor blocks direct selection mutation
```

UX friction discovered there is qualitative and may justify optional authoring
improvements; it is not a separate completion score.
