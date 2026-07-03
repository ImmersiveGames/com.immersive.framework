# First Practical Flow Transition

Use this guide to configure the first playable flow that uses the existing framework surfaces:

```text
startup/menu -> gameplay route -> activity switch -> activity clear/restore
```

This guide does not create a new runtime path. It uses `GameApplicationAsset`, `UIGlobal`, `RouteAsset`, `ActivityAsset`, current Route/Activity request entry points and the existing Transition/Loading surfaces.

F57 inserts a Model/Authorship gate before FIRSTGAME. Use this guide as the practical flow shape, but treat minimal authoring validation/project readiness as the next required gate before creating sample assets or claiming a playable framework baseline.

## Required Project Setup

Create or confirm these pieces:

- `GameApplicationAsset`: selects the startup `RouteAsset` and, when shared UI surfaces are needed, uses `GlobalUiScenePolicy.Required`.
- `UIGlobal` scene: loaded before the startup route and persisted by `FrameworkRuntimeHost`.
- Transition surface/adapter: usually `UnityFadeCurtainEffectAdapter` in `UIGlobal`.
- Loading surface/adapter: `UnityLoadingSurfaceAdapter` in `UIGlobal` when the flow also needs loading/progress presentation.
- Gameplay `RouteAsset`: declares the primary scene for the gameplay route.
- Initial `ActivityAsset`: assigned as the route startup activity when the route should enter gameplay state immediately.
- Activity targets: additional `ActivityAsset` entries for switching state/content inside the active route.
- Request caller: `RouteRequestTrigger`, `ActivityRequestTrigger`, QA Canvas or another existing caller that reaches `FrameworkRuntimeHost`.

Do not place global Transition, Loading or Pause presentation inside route/activity content scenes unless the project has a deliberate local presentation reason. The first practical path expects shared surfaces in `UIGlobal`.

## Flow Model

Think about the flow this way:

- Route changes the scene/area.
- Activity changes state or content inside the active route.
- ActivityClear removes the current activity while keeping the route context.
- Transition visually covers the before/after change.
- Loading communicates loading or progress when there is loading work to show.

Transition is not Loading. A fade can hide a change, but it does not report progress, load scenes or decide readiness. Loading can show progress, but it does not replace the visual envelope that Transition provides.

## Configuration Checklist

Before testing the flow:

- `GameApplicationAsset.StartupRoute` points to the startup/menu route or directly to the gameplay route.
- `GameApplicationAsset.GlobalUiScenePolicyValue` is `Required` when the flow depends on shared Transition/Loading/Pause surfaces.
- `GameApplicationAsset.GlobalUiScenePath` points to the canonical `UIGlobal` scene.
- The `UIGlobal` scene is included in Build Settings.
- `UIGlobal` contains a valid `UnityFadeCurtainEffectAdapter` or another `ITransitionEffectAdapter`.
- If loading/progress is expected, `UIGlobal` also contains a valid `UnityLoadingSurfaceAdapter`.
- The gameplay `RouteAsset` has a primary scene path.
- The gameplay `RouteAsset` has a startup `ActivityAsset` when gameplay should begin with an activity.
- Activity switching targets use valid `ActivityAsset` references.
- Activity content profiles are assigned only when activity-owned scenes/content should be composed or released.
- Scene-authored triggers or QA Canvas call Route/Activity requests through `FrameworkRuntimeHost`; they should not call `GameFlowRuntime` directly.

## First Test Sequence

Use the smallest sequence that proves the surfaces:

1. Boot the app with the `GameApplicationAsset`.
2. Request the gameplay route.
3. Confirm the route primary scene becomes active or loaded as expected.
4. Confirm the route startup activity becomes active when configured.
5. Request a second activity inside the same route.
6. Clear the current activity.
7. Request the initial activity again to restore the gameplay state.

For a first pass, use QA Canvas or existing `RouteRequestTrigger` / `ActivityRequestTrigger` buttons rather than creating new sample assets or runtime code.

## Expected Route and Activity Logs

For a configured `UIGlobal` with one fade adapter, Route and Activity request logs should keep the old Transition fields and add the F55 evidence fields.

