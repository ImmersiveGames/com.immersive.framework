# IF-PLAN-ADR-045 — Runtime-Safe Scene References

Status: **Approved route — implementation pending**<br>
Version: **v1**<br>
Created: **2026-10-09**<br>
Plan authority: [IF-ADR-045](../ADRs/IF-ADR-045-Persistent-Content-Scene-Reference-Player-Builds.md)<br>
Supersedes: none<br>
Superseded by: none

IF-ADR-045 is Accepted. This route is immutable; execution progress belongs in the Framework tracker. This document does not start implementation in the current baseline cut.

## Baseline revisions

- Framework code baseline: `fca1dd130bbd9a953526c6b7b21fedead4ab1e07` (`master`, `origin/master`).
- QAFramework baseline: `55c30d477c58432ffc3a6dcf3194020620e82070` (`main`, `origin/main`, clean worktree).
- The accepted ADR and this plan are currently local uncommitted documentation in the Framework worktree and are not contained in the Framework baseline SHA. Before implementation is dispatched to another checkout/host, make these accepted documents available there and record the exact integrated Framework/QA revisions in the tracker.

## Purpose

Implement runtime-safe Persistent Content scene references, consistent path-first scene identity, and read-only build validation, then prove the result in QAFramework and Player Builds. Preserve IF-ADR-008 ownership/lifetime and IF-ADR-040 composition contracts. Keep the Player Admission comparison risk outside this implementation.

## Boundaries and non-goals

- **Framework Runtime:** owns path/name identity and scene load, lookup, composition, unload and compensation behavior.
- **Framework Editor:** owns the explicit legacy asset migration, Scene Picker projection, authoring validation and Player build gate.
- **QAFramework:** owns focused technical regressions and synthetic build-input cases.
- **Consumer/Player validation:** proves actual Standalone and WebGL startup and navigation; Play Mode is insufficient evidence for serialized Player references.
- Do not change Persistent Content ownership, application scope, lifetime, root hierarchy or template model.
- Do not refactor Scene Composition, change IF-ADR-040, or alter Player Admission discovery/name matching.
- Do not create a generic scene registry/coordinator, mutate scenes or scene lists in the build gate, or immediately remove Stable `ContainerScene`.

## Ordered cuts

### Cut 0 — Baseline and implementation entry

**Boundary:** Framework and QAFramework tracker/evidence.<br>
**Work:** Use the baseline revisions above; before the first implementation change, make the accepted ADR/plan available to the target checkout, record its integrated revision, and capture current Persistent Content startup, Route replacement, additive content, unload, and Single Load terminal behavior.<br>
**Integration/evidence:** QAFramework baseline is clean at the recorded SHA. Preserve these revisions as the comparison point.<br>
**Acceptance:** Exact source revisions and reproducible pre-change observations are recorded; cleanup returns to `BaselineRestored`.

### Cut 1 — Persistent Content serialization and migration

**Boundary and files:**

- Runtime: `Runtime/Authoring/PersistentContentComposition.cs`, `Runtime/Authoring/GameApplicationAsset.cs`, `Runtime/GlobalUi/GlobalUiSceneRuntime.cs`, `Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs`.
- Editor: `Editor/Authoring/GameApplicationAssetEditor.cs`, new `Editor/Authoring/PersistentContentSceneReferenceMigration.cs`, `Editor/Authoring/ContentProfileSceneReferenceSynchronizer.cs`, `Editor/Validation/FrameworkAuthoringValidator.cs`.
- Tests: new `Tests/Editor/PersistentContent/` fixtures.

**Implementation:** Add canonical serialized `scenePath` and cached `sceneName`; derive completeness from strings. Keep the Scene Picker, displaying the asset resolved from path and writing path/name. Add an idempotent explicit Editor migration command that scans all `GameApplicationAsset` assets without opening Inspectors, converts the legacy object field using `AssetDatabase.GetAssetPath`, preserves the Stable property/field as a deprecated compatibility projection, and emits per-asset results. Runtime load uses the path. Extend move/rename synchronization to Persistent Content. Coordinate `GlobalUiSceneRuntime` with `FrameworkRuntimeHost`: keep the exact source scene loaded until Session-scope composition succeeds; on rejection, release Session bindings and restore roots to the source before failing bootstrap; only then transfer roots to application lifetime and unload the source.

