# IF-ADR-009 — Activity Local Visibility Rules

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **88%**  
Implementation classification: **Runtime integrated; authoring and regression polish remain**  
Related decisions: IF-ADR-006, IF-ADR-007, IF-ADR-010  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Activity-owned content may need to remain hidden, disabled, or presentation-gated until lifecycle and readiness conditions are satisfied. Visibility must be explicit and scoped rather than inferred from scene load or object hierarchy.

## Decision

Activity local visibility is expressed through explicit authoring/adapters bound to Activity lifecycle and readiness. Visibility authority is contextual and occurrence-aware. Required visibility failures are blocking and diagnostic; optional presentation behavior does not silently weaken required readiness.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

Local visibility adapters and lifecycle integration exist. FIRSTGAME readiness/loading demonstrations exercise hidden/covered content and reveal ordering.

## Current QA evidence

Current canonical negative coverage for stale occurrences, missing targets, owner destruction, and repeated enter/exit must be re-established.

## Current FIRSTGAME evidence

FIRSTGAME provides real evidence that local visibility and loading cover must remain separate but coordinated product concerns.

## What remains

- Complete designer-first authoring for common visibility intents.
- Add QA for missing target, stale occurrence, repeated apply/release, owner destruction, and Route replacement.
- Expose resolved visibility rule, owner, occurrence, and last application result in Advanced/Debug.
- Publish examples for scene objects, additive content, and Actor-related visibility.

## Completion criteria

- Visibility never becomes implicit scene-load authority.
- Required failures block with explicit diagnostics.
- Release restores or disposes only context-owned state.
- QA and FIRSTGAME prove enter, reentry, exit, replacement, and failure.

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
