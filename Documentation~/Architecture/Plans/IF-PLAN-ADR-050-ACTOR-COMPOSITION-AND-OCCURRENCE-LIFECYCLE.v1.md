# IF-PLAN-ADR-050 — Actor Composition and Occurrence Lifecycle Migration

Status: **Approved — implementation task-gated**
Version: **v1**  
Date: **2026-10-17**  
Architecture authority: [IF-ADR-050 — Accepted future target](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md)  
Technical design: [IF-SPEC-050](../Specifications/IF-SPEC-050-Actor-Lifecycle-Participation-and-Composition.md)  
Migration contract: [IF-ADR-050 accepted-target migration](../Reconciliation/IF-ADR-050-ACCEPTED-TARGET-CONTRACT-MIGRATION-2026-10-17.md)  
Accepted-ADR conflict crosswalk: [IF-ADR-050 reconciliation](../Reconciliation/IF-ADR-050-ACTOR-LIFECYCLE-COMPOSITION-RECONCILIATION-2026-10-17.md)  
Governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)  
Execution and evidence: [IF-TRACK-Framework](../Tracking/IF-TRACK-Framework.md)

## 1. Purpose and execution boundary

Implement the IF-ADR-050 **accepted target** through small, reviewable deliverables, with separate proof and explicit compatibility/rollback decisions per cut. The destination is a **separate generic UPM package** `com.immersive.composition`, consumed by `com.immersive.framework`; Framework remains the authority for Actors, physical roots, Player participation, Camera and Current occurrence.

This plan is **not** an implementation, a published UPM, a new Stable public contract, authorization to modify consumer projects or evidence that Unity behavior has been verified. Creating/merging this plan authorizes **planning only**. Before beginning each deliverable, issue one narrowly scoped implementation task, review its evidence, and record its completion or blocker in the mutable Framework tracker. Do not execute all deliverables as one autonomous batch.

The dependency graph is architectural; the final names of C# interfaces, serialized fields and concrete Editor widgets belong to the respective implementation/API-design cuts, not this plan.

## 2. Authority and non-goals

| Concern | Owner / boundary |
|---|---|
| Joined Player and `PlayerOccurrenceId` | Framework Player Session. Neither Composer nor Actor Profile owns Join, Leave, Host, PlayerInput or Slot membership. |
| Logical `ActorId` | Typed Actor/producer contract, separate from selected Profile and Player participation. |
| Committed physical `ActorOccurrenceId` and Current | Framework lifecycle authority; candidate materialization is correlated by a private operation identity and exact root/owner before commit. |
| Root materialization/adoption/teardown | Actual Actor producer; not Composition. |
| Content definitions, keyed reconciliation and content revision/delta | Generic `com.immersive.composition`, with Framework translating Actor/Session concepts at its own integration layer. |
| RuntimeContent resource scope, acquiring adapters and cleanup | Existing owner-specific contracts; Composer does not claim ownership of foreign instances or Scene-adopted parts. |
| Camera/Input/Route/Activity gameplay | Their existing Framework owners; Actor replacement may request controlled reprojection but never transfer these authorities to Composition. |
| Consumer gameplay and PEGA activity flow | Consumer. PEGA validates use and gameplay sequencing, **not** general Framework identity/Player switching internals. |

Do **not** add a global Actor Manager, a second visual-instantiation fallback, an ActorShell type required for all producers, fixed Layer taxonomy, universal capability table, arbitrary `OnEnable` suppression, blanket rollback of Unity effects, global cleanup registry or implicit Scene/NonPlayer root ownership transfer. Defer sockets, Addressables, generalized pooling and complex per-part state-transfer protocols unless a bounded later requirement proves necessary.

Preserve Stable 1.1.0 consumer-contract guarantees and serialized intent under IF-GOV-001/IF-ADR-047. A breaking change to a Stable contract requires a **separate explicit compatible migration/version decision**, not silent deletion in a cut. Experimental types still require recorded consumer impact rather than assuming no use.

## 3. Baseline and sequencing

Before the first code cut capture the exact Framework `master` SHA, effective Unity 6 editor version, package manifest/lock state and any active consumer/QA package resolutions. The architecture documentation baseline includes IF-ADR-050, IF-SPEC-050 and the dated accepted-target migration record. **Do not freeze the old source SHA into this immutable plan**: record a contemporaneous immutable baseline and the package/QA comparison revision in the tracker at execution entry.

The execution dependencies are:

