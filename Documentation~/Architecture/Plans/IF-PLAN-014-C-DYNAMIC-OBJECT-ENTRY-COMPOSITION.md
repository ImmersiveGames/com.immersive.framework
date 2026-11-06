# IF-PLAN-014-C — Dynamic Object Entry Composition

Status: **Approved for staged execution — C1 authorized 2026-11-05; C2 source implemented, validation pending**
Authority: accepted [IF-ADR-014 Amendment 003](../ADRs/IF-ADR-014-AMENDMENT-003-DYNAMIC-OBJECT-ENTRY-COMPOSITION.md)
Implementation: C1 implemented on the authorized branch; validation evidence and remaining gaps are recorded in the Framework tracker
Boundary: Framework Object Entry / Runtime Content integration; Composition kernel unchanged

The Project Owner approved the five Amendment 003 decisions on 2026-11-05 and authorized **C1**. Start with a scoped public API review and exact-occurrence access work; preserve the C2–C4 cut boundaries and their independent test/evidence gates. The new dynamic mutation API must not be advertised as consumer-ready before C3 release/stop/teardown guarantees are validated.

### C2 execution state — 2026-11-06

The C2 source implementation and 19 focused Edit Mode tests are authored on `codex/if-adr-014-c2-dynamic-part-mutation`. `git diff --check` passed and the independent post-implementation review returned **CLEAR**. Unity import/compile/test execution, integration and validation are **not complete**. The pending Composer resource-release race is recorded as a **production BLOCKER** for C3; C2 must not be advertised as production-ready.

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

### Expected files and surfaces

- Runtime/RuntimeContent/StableObjectBindingRegistry.cs and ObjectEntryCompositionRuntimeBinding.cs — invalidate access, single-flight close/reconcile and retain owner until physical cleanup is confirmed.
- Runtime/ActivityFlow/ActivityFlowRuntime.ResettableRegistration.cs and ActivityFlowRuntime.Transaction.cs — Activity restart, switch, clear and release order.
- Runtime/RouteLifecycle/RouteLifecycleRuntime.ResettableRegistration.cs, RouteLifecycleRuntime.cs and RouteLifecycleRuntime.ControlledStop.cs — Route switch, stop, rollback/compensation and release order.
- Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs, FrameworkRuntimeHostAdmission.cs and relevant Host partials — cancel before operation drain; define abrupt teardown.
- Tests/Editor/PersistentContent/FrameworkRuntimeHostControlledStopTests.cs, StableObjectBindingHostTeardownTests.cs and focused RuntimeContent tests.

### Dependencies and contracts

- At release start, reject new opens/writes and cancel work before awaiting operations that need this cancellation.
- FrameworkRuntimeHost.StopAsync currently drains admitted operations before Activity clear and Route exit. C must not hold admission until a release that Stop cannot reach; add an ordering seam or prove another forward-progress path.
- Track mutation, preparation, commit, replacement release and reconciliation through the exact-binding barrier. Do not overlap retries for one context.
- Retain owner and physical registration until context close, all acquired resources and safety conditions are confirmed. Release scene content afterward.
- Never block Unity's main thread or destroy/reparent the protected root.
- Controlled Stop is the current awaited barrier. OnDestroy/OnApplicationQuit invalidate synchronously but do not await Route/Activity release. Prove owner-authority cleanup for abrupt teardown. If cleanup cannot be proven, any limit requires separate explicit Project Owner approval before C certification; Amendment 003 cannot narrow Amendment 002. Never report unconfirmed cleanup as success.

### Tests and completion criteria

- Activity restart/re-entry and switch/clear release parts before old scene unload.
- Route switch and Controlled Stop invalidate leases, cancel/drain work and preserve the root until owner release.
- A Stop blocked behind async preparation sends cancellation before waiting for admitted operations and reaches release without deadlock.
- Concurrent close/retry is single-flight and cannot release a replacement or remove a binding early.
- Detach fault, pending destruction, retry and nonrecoverable failure retain distinct outcomes.
- Test host teardown while the owner scene remains loaded. If only best-effort cleanup can be proven, a separate explicit Project Owner approval is required before accepting that limit; otherwise C3 remains open. Never claim unconfirmed physical cleanup.
- Actor Composition and query/recognition contracts remain unchanged.

### Risks

- Host teardown is not equivalent to Controlled Stop today; this blocks a complete lifecycle claim until cleanup is proven or the Project Owner separately approves a limit.
- Cancellation, Unity callbacks and same-frame re-entry can deadlock or produce partial effects.
- Scheduling Unity Destroy is not proof of physical destruction.

## C4 — QAFramework certification

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

The five architectural decisions in Amendment 003 were **approved by the Project Owner on 2026-11-05**. C1 implementation is authorized, with its public API design and regression checks required before completion. C2–C4 remain planned and require their own gate reviews and evidence. Any proposal to limit the accepted teardown guarantee requires **separate explicit Project Owner approval**; no such limit has been approved. Do not advertise dynamic mutation before C3 validation. Package release, API maturity promotion and runtime merge remain separate decisions.
