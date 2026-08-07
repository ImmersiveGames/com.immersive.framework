# IF-ADR-006 — Loading, Transition, Persistence and Diagnostics

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **91%**  
Implementation classification: **Core orchestration + IF-TXN-01 transition failure authority implemented; product template gaps remain**  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-011, IF-ADR-015  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`  
Transaction cut: **IF-TXN-01 GameFlow Transition Failure Authority (implemented in package)**

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Loading and transition presentation must represent real operation state, preserve persistent application surfaces, coordinate cover/reveal and gates, expose terminal failures, and correlate diagnostics without becoming the authority for Route, Activity, or readiness.

## Decision

The framework owns persistent transition/loading surfaces and a typed orchestration path. Cover, technical loading, readiness waiting, reveal, failure, supersession, and cleanup are explicit phases. Presentation reports state; it does not calculate readiness or own destination authority. Logs distinguish operational summaries from debug/trace evidence.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

Persistent loading/transition surfaces, progress reporters, gates, typed results, diagnostics, and Activity readiness integration exist. Participant-aware progress reserves a final readiness range. Typed supersession for Route-authority replacement remains distinct from ordinary failure/cancellation.

**IF-TXN-01** closes the missing GameFlow authority bridge for Transition phase results:

```text
Transition = execution + typed TransitionResult (still passive; no lifecycle ownership)
GameFlow  = transaction decision from TransitionResult

pre-commit Transition failure
  Transition Before is not Completed (Failed/Rejected/Cancelled/invalid)
  → destination lifecycle is not started
  → previous authority remains
  → FailedPreCommitTransition (Route/Activity) / PreCommitTransitionFailed (startup)
  → transition gate released safely; no committed-target recovery

committed-target reveal failure
  destination already committed
  Transition After / reveal is not Completed
  → destination remains current authority
  → request/start must not Succeeded/Started
  → FailedCommittedTargetReveal (distinct from FailedCommittedTargetNotReady)
  → committed-target reveal recovery gate applied (policy source IF-TXN-01, not readiness)
  → no automatic blind rollback

CompletedWithWarnings is still TransitionResult.Completed and continues the transaction.
Loading may complete technical/readiness projection before After; Loading is not authority.
revealCompleted / normal success only after accepted After phase.
```

## Current QA evidence

Current QA needs a rebuilt canonical transition matrix after cleanup. Historical evidence remains useful context but is not current certification.

## Current FIRSTGAME evidence

FIRSTGAME demonstrates covered/visible loading paths and has exposed real readiness/Route replacement edge cases that drove the latest fix.

## What remains

- Host/FIRSTGAME integration proof for deliberate required-surface/adapter failure on Before and After (without leaving product surfaces broken).
- Broader compensation after partial commit beyond typed reveal recovery (no generic rollback manager).
- Publish a dedicated Transition/Loading product template and policy guide.
- Add QA for Route replacement while waiting under WaitVisible and WaitCovered.
- Ensure every external host terminal path surfaces destination identity, operation sequence, revision/occurrence, loading diagnostics, and cleanup status.

## Completion criteria

- No terminal path leaves cover, gate, progress, or transition state leaked.
- Intentional supersession is distinguishable from failure.
- Loading reaches terminal completion only when the governing operation is terminal.
- QA and FIRSTGAME prove success, failure, cancellation, supersession, and recovery.

## Completion assessment

```text
Estimated completion: 91%
Normative status: Accepted
Package implementation: IF-TXN-01 pre-commit vs committed-target reveal failure authority implemented
QA evidence: package unit tests + GameFlow Transition Failure Authority diagnostics smoke
FIRSTGAME evidence: evaluated at e551643; deliberate broken-surface proof still optional follow-up
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
