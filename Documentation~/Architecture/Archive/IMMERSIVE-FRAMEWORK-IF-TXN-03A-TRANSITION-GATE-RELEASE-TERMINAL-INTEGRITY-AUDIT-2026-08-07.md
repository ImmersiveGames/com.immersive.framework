# Immersive Framework — IF-TXN-03A Transition Gate Release Terminal Integrity Audit v4

Status: **CLOSED / CERTIFIED**  
Date: **2026-08-07**  
Scope: `com.immersive.framework`, `QAFramework`, `planet-devourer`  
Implementation status: **CUT-01 + CUT-02 implemented; QA compatibility update implemented; Unity certification complete**

---

# 1. Executive status

`IF-TXN-03A` audits the integrity of Transition Gate cleanup independently from the already-certified transaction-authority cuts `IF-TXN-01` and `IF-TXN-02`.

```text
transition/lifecycle terminal is correct
+
committed authority is correct
+
Transition Gate cleanup is integral
+
current-state projections distinguish independent gate authorities
```

| Microaudit | Subject | Status | Result |
|---|---|---:|---|
| `03A-A` | Gate state/model | **Complete** | Internal GameFlow state; not an external acquired resource |
| `03A-B` | Canonical release semantics | **Complete** | No typed release failure/refusal path |
| `03A-C` | Cleanup state & diagnostics coherence | **Resolved + certified** | Pure Transition Gate projection separated from readiness/recovery composite |
| `03A-D` | Typed terminal release coverage | **Certified** | Dedicated terminal-integrity regression passes; no audited terminal retains Transition Gate |
| `03A-E` | QA coverage | **Resolved + certified** | Focused regression added; legacy readiness QA updated to new gate semantics |
| `03A-F` | FIRSTGAME consumer impact | **Not required** | No consumer-visible defect or FIRSTGAME-specific validation requirement demonstrated |

Final overall reading:

```text
External Release failure hypothesis: rejected
Operational Transition Gate leak: NO
Typed success with Transition Gate active: NO
Typed failure with Transition Gate active: NO
Projection scope mismatch: RESOLVED
QA coverage gap: RESOLVED
Readiness compatibility: CERTIFIED
IF-TXN-03A: CLOSED / CERTIFIED
```

---

# 2. Git baselines

## Package current HEAD verified for 03A-C / 03A-D

```text
ImmersiveGames/com.immersive.framework
372c1a1c056063a53d6050517cc5bdb98766f4f1
IF-TXN-02 Docs
```

Direct parent:

```text
193e7e954deaa430920f7967b5061b4b950ed1bb
IF-TXN-02
```

Recorded QA / FIRSTGAME baselines:

```text
rinnocenti/QAFramework
cf3cf625260ff717d6bcc919703e6868b085285f
IF-TXN-02

ImmersiveGames/planet-devourer
ab1bfe65c09af8988c2fe21ce06db780fe12aa70
Demo03Etapa04
```

Evidence rule:

```text
exact repository + exact path + exact SHA
```

is authoritative for source conclusions.

---

# 3. IF-TXN-03A-A — Gate state/model

Status: **COMPLETE**

Transition Gate is represented by runtime state in `GameFlowRuntime`, primarily:

```text
_transitionGateSnapshot
_transitionGateMode
```

The canonical model does not expose:

```text
ReleaseResult
TryRelease
IDisposable lease
external handle
owner token required for release
callback-confirmed cleanup
```

Verdict:

```text
Gap confirmed: NO
External release operation: NO
External release failure possible: NO
External resource leak: NO
Runtime cut required by 03A-A: NO
```

Architectural conclusion:

```text
Transition Gate must be audited as internal GameFlow runtime state,
not as an externally acquired resource.
```

---

# 4. IF-TXN-03A-B — Canonical release semantics

Status: **COMPLETE**

Current exact release shape:

```csharp
private TransitionGateDiagnostics ReleaseTransitionGate(
    TransitionGateMode mode,
    GateSnapshot appliedSnapshot)
{
    _transitionGateSnapshot =
        TransitionGateBlockerPolicy.CreateReleasedSnapshot();

    _transitionGateMode = TransitionGateMode.None;

    return appliedSnapshot.HasBlockers
        ? TransitionGateDiagnostics.AppliedAndReleased(mode, appliedSnapshot)
        : TransitionGateDiagnostics.NotApplied(mode);
}
```

Fallback:

```csharp
private void ReleaseTransitionGateIfStillActive()
{
    if (!_transitionGateSnapshot.HasBlockers)
    {
        return;
    }

    _transitionGateSnapshot =
        TransitionGateBlockerPolicy.CreateReleasedSnapshot();

    _transitionGateMode = TransitionGateMode.None;
}
```

Consequences:

```text
release cannot return failure
release cannot refuse cleanup
release has no owner/token authorization
release has no external callback dependency
normal cleanup writes released snapshot + mode None
```

`TransitionGateDiagnostics.AppliedAndReleased(...)` is a request-level historical receipt: it preserves the applied snapshot but marks the operation as released.

Verdict:

```text
Gap confirmed: NO
False-success possible through failed canonical release: NO
Canonical release refusal possible: NO
Canonical gate leak demonstrated: NO
New ReleaseResult abstraction required: NO
Runtime cut required by 03A-B: NO
```

---

# 5. IF-TXN-03A-C — Cleanup State & Diagnostics Coherence

Status: **COMPLETE**

## 5.1 Operational cleanup

Canonical release resets:

```text
_transitionGateSnapshot -> released snapshot
_transitionGateMode     -> None
```

