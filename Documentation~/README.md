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
| Transition | Transition orchestration can use effect adapters such as `UnityFadeCurtainEffectAdapter`; Route/Activity/ActivityClear `TransitionGateMode` can block lifecycle requests, input, interaction and gameplay during transition windows. |
| Pause | Runtime Pause state drives Gate blockers, resident `UIGlobal` Pause surface and basic simulation pause through `Time.timeScale = 0`. |
| Pause input | Use `PauseInputActionTrigger` with `Global/Pause` for simple keyboard/controller Pause. Do not use `Player/Pause + UI/Pause` as the canonical path; reserve the bridge path for explicit typed InputMode / PlayerInput ownership cuts. |
| Object Entry / Object Reset | Route-scoped Object Entry and explicit Object Reset participants are usable in First Game. `firstgame.player` reset is validated through `ObjectResetTransformParticipant` and `ObjectResetUnityParticipantSource`. |
| RuntimeContent / ContentAnchor | Logical runtime, Unity materialization adapters, bridge/set authoring and composite release helpers are available. |
| QA | `FrameworkQaCanvas` exposes package smokes for setup and regression validation. |

## First Game preview.8 status

Validated through `v1.0.0-preview.8`:

- Boot, Menu Route and Gameplay Route.
- Activity A/B flow, ActivityClear and manual restore.
- Pause with `Global/Pause`, resident Pause surface and `Time.timeScale = 0`.
- Object Reset for `ObjectEntry:firstgame.player`.
- Transition Gate for Route, Activity and ActivityClear using `InputInteractionAndGameplay`.

Accepted limitation: `FirstGamePlayerMover` is a temporary consumer-side script. It reads `InputAction` directly and does not participate in the framework Gate system by itself. Gameplay movement blocking during Transition Gate belongs to a future PlayerInput/Gameplay Gate Adapter or mature Player/Actor movement flow.

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

Transition Gate is separate from Pause. It does not use `Time.timeScale`, does not replace `Global/Pause`, and does not make `PauseKeepUiActionMap` canonical. For First Game style Route/Activity/ActivityClear fades, configure `Transition Gate = InputInteractionAndGameplay` on the relevant Route and Activity assets. Transition Gate is logical; gameplay scripts that read input directly must participate through an adapter/receiver to obey it.
