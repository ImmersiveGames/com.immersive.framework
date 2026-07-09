# ADR-PROD-0001 — Product Surface Model

Status: Accepted  
Date: 2026-07-09

## Context

Immersive Framework 1.0 already has strong technical contracts, validators, diagnostics, and runtime pieces in several domains.

The transition problem is product usability. Too many features are still created by manually adding technical components, filling internal IDs/references, and running validators or smokes to prove that the wiring works.

That is not enough for a framework intended to build real games.

## Decision

Every recurring framework feature must declare its product surface.

When applicable, a feature should be shaped through these layers:

```text
Recipe / Profile / Template
  Reusable intent.

Composer / Authoring
  Concrete scene, prefab, or asset-level configuration.

Apply / Rebuild
  Idempotent operation that materializes required technical state.

Technical Materialization
  Contracts, adapters, bindings, generated objects, or _Framework/_Bindings containers.

Runtime Context / Session / Service
  Scoped runtime authority when real Play Mode behavior requires ownership.

Diagnostics
  Validators, smokes, logs, reports, and Advanced/Debug evidence.

Samples / Templates
  Minimal official examples for common usage.
```

A feature may omit a layer only by explicit decision.

## Product rule

Do not treat the following as a complete product experience:

```text
technical components + validator + smoke
```

The preferred model is:

```text
authorable product + technical contracts + real runtime + diagnostics
```

## Consequences

- Future cuts must state the product surface affected by the change.
- Validators and smokes remain important, but they do not replace authoring.
- Technical contracts remain explicit, but they should not be the default designer-facing surface when a Composer or Recipe is needed.
- Containers such as `_Framework` or `_Bindings` are materialization/organization, not product authority.
- FIRSTGAME can reveal product friction, but official reusable solutions belong in `com.immersive.framework`.

## Non-goals

- This ADR does not implement any Composer, Recipe, Runtime Context, sample, or wizard.
- This ADR does not require every low-level contract to become a product surface.
- This ADR does not remove existing validators or smokes.

## Affected systems

All recurring framework systems, including Player, Route, Activity, Camera, Global UI, Reset, Input, Content, Save, Diagnostics, and Samples.
