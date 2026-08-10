# IF-ADR-005 — Input, Pause, Gate and Reset

Status: **Accepted**  
Last updated: 2026-08-10  
Package implementation: **COMPLETE FOR CURRENT ACCEPTED PACKAGE SCOPE**  
Current technical conformity: **OPEN ONLY FOR FOCUSED PAUSE QA**  
Current planning assessment: **30/30 Package · 20/20 Surface · 13/15 QA**  
Product surface status: **AVAILABLE / direct authoring surfaces are sufficient for the current lifecycle**  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-006, IF-ADR-007, IF-ADR-010, IF-ADR-011  
Current package baseline: `7b53b47814ddf59159972f56db171d60d421b14f` (`Camera-Docs`)  
Current QA baseline inspected: `d000303c6409338888c8abe21e83c70759171df6` (`Cam-Pass`)  
Current FIRSTGAME baseline observed: `796618243c3ca76f70d582f38475320c6461420b` (`Demo02 Reajuste`)

> Package implementation and product-surface completeness are closed for the
> current accepted scope. Stage A remains open only because direct QA evidence
> for Pause authority and Pause + Activity Restart interaction is still missing.
> FIRSTGAME remains Stage B consumer evidence and does not reopen technical
> conformity.

## Context

Input eligibility, Pause, capability gates, GameFlow Transition Gate, readiness
recovery, object/group Reset and Activity Restart intersect but do not share one
authority model.

They require explicit ownership or scope, deterministic cleanup and failure
evidence appropriate to the specific contract.

## Decision

Input admission is derived from valid Player/gameplay state.

Pause has scoped runtime authority and explicit presentation/input integration.

Reset operates through registered subjects/participants with explicit scope and
typed results.

Activity Restart reconfigures the active Activity. It is not Session Player
leave and is not Route replacement.

Gate semantics remain intentionally distinct:

```text
Reusable capability / pause gates
  -> explicit scoped ownership/handles where the contract models ownership
  -> deterministic release
  -> invalid ownership/release is explicit

GameFlow Transition Gate
  -> internal operation-scoped GameFlow state
  -> not an externally acquired resource
  -> no invented external lease/release contract
  -> deterministic internal terminal cleanup

Activity Entry Readiness Recovery Gate
  -> separate recovery authority
  -> may remain active after Transition Gate release
```

Do not infer that every type named "gate" must use the same ownership abstraction.

## Architectural constraints

- Runtime authority is scoped, typed and lifetime-explicit.
- Required invalid state fails explicitly and diagnostically.
- Consumer code does not depend on internal runtime modules, object-name inference
  or implicit global lookup.
- Editor surfaces present authored intent and runtime evidence without becoming
  runtime authority.
- Direct authoring is valid when no real technical materialization exists.

## Current package coverage

The package already contains the required official runtime/product pieces for the
accepted scope, including the current families around:

```text
PauseRuntime
PausePlayerInputBinding
UnityPlayerInputGateAdapter
PauseRequestTrigger

ResetRegistry
Reset subjects / participants
ResetSelectionConfig
ResetExecutor
object/group reset triggers

Activity Restart integration
GameFlow transition/readiness gate projections
```

The representative product surfaces remain semantically sufficient:

```text
Pause Request              COMPLIANT
Activity Restart           COMPLIANT
Object Reset Group Trigger COMPLIANT
Unity Input Gate           COMPLIANT SEMANTICALLY
```

No new Composer, Recipe, Wizard or generic Gate Manager is justified by the
current lifecycle.

## Transition Gate diagnostic semantics

The IF-TXN-03A distinction remains:

```text
TransitionGateSnapshot
  = pure Transition Gate state

CurrentTransitionGateMode
  = pure Transition Gate mode

ActivityEntryReadinessGateSnapshot
  = Transition Gate + Activity Entry Readiness Recovery Gate

CurrentGateSnapshot
  = broader operational composition used by host/input admission
```

A valid state can therefore be:

```text
Transition Gate released
Readiness Recovery Gate active
```

This is recovery protection, not Transition Gate leakage.

## Current technical QA evidence

The current QA source contains focused technical regressions for the surrounding
ADR-005 contracts.

### Input Gate

`QaInputGateRuntimeBindingSmoke` verifies the runtime port, explicit binding,
blocking/release behavior, preservation of a previously disabled Action Map,
non-blocking unrelated domains, explicit missing-map failure and cleanup. It also
proves that an unbound adapter does not fall back to a current host implicitly.

Disposition:

```text
Input Gate
  IMPLEMENTED
  QA PROVEN for the inspected current regression boundary
```

### Reset and Activity Restart

