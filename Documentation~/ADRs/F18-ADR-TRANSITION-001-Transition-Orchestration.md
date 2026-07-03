# F18-ADR-TRANSITION-001 - Transition Orchestration

Status: Accepted / Closed F18F  
Phase: F18 - Transition Orchestration Foundation  
Type: Framework Core / Flow Orchestration / Boundary  
Last updated: 2026-06-26

---

## 1. Context

F17 closed Gate Foundation. The framework now has a canonical admission language for allowed/blocked decisions, blockers, facts and diagnostics. F18 can therefore define Transition as flow orchestration that consumes Gate instead of inventing a parallel blocking model.

Transition exists because route/activity/content changes need an explicit operation boundary around request admission, previous-scope exit, release, scene/content movement, readiness observation, next-scope enter and diagnostics.

Transition must not be reduced to fade, loading screen or curtain visuals.

---

## 2. Decision

Transition belongs to Framework Core as orchestration.

Transition coordinates:

```text
Gate admission/block/release
Scene Lifecycle operations
content release results
runtime scope enter/exit results
readiness observations
lifecycle callback ordering
transition facts/diagnostics
```

Transition is not:

```text
fade effect
loading screen
curtain
UI widget
scene loader replacement
route lifecycle replacement
activity lifecycle replacement
parallel lifecycle
global manager
service locator
```

Fade/loading/curtain effects are adapters planned after the orchestration contract, in F19.

---

## 3. Orchestration Boundary

Transition describes the logical flow around a change:

```text
request admitted by Gate
transition operation opened
relevant gameplay/interaction admission blocked by Gate
previous scope exit observed
owned content release observed
scene/content load or composition observed
next scope enter observed
readiness observed
transition operation completed or failed
Gate blockers released for allowed scopes
facts emitted for diagnostics and QA
```

Transition owns the operation narrative. It does not own the underlying scene, route, activity or content systems.

---

## 4. Relationship With Gate

Transition consumes Gate. It does not replace Gate.

Gate remains the admission authority. Transition may require Gate blockers while an operation is active, but it must not invent an independent blocker model.

Required transition blockers must be explicit and observable as Gate blockers/facts.

---

## 5. Relationship With Scene Lifecycle

Transition may coordinate Scene Lifecycle, but it must not create a second Scene Lifecycle runtime.

Scene Lifecycle remains owner of scene loading/unloading semantics. Transition owns flow orchestration around those operations.

The same rule applies to Route Lifecycle, Activity Flow, RuntimeContent, ContentAnchor and Object Reset. Transition coordinates their outputs; it does not absorb them.

---

## 6. Minimal F18 Runtime Shape

F18 should introduce a small logical contract before any visual effect:

```text
TransitionOperationId
TransitionKind
TransitionPhase
TransitionStatus
TransitionStep
TransitionPlan
TransitionResult
TransitionSnapshot or diagnostics summary
```

These names are implementation guidance, not a requirement to create all files in F18B. The cut must remain small and compile-safe.

Preferred first runtime cut:

```text
F18B - Transition primitives only
```

F18B implements the passive runtime shape under `Runtime/Transition/`:

```text
TransitionOperationId
TransitionKind
TransitionPhase
TransitionStatus
TransitionStep
TransitionPlan
TransitionResult
TransitionSnapshot
```

F18B also adds `FrameworkIdentityDomain.Transition` so operation identity stays typed instead of falling back to string/path/name. It does not integrate with Route/Activity flows.

---

## 7. Planned F18 Cuts

| Cut | Target | Boundary |
|---|---|---|
| F18A | ADR implementation plan | Documentation only. Accept boundary and define implementation sequence. |
| F18B | Transition primitives | CLOSED. Passive types for operation identity, kind, phase/status, plan/result/snapshot. |
| F18C | Transition diagnostics smoke | CLOSED. Synthetic QA runner for valid/warning/failed transition plan/result/snapshot shapes without scene changes. |
| F18D | Gate blocker relationship | CLOSED. Passive policy/helper and synthetic smoke that map active transition operations to Gate blockers without applying runtime Gate state. |
| F18E | Route/Activity orchestration observation | CLOSED. Passive policy/helper and synthetic smoke describe Route switch, Activity switch and Activity clear without executing requests or mutating flow. |
| F18F | Closure / handoff to F19 | CLOSED. Document evidence, add Usage Guide and hand off to Transition Effects adapters. |

The sequence may be adjusted if implementation shows a smaller safe path, but F18 must remain orchestration-only.

---

## 8. F18B Implemented Evidence

F18B adds passive primitives only. The implemented boundary is:

```text
Runtime/Transition/TransitionOperationId.cs
Runtime/Transition/TransitionKind.cs
Runtime/Transition/TransitionPhase.cs
Runtime/Transition/TransitionStatus.cs
Runtime/Transition/TransitionStep.cs
Runtime/Transition/TransitionPlan.cs
Runtime/Transition/TransitionResult.cs
Runtime/Transition/TransitionSnapshot.cs
```

The identity domain is extended with:

```text
FrameworkIdentityDomain.Transition
```

F18B does not add runtime ownership, operation registry, route/activity integration, runtime Gate blocker registration, fade/loading/curtain, visual progress, UI, Pause or input.

---

## 9. F18C Implemented Evidence

F18C adds a synthetic QA smoke for the passive Transition primitives:

```text
Runtime/Diagnostics/TransitionQaSmokeRunner.cs
Framework QA Canvas -> Run Transition Diagnostics Smoke
```

