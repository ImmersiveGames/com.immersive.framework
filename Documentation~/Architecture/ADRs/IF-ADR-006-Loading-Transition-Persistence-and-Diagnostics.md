# IF-ADR-006 — Loading, Transition, Persistence and Diagnostics

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **94%**  
Implementation classification: **Core orchestration + IF-TXN-01/02/03A transition authority and terminal gate integrity implemented and QA-certified; exceptional post-commit cleanup/compensation diagnostics and product-template gaps remain**  
Related decisions: IF-ADR-001, IF-ADR-005, IF-ADR-007, IF-ADR-011, IF-ADR-015  
Current package baseline: `c457e8cd7a11b8f2ce816734b4d97a3a820b4eec` (`IF-TXN-03A`)  
Current QA baseline: `c99df1e77a8408e6b48124a5d371f09e9af52019` (`IF-TXN-03A`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Transaction cuts: **IF-TXN-01 — COMPLETE**; **IF-TXN-02 — COMPLETE**; **IF-TXN-03A — CLOSED / CERTIFIED**

> The normative architectural decision is preserved. Completion percentages are planning estimates, not automated release certification.

## Context

Loading and transition presentation must represent real operation state, preserve persistent application surfaces, coordinate cover/reveal and gates, expose terminal failures, and correlate diagnostics without becoming the authority for Route, Activity, or readiness.

## Decision

The framework owns persistent transition/loading surfaces and a typed orchestration path. Cover, technical loading, readiness waiting, reveal, failure, supersession, recovery and cleanup are explicit phases. Presentation reports state; it does not calculate readiness or own destination authority. Logs distinguish operational summaries from debug/trace evidence.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

Persistent loading/transition surfaces, progress reporters, gates, typed results, diagnostics, and Activity readiness integration exist. Participant-aware progress reserves a final readiness range. Typed supersession for Route-authority replacement remains distinct from ordinary failure/cancellation.

### IF-TXN-01 + IF-TXN-02

Transition results govern transaction continuation for Game Application startup, Route, Activity, Clear and Restart. A non-accepted `Before` aborts before the governing lifecycle mutation. A non-accepted `After` after commit preserves real committed authority, does not report success and does not blindly rollback.

### IF-TXN-03A — gate/recovery diagnostic separation

IF-TXN-03A resolved a semantic projection defect without changing the transition transaction shape.

Canonical projections:

```text
TransitionGateSnapshot
  -> pure GameFlow Transition Gate

CurrentTransitionGateMode
  -> pure Transition Gate mode

ActivityEntryReadinessGateSnapshot
  -> Transition Gate + Activity Entry Readiness Recovery Gate

CurrentGateSnapshot
  -> broader operational gate composition
```

The Transition Gate release operation is internal state cleanup. It is not an external resource release that can be rejected by an owner/token protocol.

A committed-target readiness failure may validly produce:

```text
gateReleased = true
recoveryGate = true
```

with pure Transition Gate state clean and the readiness-composite gate still blocked. Loading/Transition presentation may remain retained according to the typed failure policy while recovery protects the destination.

This separation prevents diagnostics from falsely reporting a Transition Gate leak when the actual blocker is readiness recovery.

## Current QA evidence

Canonical Unity evidence after the IF-TXN-03A compatibility update:

```text
IF-TXN-03A Transition Gate Terminal Integrity
  PASS — 16/16

IF-TXN-02 Clear/Restart Transition Authority
  PASS — 16/16

IF-TXN-01 Transition Failure Authority
  PASS — 22/22

Participant-Aware Readiness Loading Terminal
  PASS — 34/34

Direct Activity Readiness Policies
  PASS — 42/42

Participant-Aware Readiness Loading Progress
  PASS — 32/32

Participant-Aware Startup Parity — Route
  PASS — 25/25

Participant-Aware Startup Parity — Game Application
  PASS — 20/20
```

The terminal regression proves the deliberate `RequiredParticipantFailed` path retains recovery while the pure Transition Gate is already released. The progress and direct-policy regressions prove normal wait/reveal ordering and final cleanup. Startup parity proves the same gate release contract for Route startup and Game Application startup.

## Current FIRSTGAME evidence

FIRSTGAME demonstrates covered/visible loading paths and real Player/readiness composition. IF-TXN-03A is a technical gate-state/diagnostics correction and requires no new FIRSTGAME cut for closure.

## What remains

The previously suspected generic “Transition Gate release failure” residual is closed for the current model: canonical release is unconditional internal state replacement, and no external refusal contract exists.

Remaining ADR-006 gaps are separate:

- Audit consumer/loading hook exceptions after commit.
- Audit disposal during partial presentation and verify terminal cleanup evidence.
- Add adapter partial-side-effect compensation only for concrete demonstrated paths; do not introduce a generic rollback manager by default.
- Improve full terminal cleanup receipts and external-host correlation of destination identity, operation sequence, revision/occurrence, Loading diagnostics, Transition diagnostics and cleanup state.
- Publish a dedicated Transition/Loading product template and policy guide.
- Add public-only Player waiting/joining integration cases where useful without weakening WaitCovered or making Loading an authority.

## Completion criteria

- No supported terminal path reports false success after a non-accepted Transition phase.
- Intentional supersession is distinguishable from failure.
- Loading reaches successful terminal completion only when the governing readiness projection permits it.
- Transition failure before commit and reveal failure after commit are typed and authority-correct for Start/Route/Activity/Clear/Restart.
- Transition Gate current-state diagnostics are not contaminated by a separate readiness recovery blocker.
- Exceptional post-commit hook/disposal/partial-side-effect failures are explicit and correlated when those paths are implemented.
- QA proves success, failure, cancellation, supersession, recovery and terminal cleanup for the supported boundary.

## Completion assessment

```text
Estimated completion: 94%
Normative status: Accepted
IF-TXN-01: CLOSED / CERTIFIED
IF-TXN-02: CLOSED / CERTIFIED
IF-TXN-03A: CLOSED / CERTIFIED
Operational Transition Gate leak: NO
Fallible canonical Transition Gate release: NO
Canonical evidence: 16/16 + 16/16 + 22/22 + 34/34 + 42/42 + 32/32 + startup 25/25 + 20/20
Residuals: post-commit hook/disposal exceptions, concrete adapter compensation/cleanup diagnostics, product templates
```
