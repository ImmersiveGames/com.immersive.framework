# F41 ADR RESET 008 — Reset Selection Policy

Status: Accepted / preview.11 corrective patch

## Context

The first `ActivityRestartTrigger` implementation depended on an authored `ObjectResetGroupTrigger`. That worked for the smoke, but it made authoring misleading: a restart button needed two triggers, and the framework effectively composed one public adapter through another public adapter.

The reset process is common. What changes is the target selection policy:

- explicit ObjectEntry targets by id/declaration;
- reusable group asset targets;
- current Activity scoped ObjectEntries;
- current Route scoped ObjectEntries;
- current Route + Activity scoped ObjectEntries;
- all ObjectEntries in the current runtime snapshot.

## Decision

Introduce `ObjectResetSelectionMode` and an internal `ObjectResetGroupExecutor` as the common reset execution path.

`ObjectResetGroupTrigger` remains the standalone authoring surface for a pure multi-object reset button, but it delegates execution to the common executor.

`ActivityRestartTrigger` no longer requires or references an `ObjectResetGroupTrigger`. It owns reset selection policy directly and then composes:

1. common Object Reset Group execution;
2. Activity Clear;
3. Activity Re-enter.

## Selection modes

```text
ExplicitTargets
CurrentActivityEntries
CurrentRouteEntries
CurrentRouteAndActivityEntries
AllCurrentEntries
```

`ExplicitTargets` may use inline entries or an `ObjectResetGroupAsset`. Scoped selection uses the current `ObjectEntryRuntimeContextSnapshot`; it does not perform scene search, participant discovery or fallback loading.

## Rationale

This keeps reset authoring deterministic while avoiding adapter stacking. A trigger is now a request surface; target choice is policy; reset execution is shared.

## Non-goals

- No full Cycle Reset expansion.
- No automatic participant discovery.
- No Player/Actor lifecycle ownership.
- No Route restart.
- No scene reload primitive.
- No compatibility wrapper for the earlier `ActivityRestartTrigger -> ObjectResetGroupTrigger` shape.

## Expected authoring

For a standalone room reset:

```text
Button_ResetRoom
  ObjectResetGroupTrigger
```

For Activity restart:

```text
Button_RestartActivity
  ActivityRestartTrigger
    Reset Selection Mode = ExplicitTargets / CurrentActivityEntries / CurrentRouteAndActivityEntries
```

The button should call only:

```text
ActivityRestartTrigger.RequestActivityRestart()
```
