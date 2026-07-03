# F22 — Loading Operation / Progress / Readiness Usage

Status: `CLOSED / F22F QA PASS + USAGE`  
Package: `com.immersive.framework`  
Scope: Loading operation/progress/readiness boundary.

---

## 1. What F22 provides

F22 creates the canonical Loading module:

```text
Runtime/Loading/
Immersive.Framework.Loading
```

Use it when a framework system or adapter needs to describe:

```text
an operation that is loading or preparing something
one or more loading steps
weighted progress across steps
observed SceneLifecycle / Transition loading-like work
presentation data for a future loading screen adapter
```

The closed F22 surface includes:

```text
LoadingOperationId
LoadingStepId
LoadingOperationStatus
LoadingStepStatus
LoadingProgress
LoadingStepWeight
LoadingWeightedProgress
LoadingStep
LoadingOperation
LoadingProgressAggregationStatus
LoadingProgressAggregationResult
LoadingProgressAggregator
LoadingObservationAdapter
ILoadingScreenAdapter
LoadingScreenPresentation
LoadingScreenAdapterAction
LoadingScreenAdapterStatus
LoadingScreenAdapterResult
```

---

## 2. What F22 does not provide

F22 is not a loading screen implementation.

F22 does not create:

```text
Canvas
prefab
scene object
ScriptableObject
fade execution
curtain execution
TransitionEffect execution
SceneLifecycle replacement
Transition replacement
readiness mutation
Addressables loader
save backend
PlayerPrefs usage
JSON usage
```

Loading is the canonical reporting/adapter boundary. Concrete loading UI remains a consumer that implements `ILoadingScreenAdapter`.

---

## 3. Boundary map

| Area | Owner | Relationship to F22 |
|---|---|---|
| Scene loading/unloading | `SceneLifecycle` | F22 may observe result/progress; it does not execute the scene operation. |
| Route scene composition | Route scene composition/release | F22 may report progress; it does not own Route content lifecycle. |
| Transition flow | `Transition` | F22 may observe transition steps; it does not orchestrate transitions. |
| Fade/curtain | `TransitionEffects` | Remains visual effect adapter work. |
| Loading screen UI | Future UI adapter | Consumes `LoadingOperation` through `ILoadingScreenAdapter`. |
| Save loading | `ProgressionSave` | Persistence load is not a LoadingOperation by itself. |
| Pause overlay/input | F23 | Not part of F22. |

---

## 4. Creating a loading operation

Use `LoadingOperation` for a single high-level operation state.

```csharp
using Immersive.Framework.Loading;

var operation = LoadingOperation.Running(
    "route.startup.loading",
    0.42f,
    "Startup Route Loading",
    "Example",
    "Preparing route content.");
```

Rules:

```text
operation id must be stable and logical
operation progress is normalized from 0 to 1
operation does not execute the load
operation does not imply UI visibility
```

---

## 5. Creating weighted steps

Use `LoadingStep` when one operation has multiple measurable parts.

```csharp
using Immersive.Framework.Loading;

var sceneStep = LoadingStep.Running(
    "route.startup.scene",
    3f,
    0.5f,
    "Startup Scene",
    "Example",
    "Scene load is halfway observed.");

var readinessStep = LoadingStep.Completed(
    "route.startup.readiness",
    1f,
    "Readiness",
    "Example",
    "Content readiness completed.");
```

The `weight` is relative. In this example, scene loading contributes three times more than readiness to the aggregate progress.

---

## 6. Aggregating weighted progress

Use `LoadingProgressAggregator` to combine passive steps into one aggregate result.

```csharp
using Immersive.Framework.Loading;

var operationId = LoadingOperationId.From("route.startup.loading");
var steps = new[]
{
    LoadingStep.Running("route.startup.scene", 3f, 0.5f, "Startup Scene", "Example", "Loading."),
    LoadingStep.Completed("route.startup.readiness", 1f, "Readiness", "Example", "Ready.")
};

var aggregate = LoadingProgressAggregator.Aggregate(
    operationId,
    steps,
    "Example",
    "Manual aggregation example",
    "Aggregated observed loading steps.");

var operation = new LoadingOperation(
    operationId,
    LoadingOperationStatus.Running,
    aggregate.Progress,
    "Startup Route Loading",
    "Example",
    aggregate.Message);
```

The aggregator is pure. It does not schedule work, load scenes, run transitions, show UI or mutate readiness.

---

## 7. Observing SceneLifecycle and Transition

F22D adds an internal observation adapter that maps existing diagnostics into Loading records.

Conceptually:

```text
SceneLifecycleLoadResult -> LoadingStep
SceneLifecycleUnloadResult -> LoadingStep
TransitionStep -> LoadingStep
TransitionResult -> LoadingProgressAggregationResult
LoadingProgressAggregationResult -> LoadingOperation
```

The important rule is that observation is not ownership.

```text
SceneLifecycle still executes Unity scene load/unload.
Transition still owns flow orchestration.
Loading only reports operation/progress/readiness-facing data.
```

---

