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
POST-RESET-D - Transition / Loading Surface Hardening
D1 - Pause During Transition Policy
D2 - Framework Runtime Log Level Hygiene
D3 - Loading / Transition Semantics Documentation
```

## Preview 12 loading / transition semantics

- Transition and Loading are separate concepts.
- Route switch with scene load should report `transition='SucceededWithUnitySurface'` and `loading='SucceededWithUnitySurface'`.
- Activity switch without scene load should report `transition='SucceededWithUnitySurface'` and `loading='SkippedNoSceneLoad'`. This is correct and is not an error.
- Activity Restart uses reset + clear + reenter. It may use transition; loading appears only if there is scene load or release side-effect.
- Pause during transition/loading is rejected with `status='Rejected'` and `policyStatus='RejectedTransitionInProgress'`. It does not open the PauseSurface, does not change TimeScale and does not enqueue the request.
- After D2, runtime log levels are: `Info` for operational summaries, `Debug` for technical diagnostics and `Trace` for waiting/retry/polling noise.

## Preview 12 — como criar objetos no fluxo

- **Objeto resetável:** filho do Activity content root + `UnityResetSubjectAdapter` (`Scope = Activity`) + participants.
- **Player:** `GameplayRoot` + `PlayerInput` + mover do consumidor + Gate + Reset adapter. Evidência de slot/entry é opcional.
- **NPC:** filho do Activity content root + `ActorDeclaration` (`NonPlayer`) + Reset adapter. Sem `PlayerSlot` / `PlayerInput`.
- Detalhes passo a passo: seção **Como criar** em `Usage/index.html` (`#como-criar`).

## Preview 12 camera notes

- Put one `FrameworkCameraDirector` on a persistent session object; point all Route/Activity bindings to it.
- Route camera enters via `FrameworkRouteCameraBinding` on Route content; Activity camera via `FrameworkActivityCameraBinding`.
- `PlayerViewBehaviour` is passive evidence only — it does not activate cameras. Use Camera Director bindings for gameplay camera.
- Optional Cinemachine path: assign `FrameworkCinemachineRigApplier` to `rigApplier` on the director.

## Preview 12 player evidence notes

- FIRSTGAME movement stays on consumer scripts (`FirstGamePlayerMover` + `PlayerInput`).
- Framework player components (`PlayerSlotDeclaration`, `PlayerActorDeclaration`, `PlayerEntryBehaviour`, etc.) are optional passive evidence for validation/QA.
- They do not join players, spawn actors, bind cameras or route input.
- `UnityPlayerInputGateAdapter.SourceSlot` is diagnostic only.

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
