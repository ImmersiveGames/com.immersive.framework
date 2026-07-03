# F23 — Pause Content / Overlay / Input Usage

Status: `CLOSED / F23F QA PASS + USAGE`  
Package: `com.immersive.framework`  
Scope: Pause content, presentation and input intent boundary only.

---

## 1. What F23 provides

F23 extends the logical Pause core from F20 with framework-only intent records.

Use F23 when another framework surface needs to describe:

```text
what Pause content is required
what Pause presentation should represent
what Pause input intent was observed
```

The canonical F23 runtime contracts are:

```text
Runtime/Pause/PauseContentRequirementId
Runtime/Pause/PauseContentRequirementPurpose
Runtime/Pause/PauseContentRequirement
Runtime/Pause/PausePresentationIntent
Runtime/Pause/PauseInputActionId
Runtime/Pause/PauseInputCommandKind
Runtime/Pause/PauseInputSourceKind
Runtime/Pause/PauseInputSignal
Runtime/Pause/PauseInputIntent
```

F23 is intentionally vocabulary and diagnostics. It does not materialize UI.

---

## 2. What F23 does not provide

F23 does not create:

```text
Canvas
prefab
scene object
ScriptableObject
Input System asset
action map
device binding
input polling
overlay adapter
Content Anchor binding execution
RuntimeContentAnchorBinding
TransitionEffect execution
Time.timeScale policy
Route/Activity lifecycle ownership
gameplay adapter
asmdef change
```

Concrete Unity wiring moves to F24. Gameplay behavior moves to F25+.

---

## 3. Boundary map

| Area | Owner | Relationship to F23 |
|---|---|---|
| Pause state/request/result | F20 Pause State/Gate | F23 consumes the existing logical Pause state. |
| Save/Preferences | F21 | F23 may describe menu intent, but does not read/write preferences or save data. |
| Loading reporting | F22 | F23 may coexist with loading states, but does not own loading operations. |
| Content Anchor binding | F24 Unity Build Surface | F23 records content requirements only. |
| Overlay UI | F24 Unity Build Surface | F23 records presentation intent only. |
| Input System / action maps | F24 Unity Build Surface | F23 records normalized input intent only. |
| Gameplay adapters | F25+ | F23 does not pause actors, players, NPCs, inventory, combat or timers. |

---

## 4. Declaring a Pause content requirement

Use `PauseContentRequirement` to describe content that a later Unity build surface may need.

```csharp
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Pause;

var requirement = PauseContentRequirement.Required(
    "pause.requirement.overlay.root",
    PauseContentRequirementPurpose.OverlayRoot,
    ContentAnchorId.From("route.pause.overlay"),
    "PauseMenu",
    "Route-level Pause overlay anchor is required.");
```

Rules:

```text
requirement id is logical
anchor id is a requirement reference, not a binding result
F23 does not search scenes
F23 does not call RuntimeContentAnchorBinding
F23 does not create or destroy objects
```

---

## 5. Creating a presentation intent

Use `PausePresentationIntent` to describe how Pause wants to be represented.

```csharp
using Immersive.Framework.Pause;

var presentation = PausePresentationIntent.Visible(
    "pause.presentation.visible",
    "Pause Menu",
    "Game Paused",
    "Show the logical Pause overlay.");
```

Rules:

```text
presentation intent is data
show/hide behavior is not executed in F23
no Canvas or prefab is referenced
no overlay adapter exists in F23
```

---

## 6. Creating an input intent

`PauseInputSignal` is the normalized signal. `PauseInputIntent` is the interpreted intent.

```csharp
using Immersive.Framework.Pause;

var signal = PauseInputSignal.Pressed(
    "pause.input.toggle",
    PauseInputCommandKind.TogglePause,
    PauseInputSourceKind.Keyboard,
    "Keyboard",
    "Escape pressed.");

var intent = PauseInputIntent.FromSignal(
    "pause.input.intent.toggle",
    signal,
    "PauseInput",
    "Toggle Pause requested.");
```

Rules:

```text
F23 does not read hardware
F23 does not bind Input System actions
F23 does not poll input
F23 does not dispatch global events
F23 does not navigate a concrete menu
```

---

## 7. QA usage

Open the QA Canvas and expand:

```text
Show Pause diagnostics
```

Run:

```text
Run Pause Boundary Intent Smoke
```

Expected steps:

```text
contracts
content-requirement-intent
presentation-intent
input-intent
canonical-boundary
```

The canonical boundary step must report:

```text
anchorBinding = none
overlayAdapter = none
inputAdapter = none
inputSystem = none
ui = none
timeScale = none
gameplayAdapters = none
unityBuild = deferredToF24
```

---

## 8. Designer-facing current state

At the end of F23 there is no Pause menu to place in a scene yet.

Current designer-facing rule:

```text
F20 gives Pause state.
F23 gives Pause content/presentation/input intent language.
F24 will define the first real Unity build surface for Pause overlay.
```

Screenshot placeholders for future F24:

```text
[SCREENSHOT PLACEHOLDER: F24E minimal Pause overlay GameObject hierarchy]
[SCREENSHOT PLACEHOLDER: F24E Pause overlay adapter component in Inspector]
[SCREENSHOT PLACEHOLDER: F24E QA smoke showing RequestPause opening/closing overlay]
```

---

## 9. Handoff to F24

F24 is responsible for proving framework contracts on real Unity surfaces.

For Pause, the relevant future cut is:

```text
F24E — Pause Overlay Unity Build
```

F24E may create the minimal Unity object/prefab/wiring needed to prove Pause overlay behavior, but it must still avoid gameplay adapters and full product menu scope.
