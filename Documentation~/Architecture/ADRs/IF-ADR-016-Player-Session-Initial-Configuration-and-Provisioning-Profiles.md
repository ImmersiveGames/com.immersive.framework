# IF-ADR-016 — Player Session Initial Configuration

Status: Accepted  
Last updated: 2026-08-09  
Implementation status: **Implemented and technically QA-certified for the accepted current scope**  
Supersedes: the prior ADR-016 provisioning-profile, Capacity and per-Slot override model  
Superseded by: none  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-012, IF-ADR-015

## Context

Player Session needs one authorable source for its initial intent without turning authored Profiles into live Session state. The former model split that intent between `PlayerSessionProfile` and a separate provisioning Profile, added a second Capacity limit, and allowed per-Slot Host Provisioning overrides. Those surfaces express technical decomposition rather than authoring intent and make one Session harder to understand and validate.

## Decision

`PlayerSessionProfile` is the only Profile required to configure the initial intent of a Player Session. Host Provisioning and Actor Resolution remain separate technical concepts, but are direct configuration of that Profile; they are not separate ScriptableObjects.

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

An application may supply a default `PlayerSessionProfile`. An explicit creation-time Profile replaces that default as one complete source; there is no field merge and no fallback from an invalid explicit source. A composition that does not enable Player Session is a valid absence. A composition that enables it but cannot resolve a valid Profile fails explicitly.

## Supported Slots and Joining

`Supported Slots` is the complete structural universe for the Session. It is an ordered collection of existing `PlayerSlotProfile` definitions and remains the source of stable `PlayerSlotId` identity and normal Join order.

```text
Supported Slots
├── player.1 — vacant
├── player.2 — vacant
├── player.3 — vacant
└── player.4 — vacant
```

Vacant Slots remain structurally supported and can be occupied by a later runtime Join.

The normal Join rule is:

```text
Joining Open
+ first vacant Supported Slot in authored order
```

When no Supported Slot is vacant, the request is rejected explicitly as no available Slot. There is no independent `Initial Capacity`, `Current Capacity` or `Dynamic Capacity`, and there is no runtime `SetCapacity`/`SetDynamicCapacity` operation.

This ADR does not introduce an allocation-strategy abstraction.

## Host Provisioning and Actor Resolution

Host Provisioning is one initial Session decision:

```text
Scene Provided
or
Manager Provisioned
```

It applies uniformly to every Supported Slot. Per-Slot Host Provisioning overrides and mixed Scene-Provided/Manager-Provisioned Sessions are rejected by the current model. A heterogeneous Session requires a future ADR based on a concrete game requirement.

Actor Resolution remains independent:

```text
Resolve Configured Default
or
Leave Unresolved
```

Resolving the configured default reuses referenced Slot/Actor definitions; it does not duplicate Actor data in a new Slot schema. `Leave Unresolved` is a valid initial state for a separately approved selection flow.

## Resolution and runtime authority

At Session creation the Profile resolves once into immutable effective configuration evidence. The created Session then owns mutable runtime state.

```text
PlayerSessionProfile
  → resolve once at Session creation
  → immutable effective configuration evidence
  → Session runtime authority
```

Editing the source Profile later does not mutate the current Session. Route or Activity does not silently reapply or replace it.

The following remain invariant:

- Player belongs to Session.
- `PlayerSlotId` is stable identity.
- Host and Actor are distinct concepts.
- Scene Provided and Manager Provisioned are distinct Session provisioning modes.
- Actor may remain unresolved.
- Joining is runtime state.
- Route and Activity do not silently mutate Session configuration.
- Session runtime is authority after initialization.

## Manager-Provisioned Input System bridge

For Manager-Provisioned Player, `PlayerInputManager` has a serialized technical player limit. The package/authoring bridge derives that limit from the Session structure:

```text
serialized PlayerInputManager player limit
=
PlayerSessionProfile.SupportedSlotCount
```

This value is materialized technical configuration, not domain Capacity. Runtime must fail explicitly if the derived bridge diverges from the initialized Session's Supported Slots; it must not silently repair or reinterpret the Session.

Scene-Provided Player does not use this bridge as part of its provisioning contract.

## Rejected scope

- A separate provisioning Profile asset.
- Capacity fields or runtime capacity commands.
- Per-Slot Host Provisioning overrides and mixed provisioning modes.
- Live synchronization from a Profile to an existing Session.
- A parallel Slot schema.
- Generic allocation strategy.
- Generic character-selection flow.
- Session-Persistent Player workflow.

## Technical certification — 2026-08-09

The accepted Session model was exercised by the canonical QAFramework Player orchestrator and completed with:

```text
session='PASS'
sceneProvided='PASS'
managerProvisioned='PASS'
actor='PASS'
publicSurface='PASS'
participation='PASS'
verdict='PLAYER QA CERTIFIED'
```

Representative current evidence:

```text
Player Participation Authoring        PASS — 7 cases
Scene-Provided route/negative matrix  PASS — 25 cases
Manager public contract               PASS — 9 cases
Manager waiting projection            PASS — 14 cases
Actor selection runtime binding       PASS — 13 cases
Player gameplay admission             PASS — 114 cases
Public Surface Q1                     PASS — 28 cases
Public Surface Q2                     PASS — 36 cases
Activity Session Projection           PASS — 30 cases
```

The Manager-Provisioned fixture certified:

```text
supportedSlots='2'
maxPlayers='2'
```

and the Scene-Provided fixture independently certified:

```text
hostProvisioning='SceneProvided'
supportedSlots='2'
```

This is evidence that the canonical QA no longer depends on a mixed shared provisioning fixture or the removed Capacity model.

See `../IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md`.

## Consequences

The former documentation state `R1/R2 implemented; Unity validation pending; QA unchanged` is superseded. Technical QA is green for the tested accepted model.

Remaining work is product evidence rather than missing technical certification:

```text
FIRSTGAME manual Scene-Provided proof
FIRSTGAME manual Manager-Provisioned proof
P5 authoring/tooling disposition from observed friction
```

Whether a real game needs heterogeneous Host Provisioning remains deferred. It must not be inferred from the superseded per-Slot override capability.

Technical certification does not automatically promote Experimental/preview API stability metadata.