Expected healthy values for a before/after transition are:

```text
transition='SucceededWithUnitySurface'
transitionScope='Route'
transitionEffect='Fade'
transitionEffectAdapterCount='1'
transitionEffectAdapterEvidenceCount='2'
transitionEffectAdapterEvidenceFailed='0'
transitionEffectAdapterEvidenceStatuses='Succeeded, Succeeded'
```

For Activity operations, `transitionScope` should be `Activity`. For clearing activity state, `transitionScope` should be `ActivityClear`.

The evidence count is commonly `2` because the runtime records before and after Transition Effect adapter evidence. If policy or operation shape changes, use the statuses and names fields to understand what actually ran rather than assuming a fixed count.

Useful fields:

- `transition`
- `transitionScope`
- `transitionBefore`
- `transitionAfter`
- `transitionBlockingIssues`
- `transitionVisual`
- `transitionEffect`
- `transitionEffectBefore`
- `transitionEffectAfter`
- `transitionEffectBlockingIssues`
- `transitionEffectAdapterCount`
- `transitionEffectAdapterEvidenceCount`
- `transitionEffectAdapterEvidenceApplied`
- `transitionEffectAdapterEvidenceSkipped`
- `transitionEffectAdapterEvidenceFailed`
- `transitionEffectAdapterEvidenceBlockingIssues`
- `transitionEffectAdapterEvidenceNames`
- `transitionEffectAdapterEvidenceStatuses`

When Loading is involved, inspect Loading separately:

- `loadingAdapterEvidenceCount`
- `loadingAdapterEvidenceApplied`
- `loadingAdapterEvidenceSkipped`
- `loadingAdapterEvidenceFailed`
- `loadingAdapterEvidenceBlockingIssues`
- `loadingAdapterEvidenceNames`
- `loadingAdapterEvidenceStatuses`

## Common Problems

Transition surface missing:
Check that `GameApplicationAsset` loads `UIGlobal`, that the scene is included in Build Settings and that the `UIGlobal` runtime reports Transition adapters.

Adapter missing or disabled:
Check for `UnityFadeCurtainEffectAdapter` or another `ITransitionEffectAdapter` in `UIGlobal`. `transitionEffectAdapterCount='0'`, failed evidence or missing adapter status points to setup, not route lifecycle.

Loading works but Transition does not:
Loading and Transition are separate surfaces. A valid `UnityLoadingSurfaceAdapter` does not prove a valid `UnityFadeCurtainEffectAdapter`.

ActivityClear without an active Activity:
ActivityClear can only clear current activity state. If no activity is active, inspect the Activity result and `transitionScope='ActivityClear'` diagnostics before changing Transition.

Loading confused with Transition:
Loading communicates progress/readiness. Transition covers visual change. Do not expect Transition to expose loading progress, and do not expect Loading to provide fade before/after evidence.

Expecting Transition to load scenes:
Transition does not load route primary scenes, compose activity content, release content or decide readiness. Route, Activity, SceneLifecycle, RuntimeContent and ContentAnchor own those domains.

## Not a Goal Yet

This guide does not:

- create final game visuals;
- create a prefab;
- create a new adapter;
- create a generic Surface layer;
- create a public GameFlow API;
- change Pause visual behavior;
- change save/progression;
- replace Route/Activity lifecycle documentation;
- add a QA Canvas button.
- bypass the minimum Model/Authorship validation gate.

## Validation Checklist

For doc-only adoption, no Unity validation is required. For an actual project flow, validate manually:

1. Unity import/compile succeeds.
2. Standard Smoke passes.
3. Activity Baseline Smoke passes.
4. Route Scene Composition Smoke passes.
5. Route Release Smoke passes.
6. Transition Smoke passes.
7. Transition Effect Smoke passes.
8. Transition Effect Unity Fade Curtain Smoke passes when using `UnityFadeCurtainEffectAdapter`.
9. Route/Activity request logs show the expected `transition*` and `transitionEffectAdapterEvidence*` fields.

Use failed evidence as setup evidence first. Do not add fallback, service lookup or a new runtime API to hide missing authoring configuration.
