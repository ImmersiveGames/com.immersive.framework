# F19-ADR-TRANSITION-002 - Transition Effects Boundary

Status: Accepted / Closed F19F  
Phase: F19 - Transition Effects / Loading and Fade Adapters  
Type: Unity Adapter / Optional Effects / Boundary  
Last updated: 2026-06-26

---

## 1. Context

F18 closed Transition Orchestration as a logical, passive foundation. It defined operation identity, kind, phase/status, plan/result/snapshot, diagnostics smoke, a passive relationship to Gate blockers and passive observation of Route/Activity orchestration.

F18 intentionally did not create visual effects, loading screens, fade adapters, curtain UI, scene objects or authoring assets.

F19 starts only after that boundary is closed.

The problem F19 solves is not "how to make a pretty fade". The problem is how the framework describes, validates and invokes optional or required transition effects without letting those effects redefine Gate, Transition, SceneLifecycle, RouteLifecycle, ActivityFlow or Pause.

---

## 2. Decision

Transition effects are Unity adapters/consumers of Transition Orchestration.

They are not Transition Core.

Examples:

```text
fade adapter
loading overlay adapter
curtain adapter
progress display adapter
audio/visual stinger adapter
```

An effect adapter may execute Unity operations such as changing Canvas visibility, CanvasGroup alpha, object active state or progress text. It must not decide whether the transition itself is admitted, completed, blocked or failed.

---

## 3. Effect Contract Shape

F19 should introduce a small adapter-facing vocabulary before any concrete visual implementation.

Expected concepts:

```text
TransitionEffectKind
TransitionEffectPhase
TransitionEffectRequiredness
TransitionEffectRequest
TransitionEffectResult
TransitionEffectIssue
TransitionEffectPlan
TransitionEffectSnapshot
ITransitionEffectAdapter
```

The first implementation must remain deterministic and inspectable:

```text
request before side effect
result after side effect
explicit skipped/failed/completed status
blocking vs non-blocking issues
no hidden fallback
```

---

## 4. Required vs Optional

Required effect adapter missing:

```text
explicit failure
blocking issue
diagnostic fact
no silent fallback
```

Optional effect adapter missing:

```text
allowed continuation
explicit skip or non-blocking issue/fact
no fake success that hides configuration problems
```

A required fade that is not configured must not be reported as successful just because the transition flow can technically continue.

---

## 5. Dependency Boundary

F19 must not add mandatory dependency on:

```text
DOTween
Timeline
Addressables
Asset Store fade package
custom loading screen package
Cinemachine
UGUI as core dependency beyond normal UnityEngine/UI adapter boundaries
```

Specific integrations can be future adapters with their own dependency boundary.

A minimal Unity fade adapter may use built-in Unity APIs only. If a future DOTween adapter exists, it must be optional and isolated from core Transition.

---

## 6. Unity Adapter Boundary

Unity adapters come after the logical contract.

They may reference Unity APIs, but they must not redefine:

```text
Gate
Transition orchestration
Scene Lifecycle
Route lifecycle
Activity lifecycle
Pause state
```

Unity adapter responsibilities are limited to concrete effect execution and reporting:

```text
prepare effect target
apply close/open/hold/progress visual state
return effect result
report missing target/config as explicit issue
```

---

## 7. Manual Scene/Object/SO Setup Forecast

F19A does not create scene objects or ScriptableObjects.

Scene/Object/SO setup should begin only when a concrete Unity adapter cut needs authoring validation.

Expected future setup sequence:

```text
F19B - effect primitives/contracts only; no scene objects or ScriptableObjects.
F19C - synthetic diagnostics smoke; no scene objects or ScriptableObjects.
F19D - first Unity effect adapter; likely requires a QA scene object with a component such as a fade surface/adapter.
F19E - required/optional adapter policy; may require a QA ScriptableObject/profile if authoring policy becomes necessary.
F19F - closure and Usage Guide; documents exact setup and smoke steps.
```

When F19D or later requires manual setup, the implementation note must tell the user exactly what to create in `Assets`:

```text
which scene to open
which GameObject to create
which component to add
which fields to assign
which ScriptableObject to create, if any
which QA smoke button to run
what log lines prove PASS
```

No canonical package setup should import paid/Asset Store packages, local absolute paths or old project assets.

---

## 8. Planned F19 Cuts