## 8. Implementing a loading screen adapter

A concrete loading screen should implement `ILoadingScreenAdapter` and consume canonical `LoadingScreenPresentation` data.

Minimal shape:

```csharp
using System;
using Immersive.Framework.Loading;

public sealed class MyLoadingScreenAdapter : ILoadingScreenAdapter
{
    public string AdapterName => "MyLoadingScreenAdapter";

    public bool Supports(LoadingOperation operation)
    {
        return operation.IsValid;
    }

    public LoadingScreenAdapterResult Show(LoadingScreenPresentation presentation)
    {
        if (!Supports(presentation.Operation))
        {
            return LoadingScreenAdapterResult.RejectedResult(
                presentation,
                LoadingScreenAdapterAction.Show,
                AdapterName,
                "Unsupported loading operation.",
                new[] { "Operation is invalid." });
        }

        // Drive concrete UI here in a future UI module.
        return LoadingScreenAdapterResult.SucceededResult(
            presentation,
            LoadingScreenAdapterAction.Show,
            AdapterName,
            "Shown.");
    }

    public LoadingScreenAdapterResult Update(LoadingScreenPresentation presentation)
    {
        // Update progress text/bar here in a future UI module.
        return LoadingScreenAdapterResult.SucceededResult(
            presentation,
            LoadingScreenAdapterAction.Update,
            AdapterName,
            "Updated.");
    }

    public LoadingScreenAdapterResult Hide(LoadingScreenPresentation presentation)
    {
        // Hide concrete UI here in a future UI module.
        return LoadingScreenAdapterResult.SucceededResult(
            presentation,
            LoadingScreenAdapterAction.Hide,
            AdapterName,
            "Hidden.");
    }
}
```

This adapter must not call `SceneLifecycle`, run a `TransitionEffect`, decide readiness or become a global manager.

---

## 9. Building presentation data

A presentation wraps canonical Loading data for a visual adapter.

```csharp
using Immersive.Framework.Loading;

var operation = LoadingOperation.Running(
    "route.startup.loading",
    0.65f,
    "Startup Route Loading",
    "Example",
    "Loading route content.");

var presentation = LoadingScreenPresentation.FromOperation(
    operation,
    shouldBeVisible: true,
    title: "Loading",
    detail: "Preparing route content...",
    source: "Example");
```

The presentation is display data. It is not a prefab, Canvas, transition effect or lifecycle request.

---

## 10. QA smokes closed in F22

Run these from the QA Canvas under `Show Loading diagnostics`:

```text
Run Loading Progress Aggregation Smoke
Run Loading Observation Adapter Smoke
Run Loading Screen Adapter Boundary Smoke
Run Loading Readiness Observation Smoke
Run Loading Result and Issue Smoke
```

Expected validated boundaries:

```text
Loading Progress Aggregation Smoke
- contracts
- weighted-running
- completed-with-skipped
- failed-aggregation
- no-steps
- canonical-boundary

Loading Observation Adapter Smoke
- contracts
- scene-lifecycle-observation
- scene-lifecycle-failure-observation
- transition-observation
- transition-failure-observation
- canonical-boundary

Loading Screen Adapter Boundary Smoke
- contracts
- show-update-hide
- unsupported-operation
- adapter-failure
- canonical-boundary

Loading Readiness Observation Smoke
- contracts
- waiting-observation
- ready-observation
- blocked-observation
- failed-observation
- canonical-boundary

Loading Result and Issue Smoke
- contracts
- success-result
- waiting-readiness-result
- failure-issue-result
- canonical-boundary
```

---

## 11. Designer-facing quick explanation

Think of F22 as the loading meter language.

It answers:

```text
What is loading?
Which step is active?
How much does each step count?
What is the current progress?
Can a UI adapter display this information?
```

It does not answer:

```text
Which prefab is the loading screen?
Which animation plays?
Which button pauses the game?
Which scene gets loaded?
Which save slot is read?
```

Those are separate adapters or later phases.

---

## 12. Screenshot placeholders

Add screenshots manually when building the designer manual:

```text
[SCREENSHOT PLACEHOLDER: QA Canvas with Show Loading diagnostics expanded]
[SCREENSHOT PLACEHOLDER: Loading Progress Aggregation Smoke PASS logs]
[SCREENSHOT PLACEHOLDER: Loading Observation Adapter Smoke PASS logs]
[SCREENSHOT PLACEHOLDER: Loading Screen Adapter Boundary Smoke PASS logs]
[SCREENSHOT PLACEHOLDER: Loading Readiness Observation Smoke PASS logs]
[SCREENSHOT PLACEHOLDER: Loading Result and Issue Smoke PASS logs]
[SCREENSHOT PLACEHOLDER: future loading screen UI adapter inspector, when F24 creates one]
```

---

## 13. Next phase

Next planned cut:

```text
IF-FW-F23B — Pause Content Anchor Consumer Contracts
```

F23 should consume the existing Pause core from F20, the Loading boundary from F22 and the previous Transition/Effect boundaries without turning Pause UI into a lifecycle owner.
