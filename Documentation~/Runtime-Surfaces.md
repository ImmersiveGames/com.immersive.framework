# Runtime Surfaces

This page summarizes the current runtime-facing surfaces exposed by `com.immersive.framework`.

## Surface model

Loading, Transition and Pause are current runtime-facing surfaces. A Surface is a stable runtime capability boundary with readiness, diagnostics and consumer-facing result language; it is not a synonym for any visual component.

Adapters execute local side effects and return evidence. They do not decide lifecycle, route/activity ownership or cross-domain policy. Multi-step orchestration belongs in explicit runtime services.

When a Surface or service aggregates adapters or subsystem results, its diagnostics must preserve the boundary-local failed stage, original subsystem evidence, adapter result when present, blocking issues and explicit optional/no-op reason. Surface success must mean that required side effects were applied, or that the operation was explicitly skipped by declared optional/no-op policy.

`UIGlobal` is a runtime surface host/consumer for app/session scoped presentation. It is not a universal manager.

## Surface Adapter Contract Pattern

Surface/Adapter extensibility uses domain contract patterns, not a general Surface layer.

Loading is the current reference pattern:

- The Surface defines Loading intent, availability, request data, result semantics, optional/no-op behavior and diagnostics.
- The Adapter executes one local Unity side effect and returns `LoadingSurfaceResult` evidence.
- The runtime boundary uses explicit adapters, aggregates results, preserves `LoadingSurfaceAdapterEvidence`, reports required missing capability and keeps optional/no-op explicit.
- Consumers pass source/reason diagnostics and handle absence or failure explicitly.
- QA evidence should assert status, blocking issues, adapter evidence count/names/statuses, progress support, side effects and no-op/failure causes.

Do not copy Loading status values into unrelated domains. A new Surface/Adapter should define its own domain request, result and status language.

F53 selects Transition Surface / Effects Hardening as the next contract-first track. F54 accepts the Transition Surface / Effects Contract. F55 hardens Transition runtime evidence. F56 documents the first practical authoring path for using Transition in a playable flow. Transition is not a broad Surface layer and it is not upgraded to the Loading reference/pilot status.

## Game Application

`GameApplicationAsset` is the project-authored application root. It selects the startup route and controls whether a canonical app/session scoped `UIGlobal` scene is loaded.

`FrameworkRuntimeHost` is the runtime owner that starts the application, loads `UIGlobal` when configured, requests route/activity changes and coordinates visual surfaces. It is internal runtime infrastructure, not a user-authored manager.

## Route and Activity

Route baseline:

- `RouteAsset`
- route lifecycle runtime
- route content bindings and lifecycle receivers
- route content scene composition
- lifecycle operation evidence projection in request logs

Activity baseline:

- `ActivityAsset`
- activity flow runtime
- activity content lifecycle receivers
- activity content scene composition/release
- activity operation planning and execution
- lifecycle operation evidence projection in request logs

Route owns top-level navigation. Activity owns the active unit inside the route. Activity content does not replace route lifecycle.

The shared `lifecycleOperation*` projection is lifecycle-local diagnostics. It does not create a Surface layer, does not introduce a GameFlow request envelope and does not replace Route/Activity domain statuses.

## UIGlobal

`UIGlobal` is an optional shared scene for app/session scoped presentation. It can provide:

- Transition adapters.
- Loading adapters.
- Pause surface adapters.

When configured as required, missing surfaces are reported by diagnostics. When not configured, runtime uses explicit no-op behavior.

## Loading

Loading runtime surfaces include:

- `UnityLoadingSurfaceAdapter`
- `ILoadingSurfaceAdapter`
- `ILoadingSurfaceProgressPresentationAdapter`
- `LoadingSurfaceAdapterEvidence`
- loading result/readiness/progress contracts

Loading presentation is separate from transition effects.

Aggregate `LoadingSurfaceResult` diagnostics preserve named adapter evidence for applied, skipped and failed adapters. Consumers should read the evidence collection or diagnostic fields instead of parsing adapter-prefixed issue text.

## Transition

Transition runtime surfaces include:

- `ITransitionOrchestrator`
- `NoOpTransitionOrchestrator`
- transition requests/plans/results
- transition effect adapters
- `UnityFadeCurtainEffectAdapter`

