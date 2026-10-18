# IF-PLAN-ADR-050 — ActorProfile Base Content Part Assets and Migration

Status: **Approved — implementation task-gated**<br>
Version: **v1**<br>
Date: **2026-10-18**<br>
Supplement to: [IF-PLAN-ADR-050 Actor Composition and Occurrence Lifecycle Migration v1](IF-PLAN-ADR-050-ACTOR-COMPOSITION-AND-OCCURRENCE-LIFECYCLE.v1.md)<br>
Architecture authority: [IF-ADR-050](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md)<br>
Technical specification: [IF-SPEC-050 §3.4](../Specifications/IF-SPEC-050-Actor-Lifecycle-Participation-and-Composition.md)<br>
Migration contract: [IF-ADR-050 accepted-target contract migration](../Reconciliation/IF-ADR-050-ACCEPTED-TARGET-CONTRACT-MIGRATION-2026-10-17.md)<br>
Governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)<br>
Execution and evidence: [IF-TRACK-Framework](../Tracking/IF-TRACK-Framework.md)

**Goal:** Replace the inline `ActorProfile` Base content list with ordered references to one ScriptableObject per part, migrate existing authored values without loss, and keep all runtime materialization on the existing Framework-to-Composition path.

**Architecture:** Framework owns the Unity authoring asset and ActorProfile references; the generic Composition package remains engine-independent and unchanged. The profile's canonical runtime input is the new asset-reference list. Old inline and legacy values are read only by an explicit Editor migration, never by a parallel Runtime fallback.

**Tech stack:** Unity 6.5, C#, Unity `AssetDatabase`/`SerializedObject`, Framework Editor tooling and NUnit Editor tests in the existing QAFramework project.

