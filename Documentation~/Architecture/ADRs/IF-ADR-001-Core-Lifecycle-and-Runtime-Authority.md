# IF-ADR-001 — Core Lifecycle and Runtime Authority

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **92%**  
Implementation classification: **Substantially implemented; IF-TXN-01, IF-TXN-02 and IF-TXN-03A are implemented and QA-certified; Session-Persistent Player and exceptional post-commit cleanup/compensation work remain**  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-006, IF-ADR-007, IF-ADR-011, IF-ADR-014  
Current package baseline: `c457e8cd7a11b8f2ce816734b4d97a3a820b4eec` (`IF-TXN-03A`)  
Current QA baseline: `c99df1e77a8408e6b48124a5d371f09e9af52019` (`IF-TXN-03A`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Transaction cuts: **IF-TXN-01 — COMPLETE**; **IF-TXN-02 — COMPLETE**; **IF-TXN-03A — CLOSED / CERTIFIED**

> The normative architectural decision is preserved. Completion percentages are planning estimates, not automated release certification.

## Context

The framework requires one explicit owner for application/session composition, Route and Activity lifecycle, scene/content ownership, and feature runtime bindings without creating globally discoverable mutable state. Session-scoped participation may outlive Route and Activity changes, so contextual gameplay ownership must not be confused with Session authority.

## Decision

`com.immersive.framework` owns framework-specific lifecycle and product modules. `FrameworkRuntimeHost` is the internal application/session composition root and must not expose a static current-host registry, service locator, hierarchy lookup, or implicit singleton access path. Runtime dependencies are supplied through narrow typed ports and explicit composition.

The ownership hierarchy is:

```text
Game Application / Session
  -> Session-scoped authorities and participants
     -> Logical Players
  -> Route
     -> Activity
        -> contextual projection, readiness and materialization
```

Route and Activity own contextual lifecycle, not Session participant identity. Missing required bindings fail explicitly. Functional identity is typed and domain-specific; names and paths are diagnostic data, not authority.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The package contains the scoped runtime host, bootstrap composition, Route/Activity runtimes, scene lifecycle, runtime-content ownership, explicit ports, typed results, and Session-scoped Player participation authority. Manager-Provisioned and Scene-Provided Player sources exist. Typed supersession/interruption exists when Route authority replaces an in-flight Activity readiness operation.

### IF-TXN-01 + IF-TXN-02 — transition outcome authority

Transition outcomes are authoritative at the GameFlow transaction boundary for:

```text
Game Application startup
Route request
Activity request
Activity Clear
Activity Restart
```

Canonical continuation rule:

```text
accepted Transition phase
  -> TransitionResult.Completed
  -> or intentional policy/no-visual TransitionStatus.Skipped

non-accepted Transition Before
  -> do not advance the governing lifecycle mutation
  -> preserve previous committed authority
  -> typed pre-commit Transition failure

non-accepted Transition After after commit
  -> never convert the request into success
  -> preserve the authority that actually committed
  -> no blind rollback
  -> typed committed-target reveal failure
  -> committed-target reveal recovery when a valid Activity occurrence remains authoritative
```

Clear post-commit authority remains `CurrentActivity=None`. Restart post-commit authority remains the re-entered Activity/new occurrence. `CompletedWithWarnings` remains accepted through `TransitionResult.Completed`; required `Failed`, `Rejected`, `Cancelled`, or invalid results are not accepted.

### IF-TXN-03A — Transition Gate terminal integrity

IF-TXN-03A certifies that the GameFlow Transition Gate is **internal operation state**, not an externally acquired resource with a fallible release protocol.

Canonical current-state projections are now distinct:

```text
CurrentTransitionGateSnapshot / host.TransitionGateSnapshot
  -> Transition Gate only

CurrentTransitionGateMode / host.CurrentTransitionGateMode
  -> Transition Gate mode only

CurrentActivityEntryReadinessGateSnapshot / host.ActivityEntryReadinessGateSnapshot
  -> Transition Gate + Activity Entry Readiness Recovery Gate

host.CurrentGateSnapshot
  -> operational combined view, including Pause + readiness composition
```

A valid committed-readiness failure may therefore have:

```text
TransitionGateSnapshot.HasBlockers == false
CurrentTransitionGateMode == None
ActivityEntryReadinessGateSnapshot.HasBlockers == true
```

This means the Transition Gate was released while readiness recovery intentionally remains authoritative. It is not a Transition Gate leak.

Canonical Transition Gate release is unconditional internal state replacement. The audited model has no external release refusal, token ownership mismatch, or fallible release operation. No lease/release manager, generic transaction manager, or silent fallback was introduced.

## Current QA evidence

Manual Unity certification on 2026-08-07 against the IF-TXN-03A package/QA workspaces:

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
  WaitVisible PASS
  WaitCovered PASS

Participant-Aware Readiness Loading Progress
  PASS — 32/32

Participant-Aware Startup Parity — Route
  PASS — 25/25

Participant-Aware Startup Parity — Game Application
  PASS — 20/20
```

The IF-TXN-03A regression directly covers pure Transition Gate projection, preserved readiness composition, success/failure terminal cleanup, fallback cleanup through `finally`, exception/fault cleanup, Clear/Restart wiring, readiness-recovery separation, recovery cleanup and host-surface separation.

The readiness compatibility suites prove that during a required failure the Transition Gate may be clean while the recovery gate remains active, and that final cleanup clears both the pure and composite projections.

## Current FIRSTGAME evidence

FIRSTGAME proves application boot, Route/Activity flow, Player participation, and additive content in real consumer scenes. IF-TXN-03A is a technical internal-state/projection correction and does not require an additional FIRSTGAME cut for closure.

## What remains

IF-TXN-03A closes the previously suspected Transition Gate release/leak residual. Remaining ADR-001 work is separate:

- Audit consumer/loading hook exceptions after commit.
- Audit disposal during partial presentation and correlate terminal cleanup evidence.
- Define adapter partial-side-effect compensation only for concrete demonstrated paths; do not introduce a generic rollback/retry manager by default.
- Improve full terminal cleanup receipts and lifecycle/diagnostic correlation evidence.
- Define and implement the Session-Persistent Logical Player source and its authoring/runtime contract.
- Publish a concise lifecycle/diagnostic correlation guide for Session, Route, Activity, revision, occurrence, Transition operation and cleanup.

Do **not** reopen a generic “Transition Gate release can fail” work item without new evidence. The current canonical release model is internal, unconditional state cleanup.

## Completion criteria

- No static/global runtime authority or silent fallback is introduced.
- Every supported transition terminal path produces typed, correlated evidence.
- Committed authority always reflects the runtime mutation that actually completed.
- Transition Gate and readiness-recovery state remain semantically distinct in diagnostics/current-state projections.
- Session-Persistent Player source has explicit lifetime, authoring, release, QA, and consumer proof.
- Canonical QA passes against the current package boundary.

## Completion assessment

```text
Estimated completion: 92%
Normative status: Accepted
IF-TXN-01: CLOSED / CERTIFIED
IF-TXN-02: CLOSED / CERTIFIED
IF-TXN-03A: CLOSED / CERTIFIED
Canonical evidence: 16/16 + 16/16 + 22/22 + 34/34 + 42/42 + 32/32 + startup 25/25 + 20/20
Operational Transition Gate leak demonstrated: NO
Fallible external Transition Gate release contract: NO
Residuals: Session-Persistent Player, post-commit hook/disposal exceptions, concrete compensation and cleanup diagnostics
```

The percentage increases modestly because IF-TXN-03A closes a concrete runtime projection and QA-certification residual. Unrelated ADR-001 product and exceptional-cleanup work remains open.
