# IF-ADR-005 — Input, Pause, Gate and Reset

Status: Accepted
Last updated: 2026-07-25
Supersedes: Input/Pause F10/F20/F23/F34–F38 and Reset F11–F16/F39–F43 fragments
Superseded by: none

## Context

Input posture, Pause state, capability admission and gameplay reset interact,
but they must not become one authority. Unity side effects need one explicit
writer and resettable objects need explicit registration rather than host
discovery.

Pause authoring may live in Persistent Content, Route scenes or Activity scenes.
Those authored components must not locate `FrameworkRuntimeHost`, a singleton or
a service container.

## Decision

`InputModeRuntimeContext` owns one scoped logical input posture and its
transaction evidence. `UnityPlayerInputStateWriter` is the package-owned
physical action-map writer, reached through the explicit
`UnityPlayerInputGateAdapter`.

The physical Pause input path is:

```text
officially admitted Local Player
-> PlayerInput
-> PausePlayerInputBinding
-> session-owned PauseProductBindingRuntimeContext
-> InputMode transaction
-> UnityPlayerInputGateAdapter
-> UnityPlayerInputStateWriter
```

Running enables exactly `Global + configured gameplay action map`; the default
gameplay map is `Player`. Paused enables exactly `Global`. Pause action
resolution uses the configured action reference/GUID, not a name fallback.
Lifecycle release restores the original PlayerInput posture and releases the
scoped context.

The authored request path is:

```text
PauseRequestTrigger
-> injected IPauseProductRequestPort
-> PauseProductBindingRuntimeContext
-> logical Pause + InputMode transaction
```

`PauseRequestTrigger` never searches for runtime authority. Injection is owned
by explicit composition roots:

```text
Persistent Content boot
  GlobalUiSceneRuntime binds triggers from retained persistent roots.

Route / Activity scene availability
  SceneLifecycleRuntime supplies exact loaded-scene roots.
  PauseProductBindingSceneLifecycleParticipant binds triggers in those roots.

Route / Activity scene release
  the same participant releases the exact bound port before unload.
```

Repeated scene-available notifications are idempotent. Release requires the
exact expected request port and rejects foreign or stale ports. A failed scene
composition rolls back only bindings created by that composition attempt.

An authored button does not require the user to press a Player input action, but
the current Pause product still requires one active official
`PausePlayerInputBinding`. Without it, the request port may be injected while
the product request reports `BindingUnavailable` because no PlayerInput posture
can be transacted.

Gate is capability admission, not Pause. Typed blockers may suppress lifecycle
requests, input, interaction or gameplay according to explicit policy.
Transition Gate never changes `Time.timeScale`.

Object Reset and Cycle Reset are distinct:

```text
Object Reset
  ResetRegistry + ResetExecutor + ResetSubject + participants
  restores authored/runtime gameplay object state

Cycle Reset
  Route/Activity lifecycle participants
  does not imply object, scene, Player, pool or save reset

Activity Restart
  composes Object Reset with Activity clear/re-enter
```

`UnityResetSubjectAdapter` receives an explicit
`IResetRegistrationRuntimePort`. Its public method name
`RegisterWithCurrentHost` is legacy wording; implementation uses the bound port
and does not perform static host lookup. Required participant failures are
explicit, ordered and diagnostic.

## Accepted scope

- Typed InputMode requests and exact action-map posture.
- One physical PlayerInput writer.
- Session-owned Pause runtime with scene/Activity authoring bindings.
- Automatic scoped `PauseRequestTrigger` binding for Persistent Content, Route
  scenes and Activity scenes.
- Exact request-port release before Route/Activity scene unload.
- Capability Gate and transition gate policies.
- Explicit Reset registration, selection, execution and Unity participants.
- Separate Route/Activity Cycle Reset and composed Activity Restart.

## Rejected scope

- `PauseRequestTrigger` searching for `FrameworkRuntimeHost`.
- Singleton, service locator or general DI container for Pause.
- Parallel Pause bridge, direct secondary submitter or compatibility alias.
- Action-map name as identity fallback.
- Static host lookup, service locator or scene-wide discovery for Reset.
- Treating Reset, Release, Snapshot and Save as synonyms.
- Cycle Reset silently reloading scenes or mutating gameplay objects.

## Consequences

Logical state commits only after physical application succeeds; rollback retains
exact evidence. Route and Activity authors may place Pause buttons in their own
scenes without manual runtime references. Their triggers remain scoped to the
scene lifetime and cannot retain a stale request port after release.

Reset authoring can be reused by gameplay without coupling object state to
framework lifecycle identity.

## Current implementation coverage

The canonical Pause/InputMode path, automatic Persistent/Route/Activity request
trigger binding, exact scene release, Gate adapter, explicit Reset ports,
ResetRegistry/Executor, Unity participants, Object Reset triggers, Cycle Reset
and Activity Restart exist. The obsolete Pause/InputMode bridges and static host
authority are absent.

## Pending decisions

- Authorable interactive Pause UI contract before adding a `Global + UI` posture.
- Whether direct logical Pause without an active official PlayerInput binding
  should become a separate supported product mode.
- Whether the legacy `RegisterWithCurrentHost` method name should be renamed in a
  future public API migration.
