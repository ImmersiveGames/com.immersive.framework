# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: Proposed  
Last updated: 2026-08-06  
Implementation completion: **30%**  
Implementation classification: **ADR and consumer prototype exist; official package surface not shipped**  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Manager-Provisioned Player flows repeatedly need commands such as opening/closing joining, changing dynamic capacity, requesting join, and requesting Actor selection. Route/Activity-owned UI also needs immutable evidence from Session-scoped Player authorities. Without an official surface, consumers invent event channels, bridges, lookups, and snapshot projections.

## Decision

The package owns canonical typed Player provisioning commands and immutable consumer observation contracts. The surface is a scoped request/observation boundary, not another runtime authority. It must support persistent Session authorities with Route/Activity UI without serialized cross-scene object references, service locators, reflection, scene searches, global event buses, or consumer access to internal preparation/reconcile modules.

Expected product direction:

```text
Manager-Provisioned Player Recipe/Profile
Manager-Provisioned Player Composer
Player Provisioning Command Trigger
Player Provisioning Status Presenter/Binding
Advanced/Debug correlated Slot, revision, Activity and occurrence evidence
```

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The package already has Session Player authority, provisioning operations in partial form, join/admission, Actor selection/preparation, Activity readiness contribution, and runtime snapshots in partial form. The new ADR was added at the audited HEAD. FIRSTGAME Demo03 is actively building the temporary local command/status prototype and therefore supplies current UX evidence, but the official package product surface is explicitly not shipped.

## Current QA evidence

No canonical public-only command/observation suite exists yet in the cleaned QA harness.

## Current FIRSTGAME evidence

Demo03 is the intended temporary prototype. It may prove repeated commands, cross-scene integration, presentation needs, and snapshot correlation, but it must remain consumer-specific until the package surface is implemented.

## What remains

- Finalize command vocabulary, scope, lifetime, and typed operation result contracts.
- Define immutable aggregate provisioning and Activity Player readiness snapshots with revision/occurrence correlation.
- Select the explicit cross-scene transport mechanism without creating a generic global event bus.
- Implement command trigger, status presenter/binding, validation, and Manager-Provisioned Composer integration.
- Prove positive and negative commands through public APIs in QAFramework.
- Migrate Demo03 to the package surface and remove the temporary local bridge.
- Document which presentation concerns remain consumer-owned.

## Completion criteria

- Consumer UI can issue supported commands and observe immutable state using public package surfaces only.
- No global lookup, reflection, hierarchy search, or direct internal module access is required.
- Revision and Activity occurrence correlation are preserved.
- Demo03 no longer needs its compatibility bridge and canonical QA passes.

## Completion assessment

```text
Estimated completion: 30%
Normative status: Proposed
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
