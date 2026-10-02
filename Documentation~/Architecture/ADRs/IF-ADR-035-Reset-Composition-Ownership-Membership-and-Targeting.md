# IF-ADR-035 — Reset Composition, Ownership, Membership and Targeting

Status: **Accepted**
Last updated: **2026-10-02**
Normative classification: **Corrective Reset architecture authority**
Supersedes: **the Reset authoring, explicit scope authoring, explicit subject-selection and Unity registration assumptions of IF-ADR-005 where they conflict with this ADR**
Reopens / requires reconciliation: **IF-ADR-001, IF-ADR-002, IF-ADR-005, IF-ADR-010**
Supersedes as implementation direction: **IF-ADR-034 draft in its current per-GameObject Cycle Reset authoring form**
Related: **RuntimeContent ownership, Activity Restart, Cycle Reset, Unity authoring, cross-boundary identity**

## 1. Context

The current Reset implementation proved several valuable runtime contracts:

- typed Reset subjects and participants;
- explicit registration and unregistration;
- owner-aware registry records;
- deterministic participant execution;
- typed results and issues;
- individual and selected-set execution;
- Activity Restart orchestration;
- separation between Object Reset and Cycle Reset.

The implementation also exposed too much runtime bookkeeping as normal Unity authoring.

The current normal path can require consumers to author:

- textual Reset subject identifiers;
- Reset scope on each subject;
- participant identifiers;
- explicit lists of subject references or identifiers;
- group identifiers;
- selection modes that mirror runtime registry concepts.

This is technically explicit, but it scales authoring cost with the number of gameplay objects and relationships rather than with meaningful gameplay composition boundaries.

Consumer validation in the Getting Started / planet-devourer sample demonstrated the problem:

- individual cross-scene Object Reset works;
- explicit A+B selection works and correctly excludes C;
- Activity Cycle Reset correctly remains independent from Object Reset;
- the next expansion toward Cycle Reset would have introduced another per-object registration surface.

A read-only architecture review of the real Framework, QAFramework and planet-devourer code then established that the Reset execution core is reusable and that the primary corrective boundary is Unity authoring, registration and target resolution.

## 2. Evidence and corrected observations

### 2.1 Reset execution core is reusable

`ResetRegistry`, `ResetExecutor`, typed execution requests/results/issues and the existing local Reset participants can support a new authoring model.

Runtime-generated subject identity already exists through the registration runtime.

The corrective architecture therefore does not require a Reset engine rewrite.

### 2.2 Composition must not be a Reset subject

A `ResetComposition` is a set/resolution boundary.

It is not itself an executable Reset subject.

Registering both a composition and its members as subjects would permit the same local Reset capability to execute twice when the member is selected both individually and through the composition.

The executable unit remains the `Resettable`.

### 2.3 Content ownership and Reset membership are different concepts

Content ownership answers:

> Which runtime occurrence owns this object, keeps it alive and releases it?

Reset membership answers:

> Which semantic Reset target should include this Resettable?

They must not be represented by one authored `ResetSubjectScope`.

A real sample proves the distinction:

- the Visitors NPCs physically live in Route-owned content so that they survive Activity Clear;
- their desired Reset membership can still be Activity;
- therefore Route lifetime does not imply Route-only Reset membership.

### 2.4 Ownership must come from origin, not current state at OnEnable

The current Unity adapter resolves an authored scope against the current runtime owner during component registration.

During an Activity A -> B transition, scene availability can occur before the target Activity becomes current. This makes current-state lookup the wrong ownership authority for newly materialized target content.

The Framework already has the correct pattern in owner-aware transaction binding:

- target owner exists before scene loading;
- materialized roots are available before commit;
- preparation can register against the target owner;
- pre-commit failure can roll back target registration;
- previous-owner release can be explicit.

Reset registration must follow this transaction-aware pattern for Route and Activity content.

### 2.5 Runtime materialization already carries ownership

`RuntimeMaterializationRequest` carries runtime ownership information.

