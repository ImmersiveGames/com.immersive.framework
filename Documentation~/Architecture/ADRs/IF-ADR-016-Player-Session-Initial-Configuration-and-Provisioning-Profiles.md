# IF-ADR-016 — Player Session Initial Configuration

Status: Accepted  
Last updated: 2026-08-09  
Supersedes: the prior ADR-016 provisioning-profile, Capacity and per-Slot override model  
Superseded by: none  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-012, IF-ADR-015

## Context

Player Session needs one authorable source for its initial intent without
turning authored Profiles into live Session state. The former model split that
intent between `PlayerSessionProfile` and a separate provisioning Profile, added a
second Capacity limit, and allowed per-Slot Host Provisioning overrides. Those
surfaces express technical decomposition rather than authoring intent and make
one Session harder to understand and validate.

## Decision

`PlayerSessionProfile` is the only Profile required to configure the initial
intent of a Player Session. Host Provisioning and Actor Resolution remain
separate technical concepts, but are direct configuration of that Profile; they
are not separate ScriptableObjects.

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

An application may supply a default `PlayerSessionProfile`. An explicit
creation-time profile replaces that default as one complete source; there is no
field merge and no fallback from an invalid explicit source. A composition that
does not enable Player Session is a valid absence. A composition that enables
it but cannot resolve a valid profile fails explicitly.

## Accepted scope

### Supported Slots and Joining

`Supported Slots` is the complete structural universe for the Session. It is
an ordered collection of existing `PlayerSlotProfile` definitions and remains
the source of stable `PlayerSlotId` identity and normal Join order.

```text
Supported Slots
├── player.1 — vacant
├── player.2 — vacant
├── player.3 — vacant
└── player.4 — vacant
```

Vacant Slots remain structurally supported and can be occupied by a later
runtime Join. The normal Join rule selects the first vacant Supported Slot in
authored order. A Join is accepted only when both conditions are true:

```text
Joining Open
+ a vacant Supported Slot exists
```

There is no independent `Initial Capacity`, `Current Capacity`, or `Dynamic
Capacity`. There is no runtime `SetCapacity`/`SetDynamicCapacity` operation.
When no Supported Slot is vacant, the Session has no structural capacity for a
new Player. This ADR does not introduce an allocation-strategy abstraction.

### Host Provisioning and Actor Resolution

Host Provisioning is one initial Session decision:

```text
Scene Provided
or
Manager Provisioned
```

It applies uniformly to every Supported Slot. A per-Slot Host Provisioning
override type and every equivalent override are rejected. A mixed
Scene-Provided / Manager-Provisioned Session is outside this model and requires
a future ADR based on a concrete game requirement.

Actor Resolution remains independent of Host Provisioning:

```text
Resolve Configured Default
or
Leave Unresolved
```

Resolving the configured default reuses the referenced Slot/Actor definitions;
it does not duplicate Actor data in a new Slot schema. `Leave Unresolved` is a
valid initial state for a separately approved selection flow.

### Resolution and runtime authority

At Session creation the Profile resolves once into effective configuration
evidence. The created Session then owns its mutable runtime state. Editing the
Profile later does not mutate the current Session, and Route or Activity does
not silently reapply it.

```text
PlayerSessionProfile
  → resolve once at Session creation
  → immutable effective configuration evidence
  → Session runtime authority
```

The following remain valid and are not reopened by this decision:

- Player belongs to Session.
- `PlayerSlotId` is stable identity.
- Host and Actor are distinct concepts.
- Scene Provided and Manager Provisioned are distinct models.
- Actor may remain unresolved.
- Joining is runtime state.
- Route and Activity do not silently mutate Session.
- Session runtime is authority after initialization.

## Rejected scope

- A separate provisioning Profile asset.
- Capacity fields or runtime capacity commands.
- Per-Slot Host Provisioning overrides and mixed provisioning modes.
- Live synchronization from a Profile to an existing Session.
- A parallel Slot schema, generic allocation strategy, generic character
  selection flow, or Session-Persistent Player workflow.

## Consequences

R1/R2 consolidate authoring, validation, resolution, Player Session Inspector,
runtime admission and consumer surfaces without a compatibility rail. Supported
Slots are the only Session limit. Required configuration remains fail-fast and
no Host Provisioning fallback is allowed.

## Current implementation coverage

R1/R2 implement the consolidated Profile, pure resolver, Player Session
Inspector and Slot-based admission. Effective configuration, runtime snapshots,
commands and diagnostics carry no separate Capacity. Unity compile/import
validation is pending. QA and FIRSTGAME are unchanged.

## Pending decisions

Whether a real game needs heterogeneous Host Provisioning is deferred. It must
not be inferred from the superseded per-Slot override capability.