```text
F19A - Transition Effects Boundary Implementation Plan
F19B - Transition Effect Primitives - CLOSED / PRIMITIVES APPLIED
F19C - Transition Effect Diagnostics Smoke - CLOSED / DIAGNOSTICS SMOKE APPLIED
F19D - Minimal Unity Fade/Curtain Adapter Boundary - CLOSED / UNITY ADAPTER BOUNDARY APPLIED
F19E - Required/Optional Effect Policy and Authoring Guardrails - CLOSED / POLICY GUARDRAILS APPLIED
F19F - Closure, Usage Guide and handoff to F20 Pause State/Gate - CLOSED / QA PASS + USAGE
```

F19B does not execute visuals. It creates the passive effect contract vocabulary under `Runtime/TransitionEffects`: `TransitionEffectId`, `TransitionEffectKind`, `TransitionEffectRequiredness`, `TransitionEffectStatus`, `TransitionEffectRequest`, `TransitionEffectResult`, `TransitionEffectPlan` and `TransitionEffectSnapshot`. It also adds `FrameworkIdentityDomain.TransitionEffect` so effect ids remain typed identities.

F19C validates the vocabulary through synthetic diagnostics. It does not create scene objects, ScriptableObjects or visual effect execution.

F19D is the first concrete Unity adapter cut. It adds the adapter contract and a minimal built-in Unity fade/curtain component. The canonical smoke creates a transient QA object, so saved scene setup is optional for validation.

F19E adds required/optional effect policy and authoring guardrails without creating a ScriptableObject, registry or discovery layer. The caller supplies an explicit adapter list to `TransitionEffectAuthoringPolicy.Evaluate`. Required adapter absence blocks. Optional adapter absence produces a warning and remains non-blocking. Duplicate effect ids in one plan block as authoring ambiguity.

F19F closes the phase with a usage guide and QA Canvas cleanup. It adds no new effect runtime, no ScriptableObject, no registry, no discovery, no DOTween integration and no Route/Activity execution integration.

---

## 9. Excluded From F19A

F19A does not define or create:

```text
runtime effect execution
scene objects
ScriptableObjects
Canvas prefab
loading screen prefab
fade visual implementation
DOTween integration
Route/Activity runtime integration
Pause menu
Input mode
gameplay object model
contextual reset
```

---

## 10. Guardrails

- Fade/loading/curtain never substitute Gate.
- Effects are adapters, not Transition Core.
- Required adapter absence cannot silently degrade.
- Optional adapter absence must not fabricate success facts.
- Visual implementation must not create global manager/service locator.
- Effects do not own Route/Activity lifecycle.
- Effects do not own SceneLifecycle loading/unloading.
- Effects do not decide Pause state.
- Scene objects and ScriptableObjects enter only when the adapter cut requires them, and must be documented with exact manual setup steps.


## 10. F19B Closure Note

F19B adds only passive primitives. It does not create an effect adapter, Unity component, Canvas, scene object, ScriptableObject, loading screen, fade implementation, DOTween integration, runtime effect owner, registry or fallback path.

The new primitives are:

```text
Runtime/TransitionEffects/TransitionEffectId.cs
Runtime/TransitionEffects/TransitionEffectKind.cs
Runtime/TransitionEffects/TransitionEffectRequiredness.cs
Runtime/TransitionEffects/TransitionEffectStatus.cs
Runtime/TransitionEffects/TransitionEffectRequest.cs
Runtime/TransitionEffects/TransitionEffectResult.cs
Runtime/TransitionEffects/TransitionEffectPlan.cs
Runtime/TransitionEffects/TransitionEffectSnapshot.cs
```

F19C validates these shapes through a synthetic QA smoke only.


## F19C — Transition Effect Diagnostics Smoke

F19C adds a synthetic QA smoke for the passive Transition Effect primitives introduced in F19B.

Validated shapes:

- `TransitionEffectRequest`;
- `TransitionEffectPlan`;
- `TransitionEffectResult.SucceededResult`;
- `TransitionEffectResult.SkippedResult` for optional effects;
- `TransitionEffectResult.MissingAdapterResult` for required blocking effects;
- `TransitionEffectSnapshot.FromResults`.

F19C still does not create scene objects, Canvas, ScriptableObjects, Unity adapters, fade visuals, loading screens, DOTween integration, Pause, Input, UI or runtime effect execution.

The QA entry point is `Run Transition Effect Diagnostics Smoke`.


## F19D — Minimal Unity Fade/Curtain Adapter Boundary

