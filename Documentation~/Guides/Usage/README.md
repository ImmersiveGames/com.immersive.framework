# Immersive Framework - User Guide

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
com.immersive.framework v1.0.0-preview.12
FIRSTGAME-1 - Minimal Playable Framework Flow
FIRSTGAME-2 - Minimal Pause Model Flow
FIRSTGAME-2B - Pause Keyboard Toggle
FIRSTGAME-2C - Pause TimeScale
FIRSTGAME-2D - Global Pause Input Action
FIRSTGAME-2E - Transition Gate Policy
FIRSTGAME-3 - ResetSubject and Unity reset participants
FIRSTGAME-3B - Unity PlayerInput Gate Adapter
FIRSTGAME-4 - Object Reset Group via ResetSelectionConfig
FIRSTGAME-5 - Activity Restart via Reset Selection
FIRSTGAME-5B - Runtime Awaitable reset/restart flow
FIRSTGAME-5C - Reset/Restart authoring validation
FIRSTGAME-5D - Runtime prefab reset through UnityResetSubjectAdapter
```

## Preview 12 reset/restart notes

- `Button_ResetPlayer` should call only `ObjectResetTrigger.RequestObjectReset()`.
- `Button_ResetRoom` should call only `ObjectResetGroupTrigger.RequestObjectResetGroup()`.
- `Button_RestartActivity` should call only `ActivityRestartTrigger.RequestActivityRestart()`.
- `ObjectResetTrigger` targets a `ResetSubjectReference`, not `ObjectEntryDeclaration`.
- `ObjectResetGroupTrigger` owns a `ResetSelectionConfig`; it does not use the legacy group asset path.
- `ActivityRestartTrigger` owns its `ResetSelectionConfig` directly and does not need an `ObjectResetGroupTrigger` on the same GameObject.
- Activity restart uses one visual transition window around reset + clear + re-enter. It should not show two fades.
- New reset/restart runtime orchestration uses `UnityEngine.Awaitable<T>` for Unity-bound async flow.
- Authoring validation scans reset/restart triggers in loaded scenes and reports missing subjects or ambiguous trigger stacking.

## Runtime prefab reset

Use `UnityResetSubjectAdapter` on scene objects or runtime-instantiated prefabs that must participate in framework reset/restart flows.

Recommended prefab shape:

```text
RuntimeObjectPrefab
  UnityResetSubjectAdapter
    Subject Id Generation = Runtime Instance
    Runtime Id Prefix = firstgame.runtime-object
  UnityTransformResetParticipant
  UnityGameObjectActiveResetParticipant
  <gameplay components>
```

The adapter registers a `ResetSubject` and its local reset participants with the current `ResetRegistry`. It does not spawn the object, own input, create PlayerActor, save progress or perform pooling.

For the next FIRSTGAME smoke, instantiate a prefab at runtime, let `UnityResetSubjectAdapter` register it, then verify that `Reset Room` and `Restart Activity` can reset it.
