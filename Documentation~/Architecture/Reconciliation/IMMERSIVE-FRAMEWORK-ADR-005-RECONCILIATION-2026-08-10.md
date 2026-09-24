# Immersive Framework — ADR-005 Reconciliation

**Date:** 2026-08-10  
**Type:** technical reconciliation and Stage A closure record  
**ADR:** IF-ADR-005 — Input, Pause, Gate and Reset

## Objective

Reconcile ADR-005 against the current package/QA contract, record the focused
Pause certification, record the package defect exposed by that certification
and its correction, and close the current Stage A technical boundary without
conflating it with separate FIRSTGAME consumer evidence.

## Source baselines

The original documentation reconciliation inspected:

```text
com.immersive.framework
  7b53b47814ddf59159972f56db171d60d421b14f
  Camera-Docs

QAFramework
  d000303c6409338888c8abe21e83c70759171df6
  Cam-Pass

FIRSTGAME / planet-devourer
  796618243c3ca76f70d582f38475320c6461420b
  Demo02 Reajuste
```

The focused closure work was subsequently performed on local working trees from
the current package/QA line. No new committed Git baseline is asserted by this
document until those local cuts are committed.

FIRSTGAME remains Stage B context and was not used as the Stage A runtime test
environment.

## Closure scope

```text
Input Gate focused regression
Activity Restart focused regression
Pause authority
PausePlayerInputBinding lifecycle
PauseRequestTrigger binding/failure behavior
Pause capability Gate projection
Pause + Activity Restart interaction
pre-Pause physical PlayerInput baseline restoration
scene release / disable / destroy cleanup
two-pass same-Play-Mode repeatability
terminal residual Pause/Gate state
```

## Out of scope

```text
new package architecture
new public Pause API
new Gate domain/policy
new generic Gate Manager
new Composer / Recipe / Wizard
FIRSTGAME implementation
multiplayer Pause policy
unrelated ADR changes
```

## Canonical evidence before focused Pause certification

### Input Gate

`QaInputGateRuntimeBindingSmoke` proves explicit runtime binding, no implicit host
fallback, blocking/release behavior, unrelated-domain behavior, preservation of a
previously disabled Gameplay Action Map, explicit missing-map failure and cleanup.

The current package distinguishes map-resolution failure from physical-write
failure. The QA expectation was aligned to the current explicit status:

```text
FailedGameplayActionMapResolution
```

Executed result:

```text
INPUT_GATE_RUNTIME_BINDING_SMOKE
  PASS — 9/9
```

### Activity Restart

`QaActivityRestartVerticalSmoke` proves no-active-Activity and target mismatch
failure, invalid Reset before flow mutation, nominal Reset -> Clear -> Reenter,
single-flight, warning completion, blocking Reset failure and terminal cleanup.

Executed result:

```text
ACTIVITY_RESTART_VERTICAL_SMOKE
  PASS — 8/8
```

The error/warning logs produced by its negative cases are expected evidence, not
terminal smoke failure.

## Focused Pause QA added

`QaPauseRuntimeBindingSmoke` was added under the existing GameFlow/InternalEditor
QA lifecycle rather than creating a parallel Pause runtime or new fixture domain.

The regression uses the real package surfaces and verifies:

```text
unbound PauseRequestTrigger has no implicit host fallback
missing binding authoring fails explicitly
Pause runtime/binding surfaces are available
baseline is captured before mutation
Pause applies logical state + physical PlayerInput posture + capability Gate state
repeated Pause is explicit no-change
Resume restores enabled Gameplay baseline
Pause + Activity Restart completes and preserves Pause
Resume after restart returns to Running input
scene release restores Pause/binding/input posture
destroy teardown leaves no stale binding/Gate state
disabled-before-Pause Gameplay remains disabled after Resume
disabled baseline release is clean
```

The runner repeats the contract twice in one Play Mode session and verifies
terminal residual state.

## First focused failure: QA contract correction

The first execution initially failed because the new smoke incorrectly expected a
Pause blocker at:

