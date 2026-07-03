# F25R-ADR-ACTIVITY-001 - Activity Scene Operation Architecture Reset

Status: Accepted / Documentation reset  
Phase: F25R - Activity Scene Operation Architecture Reset  
Type: Framework Core / Activity Flow Architecture  
Last updated: 2026-06-27

## Context

F25A and F25B established the correct baseline:

- `ActivityContentProfileAsset` declares Activity-owned content scenes.
- `ActivitySceneCompositionPlan` and `ActivitySceneCompositionResult` provide side-effect-free planning and diagnostics.

F25C through F25D4 added execution, release and local guards for additive Activity scenes. Those cuts are useful as evidence, but they are now classified as experimental and partial execution cuts. They do not define the final Activity operation architecture.

The observed invalid behavior is:

```text
ActivityVisualTransitionMode.Seamless
+
Activity scene load/release side-effect
=
LoadingSurface appears without a fade envelope
```

This is invalid. Loading must not be opened as a direct side-effect of scene composition or release when the Activity visual policy skipped the visual envelope.

## Problem

The current runtime separates decisions that must be made together:

- `ActivityVisualTransitionMode` decides whether Activity transition runs.
- Activity scene composition/release decides whether scenes load or unload.
- `FrameworkRuntimeHost` probes possible scene side-effects to decide whether to show `LoadingSurface`.

That split lets an Activity operation open LoadingSurface even when `Seamless` skipped TransitionSurface. It also leaves Route startup Activity composition as an incomplete special path and keeps Activity scene tracking as local runtime evidence rather than an explicit ledger.

## Decision

Activity visual policy, Activity scene composition, Activity scene release and Activity loading presentation must be reconciled through one Activity operation plan.

`ActivityOperationPlan` is the owner of the decision.

Minimum planned shape:

```text
ActivityOperationPlan
  PreviousActivity
  TargetActivity
  OperationKind
    Start
    Switch
    Clear
    RouteStartup
    RouteExitCleanup
  VisualMode
    Seamless
    Fade
    FadeWithLoading
  ScenesToLoad
  ScenesToRelease
  HasSceneSideEffects
  RequiresVisualOcclusion
  RequiresLoadingSurface
  IsValid
  BlockingIssues
```

The executor must consume the plan as a single operation:

```text
validate plan
transition before, if RequiresVisualOcclusion
loading show, if RequiresLoadingSurface
release scenes
load scenes
switch or clear Activity state
run Activity local lifecycle
loading hide, if RequiresLoadingSurface
transition after, if RequiresVisualOcclusion
```

`LoadingSurface` remains presentation only. It does not own loading, scene composition, scene release, progress, lifecycle state or policy.

`TransitionSurface` is the visual envelope. When `LoadingSurface` appears because an Activity operation has scene side-effects, the operation must have a coherent visual envelope.

## Visual Envelope

Visual Envelope means the complete visual occlusion interval around the operation:

```text
TransitionSurface before/open
  optional LoadingSurface show/update/hide
  Activity scene release/load and state operation
TransitionSurface after/close
```

The envelope is required when Activity scene load/release side-effects would otherwise expose intermediate scene state to the player.

## Final Visual Rules

```text
Seamless
  Activity scene load/release may execute.
  TransitionSurface is skipped.
  LoadingSurface is skipped.

Fade
  Activity scene load/release may execute.
  TransitionSurface is used.
  LoadingSurface is skipped.

FadeWithLoading
  Activity scene load/release may execute.
  TransitionSurface is used.
  LoadingSurface is used when the Activity operation requests it.
```

The bug is not `Seamless + scene side-effect`. The bug is `LoadingSurface` opening when the visual mode is not `FadeWithLoading`.

Invalid combinations:

```text
Seamless + LoadingSurface = invalid
Fade + LoadingSurface = invalid
```

The runtime must not silently upgrade `Seamless` or `Fade` to `FadeWithLoading`.

`AlreadyLoaded` is diagnostics only. It is not a scene load side-effect and must not require LoadingSurface.

## Route Startup Activity

Route startup Activity must use the same `ActivityOperationPlan` and executor path as a normal Activity operation.

The Route operation may wrap the whole Route switch in its own Route visual envelope. That does not make startup Activity composition a separate incomplete path. Startup Activity scene composition, release evidence and ledger writes must still be represented by the Activity operation model with `OperationKind.RouteStartup`.

## Route Exit Cleanup

Route content remains Route-scoped. Route content has no keep policy.

