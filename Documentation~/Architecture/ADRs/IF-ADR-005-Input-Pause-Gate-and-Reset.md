# IF-ADR-005 — Input, Pause, Gate and Reset

Status: Accepted
Last updated: 2026-07-23
Supersedes: Input/Pause F10/F20/F23/F34–F38 and Reset F11–F16/F39–F43 fragments
Superseded by: none

## Context

Input posture, Pause state, capability admission and gameplay reset interact,
but they must not become one authority. Unity side effects need one explicit
writer and resettable objects need explicit registration rather than host
discovery.

## Decision

`InputModeRuntimeContext` owns one scoped logical input posture and its
transaction evidence. `UnityPlayerInputStateWriter` is the package-owned
physical action-map writer, reached through the explicit
`UnityPlayerInputGateAdapter`.

The Pause product path is:

```text
PausePlayerInputBinding
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
- Capability Gate and transition gate policies.
- Explicit Reset registration, selection, execution and Unity participants.
- Separate Route/Activity Cycle Reset and composed Activity Restart.

## Rejected scope

- Parallel Pause bridge, direct secondary submitter or compatibility alias.
- Action-map name as identity fallback.
- Static host lookup, service locator or scene-wide discovery for Reset.
- Treating Reset, Release, Snapshot and Save as synonyms.
- Cycle Reset silently reloading scenes or mutating gameplay objects.

## Consequences

Logical state commits only after physical application succeeds; rollback retains
exact evidence. Reset authoring can be reused by gameplay without coupling
object state to framework lifecycle identity.

## Current implementation coverage

The canonical Pause/InputMode path, Gate adapter, explicit Reset ports,
ResetRegistry/Executor, Unity participants, Object Reset triggers, Cycle Reset
and Activity Restart exist. The obsolete Pause/InputMode bridges and static host
authority are absent.

## Pending decisions

- Authorable interactive Pause UI contract before adding a `Global + UI` posture.
- Whether the legacy `RegisterWithCurrentHost` method name should be renamed in a
  future public API migration.
