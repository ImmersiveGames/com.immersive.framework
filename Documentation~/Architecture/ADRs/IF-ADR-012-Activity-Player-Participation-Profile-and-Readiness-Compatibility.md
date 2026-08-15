# IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility

Status: **Accepted / Reconciled / Player QA Recertified 2026-08-15**  
Last updated: **2026-08-15**  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-010, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Reopen record: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)  
Closure record: [2026-08-15 Player Physical Lifetime Recertification](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)

## Context

Activities need reusable Player participation intent without duplicating Session rules inside each scene.

## Decision

Activity Player participation resolves into one normalized effective policy with provenance.

Runtime consumes explicit Slot/Player/Actor evidence and publishes requested/effective state plus diagnostic reasons.

Activity participation does not own or silently mutate Player Session configuration or the terminal lifetime of an admitted physical Player.

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
  owns retained physical preparation evidence

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

A later Activity does not require a new physical Player occurrence merely because its Activity representation occurrence is new.

## Failed contextual inclusion / reprojection

A target Activity may commit and become current while its Player contextual admission fails.

Valid state:

```text
Activity B = current / Active
Activity B readiness = NotReady
Activity B RuntimeContent scope = present
Player contextual admission for B = failed/absent
Session physical Player = still owned by the same Session occurrence
```

The Activity-owned RuntimeContent root is released by Activity exit/release. Player rollback must not destroy current Activity scope simply to make `RuntimeContentOwner.Activity(B)` count zero.

This state is not physical handoff and does not create a Player B occurrence.

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

## Observation rule

Activity participation evidence and Session physical evidence are different domains.

`Contextual=Absent` or failed Activity projection must not be interpreted as physical lifetime loss. Session physical truth is resolved from canonical Session/occurrence preparation evidence, not hierarchy shape or scene scan.

## Constraints

- One normalized effective participation policy is runtime input.
- Provenance remains diagnosable.
- Invalid compatibility fails explicitly.
- Activity policy is not a provisioning mode.
- Activity participation is not physical lifetime authority.
- Activity-owned RuntimeContent is not Player physical ownership.
- Session Leave remains Session authority.

## Certification

The 2026-08-15 Full Player QA completed `25/25` mandatory contracts and recertifies exclusion, inclusion/reprojection, fresh occurrence readiness, failed contextual reprojection and no physical handoff against the revised IF-ADR-019 lifetime model.
