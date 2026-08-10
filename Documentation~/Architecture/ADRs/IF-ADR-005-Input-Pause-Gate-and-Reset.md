# IF-ADR-005 — Input, Pause, Gate and Reset

Status: **Accepted**  
Last updated: 2026-08-09  
Package implementation: **COMPLETE FOR CURRENT ACCEPTED PACKAGE SCOPE**  
Current package assessment: **29/30** — local planning assessment; not release certification  
Product surface status: **AVAILABLE / direct authoring surfaces are sufficient for the current lifecycle**  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-006, IF-ADR-007, IF-ADR-010, IF-ADR-011  
Current package baseline: `43b96a4b100b8273da1190520536007ba82dc081` (`ADR-010B`)  
Current QA baseline inspected: `b6a45728285ddb2ce08269fc1f88ae3f1a4235e4` (`P0 — Serialized Player Migration Integrity`)

> This revision separates package/product completeness from optional future
> technical hardening. Missing Composer/Wizard/Apply flows are not considered
> gaps for Pause, Reset, Restart or Input Gate under the accepted ADR-010 model.

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

The package audit also found the representative product surfaces semantically
sufficient:

```text
Pause Request              COMPLIANT
Activity Restart           COMPLIANT
Object Reset Group Trigger COMPLIANT
Unity Input Gate           COMPLIANT SEMANTICALLY
```

No new Composer or Wizard is justified by the current lifecycle.

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

## Existing technical QA evidence

Existing certification cited by the previous revision remains relevant:

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

## Technical hardening

Additional QA is legitimate only when it proves a real technical invariant or a
known regression risk.

Possible future examples include:

```text
restart while paused
stale reset subjects
required vs optional reset failure
repeated restart
owner-destruction cleanup where ownership exists
residual gate leakage where the declared contract requires cleanup
```

These are independent system-specific hardening candidates.

They do not mean the package is missing a product surface.

## FIRSTGAME

FIRSTGAME may later reveal that a real consumer finds one of these flows confusing
or unnecessarily repetitive.

That observation can justify a small package improvement.

FIRSTGAME is not part of the technical closure gate and does not reduce the
current package implementation status.

## Current assessment

The previous 78% estimate mixed:

```text
package implementation
QA breadth
product extraction
consumer evidence
```

into one number.

That model is no longer used for closure.

Current local package assessment:

```text
29 / 30
```

Interpretation:

```text
package solution exists
runtime authority is explicit
primary product surfaces exist
no cross-cutting authoring gap identified
only focused technical hardening may remain
```

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

Future technical QA may strengthen evidence without reopening the product model.

## Normative summary

```text
Do not unify unrelated gates under one lease model.
Do not create generic authoring layers where direct authoring is sufficient.
Do not treat synthetic Inspector QA as product certification.
Keep future hardening contract-specific.
```
