# IF-PLAN-ADR-049-A001 — Scoped On-Demand Actor Occurrence Query

Status: **Approved — implementation task-gated; immutable**  
Approved: **2026-10-31** (Project Owner)  
Version: **v1**  
Date: **2026-10-31**  
Supersedes: **none**  
Superseded by: **none**  
Architecture authority: [IF-ADR-049 Amendment 001](../ADRs/IF-ADR-049-AMENDMENT-001-SCOPED-ON-DEMAND-OCCURRENCE-QUERY.md) over [IF-ADR-049](../ADRs/IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md)  
Preserved decisions: [IF-ADR-040](../ADRs/IF-ADR-040-Scene-Composition-Binding-Model.md), [IF-ADR-050](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md), [IF-ADR-051](../ADRs/IF-ADR-051-Actor-Groups-Identity-Membership-and-Player-Slot-Associations.md)  
API governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)  
Execution status: [IF-TRACK-Framework](../Tracking/IF-TRACK-Framework.md)

**Goal:** Give ordinary C# consumers a read-only, owner-scoped way to resolve any valid `ActorOccurrenceReference` once, by delegating to the existing `ActorOccurrenceReferenceRuntime.Resolve`, without changing `ActorOccurrenceReferenceBinding`.

**Architecture:** One new composed component (the *query anchor*) is discovered, bound and unbound by the `ActorOccurrenceReferenceRuntime` owner pipeline (`TryRegisterOwnerContent` → `TryCommitOwner` → `TryRollbackOwner` / `TryReleaseOwner`) that Session, Route and Activity already call. The anchor implements one public read-only interface that is handed to plain C# objects explicitly. A host-level invalidation unbinds every anchor at host teardown even when owner release never runs.

**Tech stack:** Unity 6.5 / `6000.5`, C#, existing `com.immersive.framework` Runtime assembly and `Immersive.Framework.ActorReferences.Editor.Tests`. No new package, asmdef or dependency.

**Review input:** the 2026-10-31 architecture review returned **CLEAR** with three mandatory adjustments (A1 anchor delivery, A2 `Stale` semantics, A3 host teardown). This plan carries all three as binding constraints.

## Global constraints

- `ActorOccurrenceReferenceRuntime` stays `internal`; no public or host-reachable path to it, to publication, commit, release, the Player source or materialization handles.
- `ActorOccurrenceReferenceBinding`, its scene-lifecycle discovery, `Changed` vocabulary and `RefreshAll` behavior are unchanged.
- `Resolve` is reused as-is. Its `Stale` / `Unavailable` outcomes are not changed (A2 is documentation only; IF-ADR-050 M04 stays deferred).
- No new resolver, registry of Actors, cache, index, enumeration, multi-target API, static access, singleton, service locator or scan outside the existing owner-root pass.
- The anchor is delivered only by the occurrence runtime owner pipeline. It is **not** discovered by `ActorOccurrenceReferenceSceneLifecycleParticipant` (one binding path only, A1).
- The anchor owner defines lifetime only; it never filters what can be resolved and grants no gameplay authorization.
- Supported anchor owner scopes: Session, Route, Activity. `Transient` / `Unknown` reject explicitly.
- New public types are `Experimental`. No Stable contract changes. No PEGA change in this route.

## Review focus

| Input or failure mode | Expected behavior | Gate |
|---|---|---|
| Query before commit, after rollback, after release or after host teardown | `Unavailable` with diagnostic; never throws; never reaches the runtime | Q02 |
| Query from a plain C# holder after the anchor's scope was released or the anchor destroyed | Same as above; holder keeps only the interface | Q02 |
| Query inside `Changed` or during `RefreshAll` | Returns committed state; no list or binding mutation | Q02 |
| Query is issued while a binding observes the same Actor | No `Changed` dispatch; binding `Current` unchanged | Q02 |
| Exact selector while the expected occurrence is transiently inactive (handoff A→B) or its dynamic scope is suspended | `Stale`; after compensation `Resolved` with the same `ActorOccurrenceId` | Q01 (text), Q02 (test) |
| Anchor under roots of two owners, or under an unsupported scope | Registration rejected; owner commit fails with diagnostic; no "last wins" | Q02 |
| Owner release where composition cleanup fails | Anchor unbound anyway | Q02 |
| Host destroyed with Route/Activity owners never released, or Session release failing | Every anchor invalidated by host teardown | Q02, Q03 (Unity) |

---

## 1. Reconciled evidence (source `efed6b2`, package `1.2.0-preview.4`)

