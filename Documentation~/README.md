# Immersive Framework Package Documentation

This is the public documentation entry point for `com.immersive.framework`.

Read in this order:

1. [Setup](Setup.md)
2. [Authoring](Authoring.md)
3. [Runtime Surfaces](Runtime-Surfaces.md)
4. [QA Smokes](QA-Smokes.md)
5. [Troubleshooting](Troubleshooting.md)
6. [Architecture](Architecture.md)

## Current package state

| Area | Current state |
| --- | --- |
| Game Application | `GameApplicationAsset` owns startup route and optional `UIGlobal` scene policy. |
| Route/Activity | Route lifecycle and Activity flow are the baseline navigation model. |
| UIGlobal | Shared app/session scene for Transition, Loading and Pause presentation adapters. |
| Loading | Loading surface adapters are collected from `UIGlobal`; progress contracts are available for diagnostics/presentation. |
| Transition | Transition orchestration can use effect adapters such as `UnityFadeCurtainEffectAdapter`; Route/Activity `TransitionGateMode` can block lifecycle requests, input, interaction and gameplay during transition windows; `UnityPlayerInputGateAdapter` can make gameplay `PlayerInput` obey those Gate blockers. |
| Pause | Runtime Pause state drives Gate blockers, resident `UIGlobal` Pause surface and basic simulation pause through `Time.timeScale = 0`. |
| Pause input | Use `PauseInputActionTrigger` for simple keyboard/controller Pause through `Global/Pause`; reserve `PauseInputActionRuntimeBridgeTrigger` + `PauseInputModeUnityPlayerInputRuntimeBridge` for explicit typed InputMode / PlayerInput ownership cuts. |
| RuntimeContent / ContentAnchor | Logical runtime, Unity materialization adapters, bridge/set authoring and composite release helpers are available. |
| Reset | The canonical reset path is `ResetSubject` / `ResetParticipant` / `ResetRegistry` / `ResetSelectionConfig` / `ResetExecutor`. Unity authoring uses `UnityResetSubjectAdapter`, `UnityResetParticipantBehaviour`, `UnityTransformResetParticipant` and `UnityGameObjectActiveResetParticipant`. |
| Object Reset surfaces | `ObjectResetTrigger` resets one `ResetSubject`; `ObjectResetGroupTrigger` resets a `ResetSelectionConfig`; `ActivityRestartTrigger` composes reset execution with Activity Clear/Re-enter through one visual transition. |
| Reset/Restart async | Unity runtime reset/restart orchestration uses `UnityEngine.Awaitable<T>` where the flow is tied to Unity runtime/main thread. Older route/activity APIs may still expose `Task` until migrated in a separate cut. |
| Reset/Restart validation | Open-scene authoring validation scans `ObjectResetGroupTrigger` and `ActivityRestartTrigger` for missing `ResetSelectionConfig` targets, ambiguous explicit/scoped subject policies and misleading trigger stacking on the same GameObject. |
| QA | `FrameworkQaCanvas` exposes package smokes for setup and regression validation. |

## Documentation classification

| Source | Classification | Active navigation |
| --- | --- | --- |
| `README.md` and the files listed above | Public package documentation | Yes |
| `Setup.md`, `Authoring.md` | Setup/authoring guide | Yes |
| `QA-Smokes.md` | QA/smoke guide | Yes |
| `Troubleshooting.md` | Troubleshooting | Yes |
| `Runtime-Surfaces.md`, `Architecture.md` | Public runtime/architecture reference | Yes |
| `Guides/` | Historical phase guides; some content migrated here | No |
| `ADRs/` | Historical/internal decisions for project development | No |
| `Planning/` | Historical/internal roadmap | No |

## Historical / Not Active

The old `Guides/`, `ADRs` and `Planning` folders remain available as historical source material. They are not the package documentation entry point and should not be used as the primary setup, QA or troubleshooting path.

Needs manual decision:

- Whether historical guides should be archived, deleted or kept for deep reference.
- Whether old ADRs and roadmap files should receive a stronger historical banner in a future documentation cleanup.
- Whether any user-facing examples in the old guides should be promoted into these top-level docs.

