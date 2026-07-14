# P3K.3 — Typed Control and Input Binding

Status: **implementation delta ready for Unity compile and QA**
Type: **runtime contracts and transactional Unity Input binding cut**

## Correction to P3K.1

The code-level conformance check performed before this cut confirmed that the accepted P2 product shape does **not** contain `PlayerControlRuntimeContext`.

The operative P2 runtime surface is:

```text
explicit PlayerInput
PlayerActorDeclaration PlayerInput evidence
UnityPlayerInputGateAdapter
Pause / Transition Gate snapshots
movement remains game-owned
```

Therefore P3K.3 does not attempt to reuse or recreate the abandoned F52 binding chain. It introduces one narrow Session context for the new contextual-Actor occupancy flow.

## Objective

```text
P3J Active prepared Logical Actor
+ P3K.2 effective occupancy
+ stable Local Player Host PlayerInput
+ matching PlayerActorDeclaration
+ explicit UnityPlayerInputGateAdapter
-> typed gameplay input binding
```

## Authority

`PlayerGameplayInputBindingRuntimeContext` owns only:

```text
current per-Slot Actor-to-PlayerInput binding evidence
live validation against the P3K.2 occupancy authority
gameplay action-map activation and previous-map restoration
Gate-derived Allowed / BlockedByGate availability
functional binding token
rollback and reverse release diagnostics
```

It does not:

```text
select or materialize Actors
change occupancy
read actions
move gameplay objects
publish cameras
execute PlayerComposer
use F52 PlayerBinding targets
admit an Activity
publish GameplayReady
```

## Identity guard

Every bind requires coherent current evidence:

```text
SessionContextId
PlayerSlotId
ActorProfileId
ActorId
RuntimeContent owner and identity
PlayerActorPreparationToken
PlayerGameplayOccupancyToken
stable LocalPlayerHostAuthoring
stable-host PlayerInput
prepared PlayerActorDeclaration
```

Names and hierarchy labels remain diagnostics only.

## Gate boundary

`UnityPlayerInputGateAdapter` remains the existing availability adapter. P3K.3 validates that it targets the exact stable-host `PlayerInput` and uses its configured gameplay action map.

Binding state and availability are distinct:

```text
Binding: Unbound | Bound | ReleaseFailed
Availability: Unknown | Allowed | BlockedByGate
```

Pause/Transition Gate blockers do not release Slot, Actor, occupancy or binding identity.

## Release order

```text
1. clear Gate-owned temporary block state
2. restore the action map that was active before the binding
3. clear physical binding references
4. publish Unbound evidence
```

A later Gate release cannot reactivate an already released gameplay binding.

## Files

```text
Runtime/PlayerParticipation/Contracts/
  PlayerGameplayInputBindingState.cs
  PlayerGameplayInputAvailability.cs
  PlayerGameplayInputBindingStatus.cs
  PlayerGameplayInputBindingToken.cs
  PlayerGameplayInputBindingSummary.cs
  PlayerGameplayInputBindingSnapshot.cs
  PlayerGameplayInputBindingResult.cs

Runtime/PlayerParticipation/Runtime/
  PlayerGameplayInputBindingRuntimeContext.cs
```

## Technical acceptance

```text
context initializes from the live P3K.2 occupancy authority
vacant occupancy cannot bind
released or superseded occupancy is rejected even when an older snapshot remains structurally valid
host Slot and PlayerInput must match
ActorId and PlayerInput evidence must match
configured action map must exist
Gate adapter must target the same PlayerInput
bind is idempotent
one PlayerInput cannot bind two Slots
Gate availability refresh does not replace binding identity
release is exact-token guarded and idempotent
previous action map is restored
stale token after rebind is rejected
no public snapshot retains Unity object references
```

## Next

P3K.4 adds prepared-Player camera eligibility. P3K.5 composes occupancy, input and camera evidence into `GameplayReady` and reverse release.