```text
D00 evidence + API/serialization inventory
  ├─ D01 release safety (N1)
  ├─ D02 committed identity + availability (N4; prerequisite to handoff)
  └─ D03 independent Composition kernel UPM
                   │
               D04 Framework Base visual integration + migration
                   │
               D05 in-place keyed recomposition
                   │
         D06 guarded Actor A→B replacement (N2, N3, N6)
                   │
         D07 Camera / Scene-Provided / NonPlayer integrations
                   │
         D08 integrated validation, consumer migration, distribution decision
```

D01 and D02 may be investigated independently, but do not integrate dependent runtime paths out of order. D03 lives in a different repository and is versioned independently; D04 must resolve a **concrete candidate version** of it, not an undeclared local assembly. D06 may prepare Actor replacement independently of optional content **only** when the integration can prove the same unified content contract.

## 4. Deliverables — one accepted task at a time

### D00 — Implementation baseline and contract gates

**Repository:** Framework documentation/API/QA inventories only.

**Work:** Inventory current `ActorId` issuance, `PlayerOccurrenceId`, physical occurrence/resolver predicates, preparation operation IDs, Actor Profile serialized visual fields, `VisualContentMount`, Route placement, Camera subject/reader integration, Scene-Provided adopted content, RuntimeContent release and public selection/replacement result shapes. Map each touched consumer contract and attribute to Stable/Experimental/Internal, explicitly identifying Stable authoring data, known consumers and versioning/migration constraints.

**Deliverables:** exact-code-revision evidence record; contract-to-owner matrix for M01–M12; dependency/version boundary for the new package; bounded QA scenario inventory; explicit per-cut migration/compatibility dispositions. Reuse existing evidence; do not rerun broad Framework QA merely to discover behavior already proven.

**Acceptance:** no ambiguous ownership or Stable guarantee in scope; agreed test/evidence boundaries and target Unity version recorded. **Exit gate:** authorize D01 and D02 separately. No runtime behavior changes.

### D01 — Correct current visual teardown independently (N1)

**Repository:** Framework.

**Work:** Fix `AttachedPlayerActorMaterializationAdapter.TryReleaseMaterialization` and any equivalent affected path so an active visual cannot become newly `activeInHierarchy` by being detached from an inactive root into an active hierarchy immediately before deferred `Destroy`. Preserve exact physical owner and RuntimeContent logical release semantics, including release failure/retention.

**Proof:** focused Editor/Play Mode scenario with canonical logger recording active-state transitions, `OnEnable`/`OnDisable` for visual, release request/outcome and correct owner. Include managed and borrowed/adopted resource distinctions when the path supports them. Do **not** claim that scheduling `Destroy` confirms physical destruction.

**Acceptance:** no release-induced reactivation, no foreign resource destruction, failures observable. **Exit gate:** D01 committed with focused evidence; independent of Composition package creation.

### D02 — Actor identity, committed binding and availability (N4; M01–M04)

**Repository:** Framework.

**Work:** Establish lifecycle-owned issuance/retention of `ActorOccurrenceId` at authoritative physical commit. Distinguish typed `ActorId` logical identity, `PlayerOccurrenceId` Joined participation, `PlayerSlotId`, profile identity and materialization operation correlation. Preserve committed occurrence ID through temporary inactivation/re-resolution; invalidate only on actual authoritative release, replacement or owner closure. A failed/aborted candidate never becomes public Current. Keep NonPlayer publication owner-scoped.

**Critical compatibility gate:** IF-ADR-049's **physical** `TryResolve` success must not silently expand to inactive roots; introduce a separate committed-binding/availability view or explicit compatible typed outcome as required. Reconcile historical IF-ADR-023A Player per-materialization `ActorId` behavior without changing Unity declaration/serialization by accident. Reserve an ID earlier internally only if a real producer contract demands it; never publicly publish an uncommitted candidate.

**Proof:** exact ID remains stable across temporary `SetActive(false/true)` and repeated queries; new physical root and pooled reuse obtain new ID; same logical Actor root replacement may preserve ActorId; different logical Actor changes it; stale owner/operation is rejected; Player Join/Leave and NonPlayer owner publication retain correct identity domains. Use canonical logging for identity/change evidence.

**Acceptance:** deterministic Current/publication/availability and no lazy/reissued occurrence IDs. **Exit gate:** D02 versioned with API/migration evidence before D06.

### D03 — Create the independent generic Composition UPM

**Repository:** new `ImmersiveGames/com.immersive.composition` (create only when this cut is authorized).