`QaObjectResetGroupVerticalSmoke` exercises explicit multi-subject selection,
invalid selection, missing registered subjects, empty-selection policy,
required/optional participant failure semantics, execution continuation policy,
single-flight behavior and cleanup.

`QaActivityRestartVerticalSmoke` exercises no-active-Activity failure, target
mismatch, invalid Reset selection before GameFlow mutation, nominal
Reset -> Clear -> Reenter ordering, single-flight behavior, warning completion,
blocking Reset failure and terminal request cleanup.

Disposition:

```text
Reset
  IMPLEMENTED
  QA PROVEN for the inspected current regression boundary

Activity Restart
  IMPLEMENTED
  QA PROVEN for the inspected current regression boundary
```

### Transition and readiness gates

Existing certification from the previous revision remains relevant:

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

These prove technical behavior. They are not UX certification.

## Remaining Stage A QA gap

The current QA source does not expose a focused regression that directly proves
`PauseRuntime`, `PausePlayerInputBinding` or `PauseRequestTrigger`, and it does
not directly prove Activity Restart while Pause is active.

This is the only current ADR-005 technical closure gap identified by the
2026-08-10 reconciliation.

Required focused proof:

```text
Pause authority
  -> request/state/result behavior is explicit
  -> invalid request/state fails diagnostically

PausePlayerInputBinding
  -> explicit runtime binding
  -> no hidden host/name fallback
  -> Pause blocks the intended gameplay input
  -> release restores the previous input state
  -> disable/destroy cleanup leaves no stale block

Pause + Activity Restart
  -> interaction is deterministic
  -> no stale Pause/Input/Gate state survives terminal restart behavior
  -> failure path is explicit if the current contract rejects an interaction
```

The QA cut must certify the contract that already exists. It must not change the
package merely to make the test pass. If the test reveals a real package defect,
a separate minimal package cut is required.

## Product-surface disposition

The former plan contained work such as:

```text
publish isolated product flows
create authoring surfaces
add Composer-like extraction
normalize every Inspector
```

The current package audit does not justify that as a missing package
implementation.

Current disposition:

```text
Package runtime/contracts  COMPLETE FOR CURRENT SCOPE
Product surfaces           AVAILABLE / COMPLIANT FOR INSPECTED PRIMARY FLOWS
Generic product extraction NOT REQUIRED
Composer/Wizard            NOT REQUIRED
```

Presentation normalization may happen during ordinary maintenance when a concrete
problem exists.

It is not an ADR completion blocker.

## FIRSTGAME

FIRSTGAME is Stage B for ADR-005.

It may prove that a real consumer can author and understand Pause/Input/Reset
flows, or reveal a concrete product UX problem that deserves a later package
improvement.

FIRSTGAME is not part of the Stage A technical closure gate. A missing or partial
consumer demonstration therefore does not reduce the current package
implementation status and does not manufacture a QA defect.

## Current assessment

The former percentage mixed package implementation, QA breadth, product
extraction and consumer evidence.

The current interpretation separates them:

```text
Architecture
  ACCEPTED / RECONCILED

Package
  IMPLEMENTED FOR CURRENT ACCEPTED BOUNDARY

Product Surface / Diagnostics
  IMPLEMENTED FOR CURRENT ACCEPTED BOUNDARY

Technical QA
  PARTIAL
  focused Pause certification remains

Divergent package behavior
  NONE IDENTIFIED

Missing package contract
  NONE IDENTIFIED

FIRSTGAME
  STAGE B / separate consumer evidence
```

For the Tracker planning row, the current local estimate is:

```text
Architecture 20/20
Package      30/30
Surface      20/20
QA           13/15
FIRSTGAME     9/15
Total         92%
```

The FIRSTGAME dimension is retained only as portfolio planning evidence. It does
not reopen Stage A. The technical blocker is the focused Pause QA gap.

## Completion criteria

For the accepted scope:

```text
each gate follows its declared authority model
Transition Gate terminal cleanup is explicit
readiness recovery remains separately observable
Pause and Restart interactions are deterministic
Reset scope/results are explicit
invalid operations fail diagnostically
consumer authoring does not require hidden runtime contracts
```

Current package implementation satisfies the package/product portion of these
criteria.

Current QA evidence satisfies Input Gate, Reset, Activity Restart and the cited
Transition/readiness boundaries. ADR-005 becomes technically closed when the
focused Pause and Pause + Restart proof is added and passes without requiring an
unapproved change of contract.

## Normative summary

```text
Do not unify unrelated gates under one lease model.
Do not create generic authoring layers where direct authoring is sufficient.
Do not treat synthetic Inspector QA as product certification.
Keep FIRSTGAME as separate consumer evidence.
Close the remaining Stage A gap with focused Pause QA, not new package architecture.
```
