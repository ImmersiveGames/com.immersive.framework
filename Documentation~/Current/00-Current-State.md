# 00 — Current State

Status: **canonical after R0 Documentation and Roadmap Reconciliation**.

## Closed product baseline

| Area | Current state |
|---|---|
| Player product | `PlayerRecipe` and `PlayerComposer` MVPs are closed at the current level. Validate and Apply/Rebuild are the designer-first flow. |
| Camera product | `CameraRecipe` and `CameraComposer` single-player MVPs are closed. Apply/Rebuild materializes a Unity Camera, Cinemachine Camera and Brain idempotently. |
| Technical QA | CameraComposer has dedicated current QA smoke coverage. PlayerComposer participates as a typed fixture, but the dedicated PlayerComposer Apply/Rebuild smoke described by its QA readiness plan was not found in the current QA repository. |
| FIRSTGAME | The real consumer proves `PlayerComposer -> CameraComposer` for the main gameplay camera. |
| Passive player foundation | `PlayerSlot`, `ActorId`, `PlayerEntry`, `PlayerView` and `PlayerControl` remain contracts and diagnostics. |
| Route/Activity camera | Explicit Cinemachine output is applied on enter. Activity supports `UseOwn` and `UseRoute`. Required invalid output blocks; optional invalid output is skipped. |
| Legacy camera | No functional `FrameworkCameraDirector`, `Camera.main` fallback, name-based runtime authority or global camera manager exists in the package. |

## Current product authoring model

```text
Player:
PlayerRecipe (optional) -> PlayerComposer -> Validate -> Apply/Rebuild

Camera:
CameraRecipe (optional) -> CameraComposer -> Validate -> Apply/Rebuild
```

`PlayerComposer` owns authoring intent and technical materialization; it is not a runtime manager. `CameraComposer` resolves an explicit `PlayerComposer` or explicit transforms. `PlayerViewBehaviour` remains passive evidence and is not camera authority.

## Known gaps

```text
PlayerView automatic binding = not provided
PlayerControl product runtime binding/control = pending
dedicated PlayerComposer Apply/Rebuild QA smoke = pending/not present in audited QA repository
input activation and movement = consumer-owned until P2
actor spawning = not provided
camera output automatic release on exit = not provided
camera output previous-priority restoration = not provided
```

Route/Activity camera bindings currently apply on enter only. Do not simulate release with `SetActive`, `Camera.enabled` or an implicit fallback.

## Next active lane

```text
P2 — Player Control Product
```

See the [roadmap](01-Roadmap.md), [usage map](02-Usage-Map.md) and [camera guide](../Guides/Camera-Product-Usage.md).
