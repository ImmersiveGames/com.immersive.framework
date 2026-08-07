# Immersive Framework — IF-ADR-001 + IF-ADR-006 Transaction Integrity Audit

**Date:** 2026-08-07  
**Package baseline:** `85c059b56b16c2853d32eaa000c8b58c117a88b6` — `Active readiness-waitCover`  
**Scope:** IF-ADR-001 — Core Lifecycle and Runtime Authority + IF-ADR-006 — Loading, Transition, Persistence and Diagnostics  
**Audit type:** focused technical/architectural audit  
**Goal:** identify the highest-risk concrete integrity gap and define exactly one Recommended First Cut.

---

# 1. Executive Summary

The audit found a **real transaction-integrity gap**, not only missing QA.

The current package already has comparatively strong handling for one important post-commit case:

```text
target Route/Activity committed
→ Activity readiness does not reach Ready
→ committed target remains authoritative
→ request returns typed committed-target readiness failure
→ recovery gate can remain applied
```

This is the correct direction and should be preserved.

However, the same level of authority is **not applied to Transition phase results**.

The current transition implementation explicitly returns a passive immutable `TransitionResult`. Required presentation failures such as:

```text
required Transition surface missing
adapter blocks/fails
```

produce:

```text
TransitionStatus.Failed
```

without throwing.

`GameFlowRuntime` awaits and stores the `TransitionResult` for `Before` and `After`, but the audited flows do not make continuation/terminal decisions from `TransitionResult.Completed`.

Consequences:

## Before / cover failure

Current causal risk:

```text
Transition Before
→ required visual surface fails
→ TransitionResult = Failed
→ GameFlow continues
→ Route/Activity lifecycle may run
→ target may commit
```

So an authored required transition can fail before the destination lifecycle starts, yet destination authority can still advance.

## After / reveal failure

Current causal risk:

```text
target already committed
→ Activity readiness Ready
→ Loading may reach terminal success
→ Transition After / reveal fails
→ TransitionResult = Failed
→ GameFlow can still release normal gate
→ request can still return Succeeded
```

So the target can be committed but not safely revealed, while the outer request reports normal success.

This is precisely the unresolved intersection described by IF-ADR-001 and IF-ADR-006:

```text
authority commit
transition phase
partial commit
reveal failure
compensation/recovery
terminal evidence
```

## Root classification

**High-risk architectural defect:**

```text
Transition outcome is diagnostic evidence,
but GameFlow transaction authority does not consume required Transition failure
as a transaction terminal.
```

This does **not** mean Transition should own Route/Activity lifecycle.

The correct authority remains:

```text
Transition surface
  reports typed phase result

GameFlow
  interprets the result
  and decides whether the transaction may continue
```

## Recommended First Cut

Implement one package technical cut:

```text
IF-TXN-01 — GameFlow Transition Failure Authority
```

The cut should make `Before` and `After` transition failures authoritative at the GameFlow orchestration boundary:

```text
Before failure
→ abort before destination lifecycle/commit
→ preserve previous authority
→ typed pre-commit transition failure

After failure
→ do not rollback committed destination blindly
→ keep committed target authoritative
→ do not return Succeeded
→ retain/apply recovery protection
→ typed committed-target reveal/transition failure
```

No broad transaction rewrite is recommended yet.

---

# 2. Scope and Audit Questions

This audit intentionally did **not** re-audit the entire framework.

It followed only these questions:

```text
1. Where does GameFlow acquire transition/gate state?
2. What happens when Transition Before fails?
3. When does Route/Activity authority advance?
4. What happens after target commit if readiness fails?
5. What happens when Transition After/reveal fails?
6. Can a failure be reported only as diagnostics while the request succeeds?
7. Is Loading allowed to become the authority accidentally?
8. Does gate cleanup distinguish safe success from committed-target recovery?
9. What is the smallest cut that restores transaction integrity?
```

Primary inspected areas:

```text
Runtime/GameFlow/GameFlowRuntime.cs
Runtime/GameFlow/GameFlowRuntime.ActivityEntryReadinessOrchestration.cs
Runtime/GameFlow/GameFlowRuntime.ActivityEntryLoadingProgress.cs

Runtime/GameFlow/FrameworkActivityRequestKind.cs
Runtime/GameFlow/FrameworkRouteRequestKind.cs
Runtime/GameFlow/FrameworkTransitionDiagnostics.cs

Runtime/Transition/TransitionStatus.cs
Runtime/Transition/TransitionResult.cs
Runtime/Transition/TransitionEffectOrchestrator.cs

Documentation~/Architecture/ADRs/
  IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md
  IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md
```

---

# 3. Current Transaction Model

The current architecture is broadly shaped as:

```text
Request Route/Activity
        ↓
validate request/configuration
        ↓
apply transition gate
        ↓
Transition Before
        ↓
before-lifecycle presentation/loading hook
        ↓
Route/Activity lifecycle
        ↓
destination authority may commit
        ↓
Activity readiness wait when configured
        ↓
Loading terminal projection
        ↓
after-lifecycle presentation/loading hook
        ↓
Transition After
        ↓
release normal transition gate
        ↓
publish outer request terminal
```

The architecture intentionally separates:

```text
lifecycle authority
presentation
readiness
loading
capability gate
```

That separation is correct.

The problem is not separation itself.

The problem is that the **orchestration boundary lacks a rule saying which transition outcomes permit the transaction to continue**.

---

# 4. Authority and Commit Boundaries

## 4.1 Correct authority model

IF-ADR-001 defines:

```text
Game Application / Session
  → Route
    → Activity
```

with Route/Activity owning contextual lifecycle and Session authorities outliving them where appropriate.

`FrameworkRuntimeHost` remains the composition root without a global current-host registry.

No issue found in this audit requires changing that authority model.

## 4.2 Transition is not lifecycle authority

`TransitionEffectOrchestrator` explicitly states that it owns no:

```text
Route
Activity
scene lifecycle
```

It maps transition phases to effect requests and returns evidence.

This is correct.

## 4.3 GameFlow is the decision boundary

Therefore the canonical decision must be:

```text
Transition
  returns typed outcome

GameFlow
  decides:
    continue
    abort before commit
    preserve committed target and enter recovery
```

The current code records the result but does not consistently make that decision.

---

# 5. Transition Result Semantics

`TransitionStatus` includes:

```text
Planned
Running
Observed
Skipped
Succeeded
CompletedWithWarnings
Failed
Rejected
Cancelled
```

`TransitionResult` exposes:

```text
Succeeded
CompletedWithWarnings
Failed
Rejected
Cancelled
Completed
```

where:

```text
Completed =
  Succeeded
  OR CompletedWithWarnings
```

The type documentation also explicitly classifies `TransitionResult` as:

```text
passive immutable result
diagnostics only
```

That is acceptable for the Transition subsystem.

It becomes unsafe only if its caller assumes:

```text
await completed
=
transition succeeded
```

because the async method can complete normally while returning:

```text
TransitionStatus.Failed
```

---

# 6. Confirmed Failure Production

`TransitionEffectOrchestrator` can return failure without throwing.

## Missing required surface

If required authoring cannot be satisfied:

```text
evaluation.IsAllowed == false
```

or no supporting adapter exists:

```text
matchingAdapters.Count == 0
```

it returns a failed transition result such as:

```text
FailedRequiredUnitySurfaceMissing
```

## Adapter execution failure

If one or more adapters block the transition:

```text
result.BlocksTransition
→ blockingIssueCount > 0
```

the orchestrator returns:

```text
TransitionResult.FailedResult(...)
```

The caller therefore must explicitly inspect the returned status.

This is not an exceptional CLR failure path.

---

# 7. Transition Before — Confirmed Integrity Gap

## 7.1 Intended semantic role

For covered/fade flows, `Before` establishes the transition presentation state before destination lifecycle proceeds.

Conceptually:

