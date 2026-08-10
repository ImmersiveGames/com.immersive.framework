# Immersive Framework — ADR-005 Reconciliation

**Date:** 2026-08-10  
**Type:** technical documentation reconciliation  
**ADR:** IF-ADR-005 — Input, Pause, Gate and Reset

## Objective

Reconcile ADR-005 documentation against the current official Git state without
changing runtime/package behavior, and isolate the remaining technical closure
work from separate FIRSTGAME consumer evidence.

## Source baselines

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

The package and QA repositories were inspected read-only. FIRSTGAME is recorded
only as Stage B consumer context and was not used to reopen Stage A technical
conformity.

## Scope

```text
ADR-005 normative/status wording
Tracker ADR-005 planning/status row
Tracker Pause/Input/Gate and Reset/Restart track-board disposition
current package / QA / FIRSTGAME source baselines
remaining Stage A closure definition
```

## Out of scope

```text
runtime changes
new package contracts
new Composer / Recipe / Wizard
new generic Gate Manager
FIRSTGAME implementation
QA implementation in this documentation cut
changes to unrelated ADR scoring
```

## Evidence classification

### Package

The accepted package boundary already contains the official runtime/product
families for:

```text
PauseRuntime
PausePlayerInputBinding
PauseRequestTrigger
UnityPlayerInputGateAdapter
ResetRegistry
Reset subjects / participants
ResetSelectionConfig
ResetExecutor
Object Reset Group
Activity Restart
Transition / readiness gate projections
```

No missing package contract or architectural divergence was identified during the
comparison.

Disposition:

```text
Package
  IMPLEMENTED FOR CURRENT ACCEPTED BOUNDARY

Product Surface / Diagnostics
  IMPLEMENTED FOR CURRENT ACCEPTED BOUNDARY

Divergent
  NONE IDENTIFIED

Absent
  NONE IDENTIFIED IN PACKAGE
```

### Input Gate QA

The current QA source contains `QaInputGateRuntimeBindingSmoke`, which directly
checks explicit runtime binding, no implicit host fallback for an unbound adapter,
Gameplay/InputAcceptance blocking, unrelated-domain non-blocking behavior,
restoration of previous Action Map state, explicit missing-map failure and
cleanup.

Disposition:

```text
Input Gate
  QA PROVEN for current focused regression boundary
```

### Reset QA

The current QA source contains `QaObjectResetGroupVerticalSmoke`, covering
explicit subject selection, invalid selection, unregistered subjects, empty
selection policy, required/optional participant failure semantics, continuation
policy, single-flight behavior and terminal cleanup.

Disposition:

```text
Reset
  QA PROVEN for current focused regression boundary
```

### Activity Restart QA

The current QA source contains `QaActivityRestartVerticalSmoke`, covering
no-active-Activity failure, target mismatch, invalid Reset before flow mutation,
nominal Reset -> Clear -> Reenter ordering, single-flight, warnings, blocking
Reset failure and terminal cleanup.

Disposition:

```text
Activity Restart
  QA PROVEN for current focused regression boundary
```

### Transition / readiness gate QA

Previously captured focused certifications remain part of the current ADR-005
evidence set:

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

### Remaining Pause QA gap

A search of the current QA source did not find a focused regression directly
covering:

```text
PauseRuntime
PausePlayerInputBinding
PauseRequestTrigger
Pause + Activity Restart interaction
```

This is classified as a QA gap, not a package gap.

## Reconciled status

```text
IF-ADR-005

Normative status
  ACCEPTED

Architecture
  RECONCILED

Package
  IMPLEMENTED

Product Surface / Diagnostics
  IMPLEMENTED

Input Gate
  QA PROVEN

Reset
  QA PROVEN

Activity Restart
  QA PROVEN

Pause authority
  IMPLEMENTED
  DIRECT FOCUSED QA PENDING

Pause + Activity Restart
  DIRECT FOCUSED QA PENDING

Current package blocker
  NONE

Current architecture blocker
  NONE

Current Stage A blocker
  focused Pause QA only

FIRSTGAME
  Stage B / separate consumer evidence
```

