# 00 — Current State

Status: **canonical after P2 Player Control evidence reconciliation**

For the active block and exact next cut, read [Execution Status](05-Execution-Status.md).

## Closed product baseline

| Area | Current state |
|---|---|
| Player product | `PlayerRecipe` and `PlayerComposer` are the designer-first Player surface. Validate and Apply/Rebuild materialize the canonical technical topology. |
| Player control | Explicit `PlayerInput`, validated gameplay action map and `UnityPlayerInputGateAdapter` are proven in QA and FIRSTGAME. Movement remains game-owned. |
| Player technical QA | P2D runtime baseline passed 13/13. P2E Gate/Pause/Transition block and restoration passed 14/14. |
| FIRSTGAME Player | P2G proved real Move input and real displacement through `FirstGamePlayerMover`, 11/11. |
| Camera product | `CameraRecipe` and `CameraComposer` single-player MVPs are closed. Apply/Rebuild materializes Unity Camera, Cinemachine Camera and Brain idempotently. |
| FIRSTGAME Camera | The real consumer proves `PlayerComposer -> CameraComposer` for the main gameplay camera. |
| Passive player foundation | `PlayerSlot`, `ActorId`, `PlayerEntry`, `PlayerView` and `PlayerControl` remain contracts and diagnostics. |
| Route/Activity camera | Explicit Cinemachine output is applied on enter. Activity supports `UseOwn` and `UseRoute`. |
| Legacy camera | No functional `FrameworkCameraDirector`, `Camera.main` fallback, name-based runtime authority or global camera manager exists. |

## Current authoring model

```text
Player:
PlayerRecipe (optional)
  -> PlayerComposer
  -> Validate
  -> Apply/Rebuild

Camera:
CameraRecipe (optional)
  -> CameraComposer
  -> Validate
  -> Apply/Rebuild
```

`PlayerComposer` is not a runtime manager. `CameraComposer` resolves an explicit `PlayerComposer` or explicit transforms. `PlayerViewBehaviour` remains passive evidence and is not camera authority.

## Accepted Player runtime boundary

```text
Framework:
  explicit PlayerInput reference
  validated gameplay action map
  Gate/Pause/Transition availability
  identity and diagnostics

Consumer:
  action interpretation
  movement execution
  tuning and gameplay rules
```

The original P2 scoped runtime-context/binding proposal was not retained. Do not reintroduce it without a concrete requirement that cannot be satisfied by the accepted boundary.

## Known gaps

```text
minimal playable loop integration = active G1 work
actor spawn/materialization = P3, not active
camera output automatic release on exit = C9, ordered after P3
camera output previous-output restoration = C9, ordered after P3
progression save runtime = S1, after meaningful state exists
multiplayer/rebinding/generic interaction = candidates only
```

Route/Activity camera bindings currently apply on enter only. Do not simulate release with `SetActive`, `Camera.enabled` or an implicit fallback.

## Active block

```text
G1 — Minimal Playable Loop
```

First cut:

```text
G1A — FIRSTGAME Minimal Playable Loop Audit
```

See [Execution Status](05-Execution-Status.md), [Roadmap](01-Roadmap.md), [Usage Map](02-Usage-Map.md) and the [Consolidated Development Plan](../Planning/Plano%20de%20Firstgame.md).