```text
acquire gate
→ establish cover/presentation
→ mutate destination lifecycle
```

## 7.2 Current behavior

The audited GameFlow paths await `ExecuteTransitionAsync(Before(...))`.

The result is either stored or ignored by the immediate call site.

There is no corresponding mandatory gate such as:

```text
if (!transitionBeforeResult.Completed)
{
    abort before lifecycle;
}
```

## 7.3 Consequence

A required Transition surface can fail while GameFlow continues toward:

```text
beforeRouteLifecycle / beforeActivityLifecycle
Route/Activity lifecycle execution
destination materialization
authority commit
readiness
```

This creates a mismatch:

```text
authored presentation requirement = failed
outer lifecycle transaction = still progressing
```

## 7.4 Why this is high risk

This is not merely cosmetic.

A required cover can be part of the safety/product contract for:

```text
preventing invalid intermediate visuals
retaining user interaction
coordinating Loading
maintaining transition gate expectations
```

If that required phase fails, continuing the destination mutation makes recovery ambiguous.

## 7.5 Correct terminal category

A `Before` Transition failure occurs **before destination authority should advance**.

Therefore it belongs to a pre-commit terminal class:

```text
FailedBeforeCommitTransition
```

or equivalent precise vocabulary.

The exact enum name is implementation detail; the semantic distinction is mandatory.

---

# 8. Destination Lifecycle and Commit

The audit did not find evidence requiring a rewrite of Route/Activity lifecycle authority.

The important distinction is:

```text
Before failure
  destination should not be allowed to reach lifecycle/commit

versus

failure after destination commit
  destination cannot be rolled back casually
```

That distinction already exists for Activity readiness failures and is a useful precedent.

---

# 9. Readiness Post-Commit Recovery — Existing Strong Behavior

The current Activity Entry Readiness orchestration already distinguishes:

```text
target committed
+
readiness did not finish successfully
```

from ordinary pre-commit rejection.

Current request kinds include committed-target readiness terminals such as:

```text
FailedCommittedTargetNotReady
FailedCommittedTargetReadinessInvalidated
FailedCommittedTargetReadinessCancelled
SupersededCommittedTargetByRouteReplacement
```

The runtime also supports a dedicated recovery-gate path.

This is architecturally important because it establishes the correct rule:

> Once the target is the current lifecycle authority, failure handling should preserve that fact instead of pretending the old destination is automatically current again.

This exact principle should be extended to post-commit Transition/reveal failure.

---

# 10. Loading Ordering

The participant-aware Loading path is generally correct in one important sense:

```text
Loading terminal readiness completion
does not define Activity authority.
```

However, the startup readiness path shows this sequence:

```text
readiness Ready
→ report/ensure terminal Loading completion
→ optional after-lifecycle loading hide
→ Execute Transition After
→ mark revealCompleted
```

Therefore a later `Transition After` failure can happen **after Loading has legitimately reached terminal success for readiness**.

This is not itself a Loading bug.

It means the outer transaction needs richer terminal semantics:

```text
technical/readiness preparation completed
but final reveal/presentation failed
```

Loading must not be asked to rewrite lifecycle truth.

The GameFlow request must carry the failure.

---

# 11. Transition After / Reveal — Confirmed Integrity Gap

## 11.1 Current position in transaction

By the time `After` runs in the readiness path:

```text
target Route/Activity is committed
readiness may already be Ready
Loading may already be terminal
```

## 11.2 Current behavior

`ExecuteTransitionAsync(After(...))` can return:

```text
TransitionResult.Failed
```

without throwing.

The audited paths do not promote that returned failure to a failed GameFlow terminal.

In the startup readiness flow specifically, after awaiting the transition the code proceeds to:

```text
revealCompleted = true
```

and later:

```text
ReleaseTransitionGate(...)
```

before returning normal startup success when readiness itself succeeded.

## 11.3 Consequence

The framework can conceptually reach:

```text
destination committed
readiness Ready
Loading terminal success
Transition reveal failed
normal transition gate released
outer GameFlow result = success
```

