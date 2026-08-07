# IF-ADR-011 — Participant-Aware Activity Readiness Loading Progress

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **94%**  
Implementation classification: **Runtime complete; participant-aware progress, terminal recovery, startup parity and Transition-vs-recovery gate separation are QA-certified; product presentation and focused public-only waiting/joining proof remain**  
Related decisions: IF-ADR-005, IF-ADR-006, IF-ADR-007, IF-ADR-012  
Current package baseline: `c457e8cd7a11b8f2ce816734b4d97a3a820b4eec` (`IF-TXN-03A`)  
Current QA baseline: `c99df1e77a8408e6b48124a5d371f09e9af52019` (`IF-TXN-03A`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70`

> The normative architectural decision is preserved. Completion percentages are planning estimates, not automated release certification.

## Context

Technical scene/content loading may finish before required Activity participants are ready. Loading progress must reserve space for participant-aware readiness without inventing progress, regressing, reaching 100% early, or accepting stale occurrence updates.

## Decision

Activity entry uses a monotonic progress envelope with a technical range and an optional reserved readiness range. Readiness progress is derived from occurrence-scoped aggregate evidence. Terminal 100% is issued only for Ready. Terminal failure stops completion. Stale occurrence snapshots are rejected.

## Covered waits that depend on external actions

A `WaitCovered` operation may legitimately remain below terminal completion while a Required readiness contribution is `Preparing`. If the only user action capable of advancing readiness is hidden behind retained cover, the resulting lock is a product control-plane composition problem. Loading must not compensate through fake progress, timeout, premature hide/reveal, or by becoming Player/readiness authority.

The package retains the advisory warning for:

```text
WaitCovered + ExplicitSlots + Player requirement >= JoinedSlots
```

## Gate semantics after terminal readiness failure

IF-TXN-03A certifies that Loading/readiness recovery and the ordinary Transition Gate are not the same current-state authority.

A required failure may report:

```text
gateReleased = true
recoveryGate = true
```

with:

```text
TransitionGateSnapshot.HasBlockers == false
CurrentTransitionGateMode == None
ActivityEntryReadinessGateSnapshot.HasBlockers == true
```

Loading/Transition presentation may remain retained because the committed destination is not Ready, while the ordinary Transition Gate has already completed its operation-scoped lifecycle.

QA and diagnostics must inspect:

```text
TransitionGateSnapshot
  -> when asking whether the Transition Gate itself is active

ActivityEntryReadinessGateSnapshot
  -> when asking whether Activity-entry admission/recovery remains blocked
```

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The package implements required/optional counts including completed and released evidence, readiness snapshots, range mapping, queued reporting, monotonic acceptance, stale occurrence rejection, terminal failure, and completion only when Ready. Integration exists for direct Activity entry, Route startup and Game Application startup.

IF-TXN-01 preserves final reveal authority. IF-TXN-03A preserves terminal gate/recovery authority and diagnostic separation without adding a second Loading completion authority.

## Current QA evidence

```text
Participant-Aware Readiness Loading Progress
  PASS — 32/32
  required=4
  optional=1
  optionalOutcome=FailedNonBlocking
  ordering:
    Technical<100
    -> 0/4 -> 1/4 -> 2/4 -> 3/4
    -> 4/4=100
    -> Hide
    -> Reveal
    -> GateRelease

Participant-Aware Readiness Loading Terminal
  PASS — 34/34
  terminals:
    RequiredFailed
    RequiredReleased
    ReplacementRejected
    LateOldOccurrenceRejected
    DuplicateTerminal
    OwnedCancellation

Participant-Aware Startup Parity — Route
  PASS — 25/25
  path=RouteStartupActivity
  transition-gate-released

Participant-Aware Startup Parity — Game Application
  PASS — 20/20
  path=GameApplicationStartupActivity
  transition-gate-released

Direct Activity Readiness Policies
  PASS — 42/42

IF-TXN-03A Transition Gate Terminal Integrity
  PASS — 16/16

IF-TXN-01 Transition Failure Authority
  PASS — 22/22
```

The terminal regression proves that on required failure the destination remains authoritative, successful 100% is not published, Loading/Transition retention remains correct, the ordinary Transition Gate is released, and readiness recovery remains active until cleanup. Final cleanup proves both pure and composite gate views are clean.

## Current FIRSTGAME evidence

FIRSTGAME loading demonstrations provide practical evidence, including Player readiness as the final loading phase. Loading retention during an unjoined Required Player is expected; when `RequestJoin` is available and emitted, Player reconciliation can progress the same occurrence to `Ready` and Loading completes through the existing readiness projection.

## What remains

- Add focused public-only proof for `WaitCovered + WaitingForJoin + RequestJoin while gate retained + same-occurrence Ready`.
- Publish presentation guidance for determinate versus indeterminate technical phases.
- Expose readiness ratio inputs, recovery state and rejection counts in Advanced/Debug diagnostics.
- Preserve stale occurrence, duplicate terminal, cancellation and supersession coverage as QA evolves.

Startup parity is no longer an open certification residual for the current Route/Game Application startup paths: both canonical paths are green.

## Completion criteria

- Progress never decreases and never reaches 100% before Ready.
- Stale or foreign occurrences cannot update the active operation.
- Failure and supersession terminate correctly without false completion.
- Loading/readiness recovery cannot be misdiagnosed as a residual Transition Gate.
- A covered control-plane dependency cycle cannot be repaired by Loading-side false completion.
- Current QA and FIRSTGAME pass the same supported scenarios where consumer proof is required.

## Completion assessment

```text
Estimated completion: 94%
Normative status: Accepted
Participant-aware progress: PASS — 32/32
Participant-aware terminal/recovery: PASS — 34/34
WaitVisible/WaitCovered integration: PASS — 42/42
Startup parity: PASS — Route 25/25 + Game Application 20/20
IF-TXN-03A: PASS — 16/16
IF-TXN-01 non-regression: PASS — 22/22
Remaining: public-only waiting/joining proof, product presentation guidance, Advanced/Debug polish
```
