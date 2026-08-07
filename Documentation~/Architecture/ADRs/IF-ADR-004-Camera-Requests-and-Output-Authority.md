# IF-ADR-004 — Camera Requests and Output Authority

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **78%**  
Implementation classification: **Core runtime implemented; isolated product proof incomplete**  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-005, IF-ADR-010  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Camera presentation needs one physical output authority while allowing Session, Route, Activity, Player, pause, and transition contexts to request presentation without directly mutating shared output or relying on hierarchy discovery.

## Decision

The framework owns a typed Camera request/release model and a single output authority. Request priority, ownership, replacement, release, restoration, and diagnostics are explicit. Player Camera admission is contextual to gameplay readiness. Consumers author intent through product components rather than locating or mutating the physical output directly.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The package contains Camera output/session injection, request authority, Player gameplay camera integration, Camera Rig authoring, and priority-based presentation infrastructure. Integrated Player flows demonstrate real runtime behavior.

## Current QA evidence

Camera folders and regressions remain in the reorganized QA project, but complete current negative evidence was not established in this audit.

## Current FIRSTGAME evidence

FIRSTGAME has integrated Camera behavior through Player flows. Dedicated Player Camera and override demonstrations remain incomplete.

## What remains

- Create an isolated Player Camera product demonstration and manual setup guide.
- Prove Activity, Route, and Session override priority and restoration.
- Add QA for equal priority, stale handles, release out of order, output absence, owner destruction, and replacement during transition.
- Complete designer-first override authoring and Advanced/Debug evidence.
- Document which scope owns each request and how previous presentation is restored.

## Completion criteria

- Exactly one physical output authority exists per Session.
- Requests and releases are typed, scoped, deterministic, and diagnostic.
- No consumer searches the hierarchy for a Camera authority.
- QA and FIRSTGAME prove replacement and restoration across scopes.

## Completion assessment

```text
Estimated completion: 78%
Normative status: Accepted
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