This is the highest-risk concrete finding in the audit.

## 11.4 Correct recovery semantics

Because the target is already committed:

```text
do not rollback blindly
do not restore previous Route/Activity as if commit never happened
```

Instead:

```text
target remains authoritative
request returns typed committed-target transition/reveal failure
normal gameplay release does not occur
recovery protection remains explicit
diagnostics preserve transition result
```

This mirrors the already implemented committed-target readiness model.

---

# 12. Gate Integrity

## 12.1 Existing good behavior

Readiness failure can cause a recovery gate to remain applied.

This protects a committed but unusable destination.

## 12.2 Gap for reveal failure

Transition After failure currently has no equivalent terminal classification.

Therefore the normal transition gate can be released even though the required presentation phase failed.

Risk:

```text
gameplay/input becomes available
while the authored reveal/transition contract did not complete
```

or presentation remains in an inconsistent state while the request says success.

## 12.3 Architectural direction

Do not make Transition own the gate.

GameFlow should decide:

```text
successful final presentation
→ normal gate release

committed-target reveal failure
→ retain/apply recovery gate according to explicit policy
```

---

# 13. Cancellation, Supersession and Disposal

## 13.1 Supersession

Current readiness orchestration has explicit typed supersession for Route authority replacement.

This should remain separate from failure.

## 13.2 Cancellation

Readiness cancellation also has a typed committed-target path.

Transition status itself supports `Cancelled`, but this audit did not find sufficient evidence to claim that all transition cancellation paths are already elevated to GameFlow terminals.

That should be handled by the same outcome interpretation introduced by the first cut, without broadening into a full cancellation redesign.

## 13.3 Disposal

`finally` cleanup exists around active operations and transition gate state.

That is useful defense, but cleanup in `finally` is not equivalent to correct transaction semantics.

A transaction can clean up a handle and still report the wrong authority/result.

---

# 14. Terminal Integrity Matrix

## Current observed model

| Phase | Failure source | Authority state | Current risk | Required semantic |
|---|---|---|---|---|
| Validation | invalid config | previous authority | already rejected | pre-commit rejection |
| Gate acquire/config | invalid gate/config | previous authority | mostly explicit | pre-commit rejection |
| **Transition Before** | **required surface/adapter failure** | **previous authority should remain** | **failure result may be ignored; lifecycle continues** | **abort pre-commit** |
| Route/Activity lifecycle | lifecycle failure | depends on lifecycle result | typed result exists | preserve lifecycle contract |
| Activity readiness | non-Ready terminal after commit | target committed | strong recovery path exists | committed-target failure |
| Loading progress | reporter/projection | presentation only | should not own lifecycle | diagnostic/projection |
| after-lifecycle hook | consumer/package callback exception | target may be committed | exception path, needs correlated evidence | later hardening |
| **Transition After / reveal** | **required surface/adapter failure** | **target committed** | **failure result may be ignored; gate/request may succeed** | **committed-target reveal failure** |
| Recovery | cleanup/gate release failure | target depends on commit | incomplete proof | explicit recovery evidence |
| Supersession | newer Route authority | authority replaced | typed readiness path exists | typed supersession |

---

# 15. Consolidated Findings

## Confirmed

### IF-TXN-AUD-001 — TransitionResult is intentionally passive

`TransitionResult` contains rich typed evidence but does not mutate lifecycle or release gates.

**Status:** CONFIRMED

This is valid subsystem design.

---

### IF-TXN-AUD-002 — Required transition failures are returned, not necessarily thrown

Missing required Unity transition surfaces and blocking adapter failures return `TransitionStatus.Failed`.

**Status:** CONFIRMED

---

### IF-TXN-AUD-003 — GameFlow does not consistently gate continuation on TransitionResult.Completed

Audited `Before`/`After` call paths await transition execution without using the returned failure as a mandatory transaction terminal.

**Status:** CONFIRMED

---

### IF-TXN-AUD-004 — Before failure can be followed by destination lifecycle work

