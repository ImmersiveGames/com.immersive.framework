# IF-PLAN-014-C — Dynamic Object Entry Composition

Status: **C1–C3 integrated into Framework; C3 566/566 Edit Mode PASS; C4 21/21 Play Mode PASS and integrated in QAFramework PR #3 — tested Route/Activity scope technically validated**
Authority: accepted [IF-ADR-014 Amendment 003](../ADRs/IF-ADR-014-AMENDMENT-003-DYNAMIC-OBJECT-ENTRY-COMPOSITION.md)
Implementation: C1–C3 are integrated to Framework `master` (C3 PR #27, merge `2bf0560b`); QAFramework C4 is integrated to `main` (PR #3, merge `ce9ed9f1`). Project Owner evidence: C3 566/566 Edit Mode screenshot, C4 21/21 Play Mode Console transcript, and separate B2 10/10 Play Mode. Evidence and bounded guarantees are tracked in the Framework tracker.
Boundary: Framework Object Entry / Runtime Content integration; Composition kernel unchanged

The Project Owner approved the five Amendment 003 decisions on 2026-11-05 and authorized **C1**; subsequent C1–C4 cuts were implemented and integrated through their separate PRs. The C3/C4 Unity evidence supports technical validation of the tested Route/Activity scope. The public dynamic composition API remains **Experimental**; Stable promotion, package release, supported Player builds and wider lifecycle scopes require separate decisions/evidence.

### C2 execution state — 2026-11-06

The C2 implementation and 19 focused Edit Mode tests are integrated. The Project Owner supplied visual evidence of **558/558 Edit Mode PASS** on the pre-C3 baseline. This evidence does not validate C3 changes or certify C2 Play Mode lifecycle behavior.

## C1 — Access and exact occurrence identity

### Objective

Define an optional access grant for one exact occurrence, handed off through the existing Object Entry authoring surface. Bind the grant and every opened consumer handle to one exact StableObjectBinding record. Keep occurrence and recognition queries read-only.

### Expected files and surfaces

- Runtime/ObjectEntry/ObjectEntryCompositionAuthoring.cs — default-off runtime opt-in; preserve current Profile requirement unless the opt-in is enabled.
- Runtime/RuntimeContent/StableObjectBindingRegistry.cs — snapshot opt-in at registration; grant only after commit and successful Profile/Base activation.
- Runtime/RuntimeContent/ObjectEntryCompositionRuntimeBinding.cs — separate Base initialization from lazy dynamic-only context creation; retain exact binding guard.
- New Framework-owned Experimental access-grant, request, contribution-wrapper and result contracts under Runtime/ObjectEntry/; exact names/files deferred to API review.
- Tests/Editor/CameraAuthoring/ObjectEntryCompositionAuthoringTests.cs and Tests/Editor/Reset/StableObjectBindingTests.cs.

### Dependencies and contracts

- Preserve accepted B2 Profile validation and default behavior.
- No composition authoring means no Composer or grant. Profile-only B2 keeps its Composer/Base and receives no dynamic grant.
- No public method resolves the target again by logical identity.
- The authoring anchor hands out an immutable grant; TryOpenContribution is called on that grant. Old grants/leases stay stale after readmission even when ID, owner and GameObject match.
- Open and consumer Close check the captured exact binding; Framework release remains available after invalidation.
- Framework-owned public types only; Composer and kernel contribution do not escape.

### Tests and completion criteria

- Simple object: no Composer or grant.
- Profile-only B2: behavior unchanged; no dynamic grant.
- Explicit opt-in without Profile: admission succeeds, Composer is lazy until first authorized open.
- Explicit opt-in with Profile: grant unavailable before commit and during Base activation; available only after successful activation.
- Old handle versus new registration with same ID, owner and root: all old operations fail stale and cannot affect the new context.
- Invalid, ambiguous or uncommitted registration: no grant and explicit diagnostic.
- Existing occurrence and recognition queries remain read-only.

### Risks

- Serialized opt-in field and Inspector language require review; default false must preserve old assets.
- Commit precedes async Base activation; handoff requires a later readiness point.
- A root component is an authoring/handoff anchor, not gameplay authority.

## C2 — Dynamic part mutation

### Objective

Map Framework-owned prefab requests to independent Composer contributions and reuse B2 materialization without changing the physical root or adding another resolver.

### Expected files and surfaces

- Runtime/RuntimeContent/ObjectEntryCompositionRuntimeBinding.cs — lazy Composer, distinct contribution owners, exact current guard for every operation.
- Runtime/RuntimeContent/ObjectEntryCompositionPartMaterializer.cs — reuse root protection and Acquired cleanup; share per-part validation with Profile authoring.
- Runtime/ObjectEntry/ObjectEntryCompositionProfile.cs and new Framework part request/result contracts — separate prefab validation from Profile-only closed-graph validation.
- Focused Framework Edit Mode integration tests alongside StableObjectBindingTests and authoring tests.

### Dependencies and contracts

- Use Composer CreateContribution, guarded Set/Remove and existing results. Framework assigns owner IDs; kernel alone resolves dependency and priority outcomes.
- Reserve int.MinValue for B2 Base. Preserve top-priority conflict behavior, including a tie hidden under a higher winner.
- Start with prefab-backed Acquired parts. Do not expose Adopted instances or CompositionComposer.
- Start with single-key SetPartAsync and RemovePartAsync plus typed results. Defer public SetPartsAsync and delta events until a concrete consumer demonstrates a need for atomic multi-key mutation or cross-contributor observation. Results expose status, revision, changed keys, diagnostics and EffectsMayHaveApplied.
- Consumer Close closes intent; it is not physical cleanup. NoChange, cancellation or async failure is not cleanup success.

### Tests and completion criteria

- Set, replace and remove preserve exact root, declaration, ID and binding. Test dependencies against already-present contributions; do not require a public batch surface without a consumer case.
- Higher-priority winner, equal top conflict, hidden tie revealed by close, missing dependency, cycle and dependent ordering match kernel behavior.
- Removing an override restores the next valid contribution/Base when possible; reusing the same child instance is not promised.
- Pending/failing Acquired cleanup remains visible and retains owner evidence.
- Cancellation/partial post-effect failure preserves outcome details without claiming rollback.
- Stale target blocks set, remove and consumer close. Public batch remains deferred; Framework lifecycle can still close the old context.

### Risks

- Closing a winner may expose an equal-priority conflict and leave no valid winner.
- Composer intent may remain after failed/cancelled async effects; results must not imply rollback.
- Do not advertise dynamic mutation before C3 barriers are complete.

## C3 — Lifecycle, cancellation and recovery

### Objective

Integrate dynamic work with existing RuntimeContent barriers and prove progress through Activity, Route, Controlled Stop and teardown without another lifecycle authority.

### C3 execution plan — 2026-11-06

1. Add deterministic Edit Mode regressions first for owner release during preparation, release during post-commit asynchronous cleanup, multiple pending operations, and Unity callback re-entry. Use `TaskCompletionSource`; do not infer drain completion from the Composition gate or active-operation set.
2. Add private pending-operation ownership to the exact `ObjectEntryCompositionRuntimeBinding`. Register before calling Composition, reject registration after invalidation, link consumer cancellation, and retain each operation through its complete Composer task and cleanup `finally`.
3. Make owner release single-flight for the exact `OwnerBindings` record. Invalidate all grants, signal cancellation, drain, close, reconcile, then remove only the same record. Preserve B2 receipts and retry behavior.
4. In Controlled Stop, close Host admission first, persistently fence dynamic grant publication (including lazy registry construction and activation already in flight), cancel pending dynamic work, and only then await admitted Host operations. Keep Activity/Route release and scene barriers in their existing owners.
5. Do not add kernel, public API, global task, or second scene-retention infrastructure. Preserve synchronous grant invalidation and cancellation at abrupt Host callbacks; report cleanup as unconfirmed because those callbacks cannot await it. The Project Owner explicitly accepted this bounded abrupt-teardown limit on 2026-11-06. Controlled Stop retains its awaited drain and physical cleanup confirmation.
6. Reconcile this plan, the Object Entry Composition guide and the Framework tracker with the resulting diff and evidence. Unity execution is a Project Owner validation step under workspace rules.

**Pre-edit review:** the independent architectural-contract reviewer returned `CLEAR` for the awaited owner-release and Controlled Stop cut, subject to exact-registration removal and a persistent dynamic-write publication fence. It returned `DECISION_REQUIRED` for abrupt teardown; the Project Owner later accepted the bounded limit recorded above.

### C3 implementation state — 2026-11-06

The exact-runtime task ledger, linked cancellation, owner-release single-flight, registry grant fence and Controlled Stop ordering are authored. Eight deterministic Edit Mode regressions are authored across `ObjectEntryCompositionMutationTests` and `StableObjectBindingTests`; the existing Host controlled-stop and Host teardown integration cases now exercise pending dynamic cleanup. The independent post-implementation review returned **CLEAR** after fixture corrections and `git diff --check` passed. [PR #27](https://github.com/ImmersiveGames/com.immersive.framework/pull/27) contains implementation commit `2b5de992` and test-semantics fix `4647f0b0` (`Stale` after owner/Host invalidation; `Cancelled` preserved when only consumer cancellation occurs). The Project Owner's subsequent QAFramework Unity Test Runner screenshot shows **566/566 Edit Mode PASS** across the discovered test assemblies, including 138/138 Reset tests. There is no NUnit XML or source-SHA fingerprint. The C3 Edit Mode gate is evidenced; integrated dynamic-consumer Play Mode validation and C4 certification remain pending.

### Expected files and surfaces

- Runtime/RuntimeContent/StableObjectBindingRegistry.cs and ObjectEntryCompositionRuntimeBinding.cs — invalidate access, single-flight close/reconcile and retain owner until physical cleanup is confirmed.
- Runtime/ActivityFlow/ActivityFlowRuntime.ResettableRegistration.cs and ActivityFlowRuntime.Transaction.cs — Activity restart, switch, clear and release order.
- Runtime/RouteLifecycle/RouteLifecycleRuntime.ResettableRegistration.cs, RouteLifecycleRuntime.cs and RouteLifecycleRuntime.ControlledStop.cs — Route switch, stop, rollback/compensation and release order.
- Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs, FrameworkRuntimeHostAdmission.cs and relevant Host partials — cancel before operation drain; define abrupt teardown.
- Tests/Editor/PersistentContent/FrameworkRuntimeHostControlledStopTests.cs, StableObjectBindingHostTeardownTests.cs and focused RuntimeContent tests.

### Dependencies and contracts

- At release start, revoke grants, reject new opens/writes, cancel work, drain the exact runtime's pending task ledger, close Composition, reconcile acquired resources, then remove the exact owner record.
- In Controlled Stop, begin closing Host admission, apply the persistent dynamic-write fence and cancellation before awaiting admitted operations. The later Activity/Route release retains its existing owner and scene barriers.
- Track mutation, preparation, commit, replacement release and reconciliation through the exact-binding barrier. Do not overlap retries for one context.
- Retain owner and physical registration until context close, all acquired resources and safety conditions are confirmed. Release scene content afterward.
- Never block Unity's main thread or destroy/reparent the protected root.
- Controlled Stop drains and confirms the awaited owner cleanup. OnDestroy/OnApplicationQuit synchronously invalidate grants and request cancellation but cannot await Route/Activity release or guarantee external scene-unload ordering. This bounded limit was explicitly accepted by the Project Owner on 2026-11-06. Never report abrupt-path cleanup as confirmed.

### Tests and completion criteria

- Activity restart/re-entry and switch/clear release parts before old scene unload.
- Route switch and Controlled Stop invalidate leases, cancel/drain work and preserve the root until owner release.
- A Stop blocked behind async preparation sends cancellation before waiting for admitted operations and reaches release without deadlock.
- Concurrent close/retry is single-flight and cannot release a replacement or remove a binding early.
- Detach fault, pending destruction, retry and nonrecoverable failure retain distinct outcomes.
- Test controlled release while work is pending and verify teardown's synchronous invalidation without asserting abrupt-path physical cleanup. Record that abrupt cleanup remains unconfirmed and never claim it as success.
- Actor Composition and query/recognition contracts remain unchanged.

### Risks

- Abrupt Host teardown cannot await owner cleanup; the Project Owner accepted the bounded limitation, which remains distinct from the confirmed Controlled Stop guarantee.
- Cancellation, Unity callbacks and same-frame re-entry can deadlock or produce partial effects.
- Scheduling Unity Destroy is not proof of physical destruction.

## C4 — QAFramework certification

**Execution result (Project Owner, 2026-11-06): PASS 21/21.** The C4 QAFramework generated scene setup reported `setup='Prepared' scenes='4'` and Framework boot succeeded. Exactly 21 distinct case IDs reported `pass='True'`; terminal `[IF-ADR-014-C4] status='Passed' cases='21/21' executed='21' diagnostic=''`. Physical evidence shows 8 Route-owned plus 1 current Activity-owned acquired part destroyed while the respective owner scene was still loaded and each root/declaration/ObjectEntryId remained intact; final retained-root cleanup `remaining='0'`. Activity Restart completed with non-blocking no-reset-subject warning (`blockingIssues='0'`); old grant/handle returned `Stale`, and a new physical occurrence with the same logical ID accepted a new mutation. QAFramework [PR #3](https://github.com/rinnocenti/QAFramework/pull/3) was merged at `ce9ed9f1`. The C3 **566/566 Edit Mode** screenshot and B2 **10/10 Play Mode** transcript are separate evidence. All are owner-provided visual/console artifacts without NUnit XML or a verifiable execution commit SHA. This closes C4's **defined Route/Activity integration matrix**, not broader runtime or production certification. Abrupt Host teardown still does not guarantee cleanup confirmation. No new Framework runtime API, package release, Stable promotion or Player-build/platform validation is implied.


### Objective

Certify the integration in isolated Edit Mode and actual Unity Play Mode, separately from B2 evidence.

### Expected files and fixtures

- Framework tests: Tests/Editor/Reset/StableObjectBindingTests.cs, Tests/Editor/CameraAuthoring/ObjectEntryCompositionAuthoringTests.cs, Tests/Editor/PersistentContent/FrameworkRuntimeHostControlledStopTests.cs; add a focused suite if needed.
- QAFramework fixtures/scenario: Assets/QA-IF-ADR-014/Editor/QaIfAdr014Setup.cs and Assets/QA-IF-ADR-014/Scripts/.
- QAFramework setup/evidence notes: Assets/QA-IF-ADR-014/README.md if new manual fixture steps are required.
- No PEGA gameplay, outline/shader implementation, Framework manifest change or Composition source/test change.

### Test matrix and completion criteria

1. Plain object, Profile-only B2, dynamic-only opt-in, and Profile plus dynamic opt-in.
2. Set/replace/remove, dependency graph, precedence, equal priority and hidden tie revealed on close.
3. Query/recognition stay read-only; Composition does not choose selection or interaction eligibility.
4. Same ID, root and owner after release/readmission: old grant/handle cannot mutate the new occurrence.
5. Acquired creation, failed detach, pending destruction, retry and confirmation before owner removal/scene unload.
6. Activity restart/switch, Route switch, Controlled Stop, cancellation during async preparation, concurrent mutation/close and teardown with scene loaded.
7. Actor Composition and ordinary Object Entry query regressions.

Record Unity version, checkout, package hash, test counts/results and Play Mode logs. Do not claim C validated from source coverage, a test plan, Edit Mode alone or PR description. C4 completes only after the agreed QAFramework Unity tests run and limitations are explicit.

### Risks

- Generated fixtures may miss actual Unity destruction timing and callback re-entry.
- Passing evidence must observe root/identity preservation, exact occurrence, cleanup completion and owner-scene load state, not only assertion counts.

## Approval and execution gate

The five architectural decisions in Amendment 003 were **approved by the Project Owner on 2026-11-05**. The bounded abrupt-teardown limitation described in C3 was separately and explicitly accepted by the Project Owner on **2026-11-06**. C1–C3 are integrated (C3 Framework PR #27); C4 is integrated (QAFramework PR #3). The Project Owner supplied C3 566/566 Edit Mode evidence and C4 21/21 Play Mode evidence, establishing **tested Route/Activity technical validation** for Amendment 003. The accepted abrupt-teardown limitation persists; Session, dynamic spawning, broader platforms and Player-build coverage are excluded. Do not advertise universal production readiness or promote the Experimental API to Stable automatically. Package release and API maturity promotion remain separate decisions.