```text
Gameplay / GameplayAction
```

The package contract instead publishes Pause capability blockers at:

```text
Input / InputAcceptance
Interaction / InteractionAcceptance
```

Gameplay Action Map suppression is a separate physical PlayerInput integration.
The QA was corrected to assert the actual package contract; no package policy was
changed for this issue.

## Package defect reproduced

After the QA contract was aligned, the regression advanced through 11 cases and
reproduced a real package defect:

```text
Gameplay disabled immediately before Pause
  -> Pause
  -> Resume
  -> Gameplay became enabled
```

The failure message identified the boundary directly:

```text
Resume enabled Gameplay even though Gameplay was disabled immediately before Pause.
The Pause boundary must restore the pre-mutation input baseline instead of assuming Gameplay enabled.
```

### Root cause

`PauseProductBindingRuntimeContext` entered Pause through
`TryApplyActionMapSet(Global only)`. The writer returned a
`UnityPlayerInputActionMapSetWriteReceipt` containing the exact previous physical
posture, but that receipt was discarded after the successful Pause transaction.

Resume then applied a synthetic default posture:

```text
Global + Gameplay
```

instead of restoring the posture that existed immediately before Pause.

### Canonical pattern

The package already had the required mechanism:

```text
UnityPlayerInputGateAdapter.TryRestoreActionMapSet(...)
  -> UnityPlayerInputStateWriter.TryRestoreActionMapSet(...)
```

The receipt already carries:

```text
PreviousPrimaryActionMapName
PreviousEnabledActionMapNames
```

No new writer, helper, service, manager or public API was required.

## Package correction

The correction remained in:

```text
Runtime/Pause/PauseProductBindingRuntimeContext.cs
```

The runtime now retains one Pause-time Action Map set receipt for the active
Running -> Paused boundary, only after a successful Pause commit.

Lifecycle rules:

```text
successful Pause commit
  -> retain exact pre-Pause physical receipt

failed Pause / failed commit
  -> do not retain stale receipt

repeated Pause / no-change
  -> do not overwrite the active baseline

Resume
  -> restore exact retained receipt
  -> commit
  -> clear receipt

ReleaseBinding / ClearBinding / new binding lifetime
  -> clear receipt

Resume without valid Pause baseline
  -> fail explicitly; no silent fallback
```

The existing binding-time posture remains separate. It still belongs to binding
lifetime and is restored by `ReleaseBinding`; it is not reused as the per-Pause
baseline.

### Rollback integrity

The correction also preserves transaction atomicity:

```text
Pause physical failure
  -> existing physical/logical compensation
  -> no retained Pause baseline

Pause commit failure
  -> restore physical write receipt
  -> restore previous Pause snapshot
  -> no retained Pause baseline

Resume physical failure
  -> restore/recompose Paused physical posture
  -> rollback InputMode
  -> restore previous Pause snapshot

Resume commit failure
  -> restore Paused physical posture
  -> restore previous Pause snapshot
```

A physical writer failure during compensation remains diagnosable and is not
hidden by fallback.

## Final executed evidence

After the package correction, the same focused Pause regression was run again
without weakening the contract.

Final result:

```text
QA_PAUSE_CONTRACT
  status='Passed'
  cases='27/27'
  failed='0'
```

The completed evidence includes both full passes:

```text
run-1: resume-preserves-disabled-gameplay-baseline
run-1: disabled-baseline-release-is-clean
run-2: resume-preserves-disabled-gameplay-baseline
run-2: disabled-baseline-release-is-clean
terminal-no-residual-pause-or-gate
```

The expected `PauseRequestTrigger` `BindingUnavailable` errors remain deliberate
negative-case evidence for `unbound-trigger-does-not-fallback-to-host` and occur
in both passes.

## Consolidated ADR-005 technical evidence

```text
Input Gate        PASS — 9/9
Activity Restart  PASS — 8/8
Pause Contract    PASS — 27/27
                  two complete passes in one Play Mode session
                  terminal residual state PASS
```