Activity content release policy applies only to Activity changes and Activity clear:

```text
ReleaseOnActivityChange
KeepOnActivityChange
```

On Route change, all Activity-owned content from the previous Route must be removed, ignoring Activity release policy. Content that survives Route changes belongs to Session, not Route or Activity.

Route exit cleanup must be represented by the Activity operation model with `OperationKind.RouteExitCleanup`.

## Activity Scene Ledger

Activity scene tracking must become an explicit ledger, not only a local tracked-scene list.

Minimum planned ledger entry:

```text
ActivitySceneLedgerEntry
  RouteInstanceId
  ActivityId
  ContentId
  ScenePath
  ReleasePolicy
  Ownership = Activity
  UnitySceneLoaded
```

Rules:

- Activity change respects `ReleaseOnActivityChange` and `KeepOnActivityChange`.
- Activity clear respects `ReleaseOnActivityChange` and `KeepOnActivityChange`.
- Route change removes every Activity-owned entry for the previous `RouteInstanceId`.
- A tracked entry with `UnitySceneLoaded = false` is stale and must be removed or corrected.
- Activity identity, Route identity and Content identity must not be fabricated from scene paths or object names.

## Async Execution Model

`ActivityOperationPlan` is synchronous and side-effect-free.

- It does not use `UnityEngine.Awaitable`.
- It does not load or unload scenes.
- It does not call transition or loading APIs.

`ActivityOperationExecutor` is the async runtime execution boundary.

- It may use `UnityEngine.Awaitable` for `TransitionSurface`, `LoadingSurface`, scene load/release and future progress operations.
- It must not use `Task.Delay`.
- It must not create coroutine-based canonical flow.
- It must not await the same `Awaitable` instance more than once.
- It must not mix loose Task-based async with framework lifecycle behavior unless an explicit adapter boundary exists.

`LoadingSurfaceAdapter` remains presentation only.

- It may expose awaitable show/hide/progress methods in a later executor cut.
- It does not own lifecycle, loading policy, scene lifecycle or progress aggregation.

## Status Of F25C-D4

F25C through F25D4 are retained as experimental/partial execution evidence:

| Cut | Status after F25R | Keep | Replace |
|---|---|---|---|
| F25C - Activity Scene Composition Execution | Partial / experimental | Additive scene execution evidence and diagnostics | Loading decision by host probe |
| F25D - Activity Content Release | Partial / experimental | Release result language and policy evidence | Release execution outside a unified operation plan |
| F25D1 - Activity release policy semantics | Preserve | Activity policy only affects Activity change/clear; Route change force-releases | Nothing now |
| F25D2 - Activity Loading Leak Guard | Preserve as rule | `AlreadyLoaded` is not a side-effect | Local guard as final architecture |
| F25D3 - Route Change Stale Tracking Fix | Preserve as rule | Route switch removes Activity-owned content | Local tracked-list model |
| F25D4 - Tracked-But-Unloaded Fix | Preserve as rule | Verify Unity scene state before `AlreadyLoaded` | Tracked evidence as source of truth |

No F25C-D4 runtime code is removed in this reset cut.

## Follow-Up Cuts

| Cut | Name | Scope |
|---|---|---|
| F25E | Activity Operation Plan Baseline | Add side-effect-free `ActivityOperationPlan` / result model and diagnostics. |
| F25F | Activity Operation Executor | Move Activity transition/loading/release/load/state sequencing to one executor. |
| F25G | Startup Activity Path Unification | Route startup Activity uses the same Activity operation plan/executor path. |
| F25H | Activity Scene Ledger | Replace loose Activity scene tracking with route-scoped ledger entries. |
| F25R1 | Activity Visual Policy / Awaitable Clarification | Clarify visual policy, LoadingSurface ownership and Awaitable boundaries. |
| F25I | Validator Guards | Initial validator guards for the reset path. |

## Non-Goals

This reset does not implement runtime code, validators, Editor UI, asmdef changes, assets, QA prefabs, Addressables, loading progress, coroutines, DOTween, camera, input, audio, player, pause or save behavior.

## Consequences

- Future Activity execution cuts must start from `ActivityOperationPlan`.
- LoadingSurface must only be opened when the authored visual mode requests it.
- Activity scene composition/release must report explicit blocking issues instead of silent visual fallback.
- Startup Activity and Route cleanup become first-class Activity operation kinds.
- F25C-D4 remain useful but no longer define the canonical operation architecture.