| Concern | Evidence | Consequence |
|---|---|---|
| Resolver | `ActorOccurrenceReferenceRuntime.Resolve` (`Runtime/Actors/References/ActorOccurrenceReferenceRuntime.cs:431-495`) reads `_publications` and the Player source only. | Pure and reentrant; the query delegates to it unchanged. |
| Committed-only invariant | Static publications enter `_publications` inside `TryCommitOwner` with no callback before owner promotion (`:131-165`); dynamic ones require a committed owner (`:202`); `RestoreScopePublications` rechecks (`:400`). | Recorded as a tested premise (Q02), not re-implemented. |
| Owner pipeline | Session: `FrameworkRuntimeHost.TryCommitPersistentActorOccurrencePublications` (`FrameworkRuntimeHost.ResettableRegistration.cs:52-89`). Route: `RouteLifecycleRuntime.ResettableRegistration.cs:92-199`. Activity: `ActivityFlowRuntime.ResettableRegistration.cs:80-195`. All already call register/commit/rollback/release on the occurrence runtime with the exact `RuntimeContentOwner`. | A1: anchor discovery and binding live inside these existing calls. No caller or host signature change is needed for bind/release. |
| Owner-bound access precedent | `ActorGroupAccessBinding` + `ActorGroupRuntime` (`_bindingOwners`, `CanBindRuntime`, stage → commit → unbind) (`ActorGroupAccessBinding.cs:132-171`, `ActorGroupRuntime.cs:45-175`). | Same shape for the anchor, but hosted by the occurrence runtime, not by Groups. |
| Scene lifecycle participant | Binds `ActorOccurrenceReferenceBinding` on `OnSceneAvailable` only and has no owner (`ActorOccurrenceReferenceSceneLifecycleParticipant.cs:30-80,112-118`). | Not used for the anchor (A1). |
| Exact selector semantics | `Stale` for an exact Player selector when the Slot has no active physical occurrence (`:439-443`, physical source requires `activeInHierarchy`), and for any exact selector whose ID is not among current candidates (`:475-481`). Dynamic scope suspension removes then restores records with the same ID (`:391-408`, `:540-550`). | A2: `Stale` is not terminal for exact selectors. |
| Host teardown | `OnDestroy` releases the Session owner only if `ReleaseSessionScope` succeeds (`FrameworkRuntimeHost.cs:3951-3972`, `:4010-4023`); Route/Activity owners are not released (`_gameFlowRuntime = null`). `RemoveOwner` drops the committed owner before composition cleanup and may return failure (`ActorOccurrenceReferenceRuntime.cs:615-643`). | A3: anchor invalidation must not depend on owner release succeeding. |

At execution entry, re-record the exact Framework revision; do not freeze the plan to `efed6b2` if `master` moved. If any row above is contradicted by current source, stop and revise the plan.

## 2. Minimal public contract

Two Experimental types in namespace `Immersive.Framework.Actors`, file location `Runtime/Actors/References/`. Final names are confirmed in Q02 review; changing their shape (more members, events, enumeration) requires a plan revision.

```csharp
[FrameworkApiStatus(FrameworkApiStatus.Experimental,
    "IF-ADR-049 A001 read-only point-in-time Actor occurrence query.")]
public interface IActorOccurrenceQuery
{
    /// Resolves the selector against committed state at this moment.
    /// Never throws; returns Unavailable when the query scope is not bound.
    ActorOccurrenceResolution Resolve(ActorOccurrenceReference reference);
}

[DisallowMultipleComponent]
[AddComponentMenu("Immersive Framework/Actors/Actor Occurrence Query")]
[FrameworkApiStatus(FrameworkApiStatus.Experimental,
    "IF-ADR-049 A001 owner-bound query anchor.")]
public sealed class ActorOccurrenceQueryBinding : MonoBehaviour, IActorOccurrenceQuery
{
    public ActorOccurrenceResolution Resolve(ActorOccurrenceReference reference);
    // internal: CanBindRuntime / TryBindRuntime / UnbindRuntime (ActorGroupAccessBinding shape)
}
```

Behavior:

- Unbound → `Unavailable`, diagnostic "Actor occurrence query is not bound to a committed Framework owner." Invalid selector → `InvalidReference` (same as resolver). Bound → `_runtime.Resolve(reference)` verbatim.
- No serialized fields, no state besides `(_runtime, _owner)`, no events, no `Update`. Destroying the component does not require Unity callbacks: unbound state is a plain field check, so a C# holder of a destroyed anchor still gets `Unavailable` without touching Unity APIs.
- Hand-off: the owning component holds a serialized reference to the anchor and passes it as `IActorOccurrenceQuery` to plain C# objects. No `Find*`, static or host lookup.
- Not added: `IsAvailable` / owner getters, `TryResolve` overloads, batch resolution, notifications. A public owner getter would invite treating the owner as an authorization filter.

