# Immersive Framework — ADR-006 Reconciliation

**Date:** 2026-08-10  
**QA evidence update:** 2026-08-11  
**Type:** technical reconciliation and Stage A closure record  
**ADR:** IF-ADR-006 — Loading, Transition, Persistence and Diagnostics

## Objective

Reconcile IF-ADR-006 against the official package, separate implementation gaps
from evidence gaps, and record the focused QA evidence required for Stage A
closure.

The 2026-08-11 update records the completed focused technical evidence for the
exceptional transaction, presentation-policy, supersession, loading/readiness,
gate/recovery and cleanup cases. All eight Stage A QA cases now have deterministic
executed evidence against the current package/QA line, so the accepted ADR-006
technical boundary is certified.

## Source baselines

Original source reconciliation:

```text
com.immersive.framework
  branch: master
  commit: f34eb059254287e13a0ab48f9ecab8bda072744c
  access: read-only inspection
```

Current documentation / package line inspected for this update:

```text
com.immersive.framework
  branch: master
  commit: c0003445a95baecf54ada1d76a718d1118617c29
  message: docs(architecture): reconcile ADR-006 loading transition boundary
  access: read-only inspection

QAFramework
  branch: main
  commit: ba1529e653b25c1215d4f741cd3611b7f280bc49
  message: qa: add ADR-006 transaction behavioral closure regression
  access: read-only inspection
```

The package source remains the official implementation authority. QAFramework
owns synthetic and negative technical proof. FIRSTGAME remains Stage B consumer
evidence and is not the Stage A exceptional-path laboratory.

## Scope

This reconciliation covers:

- Transition `Before` / `After` transaction continuation;
- committed authority after post-commit presentation failure;
- typed supersession versus ordinary failure;
- technical Loading progress versus readiness-governed terminal completion;
- pure Transition Gate versus readiness/reveal recovery protection;
- required presentation failure versus explicit optional/NoOp presentation;
- transition/loading diagnostics and cleanup semantics.

## Out of scope

This reconciliation does not introduce or require:

- a new runtime architecture;
- a generic rollback or compensation manager;
- readiness authority inside Loading;
- Route or Activity authority inside Transition;
- new global managers, service locators or implicit runtime lookup;
- broad Transition/Loading UX redesign;
- FIRSTGAME implementation or Stage B closure.

## Current executive disposition

```text
Architecture
  ACCEPTED / RECONCILED

Package
  IMPLEMENTED for the current accepted ADR-006 boundary

Technical QA
  CERTIFIED — focused Stage A matrix 8/8 PASS

Executed closure evidence
  ADR006-QA-01 PASS
  ADR006-QA-02 PASS
  ADR006-QA-03 PASS
  ADR006-QA-04 PASS — 32/32
  ADR006-QA-05 PASS — 34/34
  ADR006-QA-06 PASS
  ADR006-QA-07 PASS
  ADR006-QA-08 PASS

Package divergence reproduced by focused matrix
  NONE

Stage A
  CLOSED for the current accepted ADR-006 technical boundary

FIRSTGAME / Stage B
  PARTIAL and tracked separately
```

No runtime change should be added only to make documentation appear complete. If
an unchanged focal QA case reproduces an accepted-contract failure, the permanent
fix belongs in `com.immersive.framework`, followed by the same QA case again.

## Canonical package owners

The current package separates the relevant responsibilities across canonical
runtime areas:

```text
Runtime/Transition
  ITransitionOrchestrator.cs
  TransitionEffectOrchestrator.cs
  NoOpTransitionOrchestrator.cs
  TransitionGateBlockerPolicy.cs

Runtime/GameFlow
  GameFlowRuntime.cs
  GameFlowRuntime.TransitionFailureAuthority.cs
  GameFlowRuntime.ActivityEntryLoadingProgress.cs
  GameFlowRuntime.ActivityEntryReadinessOrchestration.cs
  ActivityEntryReadinessExecutionStatus.cs
  ActivityEntryReadinessRecoveryGatePolicy.cs
  CommittedTargetRevealRecoveryGatePolicy.cs
  FrameworkTransitionDiagnostics.cs
  TransitionGateDiagnostics.cs

Runtime/Loading
  ActivityEntryLoadingProgressDiagnostics.cs
  ActivityEntryLoadingProgressEnvelope.cs
  ActivityEntryLoadingProgressPlan.cs
  FrameworkLoadingProgress.cs
  FrameworkLoadingProgressReporter.cs
```

These owners describe the current package shape; the architectural contracts are
defined by the ADR rather than by permanent filename identity.

## Contract-to-source reconciliation