`UnityPrefabRuntimeMaterializationAdapter` currently instantiates and records the physical object but does not integrate Reset registration.

Runtime-created Reset content must consume the materialization owner instead of attempting to infer a current global owner.

Arbitrary `Object.Instantiate` outside an owner-aware Framework boundary has no derivable Framework content owner and must not silently acquire one.

### 2.6 Cross-scene specific targeting is a real boundary

Unity object references cannot normally serialize arbitrary references between independent scenes.

The sample has a real cross-scene case:

- Reset triggers live in RouteContent;
- target NPCs live in the Route Primary scene;
- both scenes are Route-owned;
- the trigger needs to address a specific NPC or subset, not the entire Route.

The existing textual Reset subject ID currently supplies this boundary.

The corrective architecture must preserve cross-boundary specific targeting, but stable identity becomes opt-in boundary identity rather than mandatory Reset authoring for every object.

Stable cross-boundary targeting consumes the generic `StableObjectBinding` authority defined by IF-ADR-014; Reset does not create a competing identity system.

## 3. Decision

Reset adopts the following conceptual model:

```text
Reset capability
    how one local piece of state restores itself
        ↓
Resettable
    executable aggregate of local Reset capabilities
        ↓
ResetComposition
    semantic/structural set of Resettables
        ↓
ResetTarget
    Object / Direct or Stable
    Composition / Direct or Stable
    CurrentActivity | CurrentRoute
        ↓
owner-aware / membership-aware resolution
        ↓
ResetRegistry / ResetExecutor
```

Runtime determinism remains mandatory.

Runtime determinism is not normal authoring.

## 4. Reset capability

A Reset capability owns one local restoration responsibility.

Existing contracts such as `IResetParticipant`, `IUnityResettable` and concrete Transform / GameObject-state participants remain valid implementation foundations.

A capability must not decide:

- Route or Activity ownership;
- membership of unrelated objects;
- which trigger will invoke it;
- cross-scene lookup;
- global registry discovery.

The capability answers only how its owned local state is restored.

## 5. Resettable

`Resettable` is the normal Unity authoring unit for an independently resettable gameplay object.

A Resettable:

- aggregates local Reset capabilities;
- maps to one runtime Reset subject;
- receives runtime subject identity from the Framework by default;
- does not author content ownership;
- does not require a textual Reset ID in the normal path;
- does not use object names or hierarchy paths as runtime identity.

A Resettable forms a collection boundary.

Capability collection traverses the Resettable hierarchy deterministically. Capabilities may live on the Resettable GameObject or on descendant GameObjects inside that boundary. Descendant discovery must not cross into another nested Resettable; the nested Resettable owns its own capabilities and maps to a separate runtime subject.

One Resettable may therefore register multiple capabilities located on different GameObjects while still executing as one subject. This prevents duplicate registration/execution in nested gameplay composition and keeps capability placement separate from subject identity.

## 6. ResetComposition

`ResetComposition` defines a semantic or structural set of Resettables.

It is not a Reset subject and does not execute Reset itself.

Supported authoring modes may include:

- typed descendants;
- explicit typed members.

Additional provider/dynamic modes require a concrete use case and a later decision.

### 6.1 Hierarchy rule

Hierarchy may be used as an explicit authoring/composition boundary for typed collection.

Hierarchy must not become:

- runtime identity;
- lookup by GameObject name;
- lookup by hierarchy path;
- implicit global discovery.

Nested ResetComposition / Resettable boundaries must have deterministic collection rules.

RESET-035-D uses depth-first hierarchy order for `Descendants` and serialized list order for `ExplicitMembers`. A descendant `ResetComposition` excludes its entire GameObject subtree from the outer composition and resolves its own set. Nested `Resettable` components remain distinct members; their capability collection still stops at the nested Resettable boundary. Composition roots must be part of the owner transaction's materialized roots before their members can receive registered membership. Null explicit references are ignored with diagnostics; repeated typed references are deduplicated.

### 6.2 Membership policy