## 3. Lifecycle integration (A1)

All changes are inside `ActorOccurrenceReferenceRuntime` plus one host teardown call.

1. **Register** (`TryRegisterOwnerContent`): in the same root pass that collects `ActorOccurrencePublication`, also collect `ActorOccurrenceQueryBinding` (`GetComponentsInChildren(true)`). If any anchor is found and `owner.Scope` is not Session/Route/Activity, reject. Reject an anchor already staged/committed for any owner (`_queryBindingOwners`) or failing `CanBindRuntime`. Store anchors in the owner's `OwnerRegistration`. Owners without anchors behave exactly as today (no new scope check for publications).
2. **Commit** (`TryCommitOwner`): after publications are inserted and before owner promotion, bind each anchor (`TryBindRuntime(this, owner)`). Binding is a field assignment, so the "no callbacks before promotion" invariant holds. On any failure (destroyed or already bound anchor), unbind anchors bound so far, remove the owner's publications (existing path) and return `false`; the caller's existing rollback finishes cleanup.
3. **Rollback / Release** (`TryRollbackOwner`, `TryReleaseOwner`): unbind and forget the owner's anchors (staged and committed) **first and unconditionally**, then run the existing `RemoveOwner` / pending-cleanup logic. A failed composition cleanup no longer leaves an anchor bound (A3).
4. **Scene participant:** unchanged; it does not see anchors.
5. **Binding coexistence:** `ActorOccurrenceReferenceBinding` and anchors are independent types and registries; `_consumers` and `RefreshAll` are untouched.

Documented ordering (review risk): for the Session owner, `ReleaseSessionScope` (which releases bindings and emits `BindingReleased`) runs before `TryReleaseOwner`; during that window a Session anchor can still return `Resolved` for content being released. That is valid committed state and is documented, not changed.

## 4. Host teardown invalidation (A3)

- Add `internal void InvalidateQueryAccess()` on `ActorOccurrenceReferenceRuntime`: unbinds every anchor in `_queryBindingOwners` (null-safe for destroyed components), clears the anchor registry and sets a `_queryAccessInvalidated` flag that makes later `TryRegisterOwnerContent` calls with anchors reject. Idempotent. It touches no publication, consumer, Player source or owner state.
- Call it from `FrameworkRuntimeHost.OnDestroy` and `OnApplicationQuit` **after** the existing release attempts and **independently of their result**, guarded by `_actorOccurrenceReferenceRuntime != null` (no lazy creation during teardown).
- Consequence: after host teardown no anchor holds the runtime, so no query can reach the destroyed Player source (`_playerSource` fake-null risk is avoided for the query role). The binding's identical exposure is out of scope and noted only.

## 5. A2 — contractual text for `Stale`

Resolver unchanged. Q01 adds an append-only **Clarifications** section to Amendment 001 (no decision change), and Q03 mirrors it in the guide and Public API:

> For an exact-occurrence selector, `Stale` means the expected occurrence is not physically available **now**. Causes include replacement, release, transient inactivity of a still-committed occurrence (for example during a guarded A→B handoff) and suspension of a dynamic publication scope that may be compensated with the same `ActorOccurrenceId`. `Stale` is not proof of end of life. Non-exact selectors (Slot, ActorId) report the same transient as `Unavailable`. Consumers that need a durable value must record nothing on `Stale` and may query again later.

The same Clarifications section records A1 (§2.2.1 "existing participant" means the occurrence runtime owner pipeline, not the scene lifecycle participant) and A3 (§2.3.5 is guaranteed by host teardown invalidation, not only by owner release).

## 6. Ordered deliverables

Each gate is a separately issued task. Do not advance after a blocked or failed gate.

### Entry preflight (not a gate)

Record Framework revision and `package.json` version. Re-check §1 rows against source. Confirm the existing `Immersive.Framework.ActorReferences.Editor.Tests` suite is the last owner-reported green state (11/11 at `1.2.0-preview.2`, plus anything added since).

### Q01 — Contract clarifications (documentation only)

**Target:** Amendment 001 (append-only Clarifications section, Project Owner acknowledgment), tracker row 049.  
**Acceptance:** text for A1/A2/A3 matches §3–§5 of this plan; decision sections untouched; no Runtime/test change. Doc-only: no Unity validation required.

### Q02 — Anchor, owner pipeline, host invalidation and tests

