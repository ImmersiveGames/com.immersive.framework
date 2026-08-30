# IF-ADR-005 — Input, Pause, Gate and Reset

Status: **Accepted**  
Last updated: 2026-08-10  
Package implementation: **COMPLETE FOR CURRENT ACCEPTED PACKAGE SCOPE**  
Current technical conformity: **CLOSED FOR CURRENT ACCEPTED STAGE A BOUNDARY**  
Any numeric planning assessment below is a planning estimate only; it is not certification or a conformance score.  
Current planning assessment: **30/30 Package · 20/20 Surface · 15/15 QA**  
Product surface status: **AVAILABLE / direct authoring surfaces are sufficient for the current lifecycle**  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-006, IF-ADR-007, IF-ADR-010, IF-ADR-011  
Current reconciliation: [ADR-005 reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-005-RECONCILIATION-2026-08-10.md)  
Git package baseline originally reconciled: `7b53b47814ddf59159972f56db171d60d421b14f` (`Camera-Docs`)  
Git QA baseline originally reconciled: `d000303c6409338888c8abe21e83c70759171df6` (`Cam-Pass`)  
FIRSTGAME baseline observed: `796618243c3ca76f70d582f38475320c6461420b` (`Demo02 Reajuste`)

> Package implementation, product-surface completeness and Stage A technical QA
> are closed for the current accepted scope. Focused Pause QA exposed one real
> package defect in pre-Pause PlayerInput posture restoration; the package was
> corrected in the existing Pause product owner and the same regression then
> passed 27/27 across two passes in one Play Mode session. FIRSTGAME remains
> Stage B consumer evidence and does not reopen technical conformity.

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

The package contains the official runtime/product pieces for the accepted scope,
including:

```text
PauseRuntime
PauseProductBindingRuntimeContext
PlayerPauseInput
PauseRequestTrigger
UnityPlayerInputGateAdapter
UnityPlayerInputStateWriter

ResetRegistry
Reset subjects / participants
ResetSelectionConfig
ResetExecutor
object/group reset triggers

Activity Restart integration
GameFlow transition/readiness gate projections
```

The representative product surfaces are semantically sufficient:

```text
Pause Request              COMPLIANT
Activity Restart           COMPLIANT
Object Reset Group Trigger COMPLIANT
Unity Input Gate           COMPLIANT
```

No new Composer, Recipe, Wizard or generic Gate Manager is justified by the
current lifecycle.

## Pause physical baseline correction

Focused ADR-005 QA reproduced one package defect after the initial Pause contract
cases were composed canonically.

The failing boundary was:

```text
Gameplay disabled immediately before Pause
  -> Pause
  -> Resume
  -> Gameplay incorrectly enabled
```

The first causal divergence was in `PauseProductBindingRuntimeContext`:

```text
Pause
  -> TryApplyActionMapSet(Global only)
  -> writer returned exact previous Action Map posture receipt
  -> receipt was discarded after commit

Resume
  -> applied Global + Gameplay unconditionally
  -> previous disabled Gameplay posture was lost
```

The correction remained in the existing product owner. The Pause-time
`UnityPlayerInputActionMapSetWriteReceipt` is retained only for the active
Running -> Paused transaction boundary, restored on Resume through the existing
adapter/writer restore path, cleared on successful Resume and binding cleanup,
and never reused across binding lifetimes.

This receipt is intentionally distinct from the binding-time posture captured by
`PlayerPauseInput` registration and later restored by `ReleaseBinding`.

No Pause Gate policy, Activity Restart semantic, public API or QA assertion was
changed to accommodate the defect.

## Pause Gate semantics

Logical Pause capability blockers remain:

```text
Input / InputAcceptance
Interaction / InteractionAcceptance
```

Gameplay Action Map suppression is a separate physical PlayerInput integration.
Pause does not require or publish a synthetic `Gameplay / GameplayAction` Pause
blocker.

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

### Input Gate

`QaInputGateRuntimeBindingSmoke` verifies explicit runtime binding, no implicit
host fallback, Gameplay/InputAcceptance blocking and release, unrelated-domain
non-blocking behavior, preservation of a previously disabled Action Map,
explicit missing-map resolution failure and cleanup.

Executed result:

```text
INPUT_GATE_RUNTIME_BINDING_SMOKE
  PASS — 9/9
```

The negative missing-map case now expects the current explicit diagnostic status
`FailedGameplayActionMapResolution`, rather than the superseded generic
`FailedActionMapBlock` expectation.

### Reset and Activity Restart