**Spec:** [IF-SPEC-050 §3.4](../Specifications/IF-SPEC-050-Actor-Lifecycle-Participation-and-Composition.md#34-actorprofile-base-part-serialization--accepted-authoring-decision). This plan supplements, and does not supersede or rewrite, the approved lifecycle plan v1. D08 closed on 2026-10-18; this is a separate serialized-data follow-up and does not reopen D08.

## Global constraints

- Keep one ScriptableObject asset per Base content part; ActorProfile stores an ordered list of references.
- Store dependencies as content keys, not references to other part assets. Sharing is explicit; migration never deduplicates inline values.
- Keep `com.immersive.composition` generic and engine-independent. Do not modify its repository/package or create another materialization path.
- Keep `ActorProfile` at its declared Experimental maturity. Any new C# surface requires classification under IF-GOV-001; this plan prefers an internal asset type created from the ActorProfile Inspector.
- Preserve authored keys, dependency values, prefab references, profile GUIDs and profile-selection references. A field-type change from inline serialized values to Unity object references is not an automatic Unity serialization migration.
- Keep old inline/legacy fields as migration input only until the migration gate authorizes their removal. Do not use them as Runtime fallback.
- Generate assets only under an editable project `Assets/` tree. Never write to `PackageCache`; reject unsupported/read-only paths without changing source data.
- Do not modify Stable `GameApplicationAsset`, `RouteAsset`, `ActivityAsset` or Scene-Provided authoring schemas.
- Execute implementation in the visible Framework checkout used by QAFramework. Do not create a separate Unity project/worktree for these changes.
- Do not publish/tag a package or release through this plan. Distribution requires a separate explicit release decision.
- Follow repository validation rules: Codex does not run Unity/import/EditMode tests. The Project Owner performs the listed Unity validation in the existing QAFramework project.

## Review focus

| Input or condition | Required behavior and proving task |
|---|---|
| ActorProfile with no parts or no `visual` key | Remains valid where optional visual content is supported; migration creates no placeholder asset. Test in Tasks 2 and 4. |
| Multiple inline parts with dependencies | Preserve list order, keys, prefabs and dependency values; profile validation still rejects missing keys, duplicate keys and cycles. Test in Tasks 2–4. |
| Legacy `presentationPrefab` equals an inline `visual` part | Produce one visual part asset and clear migration input only after the new reference is saved. Test in Task 4. |
| Legacy visual conflicts with an inline visual, or serialized data is malformed | Report conflict/invalid; do not change source fields or leave orphan generated assets. Test in Task 4. |
| Profile under `PackageCache` or another path not eligible for generated `Assets/` | Report blocked/unsupported without writing assets or changing the profile. Test the path guard in Task 4. |
| A part asset is referenced by multiple profiles | Treat edits as intentionally shared; migration of old inline data creates per-profile assets and does not infer sharing. Test in Tasks 2–3. |

## Baseline and scope

The implementation task must record the exact Framework HEAD, working-tree state and resolved `com.immersive.composition` candidate in IF-TRACK before code changes. This proposed plan does not freeze a source SHA.

The current source stores `ActorProfile.compositionParts` as `List<ActorCompositionPart>`, where `ActorCompositionPart` is an inline serializable class containing `key`, `prefab` and dependency keys. `legacyPresentationPrefab` is migration input. `ActorCompositionRuntimeBinding` maps profile entries into the Composer, while `ActorCompositionProfileContract`, Player provisioning and Scene-Provided validation enforce authoring constraints. Existing migration coverage is in `Tests/Editor/CameraAuthoring/ActorProfileVisualContentMigrationTests.cs`; runtime/recomposition fixtures construct inline entries in the Camera Editor test files listed below.

The new part-asset type is internal to Framework authoring and carries the key, required prefab and dependency keys. The `visual` key may be absent, but each authored part currently requires a prefab; data-only kernel definitions are outside this ActorProfile serialization cut. The new serialized ActorProfile list is named `baseContentParts` in this plan. The legacy `compositionParts` serialized shape remains available to the migration reader during the migration window; it is not consulted by Runtime. The existing obsolete `ActorProfile.VisualContentPrefab` getter continues to project the `visual` prefab from the canonical new list without changing its public signature.

## Dependency order

```text
T1 part-asset/profile schema
  ├── T2 single Runtime binding and profile validation ──┐
  ├── T3 Inspector authoring and creation flow ─────────┤
  └── T4 explicit, idempotent serialized-data migration ┘
                      ↓
             T5 fixtures and focused Editor proof
                      ↓
        T6 QAFramework and named consumer migration
                      ↓
        Update IF-TRACK; D08/release gates remain separate
```

## Ordered tasks

### T1 — Add the Framework part-asset schema

**Files:**

- Create `Runtime/Actors/ActorCompositionPartAsset.cs`.
- Modify `Runtime/Actors/ActorProfile.cs`.
- Modify `Runtime/Actors/ActorCompositionPart.cs` only as needed to retain the old serialized value shape as migration input.
- Add `Tests/Editor/CameraAuthoring/ActorProfileCompositionPartAssetTests.cs`.

**Contract:** Create an internal ScriptableObject asset type with serialized `key`, required `GameObject prefab`, and dependency-key list. `ActorProfile` stores an ordered `baseContentParts` reference list. The `visual` key remains optional; data-only ActorProfile entries are outside this cut. Do not expose the asset as a new Stable/public C# API. Create assets from the ActorProfile authoring surface. Keep old `compositionParts` and `legacyPresentationPrefab` readable to Editor migration code.

**Proof:** save/reload a profile and assert its ordered references, part values and intentionally shared asset reference survive serialization. Assert old inline data remains readable to the migration code. No automatic conversion occurs on asset load.

**Acceptance:** the source model can represent the approved schema without changing the Composition package or the profile's own asset GUID.

### T2 — Route Actor validation and Runtime through the new references

**Files:**

- Modify `Runtime/Actors/ActorProfile.cs` and `Runtime/Actors/ActorCompositionProfileContract.cs`.
- Modify `Runtime/PlayerParticipation/Runtime/ActorCompositionRuntimeBinding.cs`.
- Modify `Runtime/PlayerParticipation/Runtime/AttachedPlayerActorMaterializationAdapter.cs`.
- Modify `Runtime/PlayerParticipation/Runtime/SceneProvidedLocalPlayerComposition.cs`.
- Modify `Runtime/PlayerParticipation/Runtime/PlayerActorPreparationRuntimeHostModule.ActorOccurrence.cs`.
- Modify `Editor/PlayerParticipation/PlayerActorSelectionAuthoringValidator.cs` and `Editor/PlayerParticipation/LocalPlayerProvisioningValidator.cs`.
- Update `Tests/Editor/Camera/ActorCompositionRecompositionTests.cs` and `Tests/Editor/Camera/Assignments/ActorOccurrenceOptionalVisualContentTests.cs` fixture helpers to use part assets.

**Contract:** Convert each referenced asset into the existing kernel descriptor at the Framework integration boundary. Validate non-null references, unique/valid keys, required prefabs, dependency existence/cycles and prohibited nested Actor/PlayerInput components against the complete profile. Keep Scene-Provided's existing visual-only support limit. Runtime reads only `baseContentParts`; the old serialized list is migration-only.

**Proof:** verify optional/no-visual Actors still work, visual materializes through one Composer path, keyed recomposition retains the existing identity/revision guarantees, and Scene-Provided adoption remains non-owning. Add a regression that non-empty legacy inline data alone does not silently activate a second Runtime path.

**Acceptance:** no alternate inline materializer or compatibility fallback exists; all actor paths use the same validated profile asset references.

### T3 — Update ActorProfile authoring and validation UX

**Files:**

- Modify `Editor/PlayerParticipation/ActorSelectionProfileEditors.cs`.
- Modify `Editor/PlayerParticipation/PlayerActorSelectionAuthoringValidator.cs` and `Editor/PlayerParticipation/LocalPlayerProvisioningValidator.cs` if not completed in T2.
- Update `Documentation~/Guides/Actor-Composition-Usage.md` and `Documentation~/Guides/Player-Usage.md`.
- Update `Documentation~/API/Public-API.md` only if implementation adds or changes a public API; the intended route adds none.

**Contract:** Show Base Content Parts as an ordered asset-reference list. Provide a profile Inspector action to create a new part asset in the profile-scoped folder, initialize its fields and assign it to that profile. Allow intentional reuse by assigning an existing asset and explain in the Inspector that editing a shared asset affects every referencing profile. Keep `visual` mount and Scene-Provided authoring rules clear.

**Proof:** Inspector-created parts persist after save/reimport/reopen; validation reports missing/null asset references and existing key/dependency/prefab restrictions. Verify the profile remains usable without parts.

**Acceptance:** an author can create and assign a part without manually scripting asset creation; Inspector state reflects shared-asset behavior accurately.

### T4 — Replace the inline migration with an idempotent part-asset migration

**Files:**

- Modify `Editor/PlayerParticipation/ActorProfileVisualContentMigration.cs` (reuse the existing migration owner; do not add a second migration authority).
- Modify `Editor/PlayerParticipation/ActorSelectionProfileEditors.cs` for migration invocation and diagnostics.
- Expand `Tests/Editor/CameraAuthoring/ActorProfileVisualContentMigrationTests.cs`.

**Contract:** Migrate the old inline `compositionParts` list and any `legacyPresentationPrefab` directly to ordered `baseContentParts` references. Keep the operation explicit; do not mutate assets on load. Create a profile-scoped folder under the profile's owning `Assets/` directory and one asset per inline entry. Use deterministic filenames based on entry order and sanitized key; if a prior partial attempt left an asset at that path, reuse it only if its serialized values exactly match, otherwise report conflict without overwrite. Do not deduplicate equal entries across profiles.

Validate the complete source profile before writing. Equal legacy/inline `visual` values become one part. Conflicting visual values, duplicate keys, invalid/missing dependencies, malformed fields, unwritable paths or mismatched existing generated assets return explicit invalid/conflict/blocked results without changing the profile or leaving newly generated orphan assets. Save new assets and profile references before clearing migration-source values. Re-running the command after success creates no duplicates and returns unchanged.

**Proof cases:**

- `Migration_ConvertsInlinePartsToOrderedAssetsAndPreservesProfileAndPrefabGuids`
- `Migration_ConvertsLegacyVisualDirectlyToPartAsset`
- `Migration_MergesMatchingLegacyAndInlineVisualWithoutDuplicate`
- `Migration_ConflictLeavesProfileAndGeneratedAssetsUnchanged`
- `Migration_RejectsNonAssetsPathWithoutMutation`
- `Migration_ReusesMatchingPartialOutputAndIsIdempotent`
- `Migration_DoesNotDeduplicateEqualPartsAcrossProfiles`
- `Migration_PreservesNullProfileWithoutCreatingContent`

Assert profile IDs, display data, slot/session/default ActorProfile references, existing Prefab GUIDs, order, keys, dependency values and part-asset GUID references. Assert new part assets receive distinct GUIDs and old inline/legacy data is cleared only after successful conversion.

**Acceptance:** migration is lossless for supported inputs, repeatable, conflict-safe and explicit; no runtime fallback is needed after conversion.

### T5 — Convert test fixtures and run the focused validation gate

**Files:**

- Update the tests listed in T2 and `Tests/Editor/CameraAuthoring/ActorProfileVisualContentMigrationTests.cs`.
- Add `Tests/Editor/CameraAuthoring/ActorProfileCompositionPartAssetTests.cs` if not created in T1.
- Update the IF-ADR-050 section of `Documentation~/Architecture/Tracking/IF-TRACK-Framework.md` with the exact source and user-provided Unity evidence.

**Proof:** in the existing QAFramework Unity project, resolve the Framework from the same visible local checkout and run the focused migration, Camera Actor composition and Actor Profile Editor tests, then the full EditMode suite. Record the Test Runner result/count, Unity version, Framework revision and package source. Require successful import/compile, zero failed tests and a runner completion with no cancellation or unexpected assembly reload. The baseline screenshot reports 430/430; do not require the final count to remain 430 because this plan adds focused tests.

**Acceptance:** focused contract tests and the full existing QAFramework EditMode suite pass against the changed checkout. No new clean Unity project is required.

### T6 — Migrate identified consumers and close the serialized-data gate

**Consumers:** QAFramework, FirstGame (`planet-devourer`) and PEGA, as listed in D00 and IF-TRACK.

**Work:** After a candidate Framework checkout is imported, the Project Owner runs the explicit migration in each accessible consumer that contains affected ActorProfiles, reviews per-profile migration output, saves/reopens the profiles, and reports any profiles outside `Assets/` as blocked for a deliberate relocation decision. Verify no conflicting/malformed profile remains and the profile/slot/session selection references still point to the same GUIDs. Import/compile each consumer against the candidate; do not change consumer gameplay tests or unrelated package references.

Keep old serialized fields and the migration reader in the candidate until the Project Owner confirms the identified consumer assets are migrated. This plan does not authorize field removal, tag/publication, version promotion or consumer repository edits by Codex. A later removal/release decision must account for any additional consumers and the migration window.

**Acceptance:** known consumer profile assets have user-confirmed migration results, imports compile, and the exact candidate/revision and residual blockers are recorded in IF-TRACK. This follow-up serialization gate closes independently; D08 remains closed, and publication remains subject to its separate release gates.

## Risks and blockers

| Risk or gate | Mitigation / disposition |
|---|---|
| Inline class values to ScriptableObject references are incompatible serialized field shapes. | Keep old fields readable to explicit migration; migrate and verify before any removal. Never rely on `FormerlySerializedAs` to transform values into assets. |
| Unity asset GUIDs change for newly created parts. | Preserve profile and Prefab GUIDs, create one new GUID per part, and assert the generated reference graph after save/reimport. |
| Shared part edits affect multiple ActorProfiles. | Make reuse an explicit asset assignment; Inspector explains this; migration creates per-profile assets without dedupe. |
| Consumer assets may live outside writable `Assets/`. | Block the migration without mutation and require an owner-directed copy/relocation path. |
| Scene-Provided supports only its current visual adoption contract. | Keep that constraint; do not broaden adoption semantics in this plan. |
| Tests/consumers may still be constructing inline entries. | Search and update all Framework test fixtures; run the existing QAFramework suite after focused tests. |
| Consumer project checkouts and serialized assets are not all writable/visible in this task. | Project Owner performs migration/import there and supplies evidence; this is a validation dependency, not a blocker to authoring the migration tool. |

No material blocker to implementing this plan is currently known. Consumer evidence and explicit execution authorization are required before T5/T6 can close.

## Manual validation checklist

1. Confirm the QAFramework Manifest resolves the changed Framework checkout and Unity imports/compiles without errors.
2. Run migration tests and focused Actor Profile/Camera composition tests; review per-case outcomes.
3. Run the full EditMode suite in the existing QAFramework project; confirm no test cancellation or unexpected assembly reload.
4. In QAFramework, run migration once, inspect generated assets/references, save/reopen profiles, rerun migration and confirm `Unchanged` with no duplicate assets.
5. In FirstGame and PEGA, run migration only on affected profiles; verify counts/conflicts and import/compile, then report evidence and current package resolution.
6. Confirm Stable schemas, `com.immersive.composition`, package versions and release tags are unchanged by this implementation cut.

## Plan maintenance

After approval, this document is an immutable route. Record task status, exact source revisions, test evidence and consumer migration results only in [IF-TRACK-Framework](../Tracking/IF-TRACK-Framework.md). A material route change requires v2; do not rewrite this plan to record progress.

**Execution authorization:** The Project Owner approved this plan and authorized its implementation on 2026-10-18. Follow the ordered task gates above; pause at the QAFramework Unity validation gate. Consumer migrations, field removal, version promotion and publication remain outside this implementation authorization.
