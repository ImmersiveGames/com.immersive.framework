# ARCH-A2 — Activity Transition Runtime Conformance

Status: Implementation candidate / pending package compile and QA
Date: 2026-07-17
Package: `com.immersive.framework`
Type: Technical runtime architecture

## Objective

Implement the first runtime conformance cut for `ADR-PROD-0014` so an Activity
transition is represented as one explicit, non-concurrent transaction with
independent authority, phase, readiness and previous-Activity finalization evidence.

## Scope

```text
Typed Activity transition phase and terminal result vocabulary.
Owner-level rejection of concurrent Activity transactions.
Explicit ReadyToCommit and authority commit boundary.
All previous scene-content Exit callbacks before previous participant Exit.
All previous Exit work before target participant Enter.
Target participant Enter before target scene-content Enter.
Previous binding/root finalization before previous scene release.
CommittedNotReady and CommittedFinalizationFailed diagnostics.
ActivityEntered/ActivityExited events converted to completed facts for this path.
ActivityFlowStartResult carries the transaction snapshot.
```

## Current boundary

This implementation candidate transactionizes Activity start, same-Route switch,
explicit clear and the individual Activity operations used by Route startup. The
current Route replacement orchestration still performs previous-Activity clear and
target Startup Activity start as two sequential Activity transactions. A future
Route handoff cut must stage both sides under one typed cross-Route handoff before
this ADR can be considered fully implemented for Route replacement.

This boundary is explicit: the delta is intended to fix lifecycle ordering and
post-commit diagnostics without silently redesigning Route scene replacement in the
same change.

## Out of scope

```text
Input single-writer migration.
Camera publication ownership changes.
Local Player provisioning composition-root changes.
QAFramework edits.
FIRSTGAME edits.
Request queue or concurrent transition scheduling.
Cancellation and rollback after authority commit.
New gameplay-facing Activity API.
```

## Files created

```text
Runtime/ActivityFlow/ActivityTransitionPhase.cs
Runtime/ActivityFlow/ActivityTransitionTerminalStatus.cs
Runtime/ActivityFlow/PreviousActivityFinalizationStatus.cs
Runtime/ActivityFlow/ActivityTransitionSnapshot.cs
Runtime/ActivityFlow/ActivityTransitionRuntimeTransaction.cs
Runtime/ActivityFlow/ActivityContentRuntime.Transaction.cs
Runtime/ActivityFlow/ActivityFlowRuntime.Transaction.cs
Documentation~/Product/ARCH-A2-ACTIVITY-TRANSITION-RUNTIME-MANIFEST.md
Matching `.meta` files for every created Unity asset.
```

## Files edited

```text
Runtime/ActivityFlow/ActivityContentRuntime.cs
  Becomes partial so transaction phases reuse its canonical discovery and dispatch helpers.

Runtime/ActivityFlow/ActivityFlowRuntime.cs
  Removes event-driven internal content mutation.
  Routes start, switch, clear and no-startup paths through the transaction runtime.

Runtime/ActivityFlow/ActivityFlowStartResult.cs
  Carries immutable ActivityTransitionSnapshot evidence.
```

## Files removed

```text
None.
```

## Product surface affected

No designer-facing surface changes in this technical cut. The affected surface is
runtime diagnostics returned through Activity/Route request results. Product UX work
must remain separate from this contract correction.

## Expected runtime flow

```text
Create target Activity scope
-> prepare target scenes
-> validate activation requirements
-> ReadyToCommit
-> commit target Activity authority
-> exit all previous scene content
-> exit all previous participants
-> publish ActivityExited fact
-> discover target anchors/requirements
-> enter target participants
-> enter all target scene content
-> publish ActivityEntered fact
-> finalize previous bindings and scope
-> release previous scenes
-> calculate readiness
-> return CommittedReady, CommittedNotReady or CommittedFinalizationFailed
```

For explicit clear or a Route without Startup Activity, the no-active-Activity state
is committed and the same previous Exit/finalization ordering is used.

## Expected technical smoke

The next QA cut must prove:

```text
Concurrent Activity request is rejected while a transaction is non-terminal.
Commit occurs exactly once.
Every previous scene-content Exit completes before participant Exit completes.
Every previous Exit completes before the first target Enter.
Target participant Enter completes before target scene-content Enter.
Previous scene stays loaded through Exit and scope finalization.
Previous root and bindings are absent after successful finalization.
Pre-commit failure preserves previous authority and reports FailedBeforeCommit.
Post-commit required-participant failure preserves target authority and reports CommittedNotReady.
Post-commit cleanup/release failure preserves target authority and reports CommittedFinalizationFailed.
Activity events do not trigger hidden content mutation.
```

## Technical acceptance

```text
Package compiles in Unity 6.5.
No Runtime assembly depends on Editor.
No singleton or service locator introduced.
No fallback to the previous Activity after commit.
No silent post-commit rollback.
No target scene-content Enter before target participant Enter.
No previous scene release before finalization evidence.
Transaction snapshot remains diagnostic and immutable.
```

## Product acceptance

Not applicable as a product-surface closure. This cut only makes the runtime contract
safe enough for subsequent QA and product integration. P3M5B must not be closed until
the matching QA reconciliation passes.

## Architectural gain

```text
Activity transition becomes an explicit transaction instead of incidental call order.
Authority is no longer confused with readiness.
Previous ownership remains observable through finalization.
Post-commit failures retain the committed fact.
Events become facts rather than hidden commands.
```

## Usability gain

Indirect only: future Advanced/Debug surfaces can explain exactly which transition
phase, readiness predicate or retained previous owner blocked completion.

## Suggested commit message

```text
ARCH-A2 — implement Activity transition transaction runtime
```

## Delivery format

This delivery contains complete replacement/addition files. No patcher, generated diff,
or repository mutation script is included. Copy the `com.immersive.framework` directory
over the package repository root, review the listed files, then compile in Unity 6.5.