The fallback applies the same operational reset when blockers remain.

There is no persistent `_transitionGateDiagnostics` current-state field in the exact current source. `TransitionGateDiagnostics` is request/result-local evidence.

Therefore the previously suspected stale persistent receipt is not the current gap.

## 5.2 Confirmed projection mismatch

Current exact projection:

```csharp
internal GateSnapshot CurrentTransitionGateSnapshot =>
    CurrentActivityEntryReadinessGateSnapshot;

internal TransitionGateMode CurrentTransitionGateMode =>
    _transitionGateMode;
```

But:

```csharp
private GateSnapshot CurrentActivityEntryReadinessGateSnapshot =>
    CombineGateSnapshots(
        _transitionGateSnapshot,
        _activityEntryReadinessRecoveryGateSnapshot);
```

Therefore:

```text
CurrentTransitionGateSnapshot
  = Transition Gate + Activity Entry Readiness recovery gate

CurrentTransitionGateMode
  = Transition Gate only
```

A valid state can exist where:

```text
_transitionGateSnapshot.HasBlockers == false
_transitionGateMode == None
_activityEntryReadinessRecoveryGateSnapshot.HasBlockers == true
```

while the current projections report:

```text
CurrentTransitionGateSnapshot.HasBlockers == true
CurrentTransitionGateMode == None
```

That can make a correctly released Transition Gate appear blocked because a different authority — readiness recovery — remains active.

Classification:

```text
Gap type: Current-state projection / semantic scope mismatch
Operational Transition Gate leak: NO
Release failure: NO
False-success caused by cleanup: NO
Misleading current-state observation: YES
```

Minimal correction:

```csharp
internal GateSnapshot CurrentTransitionGateSnapshot =>
    _transitionGateSnapshot;
```

while retaining the composite readiness view for readiness/capability admission.

QA criterion:

```text
Transition Gate = released
Readiness Recovery Gate = blocked

must yield:

CurrentTransitionGateMode == None
CurrentTransitionGateSnapshot.HasBlockers == false
CurrentActivityEntryReadinessGateSnapshot.HasBlockers == true
```

Formal verdict:

```text
IF-TXN-03A-C

Gap confirmed: YES
Severity: MEDIUM
Runtime cut required: YES
Cut size: narrow projection correction + focused QA
FIRSTGAME required: NO
```

---

# 6. IF-TXN-03A-D — Typed Terminal Release Coverage

Status: **COMPLETE**

Audit question:

```text
ApplyTransitionGate
→ operation
→ success/failure/cancel/supersede/early return/exception
→ ReleaseTransitionGate or finally fallback
→ externally observable terminal
```

Does any audited terminal escape while operational Transition Gate state is still active?

Answer:

```text
NO
```

## 6.1 C# invariant used

Every audited post-`ApplyTransitionGate` operation is enclosed by `try/finally`.

A `return` inside a C# `try` executes the `finally` before the method/task completes. Therefore an early typed return may omit explicit `ReleaseTransitionGate(...)` and still be operationally safe when `finally` executes `ReleaseTransitionGateIfStillActive()`.

---

# 7. 03A-D terminal matrix

| Operation | Gate applied? | Explicit release on normal typed paths | `finally` fallback | Early return after Apply | Exception cleanup | Typed terminal observable with gate active? |
|---|---:|---:|---:|---:|---:|---:|
| Startup | Conditional | Yes | Yes | No material uncovered branch found | Yes | **No** |
| Route request | Yes for admitted transition | Yes | Yes | **Yes** — Player transition authorization failure | Yes | **No** |
| Activity request | Yes for admitted transition | Yes | Yes | **Yes** — Player transition authorization failure | Yes | **No** |
| Activity Clear | Yes for admitted transition | Yes | Yes | No uncovered typed branch found | Yes | **No** |
| Activity Restart | Yes for admitted transition | Yes | Yes | Exceptions/callback failures unwind through finally | Yes | **No** |

Pre-validation, blocked-admission and no-op terminals that occur before `ApplyTransitionGate` are not release obligations because that request never applied a new Transition Gate.

---

# 8. Startup coverage

Gated Startup follows:

```text
_routeRequestInFlight = true
try
  Create operationId
  ApplyTransitionGate
  Transition Before
  Route lifecycle
  readiness preparation/wait
  Transition After when applicable
  ReleaseTransitionGate
  typed failure or success
finally
  ReleaseTransitionGateIfStillActive
  clear request-in-flight
  complete readiness active operation
```

Typed paths inspected include:

```text
pre-commit transition failure
Route lifecycle failure
readiness preparation/configuration failure
committed reveal failure
committed readiness failure
success
```

Readiness cancellation/invalidation reaches the common release before the Startup terminal.

Result:

```text
Startup terminal cleanup: COMPLETE
Typed success while gate active: NO
Typed failure while gate active: NO
Exception can bypass cleanup: NO
```

---

# 9. Route request coverage

Pre-gate terminals include invalid config, blocked admission, already-active Route and Player lifecycle preparation failure. They have no new transition-gate release obligation.

Post-Apply shape:

```text
ApplyTransitionGate
→ Player transition authorization
→ Transition Before
→ Route lifecycle
→ readiness/reveal
→ ReleaseTransitionGate
→ terminal
```

Important branch:

```text
Player transition authorization fails
→ typed return inside try
→ no explicit ReleaseTransitionGate receipt on that branch
→ finally runs
→ ReleaseTransitionGateIfStillActive
→ result becomes observable only after cleanup
```

This is not a leak.

Explicit release precedes normal typed branches for:

```text
pre-commit Transition failure
Route lifecycle failure
readiness preparation failure
post-commit reveal failure
readiness superseded
readiness failed/cancelled/invalidated
Player lifecycle completion validation failure
Succeeded
```

Result:

```text
Route terminal cleanup: COMPLETE
Early-return cleanup guaranteed: YES
Readiness cancellation cleanup guaranteed: YES
Readiness supersession cleanup guaranteed: YES
Exception cleanup guaranteed: YES
Typed success while gate active: NO
Typed failure while gate active: NO
```

---

# 10. Activity request coverage

Pre-gate rejects include invalid target/ID, no active Route, blocked admission, already-active Activity, blocked operation plan, invalid readiness configuration and Player lifecycle preparation failure.

Post-Apply shape:

```text
ApplyTransitionGate
→ Player transition authorization
→ Transition Before
→ Activity lifecycle commit
→ readiness/reveal
→ ReleaseTransitionGate
→ typed terminal
```

As in Route, failed Player transition authorization can return from inside the guarded `try` without an explicit release receipt; `finally` releases operational state before external observability.

Explicit release precedes:

```text
pre-commit transition failure
committed-target-not-ready diagnostic terminal
Activity lifecycle failure
readiness preparation failure
post-commit reveal failure
readiness superseded
readiness failed/cancelled/invalidated
Succeeded
```

Readiness orchestration maps:

```text
Ready
Failed
Invalidated
Cancelled
Superseded
```

back into the guarded operation.

Result:

```text
Activity terminal cleanup: COMPLETE
Early-return cleanup guaranteed: YES
Readiness cancellation cleanup guaranteed: YES
Readiness supersession cleanup guaranteed: YES
Exception cleanup guaranteed: YES
Typed success while gate active: NO
Typed failure while gate active: NO
```

---

# 11. Activity Clear coverage

After gate application:

```text
ApplyTransitionGate
→ Transition Before
→ optional beforeActivityLifecycle
→ Clear lifecycle
→ optional afterActivityLifecycle
→ Transition After
→ ReleaseTransitionGate
→ evaluate typed terminal
```

Release occurs before choosing among:

```text
Clear lifecycle failure
post-commit Transition After failure
Succeeded
```

Any exception before explicit release unwinds through the outer `finally`.

Result:

```text
Clear terminal cleanup: COMPLETE
Early-return cleanup guaranteed: YES
Exception cleanup guaranteed: YES
Explicit cancellation terminal: NOT MODELED IN THIS METHOD
Typed success while gate active: NO
Typed failure while gate active: NO
```

---

# 12. Activity Restart coverage

Pre-gate validations reject missing/invalid authority and blocked operation plans before gate application.

Active flow:

```text
ApplyTransitionGate
→ Transition Before
→ optional pre-restart lifecycle
→ Clear
→ Re-enter
→ Transition After
→ ReleaseTransitionGate
→ evaluate Restart terminal
```

`beforeRestartLifecycle` exceptions are caught and normalized to `shouldContinue = false`. The flow then performs its presentation unwind, releases Transition Gate and returns typed `FailedClear`. If the unwind itself throws, outer `finally` still performs cleanup.

Clear failure:

```text
Transition After
→ ReleaseTransitionGate
→ FailedClear
```

After Re-enter:

```text
Transition After
→ transition diagnostics
→ ReleaseTransitionGate
→ choose FailedReenter / reveal failure / Completed
```

Result:

```text
Restart terminal cleanup: COMPLETE
Early-return cleanup guaranteed: YES
Exception cleanup guaranteed: YES
Explicit cancellation terminal: NOT MODELED IN THIS METHOD
Typed Completed while gate active: NO
Typed FailedClear while gate active: NO
Typed FailedReenter while gate active: NO
Post-commit reveal failure while gate active: NO
```

---

# 13. Exception cleanup

For all gated operations audited:

```text
Startup
Route
Activity
Clear
Restart
```

an exception does not bypass the Transition Gate fallback because cleanup is in `finally`.

This microaudit does **not** claim every thrown exception is converted into a typed request result.

The narrower certified statement is:

```text
exception before explicit release
→ finally executes
→ residual transition blockers are removed
→ transition mode becomes None
→ exception may propagate, but the Transition Gate does not remain operationally active
```

---

# 14. Request-level receipt nuance

`ReleaseTransitionGate(...)` creates a request-level receipt:

```text
AppliedAndReleased
or
NotApplied
```

The fallback:

```text
ReleaseTransitionGateIfStillActive()
```

returns no receipt.

Therefore an early typed return that depends on `finally` may have:

```text
operational Transition Gate:
  released correctly

request-level TransitionGateDiagnostics:
  default / no explicit AppliedAndReleased receipt
```

This is not:

```text
gate leak
false success
release refusal
```

It is a **terminal-cleanup evidence question** resolved by `03A-E`: operational cleanup is correct, but fallback-only cleanup lacks focused regression coverage.

No new cleanup-receipt abstraction is justified until QA evidence is audited.

---

# 15. IF-TXN-03A-D formal verdict

```text
IF-TXN-03A-D — Typed Terminal Release Coverage

Gap confirmed:
NO operational cleanup gap

Severity:
N/A for operational release

Startup terminal cleanup:
COMPLETE

Route request terminal cleanup:
COMPLETE

Activity request terminal cleanup:
COMPLETE

Clear terminal cleanup:
COMPLETE

Restart terminal cleanup:
COMPLETE

Early-return cleanup guaranteed:
YES

Exception cleanup guaranteed:
YES

Readiness cancellation cleanup guaranteed:
YES for Startup/Route/Activity readiness envelope

Clear/Restart explicit cancellation:
NOT MODELED by these methods

Typed success observable while Transition Gate active:
NO

Typed failure observable while Transition Gate active:
NO

Restart Completed observable while Transition Gate active:
NO

Operational gate leak demonstrated:
NO

New runtime cut required by 03A-D:
NO

Diagnostic/receipt follow-up:
YES — 03A-E confirms fallback-only cleanup lacks focused direct QA coverage
```

