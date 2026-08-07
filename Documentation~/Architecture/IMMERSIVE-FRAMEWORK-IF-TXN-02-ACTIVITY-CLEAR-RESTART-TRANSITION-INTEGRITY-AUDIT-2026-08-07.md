# Immersive Framework — IF-TXN-02 Activity Clear/Restart Transition Integrity Audit

**Date:** 2026-08-07  
**Project:** Immersive Framework / Unity 6.5  
**Audit type:** Technical architecture / transaction terminal integrity  
**Proposed cut:** `IF-TXN-02 — Activity Clear/Restart Transition Terminal Integrity`

---

## 1. Executive verdict

**IF-TXN-02 is required.**

IF-TXN-01 correctly made `TransitionResult.Completed` authoritative for the canonical GameFlow startup, Route request, and Activity request paths, with intentional policy/no-visual `Skipped` remaining accepted. However, the current Activity **Clear** and **Restart** flows remain separate transaction paths and do not uniformly apply that authority rule to their transition `Before` and `After` phases.

The current code can therefore execute lifecycle side effects and still report a normal success path even when a Clear/Restart transition phase returned a non-accepted terminal result. This is precisely the class of terminal-integrity issue that IF-TXN-01 removed from the canonical request paths, but Clear/Restart were explicitly left outside that cut.

This is **not** a reason to introduce a generic transaction manager, rollback manager, retry framework, global state machine, or new manager singleton. The smallest correct cut is to extend transition terminal authority to the existing Clear/Restart orchestration while respecting their different authority boundaries.

### Final recommendation

Implement:

```text
IF-TXN-02 — Activity Clear/Restart Transition Terminal Integrity
```

with two distinct authority models:

```text
Activity Clear
  Before failure  -> preserve current Activity; do not clear.
  Clear committed -> authoritative Activity becomes None.
  After failure   -> remain cleared; non-success; no blind restoration.

Activity Restart
  Before failure  -> preserve current Activity + occurrence; do not clear.
  Old occurrence exited, new not committed
                  -> report actual authority, possibly None; no rollback fiction.
  New occurrence committed + After failure
                  -> new occurrence remains authoritative; non-success; recovery protection.
```

---

## 2. Baseline examined

Official package repository:

```text
ImmersiveGames/com.immersive.framework
```

Current repository head observed during this audit:

```text
c0a87f3792c41e67df0236926370945e99f96dd7
Terminal IF-TXN-01Docs
```

Relevant runtime implementation inspected:

```text
Runtime/GameFlow/GameFlowRuntime.cs
Runtime/GameFlow/GameFlowRuntime.TransitionFailureAuthority.cs
```

The audit also uses the already-established architecture and QA boundaries from the project documentation and prior IF-TXN-01 certification.

### Repository responsibility remains frozen

```text
com.immersive.framework
  Official implementation and contracts.

QAFramework
  Technical regressions, synthetic failure injection, negative cases.

FIRSTGAME / planet-devourer
  Real consumer/usability proof, not technical certification.
```

No QA runner for IF-TXN-02 should be added to package runtime diagnostics.

---

## 3. What IF-TXN-01 already guarantees

`GameFlowRuntime.TransitionFailureAuthority.cs` defines the current canonical phase acceptance rule:

```text
Accepted transition phase =
  valid TransitionResult where result.Completed == true
  OR intentional TransitionStatus.Skipped
```

This preserves:

```text
Succeeded                  -> accepted
CompletedWithWarnings      -> accepted through Completed
policy/no-visual Skipped   -> accepted
Failed                     -> not accepted
Rejected                   -> not accepted
Cancelled                  -> not accepted
invalid result             -> not accepted
```

For canonical Startup, Route Request, and Activity Request, IF-TXN-01 then distinguishes:

```text
Before non-accepted
  -> abort before destination commit
  -> preserve previous authority
  -> typed pre-commit transition failure

After/reveal non-accepted after destination commit
  -> committed destination remains authoritative
  -> typed committed-target reveal failure
  -> recovery protection
  -> no blind rollback
```

That is the correct transaction model and must **not** be weakened by IF-TXN-02.

---

## 4. Why Clear is not covered by the ordinary Activity Request model

Activity Clear is materially different from Activity Request because its successful lifecycle destination is intentionally:

```text
CurrentActivity = None
```

