# IF-ADR-001 — Core Lifecycle and Runtime Authority

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **90%**  
Implementation classification: **Substantially implemented; IF-TXN-01 certified in canonical QA; residual Session-Persistent Player and broader compensation remain**  
Related decisions: IF-ADR-003, IF-ADR-006, IF-ADR-007, IF-ADR-014  
Current package baseline: `d0955e0dc58a3cc70f8533f92d63246d941d5e20` (`IF-TXN-01 COMPLETE`)  
Current QA baseline: `00cedcb78d200b1b2094eafc500e348e07dc36ab` (`IF-TXN-01 COMPLETE`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Transaction cut: **IF-TXN-01 GameFlow Transition Failure Authority — COMPLETE**

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
Route request
Activity request
Game Application startup
Activity Clear
Activity Restart
```

```text
pre-commit Transition failure (Before not accepted)
  → do not start destination lifecycle / Clear / Restart clear+re-enter
  → previous Route/Activity authority remains
  → typed FailedPreCommitTransition terminal
  → safe transition-gate cleanup
  → no committed-target recovery

committed-target reveal / post-commit presentation failure (After not accepted after commit)
  → keep committed destination authoritative
    Route/Activity switch: committed target remains current
    Clear: no-Activity remains authority (never restore previous Activity)
    Restart: re-entered Activity/occurrence remains authority (never roll back to old occurrence)
  → do not return Succeeded / Started / Restart Completed
  → no blind rollback
  → apply committed-target reveal recovery protection when an Activity occurrence remains
  → typed FailedCommittedTargetReveal terminal
  → distinct from FailedCommittedTargetNotReady (readiness)

TransitionResult.Completed remains accepted.
CompletedWithWarnings therefore continues the transaction.
Policy Skipped remains accepted only as intentional policy/no-visual completion.
Required Failed/Rejected/Cancelled results are not masked as Skipped.
```

Transition remains execution + typed result; GameFlow decides transaction continuation and terminal outcome. Route/Activity remain lifecycle authority. Loading remains presentation/progress rather than lifecycle authority.

## Current QA evidence

IF-TXN-01 is certified in the canonical QAFramework boundary against the current package/QA baselines.

```text
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

This evidence proves the IF-TXN-01 transaction decision, the WaitVisible/WaitCovered Play Mode integration, participant-aware Loading success/failure terminals, post-transition readiness mutation without a new request, and identity/ownership/supersession non-regression.

## Current FIRSTGAME evidence

FIRSTGAME proves application boot, Route/Activity flow, Player participation, and additive content in real consumer scenes. Demo03 adds current consumer evidence for cross-scene Player provisioning UX. A deliberately broken required Transition surface is not required to close IF-TXN-01 because the technical failure boundary is certified in QA; such a consumer demonstration remains optional diagnostic/product evidence.

## What remains

- Broaden compensation vocabulary beyond IF-TXN-01 pre-commit vs committed-target reveal terminals; generic rollback/retry remain out of scope unless a later cut explicitly requires them.
- Audit/cover terminal integrity for Activity Clear/Restart paths that are outside the current IF-TXN-01 authority wiring.
- Audit gate-release failure and cleanup evidence on exceptional/partial presentation paths before introducing any broader compensation mechanism.
- Define and implement the Session-Persistent Logical Player source and its authoring/runtime contract.
- Publish a concise lifecycle diagram and diagnostic correlation guide for Session, Route, Activity, revision, occurrence, Transition operation and terminal cleanup.

## Completion criteria

- No static/global runtime authority or silent fallback is introduced.
- Every transition terminal path produces typed, correlated evidence.
- Session-Persistent Player source has explicit lifetime, authoring, release, QA, and consumer proof.
- Canonical QA passes against the current package boundary.

## Completion assessment

```text
Estimated completion: 90%
Normative status: Accepted
IF-TXN-01 implementation: COMPLETE
IF-TXN-01 QA certification: PASS
Canonical evidence: 22/22 + 42/42 + 34/34 + 32/32 + post-transition PASS + identity 6/6
Residuals: Session-Persistent Player, Clear/Restart transaction authority, broader compensation/cleanup diagnostics
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. IF-TXN-01
closure removes the previously open Transition failure-authority gap, but it does not
claim completion of unrelated ADR-001 residuals.
