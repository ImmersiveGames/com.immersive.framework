# IF-ADR-007 — Activity Entry Readiness and Reveal Gating

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **96%**  
Implementation classification: **Runtime contract complete; current QA recertification remains**  
Related decisions: IF-ADR-003, IF-ADR-006, IF-ADR-009, IF-ADR-011, IF-ADR-012  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

An Activity may have technically loaded content while required participants, Actors, adapters, or local visibility are still preparing. Reveal policy must distinguish observing readiness, waiting while visible, and waiting while covered without deadlocking or treating expected authority replacement as failure.

## Decision

Activity entry uses explicit policies:

```text
ObserveOnly
WaitVisible
WaitCovered
```

Readiness is occurrence-scoped and aggregates required/optional contribution evidence. Preparing, Ready, terminal failure, invalidation, cancellation, and supersession are distinct. Loading/transition presentation may wait on readiness but does not own it. A newer Route or Activity authority may supersede an in-flight wait through a typed interruption cause.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The package implements readiness policies, occurrence correlation, required/optional pending/completed/failed/released counts, progressive state, reveal gating, failure diagnostics, loading progress integration, and the new `Superseded` wait/execution path for authority replacement. This directly addresses the recent Route replacement/readiness issue.

## Current QA evidence

The runtime contract is complete, but the reorganized QA harness must re-establish the canonical policy/replacement matrix at the current HEAD.

## Current FIRSTGAME evidence

FIRSTGAME has exercised real WaitCovered/Player-readiness interactions and Route replacement, providing high-value consumer evidence for the fix.

## What remains

- Rebuild QA for all three policies across Ready, Preparing, Failed, Released, Invalidated, Cancelled, and Superseded outcomes.
- Add explicit tests for required Player contribution when joining is closed, capacity changes, no Player exists, and Route replacement occurs.
- Publish a concise policy selection guide explaining when loading should remain covered.
- Expose occurrence/revision and pending contribution details consistently in Advanced/Debug presentation.

## Completion criteria

- WaitCovered never reveals before Ready and never deadlocks after typed supersession.
- WaitVisible permits visible preparation without losing terminal diagnostics.
- ObserveOnly never becomes an accidental blocking wait.
- Current QA passes the full policy and authority-replacement matrix.

## Completion assessment

```text
Estimated completion: 96%
Normative status: Accepted
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