**Target:** new `Runtime/Actors/References/IActorOccurrenceQuery.cs`, `ActorOccurrenceQueryBinding.cs`; `ActorOccurrenceReferenceRuntime.cs`; `FrameworkRuntimeHost.cs` (teardown calls only); new `Tests/Editor/ActorReferences/ActorOccurrenceQueryTests.cs` (same asmdef; no new references expected — verify `CS0012` risk before closing).

**Proof (EditMode, existing fake Player source / owner helpers):**

1. `Query_BeforeCommit_AndAfterRollback_ReturnsUnavailable` — includes staged owner invisibility.
2. `Query_DoesNotDispatchChangedOrMutateObservedBinding`.
3. `Query_InsideChangedCallback_AndDuringRefreshAll_SeesCommittedState`.
4. `PlayerReplacement_ExactSelectorIsStale_SlotSelectorResolvesNewOccurrence`.
5. `PlayerTransientInactivity_ExactIsStale_SlotIsUnavailable` (A2).
6. `ActivityExit_PreservesSessionOwnedPlayerForQuery`.
7. `DuplicateActorId_IsAmbiguousUntilExactOwnerSelected`.
8. `ScopeSuspensionCompensation_ExactGoesStaleThenResolvedWithSameId` (A2).
9. `OwnerRelease_UnbindsAnchor_PlainCSharpHolderGetsUnavailableWithoutThrowing`.
10. `OwnerRelease_WithFailedCompositionCleanup_StillUnbindsAnchor` (A3).
11. `InvalidateQueryAccess_UnbindsAllAnchorsWithoutOwnerRelease_IsIdempotent` (A3).
12. `AnchorBoundOwner_EqualsCompositionOwnerExactly`.
13. `AnchorUnderTwoOwners_OrUnsupportedScope_FailsRegistration`.
14. `SceneLifecycleParticipant_DoesNotBindQueryAnchor` (A1).
15. Existing `ActorOccurrenceReferenceRuntimeTests` unchanged and green (binding regression).

**Acceptance:** all of the above green in the owner's Unity EditMode run; static diff shows no change to `ActorOccurrenceReferenceBinding`, the scene participant, `Resolve` or Group code. Report `PENDING UNITY COMPILE/IMPORT VALIDATION` until the owner confirms.

### Q03 — Product/API closure and Unity confirmation

**Target:** `Documentation~/API/Public-API.md` (new row), `Documentation~/Guides/Actor-Occurrence-References.md` (new "Query on demand" section: anchor authoring, hand-off, point-in-time semantics, `Stale` text from §5, cost "per event/decision, not per frame", physical ≠ authorization, owner = lifetime only), `FrameworkApiStatus` metadata coverage, `CHANGELOG.md`, tracker.

**Manual Unity confirmation (Amendment §6.5), owner-run:**

1. In Play Mode, confirm `ActorOccurrenceReferenceBinding` is still bound only on `OnSceneAvailable` (unchanged behavior).
2. Anchor under Persistent Content: `Resolve` returns `Unavailable` before Session commit and `Resolved` after.
3. Anchor under Route and Activity roots: bound at owner commit; `Unavailable` after Activity exit / Route release; a C# holder retained across release gets `Unavailable`.
4. Exit Play Mode (host destroy) with a C# holder still referenced: no exception, `Unavailable`.

**Acceptance:** docs, status metadata, accepted ADR text and source agree; owner reports compile/import, Q02 suite and checklist results with revision. No PASS from static review. Consumer validation (PEGA) is a separate later task against the resolved candidate package (Amendment §6.7).

## 7. Non-goals

- Changing `ActorOccurrenceReferenceBinding`, its discovery or the resolver's outcomes.
- IF-ADR-050 M04 committed-binding view.
- Multi-target observation, enumeration of occurrences, caching or batching.
- Group-to-occurrence coupling (IF-ADR-051 stays by `ActorId`).
- Fixing the binding's own host-teardown exposure (noted, separate task if wanted).
- PEGA, Planet Devourer or QAFramework changes.

## 8. Gates, validation and change policy

Gates are Q01–Q03; the preflight is not a gate. Plan approval authorizes issuing Q01 only; each later gate is issued separately. After approval this plan is immutable; progress, revisions and owner-run results go only to `IF-TRACK-Framework.md`.

Agents do not run Unity build, import, Play Mode, smoke or batchmode. Static checks support review but do not close Q02/Q03.

Stop and revise (v2) if implementation needs: a resolver change, a second binding path, any new public member beyond §2, a Stable change, a lifecycle abstraction outside the existing owner pipeline, or caller-side changes in Route/Activity registration.