---

# 16. Combined findings A-D

## Confirmed gap

Only one gap is confirmed so far:

```text
03A-C
CurrentTransitionGateSnapshot conflates:
  Transition Gate
  +
  Activity Entry Readiness recovery gate
```

Classification:

```text
current-state projection / semantic scope mismatch
not operational release failure
not external resource leak
```

## Confirmed non-gaps

```text
Transition Gate is not an external acquired resource.
Canonical release does not return failure/refusal.
Owner/token mismatch cannot reject release because that authorization model does not exist.
Normal typed success is not returned before gate release.
Normal typed failure is not returned before gate release.
Early returns inside guarded try blocks execute finally cleanup first.
Exceptions do not bypass Transition Gate finally cleanup.
Clear/Restart preserve their IF-TXN-02 authority semantics without leaving Transition Gate active.
```

---

# 17. Runtime cut status after 03A-D

The only runtime correction currently justified remains the narrow 03A-C projection fix:

```text
Runtime/GameFlow/GameFlowRuntime.cs

CurrentTransitionGateSnapshot
  should project _transitionGateSnapshot only
```

No change is currently justified to:

```text
ApplyTransitionGate
ReleaseTransitionGate
ReleaseTransitionGateIfStillActive
Startup transaction shape
Route transaction shape
Activity transaction shape
Clear transaction shape
Restart transaction shape
TransitionGateDiagnostics
```

The `03A-E` audit confirms a focused QA evidence gap, but does not justify changing the release mechanism itself.

---

# 18. IF-TXN-03A-E — QA Coverage

Status: **COMPLETE**

QA baseline audited:

```text
rinnocenti/QAFramework
cf3cf625260ff717d6bcc919703e6868b085285f
IF-TXN-02
```

## 18.1 Audit question

Determine whether current QA directly proves the `03A-D` cleanup invariant:

```text
for every relevant terminal after ApplyTransitionGate
→ Transition Gate operational state is released
→ no residual Transition blocker remains
→ fallback-only cleanup is regression-protected
```

and whether QA can distinguish the `03A-C` authorities:

```text
Transition Gate
vs
Activity Entry Readiness recovery gate
```

Classification used:

```text
DIRECT
  the QA case explicitly observes/asserts Transition Gate state
  for the relevant runtime path.

INDIRECT
  the case proves compatible downstream behavior, e.g. a later lifecycle
  request succeeds, but does not assert the gate state itself.

PARTIAL
  the case directly proves a neighboring/composite invariant but cannot
  isolate the exact Transition Gate claim.

MISSING
  no current focused proof was found for the exact cleanup claim.
```

This is a source-coverage audit. It does not claim that Unity regressions were executed during this microaudit.

---

# 19. QA evidence inventory

## 19.1 IF-TXN-01

File:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaIfTxn01TransitionFailureAuthorityRegression.cs
```

The 22-case regression directly proves:

```text
Transition phase acceptance
Route pre-commit typed terminal
Route committed-reveal typed terminal
Activity pre-commit typed terminal
Activity committed-reveal typed terminal
Startup pre-commit/reveal flags
readiness terminal distinction
recovery-policy distinction
Before/After authority wiring
```

It does **not** assert `TransitionGateSnapshot` or equivalent operational gate state.

Conclusion:

```text
terminal/authority semantics: DIRECT
Transition Gate cleanup for those failures: MISSING DIRECT PROOF
```

## 19.2 IF-TXN-02

File:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaIfTxn02ClearRestartTransitionAuthorityRegression.cs
```

The 16-case regression directly proves:

```text
Clear pre-commit terminal
Clear post-commit reveal terminal
Clear committed no-Activity authority
Restart pre-commit terminal
Restart post-commit reveal terminal
Restart committed target authority
no blind rollback
Before/After wiring
```

It does **not** assert `TransitionGateSnapshot`.

Conclusion:

```text
Clear/Restart terminal authority: DIRECT
Transition Gate cleanup for Clear/Restart failure terminals: MISSING DIRECT PROOF
```

## 19.3 Direct Activity readiness policy regression

File:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaDirectActivityReadinessPoliciesRegression.cs
```

This suite directly observes that WaitVisible/WaitCovered hold gate blockers while waiting and explicitly calls a gate-release assertion after Ready:

```text
GateSnapshot gate = host.TransitionGateSnapshot;
Require(!gate.HasBlockers, ...);
```

It also checks the same invariant during request unwind/final cleanup.

Conclusion:

```text
Direct Activity WaitVisible success cleanup: DIRECT
Direct Activity WaitCovered success cleanup: DIRECT
normal readiness request unwind leaves no composite blocker: DIRECT
```

## 19.4 Route Startup / Game Application Startup parity

File:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaParticipantAwareStartupParityRegression.cs
```

Both positive paths contain an explicit terminal check:

```text
!host.TransitionGateSnapshot.HasBlockers
```

for:

```text
Route Startup Activity success
Game Application Startup Activity success
```

Conclusion:

```text
Route success cleanup: DIRECT
Game Application Startup success cleanup: DIRECT
```

## 19.5 Participant-aware readiness terminal regression

File:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaParticipantAwareReadinessLoadingTerminalRegression.cs
```

The real direct-Activity Required failure path proves:

```text
committed target failure terminal
committed destination remains authoritative
terminal progress remains below 100%
Loading remains visible
Transition cover remains visible
recovery gate remains blocked
participants are later released
fixture/presentation are cleaned
final public gate snapshot has no blockers
```

At the failure terminal the helper reads:

```text
GateSnapshot snapshot = host.TransitionGateSnapshot;
Require(snapshot.HasBlockers && ...);
```

This is expected because a recovery gate remains active.

However, under the current package projection found in `03A-C`:

```text
host.TransitionGateSnapshot
  = Transition Gate + readiness recovery gate
```

Therefore the QA cannot currently prove the required intermediate distinction:

```text
operational Transition Gate already released
while
readiness recovery gate remains active
```

Conclusion:

```text
readiness failure composite recovery behavior: DIRECT
final fully-clean state after explicit cleanup: DIRECT
Transition-only release at the failure terminal: MISSING
03A-C separation invariant: MISSING
```

## 19.6 Diagnostic fault lease / player-independent navigation

Files:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaGameFlowDiagnosticFaultLeaseSmoke.cs
  QaGameFlowPlayerIndependentNavigationRegression.cs
  QaGameFlowPlayerIndependentNavigationSupplementalCases.cs
```

They cover real runtime faults such as:

```text
PreparationTokenMismatch
OwnerMismatch
PreCommitFailure
RuntimeUnavailable
LoadingRejectedBeforePresentation
CommittedTargetNotReady
CommittedFinalizationFailure
```

and prove authority/materialization cleanup. Some post-commit scenarios then successfully request restoration of the entry Activity.

No direct `TransitionGateSnapshot` assertion is made in these files.

Also, the package fault seam used by `RuntimeUnavailable` can reject during Player lifecycle **preparation**, which occurs before `ApplyTransitionGate`; therefore that case must not be misclassified as proof of the `03A-D` fallback-only post-Apply authorization return.

Conclusion:

```text
fault terminal/authority behavior: DIRECT
ability to perform later restore operations: INDIRECT gate-cleanup evidence
post-Apply authorization fallback-only cleanup: MISSING
exception-after-Apply gate assertion: MISSING
```

## 19.7 Clear integrated transaction smoke

File:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaArchA2ActivityTransitionTransactionSmoke.cs
```

The integrated path:

```text
ClearActivityAsync succeeds
→ no-Activity authority confirmed
→ RequestActivityAsync(originalActivity) succeeds
```

A residual lifecycle-blocking Transition Gate would prevent the second request, so this is useful behavioral evidence.

It is not an explicit gate-state assertion.

Conclusion:

```text
Clear success cleanup: INDIRECT
Clear failure cleanup: not proven by this suite
```

## 19.8 Restart vertical smoke

File:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaActivityRestartVerticalSmoke.cs
```

It proves nominal Restart completes with:

```text
ClearStatus == Succeeded
ReenterStatus == Succeeded
current Activity restored
no trigger in-flight state retained
```

It does not inspect `TransitionGateSnapshot`.

Conclusion:

```text
Restart nominal terminal: DIRECT
Restart gate cleanup: INDIRECT at best
Restart failure-path gate cleanup: MISSING
```

## 19.9 M07 included/excluded failure/release regression

File:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaM07IncludedExcludedFailureReleaseScopeRegression.cs
```

This suite provides strong real-runtime proof for scoped Player failure/release, committed Activity authority and Session preservation.

No `TransitionGateSnapshot` assertion was found.

Conclusion:

```text
Player failure/release scope: DIRECT
Transition Gate cleanup: not a direct proof source
```

---

# 20. IF-TXN-03A-E coverage matrix

| Required cleanup claim | Current strongest evidence | Classification |
|---|---|---|
| Game Application Startup success releases gate | Q2B Startup parity explicitly asserts no gate blockers | **DIRECT** |
| Route Startup success releases gate | Q2B Route parity explicitly asserts no gate blockers | **DIRECT** |
| Direct Activity WaitVisible success releases gate | Direct readiness policies regression | **DIRECT** |
| Direct Activity WaitCovered success releases gate | Direct readiness policies regression | **DIRECT** |
| Activity committed-readiness failure eventually cleans all gate blockers | Q2A terminal regression after recovery cleanup | **DIRECT** |
| Activity committed-readiness failure has Transition Gate released while recovery gate remains | Current public projection is composite | **MISSING** |
| Route pre-commit Transition failure releases gate | IF-TXN-01 proves terminal/authority only | **MISSING** |
| Route post-commit reveal failure releases gate | IF-TXN-01 proves terminal/authority only | **MISSING** |
| Activity pre-commit Transition failure releases gate | IF-TXN-01 proves terminal/authority only | **MISSING** |
| Activity post-commit reveal failure releases gate | IF-TXN-01 proves terminal/authority only | **MISSING** |
| Route post-Apply authorization early return is cleaned by `finally` | Static runtime proof in 03A-D; no focused QA case | **MISSING** |
| Activity post-Apply authorization early return is cleaned by `finally` | Static runtime proof in 03A-D; no focused QA case | **MISSING** |
| Exception after gate apply cannot retain gate | Static runtime proof in 03A-D; fault QA does not assert gate | **MISSING** |
| Readiness cancellation leaves Transition Gate released | cancellation envelope exists; operation gate not directly asserted | **PARTIAL** |
| Readiness supersession leaves Transition Gate released | terminal semantics exist; no focused gate assertion | **PARTIAL** |
| Clear success leaves gate released | ARCH-A2 Clear followed by successful Activity request | **INDIRECT** |
| Clear pre-commit failure leaves gate released | IF-TXN-02 terminal proof only | **MISSING** |
| Clear post-commit reveal failure leaves gate released | IF-TXN-02 terminal proof only | **MISSING** |
| Restart success leaves gate released | Restart vertical smoke proves nominal completion, no gate assertion | **INDIRECT** |
| Restart pre-commit failure leaves gate released | IF-TXN-02 terminal proof only | **MISSING** |
| Restart Clear failure leaves gate released | no focused gate assertion found | **MISSING** |
| Restart Re-enter failure leaves gate released | no focused gate assertion found | **MISSING** |
| Restart post-commit reveal failure leaves gate released | IF-TXN-02 authority proof only | **MISSING** |
| Transition/recovery projections remain distinct | impossible to prove with current incorrect projection | **MISSING** |

---

# 21. What current QA already certifies well

The missing items above must not be interpreted as a weak overall QA harness.

Current QA already has strong direct proof for separate concerns:

```text
IF-TXN-01
  typed pre/post-commit failure authority