Transition controls the visual envelope around route/activity operations; it is not a loading screen by itself.

Current Transition status: practical authoring guide accepted for first-flow use.

Contract definitions:

- Transition Surface: visual envelope before/after Route/Activity operations.
- Transition Effect: concrete visual operation such as fade, curtain, blackout, cut or crossfade.
- Transition Effect Adapter: Unity-side executor that applies one local visual effect and returns `TransitionEffectResult` evidence.
- Transition Consumer: `FrameworkRuntimeHost` / Route/Activity request execution.
- Transition Host: `UIGlobal` or another explicit visual surface host, not a universal manager.

Transition results preserve named internal `TransitionEffectAdapterEvidence` for called or missing required effect adapters. Route/Activity logs project `transitionEffectAdapterEvidenceCount`, applied/skipped/failed counts, blocking issue count, names and statuses while preserving existing `transition*` and `transitionEffect*` fields.

For first practical authoring, use `Documentation~/Guides/First-Practical-Flow-Transition.md`. The supported path is `GameApplicationAsset` loading `UIGlobal`, `UIGlobal` hosting a Transition Effect adapter such as `UnityFadeCurtainEffectAdapter`, an optional Loading adapter when progress is needed, a gameplay `RouteAsset` with primary scene and `ActivityAsset` entries requested through `FrameworkRuntimeHost` via existing triggers or QA Canvas.

## Pause

Pause runtime surfaces include:

- `PauseRuntime`
- `PauseRequestTrigger`
- `PauseSurfaceRuntime`
- `IPauseSurfaceAdapter`
- `UnityPauseResidentSurfaceAdapter`

The production-facing default is a resident Pause panel in `UIGlobal` that is shown/hidden from logical Pause state.

Current Pause presentation status:

- Logical Pause is owned by `PauseRuntime`; it changes Pause state and Gate evidence only.
- Pause resident surface is the supported presentation path for current runtime use.
- Pause visual/materialization through RuntimeContent + ContentAnchor is experimental/frozen and must not be treated as a broad Surface or Adapter contract.
- InputMode apply is a separate boundary; it synchronizes Pause intent with Unity `PlayerInput` and does not own visual presentation.

`IPauseSurfaceAdapter` currently applies a snapshot through `void Apply(PauseSnapshot)`. That is sufficient for the resident-only scope, but it is not strong enough evidence for promoting Pause visual/materialization to a broader Surface contract.

## Pause input and InputMode

Current Pause input path:

- `PauseInputActionRuntimeBridgeTrigger`
- `PauseInputModeUnityPlayerInputRuntimeBridge`
- `PauseInputModeRequestMapper`
- `InputModeUnityPlayerInputRequestApplication`
- Unity `PlayerInput`

The bridge path keeps logical Pause, `InputMode` and Unity `PlayerInput` synchronized. It does not create a framework input manager, call `PlayerInputManager.JoinPlayer`, spawn players or read gameplay commands.

Pause visual surface application and Pause/InputMode `PlayerInput` apply are separate boundaries. The visual Pause surface shows or hides resident UI; the InputMode apply path synchronizes logical Pause intent with Unity `PlayerInput` through an explicit apply boundary.

## RuntimeContent

RuntimeContent owns logical content identity and state:

- `RuntimeContentHandle`
- `RuntimeMaterializationRequest`
- `RuntimeMaterializationResult`
- `RuntimeReleaseRequest`
- `RuntimeReleaseResult`
- lifecycle materialization registry and release plan/execution contracts

RuntimeContent handles are logical evidence. Physical Unity objects stay in adapter evidence and materialization services.

## ContentAnchor

ContentAnchor owns anchor declarations, logical binding and Unity adapter orchestration:

- `ContentAnchorDeclaration`
- `RouteContentAnchor`
- `ActivityContentAnchor`
- `ContentAnchorSet`
- `RuntimeContentAnchorBinding`
- `ContentAnchorBindingCleanup`
- `ContentAnchorReleaseExecution`
- `ContentAnchorMaterializationService`
- `UnityContentAnchorMaterializationBridge`
- `UnityContentAnchorMaterializationBridgeSet`
- `UnityContentAnchorPlacementAdapter`
- composite lifecycle release executor

The bridge components are authored Unity wrappers. The reusable materialization orchestration belongs to the non-MonoBehaviour service path.
