# IF-PLAN-014 — Object Entry Composition B2 runtime integration

Status: **Execution preflight / implementation pending** — 2026-11-05  
Authority: [IF-ADR-014 Amendment 002](../ADRs/IF-ADR-014-AMENDMENT-002-OBJECT-ENTRY-PHYSICAL-COMPOSITION.md) (**Accepted**). This plan does not introduce a new feature owner or change the accepted architecture.  
Implementation repository: `com.immersive.framework` (Framework Runtime Content/Object Entry); Composition kernel unchanged.

## Entry evidence

The Project Owner supplied a QAFramework Unity Test Runner screenshot on 2026-11-05 showing **511 Edit Mode tests passed, 0 failed**, including the nested `ObjectEntryCompositionAuthoringTests` suite (**10 tests**). This is owner-provided visual evidence, not a NUnit XML report. It does not expose the exact checkout commit or prove Inspector UI, Play Mode lifecycle, or a prefab materializer. B1 is authored and QA-observed, not full Object Entry composition certification.

The Framework B1 PR #19 is merged into `master`. The source-side profile, authoring component, two Inspectors and Edit Mode tests are present. No runtime Composer activation has been implemented.

## Source-confirmed seams

1. `StableObjectBindingRegistry.TryRegisterOwnerContent` enumerates exact declaration GameObjects beneath admitted materialized Route/Activity scene roots; `TryCommitOwner` activates committed query authority; `TryRollbackOwner`/`TryReleaseOwner` remove bindings. `StableObjectBinding` instances are distinct for each registration even if ID, owner and GameObject are reused.
2. `ActivityFlowRuntime.Transaction.cs` calls `PrepareStableObjectBindings` before the activation gate; then updates `_currentActivityState` and calls `transaction.Commit` **before** `CommitStableObjectBindings`. It releases previous bindings before unloading previous Activity scenes, both in replacement and clear flows. The exception path after `CommitReached` is a **post-commit failure**, not an ordinary precommit rollback.
3. `RouteLifecycleRuntime.cs` prepares bindings from loaded Route scene roots; sets current Route state and result before `CommitRouteStableObjectBindings`. Route transition and controlled stop release old bindings **before** physical scene content release.
4. `FrameworkRuntimeHost.OnDestroy` and `OnApplicationQuit` invalidate Object Entry query access synchronously. `StopAsync` drives the official async Activity/Route release path. Host destruction cannot honestly be assumed equivalent to awaiting a Composer close.
5. `CompositionComposer` offers `SetPartsAsync`, current-target guards and `CloseAsync`. `IPartMaterializer.ReleaseAsync` may return Pending/Failed, and `UnityEngine.Object.Destroy` is deferred in Play Mode. The Actor-specific binding's synchronous close assumption is not an appropriate new Object Entry lifecycle guarantee.

## Implementation sequence / constraints

### B2.1 — Preflight with no side effects

- Reuse the admitted declaration root and exact `RuntimeContentOwner`; discover `ObjectEntryCompositionAuthoring` **only on that same GameObject**.
- Run its existing profile/part validation during owner registration, prior to transaction commit. A malformed explicit opt-in is an explicit admission diagnostic. Without opt-in, do not allocate a Composer and do not change registration or release behavior.
- Reject ambiguous active bindings and conflicting registrations before issuing write authority.
- Keep one `CompositionComposer` attached to one exact `StableObjectBinding` **record instance**; no duplicate registry, global provider, static access or synthesized persistent occurrence ID.

### B2.2 — Commit-aware materialization

- On confirmed committed binding, translate that profile's keyed prefab parts into `PartDefinition` for the generic Composer using a Framework-owned Unity materializer.
- The materializer may instantiate only subordinate prefab-backed parts under the existing root; it cannot create/reparent/replace the Object Entry declaration root. It must verify root and binding identity before and after preparation/effects.
- A component supplied by the part must remain independently operable without Composer awareness. Existing scene components require no profile.
- Keep Composer private in B2. Public independent contributors and mutation/access are reserved for C. This bounds concurrent operations and allows the first materializer path to be verified without promising arbitrary async consumer work.
- **Post-commit failure cannot be reported as a precommit rollback.** Confirm how the current transition result represents a failed initial composition after Route/Activity authority commit. Compensate installed parts and classify an unresolved failure as degraded/committed failure. Do not silently report success, drop a bound target or unload an owning scene with unresolved acquired resources.

### B2.3 — Release barrier

- Add an owner-scoped async close path owned by the same Framework Runtime Content binding authority. Mark the exact binding invalid for further composition before close; drain/cancel operations; await close and materializer release; then remove binding.
- Await this barrier from the **existing async Route/Activity replacement, clear and controlled-stop transitions**, before scene release. A failed or pending release must preserve explicit evidence and prevent declaring a completed physical cleanup. Synchronous registration/rollback for never-committed bindings can stay synchronous.
- For Unity `Destroy` at runtime, differentiate detachment/scheduled destruction from observed physical completion. Do not represent merely scheduled destruction as confirmed complete. Use an explicit asynchronous confirmation appropriate for Unity's main-thread lifecycle.
- At host `OnDestroy`/`OnApplicationQuit`, invalidate future writes immediately; any nonawaitable best-effort cleanup must be labelled as such. Normal validation/certification requires successful `StopAsync` and the owner-scoped release barrier.

### B2.4 — Required QAFramework evidence

- No profile: unchanged Route/Activity admission, no Composer allocation.
- Valid profile: the exact declaration root and `ObjectEntryId` remain unchanged, a prefab part appears only after commit.
- Invalid profile: deterministic admission diagnostic, no partially live part.
- Activity replace and Activity clear: all acquired parts are detached/released before old scene unload.
- Route replace and controlled stop: same release ordering and no retained Composer.
- Re-admission with same ID/owner/GameObject: old binding's current-target check fails; stale operation cannot affect new registration.
- Duplicates/ambiguous binding: no write access; root never replaced by materializer.
- Effect failure, delayed preparation and release Pending/Failed: statuses remain observable; scene release is not prematurely counted as successful.
- Actor composition and Object Entry Resolve/Recognize: unchanged behavior.
- Edit Mode tests for isolated contracts; Play Mode QAFramework for Unity `Destroy`, async release and lifecycle timing. Distinguish authored, executed, integrated, validated.

## Exit gate

B2 can be marked implemented after the runtime path is wired and compiles; **integrated/validated** requires the required QAFramework evidence. Do not publish a package release or promote the Experimental API based on Edit Mode authoring tests alone.
