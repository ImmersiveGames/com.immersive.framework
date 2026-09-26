# IF-ADR-035 — Reset Composition, Ownership, Membership and Targeting

Status: **Accepted**
Last updated: **2026-09-26**
Normative classification: **Corrective Reset architecture authority**
Supersedes: **the Reset authoring, explicit scope authoring, explicit subject-selection and Unity registration assumptions of IF-ADR-005 where they conflict with this ADR**
Reopens / requires reconciliation: **IF-ADR-001, IF-ADR-002, IF-ADR-005, IF-ADR-010, IF-ADR-014**
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

This decision must reuse or reconcile the stable-identity authority of IF-ADR-014 rather than create a competing Reset-only identity system.

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
    Object | Composition | CurrentActivity | CurrentRoute | StableReference
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

Descendant discovery must not cross into another nested Resettable when collecting local capabilities.

This prevents duplicate registration/execution in nested gameplay composition.

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

### 6.2 Membership policy

Reset membership may be supplied by a ResetComposition so repeated members do not each repeat the same semantic policy.

Local override is permitted only where a real exception exists.

The common path should minimize per-object infrastructure decisions.

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

The target model is:

```text
Object
Composition
CurrentActivity
CurrentRoute
StableReference
```

### 9.1 Object

Targets one Resettable through a direct typed reference when the authoring boundary permits it.

### 9.2 Composition

Targets all Resettables resolved by one ResetComposition.

### 9.3 CurrentActivity

Targets Resettables whose effective Reset membership includes the current Activity.

It does not mean only objects physically owned by Activity scenes.

### 9.4 CurrentRoute

Targets Resettables whose effective Reset membership includes the current Route according to the final membership contract.

### 9.5 StableReference

Targets a specific Resettable across a serialization/content boundary where a direct Unity reference is not available.

StableReference is opt-in.

It must be reconciled with IF-ADR-014 stable identity rather than creating a parallel identity authority.

## 10. Runtime identity

`ResetSubjectId`, `ResetParticipantId`, registration handles and equivalent deterministic evidence remain valid runtime mechanisms.

They are internal/runtime concerns by default.

Normal authoring must not require consumers to invent textual identifiers solely so Reset can execute.

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

The desired product direction is one request surface capable of expressing the ResetTarget variants.

Legacy `ObjectResetTrigger` and `ObjectResetGroupTrigger` remain during migration and may be deprecated after the new path is technically validated and the consumer sample is migrated.

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

Those remain Advanced / Diagnostics evidence where useful.

## 18. Migration strategy

Migration is parallel and incremental.

The existing Reset runtime remains operational while the new authoring path is proven.

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

### RESET-035-C — Reset membership

Separate content ownership from Reset membership.

Prove at minimum:

- FollowOwner default;
- Route-owned Resettable with Activity membership;
- CurrentActivity includes that Resettable;
- CurrentRoute follows the defined membership contract;
- no membership leakage across unrelated owners.

### RESET-035-D — ResetComposition

Introduce descendant and explicit-member composition.

Prove:

- composition is not registered as a subject;
- individual Reset remains possible;
- composition Reset executes each member once;
- nested boundaries do not duplicate capabilities.

### RESET-035-E — request surface

Introduce the ResetTarget-based request surface.

Adapt Activity Restart to semantic targeting.

Keep legacy triggers during migration.

### RESET-035-F — stable cross-boundary targeting

Reconcile IF-ADR-014 and implement StableReference only after the identity authority is explicit.

Migrate the real Route Primary -> RouteContent sample without requiring universal Reset IDs.

### RESET-035-G — runtime materialization

Integrate Resettable registration with owner-aware runtime materialization.

Prove owner propagation and release.

### RESET-035-H — legacy migration and removal

After QA and consumer validation:

- migrate planet-devourer;
- deprecate/remove UnityResetSubjectAdapter normal authoring;
- deprecate/remove ResetSubjectReference textual normal selection;
- deprecate/remove explicit ResetSelectionConfig product authoring where replaced;
- converge ObjectResetTrigger/ObjectResetGroupTrigger as justified;
- update Reset-Usage.

Removal is not allowed before equivalent contracts are validated.

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
- cross-boundary StableReference resolves only within its explicit identity contract.

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
- migration temporarily supports both legacy and corrective authoring paths;
- ordering needs an explicit runtime contract rather than relying on authored ID text.

### Risks

- accidental double registration while legacy and new authoring coexist;
- incorrect membership inheritance through nested compositions;
- cross-boundary identity becoming a second global lookup system;
- transaction rollback gaps;
- runtime materialization bypassing Reset registration;
- Activity Restart resetting state that lifecycle would immediately recreate.

Each risk requires explicit QA before legacy removal.

## 21. Supersession and reconciliation

This ADR is the normative authority when older Reset documentation conflicts with it.

Required reconciliation:

- **IF-ADR-005:** supersede explicit per-subject scope authoring, textual normal Reset identity and ResetSelectionConfig-as-product assumptions; document ownership vs membership and Activity Restart surviving-state semantics.
- **IF-ADR-014:** reopen/reconcile stable identity for cross-boundary Reset targeting.
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

ResetTarget expresses request intent:
  Object
  Composition
  CurrentActivity
  CurrentRoute
  StableReference

Runtime IDs, handles and registry mechanics remain internal by default.

Stable identity is opt-in for real cross-boundary addressing.

Route/Activity registration is owner-aware and transaction-aware.
It does not infer ownership from current state during OnEnable.

Cycle Reset remains a separate lifecycle-system contract.

Migration preserves the existing Reset execution core until the new authoring path is proven.
```
