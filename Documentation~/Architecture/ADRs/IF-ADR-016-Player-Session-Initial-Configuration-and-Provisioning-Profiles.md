# IF-ADR-016 — Player Session Initial Configuration

Status: **Accepted**  
Last updated: 2026-08-09  
Supersedes: former separate provisioning-Profile, Capacity and per-Slot override model  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-012, IF-ADR-015

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Player Session needs one authorable source for initial intent without turning
Profiles into live Session state. The former model split intent across multiple
Profiles, introduced a second Capacity limit and allowed per-Slot Host
Provisioning overrides.

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

`Supported Slots` is the complete structural Slot universe and authored Join
order.

Normal Join:

```text
Joining Open
+ first vacant Supported Slot in authored order
```

No available Slot produces explicit rejection.

There is no independent Initial/Current/Dynamic Capacity and no runtime
SetCapacity/SetDynamicCapacity command.

## Host Provisioning

Host Provisioning is one Session decision:

```text
Scene Provided
or
Manager Provisioned
```

It applies uniformly to all Supported Slots. Mixed/per-Slot provisioning is not
part of the current model and requires a future ADR based on a concrete game
requirement.

## Actor Resolution

Actor Resolution remains independent:

```text
Resolve Configured Default
or
Leave Unresolved
```

Configured default reuse references existing Slot/Actor definitions. It does not
duplicate Actor data into another schema.

## Runtime authority

The Profile resolves once at Session creation into immutable effective
configuration evidence. The created Session then owns mutable runtime state.

```text
PlayerSessionProfile
  -> resolve once
  -> immutable effective configuration
  -> Session runtime authority
```

Later Profile edits, Route changes or Activity changes do not silently reapply
initial configuration.

## Manager-Provisioned Input System bridge

For Manager-Provisioned Player:

```text
serialized PlayerInputManager player limit
=
PlayerSessionProfile.SupportedSlotCount
```

This is derived technical configuration, not domain Capacity. Runtime fails
explicitly on divergence; it does not reinterpret the Session.

Scene-Provided does not use this bridge as a provisioning requirement.

## Rejected scope

- separate provisioning Profile asset;
- Capacity fields/commands;
- per-Slot Host Provisioning overrides;
- live Profile-to-Session synchronization;
- parallel Slot schema;
- generic allocation strategy;
- generic character-selection flow;
- Session-Persistent Player workflow.

## Integration boundary

Package/QA/FIRSTGAME integration state is tracked outside the ADR. Real
FIRSTGAME integration is part of proving the supported feature in a real product.
UX friction discovered there is qualitative and may justify optional authoring
improvements; it is not a separate completion score.
