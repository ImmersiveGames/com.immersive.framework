# IF-ADR-050 — Actor Occurrence Lifecycle, Authority and Commit

Status: **Accepted — target architecture; implementation and validation pending**
Accepted: **2026-10-17**  
Proposed: **2026-10-17**  
Revised proposal: **2026-10-17 — independent architecture reconciliation**  
Type: architecture / Actor / lifecycle / runtime authority  
Related decisions: [IF-ADR-001](IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md), [IF-ADR-003](IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md), [IF-ADR-014](IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md), [IF-ADR-020](IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md), [IF-ADR-024](IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md), [IF-ADR-025](IF-ADR-025-Local-Player-Input-Ownership-and-Device-Association.md), [IF-ADR-038](IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md), [IF-ADR-040](IF-ADR-040-Scene-Composition-Binding-Model.md), [IF-ADR-049](IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md); identity semantics also require reconciliation with IF-ADR-023A.  
Governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)  
Companion design: [IF-SPEC-050 — Actor Lifecycle Participation and Composition](../Specifications/IF-SPEC-050-Actor-Lifecycle-Participation-and-Composition.md)  
Accepted-target contract migrations: [IF-ADR-050 migration boundary — 2026-10-17](../Reconciliation/IF-ADR-050-ACCEPTED-TARGET-CONTRACT-MIGRATION-2026-10-17.md)
Accepted-ADR reconciliation: [IF-ADR-050 scoped crosswalk — 2026-10-17](../Reconciliation/IF-ADR-050-ACTOR-LIFECYCLE-COMPOSITION-RECONCILIATION-2026-10-17.md) (**accepted target migration boundary; current runtime guarantees unchanged**)

## Context

Actor authoring identity, Player participation, a physical Actor incarnation, its current binding, runtime composition, physical activity and gameplay readiness are different facts. Current Player materialization stages a root and optional visual content inactive; its replacement path changes and publishes selection before activating and registering B, leaving intermediate states visible to observers. Player occurrence identity is currently minted by resolution rather than owned by lifecycle commit. The current release path can detach still-active visual content under an active scene root before deferred destruction.

Two independent architecture assessments challenged the initial proposal and identified the need for a *real content-composition mechanism*, not only Actor replacement coordination. The resulting target architecture is now accepted for future design and implementation planning. Current published/implemented contracts and historical certification remain authoritative until their exact migrations are implemented and validated; this acceptance does not claim runtime or API readiness.

## Decision — accepted target architecture

### 1. Authorities and package boundary

- **Framework Core** owns Actor logical identity and physical occurrence identity, Player participation integration, current binding, conditional occurrence commit, publication and invalidation, gameplay-specific gates and Camera/readiness integration.
- **Producer** creates/adopts, stages, activates, deactivates and physically releases the Actor root according to its existing owner contract. Composition does not take ownership of the root.
- **`com.immersive.composition`** is selected as an **independently versioned, small generic UPM kernel** depended on by `com.immersive.framework`. It owns keyed content reconciliation and commit, not Actor, Player, Session, Route, Activity, `RuntimeContentOwner` or Actor identity.
- **Framework Actor integration** maps `ActorProfile` authoring and owner-scoped lifecycle to this kernel. It calls the kernel directly, without a mandatory-provider discovery seam or a second visual materialization mechanism.
- **Content materializers/adapters** own preparation, application-specific effects and resource release. **Contribution owners** own their intent, not necessarily the created resource or root.

Accepted target dependency direction:

```text
Consumer → com.immersive.framework → com.immersive.composition
```

The composition kernel has no dependency on Framework; it must remain useful without importing Actor types. A Framework consumer with no content entries need not create a Composer at runtime. The additional UPM dependency is nonetheless mandatory if this option is adopted.

### 2. Identity domains

