# IF-ADR-008 — Persistent Application Content Composition

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **90%**  
Implementation classification: **Product model implemented; portfolio expansion and QA remain**  
Related decisions: IF-ADR-002, IF-ADR-006, IF-ADR-010, IF-ADR-015  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Persistent application content hosts cross-Route/session presentation and integration components. Manual assembly of internal bindings is error-prone, but opaque magic generation would make ownership and diagnostics difficult.

## Decision

Persistent content uses an explicit Recipe/Composer workflow with managed technical slots, idempotent Apply/Rebuild, preservation of user-owned content, validation, receipts, and Advanced/Debug visibility. The Composer is an authoring authority, not gameplay runtime authority.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The package contains persistent content recipes/composers, materialization, managed ownership, non-destructive rebuild behavior, validation, and product-oriented Inspector surfaces. This is one of the clearest completed examples of IF-ADR-002.

## Current QA evidence

Authoring QA existed but must be reindexed against the cleaned harness. Idempotency and preservation need current executable proof.

## Current FIRSTGAME evidence

FIRSTGAME uses persistent content for loading/UI/camera/player integration and continues to expose which bindings should be productized.

## What remains

- Revalidate Apply/Rebuild idempotency, user-owned preservation, missing-slot remediation, and destructive-change diagnostics.
- Add official templates for common persistent configurations.
- Integrate the future Player provisioning command/observation bindings without turning the container into a global authority.
- Publish clear ownership receipts and migration notes for recipe changes.
- Ensure every managed technical component remains visible in Advanced/Debug.

## Completion criteria

- Rebuild produces the same technical composition when intent is unchanged.
- User-owned objects and fields are preserved unless an explicit destructive action is confirmed.
- Missing required persistent bindings fail before Play Mode where possible.
- QA and FIRSTGAME prove the canonical workflow.

## Completion assessment

```text
Estimated completion: 90%
Normative status: Accepted
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
