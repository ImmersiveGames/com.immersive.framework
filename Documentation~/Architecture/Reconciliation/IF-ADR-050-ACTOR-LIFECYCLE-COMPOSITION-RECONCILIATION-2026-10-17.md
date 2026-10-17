# IF-ADR-050 — Accepted-ADR Reconciliation for Actor Lifecycle and Composition — 2026-10-17

Status: **Reconciled — accepted future architecture; current runtime contracts preserved**
Reconciliation updated: **2026-10-17**  
Date: **2026-10-17**  
Scope: **documentation only; no runtime/API, certification, package, serialization or maturity change**  
Target decision: [IF-ADR-050](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md)  
Technical companion: [IF-SPEC-050](../Specifications/IF-SPEC-050-Actor-Lifecycle-Participation-and-Composition.md)  
Governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

## 1. Purpose and authority rule

This record reconciles the **accepted future** Actor occurrence/composition direction with existing **accepted** Framework runtime decisions, without retrospectively replacing their requirements, changing completed QA evidence or suggesting the new behavior has shipped.

**Two time-scoped authorities must not be confused:**

1. **Current/published runtime contract:** accepted ADRs and the actually implemented API still govern consumers. Historical certifications retain only their tested revision and scope.
2. **Accepted future architecture:** IF-ADR-050/IF-SPEC-050 now authorize the target design for subsequent migration planning. The [accepted contract migration boundary](IF-ADR-050-ACCEPTED-TARGET-CONTRACT-MIGRATION-2026-10-17.md) resolves prospective contract direction; the new runtime behavior is **not yet implemented**, and Stable guarantees cannot be silently replaced.

This reconciliation is a **decision crosswalk**, not a code plan or product release. Where the accepted *future* target conflicts with current implemented/published contracts, the latter continue to govern existing consumers until their exact migrations are released. The target governs what future implementation must achieve. Do not rename existing runtime types, reclassify QA results or advance API maturity on this record alone.

## 2. Boundaries that remain intact