IF-TXN-02
  Clear/Restart authority parity and no blind rollback

Direct Activity Readiness
  gate retained while waiting
  gate released after Ready
  presentation ordering

Participant-Aware Startup Parity
  Route Startup success gate release
  Game Application Startup success gate release

Participant-Aware Terminal
  real committed readiness failure
  recovery gate retention
  final cleanup

Diagnostic Fault Lease
  real fault injection
  typed failure/authority/materialization cleanup
```

The gap is specifically that these suites do not form a **single focused regression contract for Transition Gate terminal integrity**.

---

# 22. Gap classification

```text
IF-TXN-03A-E — QA Coverage

Gap confirmed:
YES

Gap type:
QA regression coverage / terminal cleanup evidence

Severity:
MEDIUM

Operational Transition Gate leak demonstrated:
NO

Existing QA proves happy-path gate release:
YES

Existing QA proves all failure-path gate release directly:
NO

Fallback-only post-Apply cleanup directly proven:
NO

Exception-after-Apply cleanup directly proven:
NO

03A-C Transition-vs-recovery separation directly proven:
NO

Existing QA sufficient to close IF-TXN-03A after package fix:
NO

QA cut required:
YES

FIRSTGAME required:
NO based on current evidence
```

Severity remains MEDIUM because:

```text
runtime source audit 03A-D shows cleanup structure is correct
but
regression coverage does not protect several terminal/finally invariants
and
03A-C is a real diagnostic projection bug that needs a dedicated assertion
```

This is not evidence of a latent high-severity runtime leak.

---

# 23. Smallest justified implementation/QA cut

No generic cleanup abstraction is justified.

The smallest package change remains the `03A-C` correction:

```text
Runtime/GameFlow/GameFlowRuntime.cs

CurrentTransitionGateSnapshot
  from: CurrentActivityEntryReadinessGateSnapshot
  to:   _transitionGateSnapshot
```

The smallest QA addition should be one focused regression, conceptually:

```text
QaIfTxn03ATransitionGateTerminalIntegrityRegression.cs
```

Its responsibility should be only:

```text
prove gate released after selected representative terminal classes
prove finally-only cleanup after a post-Apply early return
prove exception cleanup after Apply
prove Clear/Restart failure cleanup
prove Transition Gate vs readiness recovery separation
```

Prefer existing QA fakes, internal package access and diagnostic fault seams.
Do not add a production API only for QA when current internal test access can prove the invariant.

## Minimum cases

```text
1. Activity success -> transition snapshot released
2. Route success -> transition snapshot released
3. pre-commit Transition failure -> released
4. post-commit reveal failure -> released
5. Activity authorization rejected after Apply -> finally cleanup
6. Route authorization rejected after Apply -> finally cleanup
7. exception after Apply -> finally cleanup
8. readiness failure -> transition released + recovery still blocks
9. readiness cancellation/supersession -> transition released
10. Clear pre/post-commit failure -> released
11. Restart pre/clear/reenter/reveal failure -> released
12. Restart success -> released
```

The QA can combine symmetric cases when the same runtime mechanism is demonstrably shared, but must not infer Clear/Restart coverage solely from Route/Activity tests.

---

# 24. Technical acceptance for the eventual cut

Package:

```text
CurrentTransitionGateSnapshot represents Transition Gate only
CurrentTransitionGateMode remains Transition Gate only
readiness recovery authority remains unchanged
no change to release semantics
no new fallback
```

QA:

```text
focused regression executes representative success/failure/finally paths
post-terminal transition snapshot has no blockers
mode is None after transition cleanup
fallback-only early return is directly proven
exception cleanup is directly proven
readiness recovery may remain active without contaminating TransitionGateSnapshot
Clear/Restart failure paths are included
existing IF-TXN-01 and IF-TXN-02 baselines remain green
```

Product/FIRSTGAME:

```text
no new product surface
no FIRSTGAME change required
```

---

# 25. IF-TXN-03A cumulative verdict after A-E

```text
IF-TXN-03A — Transition Gate Release Terminal Integrity

Operational model:
CERTIFIED — internal GameFlow state

Canonical release semantics:
CERTIFIED — unconditional state replacement, no release refusal contract

Operational terminal cleanup:
CERTIFIED BY SOURCE AUDIT — no typed terminal escapes with Transition Gate active

Runtime gap confirmed:
YES — CurrentTransitionGateSnapshot semantic scope mismatch

Runtime gap severity:
MEDIUM

