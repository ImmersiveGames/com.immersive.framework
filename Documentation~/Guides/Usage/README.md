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
FIRSTGAME Usage Model Hardening / POST-RESET-C
C1 - Player reset model hardening
C2 - UI, buttons and reasons cleanup
C3 - Script naming and runtime object spawner hardening
C4 - Runtime Box prefab cleanup
C5 - FIRSTGAME README and manual smoke
C6 - Local cleanup
C7 - Framework usage guide sync
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

## FIRSTGAME canonical runtime reset model

Use `UnityResetSubjectAdapter` on scene objects or runtime-instantiated prefabs that must participate in framework reset/restart flows.

Canonical FIRSTGAME player shape:

```text
PlayerPrototype
  ObjectEntryDeclaration
    objectEntryId = firstgame.player
  PlayerInput
  FirstGamePlayerMover
  UnityPlayerInputGateAdapter
  UnityResetSubjectAdapter
    Subject Id = firstgame.player
    Scope = Activity
  UnityTransformResetParticipant
  FirstGamePlayerResettableState : IUnityResettable
    ResetParticipantId = firstgame.player.resettable-state
```

Canonical runtime prefab shape:

```text
FG_RuntimeBox
  BoxVisual
    MeshFilter = Cube
    BoxCollider
  UnityResetSubjectAdapter
    Subject Id Generation = Runtime Instance
    Runtime Id Prefix = firstgame.runtime.box
  UnityTransformResetParticipant
```

The adapter registers a `ResetSubject` and its local reset participants with the current `ResetRegistry`. It does not spawn the object, own input, create PlayerActor, save progress or perform pooling.

Current smoke expectations:

```text
Player only: subjects='1' participants='2'
Player + 1 Runtime Box: subjects='2' participants='3'
Player + 2 Runtime Boxes: subjects='3' participants='4'
```
