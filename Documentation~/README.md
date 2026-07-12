# Immersive Framework Documentation

## Operational entry points

```text
Current/05-Execution-Status.md
  what is closed, active and next

Current/00-Current-State.md
  supported product/runtime state

Current/01-Roadmap.md
  canonical block and cut ordering

Current/02-Usage-Map.md
  designer-first product surfaces
```

## Camera status

Camera C9 is closed at the current single-output product level.

Canonical authoring:

```text
CameraRigRecipe
-> CameraRigComposer
-> Validate
-> Apply/Rebuild virtual Cinemachine rig
```

Canonical runtime:

```text
persistent UIGlobal output
-> CameraOutputSession
-> CameraOutputContext
-> Player / Activity / Route / Session requests
-> selected Cinemachine rig
```

Default precedence:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

Read:

- `Guides/Camera-Product-Usage.md`
- `Guides/Camera-Architecture-Flow.md`
- `Current/Camera-Delivery-Reconciliation.md`
- `ADRs/Product/ADR-PROD-0006-camera-requests-output-contexts.md`

## Active block

```text
G1 — Consumer Route Loop
```

G1 proves application flow between real Routes. It does not require the
framework to own gameplay objectives, interactions, combat, missions or win
conditions.