QA gap confirmed:
YES — failure/finally cleanup regression coverage incomplete

Operational gate leak demonstrated:
NO

False-success through release failure demonstrated:
NO

New release abstraction required:
NO

Package cut required:
YES — narrow projection correction

QA cut required:
YES — focused IF-TXN-03A terminal-integrity regression

FIRSTGAME validation required:
NO based on current evidence
```

---

# 26. 03A-F disposition

`03A-F — FIRSTGAME consumer impact` was conditional.

Current evidence does not demonstrate:

```text
consumer-visible stuck Transition Gate
real FIRSTGAME navigation leak
FIRSTGAME-only integration defect
product authoring problem caused by release semantics
```

The confirmed package issue is a current-state projection scope mismatch and the second gap is QA regression coverage.

Therefore:

```text
03A-F status:
NOT REQUIRED FOR THE CURRENT CUT
```

Reopen FIRSTGAME only if the package correction or QA reproduction exposes a real consumer-facing behavioral consequence.

---

# 27. Recommended next action

The audit phase of IF-TXN-03A is sufficiently complete to stop investigating and move to a narrow implementation cut when authorized:

```text
Package
  fix CurrentTransitionGateSnapshot projection

QAFramework
  add focused IF-TXN-03A terminal-integrity regression

FIRSTGAME
  no change
```

Suggested conceptual cut name:

```text
IF-TXN-03A-transition-gate-terminal-integrity
```

Suggested commit messages when implementation is authorized:

```text
fix(game-flow): separate transition gate current-state projection

test(game-flow): certify transition gate terminal cleanup
```

No repository modification was performed by this audit.

---

# Appendix A — Canonical source evidence

Package baseline:

```text
ImmersiveGames/com.immersive.framework
372c1a1c056063a53d6050517cc5bdb98766f4f1
```

QA baseline:

```text
rinnocenti/QAFramework
cf3cf625260ff717d6bcc919703e6868b085285f
```

Primary package files:

```text
Runtime/GameFlow/GameFlowRuntime.cs
Runtime/GameFlow/GameFlowRuntime.ActivityEntryReadinessOrchestration.cs
Runtime/GameFlow/GameFlowRuntime.PlayerActivityAdmission.cs
Runtime/GameFlow/TransitionGateDiagnostics.cs
```

Primary QA files:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaIfTxn01TransitionFailureAuthorityRegression.cs
  QaIfTxn02ClearRestartTransitionAuthorityRegression.cs
  QaDirectActivityReadinessPoliciesRegression.cs
  QaParticipantAwareReadinessLoadingTerminalRegression.cs
  QaParticipantAwareStartupParityRegression.cs
  QaGameFlowDiagnosticFaultLeaseSmoke.cs
  QaGameFlowPlayerIndependentNavigationRegression.cs
  QaGameFlowPlayerIndependentNavigationSupplementalCases.cs
  QaArchA2ActivityTransitionTransactionSmoke.cs
  QaActivityRestartVerticalSmoke.cs
  QaM07IncludedExcludedFailureReleaseScopeRegression.cs
```

---

# Appendix B — Final audit-stage verdict

```text
IF-TXN-03A after A-E

A Gate model:
PASS / no gap

B Release semantics:
PASS / no gap

C Cleanup state & diagnostics coherence:
GAP / MEDIUM
CurrentTransitionGateSnapshot scope mismatch

D Typed terminal release coverage:
PASS by source audit / no operational leak

E QA coverage:
GAP / MEDIUM
Focused failure/finally cleanup regression missing

F FIRSTGAME:
NOT REQUIRED by current evidence

Smallest justified next action:
package projection fix + focused QA regression
```

# 28. Implementation record — CUT-01

Status: **IMPLEMENTED / CERTIFIED**

Files changed in `com.immersive.framework`:

```text
Runtime/GameFlow/GameFlowRuntime.cs
Runtime/GameFlow/GameFlowRuntime.ActivityEntryReadinessOrchestration.cs
Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs
```

Implemented semantic split:

```text
TransitionGateSnapshot
  -> pure canonical Transition Gate

CurrentTransitionGateMode
  -> canonical Transition Gate mode

ActivityEntryReadinessGateSnapshot
  -> Transition Gate + Activity Entry Readiness Recovery Gate

CurrentGateSnapshot
  -> operational composite used by host/input admission
```

The critical corrected state is now representable and observable without semantic contamination:

```text
Transition Gate = released
Readiness Recovery Gate = blocked

CurrentTransitionGateMode == None
TransitionGateSnapshot.HasBlockers == false
ActivityEntryReadinessGateSnapshot.HasBlockers == true
```

No changes were made to:

```text
ApplyTransitionGate
ReleaseTransitionGate
ReleaseTransitionGateIfStillActive
finally semantics
typed terminals
lifecycle authority
readiness recovery semantics
loading behavior
transition presentation
Player lifecycle
```

No new lease, token, ownership manager, release result, global manager, or fallback abstraction was introduced.

---

# 29. QA implementation record — CUT-02

Status: **IMPLEMENTED / CERTIFIED**

New regression:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaIfTxn03ATransitionGateTerminalIntegrityRegression.cs
  QaIfTxn03ATransitionGateTerminalIntegrityRegression.cs.meta