Reset membership may be supplied by a ResetComposition so repeated members do not each repeat the same semantic policy.

Local override is permitted only where a real exception exists.

The common path should minimize per-object infrastructure decisions.

RESET-035-D defines one precedence rule: an explicit local `Activity` or `Route` value on `Resettable` wins; local `FollowOwner` delegates to the containing composition's membership. A composition with `FollowOwner` leaves the member at its local policy. If a shared Resettable resolves to different membership values from multiple compositions after local precedence, owner-aware registration rejects the ambiguity before registering any subjects. Repeated inclusion with the same resolved membership remains one Resettable registration.

## 7. Ownership

`RuntimeContentOwner` remains the authority for content lifetime.

Ownership is derived from the origin that materialized or admitted the content.

Examples:

```text
Activity scene composition
    -> target Activity RuntimeContentOwner

Route scene composition
    -> target Route RuntimeContentOwner

Runtime materialization
    -> RuntimeMaterializationRequest.Owner
```

Normal Reset authoring must not choose ownership by declaring `Scope = Activity` or `Scope = Route`.

Registration must not derive ownership from whichever Route/Activity happens to be current during `OnEnable`.

## 8. Reset membership

Reset membership is independent from content ownership.

The default membership follows the content owner where that is semantically correct.

A composition may explicitly declare different membership when surviving content must reset with another gameplay boundary.

Canonical example:

```text
Visitors
  content owner      = Route
  reset membership   = Activity
```

This allows the Visitors to survive Activity Clear while still restoring selected state as part of an Activity-targeted Reset/Restart.

RESET-035-C defines the initial runtime policy as `ResetMembership.FollowOwner` (default), `Activity` or `Route`, authored on `Resettable` until composition-level policy exists. Route-owned content may opt into Activity membership. Activity-owned content cannot opt into Route membership because Activity ownership does not survive the Route boundary; that combination is rejected during owner-aware registration. Membership is recorded on the registered subject and never changes `RuntimeContentOwner`.

Terminology must not call this policy `ActivityCycle`, because Cycle Reset is a separate subsystem.

Preferred semantic vocabulary is:

```text
FollowOwner
Activity
Route
```

The exact public type names are implementation work, but the conceptual separation is normative.

## 9. ResetTarget

Normal request authoring targets gameplay intent rather than registry mechanics.

The semantic target kind is independent from its addressing mode:

```text
ResetTarget
├─ Object
│  └─ ResetObjectTarget
│     ├─ Direct -> Resettable
│     └─ Stable -> StableObjectReference
├─ Composition
│  └─ ResetCompositionTarget
│     ├─ Direct -> ResetComposition
│     └─ Stable -> StableObjectReference
CurrentActivity
CurrentRoute
```

`ResetTargetKind.Unknown` is a default/serialization sentinel. It is never valid
authoring and the resolver rejects it explicitly. `StableReference` is not a
semantic target kind; it is one addressing mode available to Object and
Composition.

### 9.1 Object

`Object/Direct` targets one Resettable through a typed reference. `Object/Stable`
uses the Object Entry stable addressing boundary when a direct reference is not
available.

### 9.2 Composition

`Composition/Direct` targets all Resettables resolved by one typed
ResetComposition reference. `Composition/Stable` resolves the current physical
GameObject occurrence through StableObjectBinding, requires ResetComposition on
that same GameObject and resolves its current members. It creates no composition
identity or registry.

### 9.3 CurrentActivity

Targets Resettables whose effective Reset membership includes the current Activity.

It does not mean only objects physically owned by Activity scenes.
It includes Activity-owned subjects in the current Activity occurrence and
Route-owned subjects in the current Route occurrence that opt into Activity
membership. It excludes Route membership.

### 9.4 CurrentRoute

Route is the parent Reset scope of its Activities (`Activity ⊂ Route`).
`CurrentRoute` targets registered subjects in the current Route occurrence with
effective Route membership, plus Route-owned subjects with effective Activity
membership and Activity-owned subjects belonging to the current Activity
context when one is active. Activity-owned subjects from other Activities or
Routes are excluded. If no Activity is active, the target still includes the
Route-owned Activity-membership subjects.