F19D adds the first concrete Unity adapter boundary for Transition Effects.

New runtime files:

```text
Runtime/TransitionEffects/ITransitionEffectAdapter.cs
Runtime/TransitionEffects/UnityFadeCurtainEffectAdapter.cs
Runtime/Diagnostics/TransitionEffectUnityFadeCurtainQaSmokeRunner.cs
```

New QA button:

```text
Run Unity Fade Curtain Effect Adapter Smoke
```

The adapter executes only one request at a time and returns explicit `TransitionEffectResult` values. It mutates only:

```text
CanvasGroup.alpha
CanvasGroup.blocksRaycasts
CanvasGroup.interactable
optional surfaceRoot.activeSelf
```

It does not own or redefine:

```text
Transition orchestration
Gate admission/block/release
SceneLifecycle
RouteLifecycle
ActivityFlow
Pause
Input
loading progress
DOTween/tween timing
adapter discovery/registry
service locator
```

Required missing surface behavior is explicit:

```text
required fade request + missing CanvasGroup => Failed result + blocking issue
```

Optional unsupported kind behavior is explicit:

```text
optional loading screen request sent to fade/curtain adapter => Rejected result + non-blocking issue
```

F19D does not add ScriptableObject authoring. Required/optional adapter policy and authoring guardrails remain F19E.

Manual visual setup is documented in:

```text
Documentation~/Guides/F19D-Minimal-Fade-Curtain-Adapter-Setup.md
```


## F19E — Required/Optional Effect Policy and Authoring Guardrails

F19E adds explicit policy evaluation for effect requiredness and basic authoring correctness.

New runtime files:

```text
Runtime/TransitionEffects/TransitionEffectPolicyIssueSeverity.cs
Runtime/TransitionEffects/TransitionEffectPolicyIssue.cs
Runtime/TransitionEffects/TransitionEffectPolicyEvaluation.cs
Runtime/TransitionEffects/TransitionEffectAuthoringPolicy.cs
Runtime/Diagnostics/TransitionEffectPolicyQaSmokeRunner.cs
```

New QA button:

```text
Run Transition Effect Policy Guardrails Smoke
```

Policy behavior:

```text
required effect + no compatible adapter => blocking issue
optional effect + no compatible adapter => warning / non-blocking issue
duplicate effect id inside one plan => blocking issue
compatible adapter present => request is matched and allowed
```

The policy receives adapters explicitly:

```text
TransitionEffectAuthoringPolicy.Evaluate(plan, adapters)
```

It does not perform scene discovery, registry lookup, automatic fallback or adapter execution.

F19E does not require scene setup or ScriptableObject authoring. The smoke creates a transient QA object with `UnityFadeCurtainEffectAdapter` only to provide one explicit fade-compatible adapter to the policy.

Still excluded from F19E:

```text
ScriptableObject profile
adapter registry
scene object discovery
runtime effect owner
Transition runtime integration
Route/Activity request integration
loading screen canonical UI
DOTween/tween timing
Pause/Input/UI gameplay flow
service locator
```


## F19F — Closure, Usage Guide and QA Canvas Cleanup

F19F closes Transition Effects as an adapter/effect boundary.

New usage guide:

```text
Documentation~/Guides/F19-Transition-Effects-Usage.md
```

The guide explains:

- what F19 adds;
- what remains outside F19;
- how to run diagnostics after QA Canvas compaction;
- optional manual visual setup for `UnityFadeCurtainEffectAdapter`;
- expected smoke evidence;
- handoff to F20 Pause State/Gate.

F19F also compacts the QA Canvas. The default visible QA buttons are limited to the baseline path:

```text
Run Standard Smoke
Run Activity Baseline Smoke
Validate Loaded Authoring
Reset QA Scenario
```

Phase diagnostics remain available but collapsed behind toggles:

```text
Gate / Transition / Effect diagnostics
Route / Content diagnostics
Foundation diagnostics
Reset / Object diagnostics
Advanced/manual controls
```

This removes the long flat button list without deleting smoke methods or changing their runtime behavior.

F19F does not add:

```text
ScriptableObject profile
adapter registry
scene object discovery
runtime effect owner
Transition runtime integration
Route/Activity request integration
loading screen canonical UI
DOTween/tween timing
Pause/Input/UI gameplay flow
service locator
```

The next phase is F20 Pause State and Pause Gate. F20 must start with state/Gate semantics before F21 visual Pause overlay/input/content.
