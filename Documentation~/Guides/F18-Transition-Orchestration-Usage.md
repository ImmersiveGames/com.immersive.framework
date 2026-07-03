# F18 — Transition Orchestration Foundation Usage

Status: F18 closed/applied. This guide covers the closed Transition Orchestration Foundation path from F18A-F18F.

Transition is the framework's logical narrative for a flow change. It describes how a Route switch, Activity switch or Activity clear should be observed as an operation, without becoming a visual effect, a scene loader replacement or a parallel lifecycle.

F18 does not create fade, loading screen, curtain, Pause, Input, UI or gameplay behavior. Those remain later phases.

---

## What lives where

Transition scripts live in the package:

```text
Packages/com.immersive.framework/Runtime/Transition/
```

F18 adds passive primitives and diagnostic policies:

```text
Runtime/Transition/
├─ TransitionOperationId
├─ TransitionKind
├─ TransitionPhase
├─ TransitionStatus
├─ TransitionStep
├─ TransitionPlan
├─ TransitionResult
├─ TransitionSnapshot
├─ TransitionGateBlockerPolicy
└─ TransitionOrchestrationObservationPolicy
```

Synthetic QA runners live under:

```text
Runtime/Diagnostics/
├─ TransitionQaSmokeRunner
├─ TransitionGateBlockerQaSmokeRunner
└─ TransitionOrchestrationObservationQaSmokeRunner
```

Configured scene objects do not need a Transition component in F18.

---

## What Transition is

Transition is a framework-core orchestration contract.

Use Transition language when the framework needs to describe a flow change as an explicit operation:

```text
request admitted
transition operation opened
Gate blocker relationship described
previous scope exit observed
content release observed
scene/content operation observed
next scope enter observed
readiness observed
transition completed, completed with warnings or failed
Gate blocker relationship released
facts emitted
```

Transition owns the operation narrative. It does not own Route Lifecycle, Activity Flow, Scene Lifecycle, RuntimeContent, Content Anchor or Object Reset.

---

## What Transition is not

F18 Transition is not:

```text
fade visual
loading screen
curtain
DOTween integration
UI widget
Pause menu
Input System wrapper
scene loader replacement
Route Lifecycle replacement
Activity Flow replacement
runtime registry
global manager
service locator
parallel lifecycle
gameplay object model
contextual reset
```

F19 may add visual transition effects as adapters. F18 intentionally does not.

---

## Transition primitives

F18B adds the passive runtime shape:

```text
TransitionOperationId       // typed identity for one logical transition operation
TransitionKind              // RouteSwitch, ActivitySwitch, ActivityClear, etc.
TransitionPhase             // current narrative phase
TransitionStatus            // Running, Succeeded, CompletedWithWarnings, Failed, etc.
TransitionStep              // one expected or observed step
TransitionPlan              // expected operation narrative
TransitionResult            // final or diagnostic outcome
TransitionSnapshot          // diagnostic state summary
```

`TransitionOperationId` uses `FrameworkIdentityDomain.Transition`, so operation identity stays typed and does not rely on GameObject name, path or loose strings.

---

## Relationship with Gate

F18D defines the passive relationship between a running Transition and Gate:

```text
running transition
→ GateBlocker
→ scope='GameFlow'
→ domain='LifecycleRequest'
→ blocker='transition-operation-in-flight'
```

This relationship is diagnostic/passive in F18. It does not register runtime Gate state and does not alter real request admission yet.

---

## Route/Activity observation

F18E describes existing Route/Activity orchestration as passive Transition observations.

Covered narratives:

```text
RouteSwitch
ActivitySwitch
ActivityClear
```

These observations produce plans/results/snapshots for diagnostics. They do not execute requests and do not mutate `GameFlowRuntime`, `RouteLifecycleRuntime`, `ActivityFlowRuntime` or `SceneLifecycleRuntime`.

---

## QA smokes

### Transition Diagnostics Smoke

Run:

```text
Run Transition Diagnostics Smoke
```

Expected steps:

```text
plan
succeeded-result
warnings-result
failed-result
snapshot
```

Expected successful log shape:

```text
QA Smoke started. name='Transition Diagnostics Smoke'.
QA Transition Diagnostics Smoke step completed. step='plan' passed='True' ...
QA Transition Diagnostics Smoke step completed. step='succeeded-result' passed='True' ...
QA Transition Diagnostics Smoke step completed. step='warnings-result' passed='True' ...
QA Transition Diagnostics Smoke step completed. step='failed-result' passed='True' ...
QA Transition Diagnostics Smoke step completed. step='snapshot' passed='True' ...
QA Smoke completed. name='Transition Diagnostics Smoke'.
```

### Transition Gate Blocker Relationship Smoke

Run:

```text
Run Transition Gate Blocker Smoke
```

Expected steps:

```text
blocker-created
running-blocks-lifecycle
completed-releases-blocker
failed-releases-blocker
```

Expected successful log shape:

```text
QA Smoke started. name='Transition Gate Blocker Relationship Smoke'.
QA Transition Gate Blocker Smoke step completed. step='blocker-created' passed='True' ...
QA Transition Gate Blocker Smoke step completed. step='running-blocks-lifecycle' passed='True' ...
QA Transition Gate Blocker Smoke step completed. step='completed-releases-blocker' passed='True' ...
QA Transition Gate Blocker Smoke step completed. step='failed-releases-blocker' passed='True' ...
QA Smoke completed. name='Transition Gate Blocker Relationship Smoke'.
```

### Transition Orchestration Observation Smoke

Run:

```text
Run Transition Orchestration Observation Smoke
```

Expected steps:

```text
route-switch-observed
activity-switch-observed
activity-clear-observed
observation-snapshot
```

Expected successful log shape:

```text
QA Smoke started. name='Transition Orchestration Observation Smoke'.
QA Transition Orchestration Observation Smoke step completed. step='route-switch-observed' passed='True' ...
QA Transition Orchestration Observation Smoke step completed. step='activity-switch-observed' passed='True' ...
QA Transition Orchestration Observation Smoke step completed. step='activity-clear-observed' passed='True' ...
QA Transition Orchestration Observation Smoke step completed. step='observation-snapshot' passed='True' ...
QA Smoke completed. name='Transition Orchestration Observation Smoke'.
```

---

## Regression smokes after Transition changes

After changing Transition primitives or diagnostics, run:

```text
Run Standard Smoke
Run Gate Admission Diagnostics Smoke
Run Transition Diagnostics Smoke
Run Transition Gate Blocker Smoke
Run Transition Orchestration Observation Smoke
```

For broader regression after integration work, also run:

```text
Run Activity Baseline Smoke
Run Cycle Reset Bridge Smoke
Run Object Reset GameObject Active Closure Smoke
Run Object Reset Unity Adapters Closure Smoke
```

---

## Current limitations

F18 does not include:

```text
runtime Transition owner
Transition registry
runtime Gate blocker registration
real Route/Activity request wrapping
fade visual
loading screen
curtain
progress UI
DOTween adapter
Pause state/runtime
Input/gameplay gate integration
gameplay object model
contextual reset for Player/Actor/NPC/Timer/Door/Pickup
```

Future phases:

```text
F19 -> Transition Effects / Loading and Fade Adapters.
F20 -> Pause State and Pause Gate.
F21 -> Pause Content / Overlay / Input Boundary.
F22+ -> Advanced consumers and contextual gameplay capabilities.
```

---

## Closure rule for future phases

When a phase closes, add or update a Usage guide in:

```text
Documentation~/Guides/
```

The guide should explain what exists, how to validate it, and what remains outside the closed phase.
