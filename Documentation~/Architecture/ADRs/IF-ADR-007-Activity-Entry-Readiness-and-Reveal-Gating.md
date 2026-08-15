# IF-ADR-007 — Activity Entry Readiness and Reveal Gating

Status: **Accepted / Reconciled / Player Boundary Recertified 2026-08-15**  
Last updated: **2026-08-15**  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-006, IF-ADR-009, IF-ADR-011, IF-ADR-012, IF-ADR-019, IF-ADR-021  
Reopen record: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)  
Closure record: [2026-08-15 Player Physical Lifetime Recertification](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)

## Context

An Activity may have loaded content while required participants, Actor context, adapters or local visibility remain preparing.

Readiness must distinguish Session truth, physical Player existence and current Activity representation.

## Decision

Activity entry uses:

```text
ObserveOnly
WaitVisible
WaitCovered
```

Readiness is occurrence-scoped and aggregates required/optional contribution evidence. Preparing, Ready, terminal failure, invalidation, cancellation and supersession remain distinct states.

Loading/Transition may wait on readiness but does not own it.

## Player readiness boundary

```text
None
JoinedSlots
SelectedActors
  -> Session evidence only
  -> current Activity representation not required

LogicalActorsPrepared
GameplayReady
  -> current Activity representation required
  -> current admitted physical Player may already exist
  -> Activity must project/activate/bind it for this occurrence
```

For the representation-required levels, "representation required" no longer means "new physical Actor must be materialized for this Activity."

It means:

```text
current Activity has valid occurrence-correlated contextual authority
over the admitted physical Player
```

A Player may therefore be:

```text
Joined = true
Physical Player exists = true
Activity representation = absent/inactive
```

without violating Session truth.

## Activity transition

When Activity A transitions to Activity B:

```text
retire A readiness evidence
retire A gameplay/camera/context bindings
preserve admitted physical Player
create B representation occurrence
activate/bind existing Player as required
evaluate B readiness
```

Readiness evidence from A cannot satisfy B.

## Commit and readiness are separate truths

A target may be committed/current without becoming Ready.

Canonical failed startup example:

```text
Route Request = Succeeded
current Activity = target Activity
ActivityState = Active
ActivityReadiness = NotReady
ActivityTransition = CommittedNotReady
blockingIssues > 0
```

`Route Request = Succeeded` means the Route navigation committed. It is not shorthand for `startup Activity = Ready`.

If the current Activity owns an Activity-scoped RuntimeContent root, that root remains legitimate until Activity exit/release even when a required Player contextual admission fails. Player rollback must not destroy the current Activity scope merely to force a zero-content observation.

## WaitCovered

`WaitCovered` retains destination presentation and unsafe gameplay capabilities until the captured occurrence reaches Ready.

No timeout, fake readiness, hidden Actor recreation or policy weakening is introduced.

A committed destination that ends `NotReady` remains explicit failure/not-ready state. Recovery, release or later navigation must act on that current Activity truth rather than pretending the commit did not happen.

## Constraints

- WaitCovered never reveals before Ready.
- WaitVisible permits visible preparation while unsafe capabilities remain gated.
- ObserveOnly does not become an accidental wait.
- Stale/foreign occurrences cannot satisfy the active occurrence.
- Required failure remains blocking and diagnostic.
- Optional participants do not silently become required.
- Validation does not auto-change participation or Joining.
- Readiness never becomes physical Player lifetime authority.
- Route/navigation success must not be interpreted as implicit Activity readiness success.
- Activity-owned RuntimeContent retention is not Player physical handoff.

## Certification

The Player interaction with this ADR is recertified by the 2026-08-15 Full Player QA `25/25` terminal result, including public WaitCovered preparation, failed first Scene adoption, failed contextual reprojection and no-physical-handoff negative paths.
