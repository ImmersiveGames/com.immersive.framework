# F40 ADR RESET 007 — Activity Restart via Object Reset Group

Status: Accepted / preview.11

## Context

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