**Package constraints:** `name: com.immersive.composition`, own `package.json`, Runtime/Editor/Test assemblies as actually needed, documentation/API maturity strategy and version. **No dependency on `com.immersive.framework`** or on Actor, Player, Session, Route, Activity, `ActorId` or Framework `RuntimeContentOwner` types. UnityEngine-specific materializers may be separate providers within the UPM; the minimal kernel must not require GameObject identity for data-only parts.

**Minimal model:** typed/validated content keys; PartDefinition (data or physical); PartMaterializer with explicit staging/apply/release and acquisition-vs-adoption policy; one Composer per content root/context; owner-scoped contribution + priority; deterministic conflict handling; dependency graph/closure; async result invalidation by exact context/key/operation; public monotonic revision and changed-key delta **after** commit. Equal-priority competing owners and cycles reject before effects.

**Proof in package:** visual/data materializer examples or controlled fakes; deterministic owner precedence, missing/cyclic dependencies, unrelated-key concurrency, stale async completion, owner close, independent content commit, non-owner release rejection, adopted-part no-destroy, partial post-effect fault reporting. Show the package can compile and execute its kernel tests without importing Framework. Canonical logging at the package integration boundary; don't invent a second Framework logger contract inside generic code.

**Acceptance:** separately reusable, dependency-acyclic, minimal UPM with a pinned candidate version and focused tests. No Actor lifecycle or global registry is included.

### D04 — Framework integration and single Base visual path (M08–M09, M11)

**Repository:** Framework; pinned dependency on exact D03 candidate.

**Work:** Integrate the Composer as a Framework dependency at the explicit Actor materialization/adoption boundary. Translate selected ActorProfile into a **Base contribution at key `visual`**, using the root's existing visual mount. Replace the existing runtime visual instantiation/release with this *one* path; prohibit hidden fallback to old `presentationPrefab` materialization when Composer is absent. An Actor with no content should still be valid without instantiating an unnecessary Composer.

**Authoring/data:** define Inspector-friendly serialization and Editor validation for missing mounts, duplicate keys, dependencies and forbidden Actor/PlayerInput components inside replaceable content; write an explicit **idempotent asset migration** preserving prior selected/default profile intent, references and GUIDs. Preserve legacy serialized fields/signatures as needed by their actual maturity and compatibility classification; do not conflate `GameApplicationAsset` Stable schema with Experimental content behavior. Scene-Provided already-owned visuals are adopted with exact provenance, not duplicated or destroyed.

**Proof:** old authored visual converts to Base entry, Null/no-visual remains valid, new authoring persists after Unity reimport and asset save, Manager-Provisioned initial visual materializes once, Scene-Provided adopted visual remains the same object, rollback/cleanup does not violate exact ownership; no second visual materialization provider remains.

**Acceptance:** one authoritative visual path, valid migrated serialized assets, no implicit scene ownership transfer. **Exit gate:** D04 candidate install/import and Inspector evidence before dynamic updates.

### D05 — Dynamic in-place keyed recomposition (M08–M09)

**Repositories:** Composition kernel (only if kernel contract repair is required) and Framework Actor integration.

**Work:** Provide an explicit owner-scoped command for changing contribution intent, distinct from `RequestReplaceActorSelection` and `RequestReplacePreparedActor`. Implement resolution by content key, preparation of affected parts, dependency-closure application and revision/delta publication. Composition-only changes preserve `PlayerOccurrenceId`, `ActorId`, `ActorOccurrenceId`, root pose, root gameplay-owned state and producer ownership. Input, Camera assignment and Activity membership remain outside kernel authority.

**Concurrency/failure:** independent slow visual preparation cannot block unrelated data-key updates; per-key/closure expected-target checks reject stale async results. Resolve conflicting owners deterministically; no arbitrary `OnEnable` rollback claim. External listeners caching child components rebind based on exact delta; failures after physical effects report degraded/faulted content state.

**Proof:** isolated visual swap and visual+dependent part swap with stable Actor/root identity; data-only part; two owners with precedence, equal-priority conflict, cancellation and late async completion, dependency cycle; owner release removes contributions without destroying foreign/Scene-owned content. Record exact before/after IDs and revision via canonical logger.

**Acceptance:** Actor remains the same physical occurrence while committed content changes, with accurate failure/ownership evidence. **Exit gate:** D05 before D06 relies on candidate composition.

### D06 — Guarded physical replacement and Player/Route reconciliation (N2, N3, N6; M05–M07)

**Repository:** Framework.

