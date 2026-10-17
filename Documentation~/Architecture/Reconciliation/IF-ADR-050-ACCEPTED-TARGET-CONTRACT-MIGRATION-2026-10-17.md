# IF-ADR-050 — Accepted Target and Contract Migration Boundary — 2026-10-17

Status: **Accepted architecture target / migration contract — implementation and validation pending**  
Accepted: **2026-10-17**  
Scope: **future Actor lifecycle and composition contracts; not an implementation, release or maturity approval**  
Decision authority: [IF-ADR-050](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md)  
Design companion: [IF-SPEC-050](../Specifications/IF-SPEC-050-Actor-Lifecycle-Participation-and-Composition.md)  
Prior conflict crosswalk: [IF-ADR-050 reconciliation — 2026-10-17](IF-ADR-050-ACTOR-LIFECYCLE-COMPOSITION-RECONCILIATION-2026-10-17.md)  
Governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

## 1. Scope and effective authority

The architecture target in IF-ADR-050 is **Accepted** for future design and implementation planning. This document settles the *intended changes* to conflicting Actor/Player/Camera contracts, preserving accepted and certified behavior in current published runtime until the corresponding migration is implemented and validated.

**Do not collapse these statuses:** `Accepted architecture` ≠ `implemented` ≠ `tested` ≠ `Stable public API` ≠ `published UPM`. IF-ADR-003/024/038/049 and related Accepted decisions remain authorities for their **current implementation and consumer guarantees**. The newly accepted target determines the direction of *subsequent changes*, without claiming existing consumers already satisfy those contracts. In case of overlap, label statements **current runtime** versus **future target** rather than silently overriding past records.

Historical [IF-ADR-023A identity reconciliation](IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md) documents an accepted historical `ActorId`-as-prepared-occurrence boundary. Its historical evidence is not retroactively rewritten. The target below explicitly replaces that interpretation **when the future identity migration is implemented**; it does not turn existing IDs into stable logical identities today.

The new `com.immersive.composition` UPM **does not exist yet** and is not a current dependency of Framework.

## 2. Contract migration matrix

