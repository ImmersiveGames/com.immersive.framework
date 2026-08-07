# IF-ADR-006 — Loading, Transition, Persistence and Diagnostics

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **92%**  
Implementation classification: **Core orchestration + IF-TXN-01/IF-TXN-02 transition authority implemented and QA-certified; exceptional cleanup/compensation diagnostics and product-template gaps remain**  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-011, IF-ADR-015  
Current package baseline: `193e7e954deaa430920f7967b5061b4b950ed1bb` (`IF-TXN-02`)  
Current QA baseline: `cf3cf625260ff717d6bcc919703e6868b085285f` (`IF-TXN-02`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Transaction cuts: **IF-TXN-01 — COMPLETE**; **IF-TXN-02 — COMPLETE**

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

**IF-TXN-01** and **IF-TXN-02** close the GameFlow authority bridge for Transition phase results across the supported canonical lifecycle paths:

```text
Game Application startup
Route request
Activity request
Activity Clear
Activity Restart
```

The governing rule is:

```text
Transition = execution + typed TransitionResult
GameFlow  = transaction decision from TransitionResult

accepted phase
  TransitionResult.Completed
  OR intentional policy/no-visual Skipped

pre-commit Transition failure
  Before not accepted
  → lifecycle mutation is not started
  → previous authority remains
  → typed pre-commit Transition failure
  → ordinary transition gate released
  → no committed-target recovery

post-commit Transition failure
  mutation already committed
  After/reveal not accepted
  → preserve real committed authority
  → request/start/restart is not success
  → typed FailedCommittedTargetReveal
  → no blind rollback
  → committed-target reveal recovery applies when a valid Activity occurrence remains
```

Authority-specific presentation semantics:

```text
Activity Clear
  Clear lifecycle completes
  → CurrentActivity = None
  → After failure does not recreate previous Activity
  → removed-Activity readiness recovery is not retained

Activity Restart
  Re-enter lifecycle completes
  → new Activity occurrence is authoritative
  → After failure does not restore old occurrence
  → reveal recovery may bind to the new occurrence
```

`CompletedWithWarnings` remains accepted through `TransitionResult.Completed`. Required `Failed`, `Rejected`, `Cancelled`, or invalid Transition results are not accepted. Loading may complete its technical/readiness projection before After; Loading is not lifecycle authority. Successful reveal/request completion is only produced after the governing accepted terminal phase.

## IF-TXN-02 certification record

IF-TXN-02 adds parity for Clear/Restart while preserving the existing orchestration shape and avoiding a generic transaction framework.

```text
Clear Before failure
→ no clear lifecycle
→ previous Activity remains
→ FailedPreCommitTransition / ActivityClear

Clear After failure
→ clear already committed
→ CurrentActivity remains None
→ FailedCommittedTargetReveal / ActivityClear
→ not Succeeded

Restart Before failure
→ no Clear and no Re-enter
→ previous Activity + occurrence remain
→ Restart failure

Restart Re-enter committed + After failure
→ new Activity + occurrence remain authoritative
→ Restart is not Completed
→ re-enter result = FailedCommittedTargetReveal
→ no rollback to old occurrence
```

The implementation reuses the IF-TXN-01 acceptance helpers and existing transition failure kinds. `FrameworkActivityRequestResult` factories carry `GameFlowRequestOperationKind` so Clear can report `ActivityClear` without adding unnecessary result kinds.

## Current QA evidence

The core Transition/Loading/Readiness boundary is re-certified in canonical QAFramework against the IF-TXN-02 package/QA baselines:

```text
IF-TXN-02 Clear/Restart Transition Authority Regression
  Passed — 16/16

IF-TXN-01 Transition Failure Authority Regression
  Passed — 22/22

Direct Activity Readiness Policies Regression
  Passed — 42/42
  WaitVisible = Passed
  WaitCovered = Passed

Participant-Aware Readiness Loading Terminal Regression
  Passed — 34/34
  committed destination remains authoritative
  terminal failure does not become success
  Loading/Transition retention and recovery gate remain correct

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

The IF-TXN-02 focused regression proves phase acceptance/rejection, Clear and Restart pre/post-commit terminals, correct authority flags, Restart non-completion on reveal failure, source wiring, and no-blind-rollback semantics.

The Play Mode non-regression matrix also exercises successful Clear cleanup paths under readiness/loading tests. The negative readiness terminal regression intentionally emits a runtime error for `RequiredParticipantFailed`; this is expected evidence and the final runner status is `Passed`.

A dedicated host Play Mode adapter that deliberately fails Clear/Restart Transition effects remains optional hardening, not a blocker for the current technical certification.

## Current FIRSTGAME evidence

FIRSTGAME demonstrates covered/visible loading paths and exposed the real WaitCovered + Player external-progression composition trap that drove the authoring warning and causal audit. Deliberately breaking a required Transition surface for Clear/Restart in FIRSTGAME remains optional; QA owns the technical proof of the supported failure contract.

## What remains

The Clear/Restart Transition authority residual is closed. Remaining ADR-006 gaps are separate cuts:

- Audit transition/gate-release failure when cleanup/release itself fails.
- Audit consumer/loading hook exception after commit.
- Audit disposal during partial presentation and verify terminal cleanup evidence.
- Add adapter partial-side-effect compensation only for concrete demonstrated paths; do not introduce a generic rollback manager by default.
- Improve full terminal cleanup receipts and external-host correlation of destination identity, operation sequence, revision/occurrence, Loading diagnostics, and cleanup state.
- Publish a dedicated Transition/Loading product template and policy guide.
- Add still-missing public-only Player waiting/joining integration cases where useful without weakening WaitCovered or making Loading an authority.

## Completion criteria

- No supported terminal path reports false success after a non-accepted Transition phase.
- Intentional supersession is distinguishable from failure.
- Loading reaches successful terminal completion only when the governing readiness projection permits it.
- Transition failure before commit and reveal failure after commit are typed and authority-correct for supported Start/Route/Activity/Clear/Restart paths.
- Exceptional cleanup/gate failures are explicit and diagnostically correlated when those paths are implemented.
- QA proves success, failure, cancellation, supersession and recovery for the supported boundary.

## Completion assessment

```text
Estimated completion: 92%
Normative status: Accepted
IF-TXN-01 implementation: COMPLETE
IF-TXN-01 QA certification: PASS
IF-TXN-02 implementation: COMPLETE
IF-TXN-02 QA certification: PASS
Play Mode readiness/loading non-regression: PASS
Canonical evidence: 16/16 + 22/22 + 42/42 + 34/34 + 32/32 + post-transition PASS + identity 6/6
Residuals: gate-release/partial-presentation cleanup, concrete compensation/cleanup diagnostics, product templates
```

The percentage increases modestly because Clear/Restart were an explicit ADR-006
runtime residual and are now both implemented and certified. It is not raised merely
because more tests were executed; exceptional cleanup and product-surface gaps remain.