**Work:** Replace the current early-selection/rollback approach with a **bounded synchronous handoff**: keep A Current and operating through B's inactive/possibly async preparation; validate B, materializers and owner; apply candidate-local Route pose without confirming shared marker; enter a non-reentrant per-domain transition barrier; deactivate A; activate B with specialized B gameplay/input gates closed; revalidate exact owner/root/expected-current evidence; commit B's physical occurrence ID and selected profile coherently; only then notify and reconcile gameplay/Camera. Release retired A through its owner. Keep the same Joined Player, Host, PlayerInput and Activity occurrence.

**Conflict policy:** conflicting voluntary same-domain reentrant mutations yield a typed busy/rejected result; forced Session/owner closure must invalidate B commit, not disappear behind barrier. `OnDisable(A)` and `Awake/OnEnable(B)` may see last committed **A binding**, but the existing physical resolver may report A **unavailable**; B remains private candidate. No async yield while barrier is held. Route candidate placement must not mark shared entry complete until commit; same-Route replacement should preserve A pose, not reapply entry.

**Failure boundary:** before deactivating A, failed preparation retains A operational. After A deactivation, callback effects cannot universally roll back; B failure leaves A last committed but potentially unavailable — **typed partial/degraded result** with explicit recovery path. After B commit, failure of gameplay/readiness or cleanup keeps B Current. No conflation of structural `GameplayReady` and `RuntimeGameplayAvailable`.

**Proof:** N2 selection event only after coherent current; N3 no double selection revision on failed candidate; N6 A and B not both activated by Framework order; callback reentrancy, forced close, stale token, Route pose, B activation failures, A physical unavailability, postcommit Camera/gameplay reprojection and cleanup failures. Use canonical ordered logs, without claiming `SetActive` propagates arbitrary MonoBehaviour exceptions. A small isolated Unity callback proof is required only for any stronger engine-specific guarantee claimed in the implementation.

**Acceptance:** single authoritative Current, exact typed outcome for degraded handoff, no uncommitted B observation, no silent loss of owner invalidation.

### D07 — Camera, Scene-Provided and external NonPlayer convergence (M04, M10–M12)

**Repository:** Framework; consumer-side adapters only by separate authorization.

**Work:** Reconcile root-stable Camera observation anchor and Actor Subject reprojection with occurrence commits and composition deltas. Bind exact `PlayerOccurrenceId` for Camera membership; physical Subject changes only when applicable. Verify lifecycle and adopted-part ownership for Scene-Provided; preserve reduced guarantees where Scene scripts have already executed. Provide an exact owner-aware NonPlayer producer publication/composition path without demanding PlayerSlotId, Session-owned root or universal NonPlayerSpawner. Ensure owner closure invalidates stale Composer operations and cleanup receipts remain attributable.

**Proof:** replacement with unchanged Camera assignment and updated physical Subject; content-only visual swap with stable observation root; cached child target either explicitly rebinds or is rejected in authoring; Scene-Provided old visual neither duplicated nor destroyed; NonPlayer content works under producer ownership; Activity exit does not close Session Player physical lifetime; terminal Leave and external owner scope closure remain independent.

**Acceptance:** all producer modes use the same content semantics **where supported**, while their physical ownership and preactivation guarantees remain distinct. Explicitly mark any Scene-Provided full-root replacement as unsupported until a separately accepted ownership contract exists.

### D08 — Integrated evidence, migration release and package rollout

**Repositories:** Composition, Framework, QAFramework; named consumers only where they actually need migration.

**Work:** Pin exact UPM version and dependency graph; validate tarball/import/assembly definitions, package/editor tests and Framework consumption on immutable candidate revisions. Run the **focused** contract matrix below and record source/tag/package hash provenance. Migrate identified `ActorProfile` authoring assets with explicit, idempotent Editor tooling and per-asset report; reimport/save/reopen smoke. Validate consumer import/compile and only necessary player/activity integration smoke. Publish or tag only under a **separate explicit release decision**; this plan does not authorize automatic Stable promotion or distribution.

**Compatibility:** distinguish Experimental behavior migration from any Stable serialized-schema or public method change. Keep necessary compatibility projection until the authorized version window; if a breaking Stable contract is unavoidable, route it to IF-ADR-047's MAJOR/version decision and explicit consumer migration. Do not assume old and new visual providers can coexist indefinitely.

**Acceptance:** package integration and provenance are reproducible; accepted target's tests pass on a known candidate; open deviations are recorded, not waived. Only then close the plan's implementation deliverables and update API guides/maturity records to **actual** implemented states.

## 5. Focused evidence matrix