There is no target Activity whose readiness should be awaited, and there is no legitimate post-clear rule that can say “the previous Activity remains authoritative” once clear lifecycle has completed.

That means Clear requires its own transaction boundary:

```text
Before clear commit
  previous Activity can still be preserved.

After clear commit
  the cleared state / no current Activity is authoritative.
```

Trying to reuse a “committed target reveal” semantic literally for Clear would be misleading because there is no committed Activity target.

---

## 5. Current Activity Clear flow — observed implementation

The current `ClearActivityAsync` flow in `GameFlowRuntime.cs` performs, in order:

```text
validate active Route
interrupt active readiness for clear
admission gate
validate active Activity
preview ActivityOperationKind.Clear
mark Activity request in flight
create ActivityClear transition operation
apply transition gate
execute Transition Before
invoke beforeActivityLifecycle hook
execute RouteLifecycleRuntime.ClearActivityAsync
invoke afterActivityLifecycle hook
execute Transition After
build FrameworkTransitionDiagnostics.Completed(...)
release transition gate
check activityFlowResult.Completed
return failure or success
```

The critical observation is that the current Clear path executes:

```text
transitionBefore = await ExecuteActivityTransitionAsync(...)
```

but does **not** immediately apply `TryAcceptTransitionPhase(transitionBefore, ...)` before invoking lifecycle work.

Likewise it executes:

```text
transitionAfter = await ExecuteActivityTransitionAsync(...)
```

and then builds diagnostics/releases the gate without making the acceptance of `transitionAfter` authoritative over the final request result.

### Consequence

A non-accepted transition phase can be recorded in diagnostics but fail to control transaction continuation.

The problematic shapes are:

```text
Clear Before Failed/Rejected/Cancelled
  -> lifecycle can still clear the Activity

Clear After Failed/Rejected/Cancelled after successful clear
  -> request can still be reported as SucceededWith(...)
```

This violates the invariant established by IF-TXN-01:

```text
A required transition phase is not telemetry-only.
Its terminal result participates in GameFlow transaction authority.
```

---

## 6. Required Activity Clear semantics

### 6.1 Clear Before — non-accepted

Required behavior:

```text
Transition Before non-accepted
  -> do not invoke beforeActivityLifecycle if it represents post-transition lifecycle preparation
     unless current contract explicitly requires pre-abort cleanup
  -> do not call RouteLifecycleRuntime.ClearActivityAsync
  -> do not invoke normal afterActivityLifecycle
  -> previous Activity remains current
  -> previous occurrence remains authoritative
  -> request result is a typed transition failure
  -> release only operation-owned normal gate state
  -> do not claim Clear succeeded
```

The exact ordering of consumer hooks must be checked against their current contract before editing, but **Clear lifecycle itself cannot begin after a non-accepted required Before phase**.

### 6.2 Clear lifecycle fails before commit

Existing lifecycle failure semantics should remain authoritative unless code inspection proves that partial clear side effects can occur before `Completed == false`.

IF-TXN-02 should not invent compensation for hypothetical side effects.

Required minimum:

```text
clearFlowResult.Completed == false
  -> no success
  -> preserve/report actual current authority
  -> transition diagnostics remain truthful
```

### 6.3 Clear committed, After non-accepted

Once clear lifecycle has completed successfully:

```text
CurrentActivity = None
```

is the correct resulting authority.

Required behavior:

```text
Transition After non-accepted
  -> previous Activity is NOT restored
  -> cleared/no-Activity state remains authoritative
  -> request result is non-success
  -> result/diagnostics state clearly that clear committed but presentation completion failed
  -> retain/apply only scoped recovery protection required to prevent unsafe continuation
  -> no blind re-entry of previous Activity
  -> no generic rollback
```

The existing IF-TXN-01 label `FailedCommittedTargetReveal` may not be semantically correct because Clear has no target Activity. The implementation cut should inspect the current result vocabulary and choose the smallest truthful representation:

```text
reuse existing terminal kind only if it remains semantically accurate,
otherwise introduce a minimal Clear/post-commit transition terminal kind.
```

Do not rename broad public surfaces merely for aesthetic consistency.

---

## 7. Current Activity Restart flow — observed implementation

The current `RestartActivityAsync` flow is a compound transaction built around a single Activity transition and two lifecycle stages:

```text
validate target/current Activity
admission gate
preview clear stage
preview re-enter stage
mark Activity request in flight
create transition operation
apply transition gate
execute Transition Before
run optional beforeRestartLifecycle
clear current Activity
if clear failed -> execute Transition After and return FailedClear
start/re-enter target Activity
execute Transition After
build transition diagnostics
release gate
build clear success result
if re-enter failed -> FailedReenter
otherwise -> Completed
```

Again, the current path obtains `transitionBefore` and `transitionAfter` but does not consistently make `TryAcceptTransitionPhase(...)` authoritative for continuation/terminal status.

The Restart flow is more delicate than Clear because it intentionally changes Activity **occurrence**:

```text
old occurrence
  -> clear / release old occurrence
  -> re-enter same Activity asset
  -> new occurrence
```

Therefore a generic “restore previous Activity” rule is unsafe after the old occurrence has actually exited.

---

## 8. Required Activity Restart semantics

### 8.1 Restart Before — non-accepted

If the required Transition Before does not complete:

```text
old Activity remains current
old occurrence remains current
beforeRestartLifecycle must not advance the restart transaction
clear is not requested
re-enter is not requested
request is non-success
normal operation gate is released
no recovery gate for a committed destination is needed
```

This is the clean pre-commit failure case.

### 8.2 Pre-clear restart stage fails

The existing `beforeRestartLifecycle` hook can stop restart before clear. This is not itself a transition failure and should remain a distinct lifecycle/pre-stage terminal.

However, any Transition After invoked solely to restore presentation after this aborted pre-stage must itself be treated truthfully. IF-TXN-02 should not silently convert a failed recovery/reveal phase into a successful restart terminal.

Do not conflate:

```text
pre-clear lifecycle/pre-stage failure
```

with:

```text
Transition Before failure
```

or:

```text
Transition After failure while trying to restore presentation.
```

### 8.3 Old occurrence cleared, re-enter not committed

This is the most important Restart-specific boundary.

If clear succeeds but re-enter fails before a new Activity occurrence becomes authoritative:

```text
old occurrence is already gone
new occurrence is not committed
actual Activity authority may be None
```

Required behavior:

```text
report actual authority
never claim old occurrence was preserved
never fabricate rollback
never synthesize a replacement occurrence
return typed restart/re-enter failure
retain only necessary scoped protection if presentation/gates require it
```

This is why IF-TXN-02 cannot be implemented as “copy IF-TXN-01 Activity Request failure return values into Restart”.

### 8.4 New occurrence committed, After/reveal non-accepted

If re-enter completes and a new occurrence is authoritative, but Transition After does not complete:

```text
new Activity occurrence remains authoritative
request/restart is non-success
no rollback to old occurrence
reveal/presentation recovery protection remains/applies
terminal evidence identifies transition failure, not readiness failure
```

This is analogous to IF-TXN-01 committed-target reveal failure, but must preserve the **new occurrence identity**.

### 8.5 Restart + readiness

When restart re-enters an Activity whose entry policy requires readiness, existing readiness semantics must remain distinct:

```text
readiness terminal failure
  != transition terminal failure
```

Do not route a transition `Failed/Rejected/Cancelled` through readiness failure kinds merely because restart ultimately re-enters an Activity.

Likewise do not report a readiness failure as a transition failure.

---

## 9. Authority matrix

| Operation | Failure point | Lifecycle side effect allowed? | Resulting Activity authority | Required terminal meaning |
|---|---|---:|---|---|
| Clear | Before non-accepted | No clear | Previous Activity + occurrence | Pre-commit transition failure |
| Clear | Clear lifecycle fails before commit | Depends on current lifecycle contract; report actual state | Actual current authority | Clear lifecycle failure |
| Clear | After non-accepted after clear commit | Clear already committed | **None** | Committed-clear/post-commit presentation failure |
| Restart | Before non-accepted | No clear/re-enter | Previous Activity + old occurrence | Pre-commit restart transition failure |
| Restart | Pre-clear hook fails | No clear/re-enter | Previous Activity + old occurrence | Pre-clear restart-stage failure |
| Restart | Clear succeeds, re-enter fails | Old occurrence already released | Actual state, commonly **None** | Restart re-enter/lifecycle failure |
| Restart | New occurrence committed, After non-accepted | Re-enter already committed | Target Activity + **new occurrence** | Committed re-entry reveal failure |
| Restart | Readiness fails after re-entry | New occurrence may remain committed per readiness contract | Actual committed occurrence | Readiness terminal, not transition terminal |

