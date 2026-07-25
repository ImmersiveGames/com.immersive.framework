# IF-ADR-005 — Input, Pause, Gate and Reset

Status: Accepted
Last updated: 2026-07-25
Superseded by: none

## Context

Input posture, logical Pause, capability admission and gameplay reset interact,
but they must not become one authority. Unity side effects require explicit
writers and authored request surfaces must not locate runtime services.

Pause requests originate from two materially different surfaces:

```text
physical Player input
authored UI / UnityEvent request
```

Requiring a Player merely to let an authored button pause the application
couples logical Pause to physical input availability.

## Decision

`PauseRuntime` remains the application authority for logical Running/Paused
state. `FrameworkRuntimeHost` remains the application port that applies logical
Pause, `Time.timeScale` and Pause presentation.

`PauseProductBindingRuntimeContext` supports two explicit execution modes.

### PlayerInput transaction

When one `PausePlayerInputBinding` is active:

```text
Pause request
  -> logical Pause
  -> InputMode transaction
  -> UnityPlayerInputGateAdapter
  -> exact action-map posture
```

The result is:

```text
PauseProductRequestStatus.Applied
executionMode = PlayerInputTransaction
```

### Application-only request

When the runtime is cleanly unbound and no Player binding evidence exists:

```text
PauseRequestTrigger
  -> logical Pause
  -> TimeScale
  -> Pause Surface
```

No action map is created, resolved or modified. The result is:

```text
PauseProductRequestStatus.AppliedWithoutPlayerInput
executionMode = ApplicationOnly
```

This is an explicit supported mode, not a fallback. A failed, partial or stale
Player binding state does not degrade to application-only execution; it is
rejected as `BindingUnavailable`.

`PauseRequestTrigger` logs every request outcome through `FrameworkLogger` and
`com.immersive.logging`, including product status, execution mode, previous and
current Pause states and diagnostic.

Authored Trigger injection remains scoped:

```text
Persistent Content
  bound during boot

Route / Activity
  bound from exact SceneLifecycle roots
  released before unload with the exact expected port
```

Physical Escape/Gamepad Pause still requires an officially admitted Player with
`PlayerInput`, `UnityPlayerInputGateAdapter` and `PausePlayerInputBinding`.

## Rejected alternatives

```text
creating a fake/global Player only for Escape
PauseRequestTrigger searching for FrameworkRuntimeHost
singleton or service locator
silently ignoring missing PlayerInput
silently treating failed Player binding as application-only
modifying action maps when no Player binding exists
```

## Consequences

Menus, title screens, accessibility overlays and gameplay scenes may expose
Pause buttons without manufacturing a Player. Games that have an official Player
retain the stronger Pause/InputMode transaction.

Diagnostics now distinguish:

```text
Applied
AppliedWithoutPlayerInput
Ignored
BindingUnavailable
Rejected
Failed
```

## Pending decisions

- Authorable `Global + UI` posture when an official Player exists.
- Multiplayer Pause authority and per-Player request policy.
- Binding an official Player while the application is already paused by an
  application-only request.