Previously captured Transition/readiness evidence remains valid:

```text
IF-TXN-03A Transition Gate Terminal Integrity
  PASS — 16/16

IF-TXN-02 Clear/Restart Transition Authority
  PASS — 16/16

Direct Activity Readiness Policies
  PASS — 42/42

Participant-Aware Readiness Loading Terminal
  PASS — 34/34

Participant-Aware Readiness Loading Progress
  PASS — 32/32
```

## Reconciled status

```text
IF-ADR-005

Normative status
  ACCEPTED

Architecture
  RECONCILED

Package
  IMPLEMENTED
  reproduced Pause baseline defect corrected

Product Surface / Diagnostics
  IMPLEMENTED

Input Gate
  QA PROVEN — 9/9

Reset
  QA PROVEN for current focused boundary

Activity Restart
  QA PROVEN — 8/8

Pause authority
  QA PROVEN

Pause + Activity Restart
  QA PROVEN

Pause physical baseline restoration
  QA PROVEN with enabled and disabled Gameplay baselines

Current package blocker
  NONE IN CERTIFIED ADR-005 BOUNDARY

Current architecture blocker
  NONE

Current Stage A blocker
  NONE

FIRSTGAME
  Stage B / separate consumer evidence
```

## Planning estimate update

Before focused Pause closure:

```text
Architecture 20/20
Package      30/30
Surface      20/20
QA           13/15
FIRSTGAME     9/15
Total         92%
```

After focused Pause closure:

```text
Architecture 20/20
Package      30/30
Surface      20/20
QA           15/15
FIRSTGAME     9/15
Total         94%
```

Stage A is therefore 100% for the accepted ADR-005 technical boundary. The
remaining 6% in the portfolio-style estimate is Stage B consumer evidence only.

The ADR-005 portfolio-style estimate therefore moves from 92% to 94%. No new
cross-ADR aggregate is asserted by this closure record.

## Product surface affected

No new surface was introduced.

The package correction was internal to the existing Pause product runtime owner.
No Inspector, Create menu, Recipe, Profile, Composer, Template, generic Gate
manager or public API was added.

## Expected consumer flow

```text
Author Pause / Input / Reset intent through existing official surfaces
  -> framework binds explicit scoped runtime authority
  -> Pause preserves exact pre-Pause PlayerInput posture
  -> Resume restores that posture
  -> Reset/Restart follow their own typed contracts
  -> diagnostics expose state/result/failure evidence
```

## Stage A acceptance result

```text
explicit Pause authority                         PASS
no hidden Pause host fallback                    PASS
invalid binding/authoring diagnostic             PASS
Pause capability Gate projection                 PASS
physical gameplay suppression                    PASS
exact enabled-baseline restoration               PASS
exact disabled-baseline restoration              PASS
Pause + Activity Restart interaction             PASS
scene release cleanup                            PASS
destroy teardown cleanup                         PASS
two-pass same-Play-Mode repeatability            PASS
terminal no residual Pause/Gate                  PASS
```

## FIRSTGAME disposition

FIRSTGAME remains Stage B.

Real-consumer authoring/usability may still identify product improvements, but it
is no longer a condition for ADR-005 Stage A technical closure.

## Architectural gain

The closure preserves the authority split instead of manufacturing a unified gate
or input manager:

```text
Pause logical authority
Pause product PlayerInput transaction
Pause capability Gate projection
Input Gate physical adapter
Transition Gate
Readiness Recovery Gate
Reset registry/execution
Activity Restart orchestration
```

The defect was corrected where the physical Pause transaction owned its receipt
rather than by weakening QA or moving responsibility into another subsystem.

## Files for this documentation closure

```text
EDIT
Documentation~/Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md

EDIT
Documentation~/Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-005-RECONCILIATION-2026-08-10.md

EDIT
Documentation~/Architecture/Tracking/IF-TRACK-Framework.md
```

## Suggested commit message

```text
Close ADR-005 focused Pause technical certification
```
