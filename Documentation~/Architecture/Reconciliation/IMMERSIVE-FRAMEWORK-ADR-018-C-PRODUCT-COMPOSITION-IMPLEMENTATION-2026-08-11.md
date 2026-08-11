# Immersive Framework — ADR-018-C Product Composition Implementation

**Date:** 2026-08-11  
**Type:** UX/product + runtime composition  
**Package Git baseline inspected:** `6ab02072f89ca988f28c95f58aee018064626a12` (`ADR018-B`)  
**Prerequisite architecture:** ADR018-A/B certified

## Objective

Make Progression Save authorable as an application feature without exposing backend
implementation mechanics as the normal workflow.

## Scope

```text
Game Application enablement/profile reference
Progression Save Profile
Built-in JSON selection
Custom Provider selection
typed provider asset
explicit store materialization
application-scoped ProgressionSaveRuntime ownership
boot rejection on invalid selected backend
Game Application Inspector
Profile Inspector
validation/status
Advanced / Debug
```

## Out of scope

```text
Snapshot orchestration
autosave
save/load UI
slot browser
global runtime accessor
service locator
singleton manager
FIRSTGAME gameplay binding
third-party vendor implementation
```

## Files created

```text
Runtime/ProgressionSave/Authoring/*
Runtime/ApplicationLifecycle/FrameworkRuntimeHost.ProgressionSave.cs
Editor/ProgressionSave/*
Documentation~/Architecture/Reconciliation/
  IMMERSIVE-FRAMEWORK-ADR-018-C-PRODUCT-COMPOSITION-IMPLEMENTATION-2026-08-11.md
```

## Files edited

```text
Runtime/Authoring/GameApplicationAsset.cs
Runtime/Bootstrap/FrameworkBootValidator.cs
Runtime/ApplicationLifecycle/FrameworkRuntimeHost.FrameRate.cs
Editor/Authoring/GameApplicationAssetEditor.cs
ADR018 architecture/tracking documents
```

## Files removed

None.

## Product surface

### Game Application

```text
Progression Save
  Enabled
  Default Progression Save Profile
  Configuration
  Create/Open/Replace
```

### Profile

```text
Backend
  Built-in JSON
  Custom Provider

Configuration Status

Advanced / Debug
  backend selection
  runtime ownership
  fallback = None
  technical provider/storage evidence
```

## Expected flow

Built-in:

```text
Create/Open Game Application
  -> enable Progression Save
  -> Create Progression Save Profile
  -> Backend = Built-in JSON
  -> Validate
  -> Play
```

Custom:

```text
create vendor/custom ProgressionSaveStoreProviderAsset
  -> Profile Backend = Custom Provider
  -> assign Provider
  -> Validate
  -> Play
```

## Runtime composition

```text
Framework boot
  -> validate Profile
  -> ProgressionSaveApplicationComposition.Resolve
  -> Profile materializes selected IProgressionSaveStore
  -> ProgressionSaveRuntime
  -> FrameworkRuntimeHost owns for application lifetime
```

## Failure semantics

```text
Custom Provider selected
provider missing/invalid/fails/null/invalid BackendId
  -> Rejected
  -> boot fails explicitly
  -> no Built-in JSON fallback
```

## Why no Apply/Rebuild

There is no generated scene/prefab technical graph.

The authored Profile is resolved into runtime state once at boot.

Adding Apply/Rebuild would create ceremony without an Editor materialization problem.

## Technical acceptance

```text
package compiles
Stable IProgressionSaveStore unchanged
no reflection
no global service locator
no singleton manager
no Resources-based provider discovery
invalid required configuration fails explicitly
Custom Provider failure never becomes JSON
runtime host owns exactly one resolved composition
runtime does not depend on Editor
```

## Product acceptance

```text
user can enable feature from Game Application
user can create/select Profile
user can understand selected backend
user can configure custom provider explicitly
Inspector shows configuration status
Advanced/Debug exposes technical evidence
short usage documentation exists
```

## Remaining product question

Gameplay access/injection is intentionally not frozen in this cut.

FIRSTGAME must prove the most usable explicit binding shape before the package adds a
game-facing runtime injection surface.

## Suggested commit

```text
feat(progression-save): add application backend authoring
```