The smoke validates:

```text
TransitionPlan for a RouteSwitch orchestration narrative
TransitionResult.SucceededResult
TransitionResult.CompletedWithWarningsResult
TransitionResult.FailedResult
TransitionSnapshot.FromPlan
```

The expected smoke envelope is:

```text
QA Smoke started. name='Transition Diagnostics Smoke'.
QA Transition Diagnostics Smoke step completed. step='plan' ...
QA Transition Diagnostics Smoke step completed. step='succeeded-result' ...
QA Transition Diagnostics Smoke step completed. step='warnings-result' ...
QA Transition Diagnostics Smoke step completed. step='failed-result' ...
QA Transition Diagnostics Smoke step completed. step='snapshot' ...
QA Smoke completed. name='Transition Diagnostics Smoke'.
```

F18C does not create scene changes, Route/Activity integration, Transition registry, runtime Gate blocker registration, fade/loading/curtain, visual progress, UI, Pause or input.


---

## 10. F18D Implemented Evidence

F18D adds a passive relationship between a logical Transition operation and Gate blockers:

```text
Runtime/Transition/TransitionGateBlockerPolicy.cs
Runtime/Diagnostics/TransitionGateBlockerQaSmokeRunner.cs
Framework QA Canvas -> Run Transition Gate Blocker Smoke
```

The helper describes a running Transition as a lifecycle request Gate blocker:

```text
blocker='transition-operation-in-flight'
scope='GameFlow'
domain='LifecycleRequest'
policySource='F18D.TransitionGateBlocker'
```

The smoke validates:

```text
blocker-created
running-blocks-lifecycle
completed-releases-blocker
failed-releases-blocker
```

The expected smoke envelope is:

```text
QA Smoke started. name='Transition Gate Blocker Relationship Smoke'.
QA Transition Gate Blocker Smoke step completed. step='blocker-created' ...
QA Transition Gate Blocker Smoke step completed. step='running-blocks-lifecycle' ...
QA Transition Gate Blocker Smoke step completed. step='completed-releases-blocker' ...
QA Transition Gate Blocker Smoke step completed. step='failed-releases-blocker' ...
QA Smoke completed. name='Transition Gate Blocker Relationship Smoke'.
```

F18D does not register Gate state, mutate GameFlow, change Route/Activity requests, create a Transition registry, create fade/loading/curtain, introduce Pause/Input/UI or add a lifecycle owner.

## 11. F18E Implemented Evidence

F18E adds passive observation of existing Route/Activity orchestration concepts as Transition diagnostics. It introduces:

```text
Runtime/Transition/TransitionOrchestrationObservationPolicy.cs
Runtime/Diagnostics/TransitionOrchestrationObservationQaSmokeRunner.cs
Framework QA Canvas -> Run Transition Orchestration Observation Smoke
```

The policy can create passive plans for:

```text
RouteSwitch
ActivitySwitch
ActivityClear
```

The synthetic smoke validates:

```text
route-switch-observed
activity-switch-observed
activity-clear-observed
observation-snapshot
```

The expected smoke envelope is:

```text
QA Smoke started. name='Transition Orchestration Observation Smoke'.
QA Transition Orchestration Observation Smoke step completed. step='route-switch-observed' ...
QA Transition Orchestration Observation Smoke step completed. step='activity-switch-observed' ...
QA Transition Orchestration Observation Smoke step completed. step='activity-clear-observed' ...
QA Transition Orchestration Observation Smoke step completed. step='observation-snapshot' ...
QA Smoke completed. name='Transition Orchestration Observation Smoke'.
```

F18E does not execute Route requests, Activity requests, scene loading, content release, readiness evaluation, Gate registration, visual effects, Pause, Input, UI, gameplay or a Transition lifecycle owner.

## 12. Required Diagnostics

Transition diagnostics should be able to answer:

```text
which operation is active
which transition kind is running
which phase is current
which Gate blocker exists because of transition
which lifecycle result was observed
which release/load/readiness step failed or skipped
whether the operation completed, completed with warnings, failed or was rejected
```

Diagnostics must be facts/loggable summaries, not hidden state inside scene objects.

---

## 13. F18F Closure Evidence

F18F closes Transition Orchestration Foundation. It adds the usage guide:

```text
Documentation~/Guides/F18-Transition-Orchestration-Usage.md
```

F18 is now closed with:

```text
F18A - implementation plan
F18B - passive primitives
F18C - diagnostics smoke
F18D - passive Gate blocker relationship
F18E - passive Route/Activity orchestration observation
F18F - usage guide and handoff
```

F19 is the next phase and may introduce visual effects/loading/fade adapters. F19 must consume the F18 logical contract and must not replace it.

---

## 14. Excluded From F18A-F18F

F18A-F18F do not implement or require:

```text
fade visual
loading screen
curtain
DOTween
Asset Store dependency
Pause menu
Pause state
input implementation
Player/Actor lifecycle
gameplay reset
runtime visual adapter
service locator
singleton transition manager
```

---

## 15. Guardrails

- Transition is orchestration of flow, not a visual effect.
- Transition consumes Gate for blocking/release.
- Transition must not create lifecycle parallel to Session/Route/Activity/Scene Lifecycle.
- Missing required callbacks/adapters in future cuts must fail explicitly.
- Visual effects remain adapters and cannot be the core contract.
- F18 closed without pulling F19 visual effects, F20 Pause or F22+ gameplay consumers forward.