| ID | Current authority or behavior | Accepted **future** contract | Migration and compatibility requirement |
|---|---|---|---|
| **M01 — Player participation** | IF-ADR-003/020/025 own exact Join/Leave, Slot, Session, Host and PlayerInput; `PlayerOccurrenceId` is an exact Joined lifetime. | Preserve PlayerOccurrenceId across logical Actor changes, root replacement and recomposition; no implicit Leave/Join or Activity-occurrence recreation. | Retain Session/Slot/PlayerInput authority and existing Stable Join/data guarantees. No new global Actor manager. |
| **M02 — logical Actor identity** | IF-ADR-023A and current Player materialization establish typed `ActorId` in preparation, currently per physical materialization. | `ActorId` denotes **logical Actor**. It may survive physical root replacement of the same Actor and may change while the Player remains Joined. It is **not** PlayerOccurrenceId or ActorProfileId. | Trace `PlayerActorDeclaration`, `ActorId` creation, preparation tokens, selectors, consumers and serialization. Define producer issuance/uniqueness; avoid assigning one logical ActorId to simultaneous *publicly committed* incompatible owners. Do not derive from PlayerOccurrenceId universally. |
| **M03 — physical incarnation** | IF-ADR-049 Player resolver lazily creates/removes `ActorOccurrenceId` on physical predicate changes. | Authoritative lifecycle allocates ActorOccurrenceId **once per successful physical occurrence commit**. It is stable through temporary inactivation and keyed content recomposition; it changes on physically replaced/reused root. | Move ID lifetime to producer/Core occurrence record. Cancelled candidates use operation identity, not a public occurrence. Test stale exact-reference semantics and owner-specific uniqueness/namespace. |
| **M04 — resolver and availability** | IF-ADR-049 public success denotes current valid physical occurrence; invalid/unavailable/stale are distinct. | Distinguish **last committed logical binding** from **currently available physical Actor**. During the handoff, A remains the committed identity while physically inactive; B is not yet publicly resolved. | Preserve physical resolver success semantics until a deliberate public amendment. Expose a typed committed-binding/availability view if needed rather than reinterpreting physical `TryResolve` success. Invalidation stays owner-driven, never hierarchy-scan driven. |
| **M05 — prepared Actor replacement** | IF-ADR-024 Manager-Provisioned V1 releases A gameplay/occupancy before replacing physical root, includes restoration on recoverable precommit failure; certified on its historical scope. | Stage B while A remains current and operating; validate required B content/spatial constraints; guard synchronous A deactivate → B activate → revalidate → coherent binding/selection commit → publication; project gameplay/Camera and release retired A. | Separate `selection desired` from `selection committed`; don't publish during staging. Define the barrier/reentrancy domain, exact expected-current checks, owner-close handling and occupancy/input timing. Preserve public V1 compatibility guarantees unless an explicit Experimental/Stable maturity review approves the breaking migration. |
| **M06 — partial handoff** | IF-ADR-024 V1 has typed failed/rolled-back/degraded/cleanup-pending outcomes and current restoration conditions. | Before deactivating A, rejection leaves A operational. After A is physically disabled, any failure to commit B can leave **A committed but unavailable**; report a partial/degraded handoff without fictitious rollback. Once B commits, later readiness/cleanup failures **keep B current**. | Define typed result/observation for last committed identity, availability, B candidacy, cleanup and recovery. Do not advertise `SetActive` as proof of user callback success. Preserve previous V1 result interpretation for already published artifacts. |
| **M07 — specialization gates** | IF-ADR-024/025/036 govern PlayerInput, gameplay admission and readiness. | Keep specialized input/gameplay/pause/transition authority; do not assume a universal cooperative Actor gate or frame-atomic callback isolation. B must not obtain current gameplay authority while merely a staged candidate. | Record exactly when A's old admission/occupancy closes and B's may open; distinguish structural `GameplayReady` from runtime gameplay availability. Forced Session/owner close must invalidate the transaction even within guarded handoff. |
| **M08 — Profile visual migration** | IF-ADR-003/038 permit optional `ActorProfile.presentationPrefab` and `VisualContentMount`; Manager-Provisioned materializer directly creates/destroys visual; Scene-Provided adopts authored visual. | One content path: profile Base entry at key `visual` and existing mount; subsequent part changes use the *same* Composer. Content-only recomposition keeps root and ActorOccurrenceId unchanged. | Provide explicit, idempotent authored/serialized data migration, including null/no-visual and Scene-Provided cases. Do not leave a second implicit visual runtime provider or silently change Stable serialized data intent (IF-GOV-001). |
| **M09 — composition and resources** | Current Framework has no generic keyed Composer. RuntimeContent owns its existing handles and scopes. | `framework → com.immersive.composition`, a small generic kernel with keyed parts, materializers, owner-scoped contributions, precedence, per-dependency-closure commit, revision and delta. | Kernel must not depend on Framework or know Actor/Session/Route/Activity types; Framework maps owners/cancellation/ActorProfile to kernel. Composer neither owns root nor physical resource acquisition. Preserve old RuntimeContent authority, no duplicate manager. |
| **M10 — Camera / Subject** | IF-ADR-038 keeps Camera assignment distinct from Actor root/visual; IF-ADR-049 publishes current Actor evidence. | Player/Camera assignment occurrence remains stable across root replacement; a new physical ActorOccurrenceId may require Subject reprojection. Content delta leaves root stable; observing Camera targets should be outside replaceable parts by default. | Order Camera/observer notifications after coherent Actor commit. For targets inside changed parts, either reject authoring or define explicit delta rebinding; never let Composition own Camera assignment. |
| **M11 — externally produced / adopted Actors** | IF-ADR-003/020 and IF-ADR-049 preserve Scene-Provided and NonPlayer external producer authority. | Producer continues to own root. Scene/externally owned parts may be **adopted**, not recreated or destroyed by Composer. Late adoption does not undo Unity callbacks already executed. | Explicit provenance and release policy for adopted part; no Scene-Provided replacement guarantee silently borrowed from Manager-Provisioned V1. NonPlayer publication remains exact-owner scoped with no PlayerSlotId requirement. |
| **M12 — release and diagnostics** | IF-ADR-020 handles terminal Leave; Unity `Destroy` can be deferred. | Close exact occurrence and cancel stale work, detach parts by declared dependencies, ask acquiring adapter to release its own resource, and release root by producer. Distinguish request, contract completion, pending and failure where relevant. | Never reparent active visual into active hierarchy during release. Retain actual pending cleanup under a longer-lived owner only if that adapter requires retry/confirmation. Do not claim requested destruction is physically confirmed. |

## 3. Explicit policy for profile change vs physical replacement

**Two different operations remain distinct:**

- **Content recomposition:** changes one or more authored/runtime content parts on the same Actor root. It MUST preserve PlayerOccurrenceId, ActorId, ActorOccurrenceId, gameplay-owned root state and the root's physical ownership. It advances only the composition revision.
- **Physical Actor replacement:** creates/adopts a distinct physical root with a new ActorOccurrenceId. It may preserve ActorId when replacing the *same logical Actor*, or establish another ActorId when the controlled Actor itself changes, without changing PlayerOccurrenceId.

