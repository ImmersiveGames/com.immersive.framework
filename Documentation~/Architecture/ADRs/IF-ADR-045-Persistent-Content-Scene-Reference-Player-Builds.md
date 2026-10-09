# IF-ADR-045 — Runtime-Safe Scene References and Deterministic Scene Identity

Status: **Accepted — implementation and validation pending**<br>
Proposed: **2026-10-09**<br>
Accepted: **2026-10-09**<br>
Last reconciled: **2026-10-09**<br>
Type: architecture / scene identity / Persistent Content / Player Build correctness<br>
Related decisions: IF-ADR-001, IF-ADR-006, IF-ADR-008, IF-ADR-040<br>
Implementation plan: [IF-PLAN-ADR-045 — Runtime-Safe Scene References](../Plans/IF-PLAN-ADR-045-RUNTIME-SAFE-SCENE-REFERENCES.v1.md)

This decision concerns authored scene references and scene resolution. It does not change the ownership, lifetime or composition model defined by IF-ADR-008 and IF-ADR-040.

## Context and evidence

The current Framework has two authored reference shapes:

- `RouteAsset`, `RouteContentSceneEntry` and `ActivityContentSceneEntry` serialize `scenePath` and cached `sceneName` strings.
- `PersistentContentComposition` serializes `UnityEngine.Object containerScene`. The Inspector assigns a `SceneAsset`; `GlobalUiSceneRuntime` checks the object and loads/resolves by its name.

The Framework authoring validator can inspect the Persistent Content Scene and the active scene list, but the inspected path is derived from the object reference. The validator is user-invoked; no package pre-build validation hook was found in the audited Framework source. QAFramework assets also serialize the legacy `containerScene` object reference.

The consumer team reported a WebGL startup failure in which Persistent Content could not resolve. The available source and QA project establish the runtime dependency and serialized shape, but this audit did not reproduce the reported Player deserialization failure. The incident is therefore **consumer-reported, not independently Player-reproduced**. The Player Build risk remains open until migration and Player smoke evidence exist.

The audit also confirmed a source-level identity inconsistency in `SceneLifecycleRuntime`: `TryGetLoadSceneIdentifier` can fall back from a supplied path to a name, while `FindLoadedScene` and `IsSceneMatch` treat a supplied path as authoritative. With an unavailable path and a different build scene sharing the cached name, the code can initiate a load for that other scene and subsequently reject it when resolving by path. This conditional flow is **confirmed in source**; no Unity runtime reproduction was run.

## Decision

### Scene identity

1. A non-empty `scenePath` is the authoritative identity of an authored scene.
2. `sceneName` is presentation/diagnostic data and is a legacy load identity only when `scenePath` is empty.
3. When a path is present, an invalid, stale, missing, disabled or unavailable path fails explicitly. The runtime MUST NOT load or accept another scene by matching `sceneName`.
4. Load, already-loaded lookup, composition, active-scene matching and unload MUST use the same identity rule. Unload MUST target the exact resolved `Scene` handle selected by that rule; it MUST NOT re-resolve a path-backed scene by name.
5. When path is empty and legacy name resolution is used, resolution MUST be deterministic. If the effective build set or loaded scenes contain more than one candidate with that name, the operation fails with a diagnostic instead of selecting an arbitrary scene.
6. Diagnostics identify the authored path, cached name, operation and failure stage. They MUST distinguish an absent authoring reference from an authored scene that is missing from the effective build set.

An explicit path is not silently downgraded to name identity. No scene is loaded by name merely because the path cannot be loaded.

### Persistent Content serialization and ownership

`PersistentContentComposition` SHALL serialize a project-relative `scenePath` and a cached `sceneName`. The path is the sole runtime identity and source of truth. The name supports presentation, diagnostics and the explicitly limited no-path legacy rule.

The Game Application Inspector SHALL retain the Scene Picker experience. It resolves the displayed `SceneAsset` from the stored path and writes path/name when a scene is selected. Any retained `containerScene` serialized field is a deprecated migration/compatibility projection only: it MUST NOT decide `HasContainerScene`, completeness, loading, lookup or unload. New authoring derives the picker value from `scenePath`; it does not establish a competing serialized authority.

