# F10H — Pause Logical Toggle to Resident Surface Usage

This guide explains the production-facing Pause visual flow after F10H.

## Simple model

For a normal game Pause menu, keep the visual UI resident in `UIGlobal`.

```text
UIGlobal Scene
  Canvas
    Pause Panel
      UnityPauseResidentSurfaceAdapter
```

When Pause changes state, the framework applies a `PauseSnapshot` to the resident adapter.

```text
Running -> panel hidden
Paused  -> panel visible
Running -> panel hidden
```

## Runtime flow

The runtime flow is:

```text
FrameworkRuntimeHost.RequestPause(Pause / Resume / Toggle)
  -> PauseRuntime updates logical state
  -> PauseSnapshot is refreshed
  -> PauseSurfaceRuntime applies the snapshot
  -> UnityPauseResidentSurfaceAdapter shows/hides the existing panel
```

## Setup checklist

1. Configure a persistent `UIGlobal` scene in the Game Application when the project is ready to use a real global UI scene.
2. Put the Pause panel in that scene.
3. Add a `CanvasGroup` to the Pause panel or a parent surface object.
4. Add `UnityPauseResidentSurfaceAdapter` to the Pause panel/surface object.
5. Set the adapter's `Surface Root` to the panel root.
6. Set the adapter's `Canvas Group` to the panel `CanvasGroup`.
7. Keep the panel hidden by default.

## What the adapter owns

`UnityPauseResidentSurfaceAdapter` owns only presentation of an existing resident surface:

- active/inactive root state;
- canvas alpha;
- raycast blocking;
- interactable state.

It does not own:

- input;
- `Time.timeScale`;
- Route or Activity lifecycle;
- materialization;
- ContentAnchor binding;
- scene loading.

## What the game designer configures

A designer should think of this as:

```text
This panel exists in the global UI.
When the game is paused, show it.
When the game resumes, hide it.
```

No prefab spawn is required for the canonical Pause menu.

## What comes next

After F10H, the likely next production step is connecting an explicit input source to `RequestPause(Toggle)`.

That should still be separate from the visual adapter:

```text
Input action -> logical Pause request -> resident Pause surface presentation
```