**Tests:** Path-only asset; legacy asset migrates without Inspector; repeat migration is idempotent; invalid/non-scene legacy reference is reported and left intact; picker assignment and scene move/rename keep path/name coherent; Stable `ContainerScene` signature returns its retained legacy object snapshot or null for path-only assets, while runtime never reads it; authoring validation rejects legacy-only/unresolved data; Session-scope composition failure releases bindings, leaves no orphaned application-lifetime roots, and preserves the exact source scene for diagnosis/retry.

**Acceptance:** Framework compiles/imports; migration preserves references/data across save/reload; runtime no longer checks or loads through `ContainerScene`; successful startup preserves application-scoped roots and shutdown release behavior, while failed Session composition leaves roots/source recoverable. No data loss or build-list mutation.

### Cut 2 — Scene Lifecycle identity

**Boundary and files:** `Runtime/SceneLifecycle/SceneLifecycleRuntime.cs`, result/diagnostic types only if required, and new `Tests/Editor/SceneLifecycle/` fixtures.

**Implementation:** Align `TryGetLoadSceneIdentifier`, `FindLoadedScene`, `IsSceneMatch`, `IsSceneLoaded`, load and unload. Explicit path means path-only load/lookup/unload; name identity is allowed only with no path and one unique candidate. Validate effective-build membership and Unity loadability before releasing/replacing the current Primary Scene. For Single Load, emit the ADR-defined terminal states; recompose the prior exact scene only if Unity confirms it remains loaded. If Unity removed it, report the post-transition scene/participant state without claiming rollback or speculatively unloading the new scene. Keep participant sequencing and local rollback per IF-ADR-040.

**Tests:** Exact path success; invalid path with homonymous scene; stale/moved path; disabled/not-in-build path; unique and ambiguous name-only cases; already-loaded scene; additive post-load failure; release callback failure; exact unload; Single Load rejection before release; operation-start failure; failure while previous scene remains; failure after Unity unloads prior scenes; target resolution/activation/Available failure. Assert no wrong-scene acceptance or false rollback claim; terminal state records exact loaded/active scenes and participant availability. Restoration is guaranteed only when the same previous Scene remains loaded and its Available pass succeeds.

**Acceptance:** Resolution is deterministic; explicit paths never use same-name fallback; preflight rejection leaves the current scene and participants untouched; every post-start Single Load failure has a truthful terminal state and exact scene/participant diagnostics; IF-ADR-040 lifecycle ordering and idempotency regressions pass.

### Cut 3 — Read-only Player build validation

**Boundary and files:** new `Editor/Build/FrameworkPlayerBuildProcessor.cs` (or the existing Editor build folder/assembly), a focused helper in `Editor/Validation/`, and focused Editor build-validation tests. Reuse existing authoring checks only where their owner and semantics fit.

**Implementation:** Use Unity 6 `BuildPlayerProcessor.PrepareForBuild(BuildPlayerContext)` and scheduled `BuildPlayerOptions.scenes` as the effective input. This covers normal Build Profiles (profile override or shared/global list) and custom scheduled scene arrays without guessing from `EditorBuildSettings.scenes`. Apply Unity's semantics to an empty options list; never substitute another list. Validate the Application dependency closure in IF-ADR-045, including missing, invalid, ambiguous and unmigrated references and absent scenes. Fail with owner, field/entry, authored path/name and reason. The gate is read-only and does not run migration.

**Tests:** Active profile override; shared/global fallback; custom options containing/omitting a required scene; empty list; profile-disabled scene omitted from scheduled options; custom list explicitly including a profile-disabled scene; invalid asset; path/name ambiguity; unmigrated Persistent Content. Assert no asset or `EditorBuildSettings` mutation.

**Acceptance:** Callback validates the scheduled paths; failures block with actionable diagnostics; valid configuration proceeds; active profile/global scene list and assets remain unchanged.

### Cut 4 — QAFramework regression integration

**Boundary and files:** QAFramework `Assets/QA-IF-ADR-045/` focused setup/scenario assets, applicable QA build scene configuration, QA execution docs/tracker.

**Implementation and integration:** Add a focused scenario for Persistent Content startup, path identity, additive/Route/Activity lifecycle and cleanup. Pin the Framework revision used. Assert existing Player Admission/IF-ADR-040 behavior is unchanged; do not alter it.

**Acceptance:** Targeted Editor/Play Mode lanes prove wrong-name scenes are not loaded/accepted, exact-path unload works, Persistent Content roots survive Route transitions, and cleanup restores scene/profile state. Evidence is reproducible and ends `BaselineRestored`, without scene-list, scene, participant or callback leaks.

