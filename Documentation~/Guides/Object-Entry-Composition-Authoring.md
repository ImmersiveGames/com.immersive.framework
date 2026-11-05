# Object Entry composition authoring and runtime

Status: **Experimental**. B1 authoring and B2 internal runtime integration are authored; Unity compile/import and QAFramework Play Mode validation remain pending. Decision: [IF-ADR-014 Amendment 002](../Architecture/ADRs/IF-ADR-014-AMENDMENT-002-OBJECT-ENTRY-PHYSICAL-COMPOSITION.md).

## Purpose

Object Entry remains a Framework owner-admitted physical occurrence with a stable `ObjectEntryId`. Composition is optional. Ordinary scene-authored Object Entries with only `ObjectEntryDeclaration` continue without a Composer, while an opted-in declaration can reference a reusable `ObjectEntryCompositionProfile` on that same GameObject.

The B2 runtime uses the existing `com.immersive.composition` Composer behind Framework Runtime Content. The profile is validated before the owner transaction commits; prefab-backed parts materialize only after the exact binding commits. The Composer is internal in B2, so consumers do not acquire write authority from occurrence queries.

## Authoring

1. Add/configure `ObjectEntryDeclaration` on the exact object that owns the logical identity; generate a valid Object Entry ID using the existing inspector.
2. Optionally add `ObjectEntryCompositionAuthoring` to that **same GameObject**. It does not assign identity or lifecycle ownership.
3. Create an asset through **Assets > Create > Immersive Framework > Object Entry > Composition Profile** and reference it in the component.
4. Configure one or more keyed prefab-backed parts in the profile. Each part has a unique `key`, a prefab and optional dependencies referencing other part keys in that profile.
5. Review the validation result in both the profile and component Inspectors. Missing profile, invalid identity, invalid/duplicate keys, missing prefabs, missing/duplicate dependencies, dependency cycles and forbidden nested authority components are reported as errors.

The first authored part descriptor is **prefab-backed**. It represents a subordinate object installed under the existing declaration root; it does not replace or reidentify the root. `ContentKey` and authored dependencies are passed to the Composer. Parts are not general-purpose scene managers or new Object Entries. Directly authored scene components do not require composition. The capability component can operate whether a scene author placed it directly or a profile supplied it.

When the owner releases or replaces the occurrence, Framework invalidates further writes, closes the Composer and confirms acquired Unity objects are destroyed before the existing Route/Activity scene release continues. If cleanup is still pending or fails, the binding record remains available for retry and the physical release is blocked. Synchronous host teardown can invalidate writes immediately but cannot await this barrier; normal confirmation requires the existing async lifecycle path.

## Boundaries

- `ObjectEntryDeclaration` owns identity metadata only. `ObjectEntryCompositionAuthoring` and the profile are optional and separate.
- The profile cannot produce a Framework owner, a committed physical binding or permission to mutate an occurrence. Runtime admission and lifecycle remain owned by the existing Framework binding registry.
- B2 installs the authored profile as the initial composition for an admitted occurrence. Public runtime contribution/write access for adding, replacing or removing parts while that occurrence remains active is a later cut; query/recognition results do not grant that access.
- Nested prefabs with `ObjectEntryDeclaration`, `ObjectEntryCompositionAuthoring`, `ActorDeclaration`, `PlayerActorRuntimeHost` or `PlayerInput` are deliberately rejected for this narrow first authoring surface.
- Effective physical coverage is Route/Activity only. Session and dynamic Object Entry admission are excluded.
- The composition kernel remains `com.immersive.composition`, engine-free; no Actor composition assets were changed.

## Validation status

**B1:** authoring and Editor validation are implemented. The Project Owner's QAFramework Test Runner screenshot (2026-11-05) reports **511/511 Edit Mode PASS** and **10/10 PASS** in `ObjectEntryCompositionAuthoringTests`; no failures. This is owner-reported visual evidence, without an NUnit XML report or source-commit fingerprint. It does not validate the B2 runtime path.

**B2:** Framework runtime binding, Unity prefab materializer, exact-record guard, async close/reconcile barrier, deferred Activity scene retry, and isolated Edit Mode test cases are authored. The QAFramework scenario covers Activity Restart/readmission to a new physical root, Activity release during Route replacement and Route release while preserving each physical root. The existing Host controlled-stop fixture includes a composed Route Object Entry and checks cleanup before same-Host release retry. Neither package tests nor the QAFramework scenario have been executed in Unity in this session. Compilation, import, Play Mode integration and certification remain **unconfirmed**. See the [Framework tracker](../Architecture/Tracking/IF-TRACK-Framework.md#object-entry-composition-b2-implementation-2026-11-05) for the evidence boundary and manual validation steps.

**C:** public live contribution access for add/replace/remove while retaining the active occurrence is not part of B2 and remains pending API review. Session admission and dynamic spawning/transfers are also excluded.