Runtime continues to load the authored Persistent Content scene, preserve its complete authored root hierarchies for application lifetime, unload its source scene after roots are retained, and release the Session composition at host shutdown. This ADR changes the reference representation only; it does not move ownership from the Game Application or alter Persistent Content lifetime.

### Existing asset migration and Stable API window

Existing Game Application assets contain only the serialized `containerScene` object reference. Conversion MUST be possible without opening each asset's Inspector:

1. Provide an explicit, idempotent Editor migration command that scans `GameApplicationAsset` assets.
2. For each legacy reference, use `AssetDatabase.GetAssetPath` to derive the scene path and the scene asset name; validate that the reference resolves to a `.unity` asset before writing.
3. Write `scenePath`/`sceneName`, preserve the scene reference as a deprecated compatibility projection during the migration window, and report per-asset success/failure. Do not change the consumer scene or any Build Profile/Shared Scene List.
4. The migration command is the only operation that converts legacy serialized data. The pre-build gate below is read-only: it fails with the migration command and asset path when required scene data is still legacy-only or invalid.

`[FormerlySerializedAs]` alone is insufficient: renaming a serialized field does not convert a Unity object reference into a project-relative path and name.

`PersistentContentComposition.ContainerScene` is Stable. Preserve its public signature throughout the `1.x` series. During the migration window, preserve the serialized object field and Editor compatibility projection so existing assets and source consumers (including QAFramework setup) continue to compile and can inspect their prior reference. The explicit migration writes `scenePath`/`sceneName` without deleting or rewriting the legacy object field; it is repeatable and does not require opening an Inspector. New path-authored assets may have no legacy object reference.

After migration, the getter's exact compatibility semantics are: return the serialized legacy Unity object reference when one exists; otherwise return `null`. It is a deprecated snapshot for Editor tooling/source compatibility only, may be null even when a valid `scenePath` exists, and MUST NOT be used to determine completeness, resolve identity, load, lookup, compose or unload. Keep `ContainerSceneName`'s signature and have it return serialized `sceneName`; `HasContainerScene` and `IsComplete` derive from the canonical strings (path, or the permitted non-empty legacy name when path is absent), never from the object. Thus new path-authored assets are complete with a null `ContainerScene`, and a migrated name-only legacy reference remains explicit string data. The runtime MUST NOT read the old object getter/field. This makes the old member non-authoritative without synthesizing a second reference from the path.

Document the deprecation in XML/API documentation during `1.x`. Do not add a compiler `[Obsolete]` attribute in the first migration cut because it creates warnings for existing Stable consumers and QAFramework; add that attribute only after known consumers have migrated and warning compatibility has been verified, still within `1.x` if safe. Keep all public signatures. The revised serialized-string semantics for `ContainerSceneName`, `HasContainerScene` and `IsComplete` are required for path-only assets and must be called out as behavior changes in migration notes. Removal of `ContainerScene` or any public signature change is not authorized in `1.x`; any later removal requires a breaking-release decision and consumer migration window.

### Scene Lifecycle consistency and compensation

`TryGetLoadSceneIdentifier`, `FindLoadedScene`, `IsSceneMatch` and `UnloadSceneAsync` SHALL implement the same path-first rule:

- With a path: validate effective-build availability and Unity loadability by that path only, before releasing current-scene participants; resolve and compare by path; unload the exact resolved `Scene` handle. An unavailable path fails before any current-primary release callback or scene replacement.
- Without a path: use the legacy name only if it uniquely identifies one candidate in the effective build/loaded scene set. Ambiguous or unavailable names fail explicitly.
- After an asynchronous operation starts, success is not established until the loaded scene is resolved to the requested identity. Composition callbacks run only for that resolved scene.

`SceneLifecycleRuntime` owns compensation for its Route/Activity scene operations because it owns the load operation, identity resolution, lifecycle notifications and unload operation. If a newly loaded additive scene cannot be accepted or composed, it releases any composition already acquired and unloads only the exact scene instance started by that operation. It MUST NOT unload a same-name scene that was already loaded or belongs to another owner.