Thus `CurrentActivity` is the narrower selection; `CurrentRoute` is its
Route-scoped umbrella while remaining isolated to the current Route and its
current Activity context. Membership never changes the subject's owner.

### 9.5 Stable addressing

Stable addressing is opt-in for Object and Composition. It carries an
`ObjectEntryId` and an optional exact `RouteAsset` or `ActivityAsset` selector;
Framework derives stable identity and definition token from the typed asset.
It never stores RuntimeContentOwner, RouteId/ActivityId text or ResetSubjectId.
Resolution goes through the generic IF-ADR-014 StableObjectBinding boundary.
Object/Stable requires a currently registered Resettable on the resolved
physical GameObject and validates its registration owner against the binding.
Composition/Stable requires ResetComposition on that same GameObject and
resolves current members. Zero matches fail as unavailable; multiple matches
fail as ambiguous. The content transaction owns binding lifetime; Reset does
not.

## 10. Runtime identity

`ResetSubjectId`, `ResetParticipantId`, registration handles and equivalent deterministic evidence remain valid runtime mechanisms.

They are internal/runtime concerns by default.

Normal authoring must not require consumers to invent textual identifiers solely so Reset can execute.

Under the Resettable path, collected capabilities receive deterministic runtime participant identity from the Resettable registration boundary; authored participant IDs are not required for normal Resettable authoring. The independent UnityResetSubjectAdapter path retains its authored participant descriptor/identity contract.

Stable authored identity remains valid only when identity itself is a real cross-boundary gameplay/integration requirement.

## 11. Registration lifecycle

Scene-authored Reset registration moves away from self-registration based on `OnEnable`, `Start`, retry loops and current-owner lookup.

Route and Activity content must be registered at owner-aware transaction boundaries where:

- the target RuntimeContentOwner is known;
- the materialized roots are known;
- registration can be prepared before commit;
- failure can roll back target registrations;
- release can remove registrations by owner.

The existing Pause Activity binding transaction is the architectural precedent.

`ResetProductBindingSceneLifecycleParticipant` may remain useful for product binding that does not establish ownership, but generic scene availability is not the authority for assigning Reset ownership.

## 12. Runtime-created content

Framework-owned runtime materialization must provide Reset registration with the `RuntimeMaterializationRequest.Owner`.

The Reset integration must operate on the materialized instance through typed Resettable discovery.

Direct arbitrary `Object.Instantiate` outside an owner-aware Framework composition boundary does not receive an implicit owner.

A future explicit transient/runtime ownership API requires its own justified contract; it is not introduced by this ADR.

## 13. Execution and determinism

`ResetRegistry` and `ResetExecutor` remain the initial execution foundation.

The new authoring path resolves semantic targets to runtime subjects and feeds the existing execution core.

The runtime must define deterministic ordering without requiring designers to author arbitrary IDs as ordering tie-breakers.

Required ordering work includes:

- deterministic ordering between resolved subjects;
- deterministic ordering of capabilities within a Resettable;
- stable behavior across repeated materialization of equivalent content.

The exact ordering algorithm belongs to implementation design and QA, provided it does not use GameObject names as authority.

## 14. Object Reset product surface

The distinction between an individual Object Reset product and an Object Reset Group product is no longer normative.

A group is a ResetComposition target, not a different kind of Reset operation.

`ResetRequestTrigger` is the single Reset request surface and expresses the
semantic target and its selected addressing mode. `ObjectResetTrigger`,
`ObjectResetGroupTrigger` and `ResetSelectionConfig` are removed; no compatibility
path or migration adapter remains.

## 15. Activity Restart

Activity Restart remains lifecycle orchestration.

It must not become an independent Reset engine.

The sequence remains conceptually:

```text
resolve restart target
    -> execute required surviving-state Reset
    -> Activity Clear
    -> Activity Reenter
```