`QaObjectResetGroupVerticalSmoke` remains the focused proof for Reset selection,
participant semantics, single-flight behavior and cleanup.

`QaActivityRestartVerticalSmoke` exercises no-active-Activity failure, target
mismatch, invalid Reset before flow mutation, nominal Reset -> Clear -> Reenter,
single-flight, warning completion, blocking Reset failure and terminal cleanup.

Executed Activity Restart result:

```text
ACTIVITY_RESTART_VERTICAL_SMOKE
  PASS — 8/8
```

### Pause authority and Pause + Activity Restart

`QaPauseRuntimeBindingSmoke` is the focused ADR-005 closure regression.

It proves, per pass:

```text
unbound trigger does not fall back to the host
missing binding authoring fails explicitly
runtime and bindings are available
baseline is captured before Pause
Pause applies logical state, physical input and capability Gate effects
repeated Pause is explicit no-change
Resume restores an enabled Gameplay baseline
Pause + Activity Restart completes while preserving Pause
Resume after restart restores Running input
scene release cleans binding, Pause and physical posture
destroy teardown leaves no stale binding or Gate state
Resume preserves a Gameplay map disabled immediately before Pause
disabled-baseline release is clean
```

The full runner executes the contract twice in the same Play Mode session and
then checks the terminal residual state.

Final executed result after the package correction:

```text
QA_PAUSE_CONTRACT
  PASS — 27/27
  failed='0'
  two complete passes in one Play Mode session
  terminal-no-residual-pause-or-gate
```

### Transition and readiness gates

Previously captured focused certifications remain relevant:

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

## Stage A closure

The previously identified focused Pause QA gap is closed.

Current ADR-005 Stage A evidence is:

```text
Input Gate        PASS — 9/9
Activity Restart  PASS — 8/8
Pause Contract    PASS — 27/27
                  two complete passes
                  terminal residual check PASS
```

The focused Pause regression did exactly what the ADR required: it first exposed
a real package divergence under canonical composition, the package was corrected
in the owning runtime context, and the same QA then passed without weakening the
assertions.

## Product-surface disposition

```text
Package runtime/contracts  COMPLETE FOR CURRENT SCOPE
Product surfaces           AVAILABLE / COMPLIANT FOR INSPECTED PRIMARY FLOWS
Technical QA               CERTIFIED FOR CURRENT ADR-005 STAGE A BOUNDARY
Generic product extraction NOT REQUIRED
Composer/Wizard            NOT REQUIRED
```

Presentation normalization may happen during ordinary maintenance when a concrete
problem exists. It is not an ADR completion blocker.

## FIRSTGAME

FIRSTGAME is Stage B for ADR-005.

It may prove that a real consumer can author and understand Pause/Input/Reset
flows, or reveal a concrete product UX problem that deserves a later package
improvement.

FIRSTGAME is not part of the Stage A technical closure gate. A missing or partial
consumer demonstration therefore does not reduce current Stage A conformity.

## Current assessment

```text
Architecture
  ACCEPTED / RECONCILED

Package
  IMPLEMENTED FOR CURRENT ACCEPTED BOUNDARY

Product Surface / Diagnostics
  IMPLEMENTED FOR CURRENT ACCEPTED BOUNDARY

Technical QA
  CERTIFIED

Divergent package behavior
  NONE REMAINING IN THE CERTIFIED ADR-005 BOUNDARY

Missing package contract
  NONE IDENTIFIED

FIRSTGAME
  STAGE B / separate consumer evidence
```

For Tracker planning, the closed technical dimensions are:

```text
Architecture 20/20
Package      30/30
Surface      20/20
QA           15/15
FIRSTGAME     9/15
Total         94%
```

The FIRSTGAME dimension is retained only as portfolio planning evidence. It does
not reopen Stage A.

## Completion criteria

For the accepted scope:

```text
each gate follows its declared authority model
Transition Gate terminal cleanup is explicit
readiness recovery remains separately observable
Pause and Restart interactions are deterministic
Pause restores the exact pre-Pause physical input posture
Reset scope/results are explicit
invalid operations fail diagnostically
consumer authoring does not require hidden runtime contracts
```

The current package and executed QA evidence satisfy these Stage A criteria.

## Normative summary

```text
Do not unify unrelated gates under one lease model.
Do not create generic authoring layers where direct authoring is sufficient.
Do not treat synthetic Inspector QA as product certification.
Keep FIRSTGAME as separate consumer evidence.
Preserve exact pre-Pause PlayerInput posture across Pause -> Resume.
ADR-005 Stage A is closed for the current accepted boundary.
```
