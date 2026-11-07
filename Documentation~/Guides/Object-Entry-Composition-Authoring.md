# Object Entry composition authoring and runtime

Status: **Experimental API, with C1–C3 integrated and C4 technically validated in the tested Route/Activity scope.** Project Owner evidence: C3 full **566/566 Edit Mode PASS** and C4 QAFramework **21/21 Play Mode PASS** (separate B2 **10/10**). The accepted abrupt Host teardown cleanup limitation persists; no Stable promotion, release, universal production readiness, Session/dynamic-spawn support or supported Player-build certification is implied. See the [Framework tracker](../Architecture/Tracking/IF-TRACK-Framework.md#if-adr-014-amendment-003-c4--integrated-dynamic-composition-play-mode--2026-11-06).

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

When the owner releases or replaces the occurrence, Framework invalidates grants, cancels and drains dynamic operations, closes the Composer, and reconciles acquired Unity objects before removing the owner record. Route/Activity scene release continues through its existing barrier only after owner cleanup is confirmed. If cleanup is pending or fails, the binding record remains available for retry and physical release is blocked.

If Composer close encounters an acquired part containing its protected Object Entry root, the context remains closed and writes stay invalidated, while physical cleanup remains pending. Framework keeps the binding and retries guarded acquired-part releases only after the root is no longer inside the acquired hierarchy. Other close faults retain their diagnostics and continue to block physical release.
## Dynamic contribution access (C1)

When Allow Runtime Contributions is enabled, the committed exact occurrence exposes an Experimental ObjectEntryCompositionAccessGrant. The grant is unavailable before owner commit and, when a Profile exists, before its Base activation succeeds. It is bound to that physical binding and its current composition context; it does not resolve the target again by logical ID.

Each authorized TryOpenContribution creates an independent empty contribution. For profileless authoring, the Composer is created lazily on the first open. C1 has no add/replace/remove-part operation and does not materialize content. CloseAsync closes only that consumer contribution intent; Framework owner release remains responsible for physical cleanup. Neither the grant nor Composer determines Player selection or grants gameplay authorization.

## Dynamic part mutation (C2)

C2 adds single-part `SetPartAsync`, `RemovePartAsync` and contribution `CloseAsync` to the Experimental handle. `ObjectEntryCompositionPartDefinition` snapshots a key, prefab reference and optional dependency keys when created. The prefab must be a prefab asset; scene objects, nested Object Entry declarations, composition authoring, Actor declarations, Player hosts and PlayerInput are rejected before the request reaches Composer.

```csharp
if (authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant grant, out string issue) &&
    grant.TryOpenContribution(priority: 20, out ObjectEntryCompositionContribution contribution, out issue))
{
    ObjectEntryCompositionOperationResult result = await contribution.SetPartAsync(
        new ObjectEntryCompositionPartDefinition("seasonal", seasonalPrefab, new[] { "base-visual" }));
    if (!result.IsSuccessful)
        Debug.LogWarning($"Composition {result.Status} at revision {result.Revision}: {result.Diagnostic}");
}
```

Dependencies can be supplied by another contribution in the same Composer. The kernel validates dependency and priority graphs; the Framework does not resolve winners. A higher-priority contribution wins a key, equal top priorities are rejected, and removing or closing a winner can reveal a lower-priority tie. `CloseAsync` closes only that contribution's intent. A conflict or Unity callback after effects begin can produce `Degraded`; inspect `Revision`, `ChangedKeys`, `Diagnostic` and `EffectsMayHaveApplied`, and do not assume rollback.

The result replaces the C1 close-only result contract: a successful C1 `Closed` outcome is now `Committed`; the shared result also reports revision, changed keys and partial-effect evidence.

The Framework revalidates the exact grant, binding, runtime context and contribution handle for each mutation, including across materializer callbacks. Do not look up a new target by `ObjectEntryId` for an existing handle. Use the API on Unity's main thread. The owner still controls physical cleanup and scene release.

## Lifecycle and cleanup (C3)

Each exact `ObjectEntryCompositionRuntimeBinding` owns the task ledger for its dynamic `SetPartAsync`, `RemovePartAsync` and contribution `CloseAsync` calls. Registration happens before Composition starts. Consumer cancellation is linked to the operation token; owner release blocks new writes, requests cancellation and awaits every registered task through its asynchronous materializer cleanup. It then closes Composition, reconciles acquired resources and removes the same owner-binding record. Repeated release calls share the in-flight release task. Failed cleanup retains the record for retry.

Controlled `FrameworkRuntimeHost.StopAsync` closes Host admission, persistently fences dynamic grants (including a registry created after Stop begins), requests cancellation, and only then waits for admitted Host work. Activity, Route and physical scene-release ordering remain with their existing lifecycle owners. Base Profile activation and read-only occurrence queries are not fenced by the dynamic-write stop gate.

`OnDestroy` and `OnApplicationQuit` synchronously invalidate dynamic grants and request cancellation. Unity does not provide an awaitable guarantee at these callbacks for Route/Activity owner release or an external scene unload. The Project Owner explicitly accepted this bounded abrupt-teardown limit on 2026-11-06: cleanup from those callbacks remains **unconfirmed** and must never be reported as physically complete. The controlled Stop path retains the awaited drain and confirmation requirement.

**Readiness limit:** the C3 source barrier and regression tests have a clear independent static review, but no Unity compile/import or C3 test run is recorded. This API remains Experimental and is not certified for production use until the Project Owner completes Unity validation.

## Boundaries

- `ObjectEntryDeclaration` owns identity metadata only. `ObjectEntryCompositionAuthoring` and the profile are optional and separate.
- The profile cannot produce a Framework owner, a committed physical binding or permission to mutate an occurrence. Runtime admission and lifecycle remain owned by the existing Framework binding registry.
- B2 installs the authored Profile as the initial composition for an admitted occurrence. C1/C2 add exact-occurrence access and single-part dynamic mutation; they do not add query-based writes, batch mutation, delta events or Adopted parts. Query/recognition results remain read-only and do not grant contribution access.
- Nested prefabs with `ObjectEntryDeclaration`, `ObjectEntryCompositionAuthoring`, `ActorDeclaration`, `PlayerActorRuntimeHost` or `PlayerInput` are deliberately rejected for this narrow first authoring surface.
- Effective physical coverage is Route/Activity only. Session and dynamic Object Entry admission are excluded.
- The composition kernel remains `com.immersive.composition`, engine-free; no Actor composition assets were changed.

## Validation status

**B1:** authoring and Editor validation are implemented. The Project Owner's QAFramework Test Runner screenshot (2026-11-05) reports **511/511 Edit Mode PASS** and **10/10 PASS** in `ObjectEntryCompositionAuthoringTests`; no failures. This is owner-reported visual evidence, without an NUnit XML report or source-commit fingerprint. It does not validate the B2 runtime path.

**B2:** Framework PR #21 and QAFramework PR #1 are merged; final follow-ups are tracked by Framework PR #22 (protected-root cleanup and retry) and QAFramework PR #2 (evidence documentation). The Project Owner supplied a Unity 6 Test Runner screenshot showing **518/518 Edit Mode PASS, 0 failed**, including the corrected cleanup regressions, and a Console transcript ending in `[IF-ADR-014-DEMO] status='Passed' cases='10/10'`. The Play Mode transcript verifies exact-root/identity preservation, old/new Activity occurrences, parts destroyed while owner scenes remained loaded, and successful Route replacement. Earlier **514/517** failures are superseded by this full-suite result. These are owner-executed tests on the B2 checkout, not Codex-executed tests; no machine-readable NUnit report or source-SHA fingerprint was provided. Build/import succeeded with non-blocking CS0618/CS0067 warnings. Technical validation is supported for the tested B2 scope; merge status is tracked by the linked follow-up PRs. See the [Framework tracker](../Architecture/Tracking/IF-TRACK-Framework.md#object-entry-composition-b2-implementation-2026-11-05).

**C3/C4 validation:** C3 adds eight deterministic regression cases for owner-release drain, consumer/owner cancellation overlap, post-commit cleanup, multiple operations, consumer Close/Unity callback re-entry, shared release flight and lazy-registry fencing; the existing Controlled Stop and Host teardown integration fixtures exercise pending dynamic cleanup. After a test-assertion-only correction from `Cancelled` to `Stale` for invalidated targets, the Project Owner provided **566/566 Unity Edit Mode PASS** visual evidence. The separate [QAFramework C4 PR #3](https://github.com/rinnocenti/QAFramework/pull/3), integrated at `ce9ed9f1`, defines 21 public-API Play Mode cases; all **21/21 PASS**, with independent part add/replace/remove, precedence, Activity Restart readmission and Route replacement. The physical witness confirms destruction of 8 Route-owned and 1 Activity-owned parts while their original scenes remained loaded and roots/declarations/IDs were intact; retained roots later report `remaining='0'`. These results validate the specified Route/Activity scenario, not Session admission or dynamic spawning/transfers. Abrupt Host callbacks still cannot confirm async cleanup.

**C1 evidence:** earlier focused tests returned 60/60 StableObjectBinding, 13/13 ObjectEntryCompositionAuthoring and 12/12 ActorGroup. A native Editor crash and Test Runner internal exceptions prevented the first final rerun, but the Project Owner subsequently provided **538/538 Unity Edit Mode PASS**, including the C1 stale-close regression. This later screenshot supersedes the earlier incomplete runs as an outcome, without determining their root cause. No machine-readable NUnit XML or source-SHA fingerprint was supplied; C4 integrated consumer Play Mode evidence is recorded separately.

**C2 validation status:** C2 is integrated and has 19 focused Edit Mode cases. The Project Owner's 2026-11-06 visual Test Runner evidence reports **558/558 Edit Mode PASS, 0 failed, 0 ignored** on the pre-C3 baseline. It is not machine-readable or tied to a verified source SHA, and it does not validate C3 or certify C2 Play Mode lifecycle behavior. Codex did not execute Unity.