## Guia de uso canonico

O botao `Open Usage Guide` em `Project Settings > Immersive Framework` abre:

```text
Documentation~/Guides/Usage/index.html
```

Esse path deve permanecer estavel. Atualize o conteudo do `index.html` conforme o package evolui, sem mudar o link publico do Project Settings.

## Transition Gate

Transition Gate is separate from Pause. It does not use `Time.timeScale`, does not replace `Global/Pause`, and does not make `PauseKeepUiActionMap` canonical. For First Game style Route/Activity fades, configure `Transition Gate = InputInteractionAndGameplay` on the relevant Route and Activity assets.

## Unity PlayerInput Gate Adapter

`UnityPlayerInputGateAdapter` is an opt-in Unity Input System adapter for gameplay-owned `PlayerInput` components. It watches the current framework Gate snapshot and disables the configured gameplay action map, normally `Player`, while `InputAcceptance` or `GameplayAction` is blocked. This makes temporary or consumer-side movement scripts that read `Player/Move` obey Pause and Transition Gate without making those scripts framework-aware.

Recommended First Game setup:

```text
PlayerPrototype
  PlayerInput
  FirstGamePlayerMover
  UnityPlayerInputGateAdapter
    Player Input = PlayerPrototype/PlayerInput
    Gameplay Action Map Name = Player
    Block On Input Acceptance = true
    Block On Gameplay Action = true
    Block Mode = Disable Action Map
```

The adapter is not a Player/Actor lifecycle, does not spawn players, does not own `PlayerInputManager`, and does not replace future movement architecture.

## Reset authoring

A resettable Unity object uses a reset subject and one or more reset participants:

```text
ResettableObject
  UnityResetSubjectAdapter
    Subject Id = firstgame.resettable.object
    Scope = Route / Activity / Runtime
  UnityTransformResetParticipant
  UnityGameObjectActiveResetParticipant
```

`ObjectResetTrigger` should target a `ResetSubjectReference`, either through `UnityResetSubjectAdapter` or an explicit Reset Subject Id. It does not require `ObjectEntryDeclaration`.

## Object Reset Group

`ObjectResetGroupTrigger` is an opt-in Unity authoring surface for resetting a selected set of `ResetSubject` entries through `ResetExecutor`. It does not restart an Activity, reload scenes, create participants automatically or perform physical reset side effects itself.

Recommended First Game use:

```text
Button_ResetRoom
  ObjectResetGroupTrigger
    Group Id = firstgame.room-reset
    Selection
      Mode = ExplicitSubjects
      Explicit Subjects[0] = PlayerPrototype/UnityResetSubjectAdapter
      Allow No Subjects = false
      Allow No Participants = false
      Stop On Failure = true
```

For scoped reset, use `CurrentActivitySubjects`, `CurrentRouteSubjects`, `CurrentRouteAndActivitySubjects`, `AllCurrentSubjects`, `RuntimeOnlySubjects` or `SceneOnlySubjects`.

## Activity Restart via Reset Selection

`ActivityRestartTrigger` is an authored composition surface for small gameplay restart flows. It owns reset selection directly through `ResetSelectionConfig`, then clears the current Activity and requests the same Activity again. This is not Cycle Reset, does not reload the Route, and does not create Player/Actor lifecycle ownership.

## Reset / Restart authoring validation

When running Authoring Validation with open-scene bindings enabled, the framework scans scene-authored reset/restart surfaces:

```text
ObjectResetGroupTrigger
ActivityRestartTrigger
```

The validator reports missing reset subjects, invalid `ExplicitSubjects` setup, scoped policies that ignore explicit subjects, and ambiguous button GameObjects that stack `ObjectResetTrigger`, `ObjectResetGroupTrigger` and `ActivityRestartTrigger`. This does not execute reset or restart; it only checks authoring shape.

## Runtime prefab reset

Runtime-instantiated objects participate in reset by registering `ResetSubject` and participants through `UnityResetSubjectAdapter` with runtime id generation. The old runtime object participation path is not part of the reset model.
