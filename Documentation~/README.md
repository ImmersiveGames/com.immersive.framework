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
| Transition | Transition orchestration can use effect adapters such as `UnityFadeCurtainEffectAdapter`. |
| Pause | Runtime Pause state drives Gate blockers, resident `UIGlobal` Pause surface and basic simulation pause through `Time.timeScale = 0`. |
| Pause input | Use `PauseInputActionTrigger` for simple keyboard/controller Pause and optional `PlayerInput` map switching; reserve `PauseInputActionRuntimeBridgeTrigger` + `PauseInputModeUnityPlayerInputRuntimeBridge` for explicit typed InputMode / PlayerInput ownership cuts. |
| RuntimeContent / ContentAnchor | Logical runtime, Unity materialization adapters, bridge/set authoring and composite release helpers are available. |
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