```

The focused regression covers 16 cases, including:

```text
projection-source-pure-transition-gate
readiness-composite-source-preserved
route-success-release-wiring
activity-success-release-wiring
pre-commit-failure-release-wiring
post-commit-reveal-failure-release-wiring
authorization-rejection-finally-release
exception-fault-finally-release
clear-terminal-release-wiring
restart-failure-release-wiring
restart-success-release-wiring
runtime-apply-release-residual-clean
readiness-recovery-active-transition-clean
recovery-cleanup-all-clean
host-surface-separation
```

The regression directly certifies the separation between Transition Gate cleanup and readiness recovery authority.

---

# 30. QA compatibility update

Status: **IMPLEMENTED / CERTIFIED**

Files updated in `QAFramework`:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaParticipantAwareReadinessLoadingTerminalRegression.cs
  QaDirectActivityReadinessPoliciesRegression.cs
  QaParticipantAwareReadinessLoadingProgressRegression.cs
  QaParticipantAwareStartupParityRegression.cs
```

Root cause of the compatibility failure discovered during certification:

```text
legacy QA assumed:
  TransitionGateSnapshot = Transition Gate + Recovery Gate

new canonical contract:
  TransitionGateSnapshot = pure Transition Gate
  ActivityEntryReadinessGateSnapshot = transition + readiness recovery composite
```

The first execution of `QaParticipantAwareReadinessLoadingTerminalRegression` failed with:

```text
Direct terminal failure did not retain the recovery gate.
```

Runtime diagnostics in the same terminal reported:

```text
gateReleased='True'
recoveryGate='True'
```

Therefore the runtime behavior was correct and the QA assertion was stale.

Compatibility corrections now distinguish:

```text
Transition Gate proof
  -> TransitionGateSnapshot

Readiness/recovery blocker proof
  -> ActivityEntryReadinessGateSnapshot

Complete cleanup proof
  -> pure Transition Gate clean + mode None + readiness composite clean
```

No migration to `CurrentGateSnapshot` was necessary for these regressions.

The update also removed a potential false-positive cleanup condition where a pure `TransitionGateSnapshot` could be empty while a recovery blocker remained active.

---

# 31. Unity certification results

All certification runs were executed manually in Unity against the updated package/QA source.

| Regression / path | Result | Cases |
|---|---:|---:|
| `IF-TXN-03A Transition Gate Terminal Integrity` | **PASS** | 16/16 |
| `IF-TXN-01 Transition Failure Authority` | **PASS** | 22/22 |
| `IF-TXN-02 Clear/Restart Transition Authority` | **PASS** | 16/16 |
| `Participant-Aware Readiness Loading Terminal` | **PASS** | 34/34 |
| `Direct Activity Readiness Policies` | **PASS** | 42/42 |
| `Participant-Aware Readiness Loading Progress` | **PASS** | 32/32 |
| `Participant-Aware Startup Parity — RouteStartupActivity` | **PASS** | 25/25 |
| `Participant-Aware Startup Parity — GameApplicationStartupActivity` | **PASS** | 20/20 |

Key executed evidence:

```text
IF-TXN-03A:
  readiness-recovery-active-transition-clean
  recovery-cleanup-all-clean
  host-surface-separation

Readiness Loading Terminal:
  direct-recovery-gate-retained
  direct-gate-released
  direct-initial-authority-restored

Direct Activity Readiness Policies:
  WaitVisible = Passed
  WaitCovered = Passed
  gate retained while operation active
  gate released after terminal

Startup Parity:
  RouteStartupActivity = Passed
  GameApplicationStartupActivity = Passed
  transition-gate-released in both paths

Loading Progress:
  terminal progress before hide
  hide before reveal
  gate released
  initial authority restored
```

The expected `RequiredParticipantFailed` error emitted by the terminal-readiness negative case is diagnostic evidence for the intentional failure scenario; the regression itself completed `Passed` after validating recovery retention and cleanup.

---

# 32. Final acceptance evaluation

Technical acceptance:

```text
package compiles in the Unity QA consumer: PASS by successful executed regressions
focused IF-TXN-03A regression: PASS
IF-TXN-01 compatibility: PASS
IF-TXN-02 compatibility: PASS
readiness regressions: PASS
no silent release fallback introduced: PASS
required failure remains explicit and diagnostic: PASS
Transition Gate and recovery contracts remain distinct: PASS
```

Product / consumer acceptance for this cut:

```text
new product authoring surface: NONE
FIRSTGAME change required: NO
FIRSTGAME validation required: NO
consumer-visible behavioral regression demonstrated: NO
```

No additional runtime cut is justified by the certification evidence.

---

# 33. Final IF-TXN-03A verdict

```text
IF-TXN-03A — Transition Gate Release Terminal Integrity

A Gate model:
PASS

B Canonical release semantics:
PASS

C Cleanup state & diagnostics coherence:
RESOLVED / PASS

D Typed terminal release coverage:
CERTIFIED / PASS

E QA coverage:
RESOLVED / PASS

F FIRSTGAME consumer impact:
NOT REQUIRED

Operational Transition Gate leak:
NO

False success through release failure:
NO

Projection contamination between Transition Gate and readiness recovery:
NO after CUT-01

QA semantic dependence on legacy composite projection:
RESOLVED

Additional runtime cut required:
NO

Additional QA compatibility cut required:
NO

FIRSTGAME cut required:
NO

FINAL STATUS:
CLOSED / CERTIFIED
```

Recommended closure labels:

```text
Implementation: PASS
Regression: PASS
Compatibility: PASS
Readiness compatibility: PASS
IF-TXN-03A: CLOSED / CERTIFIED
```

---

# Appendix C — Closure note

The audit-stage recommendation in sections 23–27 is retained as historical evidence of the decision path. Sections 28–33 supersede the earlier **pending implementation / pending QA certification** status.

The v3 document remains the pre-implementation audit snapshot. This v4 document is the canonical closure record for IF-TXN-03A.

