# Immersive Framework — User Guide

Canonical user-facing guide opened from:

```text
Project Settings > Immersive Framework > Open Usage Guide
```

Stable path:

```text
Documentation~/Guides/Usage/index.html
```

## Update policy

- Keep this folder/path stable.
- Replace `index.html` as the framework preview progresses.
- The HTML should describe the latest supported package preview, not the historical roadmap.
- Do not rename this folder unless `ImmersiveFrameworkEditorSettingsUtility.UsageGuidePath` is changed in the same cut.
- Version-specific drafts may exist elsewhere, but Project Settings should always open this canonical guide.

## Current content

```text
com.immersive.framework v1.0.0-preview.8
FIRSTGAME-1 — Minimal Playable Framework Flow
FIRSTGAME-2 — Minimal Pause Model Flow
FIRSTGAME-2B — Pause Keyboard Toggle
FIRSTGAME-2C — Pause TimeScale
FIRSTGAME-2D — Global Pause Input Action
FIRSTGAME-3 — Minimal Controllable Object + Object Reset
FIRSTGAME-4 — Transition Gate Policy for Route/Activity/ActivityClear
```

## Important preview.8 notes

- Pause input is `Global/Pause`; do not use `Player/Pause + UI/Pause` as the canonical path.
- Pause applies `Time.timeScale = 0`; Transition Gate does not use `Time.timeScale`.
- Object Reset is validated for `firstgame.player` through explicit `ObjectResetUnityParticipantSource` registration.
- Transition Gate is validated for Route, Activity and ActivityClear with `InputInteractionAndGameplay`.
- `FirstGamePlayerMover` is temporary consumer-side movement. It does not obey framework Gate by itself; a future PlayerInput/Gameplay Gate Adapter or mature Player/Actor movement flow should replace it.
