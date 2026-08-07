# IF-ADR-003 — Player Participation and Actor Lifecycle

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **84%**  
Implementation classification: **Runtime substantially implemented; product and hardening gaps remain**  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-012, IF-ADR-015  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

A Logical Player is a Session participant, while an Actor is contextual gameplay content. Joining, Actor selection, logical preparation, physical materialization, input/camera admission, readiness contribution, Activity exit, and reentry must remain distinct and diagnosable operations.

## Decision

Player participation is Session-scoped and keyed by typed Slot identity. Route/Activity may project eligible Players and own contextual Actor materialization, but they do not own Session participant identity. The lifecycle separates:

```text
Slot configuration
joining/admission
Logical Player participation
Actor selection
Logical Actor preparation
physical Actor materialization
input/camera/gameplay admission
Activity readiness contribution
contextual release/reconcile
```

Scene-Provided and Manager-Provisioned sources are supported. Reconciliation must be idempotent, occurrence-aware, revision-correlated, and must not use silent fallback or internal consumer shortcuts.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The package now includes Manager-Provisioned join flow, Slot reservation/admission, Actor selection/preparation, physical Actor materialization, gameplay admission, exact readiness contribution evidence, cold-start support, and host-owned reconciliation when Session revision or Activity occurrence changes. The previous late-join/active-Activity reconciliation gap is no longer open. FIRSTGAME Demo03 is adding a real local-multiplayer consumer flow.

## Current QA evidence

The prior QA harness contained many Player smokes, but the current cleanup removed or reorganized several tests. Runtime implementation is stronger than current canonical QA evidence.

## Current FIRSTGAME evidence

FIRSTGAME M07 and the evolving Demo03 prove real integration. Demo03 also demonstrates that recurring Player commands and status observation require a package-owned product surface rather than permanent consumer bridges.

## What remains

- Implement the Manager-Provisioned Player Recipe/Profile and Composer with idempotent Apply/Rebuild.
- Harden provisioning: duplicate/in-flight commands, callback expiry, late/divergent callbacks, capacity changes, deterministic cleanup, and failure receipts.
- Define Session Player Leave and device disconnect/reconnect separately from Activity contextual exit.
- Define the Session-Persistent Player source.
- Complete IF-ADR-015 so consumer UI uses canonical typed commands and immutable snapshots.
- Rebuild public-only positive and negative QA suites against current APIs.

## Completion criteria

- Join, selection, preparation, materialization, admission, release, and reconcile have explicit typed results.
- Late join and Activity reentry are deterministic and occurrence-safe.
- Consumer UI never invokes internal preparation/reconcile modules.
- Authoring, QA, and FIRSTGAME all use the same public product surface.

## Completion assessment

```text
Estimated completion: 84%
Normative status: Accepted
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
