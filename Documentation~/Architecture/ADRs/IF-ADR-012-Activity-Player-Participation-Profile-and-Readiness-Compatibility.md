# IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility

Status: **Accepted / Reconciled**  
Last updated: **2026-08-14**  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-010, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Current Player lifetime reconciliation: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)

## Context

Activities need reusable Player participation intent without duplicating Session rules
inside each scene.

## Decision

Activity Player participation resolves into one normalized effective policy with
provenance.

Runtime consumes explicit Slot/Player/Actor evidence and publishes requested/effective
state plus diagnostic reasons.

Activity participation does not own or silently mutate Player Session configuration or
the terminal lifetime of an admitted physical Player.

## Session boundary

```text
PlayerSessionProfile
  owns Supported Slots
  owns Initial Joining
  owns Host Provisioning origin policy
  owns Actor Resolution initial intent

Session runtime
  owns joined occurrence
  owns admitted physical Player after successful admission

Activity Player policy
  projects/qualifies current Session Slots
  defines participation/readiness intent
  controls contextual representation activation
  does not create/destroy Session membership
```

## Exclusion

```text
Joined Session Player
+ excluded by Activity policy
  -> Slot remains Joined
  -> valid Actor selection remains current
  -> physical Player remains Session-owned
  -> Activity representation is Absent / Inactive
```

Exclusion is not Actor destruction.

## Inclusion

```text
Joined Session Player
+ included by Activity policy
  -> Activity acquires a new contextual representation occurrence
  -> existing admitted physical Player is activated/bound as required
  -> readiness is evaluated for this Activity occurrence
```

A later Activity does not require a new physical Player occurrence merely because its
Activity representation occurrence is new.

## Readiness requirement compatibility

```text
None
JoinedSlots
SelectedActors
  -> Session-level evidence

LogicalActorsPrepared
GameplayReady
  -> Activity representation required
  -> existing physical Player may satisfy the physical existence prerequisite
  -> new contextual evidence is still required
```

## Leave-driven reconciliation

Successful IF-ADR-020 Leave invalidates the departed occurrence's readiness.

For an explicit required Slot:

```text
authored Slot projection remains
current Player occurrence absent
contribution -> WaitingForJoin / Preparing
Activity Ready -> false
```

No stale Ready, auto-Join, Slot substitution or policy weakening is allowed.

## Constraints

- One normalized effective participation policy is runtime input.
- Provenance remains diagnosable.
- Invalid compatibility fails explicitly.
- Activity policy is not a provisioning mode.
- Activity participation is not physical lifetime authority.
- Session Leave remains Session authority.