A change to a selected `ActorProfile` must **not** implicitly trigger either operation via the existing pre-preparation `RequestReplaceActorSelection` command. The new authoring/command contract must explicitly designate content-only change vs physical replacement (or an unambiguous compatible authoring policy), and reject unsupported or ambiguous cases. Initial implementation may limit the available modes; do not infer a new root from visual replacement alone.

**Authoring compatibility gate:** When updating serialized `ActorProfile` or any Stable `GameApplicationAsset` selection/default data, declare exactly which serialized fields are preserved, translated, or deprecated. Provide an explicit idempotent migration; fail clearly rather than silently altering stored intent. Do not invent the final C# signatures or YAML migration algorithm in this architecture document.

## 4. Composition dependency and concurrency contract

One Composer controls **content bindings per root**, and only the Core/producer lifecycle controls physical root and occurrence Current. A content commit cannot change the Actor binding as a side effect.

A contribution contains an owner identity/handle, precedence and desired content by key. Equal-priority competing owners for a key are explicitly rejected. Declared part dependencies define each affected closure; unrelated slow/failing preparation MUST NOT block independent keys. A monotonic public revision changes after successful content commit; exact target/operation/generation checks reject stale asynchronous preparation, even when the public revision advanced for an unrelated key.

Initial content is staged against an inactive candidate root where feasible. Scene-Provided content may already be active and must be handled with **reduced guarantees**. Part materializers must state their concrete resource ownership, acquired-versus-adopted provenance, staging activation rules and release semantics. Pooling `Rent` is **not assumed inert**; no pooling/Addressables support is claimed until provider contracts and tests exist.

## 5. Transition and observable failure contract

| Moment | Logical committed identity | Physical availability | Mutations / notification |
|---|---|---|---|
| B preparing | A | A normally active; B private | A remains available; stale async B results discarded |
| B staged, validated | A | A active, B inactive | All required detectable validation before physical handoff |
| Guard begins | A | A active | Conflicting same-domain mutations rejected/recorded; owner-close invalidates expected commit |
| OnDisable(A) | A | A may be inactive | Current logical A retained, physical resolver may report unavailable |
| Awake/OnEnable(B) | A | B candidate active; A inactive | B not yet published; specialized gameplay/input gates closed for B |
| Successful handoff commit | B | B active structurally | One coherent Actor/selection commit; ActorOccurrenceId allocated |
| Notifications | B | Readiness/availability separately assessed | Camera and listeners observe committed state |
| Failure after A deactivation, before B commit | A | A may remain unavailable | Typed partial/degraded failure and explicit recovery, not claimed rollback |
| Failure after B commit | B | May be degraded | B remains Current; diagnostics and old-root cleanup remain separately observable |

No protocol here guarantees the order of arbitrary MonoBehaviour side effects or that `SetActive` reports arbitrary callback exceptions. A bounded proof with canonical logging may document Unity's actual behavior in the target version. The conservative contract does not depend on such exceptions being propagated.

## 6. Status and evidence gates

| Layer | State after this decision | Requirement before claiming it done |
|---|---|---|
| IF-ADR-050 architecture | **Accepted target** | No Unity proof required merely for normative architecture acceptance |
| IF-SPEC-050 | **Accepted architectural companion at conceptual level** | Typed signatures, serialized design and public API maturity remain future design decisions |
| Old IF-ADR-003/023A/024/038/049 contracts | **Current, historically authoritative at implemented/published boundary** | Exact scoped amendments and compatibility classification during migration |
| New UPM package | **Not created** | Package creation, dependency resolution and Unity import under separate execution plan |
| New Player/Actor lifecycle/Composer code | **Not implemented** | Implement, validate targeted Actor/Camera/content sequences with canonical logger |
| Public API maturity | **Unchanged** | IF-GOV-001 individual type/contract maturity review; no automatic Stable status |
| Framework release | **No version/release action** | Separate release gate under IF-ADR-047/048 |

## 7. Exit gate for the implementation plan

An implementation plan may now be authored **against this accepted target**. Before any API-breaking increment, it must identify the *exact* affected current contracts, changes and migration path; validate whether Stable authoring or consumer guarantees would be affected. Define targeted proof for N1 (reactivation during release), N2 (early selection event), N3 (rollback stale revision), N4 (lazy occurrence identity), N6 (overlapping roots) and postcommit degraded outcomes. Do not expand validation into generic testing of unrelated Framework capabilities.

The implementation plan must sequence Core/Player semantic corrections, minimal Composition package creation, unified visual authoring migration, consumer adjustments and package validation without introducing competing Current/Composer authorities.

**No work in this record changes code, assets, dependency versions, existing published packages or Trello state.**
