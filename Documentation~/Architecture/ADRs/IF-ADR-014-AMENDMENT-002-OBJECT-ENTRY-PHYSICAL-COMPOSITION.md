# IF-ADR-014 Amendment 002 — Optional Physical Composition of Object Entries

Status: **Accepted** — 2026-11-05 (Project Owner; architecture only, subject to API review and validation)  
Amends: [IF-ADR-014 — Authored Definition and Stable Identity Authority](IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md)  
Related: [IF-ADR-014 Amendment 001 — Physical Recognition](IF-ADR-014-AMENDMENT-001-PHYSICAL-OBJECT-ENTRY-RECOGNITION.md); IF-ADR-001 (lifecycle); IF-ADR-050 (Actor composition precedent); IF-GOV-001 (API governance)  
Owner: **com.immersive.framework** (Runtime Content / Object Entry integration)

> **Decision boundary:** The Project Owner accepted the architecture on 2026-11-05, not an implementation or final public API shape. Public contracts remain subject to API review. Acceptance does not certify Unity behavior or implementation. B1 authoring implementation and its pending QA are tracked separately.

## 1. Context

Object Entry already has a logical `ObjectEntryId` and a separate physical, owner-scoped `StableObjectBinding`. The Framework host registers bindings from `ObjectEntryDeclaration` components under admitted Route/Activity scene roots, commits the owner transaction, and rolls it back or releases it. A declaration, query resolution or physical recognition result alone does not grant authority to modify content.

`com.immersive.composition` already supplies an engine-free `CompositionComposer` with keyed contributions, precedence, dependencies, current-target guards, revisions, async preparation, and acquired/adopted resource handling. The existing Framework integration is Actor-specific and must not be reused by converting Object Entries into Actors.

The new requirement is optional composition of **the exact admitted physical Object Entry occurrence**, whether its capabilities were authored in a scene or installed through composition, without changing the root, identity or Runtime Content owner.

## 2. Decision

### 2.1 Ownership and dependency direction

- `RuntimeContent` remains the single authority for physical admission, owner identity, rollback and release. The Framework host owns the integration lifetime.
- `com.immersive.composition` remains a generic engine-free kernel. Framework code translates authored object parts into Composer contributions and Unity materializers. No Unity, Object Entry, Player or lifecycle knowledge is added to the kernel in this cut.
- An optional Composer is scoped to **one admitted physical Object Entry binding**. There is no global Composer, parallel binding registry, service locator or new persistent object identity.
- `ObjectEntryDeclaration` remains a lightweight identity/requiredness component. A normal declared object without composition authoring continues to work and must not allocate a Composer.
- Player selection, gameplay eligibility, outline, shaders and contextual interaction are consumer responsibilities, not Composer decisions.

### 2.2 Exact physical occurrence and write authority

An Object Entry composition access must be issued only by the Framework's owner-bound admission integration, never by casting an `IObjectEntryOccurrenceQuery` or `IObjectEntryRecognitionQuery` result into write permission.

A contribution is valid only while the **same committed binding record** remains current. The current-target check must include:

1. Exact `RuntimeContentOwner` (scope, stable owner identity and definition-instance token);
2. Exact `ObjectEntryId` and the `GameObject` holding the corresponding declaration, still alive;
3. The same binding **registration occurrence**, not merely equality of ID, owner and Unity object reference;
4. A non-released, non-rolled-back Framework owner and a non-closed composition context.

The present binding records have no public occurrence token. Implementation may retain internal reference identity to the exact registered binding or an equivalent owner-private generation that proves continuity; it must not assume that an ID/owner/GameObject triple is sufficient after release and readmission. `CompositionContextId` is internal composition context identity, not a replacement for Framework occurrence identity.

Duplicate or ambiguous committed binding evidence is **not** resolved by first match. The target is ineligible for composition access until the exact binding can be established unambiguously. Public queries remain read-only and retain their existing behavior.

### 2.3 Optional authoring and materialization

- A reusable optional composition profile, linked by authoring to the **same physical declaration root**, may declare keyed parts, dependencies and explicit configuration. The specific class names, serialized fields and consumer API await the implementation/API review.
- The profile and parts are not added to `ObjectEntryDeclaration`. Authoring validates missing references, duplicate keys, dependency cycles, unsupported part configurations and root mismatch.
- No profile and no explicit authorized contribution means **no Composer**. Invalid opt-in authoring is diagnosed explicitly; it is never silently treated as a valid composed occurrence.
- Parts may install or remove only the capabilities/resources they own. They may not replace, destroy, reparent or recreate the Object Entry root, its declaration, its `ObjectEntryId` or its Runtime Content owner.
- Materializers distinguish `PartOwnership.Acquired` from `PartOwnership.Adopted` and retain exact provenance. Adopted pre-existing scene resources are not destroyed on removal or close; acquired resources are released only by their owner/materializer.
- A capability component does not depend on the Composer to operate: the same component can be authored directly or supplied by a compatible materializer.
- New nested `ObjectEntryDeclaration` components are not implicitly admitted by part materialization. They require a separate authorized Object Entry admission contract, which is outside this cut.