- `PlayerSlotId` identifies a reusable configured Slot; `PlayerOccurrenceId` identifies one exact joined participation from Join to Leave.
- `ActorId` identifies a **logical Actor**, not necessarily a Player participation, a selected profile or a particular root. One participation may control another logical Actor. The current Player materialization-derived `ActorId` must be reconciled with this contract rather than automatically aliased to `PlayerOccurrenceId`.
- `ActorOccurrenceId` identifies **one committed physical incarnation**, assigned on authoritative occurrence commit, never minted by a resolver and never replaced merely because its root becomes temporarily inactive. A replacement/reuse of the root establishes another occurrence; a change of composition does not.
- `ActorProfileId` identifies authored selection/configuration; it is neither logical Actor nor physical occurrence identity.
- A pre-commit candidate is correlated by an **operation identity**, exact producer/root and owner context. It is not publicly resolvable and does not require an ActorOccurrenceId; late/aborted results must fail validity checks.
- A per-occurrence composition revision changes on a successful content commit without changing any Actor identity.

This is an accepted semantic/API migration target, not a description of the existing Player declaration/reference implementation.

### 3. Occurrence transition and visibility

The preferred replacement is **staged preparation followed by a synchronous, non-reentrant transition**. It does not claim atomicity for Unity callbacks.

1. Capture expected A binding, selection, owner and operation generation. Prepare B/root and mandatory content inactive; A remains Current and operational.
2. Validate preparation and spatial entry. Apply candidate-local placement while B is inactive, without prematurely changing shared Route-completion evidence.
3. Enter a bounded transition barrier for the relevant lifecycle domain. Reject incompatible reentrant mutation requests explicitly; owner loss or forced closure must still invalidate/cancel the operation.
4. Deactivate A, then activate B. During `OnDisable(A)`, `Awake(B)` and `OnEnable(B)`, the **committed binding and selected profile still denote A**; B is a private candidate. Gameplay/input gates remain closed for B.
5. Revalidate physical evidence, owner and expected selection/binding. If valid, **commit B and selection together**, assign B's ActorOccurrenceId once and publish changes only after the coherent update. Notifications are read-only observations, not part of the commit.
6. Reproject gameplay/Camera as appropriate; release A without reactivating detached content. Dispose staged/cancelled resources by their owners.

During the barrier, resolution of the committed A identity must not generate a fresh ID or silently redirect to B. A can be `Current` while physically inactive or unavailable: **logical identity and physical/operational availability must be reported distinctly**. Existing `activeInHierarchy` predicates cannot remain the authority for an occurrence ID's lifetime.

**Failure boundary:** A failure before deactivating A preserves A. After deactivation, physical callbacks and effects are not transactionally reversible. If B cannot commit, A may remain the last committed identity but physically unavailable; expose a **degraded/partial transition result** and explicit recovery, not a successful rollback. A post-commit failure keeps B committed and reports degraded readiness/composition or cleanup, never a fictitious restoration of A. Unity may run arbitrary non-cooperative callback effects during this synchronous call; a same-frame transition is not callback-atomic.

Specialized gameplay, input, pause and transition gates retain their domain authority. No universal occurrence-capability/epoch gate is required without a demonstrated requirement for physically coexisting Actors across multiple frames. Components participating in staged activation must not assume their candidate is already Current during `Awake`/`OnEnable`.

### 4. Content recomposition, distinct from Actor replacement

Content may be changed **without** changing ActorId, ActorOccurrenceId, root, Player participation or gameplay state owned by the root.

The minimum composition model consists of **content key**, **part definition**, **part materializer**, **Composer**, **owner-scoped contribution with precedence**, and **result/delta with revision**. A Composer resolves desired content by key and commits independently safe keys or the explicit dependency closure together. Dependency cycles and equal-priority conflicting owners for the same key are rejected. Independent updates need not wait on unrelated materialization.

The existing `ActorProfile.presentationPrefab` and `VisualContentMount` must migrate to the **same** Base visual contribution and mount used for subsequent recompositions. The runtime must not maintain two divergent paths for visual content. Content adopted from a Scene remains owned by the Scene and is not destroyed as though Composer-created.

This ADR defines only the architectural boundary. Detailed composition and authoring contracts belong in IF-SPEC-050 and ultimately the kernel package documentation.