Because the returned transition failure is not promoted to an abort, lifecycle/commit can continue after failure to establish the authored transition phase.

**Status:** CONFIRMED causal consequence

---

### IF-TXN-AUD-005 — Current request kind vocabulary lacks Transition-phase transaction failures

`FrameworkRouteRequestKind` and `FrameworkActivityRequestKind` contain readiness committed-target failures but no equivalent:

```text
pre-commit Transition failure
committed-target reveal failure
```

**Status:** CONFIRMED

---

### IF-TXN-AUD-006 — Committed-target readiness recovery is the correct precedent

The package already preserves committed destination authority and applies recovery gating when initial readiness terminates unsuccessfully.

**Status:** CONFIRMED

---

### IF-TXN-AUD-007 — After/reveal failure can remain only diagnostic while the outer request progresses as success

A failed `TransitionResult` does not automatically throw and is not promoted to a failed GameFlow result in the audited sequence.

**Status:** CONFIRMED

---

### IF-TXN-AUD-008 — Normal gate release is not currently conditional on successful Transition After

The post-readiness sequence can proceed to normal transition-gate release after awaiting the `After` operation without checking that the result completed.

**Status:** CONFIRMED

---

### IF-TXN-AUD-009 — Loading is not the root defect

Loading can legitimately complete its readiness projection before the final reveal phase.

The missing contract is the outer transaction terminal for reveal failure.

**Status:** CONFIRMED

---

### IF-TXN-AUD-010 — The gap directly matches open IF-ADR-001 and IF-ADR-006 work

The ADRs already identify:

```text
authority commit vocabulary
compensation
partial commit
reveal failure
recovery
terminal diagnostics
```

as remaining areas.

**Status:** CONFIRMED

---

## Not yet proven / follow-up

### IF-TXN-AUD-011 — Exact visual state after a failed adapter may vary by adapter

A failed adapter may leave the surface unchanged or partially changed depending on implementation.

**Status:** NOT GENERALIZABLE FROM CURRENT AUDIT

The transaction must therefore preserve typed adapter evidence and treat compensation as best-effort/explicit, not assume a specific visual rollback.

---

### IF-TXN-AUD-012 — Gate-release adapter failure needs separate proof after first cut

The current high-risk gap is already sufficient to justify IF-TXN-01.

A deeper gate-release failure matrix remains necessary afterward.

**Status:** FOLLOW-UP

---

# 16. Root Risk

## Problem

The framework currently has two concepts that are both individually valid:

```text
TransitionResult is passive diagnostics.
GameFlow owns lifecycle transaction authority.
```

But the bridge between them is incomplete.

The missing rule is:

> **A required Transition phase can be passive inside the Transition subsystem, but its outcome must become authoritative when GameFlow decides whether a lifecycle transaction may continue or may finish successfully.**

Without that bridge, the framework can violate its own product intent:

```text
required transition failed
but request succeeded
```

That is a high-risk false-success condition.

---

# 17. What Should Not Be Done

Do not solve this by:

```text
making TransitionEffectOrchestrator throw for every failed result
```

That would collapse typed operational failure into exception flow and lose intentional result semantics.

Do not:

```text
make Transition own Route/Activity authority
```

Do not:

```text
rollback a committed destination automatically after reveal failure
```

Do not:

```text
let Loading decide whether the Route/Activity request succeeds
```

Do not:

```text
release the recovery gate just to avoid a stuck screen
```

Do not:

```text
add timeout-to-success
```

The fix belongs in GameFlow transaction interpretation.

---

# 18. Recommended First Cut

## Cut ID

```text
IF-TXN-01
```

## Name

```text
GameFlow Transition Failure Authority
```

## Objetivo

Make required Transition phase outcomes authoritative at the GameFlow transaction boundary so:

```text
Before failure cannot advance destination authority

and

After/reveal failure cannot be reported as normal success
after destination authority has already committed.
```

## Tipo

```text
technical / architectural integrity
```

