# IF-ADR-006 — Loading, Transition, Persistence and Diagnostics

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **91%**  
Implementation classification: **Core orchestration + IF-TXN-01 transition failure authority implemented and QA-certified; residual compensation, terminal cleanup diagnostics and product-template gaps remain**  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-011, IF-ADR-015  
Current package baseline: `d0955e0dc58a3cc70f8533f92d63246d941d5e20` (`IF-TXN-01 COMPLETE`)  
Current QA baseline: `00cedcb78d200b1b2094eafc500e348e07dc36ab` (`IF-TXN-01 COMPLETE`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Transaction cut: **IF-TXN-01 GameFlow Transition Failure Authority — COMPLETE**

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

**IF-TXN-01** and **IF-TXN-02** close the GameFlow authority bridge for Transition phase results on Route/Activity/startup and Activity Clear/Restart:

```text
Transition = execution + typed TransitionResult
GameFlow  = transaction decision from TransitionResult

pre-commit Transition failure
  Transition Before is not accepted
  → destination lifecycle / Clear / Restart clear+re-enter is not started
  → previous authority remains
  → FailedPreCommitTransition (Route/Activity/Clear/Restart) / PreCommitTransitionFailed (startup)
  → transition gate released safely
  → no committed-target recovery

committed-target reveal / post-commit presentation failure
  destination already committed
  Transition After / reveal is not accepted
  → destination remains current authority
    Clear: CurrentActivity stays None (no restore of previous Activity)
    Restart: re-entered Activity/occurrence stays current (no rollback to prior occurrence)
  → request/start/restart must not Succeeded/Started/Completed
  → FailedCommittedTargetReveal
  → committed-target reveal recovery gate applied when an Activity occurrence remains
  → policy source remains IF-TXN-01, not readiness
  → no automatic blind rollback

Accepted phase
  → TransitionResult.Completed
  → or intentional policy/no-visual Skipped

CompletedWithWarnings remains Completed.
Required Failed/Rejected/Cancelled are not accepted or masked as Skipped.
Loading may complete its technical/readiness projection before After; Loading is not authority.
Normal revealCompleted / success is only produced after an accepted After phase.
```

## Current QA evidence

The core Transition/Loading/Readiness boundary is re-certified in the canonical QAFramework:

```text
IF-TXN-01 Transition Failure Authority Regression
  Passed — 22/22

Direct Activity Readiness Policies Regression
  Passed — 42/42
  WaitVisible = Passed
  WaitCovered = Passed

Participant-Aware Readiness Loading Terminal Regression
  Passed — 34/34
  confirms committed destination authority
  confirms progress remains below terminal success on required failure
  confirms Loading/Transition retention and recovery gate

Participant-Aware Readiness Loading Progress Regression
  Passed — 32/32
  required=4
  optional=1
  optional failure non-blocking
  ordering: Technical<100 → 0/4 → 1/4 → 2/4 → 3/4 → 4/4=100 → Hide → Reveal → GateRelease

Activity Readiness Post-Transition Smoke
  Passed — Ready→NotReady, NotReady→Ready, identical-value ignored, newRequest=False

Identity Authority Regression
  Passed — 6/6, failed=0
```

The negative terminal regression intentionally emits a runtime error record for `RequiredParticipantFailed`; that record is expected evidence, and the runner still terminates `Passed` with retained recovery protection and authoritative committed destination.

## Current FIRSTGAME evidence

FIRSTGAME demonstrates covered/visible loading paths and exposed the real WaitCovered + Player external-progression composition trap that drove the authoring warning and causal audit. Deliberately breaking a required Transition surface in FIRSTGAME remains optional; QA now owns technical proof of the IF-TXN-01 failure contract.

## What remains

- Audit Activity Clear/Restart transition paths that remain outside IF-TXN-01 authority wiring.
- Audit gate-release failure, disposal during partial presentation, and cleanup evidence before any broader compensation cut.
- Define compensation after partial side effects only where a concrete terminal path requires it; do not introduce a generic rollback manager by default.
- Publish a dedicated Transition/Loading product template and policy guide.
- Add the still-missing public-only Player waiting/joining integration cases where useful, without weakening WaitCovered or making Loading an authority.
- Ensure every external host terminal path surfaces destination identity, operation sequence, revision/occurrence, loading diagnostics and cleanup state consistently.

## Completion criteria

- No terminal path leaves cover, gate, progress, or transition state leaked.
- Intentional supersession is distinguishable from failure.
- Loading reaches successful terminal completion only when the governing readiness projection permits it.
- Transition failure before commit and reveal failure after commit are typed and authority-correct.
- QA proves success, failure, cancellation, supersession and recovery for the supported boundary.

## Completion assessment

```text
Estimated completion: 91%
Normative status: Accepted
IF-TXN-01 implementation: COMPLETE
IF-TXN-01 QA certification: PASS
Play Mode readiness/loading recertification: PASS for the executed canonical suites
Residuals: Clear/Restart transition authority, gate-release/partial-presentation cleanup, broader diagnostics/product templates
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. The
percentage is intentionally not raised merely because the IF-TXN-01 QA suite passed;
remaining ADR-006 product and terminal-cleanup gaps are still real.