| ADR-006 contract | Current package owner/evidence | Source disposition | Current QA disposition |
|---|---|---|---|
| Transition does not own Route or Activity authority | `Runtime/Transition/ITransitionOrchestrator.cs`, `TransitionEffectOrchestrator.cs` | Aligned | Preserved by focused transaction evidence |
| Optional presentation absence is explicit NoOp behavior | `Runtime/Transition/NoOpTransitionOrchestrator.cs` | Aligned | **PASS — ADR006-QA-07** |
| Non-accepted `Before` prevents governing mutation | `Runtime/GameFlow/GameFlowRuntime.TransitionFailureAuthority.cs` | Aligned | **PASS — ADR006-QA-01, two passes** |
| Non-accepted `After` after commit cannot produce false success or blind rollback | `Runtime/GameFlow/GameFlowRuntime.TransitionFailureAuthority.cs` | Aligned | **PASS — ADR006-QA-02, two passes** |
| Intentional supersession is distinct from ordinary failure | readiness/identity authority orchestration | Aligned | **PASS — ADR006-QA-03, identity regression executed twice** |
| Technical loading completion is not readiness-governed terminal completion | `GameFlowRuntime.ActivityEntryLoadingProgress.cs`, `Runtime/Loading/*` | Aligned in source | **PASS — ADR006-QA-04, 32/32** |
| Pure Transition Gate is distinct from readiness/reveal recovery protection | `TransitionGateDiagnostics.cs`, readiness/reveal recovery policies | Aligned | **PASS — ADR006-QA-05, 34/34** |
| Required presentation failures are explicit | `TransitionEffectOrchestrator.cs`, transition diagnostics | Aligned | **PASS — ADR006-QA-06** |
| Terminal paths clean pure Transition Gate state | blocker policy + GameFlow cleanup/diagnostics | Aligned | **PASS — ADR006-QA-08, two passes** |

## Focused Stage A QA matrix

### ADR006-QA-01 — Before failure blocks mutation — PASS

Executed through `QaAdr006TransactionBehavioralClosureRegression`.

Observed contract:

```text
Transition Before fault
  -> typed terminal failure
  -> no target commit
  -> original authority retained
  -> no lifecycle request residue
  -> terminal cleanup clean
```

The regression executed this path in both pass 1 and pass 2 of the same Play Mode
session.

### ADR006-QA-02 — After failure preserves committed authority — PASS

Executed through `QaAdr006TransactionBehavioralClosureRegression`.

Observed contract:

```text
Transition Before accepted
  -> target lifecycle commits
  -> Transition After fault
  -> overall operation non-success
  -> committed target remains authoritative
  -> no blind rollback
  -> terminal cleanup clean
```

The regression executed this path in both pass 1 and pass 2 of the same Play Mode
session.

### ADR006-QA-03 — Superseded is not Failed — PASS

Executed through the canonical `QaRouteActivityIdentityRegression` case:

```text
legitimate-supersession-preservation
```

The identity regression was executed twice. The evidence records:

```text
waitStatus='Superseded'
executionStatus='Superseded'
routeKind='SupersededCommittedTargetByRouteReplacement'
interruption='RouteAuthorityReplaced'
executionFailure='<none>'
cleanupFailure='<none>'
```

This proves the older occurrence is non-authoritative after replacement and is not
collapsed into an ordinary failure result.

### ADR006-QA-04 — Technical loading complete while readiness waits — PASS

Executed through the canonical
`QaParticipantAwareReadinessLoadingProgressRegression`.

Final result:

```text
[QA_READY_PROGRESS_01]
status='Passed'
cases='32'
required='4'
optional='1'
optionalOutcome='FailedNonBlocking'
ordering='Technical<100,0/4,1/4,2/4,3/4,4/4=100,Hide,Reveal,GateRelease'
```

The executed path proves that technical loading remains below terminal completion
while required readiness is incomplete, optional failure is non-blocking and
excluded from the required denominator, successful terminal 100% occurs only at
4/4 required participants ready, and ordering remains terminal progress -> hide ->
reveal -> gate release.

### ADR006-QA-05 — Recovery protection is not a Transition Gate leak — PASS

Executed through the canonical
`QaParticipantAwareReadinessLoadingTerminalRegression`.

Final result:

```text
[QA_READY_PROGRESS_02A]
status='Passed'
cases='34'
runtimePath='DirectActivityRequiredFailure'
contractPaths='DirectActivity,RouteStartupActivity,GameApplicationStartupActivity'
terminals='RequiredFailed,RequiredReleased,ReplacementRejected,LateOldOccurrenceRejected,DuplicateTerminal,OwnedCancellation'
```

The required-failure path intentionally emits an error-level lifecycle result while
the regression itself passes. The executed evidence records committed destination
authority with the ordinary Transition Gate released, Loading/Transition
presentation retained, readiness/reveal recovery protection retained, last progress
below terminal 100%, and no terminal progress update. Cleanup then releases
participants, restores presentation, releases the recovery gate and restores the
initial authority. This proves that legitimate recovery protection is distinct from
a pure Transition Gate leak and that the recovery residue clears causally.

### ADR006-QA-06 — Missing required presentation contract fails explicitly — PASS

Executed through `QaAdr006PresentationPolicyRegression`.

Observed contract:

```text
required presentation missing
  -> explicit failed result
  -> MissingAdapter evidence
  -> blocking issue present
  -> GameFlow does not accept the failed transition phase
  -> no silent conversion to optional/Skipped success
```

### ADR006-QA-07 — Optional presentation uses explicit NoOp — PASS

Executed through `QaAdr006PresentationPolicyRegression`.

Observed contract:

