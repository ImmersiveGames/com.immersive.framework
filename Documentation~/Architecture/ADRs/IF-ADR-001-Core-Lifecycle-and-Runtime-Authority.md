# IF-ADR-001 — Core Lifecycle and Runtime Authority

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **90%**  
Implementation classification: **Substantially implemented; residual Session-Persistent Player and broader compensation remain**  
Related decisions: IF-ADR-003, IF-ADR-006, IF-ADR-007, IF-ADR-014  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`  
Transaction cut: **IF-TXN-01 GameFlow Transition Failure Authority (implemented in package)**

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

**IF-TXN-01** makes Transition phase outcomes authoritative at the GameFlow transaction boundary:

```text
pre-commit Transition failure (Before not Completed)
  → do not start destination lifecycle
  → previous Route/Activity authority remains
  → typed FailedPreCommitTransition terminal
  → safe gate cleanup (no committed-target recovery)

committed-target reveal failure (After not Completed after destination commit)
  → keep committed destination authoritative
  → do not return Succeeded / Started
  → no blind rollback
  → apply committed-target reveal recovery protection
  → typed FailedCommittedTargetReveal terminal
  → distinct from FailedCommittedTargetNotReady (readiness)

CompletedWithWarnings remains accepted as TransitionResult.Completed.
Policy Skipped remains accepted as intentional phase completion.
Transition remains execution + typed result; GameFlow decides the transaction.
Route/Activity remain lifecycle authority; Loading remains presentation/progress.
```

## Current QA evidence

The current QA repository was cleaned and reorganized at the audited HEAD. Historical lifecycle smokes cannot be treated as current release evidence until the canonical suites are re-registered and executed.

## Current FIRSTGAME evidence

FIRSTGAME proves application boot, Route/Activity flow, Player participation, and additive content in real consumer scenes. Demo03 adds current consumer evidence for cross-scene Player provisioning UX.

## What remains

- Broaden compensation vocabulary beyond the IF-TXN-01 pre-commit vs committed-target reveal terminals (generic rollback/retry remain out of scope).
- Define and implement the Session-Persistent Logical Player source and its authoring/runtime contract.
- Rebuild full canonical QA coverage for two sessions, Route replacement during readiness waits, disposal, and required-binding failures.
- Publish a concise lifecycle diagram and diagnostic correlation guide for Session, Route, Activity, revision, and occurrence.

## Completion criteria

- No static/global runtime authority or silent fallback is introduced.
- Every transition terminal path produces typed, correlated evidence.
- Session-Persistent Player source has explicit lifetime, authoring, release, QA, and consumer proof.
- Canonical QA passes against the current package HEAD.

## Completion assessment

```text
Estimated completion: 90%
Normative status: Accepted
Package implementation: IF-TXN-01 transition failure authority implemented
QA evidence: package unit + diagnostics smoke for IF-TXN-01; full host re-proof still external
FIRSTGAME evidence: evaluated at e551643 (no deliberate broken transition surface left in product)
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
