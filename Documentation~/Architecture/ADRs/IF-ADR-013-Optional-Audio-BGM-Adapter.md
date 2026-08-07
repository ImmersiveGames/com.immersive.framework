# IF-ADR-013 — Optional Audio BGM Adapter

Status: Accepted / Experimental  
Last updated: 2026-08-06  
Implementation completion: **65%**  
Implementation classification: **Technical adapter exists; product promotion incomplete**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-006, IF-ADR-010  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

BGM integration is optional and may depend on an external audio director. The framework should define a narrow adapter boundary without making a specific audio package part of Route/Activity identity or core lifecycle authority.

## Decision

The framework exposes a narrow optional BGM port and Route/Activity bindings for explicit policies such as own BGM, use Route BGM, retain previous, or silence. Requests return typed ownership evidence and release deterministically. Absence of an optional integration is explicit and must not corrupt core lifecycle.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The port, bindings, policy vocabulary, typed handles/results, and technical QA assets exist in partial/current form. The QA repository still contains Audio structure after cleanup, but this audit did not establish a complete current promotion suite.

## Current QA evidence

Audio QA content exists, but release-grade positive/negative evidence must be consolidated after the harness reorganization.

## Current FIRSTGAME evidence

A dedicated FIRSTGAME BGM demonstration is not yet complete. The adapter should remain Experimental until real consumer integration and restoration behavior are proven.

## What remains

- Build FIRSTGAME M16 or equivalent real consumer demonstration.
- Prove Route fallback, Activity override, retain previous, silence, reentry, replacement, and restoration.
- Create a designer-first binding/Inspector workflow and short usage guide.
- Add QA for missing optional director, rejected request, stale handle, release failure, and rapid Route/Activity changes.
- Decide promotion criteria from Experimental to supported product surface.

## Completion criteria

- Core lifecycle works when the optional adapter is absent.
- Every accepted request has deterministic release/restoration.
- No audio-specific identity leaks into Route/Activity identity.
- QA and FIRSTGAME prove the full policy matrix before promotion.

## Completion assessment

```text
Estimated completion: 65%
Normative status: Accepted / Experimental
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
