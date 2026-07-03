# F10D — Pause ContentAnchor Binding Execution Usage

This guide explains the Pause ContentAnchor binding execution step in practical terms.

F10D is still not visual Pause. It does not instantiate a Pause menu prefab. It only proves that the future Pause visual content can be logically bound to the correct anchor.

---

## 1. Designer explanation

Think of the Pause menu as a prop that will later appear on a stage.

Before creating the prop, the framework now proves it can make a reservation:

```text
Pause UI Overlay
  is allowed to use
Pause Overlay Anchor
```

That reservation is the binding.

In plain terms:

```text
The framework knows what Pause wants to show,
and it knows exactly where that Pause visual content is allowed to attach.
```

No visual object is created yet.

---

## 2. Authoring pieces

The authored Pause surface still starts with:

```text
PauseVisualSurfaceAuthoring
```

This creates:

```text
PauseVisualSurfaceContract
```

The contract contains two sides:

```text
RuntimeContent side:
- runtime owner
- runtime content id
- resource key / prefab reference
- release policy

ContentAnchor side:
- anchor scope
- anchor owner
- anchor kind
- anchor id
```

The important F10C correction still applies: `anchor owner` is required. Anchor id alone is not enough.

---

## 3. What binding execution needs

To execute binding, four things must exist:

```text
1. A valid PauseVisualSurfaceContract.
2. A RuntimeContent scope root/context for the contract owner.
3. A ContentAnchorSet containing the requested anchor.
4. The host-owned ContentAnchor binding runtime.
```

F10D does not create lifecycle wiring automatically. The QA smoke creates these conditions explicitly.

---

## 4. What happens during execution

The execution flow is:

```text
PauseVisualSurfaceContract
  -> PauseVisualSurfaceBindingRequestFactory
  -> ContentAnchorBindingRequest
  -> RuntimeContentRuntime.DeclareHandle
  -> FrameworkRuntimeHost.BindContentAnchor
  -> ContentAnchorContentHandle
```

Meaning:

```text
The future Pause visual content receives a logical runtime handle.
That handle is then bound to the target ContentAnchor.
```

Still no prefab instantiation happens.

---

## 5. What this unlocks next

After F10D, the framework has proven:

```text
Pause can author a visual surface.
Pause can derive a binding request.
Pause can execute a logical anchor binding.
```

The next implementation can safely focus on visual materialization:

```text
Create the Pause UI prefab
place it at the bound ContentAnchor
release it explicitly
```

---

## 6. What not to assume yet

Do not assume these exist after F10D:

```text
Pause menu appears on screen automatically.
Pause toggle opens/closes the UI.
InputMode changes.
Time.timeScale changes.
Route/Activity auto-release exists.
Route/Activity auto-materialization exists.
```

F10D is a logical binding proof only.
