# F10F — Pause Presentation Model Usage

This guide explains when to use resident UIGlobal Pause UI versus runtime-materialized Pause UI.

---

## 1. The simple rule

For the normal product Pause menu, prefer this:

```text
Resident UIGlobal Pause Surface
```

Meaning:

```text
The Pause menu already exists in the UIGlobal scene.
The framework does not need to instantiate it when Pause starts.
Pause only asks the resident surface to show or hide.
```

This is the most designer-friendly path.

---

## 2. Why not always materialize Pause?

Runtime materialization is useful, but it adds complexity:

```text
create prefab
bind to anchor
place physically
track runtime handle
release physical object
cleanup binding
release logical handle
```

That is correct for content that truly appears dynamically.

For a standard Pause menu, designers usually want the UI to be permanently available in the UI scene and only hidden until needed.

So the normal flow should be:

```text
Pause state changes to Paused
  -> resident UIGlobal Pause surface receives presentation state
  -> surface shows the already-authored panel
```

---

## 3. When resident UIGlobal is the right choice

Use resident UIGlobal when:

```text
- every route/activity should have the same Pause menu;
- designers need to edit the Pause hierarchy directly;
- the menu must be immediately available;
- the visual surface is part of the game's standard UI shell;
- show/hide animation is enough;
- the menu should not be loaded/spawned per activity.
```

Typical examples:

```text
Pause menu
Options menu
Confirm quit panel
Controller disconnected panel
System message overlay
```

---

## 4. When materialization is still useful

Use runtime materialization when the UI is not always resident:

```text
- optional UI module;
- DLC/streamed UI package;
- route-specific pause skin;
- activity-specific presentation variant;
- temporary debug/QA overlay;
- UI that should only exist while a feature is active.
```

Typical examples:

```text
Special combat pause overlay
Photo mode UI loaded only in photo mode
DLC-specific radial menu
Temporary tutorial overlay
```

---

## 5. What F10E proved

F10E proved this advanced path works:

```text
Pause visual contract
  -> ContentAnchor binding
  -> prefab materialization
  -> physical placement
  -> explicit cleanup
```

That proof is still valuable because many future systems need this same path.

But it should not force the standard Pause menu to be spawned.

---

## 6. What the next implementation should prove

The next technical proof should be the resident path:

```text
F10G — Pause UIGlobal Resident Surface Contract Proof
```

It should prove:

```text
A Pause surface exists in UIGlobal.
The surface can receive Pause presentation state.
The surface can show/hide without materialization.
No InputMode, PlayerInput or Time.timeScale policy changes yet.
```

---

## 7. Current boundary

Do not assume yet:

```text
Escape opens Pause UI.
Pause changes InputMode.
Pause changes Time.timeScale.
Pause owns Activity or Route lifecycle.
Route/Activity auto-materializes Pause UI.
Route/Activity auto-releases Pause UI.
```

Those are future cuts.
