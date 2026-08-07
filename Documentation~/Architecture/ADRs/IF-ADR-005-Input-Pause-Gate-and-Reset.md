# IF-ADR-005 — Input, Pause, Gate and Reset

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **78%**  
Implementation classification: **Integrated runtime exists; IF-TXN-03A clarifies and certifies Transition Gate state semantics; product extraction and broader negative coverage remain incomplete**  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-006, IF-ADR-007, IF-ADR-010, IF-ADR-011  
Current package baseline: `c457e8cd7a11b8f2ce816734b4d97a3a820b4eec` (`IF-TXN-03A`)  
Current QA baseline: `c99df1e77a8408e6b48124a5d371f09e9af52019` (`IF-TXN-03A`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)

> The normative architectural decision is preserved. Completion percentages are planning estimates, not automated release certification.

## Context

Input eligibility, pause, capability gates, the GameFlow Transition Gate, readiness recovery, object/group reset, and Activity Restart intersect but are not the same authority. They require explicit ownership or scope, deterministic cleanup, and failure evidence appropriate to each gate model.

## Decision

Input admission is derived from valid Player/gameplay state. Pause has a scoped runtime and presentation binding. Reset operates through registered subjects/participants with explicit scope and results. Activity Restart reconfigures the active Activity; it is not Session Player leave or Route replacement.

Gate semantics are intentionally split:

```text
Reusable capability / pause gates
  -> explicit scoped ownership/handles where that gate contract models ownership
  -> deterministic release
  -> invalid ownership/release is explicit

GameFlow Transition Gate
  -> internal operation-scoped GameFlow state
  -> not an externally acquired resource
  -> no external lease/release refusal contract
  -> cleanup is deterministic internal state replacement

Activity Entry Readiness Recovery Gate
  -> separate recovery authority after committed-target readiness failure
  -> may remain active after the Transition Gate is released
```

Do not infer that every type named “gate” must use the same ownership/lease abstraction.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The runtime host composes pause, time scale, pause surfaces, combined gates, reset registry, reset subjects/participants, cycle reset, object/group reset, Activity restart and GameFlow transition/readiness gate projections.

IF-TXN-03A makes the Transition Gate diagnostic/current-state surface precise:

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

Critical valid state:

```text
Transition Gate released
Readiness Recovery Gate active

TransitionGateSnapshot.HasBlockers == false
CurrentTransitionGateMode == None
ActivityEntryReadinessGateSnapshot.HasBlockers == true
```

This state is recovery protection, not gate leakage.

## Current QA evidence

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

The readiness suites were updated so recovery assertions use the readiness-composite surface while pure Transition Gate assertions use `TransitionGateSnapshot` and `CurrentTransitionGateMode`.

## Current FIRSTGAME evidence

FIRSTGAME integration proves parts of Input/Pause/Reset and real readiness/loading composition. IF-TXN-03A itself does not require a new consumer demonstration because it certifies internal GameFlow state semantics and diagnostics.

## What remains

- Publish isolated product flows for Input Gate, Object Reset, Activity Restart, and Pause.
- Expand negative QA for reusable/owned gates: double acquire where modeled, invalid owner/release, owner destruction, pause during transition and residual leakage.
- Keep Transition Gate tests focused on operation-scoped terminal cleanup rather than inventing an external lease/release contract.
- Add negative QA for restart while paused, stale reset subjects, required/optional reset failure, and repeated restart.
- Create authoring surfaces that expose intent without hiding technical ownership.
- Provide runtime status and exact-handle/state evidence in Advanced/Debug.
- Clarify cleanup order during Activity exit, Route replacement, and Session disposal.

## Completion criteria

- Every gate follows its declared authority model; ownership handles are required only for gates whose contract actually models them.
- Transition Gate terminal cleanup leaves the pure Transition Gate projection clean while allowing explicit readiness recovery to remain active when required.
- Pause and restart interactions are deterministic.
- Invalid operations fail explicitly with actionable diagnostics.
- Product flows are independently demonstrable where consumer proof is required and covered by canonical QA.

## Completion assessment

```text
Estimated completion: 78%
Normative status: Accepted
IF-TXN-03A Transition Gate integrity: CLOSED / CERTIFIED — 16/16
Direct readiness gate integration: PASS — 42/42
Participant-aware terminal recovery: PASS — 34/34
Remaining: broader gate/pause/reset negative matrix, product extraction, Advanced/Debug polish
```