```text
explicit NoOpTransitionOrchestrator
  -> valid success
  -> effect status Skipped
  -> zero effect adapters
  -> no blocking issue
  -> accepted transition phase
  -> no false lifecycle authority
```

### ADR006-QA-08 — Terminal cleanup leaves no pure Transition Gate residue — PASS

`QaAdr006TransactionBehavioralClosureRegression` executed two full passes in one
Play Mode session and completed:

```text
pass-1-terminal-clean
pass-2-terminal-clean
isolation-scene-cleaned
official-authority-preserved
```

The final regression report was:

```text
[ADR006_TRANSACTION_BEHAVIORAL_CLOSURE]
status='Passed'
passes='2/2'
cases='15'
```

The repeated pass is intentional evidence that terminal state does not accumulate
between executions.

## Current execution summary

```text
ADR006 Presentation Policy
  PASS — 5 cases
  proves QA-06 and QA-07

ADR006 Transaction Behavioral Closure
  PASS — 15 cases
  passes='2/2'
  proves QA-01, QA-02 and QA-08

Identity Authority Regression
  PASS — 6/6
  run 1 PASS
  run 2 PASS
  proves QA-03 through legitimate-supersession-preservation

Participant-Aware Readiness Loading Progress
  PASS — 32/32
  proves QA-04
  ordering='Technical<100,0/4,1/4,2/4,3/4,4/4=100,Hide,Reveal,GateRelease'

Participant-Aware Readiness Loading Terminal
  PASS — 34/34
  proves QA-05
  required failure retains recovery protection after pure Transition Gate release
  cleanup restores presentation, releases recovery gate and restores authority
```

## QA ownership rule

`QAFramework` owns synthetic and negative proof for this matrix. QA should test
canonical package behavior and diagnostics rather than replicate package internals
as a second implementation. Test-only adapters and deterministic fault injectors
are acceptable QA infrastructure.

If a case fails:

```text
focused QA reproduces contract divergence
  -> record the first causal mismatch
  -> fix the official package owner
  -> rerun the same QA without weakening assertions
```

Do not resolve a failed QA case through a FIRSTGAME workaround or silent fallback.

## Stage A closure result

The complete focused matrix now has deterministic executed evidence for the
accepted ADR-006 technical boundary:

```text
QA-01 PASS
QA-02 PASS
QA-03 PASS
QA-04 PASS — 32/32
QA-05 PASS — 34/34
QA-06 PASS
QA-07 PASS
QA-08 PASS

Technical QA: CERTIFIED
Architecture: ACCEPTED / RECONCILED
Package: IMPLEMENTED for current accepted boundary
Stage A: CLOSED
Stage A estimate: 100%
Technical remaining: 0%
Package defect reproduced by closure matrix: NONE
```

No package change was required by the focused ADR-006 closure. Any remaining
FIRSTGAME work is Stage B consumer/product evidence only and does not reopen this
technical certification unless a concrete accepted-contract regression is
reproduced.

## Stage B / FIRSTGAME boundary

FIRSTGAME should prove real consumer experience, not become the permanent
exception-path laboratory. Stage B evidence may cover real Loading + Transition
authoring, understandable cover/wait/reveal behavior, useful Advanced / Debug
evidence, and consumer usability without reconstructing internal contracts.

Synthetic `Before`/`After` failures, forced missing adapters and gate-leak probes
remain QA responsibilities unless an independent consumer bug reproduces them.

## Documentation changes in the 2026-08-11 update

Edited:

- `Documentation~/Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-006-RECONCILIATION-2026-08-10.md`
- `Documentation~/Architecture/Tracking/IF-TRACK-Framework.md`

Created:

- none.

Removed:

- none.

The normative ADR is intentionally unchanged because no ADR contract changed and
mutable certification counts belong in reconciliation/tracking.

## Stage A acceptance result

```text
QA-01 pre-commit failure authority preservation       PASS
QA-02 post-commit committed authority preservation    PASS
QA-03 typed supersession preservation                 PASS
QA-04 readiness-governed loading terminal ordering    PASS — 32/32
QA-05 recovery/gate separation and cleanup            PASS — 34/34
QA-06 required presentation failure                   PASS
QA-07 explicit optional NoOp                          PASS
QA-08 repeated terminal cleanup                       PASS
package divergence                                    NONE
runtime implementation change                         NONE
Stage A                                                CLOSED / CERTIFIED
```

QA remains the technical exceptional-path authority. FIRSTGAME remains Stage B
consumer proof, and no duplicate smoke was introduced for QA-04/05.

## Architectural gain

The evidence now proves the full focused ADR-006 technical boundary directly
in runtime: pre-commit failure preserves old authority, post-commit failure
preserves committed authority, supersession is typed, readiness governs terminal
loading completion, recovery protection remains distinct from the pure Transition
Gate, required/optional presentation policies are explicit, and repeated terminal
cleanup does not leak state.

## Usability gain

Consumers remain insulated from exceptional-path machinery. Loading/Transition
can remain an authorable product surface while deterministic fault injection and
residual-state certification stay in QA.

## Suggested documentation commit

```text
docs(architecture): certify ADR-006 stage A exceptional paths
```
