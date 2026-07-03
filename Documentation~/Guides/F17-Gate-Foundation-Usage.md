# F17 — Gate Foundation Usage

Status: F17 closed/applied. This guide covers the closed Gate Foundation path from F17A-F17E.

Gate is the framework's canonical language for admission decisions. It answers whether a request, input acceptance, interaction or gameplay action may proceed in a specific scope and domain.

F17 does not make Gate a user-facing authoring system. It provides primitives, request-admission integration for existing in-flight guards, and QA diagnostics.

---

## What lives where

Framework Gate scripts live in the package:

```text
Packages/com.immersive.framework/Runtime/Gate/
```

F17 adds passive primitives and one internal request-admission helper:

```text
Runtime/Gate/
├─ GateScope
├─ GateDomain
├─ GateDecisionStatus
├─ GateDecision
├─ GateBlocker
├─ GateEvaluationResult
├─ GateSnapshot
└─ GateRequestAdmission        // internal helper used by F17C
```

Configured scene objects do not need a Gate component in F17.

---

## What Gate is

Gate is a decision boundary for admission.

Use Gate language when the framework needs to answer questions like:

```text
Can this lifecycle request proceed now?
Can this interaction be accepted now?
Can this gameplay action run now?
Can this Pause or Transition operation admit related work?
```

A Gate evaluation should produce explicit diagnostics:

```text
scope
domain
subject
status
blockers
facts
policySource
```

---

## What Gate is not

F17 Gate is not:

```text
UI
menu logic
Input System wrapper
Activity readiness
Scene loading
Transition visual effect
Pause state
Pause menu
request queue
global registry
service locator
runtime manager
gameplay object model
```

Gate may be consumed by Transition and Pause in future phases, but F17 does not implement those consumers.

---

## Current F17 runtime integration

F17C routes existing in-flight request guards through Gate admission diagnostics.

Covered request families:

```text
Route Request
Activity Request
Clear Activity Request
Cycle Reset Request
Object Reset Request
```

The happy path is intentionally unchanged. When no request is already in flight, existing requests proceed as before.

The blocked path is now represented as a `GateEvaluationResult`, while preserving existing public result kinds.

Expected preservation:

```text
Route/Activity already in flight -> IgnoredAlreadyInFlight
CycleReset/ObjectReset already in flight -> existing invalid/rejected result shape
```

---

## Basic internal example — allowed admission

This is a framework-developer example, not required scene authoring:

```csharp
using Immersive.Framework.Gate;

var result = GateSnapshot.Empty().Evaluate(
    GateScope.GameFlow,
    GateDomain.LifecycleRequest,
    default,
    subject: "RouteRequest",
    source: "Example",
    reason: "No blockers are active.",
    policySource: "Example.Policy");

if (result.IsAllowed)
{
    // Existing request path may proceed.
}
```

---

## Basic internal example — blocked admission

```csharp
using Immersive.Framework.Gate;

var blocker = GateBlocker.ForAnyOwner(
    "route-request-in-flight",
    GateScope.GameFlow,
    GateDomain.LifecycleRequest,
    source: "GameFlowRuntime",
    reason: "Route request is already in flight.",
    policySource: "Example.Policy");

var snapshot = new GateSnapshot(new[] { blocker });

var result = snapshot.Evaluate(
    GateScope.GameFlow,
    GateDomain.LifecycleRequest,
    default,
    subject: "ActivityRequest",
    source: "Example",
    reason: "Another lifecycle request is already active.",
    policySource: "Example.Policy");

if (result.IsBlocked)
{
    // Preserve the existing public result kind and include Gate diagnostics in the message/log.
}
```

---

## QA smoke

Run:

```text
Run Gate Admission Diagnostics Smoke
```

Expected steps:

```text
allowed
route-in-flight
activity-in-flight
cycle-reset-in-flight
object-reset-in-flight
```

Expected successful log shape:

```text
QA Smoke started. name='Gate Admission Diagnostics Smoke'.
QA Gate Admission Diagnostics Smoke step completed. step='allowed' passed='True' status='Allowed' ...
QA Gate Admission Diagnostics Smoke step completed. step='route-in-flight' passed='True' status='Blocked' expectedBlocker='route-request-in-flight' blockerMatched='True'
QA Gate Admission Diagnostics Smoke step completed. step='activity-in-flight' passed='True' status='Blocked' expectedBlocker='activity-request-in-flight' blockerMatched='True'
QA Gate Admission Diagnostics Smoke step completed. step='cycle-reset-in-flight' passed='True' status='Blocked' expectedBlocker='cycle-reset-request-in-flight' blockerMatched='True'
QA Gate Admission Diagnostics Smoke step completed. step='object-reset-in-flight' passed='True' status='Blocked' expectedBlocker='object-reset-request-in-flight' blockerMatched='True'
QA Smoke completed. name='Gate Admission Diagnostics Smoke'.
```

---

## Regression smokes after Gate changes

After changing Gate or request admission, run:

```text
Run Standard Smoke
Run Activity Baseline Smoke
Run Cycle Reset Bridge Smoke
Run Object Reset GameObject Active Closure Smoke
Run Object Reset Unity Adapters Closure Smoke
Run Gate Admission Diagnostics Smoke
```

The first five validate that happy path behavior did not regress. The Gate smoke validates the diagnostic admission language.

---

## Current limitations

F17 does not include:

```text
Gate registry global
Gate authoring asset
Gate editor UI
request queue
Transition operation runtime
Transition visual effect
Pause state/runtime
Input/gameplay gate integration
gameplay object model
contextual reset for Player/Actor/NPC/Timer/Door/Pickup
```

Future phases:

```text
F18 -> Transition Orchestration consumes Gate logically.
F19 -> Transition visual effects are adapters, not Gate.
F20 -> Pause State uses Gate blockers.
F21 -> Pause content/input boundary consumes Pause/Gate decisions.
F22+ -> Advanced consumers and contextual gameplay capabilities.
```

---

## Closure rule for future phases

When a phase closes, add or update a Usage guide in:

```text
Documentation~/Guides/
```

The guide should explain what exists, how to validate it, and what remains outside the closed phase.
