# IF-ADR-007 — Activity Entry Readiness and Reveal Gating

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **96%**  
Implementation classification: **Runtime contract complete; WaitVisible/WaitCovered, participant-aware terminal behavior, startup parity and Transition-vs-recovery gate separation are re-certified; focused ObserveOnly/Player public-only matrix remains**  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-006, IF-ADR-009, IF-ADR-011, IF-ADR-012  
Current package baseline: `c457e8cd7a11b8f2ce816734b4d97a3a820b4eec` (`IF-TXN-03A`)  
Current QA baseline: `c99df1e77a8408e6b48124a5d371f09e9af52019` (`IF-TXN-03A`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70`

> The normative architectural decision is preserved. Completion percentages are planning estimates, not automated release certification.

## Context

An Activity may have technically loaded content while required participants, Actors, adapters, or local visibility are still preparing. Reveal policy must distinguish observing readiness, waiting while visible, and waiting while covered without deadlocking or treating expected authority replacement as failure.

## Decision

Activity entry uses explicit policies:

```text
ObserveOnly
WaitVisible
WaitCovered
```

Readiness is occurrence-scoped and aggregates required/optional contribution evidence. Preparing, Ready, terminal failure, invalidation, cancellation, and supersession are distinct. Loading/transition presentation may wait on readiness but does not own it. A newer Route or Activity authority may supersede an in-flight wait through a typed interruption cause.

## Covered readiness and externally-driven progression

`WaitCovered` deliberately retains destination presentation and gameplay capabilities until the captured Activity readiness occurrence reaches `Ready`. A Required contribution may remain `Preparing` indefinitely when the represented condition has not occurred. The framework does not fabricate readiness through timeout, presentation release, or Loading completion.

A product can create a control-plane dependency cycle:

```text
Required readiness depends on an external/user action
-> WaitCovered keeps the destination covered
-> the only control that can perform the action is inside that covered destination
-> the action never occurs
-> readiness remains Preparing
-> WaitCovered remains covered
```

This is a composition problem, not a reason to weaken Activity Readiness or `WaitCovered`.

The initial canonical warning remains:

```text
EntryReadinessPolicy = WaitCovered
Player projection = ExplicitSlots
Player requirement >= JoinedSlots
```

The warning is advisory and non-mutating. Valid compositions include pre-entry satisfaction, automatic progression, or an external/persistent control plane. `WaitVisible` is appropriate when preparation is intentionally part of the visible Activity experience.

## Transition Gate and readiness recovery are separate authorities

IF-TXN-03A formalizes a distinction required by committed-target readiness failures:

```text
TransitionGateSnapshot
  -> Transition Gate only

ActivityEntryReadinessGateSnapshot
  -> Transition Gate + Activity Entry Readiness Recovery Gate
```

A terminal readiness failure may intentionally retain recovery after the Transition Gate is released:

```text
TransitionGateSnapshot.HasBlockers == false
CurrentTransitionGateMode == None
ActivityEntryReadinessGateSnapshot.HasBlockers == true
```

The composite blocker protects the committed but not-ready destination. It must not be diagnosed as an active Transition Gate.

Cleanup must prove both dimensions explicitly: pure Transition state remains released and readiness recovery is removed when recovery cleanup occurs.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.
- Validation warnings must not silently change authored readiness, transition, participation, or gate configuration.

## Current implementation coverage

The package implements readiness policies, occurrence correlation, required/optional pending/completed/failed/released counts, progressive state, reveal gating, failure diagnostics, loading progress integration, typed `Superseded` execution, and separate current-state projections for Transition Gate versus Activity Entry Readiness admission/recovery.

IF-TXN-01 guarantees that a failed/non-accepted final reveal after commit cannot be reported as ordinary success. IF-TXN-03A guarantees that terminal Transition cleanup and readiness recovery cannot be conflated in current-state diagnostics.

## Current QA evidence

```text
Direct Activity Readiness Policies
  PASS — 42/42
  WaitVisible PASS
  WaitCovered PASS
  gate retained while required
  reveal ordering confirmed
  gate release confirmed

Participant-Aware Readiness Loading Terminal
  PASS — 34/34
  required failure retains readiness recovery
  pure Transition Gate released at terminal
  cleanup restores pure + composite clean state

Participant-Aware Readiness Loading Progress
  PASS — 32/32

Participant-Aware Startup Parity — Route
  PASS — 25/25

Participant-Aware Startup Parity — Game Application
  PASS — 20/20

IF-TXN-03A Transition Gate Terminal Integrity
  PASS — 16/16

IF-TXN-01 Transition Failure Authority
  PASS — 22/22
```

The readiness compatibility regressions were updated to query the semantic surface they intend to prove instead of relying on the former composite meaning of `TransitionGateSnapshot`.

## Current FIRSTGAME evidence

FIRSTGAME has exercised real WaitCovered/Player-readiness interactions and Route replacement. The Manager-Provisioned late-join path progresses to `Ready` when `RequestJoin` is emitted. The remaining product risk is keeping the operation required to advance readiness reachable while gameplay presentation is covered.

## What remains

- Complete ObserveOnly-specific negative coverage not represented by the canonical suite.
- Add focused required Player contribution cases for joining closed, capacity changes, no Player, and Route replacement where useful.
- Add a public-only `WaitCovered + WaitingForJoin + RequestJoin while gate retained + same-occurrence Ready` regression.
- Publish a concise policy selection guide explaining when loading should remain covered.
- Expose occurrence/revision and pending contribution details consistently in Advanced/Debug.

## Completion criteria

- WaitCovered never reveals before Ready and never deadlocks after typed supersession.
- WaitVisible permits visible preparation without losing terminal diagnostics.
- ObserveOnly never becomes an accidental blocking wait.
- Recovery after a committed-target readiness failure remains distinguishable from Transition Gate state.
- Known control-plane dependency risks are warned without rejecting valid automatic/pre-entry/external-control-plane compositions.
- Current QA passes the required policy and authority-replacement matrix for the supported boundary.

## Completion assessment

```text
Estimated completion: 96%
Normative status: Accepted
WaitVisible/WaitCovered: PASS — 42/42
Participant-aware terminal/recovery: PASS — 34/34
Participant-aware progress: PASS — 32/32
Startup parity: PASS — Route 25/25, Game Application 20/20
IF-TXN-03A gate/recovery separation: PASS — 16/16
IF-TXN-01 non-regression: PASS — 22/22
Remaining: focused ObserveOnly + Player/public-only matrix and product guidance
```

The percentage remains unchanged because IF-TXN-03A strengthens correctness and certification of an existing readiness model rather than closing the remaining product/public-only scope.
