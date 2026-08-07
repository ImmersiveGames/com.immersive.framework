# IF-ADR-006 — Loading, Transition, Persistence and Diagnostics

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **88%**  
Implementation classification: **Core orchestration implemented; recovery and product gaps remain**  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-011, IF-ADR-015  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Loading and transition presentation must represent real operation state, preserve persistent application surfaces, coordinate cover/reveal and gates, expose terminal failures, and correlate diagnostics without becoming the authority for Route, Activity, or readiness.

## Decision

The framework owns persistent transition/loading surfaces and a typed orchestration path. Cover, technical loading, readiness waiting, reveal, failure, supersession, and cleanup are explicit phases. Presentation reports state; it does not calculate readiness or own destination authority. Logs distinguish operational summaries from debug/trace evidence.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

Persistent loading/transition surfaces, progress reporters, gates, typed results, diagnostics, and Activity readiness integration exist. Participant-aware progress reserves a final readiness range. The latest commit adds typed supersession for Route-authority replacement, avoiding misclassification of an intentional replacement as an ordinary failure/cancellation.

## Current QA evidence

Current QA needs a rebuilt canonical transition matrix after cleanup. Historical evidence remains useful context but is not current certification.

## Current FIRSTGAME evidence

FIRSTGAME demonstrates covered/visible loading paths and has exposed real readiness/Route replacement edge cases that drove the latest fix.

## What remains

- Prove surface-adapter failure, gate-release failure, cancellation, disposal, and committed-destination recovery.
- Define compensation after partial commit and restoration after reveal failure.
- Publish a dedicated Transition/Loading product template and policy guide.
- Add QA for Route replacement while waiting under WaitVisible and WaitCovered.
- Ensure all terminal results carry destination identity, operation sequence, revision/occurrence, loading diagnostics, and cleanup status.

## Completion criteria

- No terminal path leaves cover, gate, progress, or transition state leaked.
- Intentional supersession is distinguishable from failure.
- Loading reaches terminal completion only when the governing operation is terminal.
- QA and FIRSTGAME prove success, failure, cancellation, supersession, and recovery.

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
