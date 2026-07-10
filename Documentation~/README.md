# Immersive Framework Documentation

Canonical documentation for `com.immersive.framework`. The Current surface is intentionally small; detailed cuts belong in History, Product manifests or ADRs.

## Read in this order

| Order | Document | Purpose |
|---:|---|---|
| 1 | [Current State](Current/00-Current-State.md) | Supported state and known gaps. |
| 2 | [HTML Usage Guide](Guides/Usage/index.html) | Designer-facing setup. |
| 3 | [Usage Map](Current/02-Usage-Map.md) | Correct product surface by task. |
| 4 | [Roadmap](Current/01-Roadmap.md) | One active lane: P2. |
| 5 | [Player Control Authority Audit](Current/11-Player-Control-Authority-Audit.md) | Accepted P2 authority, lifetime and binding decisions. |
| 6 | [Camera Product Usage](Guides/Camera-Product-Usage.md) | CameraComposer and lifecycle-output boundary. |
| 7 | [Consumer Project Roles](Current/03-Consumer-Project-Roles.md) | Package, QA and FIRSTGAME ownership. |
| 8 | [Player Passive Foundation](Current/04-Player-Passive-Binding-Foundation.md) | Valid contracts/diagnostics below PlayerComposer. |
| 9 | [History Index](History/000-INDEX.md) | Superseded cuts and consolidation. |
| 10 | [ADR Index](ADRs/ADR-INDEX.md) | Decision archive. |

## Current Player model

```text
PlayerRecipe (optional) -> PlayerComposer -> Validate -> Apply/Rebuild
```

PlayerSlot, ActorId, PlayerEntry, PlayerView and PlayerControl remain technical contracts/evidence. Input activation, movement, spawning and a complete PlayerControl runtime are not automatic.

## Current Camera model

```text
PlayerComposer -> CameraComposer -> Validate -> Apply/Rebuild
               -> Unity Camera + Cinemachine Camera + Brain
```

Route/Activity bindings consume explicit Cinemachine outputs and apply on enter. Automatic release/restoration on exit is pending. There is no camera director, `Camera.main` fallback, name-based runtime authority or global camera manager.

Stable guide path: `Documentation~/Guides/Usage/index.html`.
