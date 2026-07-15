# P3 ADR — Canonical Player Lane

Status: Accepted
Last updated: 2026-07-15
Supersedes: F45 passive Slot identity and F49/F51/F52 passive binding decisions
Superseded by: none

## Context

The previous passive topology duplicated authority already owned by the P3
Session participation, join, Actor preparation and gameplay admission chain.

## Decision

P3 is the only canonical lane:

```text
profile -> participation -> join -> host -> Actor -> preparation -> occupancy
-> input -> camera -> admission -> Activity
```

The join transaction assigns `PlayerSlotId` to `LocalPlayerHostAuthoring`.
Player prefabs do not contain Slot declarations. Pause/InputMode requires the
P3 `LocalPlayerProvisioningAuthoring` surface.

## Accepted scope

- preserve `PlayerSlotId`, `PlayerSlotProfile`, allocation snapshot/state/token;
- preserve Actor selection, materialization, preparation and gameplay stages;
- validate provisioning through typed issues shared by runtime and Editor;
- migrate FIRSTGAME only after Framework and QA pass the clean P3 gate.

## Rejected scope

- F49/F51/F52 passive topology and binding modules;
- `PlayerSlotDeclaration`, `PlayerSlotOccupancy` and passive Slot sets;
- `SessionPlayerInputManagerDeclaration` and manager evidence wrappers;
- compatibility aliases, shims, fallback discovery or pre-authored Slot identity.

## Consequences

The removals are intentionally destructive. A failed H2A/H2B/H2C gate is fixed
inside that cut; removed APIs are never restored as rollback. FIRSTGAME may not
compile until its dedicated migration after H5.

## Current implementation coverage

H0-H4 are implemented in Framework/QA source. H5 import, compile and smokes are
manual gates and are not yet claimed as passed. H6 is therefore pending.

## Pending decisions

None for the Framework boundary. FIRSTGAME content cleanup is execution work.
