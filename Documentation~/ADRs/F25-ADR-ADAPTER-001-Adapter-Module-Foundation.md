# F25-ADR-ADAPTER-001 — Adapter Module Foundation

Status: Deferred / superseded by F28 planning spine  
Original Phase: F25 — Adapter Module Foundation  
Current Placement: F28/F29 candidate track  
Type: Adapter Module / Gameplay & Subsystem Boundary  
Last updated: 2026-06-28

## Context

This ADR was written when F25 was expected to become the Adapter Module Foundation phase.

The project later used F25 for Activity Content Scene Composition and Activity Operation architecture reset. That history is now canonical: F25 is Activity scene operation history, not the adapter module implementation phase.

Adapter module work remains valid, but its planning moved forward to F28.

## Current Decision

Do not execute this ADR as F25 runtime work.

The adapter module foundation is now a future track selected by F28 closeout. F28 first produces:

```text
completion dependency map
adapter module taxonomy
module placement rules
Player / Actor / Input ownership plan
InputMode and Pause integration plan
next implementation entry criteria
```

A future implementation phase, likely F29 if selected by F28F, may then create minimal adapter module contracts.

## Adapter Module Direction

Adapter modules consume existing framework contracts instead of redefining them:

- Route
- Activity
- Gate
- Transition
- Loading
- Pause
- Snapshot
- Preferences
- Progression Save
- Runtime Content
- Content Anchor
- Object Entry
- Object Reset
- Pooling boundary
- Diagnostics

## Future Adapter Families

Future planning may cover:

- Player / Actor
- Unity Input
- InputMode
- Pause Integration
- Camera
- Audio
- Save / Progression
- Runtime Spawned / Pooling
- Gameplay capabilities
- Inventory
- Combat
- Projectile
- Damage

## Guardrails

- Adapter modules must not alter Route/Activity ownership.
- Adapter modules must not create a parallel lifecycle pipeline.
- Adapter modules must not bypass Gate when flow admission is required.
- Adapter modules must not redefine Transition, Loading, Pause, Save, Snapshot, Preferences or Progression Save contracts.
- Adapter modules must not use Unity object names or paths as framework identity.
- Adapter modules must not move optional subsystem behavior into framework core.
- Adapter modules must not use service locator behavior to avoid owner placement.

## Historical Note

This ADR remains in the ADR set as historical intent and guardrail material. The active planning source is now:

```text
ADRs/F28-ADR-INPUT-001-InputMode-Adapter-Boundary.md
Assets/_Documentation/Plans/F28-PLAN-InputMode-And-Adapter-Boundary.md
```