| Authority | Preserved boundary |
|---|---|
| [IF-ADR-003](../ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | Session-owned Joined Player, Actor selection and preparation barriers, Manager/Scene provisioning, separate Activity representation/readiness; no implicit Join/Leave on Actor replacement. |
| [IF-ADR-020](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) | Exact Player occurrence for terminal Leave, Activity release before Session-owned physical teardown, Scene-Provided ownership, no accidental Slot reuse or foreign cleanup. |
| [IF-ADR-025](../ADRs/IF-ADR-025-Local-Player-Input-Ownership-and-Device-Association.md) | Local Player Host and PlayerInput/device ownership remain Player-participation concerns; no part materializer owns PlayerInput or changes Input pairing. |
| [IF-ADR-038](../ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md) | Physical Actor root owns pose/spatial authority; Camera assignment/Player membership and Camera Subject are distinct from replaceable visual content. |
| [IF-ADR-014](../ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | Typed authored identities are not runtime lifetime/owner evidence. Resource/binding teardown remains owner-scoped. |
| [IF-ADR-040](../ADRs/IF-ADR-040-Scene-Composition-Binding-Model.md) | Bounded Scene discovery and Scene-owned resource authority; this Scene-composition model is not the proposed keyed Actor-content Composer. |
| [IF-ADR-049](../ADRs/IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md) | Exact root/owner evidence, owner-scoped NonPlayer publication, no global lookup, read-only observers, distinguish physical reference from gameplay eligibility. |

The proposed `com.immersive.composition` kernel is **not implemented or installed**. It should eventually handle keyed content, not Session, Actor lifecycle, input, Camera or root ownership. A Framework dependency on it is a **future package decision**, not the current `package.json`.

## 3. Explicit conflict / reconciliation matrix

| ID | Accepted evidence and present behavior | Accepted future target under IF-ADR-050 | Contract migration action before implementation/release |
|---|---|---|---|
| R01 — ActorId domain | [IF-ADR-023A](IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md) says preparation/adoption establishes an occurrence-like `ActorId`; current Player materializer creates ActorId per materialization. | `ActorId` identifies logical Actor; `ActorOccurrenceId` identifies one committed physical incarnation; neither is a universal alias of `PlayerOccurrenceId`. | Expressly amend the historical identity boundary for the future contract, preserve 023A as historical evidence; define migration of `PlayerActorDeclaration`, tokens, selectors and serialized references. |
| R02 — Player lifetime | IF-ADR-003/020/025 separate Joined Player from Activity and PlayerInput. | PlayerOccurrenceId remains per Join; a single Player can continue with another logical Actor; recomposition preserves PlayerOccurrenceId. | Clarify identity vocabulary, without moving Session/PlayerInput authority to Composition. |
| R03 — Physical replacement | [IF-ADR-024](../ADRs/IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md) V1 releases A contextual gameplay and occupancy before physical replacement and restores A in some precommit failures; QA certified this implementation boundary. | A stays logically Current while B is staged; guarded synchronous A deactivate → B activate → conditional commit+selection → notification, followed by gameplay/Camera reprojection. A may be physically unavailable during callbacks. | Amend replacement ordering, availability/failure results, occupancy/gate timing and notification semantics **prospectively**. Preserve accepted V1 and its dated certification until runtime migration is implemented and validated. |
| R04 — Public occurrence | IF-ADR-049 resolves current valid **physical** occurrence and rejects unavailable/stale state. Current Player resolver allocates IDs lazily and discards them when physical predicates fail. | Lifecycle commit owns `ActorOccurrenceId`; it persists through temporary physical inactivity. Logical binding may still identify A during handoff even when physical resolution reports unavailable. | Preserve existing physical-resolution semantics or explicitly version a separate committed-binding/availability surface. **Do not** redefine `TryResolve` success to mean “inactive but committed” without an accepted API amendment. |
| R05 — Publication / reentrancy | IF-ADR-003/024 and Camera observers currently see Player selection changes during replacement. | Selection/occurrence update is coherent before notification; callbacks during physical transition see the last committed binding and cannot initiate competing mutations. | Define barrier scope, observer ordering, forced owner closure, stale callbacks, explicit busy/partial results and exact lifecycle domain in the replacement contract. |
| R06 — Camera | IF-ADR-038 owns Player assignment and root/Subject spatial boundaries; existing Session Camera may cache a child Transform. | Camera continues to follow committed physical root; Subject anchor stays outside replaceable visual mounts by default. A content delta does not change Player or physical Actor occurrence. | Require stable root observation anchor for the initial feature or define explicit Camera rebinding on content delta; do not let content Composer acquire Camera authority. |
| R07 — Scene-Provided | IF-ADR-003/020 support Scene-provided adopted physical resources; IF-ADR-049 requires valid producer/owner publication. | Adopted content part is recorded with provenance, not duplicated or destroyed by kernel; late adoption offers reduced preactivation guarantees. | Amend future authoring/content contracts only; preserve existing scene ownership, Session survival and source-scoped release. |
| R08 — Content and profile | IF-ADR-003/038 describe optional profile presentation content subordinate to root. IF-ADR-024 treats prepared Actor replacement as a distinct command. | `presentationPrefab` becomes a Base entry at key `visual`, `VisualContentMount` becomes its mount; parts can be replaced without replacing root/occurrence. | Migrate authored/serialized data and current materializer path explicitly. Do **not** silently turn `RequestReplaceActorSelection` into prepared hot-swap; adopt a separate content command/policy. |
| R09 — Resources/cleanup | IF-ADR-020 owns terminal Player release; IF-ADR-001/014 own scoped resource release and identity boundaries. | Composer owns current part binding, each materializer owns its acquisitions, producer owns root. Release request/pending/completed must not be conflated. | Preserve exact owner, idempotency and cleanup evidence; define additional async receipts only where required. |
| R10 — Runtime status | Historical Full Player QA/Stable decisions certify existing specific paths. | Future migration to `framework → composition` and corrected replacement/reference runtime. | New feature tests/evidence must be gathered for changed contracts; **never** retroactively classify earlier QA as proof of the target. |

## 4. State and observer reconciliation

The proposed handoff has **two different truths**. This must survive API design:

| Phase | Committed logical selection/binding | Physical availability | Public change notification |
|---|---|---|---|
| B staged / preparation async | A | A available; B private candidate | None |
| `OnDisable(A)` | A until conditional commit | A may already be inactive | None |
| `Awake(B)` / `OnEnable(B)` | A until conditional commit | B physically activating; A inactive; B not Current | None |
| Structural revalidation / commit | One coherent switch A→B, or A stays binding with a degraded result | Candidate may be active; callbacks may already have produced effects | Not until coherent commit |
| After commit / observation | B | Readiness, availability, gameplay admission independently determined | Change published; observers see B |
| Cleanup | B, until a later authoritative close | Retired A releases under producer/resource owners | Release diagnostics independent from Current |

A **same-call-stack transition is not callback-atomic**. `Awake`, `OnEnable`, `OnDisable`, C# events and direct callbacks may run during it. Reject or defer incompatible same-domain reentrant mutations with observable results, but forced owner closure/invalidation must not be dropped. A failed handoff after deactivating A cannot claim successful restoration just because A remains the last logical binding.

**Important public API limitation:** IF-ADR-049 physical resolution currently requires a valid available physical occurrence. Preserving a committed `ActorOccurrenceId` does not require returning success from an existing physical resolver when its root is unavailable. A separate committed-binding query or typed availability result is necessary if consumers must observe A's committed identity during an inactive transition.

## 5. Two distinct commit authorities

| Commit | Sole authority | Effect | No effect on |
|---|---|---|---|
| Actor occurrence replacement | Framework lifecycle / Session selection integration as appropriate | Current occurrence/root/owner evidence and selected profile (where applicable) change coherently, new ActorOccurrenceId issued | PlayerOccurrenceId, Camera assignment occurrence, producer root ownership |
| Composition dependency closure | One Composer bound to that root, under the owning producer/materializer contract | Winning content per affected key, revision/delta after apply | ActorId, ActorOccurrenceId, root, Session membership or committed Player selection |

Composition revision is not `SelectionRevision` and does not establish physical Actor identity. Independent content keys should not be blocked by unrelated slow preparation; dependent keys must form one closure. Ownership of *contribution intent*, *part instance* and *Actor root* may belong to three different parties.

## 6. Required sequencing and documentation dispositions

1. **Architecture accepted (2026-10-17):** IF-ADR-050 and IF-SPEC-050 establish the future architecture. The [migration boundary](IF-ADR-050-ACCEPTED-TARGET-CONTRACT-MIGRATION-2026-10-17.md) documents exact R01/R03/R04 effects and protects existing contract evidence. Forward-reconciliation notices in accepted ADRs describe prospective changes without silently superseding current runtime.
2. **Implementation plan next:** design the explicit content-only versus physical replacement command/policy; define the typed availability/result surfaces, public-API maturity and serialized `ActorId`/`ActorOccurrenceId`/`presentationPrefab` migration according to the accepted contract boundary.
3. **Before any breaking API release:** complete the scoped compatibility and migration review under IF-GOV-001 and IF-ADR-047.
4. **Implementation increments:** fix separately evidenced current defects N1 visual reactivation on release, N2 early selection publication, N3 rollback revision stale, N4 lazy occurrence ID and N6 A/B overlap, either as bounded corrections or within explicit migration tasks. Test changed Actor/Camera/Player workflows in Framework, with canonical logger evidence.
5. **After verification:** update implementation/maturity matrices and current-status tracking. Do not rewrite historical QA or retroactively claim passed tests.

## 7. Provisions not reopened

No request here to alter IF-ADR-014 stable identity rules, IF-ADR-020 terminal Leave order/ownership, IF-ADR-025 physical PlayerInput device ownership, IF-ADR-038 Camera assignment policy, or IF-ADR-040 Scene discovery authority. The future implementation must satisfy these constraints; if evidence shows a true conflict, raise a new scoped amendment rather than broadening this reconciliation implicitly.

## 8. Review and unresolved proof

The target Unity version may require a bounded callback-order and exception-propagation proof before anyone claims that `SetActive` detects callback exceptions; no such claim is made. The architectural contract itself is conservative and does not depend on exception propagation.

This record **completes the documentary crosswalk**. IF-ADR-050 is now **Accepted as architecture target**; this crosswalk does **not** change implemented/published APIs, execute their migrations, approve a UPM release or certify the target runtime. Those remain separate actions. Existing consumer QA remains valid only for its dated tested boundary.
