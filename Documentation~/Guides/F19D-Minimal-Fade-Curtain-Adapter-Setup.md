# F19D — Minimal Fade/Curtain Adapter Setup

Status: F19D applied / manual visual setup guide  
Scope: Transition Effects / Unity Adapter boundary

This guide explains how to manually inspect the F19D fade/curtain adapter in a Unity scene. The canonical smoke does not require a saved scene object because it creates a transient QA surface at runtime.

## 1. What F19D adds

Runtime adapter contract:

```text
Runtime/TransitionEffects/ITransitionEffectAdapter.cs
```

Minimal Unity adapter:

```text
Runtime/TransitionEffects/UnityFadeCurtainEffectAdapter.cs
```

QA smoke:

```text
Run Unity Fade Curtain Effect Adapter Smoke
```

The adapter only changes a configured `CanvasGroup` and optional surface root active state. It does not animate, use DOTween, own Transition lifecycle, own Gate blockers, load scenes or register itself globally.

## 2. Canonical validation path

No scene setup is required for the canonical F19D smoke.

Run:

```text
Immersive Framework QA > Core Smokes > Run Unity Fade Curtain Effect Adapter Smoke
```

Expected steps:

```text
QA Smoke started. name='Unity Fade Curtain Effect Adapter Smoke'.
QA Unity Fade Curtain Effect Adapter Smoke step completed. step='adapter-created' ...
QA Unity Fade Curtain Effect Adapter Smoke step completed. step='visible-state-applied' ...
QA Unity Fade Curtain Effect Adapter Smoke step completed. step='hidden-state-applied' ...
QA Unity Fade Curtain Effect Adapter Smoke step completed. step='required-missing-surface-blocks' ...
QA Unity Fade Curtain Effect Adapter Smoke step completed. step='optional-unsupported-kind-nonblocking' ...
QA Smoke completed. name='Unity Fade Curtain Effect Adapter Smoke'.
```

## 3. Optional manual visual setup

Use this only when you want to see the curtain surface in the Scene/Game view.

### Scene

Open the scene that already contains your QA framework setup, for example:

```text
Assets/.../StartupScene.unity
```

Use the real project-relative scene location. Do not use absolute local paths in docs or package setup.

### GameObject

Create a GameObject for the surface:

```text
QA_TransitionFadeSurface
```

Add these components:

```text
CanvasGroup
Unity Fade Curtain Effect Adapter
```

Assign fields in `UnityFadeCurtainEffectAdapter`:

| Field | Value |
|---|---|
| Adapter Name | `QA Fade Curtain Adapter` |
| Effect Kind | `Fade` or `Curtain` |
| Canvas Group | the `CanvasGroup` on `QA_TransitionFadeSurface` |
| Surface Root | `QA_TransitionFadeSurface` |
| Set Surface Root Active | enabled |
| Hidden Alpha | `0` |
| Visible Alpha | `1` |
| Block Raycasts When Visible | enabled |
| Interactable When Visible | disabled |
| Apply Hidden State On Awake | enabled |

### Optional visual child

For an actual black screen curtain, create a UI Canvas/Image under this object using normal Unity UI tooling. The framework adapter does not require or reference `Image`; it only controls `CanvasGroup`.

Suggested hierarchy:

```text
QA_TransitionFadeSurface
  Canvas
    FullscreenBlackImage
```

Configure `FullscreenBlackImage` manually as a full-screen black rectangle. This is visual authoring only, not framework core.

## 4. Manual context-menu check

Select `QA_TransitionFadeSurface` and use the component context menu:

```text
Immersive Framework/QA Apply Visible Curtain State
Immersive Framework/QA Apply Hidden Curtain State
```

Expected behavior:

| Action | Expected state |
|---|---|
| Apply Visible | `CanvasGroup.alpha = 1`, raycasts blocked, surface root active |
| Apply Hidden | `CanvasGroup.alpha = 0`, raycasts unblocked, surface root inactive if configured |

## 5. What not to create in F19D

Do not create yet:

```text
Transition Effect ScriptableObject profile
Transition Effect registry
DOTween adapter
loading screen prefab as canonical framework asset
Pause overlay
input mode switch
Route/Activity integration
```

Those are future cuts. F19D only proves the minimal Unity adapter boundary.

## 6. Failure semantics

Required fade/curtain with missing `CanvasGroup` must report explicit failure and block:

```text
status='Failed' blocksTransition='True' issues='1'
```

Optional unsupported kind must report explicit rejection but not block:

```text
status='Rejected' blocksTransition='False' issues='1'
```

No fallback success is allowed.
