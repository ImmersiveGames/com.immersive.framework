# IF-ADR-014 — Authored Definition and Stable Identity Authority

Status: Proposed  
Last updated: 2026-08-06  
Implementation completion: **75%**  
Implementation classification: **IF-ID-02..06 landed (vocabulary, reference authority, ownership tokens, validation scopes, regenerate UX); resolver + FIRSTGAME remain**  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-015  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`  
Execution plan: [IF-ID-IDENTITY-AUTHORITY-EXECUTION-PLAN-2026-08-06](../Plans/IF-ID-IDENTITY-AUTHORITY-EXECUTION-PLAN-2026-08-06.md)

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

### Landed (IF-ID-02 .. IF-ID-06)

- `RouteAsset.HasSameStableId` / `ActivityAsset.HasSameStableId` make stable-boundary equality explicit.
- `HasSameIdentity` is obsolete and redirects to stable-ID comparison only.
- Route and Activity lifecycle target equality, enter/exit publication, host reconciliation, readiness ownership, content matching, and related request equality use exact asset `ReferenceEquals`.
- `RuntimeContentOwner` operational equality includes `DefinitionToken` (`EntityId` via `GetEntityId()`); stable ID remains boundary evidence via `HasSameStableDefinition`.
- Definition-local, Game Application graph, and project identity audit scopes are separated.
- Route/Activity Inspectors expose `Regenerate Stable ID...` with confirmation and Undo.
- Package tests cover reference vs stable-ID vs owner-token collision cases.

### Still open

- Application-scoped stable-ID resolver not required until save/external boundary needs it (IF-ID-07).
- FIRSTGAME product duplication/remediation workflow proof (IF-ID-08).
- Broader QAFramework matrix and deeper application graph identity walk.
- Optional future occurrence-sequence minting if concurrent same-definition owners are required.

## Current QA evidence

Package identity baseline tests prove reference vs stable-ID distinction and readiness ownership. Full lifecycle collision matrix and ownership release matrix remain for QA/FIRSTGAME.

## Current FIRSTGAME evidence

FIRSTGAME provides real assets and transitions that can expose collisions and rename/move behavior, but it is not yet an identity migration test suite.

## What remains

- IF-ID-07: application-scoped resolver when a real boundary requires it.
- IF-ID-08: FIRSTGAME create/duplicate/diagnose/regenerate/run/rename/move proof.
- Expand QA collision matrix for Route/Activity transitions, supersession, and ownership release.
- Optional deeper Game Application graph identity walk beyond Startup Route chain.

## Completion criteria

- Distinct assets never compare equal solely because their stable IDs collide. **(done for definition + owner paths)**
- Persistence/external boundaries resolve IDs through explicit typed results. **(open — IF-ID-07)**
- Collisions are blocking, visible, and repairable only through explicit action. **(package UX done; FIRSTGAME proof open)**
- Ownership release never confuses two definitions that share a stable ID. **(done via definition tokens)**

## Completion assessment

```text
Estimated completion: 75%
Normative status: Proposed
Package implementation: IF-ID-02..06 landed
QA evidence: package identity baseline tests; QAFramework matrix still open
FIRSTGAME evidence: evaluated at e551643 (not identity-workflow proof)
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
