# IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **90%**  
Implementation classification: **Contract and runtime implemented; product/QA consolidation remains**  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-010, IF-ADR-015  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

Activities need reusable Player participation intent that can express projected Slots, controller modes, caps, Actor mapping, readiness requirements, and compatibility without duplicating runtime rules in each scene.

## Decision

Activity Player participation is authored through inline configuration or a reusable Profile resolved into one normalized effective policy with provenance. Runtime uses explicit Slot/Player/Actor evidence and publishes requested versus effective state and diagnostic reasons. Invalid or incompatible states fail explicitly.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

Profiles, normalized resolution, controller modes, Slot caps, explicit Actor maps, requested/effective state, compatibility diagnostics, exact readiness evidence, and FIRSTGAME M07 integration exist. Manager-Provisioned lifecycle reconciliation now respects active Activity occurrences.

## Current QA evidence

Public-authoring QA existed historically, but the cleaned harness requires current canonical revalidation.

## Current FIRSTGAME evidence

FIRSTGAME M07 and Demo03 provide direct consumer proof and expose the need for canonical commands/status presentation under IF-ADR-015.

## What remains

- Complete Create menu/template and designer-first Profile/Activity Inspector flow.
- Expose provenance, effective policy, projected Slots, and readiness contribution in Advanced/Debug.
- Rebuild QA for inline/profile precedence, invalid maps, caps, unsupported modes, zero Slots, and runtime changes.
- Clarify which policy changes may apply to an active Activity and which require reentry/restart.
- Integrate consumer observation through IF-ADR-015 without duplicating readiness calculation.

## Completion criteria

- One normalized effective policy is the sole runtime input.
- Provenance and requested/effective differences are visible.
- Invalid compatibility never falls back silently.
- Current QA and FIRSTGAME prove profile reuse and runtime outcomes.

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