### Cut 5 — Player Build smoke and consumer proof

**Boundary:** QAFramework or a named consumer project, build inputs and evidence. No package Runtime/Editor changes unless a defect is found and reopened as a new implementation cut.

**Integration:** Build Standalone and WebGL using the intended production Build Profile and actual scheduled scene set. In each Player, verify bootstrap, Persistent Content, startup Route, `RouteRequestTrigger`, a second Route, Activity-owned content and unload/lifetime. Capture logs and platform/build inputs. Re-run consumer WebGL proof from title screen to Menu.

**Acceptance:** Both Players start and navigate; no missing reference/scene or uninitialized Game Flow; Persistent Content ownership/lifetime is correct; results are tied to exact package and consumer revisions.

## Contract and compatibility gates

- `scenePath` is the only runtime identity when populated. `sceneName` is display/diagnostic cache and legacy identity only when path is absent.
- Runtime result properties preserve authored path/name for diagnostics and report which identity was used.
- `PersistentContentComposition.ContainerScene` keeps its public signature through `1.x`; in migration it is documented deprecated, retains the legacy serialized object snapshot, returns that snapshot or null for path-only authoring, and is not read by Framework runtime. `ContainerSceneName`, `HasContainerScene` and `IsComplete` keep their signatures and derive from serialized path/name strings. Document this behavior change for consumers. Do not add a compiler obsolete warning until known consumers are migrated and warning compatibility is verified. Removal/signature changes are not authorized in `1.x`.
- The deprecated serialized object field is migration/compatibility input only. Inspector presentation is reconstructed from path; runtime never treats the object as authority.
- Game Application ownership, application lifetime and IF-ADR-040 `Available` / `Releasing` participant ordering remain unchanged.
- Pre-build validation consumes scheduled options and fails only; migration is an explicit separate Editor operation.
- Player Admission's path-or-name test remains unchanged and outside this plan.

## Required validation matrix

| Domain | Required cases | Evidence type |
|---|---|---|
| Serialized migration | Current path-only asset; legacy object-only asset; repeated migration; missing/invalid reference; save/reload; rename/move | Framework EditMode tests plus manual Editor asset inspection |
| Persistent runtime | startup load, application-lifetime roots, source scene unload, host/session shutdown release | Framework tests and QAFramework Play Mode |
| Identity resolution | explicit valid/invalid path; same-name different path; name-only unique/ambiguous; loaded/unloaded; additive/single; exact unload | Framework targeted tests plus QAFramework Play Mode |
| Failure compensation | unavailable target before operation; additive post-load composition failure; unload failure; Single Load rejection, old scene retained, old scene unloaded, target resolution/activation/composition failure | Framework tests with exact terminal scene/participant state and diagnostics |
| Build gate | Build Profile override; shared/global list; custom options list; empty options; omitted/disabled/missing scene; legacy-only app; read-only guarantee | Editor build validation tests and a manually inspected build invocation |
| Player | Standalone and WebGL startup + route request + Route/Activity content transition + cleanup | Actual Player smoke logs/build evidence, not Play Mode |
| Consumer | Existing WebGL title-to-Menu flow | Consumer proof tied to exact package revision |

## Risks and manual decisions

- `ContainerScene` remains through `1.x`; any later removal requires a separate breaking-release decision. QAFramework consumers must migrate their authoring assertions to path/name before compiler deprecation warnings are enabled.
- The build gate validates serialized, discoverable Game Application/Route/Activity targets. Routes referenced only from arbitrary game code are not inferable from package assets; the consumer must include those scenes in the scheduled build list.
- Unity Single Load can unload every loaded scene. This contract does not promise reconstruction of unloaded scenes; tests must prove diagnostics and participant recovery for the exact state Unity leaves. Do not call a failed transition rolled back when its prior scene was unloaded.
- Player Admission name fallback is tracked outside this plan because changing it is not required for IF-ADR-045 and IF-ADR-040 explicitly preserves its distinct candidate-discovery contract.
- The reported consumer WebGL failure remains unconfirmed until Player Build smoke and consumer proof complete.

## Change policy

IF-ADR-045 authorizes the architecture, but this baseline task did not begin implementation. Do not rewrite this plan to track progress; record status and evidence in `Documentation~/Architecture/Tracking/IF-TRACK-Framework.md`. Any material architecture change requires a new plan version or ADR amendment, as appropriate.