---

## 10. Result-contract requirements

IF-TXN-02 should provide enough typed result evidence to determine what actually happened without parsing log text.

At minimum the existing/resulting contracts should make the following recoverable either directly or through nested flow results/diagnostics:

```text
operation kind: ActivityClear or Restart stage
source / reason
previous Activity
previous occurrence
resulting/current Activity or None
resulting/current occurrence or invalid/None
Transition Before result
Transition After result when executed
whether clear lifecycle started/completed
whether old occurrence was released
whether re-entry started/completed
whether new occurrence committed
readiness terminal when relevant
transition gate diagnostics
recovery protection/gate state when relevant
normal gate release state
final success/failure kind
```

### Required invariant

```text
A terminal result must describe the actual authority that exists after the operation,
not the authority the operation intended to have.
```

### No false success

The following must never be true:

```text
required transition phase Failed/Rejected/Cancelled
AND
Clear/Restart final result == normal success
```

### Skipped remains narrow

`TransitionStatus.Skipped` may remain accepted only for the legitimate policy/no-visual semantics already established by IF-TXN-01.

A required transition failure must never be converted into `Skipped` merely to keep Clear/Restart moving.

---

## 11. Recovery/gate semantics

IF-TXN-02 should reuse existing gate infrastructure when the ownership semantics match, but should not force a target-owned recovery gate onto a state with no target Activity.

### Clear

After a committed Clear + failed presentation phase:

```text
Activity authority = None
```

Therefore any recovery protection must be scoped to the actual operation/presentation condition. It must **not** invent an Activity owner just to reuse `CommittedTargetRevealRecoveryGatePolicy`.

If the existing recovery policy cannot represent “clear committed, no Activity target”, add only the minimal policy/owner representation required for that case.

### Restart

After new occurrence commit + failed reveal:

```text
new occurrence is the natural recovery owner
```

Reusing the committed-target reveal recovery mechanism is reasonable if its ownership and release conditions exactly match the new occurrence semantics.

### Out of scope for this cut

A broader audit of:

```text
gate-release failures
consumer hook exceptions in every path
disposal during partial presentation
generic compensation
retry orchestration
```

remains a separate residual unless a concrete Clear/Restart path cannot be made terminally correct without addressing one of them.

---

## 12. Why this is a separate cut from IF-TXN-01

IF-TXN-01 deliberately established the transition authority rule first on the canonical navigation paths:

```text
Startup
Route Request
Activity Request
```

Clear/Restart deserve a separate cut because:

1. **Clear commits to “no Activity”, not a target Activity.**
2. **Restart crosses two lifecycle stages and replaces an occurrence.**
3. **A restart can lose the old occurrence before the new occurrence exists.**
4. **Blind rollback would be especially dangerous after old occurrence release.**
5. **Result vocabulary that is correct for a committed target may be semantically false for Clear.**

The separate cut keeps the transaction model explicit rather than over-generalizing the first implementation.

---

## 13. Rejected alternatives

### A. Ignore transition result and keep it as diagnostics

Rejected.

That is the exact integrity gap IF-TXN-01 was created to remove.

### B. Treat any failed Clear/Restart transition as success with warning

Rejected.

`CompletedWithWarnings` is already a typed accepted terminal. `Failed`, `Rejected`, and `Cancelled` must retain their meaning.

### C. Automatically restore/re-enter the previous Activity after Clear failure

Rejected.

After clear commit, the previous occurrence has been released. Re-entering it would create new lifecycle work and potentially a new occurrence; it is not rollback.

### D. Generic rollback manager

Rejected.

There is no demonstrated need for a generic compensation subsystem. The correct authority state can be retained explicitly without pretending irreversible side effects did not happen.

### E. Generic transaction/service singleton

Rejected.

The existing scoped GameFlow/RouteLifecycle authority is sufficient. No implicit global manager is justified.

### F. Move technical certification into FIRSTGAME

Rejected.

Failure injection and transaction-contract regression belong in QAFramework. FIRSTGAME may prove the normal consumer path later.

