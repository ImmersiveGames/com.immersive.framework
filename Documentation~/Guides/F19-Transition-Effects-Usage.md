# F19 — Transition Effects Usage

Status: `CLOSED / F19F QA PASS + USAGE`  
Phase: F19 — Transition Effects / Loading and Fade Adapters  
Last updated: 2026-06-26

---

## 1. What F19 Adds

F19 adds the first Transition Effects layer after F18 Transition Orchestration.

Transition Effects are adapters/consumers of Transition. They are not Transition Core and they do not decide Route, Activity, SceneLifecycle, Gate or Pause behavior.

Implemented in F19:

```text
Runtime/TransitionEffects/TransitionEffectId.cs
Runtime/TransitionEffects/TransitionEffectKind.cs
Runtime/TransitionEffects/TransitionEffectRequiredness.cs
Runtime/TransitionEffects/TransitionEffectStatus.cs
Runtime/TransitionEffects/TransitionEffectRequest.cs
Runtime/TransitionEffects/TransitionEffectResult.cs
Runtime/TransitionEffects/TransitionEffectPlan.cs
Runtime/TransitionEffects/TransitionEffectSnapshot.cs
Runtime/TransitionEffects/ITransitionEffectAdapter.cs
Runtime/TransitionEffects/UnityFadeCurtainEffectAdapter.cs
Runtime/TransitionEffects/TransitionEffectAuthoringPolicy.cs
Runtime/TransitionEffects/TransitionEffectPolicyEvaluation.cs
Runtime/TransitionEffects/TransitionEffectPolicyIssue.cs
Runtime/TransitionEffects/TransitionEffectPolicyIssueSeverity.cs
```

QA smokes added in F19:

```text
Run Transition Effect Diagnostics Smoke
Run Unity Fade Curtain Effect Adapter Smoke
Run Transition Effect Policy Guardrails Smoke
```

---

## 2. What F19 Does Not Add

F19 does not add:

```text
Transition runtime owner
runtime effect registry
adapter discovery layer
ScriptableObject effect profile
DOTween integration
tween timing
canonical loading screen UI
Pause menu
Pause input
Route/Activity request integration
SceneLifecycle ownership
gameplay object model
contextual reset
service locator
fallback for required missing adapters
```

A missing required effect adapter must remain explicit and blocking. It must not degrade silently to success.

---

## 3. QA Canvas After F19F

F19F compacts the QA Canvas.

Visible by default:

```text
Run Standard Smoke
Run Activity Baseline Smoke
Validate Loaded Authoring
Reset QA Scenario
```

Diagnostics are still available, but collapsed behind toggles:

```text
Show Gate / Transition / Effect diagnostics
Show Route / Content diagnostics
Show Foundation diagnostics
Show Reset / Object diagnostics
Show advanced/manual controls
```

To run the F19 smokes:

```text
1. Open Immersive Framework QA.
2. Open Show Gate / Transition / Effect diagnostics.
3. Run the required F19 smoke button.
```

---

## 4. Normal Validation Path

After applying F19 packages, use this minimum validation path:

```text
1. Compile/import without CS errors.
2. Run Standard Smoke.
3. Run Activity Baseline Smoke when changing lifecycle-sensitive code.
4. Open Show Gate / Transition / Effect diagnostics.
5. Run Transition Effect Diagnostics Smoke.
6. Run Unity Fade Curtain Effect Adapter Smoke.
7. Run Transition Effect Policy Guardrails Smoke.
```

Expected pass markers:

```text
QA Smoke completed. name='Standard Smoke'.
QA Smoke completed. name='Transition Effect Diagnostics Smoke'.
QA Smoke completed. name='Unity Fade Curtain Effect Adapter Smoke'.
QA Smoke completed. name='Transition Effect Policy Guardrails Smoke'.
```

---

## 5. Using the Minimal Fade/Curtain Adapter Manually

The canonical smoke creates a transient QA object, so saved scene setup is not required to validate F19.

For manual visual testing:

```text
1. Open a QA scene, for example StartupScene.
2. Create a GameObject named QA_TransitionFadeSurface.
3. Add CanvasGroup.
4. Add Unity Fade Curtain Effect Adapter.
5. Assign Canvas Group to the CanvasGroup on the same GameObject.
6. Assign Surface Root to QA_TransitionFadeSurface.
7. Set Effect Kind to Fade or Curtain.
8. Set Hidden Alpha to 0.
9. Set Visible Alpha to 1.
10. Enable Set Surface Root Active.
11. Enable Block Raycasts When Visible.
12. Disable Interactable When Visible.
13. Enable Apply Hidden State On Awake.
```

To see a real full-screen surface, create the visual UI manually:

```text
Canvas
  Fullscreen Image or Panel
    CanvasGroup
    UnityFadeCurtainEffectAdapter
```

The framework adapter only controls `CanvasGroup` and optional root active state. It does not require or reference `Image` directly.

---

## 6. Required vs Optional Effects

F19E closes this policy:

```text
required effect + compatible adapter present => allowed
required effect + no compatible adapter => blocking issue
optional effect + no compatible adapter => warning / non-blocking issue
duplicate effect id inside one plan => blocking issue
```

Policy evaluation is explicit:

```csharp
TransitionEffectAuthoringPolicy.Evaluate(plan, adapters)
```

The policy does not search the scene, load assets or consult a registry.

---

## 7. First Quick-Use Mental Model

Use F19 like this:

```text
Transition describes what is happening.
Transition Effect Request describes what visual/effect response is desired.
Transition Effect Adapter tries to execute that request.
Transition Effect Result records exactly what happened.
Transition Effect Authoring Policy checks whether required adapters exist before pretending the plan is valid.
```

This remains adapter-level, but F24C now wires the minimal Unity surface into real Route/Activity/ActivityClear requests through the persistent `FrameworkRuntimeHost` when a `GameApplicationAsset` provides a transition surface prefab.

---

## 8. Handoff to F20

F20 starts Pause State and Pause Gate.

The next phase must not start from visual overlay. It must first define:

```text
Pause state
Pause request/result
Pause Gate blocker relationship
what Pause blocks
what Pause does not own
```

Pause content, overlay and input are F21 concerns.
