# Framework Usage

Status: Current
Last updated: 2026-07-23

## Create and author

1. Create a `GameApplicationAsset`.
2. Assign the ordered Player Slot configuration and explicit product policies
   needed by the game.
3. Create `RouteAsset` and `ActivityAsset` assets for the application flow.
4. Configure startup Route, primary/additive scenes, Activity participation,
   transition and gate policies.
5. Provide the `UIGlobal` scene surfaces required by the selected modules.
6. Use the package bootstrap surface to start the application.

Required configuration is not repaired by hidden lookup. Missing bindings return
typed diagnostics and block the owning operation.

## Runtime model

```text
GameApplicationAsset
-> bootstrap
-> internal FrameworkRuntimeHost
-> Session
-> Route lifecycle
-> Activity lifecycle
-> scoped content and feature modules
```

The host is a composition root, not a public service locator. Runtime components
receive narrow feature ports through bootstrap or lifecycle composition.

## Apply and rebuild

Only product surfaces with derived technical materialization expose
Apply/Rebuild. Camera is the mature example. Do not expect Apply/Rebuild to
invent required application policy, identity or runtime ownership.

## Diagnose

- Start with Inspector validation and Advanced/Debug evidence.
- Use typed result/status/issue fields rather than parsing log text.
- Inspect the current Route, Activity, readiness and feature snapshots.
- Fix the owner that failed to supply a required dependency.
- Do not add name lookup, scene search, fallback objects or static access.

## Real-game boundary

The framework owns lifecycle, scoped content, feature authority and diagnostics.
The game owns objectives, interactions, win/loss rules and content. QAFramework
proves synthetic behavior; FIRSTGAME proves that the official package surface is
usable without consumer facades.

## Manual validation

After runtime or serialized changes:

1. Import and compile the package in Unity.
2. Import and compile QAFramework.
3. Run only the focused QA suites for the affected owner.
4. Exercise startup, Route/Activity exit and re-entry.
5. Verify diagnostics contain no missing binding or retained-scope error.
6. Validate FIRSTGAME when the changed surface is product-facing.
