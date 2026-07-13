# P3F.1 — Session Slot Runtime Foundation

Status: Implementation candidate / requires Unity compile and QA smoke  
Date: 2026-07-13  
Baseline: P3E accepted runtime authority audit  
Type: Runtime foundation + technical QA handoff

## Objective

Implement the isolated Session participation state machine before composing it into `FrameworkRuntimeHost`.

## Product/runtime surface

```text
PlayerParticipationRuntimeContext
  ordered PlayerSlotProfile roster
  mutable Slot allocation state
  dynamic join capacity
  explicit join window
  atomic reservation token
  reservation release
  mark Joined
  immutable snapshots
  typed operation results
```

## Initial rules

```text
GameApplication/Profile order is preserved.
All valid configured Slots begin Available.
Dynamic capacity controls concurrent Reserved + Joined + Leaving Slots.
Joining must be explicitly open before reservation.
Capacity reduction is non-destructive.
Reservation tokens are Session-context and Slot-revision scoped.
Foreign or stale reservation tokens are rejected.
Profiles are never mutated.
```

## Files created

```text
Runtime/PlayerParticipation/Contracts/PlayerSlotAllocationState.cs
Runtime/PlayerParticipation/Contracts/PlayerParticipationOperationStatus.cs
Runtime/PlayerParticipation/Contracts/PlayerSlotReservationToken.cs
Runtime/PlayerParticipation/Contracts/PlayerSlotRuntimeSnapshot.cs
Runtime/PlayerParticipation/Contracts/PlayerParticipationSnapshot.cs
Runtime/PlayerParticipation/Contracts/PlayerParticipationOperationResult.cs
Runtime/PlayerParticipation/Runtime/PlayerParticipationRuntimeContext.cs
Documentation~/Product/P3F1-SESSION-SLOT-RUNTIME-FOUNDATION-MANIFEST.md
```

## Out of scope

```text
FrameworkRuntimeHost composition
PlayerInputManager
LocalPlayerJoinRequest/Result
Actor selection
Actor materialization
Activity projection/admission
leave destruction or reconnect
FIRSTGAME
```

## Acceptance

```text
ordered first-available reservation
same Slot cannot be reserved twice
joining closed blocks reservation
capacity includes Reserved and Joined
capacity reduction does not evict
capacity increase permits later reservation
release returns Slot to Available
mark Joined keeps Slot allocated
foreign/stale token rejected
invalid/duplicate Profiles rejected
snapshots preserve order and do not expose mutable collections
```

## Next microcut

```text
P3F.2
  compose one PlayerParticipationRuntimeContext in FrameworkRuntimeHost
  initialize from GameApplication.LocalPlayerSlots
  expose narrow internal snapshot/operation forwarding
  add boot/runtime diagnostics
```