Activity-owned scene content is normally physically released/recreated by Clear/Reenter.

Reset before Clear is therefore primarily meaningful for state that survives that lifecycle boundary, including Route-owned content with Activity Reset membership.

The final implementation must prevent duplicate restoration when content will already be recreated by lifecycle.

## 16. Cycle Reset

Cycle Reset remains a separate lifecycle-system contract.

`ICycleResetParticipant`, Cycle Reset planning/execution and Cycle Reset results are not replaced by Resettable.

Gameplay Resettable objects do not automatically become `ICycleResetParticipant`.

If a lifecycle participant intentionally invokes a ResetTarget in the future, that is explicit orchestration and requires a concrete use case.

The IF-ADR-034 draft must not proceed with a per-GameObject Cycle Reset authoring model that recreates the bookkeeping corrected by this ADR.

## 17. Editor / Inspector contract

This ADR refines IF-ADR-010 for Reset.

Normal Reset authoring should expose gameplay intent:

```text
Resettable
  local capabilities

ResetComposition
  member mode
  membership policy when exceptional

Reset Request
  target
```

Normal authoring should not require:

- runtime handles;
- generated subject IDs;
- generated participant IDs;
- owner occurrence tokens;
- registry binding details;
- repeated lifecycle scope that can be derived.
- copied stable identity text across declaration and request;
- RouteId/ActivityId text when an exact typed owner asset can be referenced.

Those remain Advanced / Diagnostics evidence where useful.

## 18. Migration strategy

Migration is parallel and incremental.

The existing Reset execution and owner-aware registration runtimes remain
operational. `ResetRequestTrigger` is the only Reset request authoring path.

### RESET-035-A — target model and internal resolution

Introduce the ResetTarget domain model and an internal resolver that can produce the existing execution input without changing ResetExecutor behavior.

No legacy removal.

### RESET-035-B — Resettable and owner-aware registration

Introduce Resettable and transaction-aware registration for Route/Activity scene content.

Requirements:

- runtime-generated subject identity;
- generated/internal participant identity where appropriate;
- nested-boundary-safe capability collection;
- explicit rejection of mixed legacy/new registration on the same object;
- rollback before transaction commit;
- release by owner.

**Tracking (2026-10-01):** RESET-035-B is closed and validated, including QAFramework lifecycle integration and Unity compile/import validation.

### RESET-035-C — Reset membership

Separate content ownership from Reset membership.

Prove at minimum:

- FollowOwner default;
- Route-owned Resettable with Activity membership;
- CurrentActivity includes that Resettable;
- CurrentRoute follows the defined membership contract;
- no membership leakage across unrelated owners.

**Tracking (2026-10-02):** RESET-035-C membership metadata and CurrentActivity/CurrentRoute resolution are implemented. The CurrentRoute umbrella contract is covered by the focused membership and resolver regressions; Unity validation remains pending under repository policy.

### RESET-035-D — ResetComposition

Introduce descendant and explicit-member composition.

Prove:

- composition is not registered as a subject;
- individual Reset remains possible;
- composition Reset executes each member once;
- nested boundaries do not duplicate capabilities.

**Tracking (2026-10-01):** RESET-035-D is closed and validated per user direction. Descendants/ExplicitMembers authoring, deterministic typed resolution, composition membership precedence and owner-registration preparation integration are implemented with focused Editor contracts.

### RESET-035-E — request surface

Introduce the ResetTarget-based request surface and adapt Activity Restart to
semantic targeting. Semantic target kind remains independent from addressing
mode.

**Tracking (2026-10-02):** `ResetRequestTrigger` resolves Object, Composition, CurrentActivity and CurrentRoute through `ResetTargetResolver` into the existing `ResetSelectionResolution` and `ResetExecutor` path. Object and Composition require registered members in the current Activity/Route owner context and never register implicitly. Activity Restart defaults to CurrentActivity but filters its pre-clear selection to current Route-owned subjects; CurrentActivity additionally requires effective Activity membership. This excludes Activity-owned content recreated by Clear/Reenter and avoids duplicate restoration. CurrentRoute applies the explicit Route umbrella contract in §9.4. Focused Edit Mode contracts were added; Unity compile/import, Edit Mode execution and Activity Restart lifecycle integration QA remain pending.

