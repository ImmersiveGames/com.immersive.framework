# F10E — Pause Visual Materialization Usage

This guide explains what changes after the Pause ContentAnchor binding execution proof.

F10E is the first cut where the Pause visual prefab is actually instantiated and placed under a ContentAnchor. It is still explicit and QA-only. It does not make the Pause button open a menu automatically.

---

## 1. Designer explanation

Think of the Pause UI as a prop.

Previous cuts proved the paperwork:

```text
F10B: describe the Pause visual prop.
F10C: create the request saying where it wants to attach.
F10D: reserve/bind the target ContentAnchor logically.
```

F10E proves the physical step:

```text
Create the Pause visual prefab
and place it under the requested Pause ContentAnchor.
```

In practical terms:

```text
Pause Overlay prefab
  appears under
Pause Overlay Anchor
```

The framework now proves that the authored Pause visual surface can become a real Unity object at the correct anchor.

---

## 2. What must exist

To materialize a Pause visual surface, the framework needs:

```text
1. PauseVisualSurfaceAuthoring / PauseVisualSurfaceContract
2. RuntimeContent scope root/context for the Pause visual owner
3. ContentAnchorSet containing the requested anchor
4. Unity Transform for the physical anchor
5. UnityRuntimeMaterializedObjectRegistry for physical evidence
6. FrameworkRuntimeHost with RuntimeContent and ContentAnchor binding runtimes
```

The important authored fields are:

```text
RuntimeContent side:
- runtime scope
- runtime owner
- runtime content id
- visual prefab / resource key
- release policy

ContentAnchor side:
- anchor scope
- anchor owner
- anchor kind
- anchor id
```

Anchor owner remains required. Anchor id alone is not enough.

---

## 3. Execution flow

The F10E proof executes this path:

```text
PauseVisualSurfaceContract
  -> PauseVisualSurfaceBindingRequestFactory
  -> ContentAnchorBindingRequest
  -> RuntimeContent materialization request
  -> Unity prefab instantiation
  -> logical RuntimeContent materialized handle
  -> ContentAnchor binding
  -> physical placement under the anchor Transform
```

That means the visual surface is now:

```text
created physically
registered logically
bound to the anchor
parented under the anchor Transform
```

---

## 4. What F10E does not do

F10E does not add product Pause behavior yet.

Do not assume:

```text
Pressing Escape opens the visual Pause menu.
Pause toggle is connected to this materializer.
InputMode changes when paused.
PlayerInput is redirected.
Time.timeScale changes.
Route/Activity auto-materialization exists.
Route/Activity auto-release exists.
```

F10E is still an explicit materialization proof.

---

## 5. Cleanup in QA

The QA smoke materializes the visual surface and then explicitly cleans it up.

That cleanup proves the smoke leaves no object, binding or handle behind, but it is not lifecycle wiring.

Expected cleanup evidence:

```text
smokeCleanupPhysicalRelease=True
smokeCleanupLogicalRuntimeContentRelease=True
smokeCleanupContentAnchorBindingCleanup=True
```

The runtime should still report:

```text
automaticLifecycleWiring=False
routeActivityAutoMaterialization=False
routeActivityAutoRelease=False
inputModeChange=False
timeScalePolicy=False
```

---

## 6. What this unlocks next

After F10E, Pause has a complete explicit visual chain:

```text
author contract
binding request
logical binding execution
visual materialization
explicit cleanup
```

The next safe step is to prove visual release as its own consumer-level operation, then decide how Pause state/toggle should request materialization and release.
