# Immersive Framework Documentation

Canonical documentation for `com.immersive.framework`.

## Read in this order

| Order | Document | Purpose |
|---:|---|---|
| 1 | [Execution Status](Current/05-Execution-Status.md) | Single operational answer for current block, last accepted evidence and next cut. |
| 2 | [Current State](Current/00-Current-State.md) | Supported product/runtime state and known gaps. |
| 3 | [HTML Usage Guide](Guides/Usage/index.html) | Designer-facing setup. |
| 4 | [Usage Map](Current/02-Usage-Map.md) | Correct product surface by task. |
| 5 | [Roadmap](Current/01-Roadmap.md) | Closed blocks, active block and ordered future blocks. |
| 6 | [Consolidated Development Plan](Planning/Plano%20de%20Firstgame.md) | Detailed intent, scope and acceptance criteria for R0, P2, G1, P3, C9 and S1. |
| 7 | [Planning Index](Planning/README.md) | Planning authority and rules for using detailed plans. |
| 8 | [Player Control Authority Audit](Current/11-Player-Control-Authority-Audit.md) | P2 architectural decisions and historical proposal context. |
| 9 | [Camera Product Usage](Guides/Camera-Product-Usage.md) | CameraComposer and lifecycle-output boundary. |
| 10 | [Consumer Project Roles](Current/03-Consumer-Project-Roles.md) | Package, QA and FIRSTGAME ownership. |
| 11 | [Player Passive Foundation](Current/04-Player-Passive-Binding-Foundation.md) | Contracts and diagnostics below PlayerComposer. |
| 12 | [History Index](History/000-INDEX.md) | Superseded cuts and consolidation. |
| 13 | [ADR Index](ADRs/ADR-INDEX.md) | Accepted decision archive. |

## Documentation authority

Use documents by this priority:

```text
1. Current/05-Execution-Status.md
   Current operational truth and next cut.

2. Current/00-Current-State.md
   Supported product/runtime state.

3. Current/01-Roadmap.md
   Ordered block status.

4. Planning/Plano de Firstgame.md
   Detailed accepted sequence and per-block intent.

5. ADRs
   Architectural constraints and accepted decisions.

6. History
   Traceability only; never use as current direction.
```

When these documents disagree, update the higher-priority current document and reconcile the lower-priority one in the same documentation cut.

## Current product model

```text
PlayerRecipe (optional)
  -> PlayerComposer
  -> Validate
  -> Apply/Rebuild

PlayerComposer
  -> CameraComposer
  -> Validate
  -> Apply/Rebuild
  -> Unity Camera + Cinemachine Camera + Brain
```

Player movement remains consumer-owned. Gate/Pause/Transition control gameplay-input availability through the canonical Unity PlayerInput adapter.

Stable designer guide: `Documentation~/Guides/Usage/index.html`.