## Escopo

Implement explicit GameFlow handling for non-completed Transition outcomes in:

```text
Route requests
Activity requests
Game Application startup paths that use the same transition orchestration
```

At minimum:

### A. Before / pre-commit

```text
execute Transition Before
→ inspect TransitionResult
→ if Completed:
     continue
→ otherwise:
     do not start destination lifecycle
     preserve previous authority
     produce typed pre-commit Transition terminal
     perform explicit safe cleanup/compensation
```

### B. After / post-commit reveal

```text
target already committed
→ execute Transition After
→ inspect TransitionResult
→ if Completed:
     release normal gate
     success
→ otherwise:
     preserve target as current authority
     do not report success
     retain/apply committed-target recovery protection
     produce typed committed-target Transition/reveal terminal
```

### C. Diagnostics

Terminal evidence must retain:

```text
TransitionOperationId
scope/kind/phase
TransitionResult
destination Route/Activity identity
commit state
gate release/recovery state
cleanup result/evidence
```

## Fora de escopo

```text
full transaction state-machine rewrite
Activity Readiness semantic changes
WaitCovered semantic changes
Loading progress percentage redesign
Player participation changes
Actor lifecycle changes
Camera behavior
Pause/Reset behavior
new service locator/global manager
automatic visual fallback
generic retry system
timeout-to-success
Session-Persistent Player
```

## Projeto responsável

Primary:

```text
com.immersive.framework
```

Validation sequence:

```text
1. package
2. QAFramework
3. FIRSTGAME only for real integration proof
```

## Arquivos provavelmente afetados

Package:

```text
Runtime/GameFlow/GameFlowRuntime.cs

Runtime/GameFlow/
  FrameworkRouteRequestKind.cs
  FrameworkRouteRequestResult.cs
  FrameworkActivityRequestKind.cs
  FrameworkActivityRequestResult.cs

Runtime/GameFlow/GameFlowRuntime.ActivityEntryLoadingProgress.cs
  if startup/loading-specific outcome propagation requires alignment

Runtime/GameFlow/
  recovery-gate policy/helper
  only if the current readiness-specific helper cannot be reused cleanly

Documentation~/Architecture/ADRs/
  IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md
  IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md
```

A new small typed terminal/phase enum is acceptable if it prevents overloading readiness-specific result kinds.

Do not add a broad transaction framework unless implementation evidence proves it necessary.

## Superfície de produto afetada

Indirect but important:

```text
Route transition reliability
Activity transition reliability
Loading/cover failure diagnostics
Advanced/Debug terminal evidence
```

Normal successful authoring flow should not become more complex.

Invalid runtime/presentation states should become more explicit.

## Fluxo esperado

### Success

```text
Gate acquire
→ Before Completed
→ lifecycle
→ target commit
→ readiness
→ Loading
→ After Completed
→ gate release
→ Succeeded
```

### Before failure

```text
Gate acquire
→ Before Failed
→ destination lifecycle NOT started
→ previous authority remains
→ cleanup/compensation
→ gate safe
→ FailedBeforeCommitTransition
```

### After failure

```text
Gate acquire
→ Before Completed
→ lifecycle
→ target committed
→ readiness Ready
→ Loading terminal
→ After Failed
→ target remains authoritative
→ normal gameplay release NOT treated as success
→ recovery protection retained/applied
→ FailedCommittedTargetTransition/Reveal
```

## QA necessário

Create one focused canonical regression family.

### QA-1 — Required surface missing on Before

Prove:

```text
Transition Before = Failed
destination Route/Activity lifecycle not invoked
current authority unchanged
no destination occurrence committed
request not Succeeded
gate cleanup deterministic
```

### QA-2 — Adapter blocks Before

Same assertions with an explicit failing adapter.

### QA-3 — Required surface/adapter fails on After

Prove:

```text
destination committed
readiness can be Ready
Transition After = Failed
destination remains current
request not Succeeded
recovery gate retained/applied
no false revealCompleted
```

