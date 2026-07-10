# Immersive Framework — User Guide

Canonical designer guide opened from Project Settings:

```text
Documentation~/Guides/Usage/index.html
```

Keep this path stable.

## Current product notes

- Create Player through optional `PlayerRecipe`, `PlayerComposer`, Validate and Apply/Rebuild.
- Create the main camera through optional `CameraRecipe`, `CameraComposer`, an explicit PlayerComposer or explicit transforms, Validate and Apply/Rebuild.
- Player technical declarations remain visible as Advanced/Debug materialization, not the primary creation flow.
- `PlayerViewBehaviour` is passive evidence, not camera authority.
- Player input activation and movement remain game-owned until P2 Player Control Product.
- Route/Activity camera outputs apply on enter; automatic release/restoration on exit is pending.
- No `Camera.main`, scene/name lookup, global manager or legacy director is part of the current flow.
