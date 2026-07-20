# 00 - Current State

Status: **H2.4 closed and Unity-validated**
Last reconciled: **2026-07-20**
Version: **1.0.0-preview.16**

For the operational validation gate, read `05-Execution-Status.md`.

## H2 runtime authority boundary

```text
GameApplication bootstrap
-> FrameworkRuntimeHost.Create
-> host-owned scoped runtimes
-> explicit narrow runtime ports
-> authoring and Unity adapter bindings
```

`FrameworkRuntimeHost` remains the application/session composition root. It
owns composition and scoped runtime lifetime, but it is not a global registry,
service locator or feature manager.

The factory is stateless: the package has no `FrameworkRuntimeHost._current`
field and no `FrameworkRuntimeHost.TryGetCurrent` API. Production code must
receive its required runtime port from an explicit binding or composition path.

## Explicit binding coverage

H2 uses narrow ports for the following Unity-facing boundaries:

```text
Pause input mode
Route and Activity requests
Route and Activity cycle reset
Activity restart
Reset execution, selection and registration
Input gate
Content Anchor materialization
Player Actor selection
Runtime diagnostics
```

Each binding reports an explicit failure when its required port is unavailable;
there is no fallback discovery through a static host.

## QA boundary

QAFramework may resolve a host only in its friend-assembly harness:

```text
loaded FrameworkRuntimeHost components
-> loaded valid Unity scenes
-> reference deduplication
-> exactly one candidate required
```

This is test-harness infrastructure only. It is not public package API, a
product service locator or a runtime fallback.

## Validation state

H2.4 source is delivered in the package and QA repositories. The approved
Unity evidence covers import, compile and the focused Play Mode smoke:

```text
[H24_STATIC_HOST_AUTHORITY_REMOVAL_SMOKE]
status='Passed'
cases='10'
```

No post-H2 implementation lane is selected yet.
