# IF-ADR-002 — Product Authoring Model

Status: Accepted  
Last updated: 2026-08-06  
Implementation completion: **65%**  
Implementation classification: **Partially implemented across the product portfolio**  
Related decisions: IF-ADR-008, IF-ADR-010, IF-ADR-012, IF-ADR-015  
Audit baseline: package `9ed698e55b48077c54be5056c6951b7e52dac51b`, QA `0521d1f1804dff2806e06b1e095d47023a062b9e`, FIRSTGAME `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

> This is a consolidated audit revision. The normative architectural decision is
> preserved and the implementation assessment is explicitly separated from ADR
> acceptance status. Percentages are planning estimates, not automated release
> certification.

## Context

A technically correct collection of components, validators, and smokes is not sufficient as a product. Recurrent framework features need an authorable surface that lets a designer express intent, materialize technical contracts safely, understand validation, and inspect runtime evidence without assembling internal contracts manually.

## Decision

When appropriate, framework systems follow layered product architecture:

```text
Recipe / Profile / Template
  reusable intent
Composer / Authoring Component
  concrete scene or prefab composition
Technical materialization
  explicit components, adapters and bindings
Scoped Runtime Context / Session / Service
  runtime authority with explicit lifetime
Diagnostics
  validation, reports, logs, smokes and Advanced/Debug evidence
```

Designer-facing flows should prefer Create menu or wizard, reusable intent assets, clear Composer components, idempotent Apply/Rebuild, designer-first Inspectors, Advanced/Debug disclosure, embedded validation, and official samples/templates. Technical components remain inspectable and are not silently hidden.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.

## Current implementation coverage

The package already demonstrates the model in Persistent Application Content composition, Camera Rig authoring, Activity/Route assets, Player participation profiles, validation dashboards, and several product Inspector passes. Apply/Rebuild and managed materialization exist for selected systems, proving the direction is viable.

## Current QA evidence

Editor UX and authoring regressions exist in partial form, but the QA cleanup means product-flow coverage must be reindexed and re-executed. Current evidence is uneven by feature.

## Current FIRSTGAME evidence

FIRSTGAME demonstrates manual consumer assembly and exposes friction. Demo03 is actively testing Manager-Provisioned multiplayer controls and status presentation, revealing a missing official package surface described by IF-ADR-015.

## What remains

- Apply the full Recipe/Profile + Composer + Apply/Rebuild pattern to Manager-Provisioned Player.
- Create canonical product surfaces for Input Gate, Pause, Reset/Restart, Transition/Loading, Camera Overrides, and optional BGM where repetition justifies them.
- Provide guided creation, safe remediation, validation receipts, and Advanced/Debug sections consistently.
- Publish short usage docs and templates for each recurrent product flow.
- Define a product-surface maturity checklist and require it before a feature is described as complete.

## Completion criteria

- A new consumer can create and configure the feature without manually wiring internal contracts.
- Apply/Rebuild is idempotent, non-destructive, diagnostic, and preserves user-owned content.
- Normal Inspector shows intent; Advanced/Debug shows technical evidence.
- QA proves authoring and materialization; FIRSTGAME proves real-game usability.

## Completion assessment

```text
Estimated completion: 65%
Normative status: Accepted
Package implementation: evaluated at 9ed698e
QA evidence: evaluated at 0521d1f
FIRSTGAME evidence: evaluated at e551643
```

The percentage includes architecture/contract, runtime behavior, product authoring,
diagnostics/documentation, current QA evidence, and real-consumer evidence. A high
runtime percentage may still be reduced when the canonical QA harness or product
surface is incomplete.