Persistent Content has a separate load owner and compensation boundary: `GlobalUiSceneRuntime` loads the authored scene and manages root transfer/source-scene unload; `FrameworkRuntimeHost` owns Session-scope `ComposeSessionScope` and `ReleaseSessionScope`. The source scene MUST remain available until Session-scope composition succeeds. If composition fails, the host releases/rolls back the Session scope and the loader restores the authored roots to that still-loaded source scene; bootstrap fails with both composition and cleanup diagnostics. Only after successful composition may the roots be moved to application lifetime and the source scene unloaded. A post-load identity failure before root transfer unloads only the exact scene instance started by that Persistent Content operation. This preserves IF-ADR-040's participant contract while preventing orphaned persistent roots.

#### Single Load failure contract

`LoadSceneMode.Single` is destructive: Unity unloads all currently loaded scenes as part of the operation. Releasing the active scene's participants before starting the load does not make the engine scene replacement transactional, and `AsyncOperation.isDone` alone does not prove that the requested scene was resolved, activated or composed.

The operation SHALL distinguish these observable terminal outcomes in its internal result/diagnostic:

- **RejectedBeforeTransition:** target path/name is absent, invalid, ambiguous, not in the effective build set, or cannot be loaded; validation occurs before participant release. The current loaded/active scene state and participant availability remain untouched.
- **FailedAfterTransitionCurrentSceneRestored:** the Unity operation failed after release began, the previous exact scene is still loaded, and its participant availability was successfully re-established. The requested load remains failed.
- **FailedAfterTransitionRecoveryIncomplete:** Unity changed or removed prior scene state, or participant restoration failed. Report requested identity, operation stage, active scene and loaded-scene identities, prior-scene presence, and each release/recomposition diagnostic. Do not report rollback or success.
- **LoadedAndComposed:** only after the exact requested scene is loaded, active, and its `Available` notifications succeed.

The minimum guaranteed compensation is participant re-availability for the exact previous scene only when Unity confirms that same scene remains loaded. If Single Load has unloaded it, the runtime does not claim to reconstruct it automatically and MUST NOT hide the failed transition as success. Reloading/reconstructing the previous scene is not part of this contract: it can itself fail and cannot restore arbitrary additive scenes or external state. A future recovery mechanism requires separate evidence and must preserve IF-ADR-040 release/reentry semantics. If the new scene loaded but activation/composition fails, report that exact post-transition state and do not unload unrelated or pre-existing scene instances as speculative rollback.

Before release, validate the explicit path against the effective build set and `Application.CanStreamedLevelBeLoaded(path)`; for no-path legacy identity, prove there is one candidate and loadability. Then notify `Releasing` on the current active scene and start Single Load. After completion, resolve the exact requested identity, activate it, and only then notify `Available`. Composition rejection follows IF-ADR-040 local rollback. Tests must inject/observe failure before start, operation-start failure, failure after old-scene removal, resolution failure, activation failure, participant release failure, and participant availability failure, asserting terminal result, loaded/active scenes, participant state and diagnostics.

IF-ADR-040 participant release/reentry and compensation rules remain authoritative for composition; no Scene Composition refactor, generic coordinator or new global scene scanner is introduced.

### Player Admission boundary

`SceneLocalPlayerAdmissionRuntimeHostModule` currently accepts a candidate when its scene path **or** scene name matches an Activity Content declaration. This can admit a different same-name scene when both paths are present. It is a recorded identity risk, not part of the Persistent Content or Scene Lifecycle fix.

IF-ADR-040 explicitly keeps Player admission/candidate discovery outside the common Scene Composition binding model because it must discover candidates in already-loaded scenes, including scenes loaded outside Framework Scene Lifecycle, and handle lazy binding. IF-ADR-045 MUST NOT replace that behavior with managed-root-only discovery or otherwise change IF-ADR-040. The Player Admission comparison is out of implementation scope for this ADR; track it for a separate decision if its name-fallback contract is to change.

### Pre-build validation

Every Player build SHALL run a read-only gate before build data is produced. In Unity 6 the gate SHALL inspect the scheduled `BuildPlayerOptions.scenes` supplied by `BuildPlayerProcessor.PrepareForBuild`, not mutate `EditorBuildSettings.scenes` and not assume the editor's displayed list is the build input.

