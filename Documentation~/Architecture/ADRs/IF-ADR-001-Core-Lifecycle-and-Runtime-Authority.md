# IF-ADR-001 — Core Lifecycle and Runtime Authority

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **88%**  
Implementation classification: **Substantially implemented; architectural residuals remain**  
Related decisions: IF-ADR-003, IF-ADR-006, IF-ADR-007, IF-ADR-014  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

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

The package contains the scoped runtime host, bootstrap composition, Route/Activity runtimes, scene lifecycle, runtime-content ownership, explicit ports, typed results, and Session-scoped Player participation authority. Manager-Provisioned and Scene-Provided Player sources exist. The latest package change also introduces typed supersession/interruption behavior when Route authority replaces an in-flight Activity readiness operation, strengthening lifecycle unwind semantics.

## Current QA evidence

The current QA repository was cleaned and reorganized at the audited HEAD. Historical lifecycle smokes cannot be treated as current release evidence until the canonical suites are re-registered and executed.

## Current FIRSTGAME evidence

FIRSTGAME proves application boot, Route/Activity flow, Player participation, and additive content in real consumer scenes. Demo03 adds current consumer evidence for cross-scene Player provisioning UX.

## What remains

- Finalize the Activity transition transaction vocabulary: authority commit, phase, readiness, previous-Activity finalization, supersession, and unwind evidence.
- Define cancellation and compensation rules before Activity authority commit.
- Define and implement the Session-Persistent Logical Player source and its authoring/runtime contract.
- Rebuild canonical QA coverage for two sessions, Route replacement during readiness waits, disposal, and required-binding failures.
- Publish a concise lifecycle diagram and diagnostic correlation guide for Session, Route, Activity, revision, and occurrence.

## Completion criteria

- No static/global runtime authority or silent fallback is introduced.
- Every transition terminal path produces typed, correlated evidence.
- Session-Persistent Player source has explicit lifetime, authoring, release, QA, and consumer proof.
- Canonical QA passes against the current package HEAD.

## Completion assessment

```text
Estimated completion: 88%
Normative status: Accepted
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
