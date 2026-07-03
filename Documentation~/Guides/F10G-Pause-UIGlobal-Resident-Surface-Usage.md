# F10G — Pause UIGlobal Resident Surface Usage

## What this guide is for

Use this guide when you want a normal production Pause menu.

The canonical model is:

```text
Pause UI already exists in UIGlobal.
Pause logic changes state.
The resident Pause surface shows or hides itself.
```

This is different from the optional materialized path from F10E. F10E proved that Pause UI can be spawned through RuntimeContent + ContentAnchor, but that is not the default product path.

## Game designer mental model

Think of `UIGlobal` as the place where app-level UI lives.

Typical examples:

```text
Loading screen
Transition overlay
Pause menu
Global blocker overlay
```

For Pause, the simplest production setup is:

```text
UIGlobal Scene
└─ Canvas
   └─ Pause Panel
      └─ UnityPauseResidentSurfaceAdapter
```

When the framework enters logical Pause, the adapter shows the panel. When Pause returns to Running, the adapter hides it.

## Setup steps

### 1. Create or open the UIGlobal scene

Use your canonical `UIGlobal` scene. In the `GameApplicationAsset`, set:

```text
Global UI Scene Policy = Required
UIGlobal Scene = your UIGlobal scene
```

The framework loads this scene before the startup Route and persists its roots under the runtime host.

### 2. Create the Pause visual hierarchy

In the `UIGlobal` scene, create a resident UI object, for example:

```text
Canvas
└─ Pause Panel
   ├─ Background
   ├─ Title
   ├─ Resume Button
   ├─ Options Button
   └─ Quit Button
```

The exact UI layout is project-owned. The framework only needs a surface it can show/hide.

### 3. Add a CanvasGroup

Add `CanvasGroup` to the Pause Panel root.

The adapter uses it to control:

```text
alpha
blocksRaycasts
interactable
```

### 4. Add the resident Pause adapter

Add:

```text
Immersive Framework / Pause / Unity Pause Resident Surface Adapter
```

Recommended setup:

```text
Surface Root = Pause Panel
Canvas Group = Pause Panel CanvasGroup
Set Surface Root Active = true
Apply Hidden State On Awake = true
Hidden Alpha = 0
Visible Alpha = 1
Block Raycasts When Paused = true
Interactable When Paused = true
```

### 5. Start hidden

The Pause Panel should be hidden at startup. The adapter can apply this automatically on Awake when `Apply Hidden State On Awake` is enabled.

## What the adapter does

The adapter receives a `PauseSnapshot` from `PauseSurfaceRuntime`.

When the snapshot is Paused:

```text
Pause Panel active = true
CanvasGroup alpha = 1
CanvasGroup blocksRaycasts = true
CanvasGroup interactable = true
```

When the snapshot is Running:

```text
CanvasGroup alpha = 0
CanvasGroup blocksRaycasts = false
CanvasGroup interactable = false
Pause Panel active = false
```

## What the adapter does not do

It does not:

```text
instantiate prefabs
use ContentAnchor binding
use RuntimeContent materialization
change InputMode
read PlayerInput
change Time.timeScale
own Route/Activity lifecycle
save/load state
control camera/audio/gameplay
```

This keeps Pause presentation simple and production-facing.

## When to use the materialized path instead

Use the F10E materialized path only for explicit advanced cases, such as:

```text
route-specific Pause skin
activity-specific Pause UI
streamed UI package
DLC UI variant
temporary QA visual surface
runtime-swapped presentation module
```

Do not use materialization just to show a normal Pause menu.

## QA validation

Run:

```text
Run Pause UIGlobal Resident Surface Smoke
```

Expected intent:

```text
initial hidden state works
paused snapshot shows the resident surface
running snapshot hides it again
no materialization happens
no ContentAnchor binding happens
no InputMode or Time.timeScale policy changes happen
```