---

## 14. Proposed implementation cut

# IF-TXN-02 — Activity Clear/Restart Transition Terminal Integrity

### Type

```text
Technical / runtime contract / transaction integrity / QA regression
```

### Objective

Make transition terminal results authoritative for Activity Clear and Restart without inventing rollback, while preserving actual Activity/occurrence authority after partial lifecycle progress.

### In scope

```text
Activity Clear Transition Before authority
Activity Clear Transition After authority
Activity Restart Transition Before authority
Activity Restart Transition After/reveal authority
actual authority reporting after clear/re-enter partial progress
minimal typed terminal/result vocabulary needed for truthful results
minimal scoped recovery-gate support when required
QAFramework synthetic regression covering negative terminals
ADR/tracker update after behavior is implemented and proven
```

### Out of scope

```text
generic transaction framework
generic rollback/compensation manager
automatic retry
new global manager/singleton/service locator
redesign of TransitionEffectOrchestrator
unrelated Route/Startup/Activity Request semantics already closed by IF-TXN-01
full gate-release-failure audit
full disposal/partial-presentation audit
M07 Player work
FIRSTGAME feature redesign
```

---

## 15. Files expected to be inspected/affected

Codex must inspect the current workspace before deciding exact paths. Do not assume this list is exhaustive or that a new file is always needed.

### Package — expected inspection

```text
Runtime/GameFlow/GameFlowRuntime.cs
Runtime/GameFlow/GameFlowRuntime.TransitionFailureAuthority.cs
Runtime/GameFlow/FrameworkActivityRequestKind.cs
Runtime/GameFlow/FrameworkActivityRequestResult.cs
existing FrameworkActivityRestartFlowResult definition
existing transition diagnostics/result contracts
existing committed-target reveal recovery gate policy
ADR001 / ADR006 and current transaction/readiness documentation
```

Possible implementation may use an additional focused partial/helper file if it materially improves clarity, but do not create abstraction layers without need.

### QAFramework — expected new canonical regression

