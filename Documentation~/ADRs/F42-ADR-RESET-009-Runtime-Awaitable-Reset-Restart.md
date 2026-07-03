# F42 ADR RESET 009 — Runtime Awaitable Reset/Restart Flow

Status: Accepted / preview.11

## Context

Unity 6.5 provides `UnityEngine.Awaitable` for Unity-bound async orchestration. The reset/restart flow added in preview.11 runs on Unity runtime surfaces: scene-authored triggers, framework runtime host, GameFlow, Transition Gate and Object Reset participants.

The first implementation used `Task` for convenience. That worked, but it did not match the framework direction for Unity runtime orchestration.

## Decision

Use `UnityEngine.Awaitable<T>` for the new reset/restart runtime flow where execution is tied to Unity runtime/main thread.

Targeted methods include the new authored Object Reset Group and Activity Restart paths:

```text
ObjectResetGroupTrigger.RequestObjectResetGroupAsync
ObjectResetGroupExecutor.ExecuteAsync
ActivityRestartTrigger.RequestActivityRestartAsync
FrameworkRuntimeHost.RestartActivityAsync
GameFlowRuntime.RestartActivityAsync
```

Button/Inspector entry points remain `async void` wrappers because Unity UI events and context menu methods do not consume return values.

## Non-goals

- No migration of older route/activity/cycle flow APIs.
- No save/IO policy.
- No coroutine replacement.
- No `Task.Delay`.
- No behavior change to reset, restart, Transition Gate or PlayerInput Gate.

## Rationale

`Awaitable<T>` better communicates that these flows are Unity runtime operations. It keeps the new preview.11 reset/restart path aligned with the technical target without forcing a broad async migration across the whole package.

## Constraint

Do not cache or await the same Awaitable instance multiple times. Awaitable operations should be linear: create operation, await once, compose the result, return.

## Expected smoke

```text
Object Reset Request completed. status='Succeeded'
Object Reset Group Request completed. status='Succeeded'
Activity Restart Request completed. status='Succeeded' resetStatus='Succeeded' clearStatus='Succeeded' reenterStatus='Succeeded'
```
