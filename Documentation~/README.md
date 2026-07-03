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
| Object Reset | Single-target Object Reset is available through `ObjectResetTrigger`; multi-target reset sets are available through `ObjectResetGroupAsset` and `ObjectResetGroupTrigger`; authored Activity restart uses reset selection policy directly and composes reset execution with Activity Clear/Re-enter through one visual transition in `ActivityRestartTrigger`. |
| Reset/Restart async | New Unity runtime reset/restart orchestration uses `UnityEngine.Awaitable<T>` wrappers where the flow is tied to Unity runtime/main thread. Older route/activity APIs may still expose `Task` until migrated in a separate cut. |
| Reset/Restart validation | Open-scene authoring validation now scans `ObjectResetGroupTrigger` and `ActivityRestartTrigger` for missing targets, ambiguous explicit/scoped target policies and misleading trigger stacking on the same GameObject. |
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

The old `Guides/`, `ADRs/` and `Planning/` folders remain available as historical source material. They are not the package documentation entry point and should not be used as the primary setup, QA or troubleshooting path.

Needs manual decision:

- Whether historical guides should be archived, deleted or kept for deep reference.
- Whether old ADRs and roadmap files should receive a stronger historical banner in a future documentation cleanup.
- Whether any user-facing examples in the old guides should be promoted into these top-level docs.
---

## Guia de uso canônico

O botão `Open Usage Guide` em `Project Settings > Immersive Framework` abre:

```text
Documentation~/Guides/Usage/index.html
```

Esse path deve permanecer estável. Atualize o conteúdo do `index.html` conforme o package evolui, sem mudar o link público do Project Settings.

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

## Object Reset Group

`ObjectResetGroupTrigger` is an opt-in Unity authoring surface for resetting several logical `ObjectEntryDeclaration` targets in sequence. It composes existing Object Reset requests; it does not restart an Activity, reload scenes, discover participants automatically, or perform physical reset side effects itself.

Recommended First Game use:

```text
Button_ResetRoom
  ObjectResetGroupTrigger
    Group Id = firstgame.room-reset
    Allow No Participants = false
    Stop On Failure = true
    Entries[0] = firstgame.player
```

For project asset groups, prefer string `Object Entry Id` entries. For scene-local buttons, inline entries may reference scene `ObjectEntryDeclaration` components directly.

## Activity Restart via Object Reset Group

`ActivityRestartTrigger` is an authored composition surface for small gameplay restart flows. It now owns reset selection policy directly instead of depending on an `ObjectResetGroupTrigger`: explicit targets, an `ObjectResetGroupAsset`, current Activity entries, current Route entries, Route + Activity entries, or all current entries. It then clears the current Activity and requests the same Activity again. This is not Cycle Reset, does not reload the Route, and does not create Player/Actor lifecycle ownership.


## Reset / Restart authoring validation

When running Authoring Validation with open-scene bindings enabled, the framework scans scene-authored reset/restart surfaces:

```text
ObjectResetGroupTrigger
ActivityRestartTrigger
```

The validator reports missing reset targets, invalid `ExplicitTargets` setup, scoped policies that ignore explicit entries, and ambiguous button GameObjects that stack `ObjectResetTrigger`, `ObjectResetGroupTrigger` and `ActivityRestartTrigger`. This does not execute reset or restart; it only checks authoring shape.

## Runtime Awaitable policy

For preview.11 reset/restart additions, Unity runtime orchestration should use `UnityEngine.Awaitable<T>` when the flow depends on the Unity runtime/main thread. This applies to the new Object Reset Group and Activity Restart authored flows. Legacy `Task` APIs outside this cut remain unchanged until a dedicated migration.

### v1.0.0-preview.12 — Runtime Object Participation Foundation

Adds `UnityRuntimeObjectParticipationAdapter`, allowing runtime-enabled or runtime-instantiated objects to register `ObjectEntry` descriptors and reset participants without requiring scene-authored `ObjectEntryDeclaration` components. This is a small participation layer, not PlayerActor or a spawner system.
