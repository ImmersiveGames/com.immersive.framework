# IF-ADR-014 Amendment 003 — Dynamic Object Entry Composition

Status: **Proposed — Pending Project Owner Approval**
Amends: [IF-ADR-014 — Authored Definition and Stable Identity Authority](IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md)
Related: [Amendment 001](IF-ADR-014-AMENDMENT-001-PHYSICAL-OBJECT-ENTRY-RECOGNITION.md); [Amendment 002](IF-ADR-014-AMENDMENT-002-OBJECT-ENTRY-PHYSICAL-COMPOSITION.md); [IF-ADR-050](IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md); [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
Owner: **com.immersive.framework** (Runtime Content / Object Entry integration)

> **Decision boundary:** This proposal authorizes no implementation. It does not approve public API signatures, change accepted B2 behavior, alter Actor Composition or the Composition kernel, or certify Unity behavior. A future public surface remains Experimental until separately reviewed under IF-GOV-001.

## 1. Context

Amendment 002 established optional, profile-authored composition for an exact admitted Object Entry binding. B2 is implemented and merged through Framework PRs [#21](https://github.com/ImmersiveGames/com.immersive.framework/pull/21) and [#22](https://github.com/ImmersiveGames/com.immersive.framework/pull/22), on master baseline a9e379362dd80dd8dc581215a62d331dd62baf14. PR #22 reports owner-run validation of 518 Edit Mode tests and the IF-ADR-014 Play Mode scenario at 10/10. That evidence applies to B2, not this proposal.

Corte C would let authorized consumers add, replace and remove keyed parts while the same physical Object Entry occurrence remains active. Visual selection feedback is one possible consumer. The Composer must remain generic and must not decide Player selection, interaction eligibility or gameplay.

The QAFramework resolves com.immersive.composition 0.1.0-preview.3 at commit ef577765d0b565a020f3e98f6e5eaae681333c29. The checked local Composition repository head is newer and is not the QA-resolved source. This proposal relies on the QA-resolved contract and requires no Composition package change.

## 2. Proposed decision

### 2.1 Ownership and identity

- ObjectEntryId remains logical identity only. It does not identify an occurrence or own lifecycle.
- RuntimeContent and StableObjectBindingRegistry remain the only authorities for physical admission, exact binding records, owner identity, rollback, release and retry.
- A composition context belongs to one exact registered StableObjectBinding record instance. It must never re-resolve a target by ObjectEntryId, owner or GameObject.
- com.immersive.framework owns the integration, contribution wrappers, validation and Unity materializer. com.immersive.composition remains the engine-free generic kernel.
- A contribution owns only its keyed intent. The Framework materializer owns Acquired resources; a contribution does not own the root, RuntimeContent owner, composition context or release lifecycle.
- IObjectEntryOccurrenceQuery and IObjectEntryRecognitionQuery remain read-only. Their results are never write grants.
- Actor, Player and Object Entry identity/lifecycle domains remain separate.

### 2.2 Explicit opt-in and lazy Composer

Reuse ObjectEntryCompositionAuthoring on the same GameObject as ObjectEntryDeclaration; do not add a second public access component.

- Add an explicit runtime-contribution opt-in, default false. Existing Profile-only B2 authoring keeps its current validation and activation behavior.
- When the new opt-in is false, an authored component still requires its B2 Profile. When true, the Profile may be absent; if present, it is validated and activated as the Base contribution.
- An Object Entry with no composition authoring remains plain and allocates no Composer. Profile-only B2 authoring with dynamic opt-in disabled preserves its existing Composer/Base and receives no dynamic consumer grant. A declaration with composition authoring but no Profile remains invalid unless dynamic opt-in is enabled.
- For Profile-backed objects, grant access only after owner commit and successful Base activation. Commit alone is insufficient because Base activation is asynchronous and can fail after commit.
- For an opted-in object without a Profile, create the Composer lazily on the first authorized contribution open. Opening before admission, during Base activation or after invalidation fails explicitly.

The authoring component may provide an immutable access grant for one exact occurrence after commit and, when present, Base activation. The component is only the handoff point; possession of a Unity reference is not occurrence authority. TryOpenContribution operates on that grant. Each opened contribution captures the same exact binding record and context, and neither grant nor contribution re-resolves by logical identity. Old grants and handles stay stale after release/readmission, even when ObjectEntryId, RuntimeContentOwner and GameObject are reused.

### 2.3 Contribution and API shape

Give each consumer an independent Framework wrapper around one Composer contribution. Framework creates the CompositionOwnerId; the caller cannot choose or reuse another contribution's identity.

Candidate operations, with names/signatures subject to API review:

- obtain an immutable per-occurrence access grant from the opted-in authoring handoff, then TryOpenContribution on that grant;
- SetPartAsync and RemovePartAsync;
- CloseAsync to close that consumer's contribution intent.

Defer a public batch operation until a concrete consumer demonstrates a need for one atomic multi-key update; the kernel SetPartsAsync remains reusable internally. Defer public delta events as well: operation results report the caller's revision and changed keys, and no current Object Entry consumer requires observing other contributors' commits.

Return a Framework-owned typed result with status, revision, changed keys, diagnostics and whether effects may have applied. Do not return CompositionComposer, CompositionContribution, PartDefinition or other kernel handles. Cancellation, NoChange, Faulted, Degraded and successful intent updates are not proof of physical cleanup.

Reserve int.MinValue for the B2 Base. Dynamic contributions have unique Framework-generated owner IDs and explicit priorities. The Composer alone resolves precedence and dependencies. SetPart/Remove operations with an equal highest-priority conflict reject before materializer effects. Closing a higher-priority winner can expose a hidden tie and degrade; report the kernel outcome instead of adding a second resolver. Removing an override may rematerialize the next valid contribution or Base under the same root; reusing the same child instance is not promised.

### 2.4 Materialization and package boundary

Reuse the B2 ObjectEntryCompositionPartMaterializer and exact-root guards, subject to C2 validation and retry coverage. Dynamic prefab validation must continue to reject nested Object Entry identity, composition authoring, Actor host and PlayerInput.

The first public dynamic contract is prefab-backed and Acquired. Current Profile authoring is prefab-only; its internal Adopted path does not authorize public adoption of arbitrary scene instances. Defer public adoption until a consumer and exact external ownership/provenance contract justify it. The kernel's generic Acquired/Adopted model remains unchanged.

Dependency direction:

    com.immersive.framework
            ↓
    com.immersive.composition

No new package, global service, duplicate registry, Actor conversion or Composition-kernel change is proposed. The missing surface belongs to Framework authority/integration; the kernel already supplies keyed contributions, priorities, dependencies, guarded mutation, batches, revisions, deltas and explicit outcomes.

### 2.5 Lifecycle and failure contract

1. Validate the opt-in during owner registration and snapshot it into the exact binding. Invalid opt-in fails admission with a diagnostic.
2. Commit the owner transaction. Issue no grant until optional Base activation succeeds.
3. Check the captured record, owner, context and opt-in on every open/mutation. Never look up a fresh target by logical ID.
4. At release start, block opens/mutations and invalidate leases before cancelling/draining work. Consumer CloseAsync closes intent only; it is not the physical cleanup barrier.
5. The existing owner path closes the Composer, reconciles Acquired resources and retains owner/bindings while cleanup or a safety condition is unresolved. Release scene content only after confirmed cleanup.
6. Activity restart/switch/clear, Route switch and Controlled Stop continue through existing RuntimeContent barriers. No new lifecycle owner is introduced.
7. C3 must cancel before stop waits for outstanding operations. FrameworkRuntimeHost.StopAsync currently waits before Activity clear and Route exit; work that only receives cancellation from owner release could prevent Stop from reaching release.
8. OnDestroy/OnApplicationQuit invalidate access synchronously but cannot await Route/Activity release. C3 must prove cleanup under the existing owner authority. If it cannot, any limitation to best-effort teardown requires a separate explicit Project Owner approval before C is certified; Amendment 003 does not narrow Amendment 002. Do not claim confirmed cleanup without evidence.
9. Never destroy, replace or reparent the Object Entry root. Preserve partial-effect, cancellation, pending-cleanup and diagnostic outcomes.

### 2.6 Alternatives

| Alternative | Disposition |
|---|---|
| Add an ObjectEntryCompositionAccessBinding beside B2 authoring | Not recommended initially. It duplicates the existing root-level composition opt-in. Reconsider only if API/Inspector review proves distinct authoring semantics are needed. |
| Turn query/recognition results into write access | Rejected; those contracts are read-only and can resolve occurrences outside an anchor's owner. |
| Let consumers create Composer contexts or expose CompositionComposer | Rejected; this bypasses exact binding and cleanup authority. |
| Convert Object Entries to Actors or reuse Actor hosts/profiles/mounts | Rejected; Actor and Player assumptions are not Object Entry contracts. Only the generic wrapper pattern is reusable. |
| Add a second composition mechanism or change com.immersive.composition | Rejected; it duplicates existing generic capabilities without a demonstrated kernel deficiency. |
| Allocate a Composer for every declaration | Rejected; ordinary Object Entries remain optional and unchanged. |
| Keep composition Profile-only | Rejected for dynamic independent contributors; a Profile supplies Base content, not runtime access. |

## 3. Consequences and unresolved risks

- Initial scope: already admitted Route and Activity Object Entries. Session admission, dynamic spawning, owner transfer and mobile-item lifecycle remain excluded.
- The authoring component gains a default-off opt-in and a profile-less mode. Serialization and Inspector behavior require C1 review.
- Contribution close is not physical cleanup. Kernel close can remove intent before effects; a later close may say NoChange after detach failure. Framework release owns retry and confirmation.
- Async cancellation/drain and Host Stop ordering need a forward-progress protocol that does not block Unity's main thread.
- Higher-priority intent can hide ties; closing the winner can expose a conflict and fail to restore Base.
- Abrupt Host destruction is not a confirmed release until C3 proves a completion path.

## 4. Acceptance criteria

Future implementation must demonstrate:

1. No composition authoring means no Composer and unchanged plain-object behavior. Profile-only B2 keeps its Composer/Base and receives no dynamic grant; profile-less opt-in creates a Composer only on the first authorized open.
2. Dynamic-only opt-in opens a lazy context after commit; Profile-backed access waits until Base is active.
3. A prior handle cannot mutate a new binding after readmission, including reuse of ID, owner and GameObject.
4. Set/replace/remove preserve the exact root, declaration, logical identity and binding.
5. Independent contributions use one Composer and kernel conflict/dependency rules; no parallel resolver exists.
6. Acquired resources are released only by the Framework materializer that acquired them; pending/failure retains owner evidence.
7. Cancellation/stop cannot deadlock; work and cleanup are drained or explicitly pending before scene release.
8. Activity/Route lifecycle and Controlled Stop preserve owner barriers; abrupt teardown is proven or explicitly bounded.
9. Actor Composition and read-only Object Entry queries/recognition retain their contracts.
10. Edit Mode and Play Mode QA cover races, ties, readmission and physical cleanup.

## 5. Project Owner decisions required

Recommendations are not approvals.

| # | Decision | Recommendation |
|---|---|---|
| 1 | Root opt-in/handoff or separate access binding | Reuse ObjectEntryCompositionAuthoring on the declaration root with default-off runtime opt-in; issue a private exact-binding lease after commit and Base readiness. |
| 2 | Dynamic composition without Profile | Allow only by explicit opt-in. Keep Profile-backed B2 intact; lazily create the Composer at first authorized open. |
| 3 | Prefab-only or public adoption | Start with prefab-backed Acquired parts. Defer public Adopted instances pending exact ownership/provenance and a concrete consumer. |
| 4 | Batch operations and delta events | Defer public batch operations and delta events until a consumer demonstrates need. Keep single-key Set/Remove and per-operation results initially. |
| 5 | Composition versus gameplay authorization | Grant keyed-part mutation on the exact occurrence only. Selection, target eligibility, interaction and gameplay authorization remain consumer-owned. |

## 6. Status

This amendment remains **Proposed — Pending Project Owner Approval**. Approval of the five decisions is required before C1 implementation. API review, C3 teardown/cancellation gates (including explicit approval if teardown guarantees must be limited) and C4 evidence remain separate. Corte C is not implemented, integrated or validated.
