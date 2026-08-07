# IF-ADR-011 — Participant-Aware Activity Readiness Loading Progress

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **92%**  
Implementation classification: **Runtime complete; current QA and product presentation recertification remain**  
Related decisions: IF-ADR-006, IF-ADR-007, IF-ADR-012  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Technical scene/content loading may finish before required Activity participants are ready. Loading progress must reserve space for participant-aware readiness without inventing progress, regressing, reaching 100% early, or accepting stale occurrence updates.

## Decision

Activity entry uses a monotonic progress envelope with a technical range and an optional reserved readiness range. Readiness progress is derived from occurrence-scoped aggregate evidence. Terminal 100% is issued only for Ready. Terminal failure stops completion. Stale occurrence snapshots are rejected.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The package implements required/optional counts including completed and released evidence, readiness snapshots, range mapping, queued reporting, monotonic acceptance, stale occurrence rejection, terminal failure, and completion only when Ready. Integration exists for Activity entry and startup paths.

## Current QA evidence

Historical implementation cuts and QA evidence exist, but the current cleaned QA project must re-register and run the canonical progress suite.

## Current FIRSTGAME evidence

FIRSTGAME loading demonstrations provide practical evidence, including Player readiness as the final loading phase.

## What remains

- Rebuild current QA for monotonicity, duplicate reports, stale occurrence, failure, release, zero participants, optional-only participants, and supersession.
- Validate all loading policies and startup paths use consistent phase/message semantics.
- Publish presentation guidance for determinate versus indeterminate technical phases.
- Expose readiness ratio inputs and rejection counts in Advanced/Debug diagnostics.

## Completion criteria

- Progress never decreases and never reaches 100% before Ready.
- Stale or foreign occurrences cannot update the active operation.
- Failure and supersession terminate correctly without false completion.
- Current QA and FIRSTGAME pass the same scenarios.

## Completion assessment

```text
Estimated completion: 92%
Normative status: Accepted
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
