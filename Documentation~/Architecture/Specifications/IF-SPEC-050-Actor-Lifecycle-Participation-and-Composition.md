# IF-SPEC-050 — Actor Lifecycle Participation and Composition

Status: **Draft for architectural review — non-normative until IF-ADR-050 and affected accepted decisions are reconciled**
Revised proposal: **2026-10-17 — independent architecture reconciliation**<br>
Proposed: **2026-10-17**<br>
Authority: [IF-ADR-050 — Actor Occurrence Lifecycle, Authority and Commit](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md)<br>
Governance: [IF-GOV-001 — API Maturity and Validation Governance](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

## 1. Purpose and evidence boundary

This specification expands the revised lifecycle authority into candidate, replacement, minimal composition-kernel integration, identity and failure contracts. IF-ADR-050 is the sole decision authority for the proposed direction. This file is a technical companion, not an accepted contract, implementation plan or API inventory.

The reviewed source demonstrates the following current behavior:

- `AttachedPlayerActorMaterializationAdapter` stages an inactive root, creates the runtime host and optional visual beneath it, establishes Actor/PlayerInput identity, registers logical materialization and returns a staged handle. During release it deactivates and detaches objects, schedules Unity destruction, then performs logical release. See [AttachedPlayerActorMaterializationAdapter](../../../Runtime/PlayerParticipation/Runtime/AttachedPlayerActorMaterializationAdapter.cs) and [RuntimeContent release adapter boundary](../../../Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs).
- `PlayerActorMaterializationHandle.TryActivate` enables visual and Actor roots before marking the handle Active. There is no generic operational authorization in the handle. See [PlayerActorMaterializationHandle](../../../Runtime/PlayerParticipation/Runtime/PlayerActorMaterializationHandle.cs).
- Player initial preparation activates the handle before adding the prepared record. Replacement mutates selected-Actor evidence before activating B, then updates the prepared record after activation. The physical current-source predicate requires the selected Slot/revision and prepared record to agree. See [PlayerActorPreparationRuntimeContext](../../../Runtime/PlayerParticipation/Runtime/PlayerActorPreparationRuntimeContext.cs) and [PhysicalOccurrence](../../../Runtime/PlayerParticipation/Runtime/PlayerActorPreparationRuntimeContext.PhysicalOccurrence.cs).
- `PreparedActorReplacement` releases current gameplay admission/occupancy before starting physical replacement and reprojects gameplay after B commits. Failure after B commit does not reconstruct A. See [PreparedActorReplacement](../../../Runtime/PlayerParticipation/Runtime/ActivityPlayerActorLifecycleParticipant.PreparedActorReplacement.cs) and [IF-ADR-024](../ADRs/IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md).
- The PlayerInput adapter gates a Gameplay Action Map; it is not an Actor-wide gate. `PlayerActorRuntimeHost` validates runtime composition and Actor identity but does not gate arbitrary Actor behavior. See [UnityPlayerInputGateAdapter](../../../Runtime/UnityInput/UnityPlayerInputGateAdapter.cs) and [PlayerActorRuntimeHost](../../../Runtime/PlayerParticipation/Authoring/PlayerActorRuntimeHost.cs).
- Player occurrence reference resolution asks the current physical source before creating evidence; the current Player identity path must be reconciled with the proposed candidate-time `ActorOccurrenceId`. See [ActorOccurrenceReferenceRuntime](../../../Runtime/Actors/References/ActorOccurrenceReferenceRuntime.cs), [IF-ADR-049](../ADRs/IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md) and [Actor Occurrence References guide](../../Guides/Actor-Occurrence-References.md).
- `RuntimeScopeCancellationToken` is a passive transition token. `RuntimeContentRuntime` is internal and delegates physical release to adapters; logical release is not proof of physical destruction. See [RuntimeScopeCancellationToken](../../../Runtime/RuntimeContent/RuntimeScopeCancellationToken.cs), [RuntimeContentRuntime](../../../Runtime/RuntimeContent/RuntimeContentRuntime.cs) and [IRuntimeReleaseAdapter](../../../Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs).

Unity may run `Awake` and `OnEnable` when an inactive hierarchy becomes active; `Start` is scheduled before the enabled component's first `Update`. Unity does not establish arbitrary cross-GameObject callback ordering. `Object.Destroy` is deferred until after the current Update loop and before rendering. These engine behaviors constrain guarantees; they do not implement the proposed protocol. See the Unity 6.5 references for [Instantiate](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Object.Instantiate.html), [SetActive](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/GameObject.SetActive.html), [Awake](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/MonoBehaviour.Awake.html), [OnEnable](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/MonoBehaviour.OnEnable.html), [Start](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/MonoBehaviour.Start.html) and [Object.Destroy](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Object.Destroy.html).

## 2. Identity, vocabulary and authority

The following is **proposed**, not a description of the present Player runtime. IF-ADR-050 owns the architectural decision; this section supplies its technical meaning.

| Identity or concept | Domain and lifetime | Authority and migration |
|---|---|---|
| `PlayerSlotId` | Configured/reusable Player slot, including across Join/Leave. | Player Participation; not a physical Actor reference. |
| `PlayerOccurrenceId` | One exact Joined Player participation (Join → Leave). | Player Participation; survives composition and Actor replacement; Camera membership already uses it. |
| `ActorId` | Logical Actor, independent of controlling Player and physical root. | Actor/producer contract. Current Player materializer generates it per materialization; reconcile that implementation and IF-ADR-023A. Never universally derive from PlayerOccurrenceId. |
| `ActorProfileId` | Reusable authoring/selection asset identity. | Authoring/Player selection; profile change need not imply new logical Actor. |
| `ActorOccurrenceId` | One **committed** physical incarnation; stable despite temporary inactivity or recomposition. | Core authoritative lifecycle, minted at commit; not minted by resolvers or on candidate acceptance. Reuse of GameObject after release establishes another ID. |
| Candidate operation identity | Exact preparation/materialization attempt, owner, root and expected revisions. | Existing `PlayerActorMaterializationOperationId` is a possible Player correlation source; candidates are not public Current occurrences. Do not add a redundant public candidate ID. |
| Preparation token | Evidence of a particular preparation and its selection/materialization context. | Remains preparation evidence, not a logical or physical Actor identity. |
| Composition revision | Monotonic counter of successful keyed content commits **within one occurrence**. | Composer; changes without changing ActorId or ActorOccurrenceId. Per-operation validation still requires target/owner/generation checks. |
| Content part | Definition plus a prepared/applied instance or data value associated with a content key. | Materializer owns resource acquisition and release; Composer owns application/binding. |
| Contribution | Owner-scoped desired entries with precedence. | Contributor owns intent; it is not necessarily owner of assets, parts or root. |

For NonPlayer there may be no Slot or PlayerOccurrenceId. For a Player switching controlled logical Actors during the same Join, PlayerOccurrenceId remains but ActorId may change. For a replaced root of the *same* logical Actor, ActorId remains and ActorOccurrenceId changes. Profile replacement is a content operation when no physical replacement is required; the exact rule is an authoring/product policy, not an identity axiom.

**Identity must not be derived from availability.** Existing `ActorOccurrenceReferenceRuntime.TryGetPlayer` drops the record when its active-root predicate fails and mints a new ID on later resolution. The proposed binding must retain committed identity until lifecycle invalidation, while availability/readiness is separately observable. The ID is allocated by commit, regardless of whether a resolver is ever called. A host-local ID must be interpreted in its declared namespace; don't present its numeric value alone as globally unique.

## 3. Modular boundary — minimal composition kernel

### 3.1 Chosen proposed direction

```text
Unity consumer
     └── com.immersive.framework
            ├── Actor/Player integration: ActorProfile, root lifecycle, owner bridge,
            │   Camera and gameplay participation
            └── com.immersive.composition (required UPM dependency)
                   └── keyed content engine; no Actor/Session/Route/Activity types
```

`com.immersive.composition` is a separately versioned **generic and small** UPM package. It depends on no Framework assembly and does not interpret `ActorId`, `ActorOccurrenceId`, `RuntimeContentOwner` or Player selection. Framework Actor integration calls the kernel directly. Existing `ActorProfile.presentationPrefab` and `PlayerActorRuntimeHost.VisualContentMount` migrate to the **Base contribution** at the `visual` key and its mount; the old manual visual instantiation/release path cannot remain as a parallel runtime implementation.

The package dependency is mandatory *for the Framework package* if adopted, but runtime Composer creation can be lazy or omitted for Actors without content entries. This removes the previous proposed host-provider discovery seam and the Core-owned “Composition package required/optional” flag. It does not require every consumer to author content contributions.

This boundary is preferred for unified Player content integration and smaller Framework lifecycle contracts, **not** because inversion of dependency is impossible. The old alternative `actor-composition → framework` is technically valid but requires additional provider integration and/or authoring indirection. No actual second non-Actor consumer has validated the kernel's generality yet; keep its API minimal, and recheck the separation before Stable API publication.

### 3.2 Contract inventory — conceptual, not C# signatures

| Contract | Kernel or Framework owner | Required guarantee / limitation |
|---|---|---|
| Content key / mount | Kernel abstraction, host supplies mount | Distinguishes replaceable positions/values. Cardinality-one per key initially; no mandatory SlotDefinition asset. |
| Part definition | Kernel | Describes data or physical part and its required keys; definitions do not own instances. |
| Part materializer | Kernel interface, concrete provider owns resources | Validates, prepares (possibly async), applies/detaches and releases. Staging contracts are explicit; a pooled `Rent` that activates is **not** equivalent to inactive preparation. |
| Composer | Kernel | One serialized commit authority per composition root; resolves desired entries by key, prepares affected dependency closures, performs bounded synchronous application, increments revision, emits delta after commit, rejects/defers reentrant writes by explicit policy. Never owns root creation/destruction. |
| Owner-scoped contribution | Kernel handle; Framework bridges its owners | Priority and desired key→part mapping; owner close removes intent and invalidates work. Equal-priority competing owners for the same key are rejected, not resolved by incidental order. |
| Result and delta | Kernel | Exact changed keys, revision, committed/rejected/stale/cancelled/degraded/faulted outcome and per-key issues; doesn't imply Unity callback or resource-release success. |
| Actor content integration | Framework | Maps profile Base and further owner-scoped contributions to Composer, finds exact occurrence/root, bridges Session/Route/Activity ownership and cancellation. |
| Actor occurrence binding/commit | Framework | Validates expected logical Actor, owner, selection, candidate operation and root; owns the **only** Current commit and publication. Not a content revision commit. |
| Lifecycle/availability observation | Framework | Separates exact committed identity, active root, gameplay admission and readiness. Read-only subscribers never see an intentionally published half-selection. |

Authoring of generic PartDefinition/prefab or data materializers should support validation of mounts, missing dependencies, cycles and prohibited embedded components (e.g., PlayerInput or ActorDeclaration inside replaceable visuals). Inspector integration in the Framework uses Actor terminology; kernel APIs must not adopt those domain concepts.

### 3.3 Deferred abstractions

Do not require `ActorShell` as a new universal class, `SlotDefinition` assets, fixed Base/Selection/Loadout/Activity layer taxonomy, `CapabilityTable`, sockets, generic `IConfigurable<T>` / part-state-transfer hierarchies, an Actor-specific provider plugin system, a Framework-owned NonPlayerSpawner, or generalized global asset lease registries in the first contract. Preserve extension seams in materializers/declared dependencies without promising these capabilities now.

## 4. Keyed composition and contribution commits

### 4.1 Desired state and dependency closure

The Composer resolves the **winning contribution per content key** from exact owner-scoped entries and priority. A key can be independently updated unless it participates in a declared dependency closure. If `visual` changes and `weapon` depends on `visual`, the two are validated/prepared/committed as one affected group. Reject missing dependencies, cycles and conflicting equal-priority contributors **before** applying effects.

Preparation of unrelated keys may proceed concurrently. Slow/failing resource preparation for one independent key must not indefinitely block another key. Do **not** use a single global desired-state transaction that restarts every key whenever an unrelated contribution changes.

The Composer exposes one monotonically increasing **public revision per occurrence**, useful for cache invalidation and observation. It also requires *internal* operation generation/expected-target verification for affected keys/closure, so two concurrent preparations cannot apply stale results. A global revision is not a global lock: incrementing it does not by itself invalidate unrelated preparation.

### 4.2 Initial preparation and subsequent recomposition

**Initial Manager-Provisioned content:** producer stages the Actor root inactive; Framework resolves Base content from the selected profile; kernel prepares parts under inactive staging and attaches compatible parts while the Actor root is still inactive. Framework validates candidacy and later commits the physical occurrence. The content's staged/internal revision does not make an Actor publicly Current.

**Subsequent content change:** materializer stages new part(s), Composer validates exact occurrence/owner/targets and dependencies, applies the affected closure synchronously, commits its content revision and emits a delta **after** application. ActorId, ActorOccurrenceId, PlayerOccurrenceId and root remain unchanged. Systems caching child Transform/Animator or data-provider handles must rebind after a delta. Camera targets should be root-owned stable anchors by default; a target under replaceable content requires deliberate rebound behavior.

**Scene-Provided or external NonPlayer:** can supply already-existing content as **adopted parts**. Adoption records provenance and owner; Composer may detach its association but cannot destroy Scene/producer-owned resources. Adoption does not retroactively suppress prior `Awake`/`OnEnable`. A NonPlayer producer retains authority over its root without needing a Player slot.

### 4.3 Commit boundary and callbacks

```text
Resolve winning keys → stage/prepare inactive → validate dependency closure
      → no-return application boundary
      → detach old (do not reparent activeSelf=true into active scene root)
      → attach new in dependency order → content revision++
      → notify delta → schedule old-resource release by its owner
```

This call is synchronous but **not callback-atomic**. `OnDisable`/`Awake`/`OnEnable` may run during detach/attach, observe intermediate hierarchy and cause external effects. A generic rollback by reattaching old parts cannot undo these effects or guarantee continuity of Animator state. After no-return, unexpected failure is reported as degraded/faulted rather than automatically “fully compensated”. Future optional per-part readiness probes can help detect failures of cooperating components; don't assume `SetActive` communicates every callback exception.

Materializer obtains/releases assets and instances it owns. Composer applies/removes part bindings, not the producer root. A resource obtained by a stale asynchronous attempt is released by that attempt's acquiring provider, even when Actor or contribution owner has already closed.

## 5. Occurrence replacement C01 — synchronous guarded transition

### 5.1 Required ordering

1. **Capture A:** exact current binding, ActorId, `PlayerOccurrenceId` if any, current selection revision, owner and operation.
2. **Prepare B:** stage the new root and required content while A remains Current and operating. Candidate correlation uses operation/owner/root, not a prematurely assigned public ActorOccurrenceId.
3. **Validate and place B inactive:** check profile, authoring, owner, mount, expected A and selection, dependencies and spatial-entry policy/anchor. Apply B's pose while inactive; **do not commit a shared Route “already applied” marker** until the occurrence transition succeeds. Preserve A's pose for same-participation replacement on the same Route where appropriate.
4. **Guard the lifecycle domain:** enter a bounded non-reentrant barrier. Incompatible nested selection/replacement/Leave requests must return explicit rejected/busy results. **Forced owner closure/invalidation must remain authoritative**: record closure/invalidation and reject B commit, rather than silently losing the event.
5. **Physical handoff:** deactivate A, then activate B. Keep public/committed selection and identity pointing to A during `OnDisable(A)`, `Awake(B)` and `OnEnable(B)`. B's callbacks must not assume candidate is already Current. Domain-specific gameplay/input gates remain closed for B.
6. **Revalidate and commit:** verify exact owner still valid, expected A binding/selection still match, B structure remains valid and closure was not requested; perform a coherent Current+selection commit and allocate B's ActorOccurrenceId exactly once; publish changes only after the logical update has completed.
7. **After commit:** project Camera subject and gameplay/readiness, retire A and release through owners (without reactivating visual during detach). Late/stale work cannot publish or update another occurrence.

**No async suspension is permitted inside the guarded handoff.** Async preparation is allowed before it; any event or request that is not a supported nested observation must not execute a competing mutation in the middle.

### 5.2 Resolver and callback-observation contract

| Observation point | Selected profile / committed binding | Physical evidence and availability | Allowed mutation |
|---|---|---|---|
| Before handoff; B preparing | A | A current/active; B staged and private | Standard owner-scoped operations, validated against current revision. |
| `OnDisable(A)` | A | A is still logically Current but may be inactive; it retains the same occurrence ID. | Incompatible nested mutation explicitly rejected; forced closure invalidates transaction. |
| `Awake(B)` / `OnEnable(B)` | A | A is still committed, but unavailable; B is private candidate, not resolved as Current. | Same barrier. Candidate setup via operation-specific references only. |
| Immediately after successful commit, before dispatch | B | B is committed with new exact occurrence ID and validated physical root. | Same barrier until ordered publication and transition finalization. |
| Notification and later queries | B | B is Current, readiness/gameplay availability separately evaluated. | Normal operations after barrier exit. |

**Important migration:** current `TryGetCurrentPhysicalActorOccurrence` rejects inactive root/handle and `ActorOccurrenceReferenceRuntime` discards the matching ID when that predicate fails. Under the proposed model, a *committed-binding query* can still identify A during the barrier while an *active-physical-evidence query* reports unavailable. A caller relying on the current “resolvable means physically active” interpretation of IF-ADR-049 must be migrated explicitly; simply changing the predicate without distinguishing query semantics would silently weaken the accepted contract.

Use typed results (current-but-inactive/unavailable versus no committed occurrence) rather than silently minting an ID or treating B as Current. No observer notification may be dispatched before the binding/selection change is coherent. Event subscriber exceptions are diagnostic and cannot roll back a committed identity.

### 5.3 Failure boundary and specialized gates

The last wholly reversible stage ends **before deactivating A**. Once A is disabled, its `OnDisable` effects have occurred. If B cannot be committed, A may remain last Current but unavailable; the operation must return a **degraded or partial handoff** result and explicit recovery obligation. “A preserved” alone does not mean A usable. After B commits, B remains authoritative even when readiness, gameplay admission, callback-probe evidence or cleanup fails; report degraded availability rather than a fictional rollback.

A generic occurrence-scoped operational capability/epoch is **not** required for the default no-multi-frame-coexistence replacement. Input, gameplay readiness/admission, pause and transition continue to use specialized domain gates. The Framework cannot suppress arbitrary non-cooperative callback side effects. A stronger multi-frame coexistence contract would need a separate accepted requirement and isolation design.

**Route placement:** current `RoutePlayerSpatialEntryRuntimeBinding.TryApplyBeforeActivation` combines policy checks, pose effects and “already applied” bookkeeping. Decompose candidate-local validate/plan/apply from shared commit confirmation. A candidate failure must not falsely consume the Route placement for the eventual committed Actor.

## 6. Actor/Composition lifecycle integration

```text
Producer owns candidate root
     ↓
(optional) Composer prepares required Base content; candidate not public
     ↓
Core/producer validates exact owner + operation + previous binding
     ↓
guarded synchronous physical handoff; callbacks may observe transition
     ↓
Core commits ActorOccurrenceId + Current/selection → publish
     ↓
Composer may recompose content on the same occurrence; revisions advance
     ↓
Core invalidates occurrence at leave/release/replacement
     ↓
Composer invalidates work/detaches parts; resource owners release parts
     ↓
Producer releases root; cleanup outcomes remain attributable to owning scope
```

Staged Player materialization has stronger pre-activation guarantees than Scene-Provided adoption or third-party NonPlayer publication. Late adoption is valid but cannot promise prior callbacks were staged. Composition itself neither grants current Actor authority nor performs a physical Actor replacement. A physical root replacement is never inferred from a content-only commit; choosing whether a profile change is content-only or requires another root is a separate product/authoring policy.

Readiness is independent of the existence of the logical binding and content revision. A successful content commit may lead to degraded readiness. Camera assignment remains owned by the relevant Player/Camera contract, not Composer. Reset/Pause integrations should consume composition deltas where appropriate, not acquire ownership of parts from the kernel.

## 7. Closure, cancellation and release

1. Core invalidates the exact occurrence and rejects new/current commits when its owner closes; owner invalidation during a guarded transition must be observed before any proposed commit.
2. Framework bridges owner-scoped cancellation to pending Composer/materializer operations. The existing `RuntimeScopeCancellationToken` is passive; an active async cancellation mechanism would be new implementation, not a current feature.
3. Composer stops accepting the closed occurrence, detaches dependent parts safely and asks each materializer to release only what it acquired/owns. Adopted Scene parts are not destroyed by the Composer.
4. The producer releases its root according to the owner contract. Failed cleanup of unrelated parts should not block independent releases.
5. Results distinguish **release requested**, **completed under the adapter's definition**, **pending** or **failed**, with exact owner/resource/operation diagnostics. A `Destroy` request is not proof of physical destruction; avoid a blanket “fully physically destroyed” report.
6. If a future adapter supports async completion/retry, its cleanup obligation must be retained by an owner whose lifetime outlives the closing root; do not create a global receipt ledger or assume an async provider exists today.

Do not modify another owner's resources merely to simplify Actor teardown. For Scene unload or forced destruction, best-effort behavior must not be misrepresented as guaranteed callbacks, cancellation or release.

## 8. Failure/edge-case matrix

| Case | Required result and evidence |
|---|---|
| **F01 — Initial preparation rejected** | A unchanged (if present), B not committed; materializer frees staged acquisitions. |
| **F02 — B physical activation invalid** | If detected after A deactivation, return degraded/partial transition; B not published. Never claim A automatically restored. |
| **F03 — Reentrant selection/Leave during handoff** | Explicit rejection for conflicting voluntary mutations; forced owner closure invalidates B commit, never silently dropped. |
| **F04 — B committed, gameplay or content readiness fails** | B stays Current, report degraded readiness; A does not become Current again by rollback. |
| **F05 — Release pending or failed** | Logical closure persists; adapter/owner reports requested vs completed vs pending/failed and retains any actual retry obligation. |
| **F06 — Owner closes during async preparation** | Invalidate operation; late results refused and released by acquiring provider. |
| **F07 — Arbitrary Unity callbacks** | `Awake`/`OnEnable`/`OnDisable` may perform effects; no universal suppression, detection or reversal. |
| **F08 — Scene-Provided already active** | Late adoption marked; prior callbacks cannot be undone; adopted part root/owner not destroyed. |
| **F09 — Composition absent in authoring** | Framework and Actor can run without content entries; kernel may be installed but need not construct a Composer. |
| **F10 — Concurrent keyed updates** | Verify exact desired target, owner and dependency closure; stale result cannot apply another key/occurrence. |
| **F11 — Callback exception during SetActive** | Framework never equates `TryActivate` return with proof every component initialized; optional explicit probe may diagnose. |
| **F12 — Callback destroys mount or changes root** | Revalidate structure after effects; report degraded/faulted rather than post-effect safe rollback. |
| **F13 — Adopted Scene visual** | Record ownership/provenance; detaching must not destroy borrowed content or duplicate the visual. |
| **F14 — Pool activates on Rent** | Current pooling `Rent` is not inert; only implement pooling materializer after explicit inactive-rent/preparation contract. |
| **F15 — Cached child references** | Anchor Camera to stable root by default; listeners holding changed part components must rebind on delta/revision. |
| **F16 — Release visual reactivation** | Never detach `activeSelf=true` content from an inactive parent into an active hierarchy as part of cleanup. |
| **F17 — Temporary Actor inactivity** | Keep committed occurrence ID; report availability separately. No new ID minted on resolver retry. |
| **F18 — Logical Actor/Player participation diverge** | PlayerOccurrenceId remains Joined while another ActorId is controlled; selection/binding must follow explicit domain semantics. |

## 9. Current-code findings requiring independent attention

| Finding | Current evidence | Proposed owner/correction |
|---|---|---|
| N1 visual reactivation during release | `AttachedPlayerActorMaterializationAdapter.TryReleaseMaterialization`: deactivates host then re-parents activeSelf visual to scene root before `Destroy`. | Producer/materializer release; avoid reactivation even before Composition ships. |
| N2 early selection publication | `PlayerActorPreparationRuntimeContext.TryReplacePreparedActor` calls selection mutation before B activation and registration; `CommitActorSelection` publishes Changed synchronously. | Player selection + occurrence commit boundary. Camera should reconcile post-commit. |
| N3 rollback stales old preparation | Failed activation restores selection via another revision increment but A record retains previous revision. | Remove premature selection mutation rather than simulating rollback. |
| N4 lazy occurrence identity | `ActorOccurrenceReferenceRuntime.TryGetPlayer` mints ID during lookup and removes record when active-root predicate fails. | Move identity authority to commit and preserve across temporary unavailability. |
| N6 physical coexistence | Replacement activates B before releasing/deactivating A. | Use guarded order A inactive → B active, allowing for non-cooperative callbacks as a separate limitation. |

These are source-observed paths, not evidence of a Unity execution. The accepted public API surface must not be changed by this documentation proposal.

## 10. Decision register

### Proposed architectural choices consolidated for review

| Choice | Proposed disposition | Qualification |
|---|---|---|
| Package boundary | Required `framework → composition` with small generic kernel. | Package not yet created; no non-Actor consumer proof yet. |
| Content model | Key + part + materializer + Composer + owner-scoped contribution + delta/revision. | Authoring/UI signatures remain undecided. |
| Transaction unit | Desired state per key, group by declared dependency closure, one public revision per occurrence. | Internal stale-result generation checks mandatory. |
| Physical handoff | Guarded synchronous A deactivate → B activate → commit → publish. | Not callback-atomic; degraded transition result retained. |
| Identity | Player participation, logical Actor and committed physical occurrence stay distinct. | Exact Player ActorId issuance/migration still requires acceptance. |
| Operational gate | Keep specialized gates; no universal capability contract without a new product requirement. | Cannot claim suppression of arbitrary callbacks. |
| Cleanup | Per-owner exact outcome; do not assert `Destroy` confirmation or invent global ledger. | Async future adapters must define longer-lived retry owner. |
| Content authoring | Profile Base entry + visual mount; one runtime content path. | Migration of serialized assets requires plan. |

### Residual decisions and evidence gates

| Open item | Evidence needed and impact |
|---|---|
| Accepted-contract reconciliation | Review amendments to IF-ADR-003/023A/024/038/049 (and relevant 014/020/025 scopes) before implementation. Proposed architecture cannot silently override accepted replacement/reference semantics. |
| Unity callback exception propagation | Small isolated Unity 6000.5 proof if claiming `SetActive` propagates/does not propagate callback exceptions. Conservative contract already avoids either promise. |
| Profile replacement policy | Decide when a new root/ActorId is intentionally requested versus a content-only change; impacts authoring and migration, not generic Composer authority. |
| Authoring/typed API shape | Decide Inspector serialization, default key/mount validation, public identity namespace and exact C# signatures in design/API review; these are not defined here. |
| Async/pooling implementations | Require separate provider capability and staged/active semantics before claiming safe pooling or Addressables support. No such feature is asserted today. |

## 11. Existing ADR reconciliation

| Existing decision | What remains authoritative | Reconciliation required if this proposal is accepted |
|---|---|---|
| IF-ADR-001 | Exact owner/scoped teardown authority. | Actor/part release honors root and resource owners. |
| IF-ADR-003 | Session Player selection, participation and Actor preparation authority. | Commit selection only with coherent current binding; no premature Changed event. |
| IF-ADR-014 | Stable authored identity/owner-scoped binding domains. | Profile and part definitions are not runtime occurrence identities. |
| IF-ADR-020 | Session leave/resource release ownership. | Retain independent resource ownership; adopted Scene parts not destroyed by Composer. |
| IF-ADR-023A | Logical Actor identity authority (documented in reviewed identity contract). | Do not alias ActorId to PlayerOccurrenceId; migrate per-materialization Player ActorId carefully. |
| IF-ADR-024 | Accepted Manager-Provisioned replacement API and pre/post-commit semantics. | A remains Current while B stages; resolve availability during handoff and preserve degraded failure outcomes. |
| IF-ADR-025 | Player input physical/device ownership. | Composition cannot own PlayerInput or displace existing specialized input gates. |
| IF-ADR-038 | Root spatial identity and Player/Camera assignment separation. | Preserve PlayerOccurrenceId and Camera subject boundaries; default anchor remains root-stable. |
| IF-ADR-040 | Bounded discovery and Scene composition ownership within its scope. | Useful validation precedent, not authority for Actor part composition. |
| IF-ADR-049 | Exact public Actor occurrence resolution and invalidation. | Allocate ID at lifecycle commit; distinguish committed identity from active-root availability; change accepted resolver semantics only through explicit reconciliation. |
| IF-GOV-001 | Per-contract API maturity, validation governance and Stable protection. | Proposed kernel/Core APIs are not Stable, not implemented and not an API maturity map change. |

## 12. Readiness for review

This document and IF-ADR-050 remain **non-normative proposals**. They provide one coherent target for lifecycle authority and a small generic composition kernel; no C# signatures, UPM files, assemblies, code, tests, assets or consumer migration are implemented.

A bounded Unity proof may record `OnDisable(A)`, `Awake(B)`, `OnEnable(B)`, active-state/Current observations and callback exception behavior, using the canonical logger. This validates the engine-facing assumptions without duplicating unrelated Framework validation. It does not block a conservative documentation review.

The next formal action, **after architectural review**, is to reconcile the accepted ADR contracts explicitly, then author a separate implementation/migration plan. Neither this draft nor IF-ADR-050 automatically supersedes those authorities.
