# IF-ADR-014 — Authored Definition and Stable Identity Authority

Status: Proposed  
Last updated: 2026-08-06  
Implementation completion: **25%**  
Implementation classification: **Decision documented; systemic migration not implemented**  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-015  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Unity asset references and stable external identifiers solve different problems. Treating equal stable IDs as authored-definition equality can merge distinct assets, while relying only on references breaks persistence and external boundaries.

## Decision

Authored/runtime definition equality uses the `RouteAsset` or `ActivityAsset` reference. `RouteId` and `ActivityId` are stable projections for persistence, serialization, ownership boundaries, diagnostics, and external references. Two distinct assets with the same stable ID are a collision and must not silently become the same authored definition.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

Typed IDs and stable identity fields exist in parts of the package, but equality, dictionaries, ownership keys, request idempotency, collision validation, duplication behavior, and migration are not yet systemically aligned. This remains the largest cross-cutting architectural gap.

## Current QA evidence

No current canonical QA matrix proves reference equality versus stable-ID boundary semantics across lifecycle and ownership.

## Current FIRSTGAME evidence

FIRSTGAME provides real assets and transitions that can expose collisions and rename/move behavior, but it is not yet an identity migration test suite.

## What remains

- Complete an authority audit of every Route/Activity comparison, dictionary, ownership key, cache, request, and persistence boundary.
- Define stable-ID generation, duplication, regeneration, rename/move preservation, and migration rules.
- Implement blocking collision validation with deep links to all conflicting assets.
- Migrate runtime equality and idempotency to asset-reference semantics where authored definition is intended.
- Preserve stable IDs at persistence/external boundaries and add explicit resolution results.
- Create QA for same reference, different reference/same ID, rename/move, duplicate, regenerate, stale handles, and ownership release.
- Create an Advanced/Debug identity Inspector and migration documentation.

## Completion criteria

- Distinct assets never compare equal solely because their stable IDs collide.
- Persistence/external boundaries resolve IDs through explicit typed results.
- Collisions are blocking, visible, and repairable only through explicit action.
- All affected runtime modules and QA suites migrate in one coordinated cut.

## Completion assessment

```text
Estimated completion: 25%
Normative status: Proposed
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
