# IF-ADR-003 — Player Participation and Actor Lifecycle

Status: **Accepted / Reconciled**  
Last updated: **2026-08-14**  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Current Player lifetime reconciliation: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)

## Context

A Logical Player is a Session participant while Activity participation is contextual
gameplay authority.

The framework must keep these decisions distinct:

```text
Host Provisioning
Slot Assignment
Session Join
Actor Selection
physical Player acquisition/adoption
Activity projection / activation
readiness
gameplay admission
Session Player Leave
```

The former interpretation incorrectly treated Activity ownership of presentation as
ownership of the physical Actor lifetime.

## Decision

Player participation is Session-scoped and keyed by typed Slot identity.

```text
Session
  Slot configuration
  Joining / admission
  Logical Player occurrence
  Actor selection
  admitted physical Player representation

Activity
  participation projection
  representation activation
  readiness contribution
  gameplay / input / camera authority
  contextual bindings
```

Scene-Provided and Manager-Provisioned remain peer provisioning modes. They converge on
the same Session/Slot/Actor authority after successful admission.

## Physical Player versus Activity representation

The admitted physical Player occurrence and an Activity representation occurrence are
different lifetimes.

```text
Physical Player occurrence
  Session-owned after successful admission

Activity representation occurrence
  Activity-scoped
```

Therefore:

```text
Activity A exits
  -> release A readiness/gameplay/camera/context
  -> do not implicitly destroy admitted physical Player

Activity B enters
  -> bind/project the existing admitted physical Player
  -> begin new Activity representation occurrence
  -> do not re-Join
  -> do not implicitly recreate physical Player
```

An Activity may exclude a Joined Player. In that case:

```text
Logical Player = Joined
Physical Player = Exists
Activity representation = Absent / Inactive
```

## Provisioning

### Manager-Provisioned

```text
Framework creates candidate Host / PlayerInput / Actor composition
        ↓
successful admission
        ↓
Session owns admitted physical Player representation
```

### Scene-Provided

```text
consumer scene authors candidate Host / PlayerInput / Actor composition
        ↓
Framework validates/adopts exact candidate
        ↓
successful admission
        ↓
runtime ownership transfers to Session Player occurrence
```

A failed Scene-Provided admission does not transfer ownership.

After successful adoption, unloading the supplying Activity scene must not implicitly
destroy the admitted Player. The implementation must move/attach the admitted composition
to the canonical Session-owned runtime scope before the supplying scene can invalidate it.

## Slot Join and assignment

The Session remains the authority over Slot allocation and assignment.

```text
Untargeted Join
  -> first eligible vacant Supported Slot in authored order

Targeted Join
  -> exact requested Supported Slot when eligible
```

Targeted Join has no fallback to another Slot. `PlayerSlotId` is domain identity and is
not `PlayerInput.playerIndex`.

## Actor selection

Actor selection is Session-scoped mutable intent for one exact Joined Slot.

Direct Actor selection mutation is not an implicit physical hot-swap. Replacing a
currently prepared/admitted physical Actor requires a separate explicit operation.

Selection remains revision-aware and stale mutation rejects.

## Activity readiness boundary

An Activity may project a required Slot before it joins.

```text
WaitingForJoin / Preparing
```

is valid current evidence.

Activity readiness never becomes Session membership or physical lifetime authority.

## Session Player Leave

IF-ADR-020 owns the explicit terminal operation.

```text
Activity release
  !=
Session Player Leave

Session Player Leave
  -> retires current Activity context when present
  -> releases admitted physical Player resources owned by occurrence
  -> ends Session Player occurrence
  -> Slot becomes Vacant / Available
```

## Rejected behavior

- Activity exit destroying/recreating the admitted physical Player by default.
- Treating Activity representation absence as physical Player absence.
- Re-Join when moving an already Joined Player between Activities.
- Scene unload silently ending an adopted Scene-Provided Player.
- Manager/Scene provisioning modes having divergent post-admission lifetime semantics.
- Consumer direct Slot mutation.
- Consumer direct materialization/reconcile authority.
- `playerIndex` as Slot identity.
- Silent fallback.
- Global Player manager/service locator.
