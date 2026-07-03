# F43 ADR RESET 010 — Reset/Restart Authoring Validation

Status: Accepted / preview.11

## Context

Preview.11 introduced three related authoring surfaces:

```text
ObjectResetTrigger
ObjectResetGroupTrigger
ActivityRestartTrigger
```

The runtime behavior is now correct, but designer configuration can still become ambiguous. Examples include a restart button that also calls a reset group trigger, `ExplicitTargets` with no entries, or scoped reset policies combined with explicit entries that will be ignored.

## Decision

Add open-scene validation for reset/restart authoring. The validator scans loaded scenes for:

```text
ObjectResetGroupTrigger
ActivityRestartTrigger
```

It reports:

- Object Reset Group with no Group Asset and no inline entries.
- Object Reset Group Asset with no targets.
- Activity Restart with no target Activity and no current-activity fallback.
- Activity Restart `ExplicitTargets` with no targets.
- Activity Restart scoped selection modes combined with explicit entries or group assets.
- Activity Restart GameObjects stacked with Object Reset triggers, because a restart button should call only `ActivityRestartTrigger.RequestActivityRestart()`.

## Non-goals

- No runtime execution during validation.
- No UI Button OnClick introspection in this cut.
- No automatic scene search for participants.
- No Cycle Reset validation expansion.

## Rationale

Reset/restart now has a common execution model. The validator preserves that model at authoring time by detecting the most common misconfigurations before smoke.

## Expected FIRSTGAME shape

```text
Button_ResetPlayer
  ObjectResetTrigger

Button_ResetRoom
  ObjectResetGroupTrigger

Button_RestartActivity
  ActivityRestartTrigger
```

The restart button should call only:

```text
ActivityRestartTrigger.RequestActivityRestart()
```
