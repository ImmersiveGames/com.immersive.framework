# P3K.2 — Effective Runtime Player Occupancy

Status: implementation delta ready for Unity compile and QA.  
Type: runtime foundation cut.

## Objective

Introduce the scoped runtime authority that confirms the effective relation between
one configured `PlayerSlotId` and one current prepared Logical Player Actor.

```text
PlayerActorPreparationSummary
-> validate current Active preparation
-> confirm effective occupancy
-> return typed occupancy token and immutable Session evidence
```

This cut does not bind input or publish camera requests.

## Runtime boundary

```text
PlayerActorPreparationRuntimeContext
  owns Logical Actor preparation and physical materialization

PlayerGameplayOccupancyRuntimeContext
  owns only effective Slot -> prepared Actor occupancy evidence

PlayerSlotOccupancy
  remains a passive authored diagnostic declaration
```

`PlayerGameplayOccupancyRuntimeContext` is plain Session-scoped C#. It is not a
MonoBehaviour, singleton, service locator, spawner or Actor lifetime authority.

## Identity guard

Every occupied record preserves:

```text
Session context id
RuntimeContent owner
PlayerSlotId
ActorProfileId
ActorId
PlayerActorPreparationToken
RuntimeContentIdentity
materialization revision
occupancy revision
```

Confirm rejects unprepared, inactive, foreign, stale or incoherent preparation
evidence.

Release requires the exact current `PlayerGameplayOccupancyToken`. Releasing an
already vacant Slot without a token is idempotent. Supplying an old token after
release is rejected.

## Files

```text
Runtime/PlayerParticipation/Contracts/
├── PlayerGameplayOccupancyResult.cs
├── PlayerGameplayOccupancySnapshot.cs
├── PlayerGameplayOccupancyState.cs
├── PlayerGameplayOccupancyStatus.cs
├── PlayerGameplayOccupancySummary.cs
└── PlayerGameplayOccupancyToken.cs

Runtime/PlayerParticipation/Runtime/
└── PlayerGameplayOccupancyRuntimeContext.cs
```

## Out of scope

```text
FrameworkRuntimeHost composition
automatic release before P3J Actor release
PlayerControlRuntimeContext binding
PlayerInput bridge or action-map activation
Gate availability
camera request publication
GameplayReady aggregation
Activity activation gate
local Player leave
```

These remain P3K.3 through P3K.5.

## Technical acceptance

```text
context initializes from the ordered P3J preparation snapshot
only configured Slots are accepted
only Active prepared Actors may become occupants
same preparation confirm is idempotent
one Slot cannot accept a second current preparation
one runtime Actor identity cannot occupy two Slots
foreign Session preparation is rejected
release is guarded by the current occupancy token
repeated release without a token is idempotent
stale token after release is rejected
two Slots occupy independently
public snapshots retain no Unity object references
PlayerSlotOccupancy remains unchanged and passive
```

## Expected QA

```text
Immersive Framework
  > QA
    > Player
      > P3K.2 Run Effective Runtime Occupancy Smoke
```

Expected:

```text
[P3K2_EFFECTIVE_RUNTIME_OCCUPANCY_SMOKE] status='Passed'
```