Recommended shape:

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaIfTxn02ActivityClearRestartTransitionIntegrityRegression.cs
```

Name/path should follow the actual current QA canonical pattern found in the workspace.

### Removed files

```text
None expected.
```

If obsolete temporary package-local tests/runners are discovered for this cut, do not remove them without reporting why.

---

## 16. QA plan

Technical certification belongs in QAFramework.

Recommended regression family:

```text
QaIfTxn02ActivityClearRestartTransitionIntegrityRegression
```

Do not hard-code a final case count in the contract; derive the exact test set from the current APIs.

### Minimum Clear evidence

```text
1. accepted Before allows clear lifecycle
2. Failed Before blocks clear and preserves Activity/occurrence
3. Rejected Before blocks clear
4. Cancelled Before blocks clear
5. invalid Before blocks clear
6. CompletedWithWarnings accepted
7. legitimate policy/no-visual Skipped accepted
8. required failure is not masked as Skipped
9. committed clear + failed After => Activity None, non-success
10. committed clear + rejected/cancelled After => same authority invariant
11. no blind restoration/re-enter
12. gate/recovery ownership is truthful
```

### Minimum Restart evidence

```text
1. Failed Before => same Activity and same old occurrence
2. Rejected/Cancelled Before => no clear/re-enter
3. successful restart => same Activity asset, new occurrence
4. pre-clear stage failure remains distinct from transition failure
5. clear succeeds + re-enter fails => old occurrence not fabricated as current
6. actual authority after failed re-enter is reported truthfully
7. new occurrence commits + failed After => new occurrence remains current
8. failed After => restart is not Completed
9. readiness failure remains readiness failure, not transition failure
10. old occurrence cannot release/corrupt the new occurrence after supersession
11. CompletedWithWarnings accepted
12. legitimate Skipped accepted only under established policy/no-visual semantics
13. no duplicate normal completion after a non-accepted phase
14. cleanup leaves QA host in a deterministic baseline
```

### Regression reruns

After the new IF-TXN-02 regression passes, rerun at minimum:

```text
QaIfTxn01TransitionFailureAuthorityRegression
QaDirectActivityReadinessPoliciesRegression
QaParticipantAwareReadinessLoadingProgressRegression
QaParticipantAwareReadinessLoadingTerminalRegression
```

If QAFramework already has canonical Activity Restart regressions, run those as well.

---

## 17. FIRSTGAME validation

FIRSTGAME is **not required to inject transition failures** for IF-TXN-02 certification.

After package + QA are green, a normal consumer Restart/Clear happy path in FIRSTGAME is useful only as a no-regression/usability check if that flow already exists.

Do not add synthetic QA scaffolding to FIRSTGAME.

---

## 18. Technical acceptance criteria

IF-TXN-02 is technically accepted when all are true:

```text
[ ] package compiles in Unity 6.5
[ ] Clear Before non-accepted cannot start clear lifecycle
[ ] Clear After non-accepted after successful clear cannot return normal success
[ ] committed Clear preserves None as actual Activity authority
[ ] no blind restoration of previous Activity after committed Clear
[ ] Restart Before non-accepted preserves old occurrence and performs no clear
[ ] restart failure after old occurrence release reports actual authority
[ ] restart After failure after new occurrence commit preserves new occurrence authority
[ ] transition failure and readiness failure remain typed/distinct
[ ] CompletedWithWarnings remains accepted
[ ] legitimate policy/no-visual Skipped remains accepted
[ ] Failed/Rejected/Cancelled are never silently converted into success/Skipped
[ ] gate/recovery ownership is scoped and diagnostic
[ ] no new singleton/service locator/global manager
[ ] QA regression lives in QAFramework, not package runtime
[ ] new IF-TXN-02 regression passes
[ ] IF-TXN-01 regression remains green
[ ] readiness policy/progress/terminal regressions remain green when affected
[ ] ADR/tracker accurately state what was closed and what remains
```

---

## 19. Product acceptance criteria

This is primarily a technical cut; it should be intentionally invisible to normal authoring UX.

Product acceptance:

```text
[ ] existing Clear/Restart API usage remains understandable
[ ] no new manual internal contract assembly is imposed on consumers
[ ] no new designer-facing setting is required solely to handle transaction failures
[ ] failure diagnostics identify operation, phase, actual authority and recovery state
[ ] FIRSTGAME normal happy path, when exercised, does not require consumer workaround
```

---

## 20. Architectural gain

Before IF-TXN-02:

```text
transition authority is canonical for Start/Route/Activity Request,
but Clear/Restart can still treat transition terminals as observational diagnostics.
```

After IF-TXN-02:

```text
GameFlow transition authority becomes consistent across the complete Activity operation family:
  Start/Request
  Clear
  Restart

while preserving operation-specific commit semantics.
```

The architectural gain is not “more abstraction”. It is a stronger invariant:

```text
GameFlow never reports success after a required transition terminal says the transaction phase did not complete.
```

and:

```text
Failure results preserve actual runtime authority after irreversible lifecycle side effects.
```

---

## 21. Usability gain

The user/developer no longer has to reason about a hidden exception where:

```text
Activity Request respects transition failure,
but Clear/Restart may continue anyway.
```

Diagnostics become actionable because a failure result tells the truth about whether:

```text
old Activity is still active,
Activity was already cleared,
or a new restart occurrence is authoritative but unrevealed.
```

This removes a particularly dangerous class of “the operation says success but the presentation failed” behavior.

---

## 22. Remaining residuals after IF-TXN-02

Do not silently absorb these into this cut:

```text
gate-release failure integrity
consumer hook exception policy beyond concrete Clear/Restart needs
disposal during partial presentation
cleanup evidence after exceptional presentation paths
broader compensation vocabulary only if a real terminal path demands it
```

After IF-TXN-02, these should be re-audited as a separate transaction-hardening cut rather than assumed solved.

---

## 23. Suggested commit message

```text
IF-TXN-02 enforce Activity Clear/Restart transition terminal integrity
```

Alternative:

```text
GameFlow: make Clear/Restart transition failures authoritative
```

---

## 24. Closure statement

The audit finds a **real, bounded consistency gap**, not a need for a new transaction subsystem.

The correct next step is:

```text
IF-TXN-02
  extend the already-proven IF-TXN-01 transition authority invariant
  to Activity Clear and Activity Restart,
  while treating their actual commit boundaries explicitly.
```

The most important non-negotiable rule for the implementation is:

```text
Never manufacture rollback authority.
After side effects occur, report and protect the authority that actually exists.
```