| Evidence key | Covered contract(s) | Primary proof owner | Required observations |
|---|---|---|---|
| **E01** | N1, D01 | Framework / QAFramework | Visual no-reactivation during release; requested vs completed release evidence; adopted object preservation. |
| **E02** | M01–M04, N4 | Framework | Player/Actor/physical ID separation; committed ID survives temporary inactive; stale/owner mismatch rejected; NonPlayer exact binding. |
| **E03** | M09 | Composition package | No Framework dependency; deterministic keyed resolution, cycle/conflict rejection, dependency closure, owner/async invalidation. |
| **E04** | M08, M11 | Framework Editor + QA | Serialized profile migration, Base visual and mount, Scene-adopted visual once; no parallel provider. |
| **E05** | M08–M09 | Framework + Composition | Content-only visual/data update, stable root/occurrence ID, revision/delta; keyed concurrency and failure evidence. |
| **E06** | M05–M07, N2/N3/N6 | Framework + QAFramework | A/B callback observations, coherent selection/commit, forced close, degraded precommit outcome, postcommit B authority, Route spatial pose. |
| **E07** | M10–M12 | Framework + QAFramework | Camera membership/Subject, root-stable target, Scene/NonPlayer lifetime, cleanup ownership. |
| **E08** | Distribution/consumers | Framework/package + relevant consumers | Exact installed artifact, compile/import, authored data migration evidence, targeted Player/activity smoke and release decision. |

Use the **canonical logger** for lifecycle, selection, Camera and ownership proofs; preserve operation IDs, exact Actor/Player occurrence IDs, stage, owner and before/after availability. Prove changed Framework contracts in Framework/QAFramework. PEGA tests its gameplay and flow sequence against a consumed version; it is **not** responsible for recertifying generic Framework infrastructure. Do not rerun broad unrelated Framework capabilities merely to satisfy a plan checklist.

## 6. Cross-deliverable gates and blocking rules

1. **Single Current authority:** no consumer may observe B as a committed physical Actor before Core commit; no Composer content delta may change Actor Current or Session selection.
2. **Two commits:** content revision/delta ≠ Actor occurrence commit. No visual swap changes ActorOccurrenceId; no root replacement silently inherits a physical ID.
3. **Ownership:** stage and release only resources acquired by the reporting adapter; adopted Scene resources are not owned by the Composer. Deferred Unity destruction is not physical completion evidence.
4. **Unity callbacks:** same-frame synchronous code is **not** callback atomic. Observability and typed partial result semantics must remain true even if side effects cannot be undone.
5. **Player/Camera consistency:** no early public selection change, no stale readiness proof recycled after Actor root change, no accidental Leave/Join, Host/Input replacement or Camera assignment replacement.
6. **Migration:** before removing a serialized field/old API, show exact maturity, affected consumers, idempotent migration and required version decision.
7. **One increment at a time:** blocked/failed work remains explicit and does not auto-advance. Independently finished deliverables can be committed/reviewed without claiming subsequent phases passed.
8. **Avoid accidental Stable promotion:** a new package/version and passing tests do not automatically make any consumer-facing contract Stable.

## 7. Review questions before authorizing D00

The plan's **architecture is accepted by IF-ADR-050**. These implementation choices belong to scoped cuts and do not require reopening architecture:

- Exact initial exposed API types and C# namespaces; choose at D02/D03 after contract inventory.
- Whether the old physical resolver needs a separate committed-binding query or a compatible typed availability result; choose at D02 without silently widening `TryResolve`.
- Serialization shape for Base visual, asset migration and source compatibility projection; choose at D04 after Stable/Experimental check.
- Per-domain barrier/owner-close handling and typed degraded result surface; choose at D06 following exact current API inventory.
- Scene-Provided replacement feature scope; **not included** in the first full-root replacement implementation, while Scene-adopted content and NonPlayer publication remain in scope.
- Release version and Stable compatibility/distribution gate; decide only at D08 with real packaged artifact evidence.

## 8. Plan review and maintenance

This is a **Proposed v1 implementation route**, not evidence of completed stages or permission for unbounded implementation. On plan review, either mark the exact v1 document **Approved — task-gated**, or create a revised v2 with explicit differences. Once approved, treat its deliverable scope/order as an immutable route; keep *mutable* task status, candidate revisions, test evidence and blockers in IF-TRACK. Do not rewrite historical certification, previously released tags or old ADR semantics to make this plan appear completed.

The immediate next action is **plan review/approval**, then a separately authorized D00 baseline/inventory task. Do not create `com.immersive.composition`, modify `Packages/manifest.json`, alter Unity project assets or execute Unity merely by merging this document.