## Planning estimate correction

The Tracker row is corrected from:

```text
20/20 | 27/30 | 18/20 | 11/15 | 9/15 | 85%
```

to:

```text
20/20 | 30/30 | 20/20 | 13/15 | 9/15 | 92%
```

Rationale:

- Package is not missing a current accepted contract.
- Current direct product surfaces are sufficient under ADR-010.
- Existing QA is substantially broader than the previous 11/15 representation.
- The remaining technical evidence gap is focused on Pause.
- FIRSTGAME stays in the planning table as consumer evidence but does not reopen
  Stage A technical conformity.

With the current Tracker values, the portfolio planning mean becomes
approximately **89.7%**.

## Product surface affected

Documentation only.

No Inspector, Create menu, Recipe, Profile, Composer, Template, runtime authority
or authoring materialization changes are introduced by this reconciliation.

## Expected user flow

The accepted product flow remains direct and feature-specific:

```text
Author Pause / Input / Reset intent through the existing official surface
  -> framework binds to explicit scoped runtime authority
  -> runtime performs Pause, Gate, Reset or Restart behavior
  -> diagnostics expose state/result/failure evidence
  -> QA certifies the declared contract
```

No generic authoring layer is required solely for ADR completion.

## Required next technical cut

### ADR-005A — Pause Authority and Restart Interaction QA Certification

Type:

```text
technical / QA-only unless a real package defect is reproduced
```

Expected focused proof:

```text
PauseRuntime request/state/result semantics
explicit invalid-state diagnostics
PausePlayerInputBinding explicit bind/release
no hidden host or name fallback
previous input state restoration
OnDisable / destroy cleanup
Pause + Activity Restart deterministic interaction
no stale Pause/Input/Gate state after terminal behavior
```

The QA implementation must test the package contract as it exists. It must not
change package semantics merely to make the regression pass.

If the QA cut reproduces a package defect, that defect must be handled as a
separate minimal package cut with its own scope and acceptance criteria.

## Expected smoke

A focused Play Mode regression under the canonical QA hierarchy, conceptually:

```text
Immersive Framework/QA/Regressions/Pause/Run Pause Authority Regression
```

The exact menu/file naming should follow the current QA canonical organization at
time of implementation; this reconciliation does not create that test.

## Technical acceptance criteria

```text
existing package compiles unchanged
Input Gate evidence remains valid
Reset evidence remains valid
Activity Restart evidence remains valid
focused Pause regression passes
Pause + Restart interaction is deterministic
no fallback to implicit/global authority
invalid required states fail explicitly
cleanup leaves no stale Pause/Input/Gate state
```

## Product acceptance criteria

For this documentation cut:

```text
ADR accurately states the current package/QA boundary
Tracker no longer treats missing package/product work as ADR-005 limiter
FIRSTGAME is explicitly separated as Stage B consumer evidence
no unnecessary Composer/Wizard/manager work is manufactured
next required technical work is unambiguous
```

Real-consumer authoring/usability proof remains a separate FIRSTGAME concern.

## Architectural gain

The reconciliation preserves the intended authority split instead of creating a
single generic gate abstraction:

```text
Pause authority
Input Gate projection
Transition Gate
Readiness Recovery Gate
Reset registry/execution
Activity Restart orchestration
```

Each contract remains typed, scoped and independently diagnosable.

## Usability gain

The documentation now tells a framework consumer or maintainer where the real
remaining work is. Package/product implementation is not incorrectly presented
as incomplete, and QA can close the specific Pause risk without creating new
product machinery.

## Files changed by this documentation cut

```text
EDIT
Documentation~/Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md

EDIT
Documentation~/Architecture/Tracking/IF-TRACK-Framework.md

CREATE
Documentation~/Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-ADR-005-RECONCILIATION-2026-08-10.md
```

## Suggested commit message

```text
Reconcile ADR-005 pause input gate and reset status
```