### 5. Closure and resource ownership

Logical closure invalidates Current/publication and stale commits first. Composer cancels/revalidates pending work, detaches dependent parts and asks the responsible materializers to release their resources; producer retains root teardown authority. A staged resource is released by its acquiring owner, even if its result arrives after cancellation. An adopted Scene part must not be destroyed by Composition.

Distinguish at least **release requested**, **release completed to the adapter's stated contract**, **pending** and **failed** where applicable. Scheduling Unity `Destroy` is not confirmation of physical destruction. Do not force a universal cleanup state machine or receipt ledger for resources that do not need one; any asynchronous retry obligation must outlive the object it cleans and remain with an appropriate longer-lived owner.

## Accepted scope and excluded commitments

Scope includes Manager-Provisioned Player, Scene-Provided/adopted Actors and externally produced NonPlayer occurrences, subject to their different staging/ownership guarantees. No global Actor Manager, generalized mandatory `ActorShell` class, global service locator, fixed gameplay Layer taxonomy, unconditional automatic rollback of Unity effects, or public C# signatures are decided.

No claim is made that every MonoBehaviour obeys staged activation, that `SetActive` catches callback exceptions, or that frame-boundary atomicity implies callback-observer atomicity. Any runtime validation of callback exception propagation in the target Unity version is future bounded evidence, not an approval prerequisite for the conservative contract.

## Consequences, migration and reconciliation

- Player selection and physical occurrence commit must become coherent; the current published selection event cannot fire during intermediate materialization.
- Exact ActorOccurrenceId identity must be owned by the authoritative lifecycle and survive temporary inactivity, not be minted on first lookup.
- `PlayerOccurrenceId`, `ActorId` and `ActorOccurrenceId` must retain separate semantics; Player's current per-materialization `ActorId` and camera subject identity require migration analysis.
- Spatial entry must separate candidate-local placement from shared Route completion evidence. Camera observation should remain on a stable root-owned anchor by default; consumers caching replaced part references need composition-delta rebinding.
- Content replacement no longer implies physical Actor replacement. Physical replacement remains an explicit producer/lifecycle operation.
- This decision replaces the earlier proposed UPM direction from `actor-composition → framework` to `framework → composition`. The earlier candidate remains historical design rationale only, not a second supported runtime architecture.
- New contracts remain **unimplemented**; when introduced, public surfaces begin Experimental unless separately accepted under IF-GOV-001. A separate implementation/migration plan and evidence are required.

Current-code findings to track independently of package development: reactivation of visual during release; selection notifications before B is ready; selection rollback invalidating A's retained revision; resolver minting/losing occurrence IDs on transient predicate changes; and B activation while A remains physically active. These are reviewed code risks/defects, **not** claims of an executed Unity test.

## Normative dependency disposition

IF-ADR-003, IF-ADR-020, IF-ADR-024, IF-ADR-025, IF-ADR-038 and IF-ADR-049, together with the relevant identity decisions, remain authoritative for the current implemented/published contracts. This ADR is the **accepted future architecture target**, not retroactive evidence or a silent behavioral amendment to already distributed APIs. The [accepted-target migration contract](../Reconciliation/IF-ADR-050-ACCEPTED-TARGET-CONTRACT-MIGRATION-2026-10-17.md) explicitly maps the required future amendments, including the historical IF-ADR-023A interpretation, compatibility review and release/evidence gates. Those migrations become effective for consumers only when implemented, validated and released.

## Review readiness

**Architecture accepted on 2026-10-17 for implementation planning.** The [accepted-target migration contract](../Reconciliation/IF-ADR-050-ACCEPTED-TARGET-CONTRACT-MIGRATION-2026-10-17.md) defines exact future-vs-current boundary conditions. IF-SPEC-050 owns detailed conceptual design and failure cases. No new UPM package, source implementation, Unity execution, public API promotion or release is asserted. Remaining API signatures, serializer migration and target-specific tests belong to a separate implementation plan; they do not undo this architecture decision.