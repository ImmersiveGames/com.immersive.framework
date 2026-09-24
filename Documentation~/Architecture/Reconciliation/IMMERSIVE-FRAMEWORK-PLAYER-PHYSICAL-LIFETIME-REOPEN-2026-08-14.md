# Immersive Framework — Player Physical Lifetime Reopen and Documentation Reconciliation

Status: **Architecture Freeze Accepted / Implementation Reconciliation Open**  
Date: **2026-08-14**  
Repository baseline reviewed: `661968297a0436c5bcafaa197b86bc486fc7ed4d` (`ADR21Build`)

## Purpose

This record freezes a corrected interpretation of Player physical lifetime and reopens
the affected ADR implementation/certification boundaries.

The correction was triggered by a product-intent mismatch:

```text
Intended:
  Activity controls whether/how the Player is represented.

Previously implemented/interpreted:
  Activity owns physical Actor occurrence and may destroy/recreate it.
```

The second statement is not the intended product model.

## Frozen architecture

### Session

After successful admission, Session owns:

```text
Joined Logical Player occurrence
Slot occupancy
Actor selection intent
admitted physical Player representation
physical Host / PlayerInput where applicable
physical Actor / visual hierarchy
```

### Activity

Activity owns:

```text
participation projection
active/inactive representation
gameplay admission
Camera requests
readiness contribution
interaction/contextual bindings
Activity-local references
Activity representation occurrence
```

### Core invariant

```text
Activity representation lifetime
!=
physical Player lifetime
```

Activity-to-Activity transition must preserve the admitted physical Player by default.

## Physical states

```text
Joined + represented
  physical Player exists
  Activity representation active

Joined + not represented
  physical Player exists
  Activity representation absent/inactive

Leaving
  Activity context retires
  physical Player releases
  Session occurrence ends

Vacant
  no current Player occurrence
```

## Provisioning convergence

### Manager-Provisioned

```text
Framework supplies candidate
-> successful admission
-> Session owns
```

### Scene-Provided

```text
consumer scene supplies candidate
-> validate/adopt
-> successful admission
-> Session owns
```

The provisioning modes differ in acquisition source, not post-admission lifetime.

Scene-Provided runtime ownership transfer occurs only after successful admission.

## Scene-Provided promotion requirement

A Scene-Provided object authored inside an Activity scene must be promoted/migrated to a
Session-owned runtime scope before that scene can unload.

The semantic authority is the Session Player occurrence, not `DontDestroyOnLoad`.

A later Activity must reuse the admitted physical Player instead of silently replacing it
with another candidate.

## Activity transition

```text
A contextual authority releases
        ↓
same physical Player remains
        ↓
B contextual authority binds/activates
```

No default destroy/recreate and no re-Join.

This rule is independent from transition presentation mode.

## No-Activity state

A Route/Session may temporarily have no Activity representation for a Joined Player.

The Player may be visually absent by deactivation while the physical object continues to
exist under Session ownership.

## Initial Placement impact

The former ADR-021 assumption that an incoming Activity normally receives a new physical
Actor occurrence is superseded.

Default continuity is:

```text
same physical Player
same current spatial pose
new Activity contextual occurrence
```

A new placement is applied only when explicit spatial-start/placement intent requires it.

## Leave impact

IF-ADR-020 keeps occurrence-safe Leave semantics but changes Scene-Provided resource
ownership after successful adoption.

Both provisioning origins must release Session-owned admitted physical resources on Leave.

## Documentation status

Reconciled/reopened files in this package:

```text
IF-ADR-001
IF-ADR-003
IF-ADR-007
IF-ADR-012
IF-ADR-015
IF-ADR-016
IF-ADR-019
IF-ADR-020
IF-ADR-021
Architecture README
Framework Tracker
Player Usage Guide
```

Historical dated ADR-019/020 certification records are intentionally not rewritten.
Their evidence remains historically valid for the former contract, but they no longer
certify the revised physical lifetime boundary.

## Certification disposition

### Historical evidence retained

ADR-019 previous proof still supports:

```text
Activity exit != Leave
Joined Slot persists
Manager Host survives Activity exit
Scene-Provided logical reprojection without re-Join
```

ADR020-H still supports:

```text
occurrence-safe Leave
Joining Closed does not block Leave
Slot terminal commit
readiness invalidation
rejoin and stale Leave safety
Manager release timing
```

### Recertification required

New focused proof must establish:

```text
Activity A -> B preserves exact physical Player identity
no Actor destroy/recreate during normal Activity transition
no-Activity representation deactivates without physical destruction
Scene-Provided adoption transfers runtime ownership to Session
supplying scene unload does not destroy adopted Player
later Activity reuses same adopted Player
Leave releases adopted Scene-Provided physical Player
new Activity readiness evidence is fresh despite same physical identity
Initial Placement does not teleport ordinary continuous Activity transition
```

## Implementation status

The package implementation at reviewed HEAD is not treated as conforming to this revised
boundary until code reconciliation and focused QA complete.

ADR-019 and ADR-020 closures are reopened for the affected physical-lifetime scope.

ADR-021 remains non-certifiable until reconciled with this freeze.
