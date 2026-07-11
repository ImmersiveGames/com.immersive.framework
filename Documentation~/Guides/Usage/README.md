# Immersive Framework — User Guide

Canonical designer guide opened from Project Settings:

```text
Documentation~/Guides/Usage/index.html
```

Keep this path stable.

## Current product notes

- Create Player through optional `PlayerRecipe`, `PlayerComposer`, Validate and Apply/Rebuild.
- Player technical declarations remain visible as Advanced/Debug materialization, not the primary creation flow.
- `PlayerViewBehaviour` is passive evidence and is not camera authority.
- Player input availability is framework-owned; movement behavior remains game-owned.
- Cinemachine is mandatory for the Camera Product Surface.
- The previous `CameraComposer` single-player runtime ownership model and Route/Activity camera bindings are superseded by ADR-PROD-0006.
- Do not create new camera integrations with `FrameworkCameraDirector`, Route/Activity legacy bindings, PlayerView activation, raw `Camera.enabled` selection or direct cross-owner priority competition.
- The replacement product flow is under C9 implementation:

```text
CameraRigRecipe
-> CameraRigComposer
-> Cinemachine materialization

Route / Activity / Local Player
-> CameraRequest

CameraOutputContext
-> request arbitration per output/viewport
-> winning rig applied through Cinemachine
```

- Until C9H closes, the current HTML guide may contain historical camera examples. Treat ADR-PROD-0006 and `Current/` documents as authoritative.
- No `Camera.main`, scene/name lookup, global manager or silent fallback is part of the camera model.