### RESET-035-F — stable cross-boundary targeting

Consume the accepted `StableObjectBinding` authority from IF-ADR-014 and
implement stable addressing for Object and Composition. The owner-aware Route/Activity content
transaction creates the binding; rollback/release removes it with that owner.
`StableObjectReference` carries an `ObjectEntryId` and accepts an optional typed
`RouteAsset`/`ActivityAsset` owner selector. Stable identity projections are
derived by Framework; no owner identity text is authored. It resolves to
exactly one current physical occurrence. Object/Stable then resolves the
registered Resettable; Composition/Stable resolves ResetComposition and its
current members. Stable addressing remains exceptional: prefer direct
references when suitable.

**Tracking (2026-10-01):** `StableObjectBindingRegistry` is host-owned and receives materialized Route/Activity roots from their existing transactions. Prepared bindings are not resolvable until the content owner commits; rollback/release removes them with that owner. It rejects ambiguity and never stores runtime Reset subject identity in the reference. Stable Object/Composition addressing consumes that authority and validates the required domain component on the resolved physical GameObject. Edit Mode contracts cover commit visibility, resolve/missing/ambiguity/owner separation/unload-reload/current registration; Unity execution remains subject to repository validation policy. Real application/sample use is validated separately.

**Authoring refinement:** Object Entry ID is serialized on the declaration and repeated only when stable addressing crosses an authoring boundary. The Inspector exposes Object/Direct, Object/Stable, Composition/Direct, Composition/Stable, CurrentActivity and CurrentRoute; Current* has no payload. Switching kind or reference mode clears inactive serialized payloads. Route/Activity scope and owner derive only from the admission transaction. No identity container or dual path exists.

### RESET-035-G — runtime materialization

Integrate Resettable registration with owner-aware runtime materialization.

Prove owner propagation and release.

### RESET-035-H — legacy migration and removal

After QA and consumer validation:

- migrate planet-devourer;
- deprecate/remove UnityResetSubjectAdapter normal authoring;
- update Reset-Usage.

Removal of the remaining registration authoring is not allowed before its
equivalent contracts are validated.

## 19. QA obligations

Existing behavioral Reset QA should be preserved where it tests runtime behavior rather than legacy structure.

Required new evidence includes:

- Resettable returns perturbed state to baseline;
- runtime subject identity is unique per prefab instance;
- nested Resettable/Composition boundaries execute capabilities once;
- deterministic subject ordering;
- owner-aware A -> B transition never registers target content under previous owner;
- pre-commit rollback removes target registrations;
- additive Route-owned and Activity-owned content receives correct owner;
- Route-owned content with Activity membership participates in CurrentActivity;
- Activity Restart does not duplicate restoration of content recreated by Clear/Reenter;
- runtime materialized Resettable receives request.Owner;
- ownerless arbitrary runtime instantiation is rejected or remains unregistered explicitly;
- CurrentActivity does not leak unrelated Route membership;
- Cycle Reset does not automatically execute Resettable;
- cross-boundary stable Object/Composition addressing resolves only within its explicit identity contract.

QA must distinguish behavioral contracts from legacy authoring structure.

## 20. Consequences

### Positive

- normal authoring scales with meaningful composition boundaries instead of infrastructure bookkeeping;
- content ownership has one authority;
- Reset membership becomes explicit only where it differs from ownership;
- runtime determinism is preserved;
- existing execution investment is retained;
- prefabs can be instantiated without authored Reset ID collisions;
- individual, composition and lifecycle-level Reset share one targeting model;
- Cycle Reset remains cleanly separated.

### Tradeoffs

