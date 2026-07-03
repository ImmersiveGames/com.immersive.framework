# F20 — Pause State/Gate Usage

> Status: `F20 CLOSED / F20F QA PASS + USAGE`  
> Scope: logical Pause core only.

F20 defines Pause as framework state plus Gate admission effects. It does not define Pause as an Activity, menu, overlay, input system or `Time.timeScale` contract.

---

## 1. What F20 provides

F20 adds the logical language and minimal runtime path for Pause:

```text
Runtime/Pause/PauseRequestId
Runtime/Pause/PauseState
Runtime/Pause/PauseRequestKind
Runtime/Pause/PauseRequestStatus
Runtime/Pause/PauseIssueSeverity
Runtime/Pause/PauseIssue
Runtime/Pause/PauseRequest
Runtime/Pause/PauseResult
Runtime/Pause/PauseSnapshot
Runtime/Pause/PauseGateBlockerPolicy
Runtime/Pause/PauseRuntime
```

Runtime host integration:

```text
FrameworkRuntimeHost.RequestPause(...)
FrameworkRuntimeHost.PauseSnapshot
FrameworkRuntimeHost.PauseGateSnapshot
```

Diagnostics:

```text
Show Pause diagnostics
  Run Pause Diagnostics Smoke
  Run Pause Gate Blocker Smoke
  Run Pause Runtime Request Smoke
```

---

## 2. What F20 does not provide

F20 intentionally does not create:

```text
Pause menu
Pause overlay
pause input binding
Canvas or prefab setup
ScriptableObject authoring
Time.timeScale adapter
Gate runtime registry
Route/Activity lifecycle ownership
Pause as Activity
service locator
```

Pause content, overlay and input belong to F23. F21 closed Save / Snapshot / Preferences / Progression Save, and F22 is Loading Operation / Progress / Readiness.

---

## 3. Current runtime behavior

The minimal request path is in-memory only:

```text
FrameworkRuntimeHost.RequestPause(...)
  -> PauseRuntime.Request(PauseRequest)
  -> PauseResult
  -> PauseSnapshot
  -> PauseGateBlockerPolicy-derived GateSnapshot
```

When state is `Paused`, F20 derives passive Gate blockers for:

```text
Gameplay / GameplayAction
Interaction / InteractionAcceptance
```

Pause requests remain allowed:

```text
Pause / PauseRequest
```

When state is `Running`, those Pause-derived blockers are absent.

---

## 4. Current public usage boundary

F20 does not yet expose a user-facing scene trigger or input binding. Current usage is framework-internal and QA-facing.

The intended direction is:

```text
F20: logical request/result/state/snapshot/Gate relationship
F23: input, overlay and content adapters that call into F20
```

So for now, do not add arbitrary scene buttons or UI scripts that bypass the planned F21 boundary unless the cut explicitly requests that for QA.

---

## 5. Internal code shape

The current request shape is:

```csharp
var result = runtimeHost.RequestPause(
    PauseRequest.Pause(
        "my.pause.request",
        "MySource",
        "my.reason"));
```

Resume:

```csharp
var result = runtimeHost.RequestPause(
    PauseRequest.Resume(
        "my.resume.request",
        "MySource",
        "my.reason"));
```

Toggle:

```csharp
var result = runtimeHost.RequestPause(
    PauseRequest.Toggle(
        "my.toggle.request",
        "MySource",
        "my.reason"));
```

This is intentionally not documented as scene-authoring API yet. F21 should decide the proper authoring component, input binding and overlay flow.

---

## 6. QA usage

Open the QA Canvas and expand:

```text
Show Pause diagnostics
```

Run these smokes in order when validating F20:

```text
Run Pause Diagnostics Smoke
Run Pause Gate Blocker Smoke
Run Pause Runtime Request Smoke
```

Recommended regression smoke:

```text
Run Standard Smoke
```

---

## 7. Expected smoke evidence

### Pause Diagnostics Smoke

Expected steps:

```text
request
pause-applied-result
resume-applied-result
toggle-target-state
ignored-no-change-result
rejected-result
snapshot
```

This validates passive request/result/snapshot shapes only.

### Pause Gate Blocker Smoke

Expected steps:

```text
paused-blockers-created
paused-blocks-gameplay-action
paused-blocks-interaction-acceptance
pause-request-remains-allowed
running-releases-blockers
rejected-resume-keeps-blockers
```

This validates the passive Pause-to-Gate relationship.

### Pause Runtime Request Smoke

Expected steps:

```text
ensure-running
pause-request-applied
paused-gate-blocks-gameplay
toggle-request-resumes
resume-no-change
snapshot-running
```

This validates the real in-memory request path and intentionally leaves the runtime in `Running`.

---

## 8. Manual setup

None for F20.

Do not create these for F20 validation:

```text
scene object
Canvas
pause menu prefab
ScriptableObject profile
input binding
Time.timeScale adapter
```

F21 will define the setup for Pause content, overlay and input.

---

## 9. Common mistakes

### Mistake: Treat Pause as an Activity

Do not model Pause as Activity. Pause is a cross-cutting state that gates gameplay/interaction while preserving controlled escape paths.

### Mistake: Make `Time.timeScale` the Pause contract

Do not use `Time.timeScale` as the framework contract. A future time-scale adapter may exist, but the framework contract is Pause state, request/result, snapshot and Gate relationship.

### Mistake: Let Pause own Route/Activity lifecycle

Pause does not enter/exit routes or activities. It may gate requests later, but it does not become a lifecycle owner.

### Mistake: Add a global Pause singleton

The current owner is framework runtime state through `PauseRuntime`. Do not add a parallel service locator/global singleton.

---

## 10. Handoff to F21

F21 should add the consumer layer:

```text
Pause content/overlay boundary
pause input boundary
optional scene/Canvas setup
possibly authoring components or profiles
manual setup guide when a scene object becomes required
```

F21 must consume F20. It must not redefine Pause as Activity or replace the F20 request/result/state model.
