# Setup

Use this page to wire the current package baseline in a Unity project.

## Package dependencies

`com.immersive.framework` depends on:

- `com.immersive.foundation`
- `com.immersive.logging`
- `com.unity.inputsystem`

The framework package should be installed as a Unity package. Do not copy old project assets, scenes or `ProjectSettings` into the package.

## Install from Git package

Package version: `1.0.0-preview.1`

Recommended tag after validation: `v1.0.0-preview.1`

Install private Immersive sibling packages in the consumer project's `Packages/manifest.json` before installing `com.immersive.framework`, unless those packages are available from a scoped registry.

Example consumer manifest entries:

```json
{
  "dependencies": {
    "com.immersive.foundation": "https://github.com/ImmersiveGames/com.immersive.foundation.git#v1.0.0-preview.1",
    "com.immersive.logging": "https://github.com/ImmersiveGames/com.immersive.logging.git#v1.0.0-preview.1",
    "com.immersive.framework": "https://github.com/ImmersiveGames/com.immersive.framework.git#v1.0.0-preview.1"
  }
}
```

Unity package manifests do not support Git URLs as transitive dependencies. Do not put Git URLs in `com.immersive.framework/package.json`; install private sibling packages directly in the consumer project or publish them through a compatible registry.

Package Manager flow:

1. Add `com.immersive.foundation` from Git URL/tag or registry.
2. Add `com.immersive.logging` from Git URL/tag or registry.
3. Add `com.immersive.framework` from Git URL/tag.
4. Let Unity resolve and update `Packages/packages-lock.json`.
5. Run Unity import/compile.
6. Configure Model 1.0 authoring assets.
7. Run Project Settings > Immersive Framework > Model Readiness > `Run Model Readiness Check`.

## Game Application

Create a `GameApplicationAsset` and assign:

- Startup route: a `RouteAsset`.
- Validation mode: `Strict`, `Standard` or `Release` according to the project's readiness policy.
- `UIGlobal` scene policy: `NoneConfigured` for explicit no-op visuals, or `Required` when shared visual surfaces must be loaded additively after the startup route primary scene is prepared.
- `UIGlobal` scene path/name when the policy requires it.

The runtime host loads the Game Application, then starts route/activity runtime state from the authored assets.

## UIGlobal scene

Use a dedicated `UIGlobal` scene when the project wants shared presentation surfaces.

Common contents:

- Loading surface GameObject with `UnityLoadingSurfaceAdapter`.
- Transition curtain/effect GameObject with `UnityFadeCurtainEffectAdapter`.
- Resident Pause panel with `UnityPauseResidentSurfaceAdapter`.
- Optional `PauseInputModeUnityPlayerInputRuntimeBridge` and `PauseInputActionRuntimeBridgeTrigger` when Pause input should be authored in the shared scene.

If the policy is `Required`, missing adapters can block startup diagnostics. If the policy is not configured, the framework keeps explicit no-op visual behavior.

For the minimum Model boundary, `UIGlobal` is the shared Surface Model host. It should contain expected shared adapters only; route/activity gameplay content belongs in route/activity scenes.

## Route and Activity baseline

Create route and activity assets:

- `RouteAsset` for top-level route selection.
- Primary scene on each route that should load content.
- `ActivityAsset` for route-owned activity selection.
- Optional route/activity content profiles when the project uses additive content scenes.

Route changes may use Transition and Loading surfaces. Activity changes may request visual wrapping depending on the authored activity visual transition mode.

FIRSTGAME setup is deferred until minimal authoring validation/project readiness is consolidated. Before treating a project as package-ready, validate that the active Game Application, routes, scenes, expected `UIGlobal` adapters and optional content/anchor declarations are explicit and build-loadable.

## Model readiness check

Use Project Settings > Immersive Framework > Model Readiness > `Run Model Readiness Check` after wiring the baseline.

The check is Editor-only and non-destructive. It reports:

- `totalIssues`
- `errors` as blocking issues
- `warnings`
- `info`
- `optionalSkips`

It does not create assets, assign scenes, change Build Settings or add adapters. Fix blocking issues in the owning asset or scene, then rerun the check.

## Pause input baseline

Use this path for authored Pause input:

```text
InputAction
  -> PauseInputActionRuntimeBridgeTrigger
  -> PauseInputModeUnityPlayerInputRuntimeBridge
  -> PauseRuntime
  -> InputMode request
  -> Unity PlayerInput application
```

Do not add new usage of the retired direct Pause input adapter. The supported path is the runtime bridge trigger plus the runtime bridge.

## RuntimeContent / ContentAnchor setup

For explicit materialization paths:

- Author ContentAnchor declarations with route/activity ownership.
- Use RuntimeContent identities and handles as logical content state.
- Use Unity materialization adapters/bridges for physical prefab materialization.
- Use ContentAnchor placement adapters/services for physical placement under anchor transforms.
- Use explicit release/composite release paths for cleanup; do not assume route/activity auto-materialization.
