# IF-ADR-010 — Editor and Inspector Product Surface Authority

Status: Proposed  
Last updated: 2026-08-06  
Implementation completion: **70%**  
Implementation classification: **Broad foundation exists; decision not fully accepted or consistently applied**  
Related decisions: IF-ADR-002, IF-ADR-008, IF-ADR-012, IF-ADR-015  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

The Editor and Inspector are the primary product surface for Unity consumers. Technical correctness alone does not make a framework usable; creation, validation, remediation, runtime evidence, and safe materialization need consistent authority and vocabulary.

## Decision

Package Editor tooling owns canonical framework creation, configuration, validation, materialization, and diagnostic presentation. Normal Inspector prioritizes designer intent and actionable status. Advanced/Debug reveals technical components, identities, handles, revisions, occurrences, and receipts. Tools are idempotent, non-destructive, undo-aware, and do not execute gameplay accidentally.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

Shared editor utilities, validation dashboards, product status rows, creation actions, Route/Activity/Camera/Persistent Inspector passes, and diagnostic helpers exist. The package has enough foundation to prove the ADR direction.

## Current QA evidence

Editor UX regressions were moved/reorganized during the QA cleanup. A canonical product-surface suite is still needed.

## Current FIRSTGAME evidence

FIRSTGAME manual assembly continues to reveal unclear ownership, missing cross-scene commands, and gaps between technical components and user intent.

## What remains

- Accept the ADR after agreeing on mandatory product-surface standards and exceptions.
- Provide guided creation and safe remediation for missing required references.
- Standardize Apply/Rebuild receipts, deep links, runtime read-only status, and Advanced/Debug sections.
- Create reusable validation sessions that aggregate cross-asset issues without silent fixes.
- Add editor QA for Undo/Redo, prefab stage, multi-object editing, domain reload, asset duplication, and non-destructive rebuild.
- Align docs, samples, menus, and Inspector vocabulary.

## Completion criteria

- Every recurrent feature has a clear creation/configuration/debug path.
- Editor actions are idempotent, undo-safe, and non-destructive.
- Validation identifies the exact asset/object/field and offers only safe explicit remediation.
- Product QA and FIRSTGAME consumer tests confirm usability.

## Completion assessment

```text
Estimated completion: 70%
Normative status: Proposed
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
