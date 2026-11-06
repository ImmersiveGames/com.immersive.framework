# Object Entry composition authoring and runtime

Status: Experimental. B1/B2 are complete; Amendment 003 C1 exact-occurrence access is implemented on the authorized branch. Earlier focused Unity runs passed the listed C1-related classes, but the final stale-handle rerun crashed without NUnit XML and the full Edit Mode suite remains unvalidated; see the [Framework tracker](../Architecture/Tracking/IF-TRACK-Framework.md#if-adr-014-amendment-003-c1--exact-occurrence-composition-access). Prior owner-reported B2 evidence does not validate C1.

## Purpose

Object Entry remains a Framework owner-admitted physical occurrence with a stable `ObjectEntryId`. Composition is optional. Ordinary scene-authored Object Entries with only `ObjectEntryDeclaration` continue without a Composer, while an opted-in declaration can reference a reusable `ObjectEntryCompositionProfile` on that same GameObject.

The B2 runtime uses the existing `com.immersive.composition` Composer behind Framework Runtime Content. The profile is validated before the owner transaction commits; prefab-backed parts materialize only after the exact binding commits. The Composer is internal in B2, so consumers do not acquire write authority from occurrence queries.

## Authoring

1. Add/configure `ObjectEntryDeclaration` on the exact object that owns the logical identity; generate a valid Object Entry ID using the existing inspector.
2. Optionally add `ObjectEntryCompositionAuthoring` to that **same GameObject**. It does not assign identity or lifecycle ownership.
3. Optionally create an asset through **Assets > Create > Immersive Framework > Object Entry > Composition Profile** and reference it in the component. A Profile is required for authored Base parts unless Allow Runtime Contributions is enabled; with that opt-in, profileless access is supported.
4. Configure one or more keyed prefab-backed parts in the profile. Each part has a unique `key`, a prefab and optional dependencies referencing other part keys in that profile.
5. Review validation in the profile and component Inspectors. Invalid identity, invalid/duplicate keys, missing prefabs, invalid dependencies, dependency cycles and forbidden nested authority components are reported. A missing Profile is an error only when required by the selected authoring mode.

The first authored part descriptor is **prefab-backed**. It represents a subordinate object installed under the existing declaration root; it does not replace or reidentify the root. `ContentKey` and authored dependencies are passed to the Composer. Parts are not general-purpose scene managers or new Object Entries. Directly authored scene components do not require composition. The capability component can operate whether a scene author placed it directly or a profile supplied it.

When the owner releases or replaces the occurrence, Framework invalidates further writes, closes the Composer and confirms acquired Unity objects are destroyed before the existing Route/Activity scene release continues. If cleanup is still pending or fails, the binding record remains available for retry and the physical release is blocked. Synchronous host teardown can invalidate writes immediately but cannot await this barrier; normal confirmation requires the existing async lifecycle path.

If Composer close encounters an acquired part containing its protected Object Entry root, the context remains closed and writes stay invalidated, while physical cleanup remains pending. Framework keeps the binding and retries guarded acquired-part releases only after the root is no longer inside the acquired hierarchy. Other close faults retain their diagnostics and continue to block physical release.
## Dynamic contribution access (C1)

When Allow Runtime Contributions is enabled, the committed exact occurrence exposes an Experimental ObjectEntryCompositionAccessGrant. The grant is unavailable before owner commit and, when a Profile exists, before its Base activation succeeds. It is bound to that physical binding and its current composition context; it does not resolve the target again by logical ID.

Each authorized TryOpenContribution creates an independent empty contribution. For profileless authoring, the Composer is created lazily on the first open. C1 has no add/replace/remove-part operation and does not materialize content. CloseAsync closes only that consumer contribution intent; Framework owner release remains responsible for physical cleanup. Neither the grant nor Composer determines Player selection or grants gameplay authorization.

## Boundaries

- `ObjectEntryDeclaration` owns identity metadata only. `ObjectEntryCompositionAuthoring` and the profile are optional and separate.
- The profile cannot produce a Framework owner, a committed physical binding or permission to mutate an occurrence. Runtime admission and lifecycle remain owned by the existing Framework binding registry.
- B2 installs the authored Profile as the initial composition for an admitted occurrence. C1 adds optional exact-occurrence access to open and close empty contribution intents; mutating contribution contents to add, replace or remove parts remains deferred to later cuts. Query/recognition results remain read-only and do not grant contribution access.
- Nested prefabs with `ObjectEntryDeclaration`, `ObjectEntryCompositionAuthoring`, `ActorDeclaration`, `PlayerActorRuntimeHost` or `PlayerInput` are deliberately rejected for this narrow first authoring surface.
- Effective physical coverage is Route/Activity only. Session and dynamic Object Entry admission are excluded.
- The composition kernel remains `com.immersive.composition`, engine-free; no Actor composition assets were changed.

## Validation status

**B1:** authoring and Editor validation are implemented. The Project Owner's QAFramework Test Runner screenshot (2026-11-05) reports **511/511 Edit Mode PASS** and **10/10 PASS** in `ObjectEntryCompositionAuthoringTests`; no failures. This is owner-reported visual evidence, without an NUnit XML report or source-commit fingerprint. It does not validate the B2 runtime path.

**B2:** Framework PR #21 and QAFramework PR #1 are merged; final follow-ups are tracked by Framework PR #22 (protected-root cleanup and retry) and QAFramework PR #2 (evidence documentation). The Project Owner supplied a Unity 6 Test Runner screenshot showing **518/518 Edit Mode PASS, 0 failed**, including the corrected cleanup regressions, and a Console transcript ending in `[IF-ADR-014-DEMO] status='Passed' cases='10/10'`. The Play Mode transcript verifies exact-root/identity preservation, old/new Activity occurrences, parts destroyed while owner scenes remained loaded, and successful Route replacement. Earlier **514/517** failures are superseded by this full-suite result. These are owner-executed tests on the B2 checkout, not Codex-executed tests; no machine-readable NUnit report or source-SHA fingerprint was provided. Build/import succeeded with non-blocking CS0618/CS0067 warnings. Technical validation is supported for the tested B2 scope; merge status is tracked by the linked follow-up PRs. See the [Framework tracker](../Architecture/Tracking/IF-TRACK-Framework.md#object-entry-composition-b2-implementation-2026-11-05).

**Remaining C scope:** C1 provides exact-occurrence handles for empty contributions. Runtime add/replace/remove of parts while retaining the active occurrence remains deferred to later cuts. Session admission and dynamic spawning/transfers are also excluded.

**C1 validation — Codex execution:** StableObjectBindingTests previously completed 60/60, ObjectEntryCompositionAuthoringTests 13/13 and ActorGroupRuntimeTests 12/12. These runs preceded the final extra stale-handle assertion. The rerun after that assertion ended in a native Unity Editor crash (exit -536870911) before NUnit XML was written. Focused Controlled Stop, Host Teardown and the full Edit Mode run each ended with an internal Unity Test Runner NullReferenceException in EditModeRunTask (exit 3, no NUnit XML). No final rerun, full-suite result or C1 Play Mode result is claimed. See the tracker for the exact evidence boundary.