The effective list already reflects Build Profile selection for the normal Build Profiles workflow, including a profile override versus the shared/global scene list. For custom build pipelines, the scheduled `BuildPlayerOptions.scenes` is the authority. An empty custom list follows Unity's build semantics and is validated as such; it MUST NOT be silently replaced with the Framework's preferred list.

The gate validates the Game Application's declared startup dependency closure: Persistent Content, Startup Route primary scene, that Route's declared Route Content scenes, and its Startup Activity's declared Activity Content scenes. Any Route/Activity targets discovered in scenes included in the scheduled build are validated against the same effective list. Scene references reachable only from arbitrary game code cannot be inferred by the Framework; consumers own inclusion of such dynamic targets in the scheduled build list.

For each required scene, the gate reports the owning asset/entry, authored path/name, and whether the reference is absent, invalid/missing, ambiguous under the legacy name rule, or absent from the scheduled scene list. In the normal Build Profiles workflow, a disabled profile/shared-list entry is absent from that scheduled list and fails. A custom `BuildPlayerOptions.scenes` list is authoritative even if it explicitly includes a scene whose profile checkbox is disabled. It fails the build on required errors. It does not open/save consumer scenes, migrate assets, rewrite authoring data, enable/add scenes, or change profile/shared lists. Migration is a separate explicit command.

This is one Framework-owned Editor build gate and its focused validation helper, not a shared package or general build orchestration subsystem.

## Accepted scope

- Runtime-safe Persistent Content path/name storage with an explicit legacy asset migration.
- Consistent path-first identity through load, lookup, composition, match and unload.
- Deterministic legacy name-only behavior and actionable diagnostics.
- Read-only Player build validation against the scheduled effective scene list.
- Preservation of Game Application ownership, Persistent Content lifetime, Stable API compatibility during the migration window, and IF-ADR-040's composition contract.

## Rejected / out of scope

- Changing Persistent Content ownership, lifetime, root hierarchy or Scene Template product model.
- Refactoring Scene Composition or Player admission/discovery.
- Automatically editing consumer assets or scene lists during validation/build.
- A generic scene registry, service, global scanner or build coordinator.
- Treating Editor Play Mode as Player Build evidence.
- Immediate removal of Stable `ContainerScene` or claiming the consumer-reported Player failure is reproduced by this audit.

## Consequences

- A stale path no longer risks loading a different homonymous scene; it fails before replacing the current primary scene.
- Existing Game Application assets need a one-time Editor migration before the read-only build gate accepts them.
- The path-backed identity model applies consistently to all Framework-managed authored scenes; name-only compatibility is narrow and deterministic.
- External code that reads Stable `ContainerScene` requires a deprecation period and later explicit breaking-release decision.
- Custom build pipelines are checked against their actual scheduled scene list, including omissions not visible from the active profile list.
- Player Admission name fallback remains a separately tracked risk.

## Current evidence / implementation coverage

- Source confirmed: Persistent Content currently stores a direct `UnityEngine.Object` reference and runtime loads/resolves by name (`PersistentContentComposition`, `GlobalUiSceneRuntime`).
- Source confirmed: Route and Activity Content store path/name strings and authoring inspectors write them.
- Source confirmed: `SceneLifecycleRuntime` has path/name fallback in load identifier selection but strict path lookup/match afterward.
- Source confirmed: authoring validators inspect `EditorBuildSettings.scenes` when invoked; no Framework pre-build gate was found in the audited source.
- QAFramework has Editor/Play Mode route/activity/lifecycle scenarios, but no coverage found for this mismatch, legacy migration, custom scheduled scene lists or Player Build startup.
- Consumer-reported WebGL failure remains **not independently reproduced**. No Player Build was run for this reconciliation.

## Implementation and validation gates

1. Implement each boundary in the linked plan; acceptance does not mean implementation or validation is complete.
2. Prove migration of legacy assets without Inspector interaction and prove the read-only build gate rejects unmigrated assets.
3. Prove duplicate-name/path mismatch rejection and no wrong-scene acceptance. Exercise every Single Load terminal state described above in Unity tests before changing that flow.
4. Pass Standalone and WebGL Player smoke plus consumer proof before marking the reported failure resolved.
