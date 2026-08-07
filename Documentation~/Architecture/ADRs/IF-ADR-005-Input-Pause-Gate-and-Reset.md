# IF-ADR-005 — Input, Pause, Gate and Reset

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **76%**  
Implementation classification: **Integrated runtime exists; product extraction and negative coverage incomplete**  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-006, IF-ADR-010  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Input eligibility, pause, transition gates, object/group reset, and Activity restart intersect but are not the same authority. They require explicit ownership, typed handles, deterministic release, and failure evidence.

## Decision

Input admission is derived from valid Player/gameplay state. Pause has a scoped runtime and presentation binding. Gates use exact ownership handles and never rely on anonymous counters. Reset operates through registered subjects/participants with explicit scope and results. Activity Restart reconfigures the active Activity; it is not Session Player leave or Route replacement.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The runtime host composes pause, time scale, pause surfaces, combined gates, reset registry, reset subjects/participants, cycle reset, object/group reset, and Activity restart. These capabilities have been exercised in integrated flows.

## Current QA evidence

Some regression smokes were retained or moved, but the current QA baseline does not yet provide one canonical matrix for all gate, pause, reset, and restart terminal paths.

## Current FIRSTGAME evidence

FIRSTGAME integration proves parts of the runtime, but dedicated, teachable consumer demonstrations for M09–M13 remain incomplete.

## What remains

- Publish isolated product flows for Input Gate, Object Reset, Activity Restart, and Pause.
- Add negative QA for double acquire, invalid release, owner destruction, gate leakage, pause during transition, restart while paused, stale reset subjects, required/optional reset failure, and repeated restart.
- Create authoring surfaces that expose intent without hiding technical ownership.
- Provide runtime status and exact-handle evidence in Advanced/Debug.
- Clarify cleanup order during Activity exit, Route replacement, and Session disposal.

## Completion criteria

- Every acquired gate and registered reset object has an explicit owner and terminal cleanup.
- Pause and restart interactions are deterministic.
- Invalid operations fail explicitly with actionable diagnostics.
- Product flows are independently demonstrable in FIRSTGAME and covered by canonical QA.

## Completion assessment

```text
Estimated completion: 76%
Normative status: Accepted
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
