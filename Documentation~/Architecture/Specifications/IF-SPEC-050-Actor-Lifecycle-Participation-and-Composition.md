# IF-SPEC-050 — Actor Lifecycle Participation and Composition

Status: **Draft for architectural review — non-normative until IF-ADR-050 is accepted**<br>
Proposed: **2026-10-17**<br>
Authority: [IF-ADR-050 — Actor Occurrence Lifecycle, Authority and Commit](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md)<br>
Governance: [IF-GOV-001 — API Maturity and Validation Governance](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

## 1. Purpose and evidence boundary

This specification expands the proposed lifecycle authority into candidate, replacement, package integration, composition and failure contracts. IF-ADR-050 is the sole decision authority for the proposed direction. This file is a technical companion, not an accepted contract, implementation plan or API inventory.

The reviewed source demonstrates the following current behavior:

- `AttachedPlayerActorMaterializationAdapter` stages an inactive root, creates the runtime host and optional visual beneath it, establishes Actor/PlayerInput identity, registers logical materialization and returns a staged handle. During release it deactivates and detaches objects, schedules Unity destruction, then performs logical release. See [AttachedPlayerActorMaterializationAdapter](../../../Runtime/PlayerParticipation/Runtime/AttachedPlayerActorMaterializationAdapter.cs) and [RuntimeContent release adapter boundary](../../../Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs).
- `PlayerActorMaterializationHandle.TryActivate` enables visual and Actor roots before marking the handle Active. There is no generic operational authorization in the handle. See [PlayerActorMaterializationHandle](../../../Runtime/PlayerParticipation/Runtime/PlayerActorMaterializationHandle.cs).
- Player initial preparation activates the handle before adding the prepared record. Replacement mutates selected-Actor evidence before activating B, then updates the prepared record after activation. The physical current-source predicate requires the selected Slot/revision and prepared record to agree. See [PlayerActorPreparationRuntimeContext](../../../Runtime/PlayerParticipation/Runtime/PlayerActorPreparationRuntimeContext.cs) and [PhysicalOccurrence](../../../Runtime/PlayerParticipation/Runtime/PlayerActorPreparationRuntimeContext.PhysicalOccurrence.cs).
- `PreparedActorReplacement` releases current gameplay admission/occupancy before starting physical replacement and reprojects gameplay after B commits. Failure after B commit does not reconstruct A. See [PreparedActorReplacement](../../../Runtime/PlayerParticipation/Runtime/ActivityPlayerActorLifecycleParticipant.PreparedActorReplacement.cs) and [IF-ADR-024](../ADRs/IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md).
- The PlayerInput adapter gates a Gameplay Action Map; it is not an Actor-wide gate. `PlayerActorRuntimeHost` validates runtime composition and Actor identity but does not gate arbitrary Actor behavior. See [UnityPlayerInputGateAdapter](../../../Runtime/UnityInput/UnityPlayerInputGateAdapter.cs) and [PlayerActorRuntimeHost](../../../Runtime/PlayerParticipation/Authoring/PlayerActorRuntimeHost.cs).
- Player occurrence reference resolution asks the current physical source before creating evidence; the current Player identity path must be reconciled with the proposed candidate-time `ActorOccurrenceId`. See [ActorOccurrenceReferenceRuntime](../../../Runtime/Actors/References/ActorOccurrenceReferenceRuntime.cs), [IF-ADR-049](../ADRs/IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md) and [Actor Occurrence References guide](../../Guides/Actor-Occurrence-References.md).
- `RuntimeScopeCancellationToken` is a passive transition token. `RuntimeContentRuntime` is internal and delegates physical release to adapters; logical release is not proof of physical destruction. See [RuntimeScopeCancellationToken](../../../Runtime/RuntimeContent/RuntimeScopeCancellationToken.cs), [RuntimeContentRuntime](../../../Runtime/RuntimeContent/RuntimeContentRuntime.cs) and [IRuntimeReleaseAdapter](../../../Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs).

Unity may run `Awake` and `OnEnable` when an inactive hierarchy becomes active; `Start` is scheduled before the enabled component's first `Update`. Unity does not establish arbitrary cross-GameObject callback ordering. `Object.Destroy` is deferred until after the current Update loop and before rendering. These engine behaviors constrain guarantees; they do not implement the proposed protocol. See the Unity 6.5 references for [Instantiate](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Object.Instantiate.html), [SetActive](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/GameObject.SetActive.html), [Awake](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/MonoBehaviour.Awake.html), [OnEnable](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/MonoBehaviour.OnEnable.html), [Start](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/MonoBehaviour.Start.html) and [Object.Destroy](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Object.Destroy.html).

## 2. Proposed vocabulary and authority

These definitions are proposed for review. They do not retroactively rename current APIs.

| Concept | Proposed meaning | Authority / limitation |
|---|---|---|
| Actor Definition | Authored description or reusable source from which an Actor may be selected or produced. | Consumer authoring; not a live Actor or owner. No additional asset type is decided here. |
| `ActorProfile` | Current reusable authored Actor option/profile. | Existing surface remains Experimental; it does not identify a live occurrence. |
| `ActorId` | Typed Actor identity in its declared identity domain. | It is not sufficient to identify one physical lifetime. Current `PlayerActorDeclaration.ActorId` has occurrence-like use; the proposed semantic separation requires migration and reconciliation. |
| `ActorOccurrenceId` | Identity of one accepted physical Actor incarnation. | Core assigns it when it accepts a producer's candidate; it is not reused after abort. Its namespace/host scope remains governed by the occurrence identity contract. |
| Candidate | Accepted physical candidate plus its exact owner and operation context, before occurrence commit. | Core assigns candidate identity and validates transitions; producer owns physical effects. Candidate identity does not mean Current or public. |
| `OperationId` | Identity of one preparation, replacement, contribution update or cleanup attempt. | Operation owner; used to reject delayed or duplicate results. It is not Actor identity. |
| Owner | Authority responsible for a physical occurrence or resource lifetime. | Session, Scene/Route/Activity, NonPlayer producer or specialized adapter according to its contract. Owner cannot be inferred from hierarchy alone. |
| Current binding | Core's committed mapping from lifecycle domain/Slot to exact Actor identity, occurrence, owner and physical root evidence. | Core exclusively commits, publishes and invalidates it. It is not readiness. |
| Runtime configuration | Resolved contribution/configuration applied to one occurrence. | Consumer and adapters author/resolve it; changing it does not change `ActorOccurrenceId`. |

The Core owns logical occurrence lifecycle, current binding, validation, commit and publication. Producers own physical creation/adoption, activation, quiesce and root release. Composition coordinates contributions and operations. Adapters own concrete resources. Consumers own recipes, policies and domain rules. No two layers are final authority for the same commit.

### 2.1 Occurrence states and orthogonal evidence

Proposed progression:

```text
Candidate → Prepared → Activating → Committed/Current → Retiring → Released
    └─────────────── pre-commit cancellation/failure ──→ Aborted
```

`CleanupPending` and `CleanupFailed` are cleanup outcomes that may outlive logical retirement; they do not make an occurrence current. No public enum is proposed. Physical active state, public resolution, operational authorization and gameplay readiness are separate evidence dimensions. A producer may adopt an already-active object, but that path cannot claim the inactive-preparation guarantee.

## 3. P01 — Physical activation and operational authorization

### 3.1 Options considered

| Option | Assessment |
|---|---|
| A. Inactive preparation, activate before commit, explicit operation gate | Recommended foundation. It permits physical validation without publishing or authorizing the candidate. It requires cooperative components and specialized gates. |
| B. Commit before physical activation | Rejected as the general replacement protocol: it makes B logically current while inactive and requires a new compensating commit if activation fails. It is viable only where temporary unavailability is an explicit product contract. |
| C. Phased component activation | Useful inside adapters that control their phases. Not a universal barrier: a disabled `MonoBehaviour` can still receive `Awake` when its active GameObject becomes active, and custom callbacks may perform effects. |
| D. Occurrence-scoped operational capability | Recommended with A. Core issues/revokes logical authority; specialized adapters enforce it at operation boundaries. It is not a universal Unity callback interceptor. |

### 3.2 Proposed guarantee

Before commit, Core does not expose B as Current and does not issue a valid operational capability for B. Components claiming the stronger guarantee must keep `Awake`/`OnEnable` limited to local, reversible initialization; all externally visible gameplay operations must validate the exact occurrence capability. Subscriptions, services, input and physics need owner-aware adapters or candidate-scoped reversible registrations. A precommit candidate may be physically active while its authorized operation set remains empty.

Core cannot prevent arbitrary user code from performing effects in `Awake`, `OnEnable`, `Start`, direct event/service calls or un-gated physics. In particular, disabling a behaviour does not promise to suppress `Awake`. The contract applies only to participants that cooperate; authoring validation may reject or downgrade unsupported candidates. Which components require mandatory adapters remains open in Section 10.

## 4. P02 — Conditional A→B occurrence replacement

### 4.1 Proposed transaction

1. Capture A's exact committed binding and current selection revision. Store B's desired selection separately; do not mutate the committed selection.
2. Core accepts B's physical candidate, allocates a non-reused `ActorOccurrenceId`, and creates an operation generation. B remains unresolvable.
3. Producer stages B; Composition prepares required contributions and dependencies against B's candidate context. All B operation gates remain closed.
4. Producer activates B. Callbacks may initialize candidate-local dependencies, but cannot acquire operational authorization through Core.
5. Immediately before handoff, Core revalidates owner, A binding, B candidate, operation generation and desired selection revision. Producer/Player adapters suspend A's operational entry. B remains closed.
6. Core conditionally commits one coherent binding: committed selection/revision, `ActorId`, `ActorOccurrenceId`, exact owner, physical root evidence and authorization epoch. Only after the swap do observers see replacement/publication.
7. Open B's specialized operation gates and evaluate gameplay readiness. Retire/release A through A's owner.

Before step 6, a failed candidate leaves A Current. If suspending A or recovering its operation gate fails, return a distinct partial-handoff/blocked outcome: A remains logically Current, B is not committed, and A may be operationally blocked. Do not say “A restored” unless its gate confirms restoration. After step 6, B remains Current if readiness projection, gate opening or A cleanup fails; report the corresponding condition without rollback fiction.

Reentrant mutations during activation are rejected or deferred for the exact Slot/owner transition. The final commit compares expected IDs/revisions/generation. Any late completion that fails the comparison is stale, cannot publish or commit, and must release only resources it owns.

### 4.2 Invariants

- One Core-committed Current binding per lifecycle slot.
- Desired selection changes do not invalidate committed A.
- Only a conditional Core commit changes Current; observers do not see intermediate binding state.
- Candidate IDs remain private until commit/publication policy allows resolution.
- At most one valid operation capability is accepted for the slot when all relevant operations validate it; no physical no-overlap promise exists for non-cooperative components.
- Pre-commit failure preserves A's logical Current; gate restoration is a separate result.
- Post-commit failure preserves B as Current and records degraded readiness/cleanup.
- Content-only contribution replacement preserves `ActorOccurrenceId`; replacement of the physical Actor creates a new occurrence.

## 5. P03 — Closure, cancellation and cleanup

### 5.1 Proposed sequence

1. Core closes the occurrence logically: advances/invalidate its operation generation, revokes authorization, removes public Current/resolution and rejects future commits.
2. Composition stops accepting operations for that exact occurrence and requests cancellation. Each adapter validates the operation/owner before and after side effects; cancellation remains cooperative.
3. Composition asks dependent adapters to quiesce/disassociate in dependency order. Idempotent disassociation is required where retries are supported.
4. Each resource owner performs its own release and reports requested, confirmed, pending or failed with an operation/ticket correlation.
5. Producer releases its physical root only under its own physical ownership policy. A failed resource adapter does not prevent attempts on independent adapters.

Logical closure must not wait indefinitely. Resources whose safe release requires the Actor root may have a bounded quiesce/disassociation window; after the selected policy expires, the owner records an explicit unconfirmed/pending outcome and proceeds according to its own teardown authority. Forced unload/destruction is best-effort; it cannot guarantee callback delivery or external-resource cleanup.

### 5.2 Proposed lifecycle evidence

```text
CancelRequested → OperationInvalidated → Disassociated
                → ReleaseRequested → ReleaseConfirmed
                                   ├→ CleanupPending
                                   └→ CleanupFailed
```

These are conceptual facts, not final enum members. `ReleaseRequested` does not mean `ReleaseConfirmed`. Unity `Destroy` scheduling does not equal an observed destroyed object. Confirmation is adapter-specific; a native Unity object can be observed later, while a service/network/resource adapter needs its own acknowledgment. A retained cleanup receipt belongs to a lifecycle/producer owner that outlives the Actor root; the exact registry/ledger surface remains open.

### 5.3 RuntimeContent relationship

Reuse conceptually: explicit owner/scope, transition generation, exact resource handle, cancellation validation before/after effects, release outcome and separate physical adapters. Do not depend on internal `RuntimeContentRuntime`, treat the passive `RuntimeScopeCancellationToken` as active async cancellation, claim that logical release destroys a Unity object, or use RuntimeContent to seize Actor ownership. IF-ADR-001/008/014/020 and the RuntimeContent adapter contracts remain in their own scope.

## 6. P04 — Proposed public Core contracts

No C# signatures or final public type names are defined here. Any future API starts Experimental and is governed per consumer contract by IF-GOV-001.

| Contract | Responsibility and authority | Conceptual input/result and state | Ownership, guarantees, failure and dependency |
|---|---|---|---|
| Host-scoped participant registration | Core defines a public participation seam; a provider implemented by Composition participates only in the explicit producer/host context. | Exact host/provider context; discovered/registered, absent, duplicate or invalid. | Provider implementation belongs to Composition. No global locator, global scan, reflection lookup or concrete package reference. Required absence fails before activation; optional absence leaves Core behavior available. |
| Producer/materializer | Core defines the physical-candidate protocol; producer implements physical operations. | Create/adopt, stage, activate, report evidence, quiesce and release; returns staged/active/rejected/release progress. | Producer retains root ownership. Core cannot infer physical success or force rollback; missing evidence rejects commit. |
| Candidate context | Core creates immutable occurrence/operation/owner context and validates it. | Typed Actor/occurrence/owner, expected Current, generation, source/provenance and candidate evidence; returns valid/stale/closed. | Core owns identity and logical validity. ID allocation is not commit; stale/closed context cannot publish or commit. Root data is exposed only as required by a public Unity adapter boundary. |
| Preparation participant | Core invokes a participant to prepare required contributions, report result/diagnostics, validate commit readiness and receive cancellation/retirement. | Candidate context, declared requirements and operation identity; returns prepared/rejected/cancelled plus opaque evidence. | Composition owns contribution coordination; adapters own resources. It may veto its own readiness but cannot commit Actor Current or transfer producer ownership. |
| Conditional Actor commit | Core validates and atomically replaces only the expected Current binding. | Expected prior binding/revision, candidate, owner and operation generation; returns committed or stale/conflict/failure. | Core is sole authority. On success it updates one coherent binding before observers run; on failure no partial selection/occurrence update is allowed. |
| Occurrence-scoped operational authorization | Core issues and revokes a capability/epoch bound to exact occurrence; specialized adapters validate it. | Exact occurrence + operation capability; returns valid/revoked/stale. | Core controls logical authorization; adapter controls concrete input/physics/effect gate. Arbitrary code that bypasses the capability is outside guarantee. |
| Lifecycle observer/invalidation | Core sends read-only Current committed/replaced/retiring/invalidated facts. | Exact occurrence/owner/generation and lifecycle reason; queryable snapshot plus notifications. | Observer cannot initiate lifecycle or depend on intermediate state. Callback failures are diagnostics and do not roll back authority. |
| Cleanup coordination and outcomes | Core exposes closure correlation; Composition coordinates contributions; producer/adapters report owner-specific release. | Operation/ticket, exact resource/occurrence, dependency result and status. | No layer releases another owner's resource. Failure/pending survives in a longer-lived owner receipt; lifecycle closure does not wait forever. |

### 6.1 Explicit participant discovery

Recommended seam: Core-owned host configuration refers to one or more Unity components through a Core-compatible base (`MonoBehaviour`/Unity object) or a bounded host-local discovery root, then checks the public Core participant interface. Validate cardinality and declared capability before activation. Do not add a `com.immersive.actor-composition` assembly reference to Core. Composition implements the interface and is installed/configured by the consumer. Discovery is limited to the exact producer/occurrence context and follows the bounded-root precedent in IF-ADR-040, not global scene search.

Core must know when a composition is required even if the package/provider is absent. Recommended direction is a Core-owned required/optional policy or equivalent typed requirement in the producer context. A requirement stored only in an asset/script owned by the absent package cannot be reliably inferred by Core. Exact authoring UX and serialized representation remain open.

## 7. Proposed `com.immersive.actor-composition` architecture

### 7.1 Boundary and dependency

```text
Consumer
  └── com.immersive.actor-composition
        └── com.immersive.framework

Core publishes lifecycle/producer/participant contracts;
Core does not reference Composition implementation.
```

Composition is an independently versioned Framework extension, not a generic low-level technical package. It contains reusable contribution coordination and depends on Core's public Actor lifecycle contract. Do not create the package or its UPM/assembly structure in this documentation stage. Core remains useful without it when no composition requirement is declared.

### 7.2 Composition and contribution model

An effective composition is a set of contribution records associated with one exact `ActorOccurrenceId` and owner. Composition is not ownership of the Actor root or all attached resources.

Each contribution conceptually carries a typed contribution identity, exact occurrence/owner association, responsible adapter, resolved configuration, local revision, explicit dependencies, operation state and detach/release policy. It may not migrate silently to another occurrence. A content-only update keeps the Actor occurrence ID; an Actor replacement creates a new one.

Independent contributions can advance independently. A grouped operation contains only the affected dependency closure. Dependency cycles and invalid/missing required dependencies are rejected before effects. Do not hold global locks across adapter waits. Stage from a captured local revision; revalidate operation, occurrence and expected revision immediately before commit. An obsolete result can neither overwrite a newer contribution nor commit to a replacement Actor.

Distinguish two commits:

1. **Actor occurrence commit:** exclusively Core-owned; establishes which occurrence is Current/public/authorized.
2. **Composition contribution commit:** Composition-owned; advances contribution revisions only for the exact still-valid occurrence. Core can validate the occurrence/owner boundary but does not own contribution state.

Initial candidate contributions are prepared and validated before Actor commit. Their physical effects remain gated. Composition receives the committed occurrence fact only after Core's binding swap. If post-commit application/gate opening fails, the Actor remains Current and Composition reports the contribution/readiness failure. For later content-only updates, Composition conditionally commits its own revision against the exact current occurrence; the occurrence ID remains unchanged.

### 7.3 Operations and compensation

| Operation | Proposed behavior |
|---|---|
| Initial preparation | Resolve required contribution set and dependency order; stage reversible work; report ready/rejected against candidate context. |
| Apply/update | Prepare only the selected contribution or affected dependency closure; validate captured revisions and exact occurrence before contribution commit. |
| Grouped substitution | Stage the dependency group under one operation; preserve previous committed group until the conditional contribution commit succeeds. |
| Actor replacement | Prepare Composition against B while A remains Current; Core owns Actor commit; association of A/B is not transferred by Composition. |
| Cancellation | Invalidate operation generation; signal adapters; reject any late result; compensate only resources the operation acquired. |
| Disassociation | Stop contribution use of the exact occurrence before root teardown where required; idempotent for retryable owners. |
| Release | Ask each owning adapter for release; aggregate successes and failures; retain pending receipt with a longer-lived owner. |
| Compensation | Undo reversible staging through the same responsible adapter. Irreversible callback/effect compensation is domain-specific and never promised generically. |

The package must not create a global arbitrary-resource manager, a second Actor Manager, an implicit Actor Scope, or a Core-internal dependency. Reusable authoring intent, concrete host composition, Apply/Rebuild UX and Advanced/Debug diagnostics need a product-surface review before final package API; they are not defined by the runtime contracts here. No resource categories become mandatory Framework concepts.

### 7.4 RuntimeContent relationship

RuntimeContent remains owner-aware materialization/binding infrastructure under its existing scope. Composition may use public Core concepts where the contract fits, but must not depend on internals or duplicate ownership. The current cancellation token is passive, current runtime is internal, and physical release remains an adapter responsibility. Composition adds contribution coordination that RuntimeContent does not claim; it does not replace RuntimeContent.

## 8. Publication, readiness and authority ordering

```text
physical candidate accepted → occurrence ID allocated
            │
            ├─ not Current / not publicly resolvable / no operational capability
            ▼
prepared → activated with gates closed → owner and candidate revalidated
            ▼
Core commits current binding → publication/observer notification
            ▼
specialized operation gates open → gameplay readiness evaluated separately
```

Public reference resolution follows authoritative commit/publication, never candidate identity alone. This is intended to clarify IF-ADR-049's “successful physical preparation/adoption and relevant authoritative commit” language, not to make all prepared or active objects resolvable. If reviewers interpret an existing published Actor surface differently, reconcile IF-ADR-049 before acceptance or implementation.

## 9. Failure and recovery matrix

| Case | Preconditions and sequence | Authority, expected state and commit | Compensation, evidence and limitation |
|---|---|---|---|
| **F01 — Preparation failure** | A is Current (if present); B candidate and one or more contributions are staged; a required contribution rejects. | Core keeps A Current; B never commits/publishes. | Invalidate B operation; each owner compensates its staged work; aggregate per-adapter results. A remains operational because final handoff has not started. Irreversible external effects cannot be generically undone. |
| **F02 — Physical activation failure** | Prepared B is activated with authorization closed; producer reports failure or incomplete physical evidence. | Core rejects B commit; A remains Current. | Quiesce/release B through producer; record callback/adapter uncertainty. Core cannot prove that arbitrary Unity callbacks had no side effects and cannot guarantee callback rollback. |
| **F03 — A→B handoff failure** | B prepared/active but gated; Core revalidates A/B; A's operational entry is suspended before final commit. | If suspension or conditional commit fails, A remains logical Current; B is not Current. | Attempt to restore A through its adapters. Return an explicit partial-handoff/operationally-blocked outcome if restoration is unconfirmed. Keep B closed and compensate it. Never claim A is operational solely because it remains Current. |
| **F04 — Failure after B commit** | Core committed B; opening B's specialized gate, gameplay projection or later initialization fails. | B remains Current and published; readiness/operation status reports unavailable or blocked. A is retired. | Diagnose failure and apply explicit retry/repair policy. Do not reconstruct A or call it rollback. Release A independently and retain any cleanup receipt. |
| **F05 — Cleanup pending/failure** | An occurrence is logically closed; cancellation, disassociation or adapter release is incomplete. | Core keeps it non-current and unauthorized. Producer/adapters own release confirmation. | Preserve per-operation/resource result with a longer-lived owner; continue independent releases. `Destroy` scheduled is not physical destruction observed. Pending/terminal-failure vocabulary and retries are explicit. |
| **F06 — Owner closes during operation** | Preparation/update is in flight while the exact owner closes or root disappears. | Core invalidates owner/generation; a late result cannot commit or publish. | Cooperative adapters cancel; non-cancellable external work must validate before effects/commit and report late completion for compensation. Forced owner loss may leave cleanup unconfirmed. |
| **F07 — Non-cooperative components** | Active callbacks or direct service/physics effects bypass the occurrence capability. | Core can reject only operations that validate its authority; no universal guarantee that the candidate remained inert. | Authoring validation, adapter encapsulation or a reduced-guarantee adoption path is required. Do not report strong isolation for unverified components. |
| **F08 — Active Scene-Provided adoption** | Scene Actor has already been active before Core accepts/adopts it. | Producer/Session may adopt after current contract validation; commit/publication is still explicit. | Close only gates the Framework or producer controls. Prior `Awake`/`OnEnable` effects cannot be undone; report late-adoption guarantee class and provenance. |
| **F09 — Required Composition absent** | Core-owned authoring/policy declares composition mandatory, but no valid participant is configured/discovered in the exact host. | Fail before physical activation; no candidate Current or public reference. | Emit missing/duplicate/incompatible participant diagnostics. If requirement signal exists only inside the absent package, Core cannot infer it; that authoring model is rejected. |
| **F10 — Concurrent or stale work** | Replacement, contribution update or close races with a delayed prepare result/revision. | Core validates exact operation generation, occurrence, owner and expected Current at commit; Composition validates its local revision. | Stale result is rejected, cannot mutate another Actor/contribution, and compensates only resources it owns. No locks are held across asynchronous waits; cancellation remains cooperative. |

## 10. Decision register

These questions remain open for architectural review. The recommendation is recorded so they do not block the whole conceptual model; items marked API gate must be settled before public contract stabilization.

| Open decision | Recommended alternative and consequence | Priority / planning effect |
|---|---|---|
| How Core declares Composition required | Core-owned typed required/optional policy or requirement metadata in the producer/authoring context. This lets Core fail before activation even when the package is absent; it adds a Core authoring concept whose Inspector language needs review. | **High — API/authoring gate.** Does not block lifecycle modeling; blocks finalized provider/requirement contract. |
| Required operational-gate coverage | Strong guarantee only for cooperative components and audited adapters; validate required capabilities/adapters at authoring. Non-cooperative Actors use late adoption/reduced guarantees. | **High — guarantee gate.** Blocks claiming isolation; does not block the logical commit design. |
| Partial handoff result | Preserve A as logical Current, B uncommitted/gated, report A gate state as restored/blocked/unconfirmed; retain both owner receipts if needed. | **High — contract gate.** Must be settled before replacement result API; planning may proceed with this state model. |
| Where cleanup receipts survive | Producer/lifecycle owner that outlives the Actor root retains owner-specific receipts; avoid a global registry. Composition exposes observation while the longer-lived owner owns retry. | **Medium — ownership/API gate.** Does not block initial lifecycle planning; blocks reliable retry/diagnostics API. |
| Definition of release confirmed | Adapter-specific receipt. Unity destruction observation, service release acknowledgment and shared-resource lease completion are distinct; there is no universal “destroyed” result. | **High — API gate.** Must be explicit before exposing a common cleanup result. |
| Timeout and recovery | Bound only the quiesce/disassociation prerequisite that requires a live root; never wait indefinitely. On expiry close logical authority, continue independent cleanup and retain unconfirmed result under producer policy. | **Medium — policy gate.** Does not block logical closure; exact durations/escalation can be owner policy. |
| Guarantee for already-active adoption | Report late adoption explicitly; do not promise pre-activation isolation. Permit strong guarantee only when producer can prove gates before effects. | **High — scope gate.** Does not block Manager-Provisioned path; must be settled before claiming parity across producer types. |
| `ActorId` / `ActorOccurrenceId` migration | Preserve ActorId's declared identity domain and introduce occurrence ID for physical incarnation; migrate current Player declaration usage rather than aliasing two domains. Reconcile serialization and exact-reference behavior under IF-ADR-003/049. | **High — identity gate.** Blocks implementation/API stabilization for Player reference/replacement; does not block package dependency direction. |
| Composition authoring/product surface | Before package release, design reusable contribution intent, concrete host authoring, idempotent Apply/Rebuild and Advanced/Debug diagnostics with user-facing Inspector terms. Keep runtime contribution interfaces as an internal/advanced layer. | **Medium — product gate.** Does not block core contracts; blocks a usable consumer package surface. |

## 11. Existing decision impact and reconciliation

| Existing authority | Current scope to preserve | Proposed relationship and required follow-up |
|---|---|---|
| IF-ADR-001 | Scoped lifecycle owners, explicit teardown order and retryable owner obligations. | Actor cleanup must fit its exact-owner model; a timeout must not silently destroy another owner's resource or erase its receipt. |
| IF-ADR-003 | Player participation, Session selection, preparation and lifecycle authority. | Remains Player authority. Reconcile Actor identity distinctions and selection/current binding semantics; do not move Session membership to Composition. |
| IF-ADR-014 | Authored identity and owner-scoped binding semantics. | Preserve typed identity domains; do not use a content/declaration identity as an occurrence ID. |
| IF-ADR-020 | Session Player Leave and physical resource release authority. | Actor occurrence close integrates with Session leave; it does not transfer Player physical ownership to Composition. |
| IF-ADR-024 | Accepted Manager-Provisioned prepared Actor replacement and its existing pre/post-commit results. | Material handoff timing differs: current contract releases A gameplay before B physical replacement; proposal keeps A operational through B preparation and adds an explicit partial-handoff result. Requires accepted amendment/reconciliation before implementation. Until then ADR-024 remains normative. |
| IF-ADR-038 | Actor occurrence root is physical/spatial authority; Player/Camera assignment semantics stay separate. | Preserve root, pose, Session membership and Camera Subject boundaries; update only after new lifecycle contract is accepted. |
| IF-ADR-040 | Bounded composition-root discovery, explicit participant ownership, aggregate failures and continued release of remaining participants. | Reuse as a discovery/cleanup precedent within matching lifetime; do not imply its Scene lifecycle owns Player Actors or generic resources. |
| IF-ADR-049 | Experimental public reference resolves an exact owner/occurrence; publication follows authoritative preparation/adoption and commit. | Clarify that candidate ID is not resolvable before Core commit. Reconcile ActorId vs ActorOccurrenceId and visibility language before changing implementation. |
| IF-GOV-001 / Public-API | Per-contract maturity; Stable guarantees cannot be weakened silently. | All proposed new contracts remain non-normative and unimplemented. No Public-API row or Stable promotion is made by this proposal. |

### 11.1 Core/package dependency cycle check

There is no structural cycle in the proposed direction:

```text
Consumer → com.immersive.actor-composition → com.immersive.framework
```

Core publishes provider/producer/commit contracts; Composition implements them. Core discovers implementations only at an explicit host boundary through Core-owned interfaces/configuration. The requirement signal also lives in Core so missing package detection does not depend on a missing package type. No contract allows Composition to make Actor Current, and no Core contract takes a Composition concrete type. This keeps Actor commit authority singular.

## 12. Review readiness

This proposal is ready for architectural review, not implementation. Approval should confirm the identity migration, partial handoff result, mandatory-provider signal and cleanup receipt authority; it should also reconcile IF-ADR-024 and IF-ADR-049 without editing them retroactively. After approval, a separate plan may define code and consumer migrations. No runtime change, final C# signature, new package, Stable promotion, Unity run or test result is claimed here.
