# IF-ADR-034 — Cycle Reset Unity Participant Registration

Status: **Proposed**
Last updated: 2026-09-26
Related decisions: IF-ADR-001, IF-ADR-005, IF-ADR-010, IF-ADR-014

## Context

Cycle Reset (`Runtime/CycleReset`) already has a complete, Unity-free core:
`ICycleResetParticipant` → `CycleResetParticipantDescriptor` (id, scope,
requiredness, order) → `CycleResetPlan` (validation, duplicate-id blocking,
Route-then-Activity / order / source-index sort) → `CycleResetRuntime`
(execution, required/optional aggregation) → `CycleResetResult`.
Route/Activity triggers are bound through explicit runtime ports.

Participants reach a request only through `ICycleResetParticipantSource`,
held by `RouteLifecycleRuntime` and defaulting to
`EmptyCycleResetParticipantSource`. The injection chain
`FrameworkRuntimeHost.SetCycleResetParticipantSource` →
`GameFlowRuntime` → `RouteLifecycleRuntime` exists but has **no caller**, and
the host setter is not buffered (`_gameFlowRuntime?.`), unlike
`ActivityParticipantSourceBindings`. In practice every Cycle Reset resolves
zero participants (`SucceededNoParticipants`).

There is no Unity authoring surface that lets a GameObject explicitly become a
Cycle Reset participant, and no owner/lifetime model for such a registration.

## Decision

Add the smallest registration boundary that mirrors the existing Object Reset
composition pattern **without sharing any Object Reset type or registry**.

```text
UnityCycleResetParticipant (authored, Scene)
  -> ICycleResetRegistrationRuntimePort   (bound by composition, never looked up)
  -> CycleResetParticipantRegistry        (host-owned instance)
  -> ICycleResetParticipantSource         (existing boundary, now fed)
  -> CycleResetPlan / CycleResetRuntime   (unchanged)
```

### 1. Authoring component — `UnityCycleResetParticipant`

Public `MonoBehaviour`, ADR-010 Class A (Add Component → concise Inspector →
explicit validation → Play Mode evidence). Authored fields:

| Field | Rule |
|---|---|
| `participantId` | Authored stable string → `CycleResetParticipantId`. Required. Never generated from name, hierarchy or scene path; never regenerated on `OnValidate`/rename (ADR-014). |
| `scope` | `Route` or `Activity`. `Unknown` is invalid configuration. |
| `requiredness` | `Required` or `Optional`. `Unknown` is invalid configuration. |
| `order` | `int`, used as-is by `CycleResetPlan`. |
| `target` | Explicit serialized reference to a component implementing `IUnityCycleResettable`. No `GetComponent` discovery. |
| `displayName` | Diagnostics only. |

`IUnityCycleResettable` (public) is the consumer behavior contract:
`CycleResetParticipantResult ResetCycle(CycleResetContext context)`. The
component itself implements `ICycleResetParticipant`, building its descriptor
from authored fields and delegating execution to `target`.

It is not an `IResetParticipant`, not an `IUnityResettable`, and does not
attach to `UnityResetSubjectAdapter`.

### 2. Registration port — `ICycleResetRegistrationRuntimePort` (internal)

```text
TryResolveCurrentCycleResetOwner(CycleResetScope, out RuntimeContentOwner, out issue)
Register(ICycleResetParticipant, RuntimeContentOwner, UnityEngine.Object owner, source, reason) -> handle/result
Unregister(handle, UnityEngine.Object owner, source, reason) -> result
```

Implemented by `FrameworkRuntimeHost`, resolving the owner from the same
Route/Activity runtime scope owners used by `TryResolveCurrentResetOwner`
(including `RuntimeDefinitionToken`). Separate interface from
`IResetRegistrationRuntimePort`.

### 3. Registry — `CycleResetParticipantRegistry` (internal)

- One instance per `FrameworkRuntimeHost`; no static state.
- Stores `{handle, participant, descriptor snapshot, RuntimeContentOwner, Unity owner}`.
- Rejects registration of a duplicate `participantId` within the same current owner; `CycleResetPlan` keeps its own duplicate check as the final guard.
- Implements `ICycleResetParticipantSource`: returns only participants whose
  owner equals the **current** Route owner (Route scope) or current Activity
  owner (Activity scope) at resolve time. Scope acceptance stays in
  `CycleResetRequest.AcceptsParticipantScope`/`CycleResetPlan`.
- Entries whose owner is no longer current are never returned and are purged
  on unregister/scene release.

The host injects the registry once, through a buffered binding applied whenever
`GameFlowRuntime` is created, replacing the unbuffered `?.` setter.

### 4. Composition — `CycleResetBindingSceneLifecycleParticipant` (internal)

A new `ISceneLifecycleParticipant` registered in `SceneLifecycleRuntime`
beside — not inside — `ResetProductBindingSceneLifecycleParticipant`.

- `OnSceneAvailable(scene, roots)`: typed collection of
  `UnityCycleResetParticipant` under the explicit lifecycle-owned roots that
  Scene Lifecycle supplies (the same boundary already used for Reset adapters
  and Cycle Reset triggers); binds the port; each component registers itself.
- `OnSceneReleasing(scene, roots)`: unregisters every participant under those roots.
- Component `OnDisable`/`OnDestroy`: unregisters through its bound handle.

No `FindObjectsOfType`, no name/tag/path lookup, no static host access.

### 5. Editor — ADR-010

Class A Inspector: the five authored decisions + configuration status
(missing id, `Unknown` scope/requiredness, missing or wrong-type target) +
Advanced/Debug with read-only Play Mode evidence (registered, owner, handle).
No Wizard, Composer or Apply/Rebuild.

## Invariants

- Cycle Reset and Object Reset share no registry, port, participant interface or authoring component.
- Registration is explicit: a participant exists for Cycle Reset only after the composition binds its port and the component registers.
- Ownership is a `RuntimeContentOwner` for the exact Route/Activity occurrence; stable ID is identity, never lifetime authority.
- Invalid required configuration fails registration with a typed diagnostic; it never silently falls back.
- Cycle Reset still never reloads scenes, releases content or resets Object Reset subjects.

## Non-goals

Runtime-instantiated (prefab) participants, Persistent/Session scope,
UnityEvent-based participants, async participants, cross-scope ordering
changes, and any change to `CycleResetPlan`/`CycleResetRuntime`.

## Open points for the implementation cut

1. Activity-scene timing: confirm the Activity owner is resolvable at
   `OnSceneAvailable`; otherwise adopt the deferred-registration refresh
   already used by `ResetProductBindingSceneLifecycleParticipant`.
2. Handle type: reuse a Cycle-Reset-local handle struct (do not reuse `ResetRegistrationHandle`).
3. API status: component and `IUnityCycleResettable` start as Experimental (IF-GOV-001).

## Consequences

`RequestActivityCycleResetAsync` / `RequestRouteCycleResetAsync` start
resolving real participants with no change to request, plan, execution or
trigger surfaces. `EmptyCycleResetParticipantSource` remains the fallback.
