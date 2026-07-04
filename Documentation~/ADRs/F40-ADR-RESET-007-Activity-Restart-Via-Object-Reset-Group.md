# F40 ADR RESET 007 — Activity Restart via Object Reset Group

Status: Amended by preview.12G / current trigger uses ResetSelectionConfig + ResetExecutor


> Amendment note — preview.12G: Activity Restart no longer runs through any Object Reset Group adapter/executor. The current canonical implementation is `ActivityRestartTrigger` + `ResetSelectionConfig` + `ResetExecutor`, followed by `FrameworkRuntimeHost.RestartActivityAsync(...)` for clear + re-enter in one transition window.

## Historical context

After `ObjectResetTrigger` and `ObjectResetGroupTrigger`, the framework has enough object-level reset tooling to support a small authored restart flow without introducing full Cycle Reset, PlayerActor lifecycle or scene reload semantics.

## Decision

Add `ActivityRestartTrigger` as an explicit Unity authoring surface. The trigger composes two layers:

1. Execute Object Reset through the common reset selection/executor path.
2. Execute Activity Clear + Activity Re-enter through a composite restart flow.

The composite restart flow opens one Activity transition, clears the current Activity, re-enters the same Activity, and closes that same transition. It must not execute Clear and Re-enter as two independent visual fades.

The original implementation referenced an `ObjectResetGroupTrigger`; this was corrected by F41. The trigger now owns reset selection policy directly. By default, the target Activity must be the current active Activity.

## Non-goals

- No Cycle Reset expansion.
- No Route restart.
- No scene reload primitive.
- No Player/Actor movement or spawn ownership.
- No automatic discovery of reset participants.

## Rationale

This gives designers a useful room/encounter restart button while preserving the current framework ownership model: objects reset through Object Reset participants, Activity lifecycle remains the lifecycle boundary, target selection is explicit policy, and future Cycle Reset can still be designed separately. The visual transition is intentionally composed as one window so restart does not look like two sequential Activity changes.

## Expected log

```text
Activity Restart Request completed.
status='Succeeded'
resetStatus='Succeeded'
clearStatus='Succeeded'
reenterStatus='Succeeded'
```


## Preview.11 follow-up decisions

- F41 corrected the authoring shape so `ActivityRestartTrigger` owns Reset Selection Policy directly and does not depend on `ObjectResetGroupTrigger`.
- F42 migrated the new Unity runtime reset/restart orchestration to `UnityEngine.Awaitable<T>`.
- F43 added open-scene authoring validation for reset/restart trigger configuration.