### 2.4 Transaction timing, lifecycle and failure semantics

- Prepare: resolve and validate optional authoring against roots admitted by the **existing Route/Activity transaction**. Never publish a writable contribution against an uncommitted binding.
- Activation: only after that owner has a live committed binding may the Framework create/open the corresponding Composer and publish a per-occurrence contribution surface. Initial-part failure must be explicit and compensated or marked as degraded; it must never masquerade as successful configured admission.
- Mutation: set, replace and remove operations use the Composer's existing contribution/priority/dependency semantics and a current-target guard. An obsolete occurrence yields a stale/rejected outcome, never effects against a new occurrence with the same stable ID.
- Rollback, release, replacement and host teardown: first invalidate further composition writes; cancel or drain in-flight work; close affected contributions/Composer and reconcile acquired/adopted resources **before physical object release**. Then remove the Framework binding through its existing owner path.
- Async cancellation, `CloseAsync`, pending/failed materializer release and Unity destruction may not be represented as synchronous success. Report partial effects and degraded cleanup accurately; do not silently abandon pending resources or resurrect a released root.
- The implementation must prove its ordering against existing Route/Activity transaction and host teardown seams before changing code. This amendment does not prescribe a new global scheduler, new lifecycle authority or blocking wait on Unity's main thread.

### 2.5 Initial scope and compatibility

The initial physical integration supports **Route** and **Activity** Object Entries already admitted by the current flow. Although `ObjectEntryScope` declares Session, the current `StableObjectBindingRegistry` rejects Session owner registration; Session composition is not promised here.

Actor composition and its public contracts remain unchanged. No unified Actor/Object Entry entity model is introduced. Existing `IObjectEntryOccurrenceQuery` and `IObjectEntryRecognitionQuery` remain read-only and unaffected. Any new consumer-facing API is **Experimental**, subject to IF-GOV-001 and explicit API review.

## 3. Alternatives

| Option | Decision |
|---|---|
| Put Object Entry and Unity lifecycle inside `com.immersive.composition` | Rejected: breaks the kernel's engine-free boundary. |
| Make gameplay consumers create Composers from query results | Rejected for official integration: bypasses owner-bound write/cleanup authority. |
| Reuse Actor-specific composition assets and bindings verbatim | Rejected: couples non-Actor objects to Actor profile and visual-mount assumptions. |
| Integrate optional per-binding composition inside Framework | **Selected for proposal:** reuses both existing owners without adding a parallel registry. |

## 4. Implementation cuts and required evidence

**B — Optional authoring and adapter:** inspect actual transaction seams; add the minimum reusable profile/part configuration and Unity materializer needed for a concrete capability. No refactor of Actor composition and no generic materializer catalogue without a real second use.

**C — Contribution and lifecycle:** provide owner-issued per-occurrence write access, prove the exact-record current guard, integrate commit/rollback/release/teardown ordering, and surface stale, ambiguous, pending and degraded outcomes.

**D — QA and documentation:** validate through QAFramework and update API/authoring guides and the Framework tracker. Evidence must cover:

1. Object without composition authoring: unchanged behavior, no Composer.
2. Same component authored directly or installed by composition: equivalent behavior.
3. Same root and logical ID throughout set/replace/remove; no duplicate or parallel bindings.
4. Key conflicts and dependencies; invalid authored configuration produces explicit diagnostics.
5. Release/readmission using the same ID, owner and GameObject: stale access cannot mutate the new registration.
6. Ambiguous binding, destroyed root and uncommitted owner: no write access.
7. Acquired versus adopted resources; partial failures and pending cleanup are not reported as success.
8. Route/Activity commit, rollback, release and host teardown: no surviving writes or unintended destruction.
9. Actor composition, occurrence resolution and physical recognition: no regressions.

**Excluded:** Session physical admission, dynamically spawned/transferred Object Entries, PEGA outline/selection/gameplay, new global infrastructure and changes to the generic Composition package without a demonstrated kernel deficiency.

## 5. Evidence reviewed and status

Source baseline examined: Framework `master` at `50ede592606fbeb7a4045d402a674776361f6376`; Composition `main` at `be73693470424335b76212eb82093833faf1a9ba` (2026-11-05).

Relevant source: `Runtime/RuntimeContent/StableObjectBindingRegistry.cs`; `Runtime/RuntimeContent/RuntimeContentOwner.cs`; `Runtime/RouteLifecycle/RouteLifecycleRuntime.ResettableRegistration.cs`; `Runtime/ActivityFlow/ActivityFlowRuntime.ResettableRegistration.cs`; `Runtime/ApplicationLifecycle/FrameworkRuntimeHost.ResettableRegistration.cs`; `Runtime/ObjectEntry/ObjectEntryOccurrenceQueryBinding.cs`; Composition `Runtime/CompositionComposer.cs` and `Documentation~/Composition-Boundary.md`.

This is **static architecture analysis** only. Implemented: **NO**. QA authored: **NO**. Unity tested: **NO**. Integrated: **NO**. Validated: **NO**. Acceptance: **PENDING**.