- registration moves deeper into Route/Activity transaction integration;
- ownership and Reset membership become separate concepts that tooling/documentation must present clearly;
- cross-scene specific targeting still requires stable identity;
- existing UnityResetSubjectAdapter registration authoring remains for separate migration work;
- ordering needs an explicit runtime contract rather than relying on authored ID text.

### Risks

- incorrect membership inheritance through nested compositions;
- cross-boundary identity becoming a second global lookup system;
- transaction rollback gaps;
- runtime materialization bypassing Reset registration;
- Activity Restart resetting state that lifecycle would immediately recreate.

RESET-035-H remains pending for consumer migration and the separate
UnityResetSubjectAdapter authoring path; ObjectResetTrigger,
ObjectResetGroupTrigger and ResetSelectionConfig have been removed in this cut.

## 21. Supersession and reconciled boundaries

This ADR is the normative authority when older Reset documentation conflicts with it.

Reconciled authority and remaining requirements:

- **IF-ADR-005:** supersede explicit per-subject scope authoring, textual normal Reset identity and ResetSelectionConfig-as-product assumptions; document ownership vs membership and Activity Restart surviving-state semantics.
- **IF-ADR-014 (reconciled 2026-10-01):** `StableObjectBinding` is the generic owner-transaction projection from `ObjectEntryId` to a live physical occurrence; Reset consumes it without owning its lifecycle.
- **IF-ADR-001:** add owner-aware transaction registration and rollback invariant; current state is not ownership authority for target content during transition.
- **IF-ADR-002:** reconcile typed hierarchy/composition as authoring boundary without making hierarchy runtime identity.
- **IF-ADR-010:** classify Resettable / ResetComposition / ResetTarget as the intended Reset product surface; generated runtime identity belongs in Advanced/Diagnostics.
- **IF-ADR-034 draft:** do not advance the current per-GameObject Cycle Reset registration proposal; Cycle Reset remains separate and must be reconsidered after RESET-035 boundaries exist.
- **Reset-Usage.md:** migrate from authored subject ID/scope/group-list instructions to Resettable / ResetComposition / ResetTarget.

## 22. Non-goals

This ADR does not introduce:

- a global Reset manager;
- service locator;
- global scene search;
- name/path-based runtime identity;
- automatic gameplay intent inference;
- Save/Load semantics;
- network replication semantics;
- Addressables architecture;
- pooling architecture;
- automatic conversion of Resettable to Cycle Reset participant.

## 23. Normative summary

```text
Capability restores local state.

Resettable is the independently executable gameplay Reset unit.

ResetComposition resolves sets of Resettables.
It is not a Reset subject.

RuntimeContentOwner owns lifetime.
Ownership comes from composition/materialization origin.

Reset membership answers which semantic Reset targets include a Resettable.
Ownership and membership are independent.

ResetTarget expresses semantic request intent and addressing:
  Object / Direct or Stable
  Composition / Direct or Stable
  CurrentActivity
  CurrentRoute

Runtime IDs, handles and registry mechanics remain internal by default.

Stable identity is opt-in for real cross-boundary addressing.

Route/Activity registration is owner-aware and transaction-aware.
It does not infer ownership from current state during OnEnable.

Cycle Reset remains a separate lifecycle-system contract.

ResetRequestTrigger is the sole request surface and feeds the existing Reset execution core.
```


## 24. Consumer proof closure — 2026-10-02

The IF-ADR-035 consumer authoring/proving slice is closed in `planet-devourer` for the current implementation.

Integrated and manually validated scenarios:

```text
Object / Direct
Object / Stable
Composition / Direct - Descendants
Composition / Stable - Explicit Members
CurrentActivity
CurrentRoute
Activity Restart
Multiple Participants
```

The Multiple Participants proof uses one root `Resettable` with capabilities collected from two descendant GameObjects: one Transform capability and one GameObject active-state capability. The Resettable diagnostic display name remains free to preserve the original authored object name; it is not runtime identity.

This closure records consumer proof of the accepted architecture. It does not change the normative model and does not claim UPM package-import/release validation.