### QA-4 — CompletedWithWarnings

Prove that:

```text
CompletedWithWarnings
```

remains a completed phase and does not become false failure.

### QA-5 — Existing readiness failure unaffected

Prove existing:

```text
FailedCommittedTargetNotReady
Cancelled
Invalidated
Superseded
```

semantics remain intact.

### QA-6 — Repeated execution / cleanup

Execute failures repeatedly and prove:

```text
no gate leak
no stale transition operation
no stale Loading reporter/forwarder
no authority drift
```

## FIRSTGAME necessário

Not required to define the package contract.

After package + QA are green, one real integration proof should intentionally make the required transition surface unavailable or use a controlled failing test adapter/sample and verify:

```text
Before failure does not enter destination

After failure does not expose gameplay as successful
and diagnostics clearly show committed target + reveal failure
```

Do not leave a deliberately broken surface in normal FIRSTGAME product state.

## Critérios de aceite técnico

```text
A failed Before Transition cannot start destination lifecycle.
A failed Before Transition cannot change Route/Activity authority.
A failed Before Transition returns typed non-success evidence.

A failed After Transition cannot return normal request success.
A failed After Transition does not blindly rollback committed authority.
A failed After Transition retains/applies recovery protection.
Normal gameplay gate release only occurs after accepted final presentation terminal.

CompletedWithWarnings remains accepted as completed.
Transition failure remains typed operational evidence, not forced exception flow.
Existing readiness failure/supersession behavior is preserved.
Loading remains presentation/progress, not lifecycle authority.
No silent fallback is introduced.
No global authority is introduced.
All terminal paths expose correlated operation/destination/cleanup evidence.
```

## Critérios de aceite de produto

```text
A required Transition failure is visible as a real operation failure.
The Inspector/authoring model does not silently downgrade required presentation.
Advanced/Debug can distinguish:
  pre-commit transition failure
  committed-target readiness failure
  committed-target reveal failure
  supersession

A user does not see “request succeeded” when required reveal failed.
Recovery state is diagnosable and actionable.
```

## Ganho arquitetural

Closes the authority bridge:

```text
Transition
  reports outcome

GameFlow
  owns transaction decision

Route/Activity
  retain lifecycle authority

Loading
  remains projection

Gate
  remains protection
```

It also establishes the missing distinction:

```text
pre-commit failure
vs
post-commit recovery failure
```

without creating a new global transaction manager.

## Ganho de usabilidade

Transforms a difficult false-success bug into explicit product evidence.

Instead of:

```text
screen/gate looks wrong
but request says success
```

the consumer gets:

```text
destination not committed because cover failed
```

or:

```text
destination committed, but final reveal failed and recovery protection remains
```

That materially improves debugging and prevents invalid gameplay exposure.

## Commit message sugerida

```text
fix(gameflow): make transition failures authoritative across commit boundaries
```

---

# 19. Follow-up After IF-TXN-01

Only after IF-TXN-01 is proven should the broader ADR-001/006 transaction matrix be reopened for the next cut.

Likely follow-up topics:

```text
gate-release failure
consumer/loading hook exception after commit
disposal during partial presentation
adapter partial-side-effect compensation
full terminal cleanup receipts
```

Those are real concerns, but they should not be mixed into IF-TXN-01.

The current defect is already narrow, reproducible from source semantics, and high enough risk to justify the first cut independently.

---

# 20. Final Conclusion

The highest-risk unresolved point between IF-ADR-001 and IF-ADR-006 is now concrete:

```text
Transition failure is represented,
but not yet authoritative for the surrounding GameFlow transaction.
```

The framework already has the correct architectural precedent for committed-target readiness failures.

The next cut should extend that principle to Transition phases:

```text
Before failure
  stops before authority commit.

After failure
  preserves committed authority,
  enters explicit recovery,
  and cannot report normal success.
```

This is the recommended first cut because it fixes a real false-success/authority-integrity defect while remaining small enough to prove rigorously in package + QA before broadening transaction compensation work.
