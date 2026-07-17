# ADR-PROD-0014 — Activity Transition Authority, Readiness and Finalization

Status: Accepted  
Date: 2026-07-17  
Package: `com.immersive.framework`  
Area: Framework Core / ActivityFlow / GameFlow  
Extends: `F04-ADR-ACTIVITY-001`, `F18-ADR-TRANSITION-001`, `ADR-PROD-0012`, `ADR-PROD-0013`

## Context

The current Activity path exposes active Activity, readiness, content lifecycle and
previous-scope cleanup, but it does not yet express their transaction boundary as
independent contracts. This permits an implementation to confuse authority with
readiness, report a post-commit failure as an ordinary request failure, or hide
retained ownership while the previous Activity is still being finalized.

This ADR freezes the contract for the next runtime cut. It does not claim that the
current runtime already follows the transaction order below.

## Decision

An Activity transition is one non-concurrent transaction with four independent
dimensions:

```text
Activity Authority
  The Activity that owns the current Activity identity after commit.

Activity Transition Phase
  The transaction step currently being executed.

Activity Readiness
  Whether the authoritative Activity is ready for gameplay.

Previous Activity Finalization
  Whether the previous Activity still retains content, handles, bindings or scope.
```

The canonical phases are:

```text
Idle
PreparingTarget
ReadyToCommit
CommittedTransitioning
PreviousExiting
TargetEntering
PreviousFinalizing
Completed
FailedBeforeCommit
CommittedNotReady
CommittedFinalizationFailed
```

`Completed`, `FailedBeforeCommit`, `CommittedNotReady` and
`CommittedFinalizationFailed` are terminal transaction results, not authority or
readiness aliases. The canonical request API continues to reject concurrent
requests while one transaction is non-terminal; no request queue is introduced.

### Commit boundary and results

The target may be prepared before commit. All pre-commit requirements must be
validated at `ReadyToCommit`. Before authority commit, a failure preserves the
previous authority and returns `FailedBeforeCommit`.

Commit atomically makes the target the Activity authority. From that instant, the
previous Activity may remain retained only for explicit finalization; it cannot
remain an alternative authority. Readiness is calculated independently and may be
false after commit.

The public result vocabulary is:

```text
FailedBeforeCommit
  Target never acquired authority.

CommittedReady
  Target acquired authority, finalization completed and target is gameplay-ready.

CommittedNotReady
  Target acquired authority, but target readiness is not satisfied.

CommittedFinalizationFailed
  Target acquired authority, but previous finalization did not complete.
```

Post-commit failures are never converted to a normal configuration failure or a
silent rollback. Diagnostics must retain the commit fact, target identity, phase,
readiness evidence, finalization status and every retained handle/owner.

There is no silent rollback at any phase: any deliberate compensation before
commit is explicit in its `FailedBeforeCommit` diagnostic, and post-commit
authority remains with the target.

### Canonical execution order

The implementation must preserve this order:

```text
1.  Create target scope.
2.  Prepare target scenes and dependencies.
3.  Validate pre-commit requirements.
4.  Reach ReadyToCommit.
5.  Commit target authority.
6.  Exit all previous scene content.
7.  Exit all previous participants.
8.  Discover/materialize remaining target requirements.
9.  Enter target participants.
10. Enter all target scene content.
11. Clear previous bindings and finalize previous scope.
12. Release previous scenes.
13. Calculate final target readiness.
14. Publish completed facts.
15. Finish as CommittedReady, CommittedNotReady or CommittedFinalizationFailed.
```

All previous `Exit` callbacks complete before the first equivalent target `Enter`.
Previous scenes remain loaded through the callbacks, required releases, binding
cleanup and scope finalization. `RuntimeContent` release remains part of previous
finalization and must expose retained handles rather than releasing scenes early.

Player, camera and input have no exception to this order. Their Activity-scoped
release belongs to previous participant exit/finalization; target input and camera
eligibility may only be published after target admission/readiness evidence. This
ADR does not change their participant ordering or provisioning contracts.

### Facts and diagnostics

Events report completed facts and must not trigger hidden mutation. The planned
fact surface is intentionally compact:

```text
ActivityTargetPrepared
ActivityAuthorityCommitted
ActivityPreviousContentExited
ActivityPreviousParticipantsExited
ActivityTargetParticipantsEntered
ActivityTargetContentEntered
ActivityPreviousFinalized
ActivityTransitionCompleted
ActivityTransitionFailedBeforeCommit
ActivityTransitionCommittedNotReady
```

`ActivityTransitionCompleted` carries the aggregated terminal result. A separate
finalization-failed fact is optional only if the aggregate result cannot provide
the required diagnostic detail; it must not duplicate an incomplete operation.

### Route Startup handoff

Route Startup may temporarily retain ownership of the previous Activity after its
handoff commit. That retention must migrate from procedural cleanup to a typed,
diagnostic handoff context containing:

```text
PreviousActivity
TargetActivity
PreviousOwner
TargetOwner
RetainedHandles
CommitReached
PreviousFinalizationStatus
```

`FinalizeRouteStartupPreviousActivityScope` is not removed by this ADR. Its
procedural shape is a migration target: the future implementation must consume
the typed handoff context and make retained ownership observable.

## Accepted scope

```text
Transaction vocabulary, commit boundary, terminal results and event semantics.
Canonical Exit/Enter/finalization order.
Explicit previous-Activity retention and Route Startup handoff direction.
```

## Rejected scope

```text
Runtime, QA or FIRSTGAME changes in this cut.
New runtime state classes, moving the current commit, participant-order inversion.
Rollback, request queue, Input changes or Local Player provisioning changes.
```

## Consequences

The future `ActivityFlow`/`GameFlow` cut must introduce typed transaction result,
phase and finalization evidence without using events as commands. It must audit
the current `ActivityFlowRuntime` event-driven content lifecycle, scoped
`RuntimeContent` release, Route Startup finalization, and Player/Camera/Input
admission publication against this order.

## Current implementation coverage

Partial and not conformant by assertion: `ActivityReadinessState` already exists
as separate readiness evidence, and `ActivityFlowRuntime.RouteStartupActivation`
already has a previous-scope finalization path. The explicit authority commit,
transaction phases, terminal result vocabulary and typed Route Startup handoff do
not yet exist as this ADR defines them.

## Pending decisions

```text
Exact public/internal type ownership for transaction snapshots and diagnostics.
Whether finalization failure needs a dedicated fact in addition to the aggregate result.
The precise pre-commit readiness predicates and cancellation policy.
The migration sequencing for the current event-driven content lifecycle.
```

## Suggested commit message

```text
ARCH-A0 — define Activity transition authority and readiness contract
```
