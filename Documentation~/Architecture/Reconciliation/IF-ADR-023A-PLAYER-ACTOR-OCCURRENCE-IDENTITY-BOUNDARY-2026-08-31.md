# IF-ADR-023A — Player Actor Occurrence Identity Boundary — 2026-08-31

Status: **Reconciled identity boundary; Scene-Provided authored-composition implementation complete**

## Scope

This reconciliation clarifies the occurrence-identity boundary after IF-ADR-023.
It does not change the Scene-Provided target contract or prescribe a runtime
resolver API.

## Identity contract

The generic reusable Player Actor prefab keeps an empty authored occurrence ID:

```text
PlayerActorRuntimeHost
├── PlayerActorDeclaration
│   └── ActorId = EMPTY
└── PresentationMount
```

The physical preparation/adoption transaction establishes the runtime occurrence
identity. Before that boundary, a typed `ActorId` is unavailable; after it, the
typed identity is valid. `ActorProfileId`, `PlayerSlotId` and occurrence `ActorId`
are separate authorities.

```text
AUTHORED / UNPREPARED
  authored ActorId = empty

→ physical preparation/adoption establishes runtime occurrence identity

IDENTITY ESTABLISHED / PREPARING
  typed ActorId is valid

→ commit

PREPARED / COMMITTED
  runtime occurrence identity may be consumed
```

No generic Player Actor prefab receives a persistent ID, and typed identity rules
remain strict.

## Scene-Provided target contract

Scene-Provided is pre-authored physical composition, validated in authoring,
deterministically resolved at runtime, then adopted. Identity establishment happens
during the adoption/preparation transaction; it is not evidence that an Editor
Apply / Rebuild operation previously ran.

Route spatial entry remains Slot/Transform-based and does not require a
pre-existing Player Actor occurrence identity.

## Historical evidence

The 2026-08-31 FIRSTGAME run observed `LogicalActorsPrepared` and `GameplayReady`
with one projected, selected and prepared Player and zero failures. It proves the
then-executed runtime boundary only; it is not retroactively reclassified as proof
that derived Scene-Provided evidence has already been removed.

## Current implementation coverage

The source resolves the physical Scene-Provided composition transiently before
admission and adoption. Derived evidence, runtime validation of it and the
`ScenePlayerActorPresentationEvidence` type are removed without introducing a
fallback or persisting authored occurrence IDs.

## Deferred decisions

- No occurrence-identity boundary decision remains pending in this cut.
