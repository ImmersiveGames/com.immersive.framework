# IF-ADR-001 — Core Lifecycle and Runtime Authority

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **91%**  
Implementation classification: **Substantially implemented; IF-TXN-01 and IF-TXN-02 are implemented and certified in canonical QA; residual Session-Persistent Player and exceptional terminal-cleanup/compensation work remain**  
Related decisions: IF-ADR-003, IF-ADR-006, IF-ADR-007, IF-ADR-014  
Current package baseline: `193e7e954deaa430920f7967b5061b4b950ed1bb` (`IF-TXN-02`)  
Current QA baseline: `cf3cf625260ff717d6bcc919703e6868b085285f` (`IF-TXN-02`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Transaction cuts: **IF-TXN-01 GameFlow Transition Failure Authority — COMPLETE**; **IF-TXN-02 Activity Clear/Restart Transition Authority Parity — COMPLETE**

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

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

**IF-TXN-01** and **IF-TXN-02** make Transition phase outcomes authoritative at the GameFlow transaction boundary for:

```text
Game Application startup
Route request
Activity request
Activity Clear
Activity Restart
```

The canonical continuation rule is:

```text
accepted Transition phase
  → TransitionResult.Completed
  → or intentional TransitionStatus.Skipped for policy/no-visual execution

non-accepted Transition Before
  → do not advance the governing lifecycle mutation
  → preserve the previous committed authority
  → typed FailedPreCommitTransition / PreCommitTransitionFailed terminal
  → release the ordinary transition gate
  → no committed-target recovery

non-accepted Transition After after commit
  → never convert the request into success
  → preserve the authority that actually committed
  → no blind rollback
  → typed FailedCommittedTargetReveal terminal
  → apply committed-target reveal recovery when a valid Activity occurrence remains authoritative
```

Authority-specific post-commit semantics are now explicit:

```text
Route / Activity switch
  committed target remains current

Activity Clear
  CurrentActivity remains None
  previous Activity is not restored

Activity Restart
  re-entered Activity and new occurrence remain current
  old occurrence is not restored
```

`CompletedWithWarnings` remains accepted through `TransitionResult.Completed`. Required `Failed`, `Rejected`, `Cancelled`, or invalid results are not accepted. Transition remains execution + typed result; GameFlow decides transaction continuation and terminal outcome. Route/Activity remain lifecycle authority. Loading remains presentation/progress rather than lifecycle authority.

## IF-TXN-02 certification record

IF-TXN-02 extends the IF-TXN-01 authority contract without introducing a transaction manager, generic rollback, retry, or silent recovery.

### Activity Clear

```text
Before not accepted
→ Clear lifecycle is not called
→ previous Activity remains authority
→ FailedPreCommitTransition
→ OperationKind = ActivityClear

Clear committed + After not accepted
→ no-Activity remains authority
→ previous Activity is not restored
→ request is not Succeeded
→ FailedCommittedTargetReveal
→ Activity readiness recovery belonging to the removed Activity is released
```

### Activity Restart

```text
Before not accepted
→ no Clear
→ no Re-enter
→ previous Activity and occurrence remain authority
→ Restart fails

Clear fails
→ no Re-enter
→ existing clear-stage terminal semantics remain

Clear committed + Re-enter fails
→ old occurrence is not recreated
→ Restart fails with the real resulting authority

Re-enter committed + After not accepted
→ new Activity / occurrence remains authority
→ Restart is not Completed
→ FailedCommittedTargetReveal on the re-enter stage
→ reveal recovery may bind to the new occurrence
→ no rollback to the old occurrence
```

## Current QA evidence

The transaction/readiness/identity boundary was manually re-certified on 2026-08-07 against the IF-TXN-02 package and QA workspaces.

```text
IF-TXN-02 Clear/Restart Transition Authority Regression
  status: Passed
  cases: 16/16

IF-TXN-01 Transition Failure Authority Regression
  status: Passed
  cases: 22/22

Direct Activity Readiness Policies Regression
  status: Passed
  cases: 42/42
  WaitVisible: Passed
  WaitCovered: Passed

Participant-Aware Readiness Loading Terminal Regression
  status: Passed
  cases: 34/34

Participant-Aware Readiness Loading Progress Regression
  status: Passed
  cases: 32/32

Activity Readiness Post-Transition Smoke
  status: Passed
  ReadyToNotReady
  NotReadyToReady
  IdenticalValueIgnored
  newRequest=False

Identity Authority Regression
  status: Passed
  executed: 6
  completed: 6
  failed: 0
```

The focused IF-TXN-02 regression proves accepted/rejected phase semantics, Clear pre/post-commit terminals, Restart pre/post-commit terminals, real-authority preservation, `Restart.Completed == false` on reveal failure, source wiring, and no-blind-rollback messaging. The Play Mode suites additionally prove no regression in readiness, Loading, cleanup Clear calls, occurrence mutation, supersession, and identity authority.

The participant-aware terminal regression intentionally emits a runtime error for the deliberate `RequiredParticipantFailed` case; its final runner status is `Passed`, with the committed destination authoritative and recovery protection retained.

## Current FIRSTGAME evidence

FIRSTGAME proves application boot, Route/Activity flow, Player participation, and additive content in real consumer scenes. Demo03 provides current consumer evidence for cross-scene Player provisioning UX. A deliberately broken Transition surface for Clear/Restart is not required to close IF-TXN-02 because the technical failure boundary is certified in QA; such a consumer demonstration remains optional diagnostic/product evidence.

## What remains

The Clear/Restart transaction-authority gap is closed. Remaining ADR-001 work is intentionally narrower and separate:

- Audit transition/gate-release failure where ordinary cleanup itself fails.
- Audit consumer/loading hook exceptions after commit and disposal during partial presentation.
- Define adapter partial-side-effect compensation only where a concrete terminal path requires it; do not introduce a generic rollback/retry manager by default.
- Improve full terminal cleanup receipts and lifecycle/diagnostic correlation evidence.
- Define and implement the Session-Persistent Logical Player source and its authoring/runtime contract.
- Publish a concise lifecycle diagram and diagnostic correlation guide for Session, Route, Activity, revision, occurrence, Transition operation and terminal cleanup.

## Completion criteria

- No static/global runtime authority or silent fallback is introduced.
- Every supported transition terminal path produces typed, correlated evidence.
- Committed authority always reflects the runtime mutation that actually completed.
- Session-Persistent Player source has explicit lifetime, authoring, release, QA, and consumer proof.
- Canonical QA passes against the current package boundary.

## Completion assessment

```text
Estimated completion: 91%
Normative status: Accepted
IF-TXN-01 implementation: COMPLETE
IF-TXN-01 QA certification: PASS
IF-TXN-02 implementation: COMPLETE
IF-TXN-02 QA certification: PASS
Canonical evidence: 16/16 + 22/22 + 42/42 + 34/34 + 32/32 + post-transition PASS + identity 6/6
Residuals: Session-Persistent Player, exceptional gate/presentation cleanup, concrete compensation/cleanup diagnostics
```

The percentage increases modestly because IF-TXN-02 closes an actual runtime/contract
residual and is certified in QA; it is not raised merely because additional smokes were
executed. Unrelated ADR-001 product and exceptional-cleanup gaps remain open.
