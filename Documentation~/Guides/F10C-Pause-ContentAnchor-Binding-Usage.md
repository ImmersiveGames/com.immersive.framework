# F10C — Pause ContentAnchor Binding Usage

This guide explains the Pause → ContentAnchor binding request step in game-design terms and then shows the technical usage shape.

F10C is still **request-only**. It prepares the binding request. It does not show Pause UI yet.

---

## 1. Game-design view

A Pause visual surface needs two answers before it can appear:

```text
1. What should appear?
2. Where is it allowed to appear?
```

In this framework:

```text
What should appear?  = RuntimeContent
Where can it appear? = ContentAnchor
```

For Pause, the authored surface says:

```text
When Pause is Paused,
use this prefab/template,
identify it as this RuntimeContent,
and target this ContentAnchor.
```

F10C converts that authored data into a formal request:

```text
PauseVisualSurfaceContract
  -> ContentAnchorBindingRequest
```

That request is like a work order. It says:

```text
Bind runtime content X
owned by runtime owner Y
to anchor A
owned by anchor owner B.
```

The request does **not** perform the work yet.

---

## 2. Important vocabulary

| Term | Meaning for a designer |
|---|---|
| `PauseVisualSurfaceAuthoring` | Scene/component authoring for a Pause visual surface. |
| `PauseVisualSurfaceContract` | Validated data extracted from the authoring component. |
| `RuntimeContentOwner` | Who owns the future created Pause visual content. |
| `RuntimeContentId` | The id of the visual content that may be materialized later. |
| `ContentAnchorScope` | Where the anchor belongs: Route, Activity or Local. |
| `ContentAnchorOwner` | The specific owner inside that scope. Required to avoid ambiguity. |
| `ContentAnchorId` | The specific anchor point/root id. |
| `ContentAnchorBindingRequest` | The explicit request that connects runtime content identity to anchor identity. |

The key correction in F10C is the `ContentAnchorOwner`. An anchor id alone is not enough.

Example:

```text
anchorId = pause.overlay
```

is weaker than:

```text
anchorScope = Local
anchorOwner = Local:qa.pause.visual.anchor-owner
anchorKind = Root
anchorId = pause.overlay
```

The second form is the canonical binding target.

---

## 3. Authoring shape in the Inspector

Add this component to a scene object or QA object:

```text
Immersive Framework > Pause > Pause Visual Surface Authoring
```

Recommended initial values:

```text
Surface Id: pause.visual.surface
Surface Kind: OverlayRoot
Pause State: Paused
Visual Prefab: <your Pause UI prefab/template>
Reset Local Transform: true

Runtime Scope: Transient
Runtime Owner Id: pause.visual.owner
Runtime Owner Name: Pause Visual Surface
Runtime Content Id: pause.visual.content
Resource Key: pause.visual.prefab
Release Policy: MarkReleasedAndUnregister

Anchor Scope: Local
Anchor Kind: Root
Requiredness: Required
Anchor Owner Id: pause.visual.anchor-owner
Anchor Id: pause.visual.overlay
```

Use `Local` while the surface is still QA/authored. Route/Activity-owned anchors can come later when the consumer wiring is selected explicitly.

---

## 4. Minimal code shape

The current cut is request-only:

```csharp
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Pause;

public sealed class PauseBindingRequestExample
{
    public bool TryBuildRequest(
        PauseVisualSurfaceAuthoring authoring,
        out ContentAnchorBindingRequest request)
    {
        request = default;

        if (authoring == null)
        {
            return false;
        }

        if (!authoring.TryCreateContract(out var contract, out _))
        {
            return false;
        }

        var result = PauseVisualSurfaceBindingRequestFactory.Create(
            contract,
            nameof(PauseBindingRequestExample),
            "pause.binding-request.example");

        if (!result.Succeeded)
        {
            return false;
        }

        request = result.Request;
        return true;
    }
}
```

This only creates the request. It does not execute the bind.

---

## 5. What this does not do yet

F10C does not:

```text
create Pause UI;
instantiate the prefab;
move transforms;
execute RuntimeContentAnchorBinding.Bind;
register lifecycle materialization;
release content;
change InputMode;
change PlayerInput;
change Time.timeScale;
turn Pause on/off;
wire Route/Activity lifecycle.
```

That is intentional. F10C is the bridge between **authoring** and **binding request**.

---

## 6. How to test

Use the QA Canvas button:

```text
Run Pause Content Anchor Binding Request Smoke
```

Expected result:

```text
passed='True'
bindingRequest='SucceededCreated'
mismatchedContext='RejectedMismatchedRuntimeOwner'
requestOnly='True'
bindingExecution='False'
materialization='False'
```

The important check is that the request contains both sides:

```text
RuntimeContent identity
+ ContentAnchor identity
```

and that a mismatched runtime owner is rejected.

---

## 7. Next step after F10C

The next likely cut is:

```text
F10D — Pause ContentAnchor Binding Execution Proof
```

That should prove explicit logical binding execution for Pause, still without visual materialization or Pause toggle integration.
