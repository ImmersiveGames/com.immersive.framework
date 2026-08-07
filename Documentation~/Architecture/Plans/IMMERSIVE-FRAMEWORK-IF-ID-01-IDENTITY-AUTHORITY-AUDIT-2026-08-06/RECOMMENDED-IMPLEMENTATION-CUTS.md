# ADR-014 — Recommended Implementation Cuts

**Baseline:** `com.immersive.framework@9ed698e55b48077c54be5056c6951b7e52dac51b`  
**Rule:** package first, QA second, FIRSTGAME proof after the official contract is stable.

---

## IF-ID-02 — Identity Vocabulary and QA Baseline

**Type:** technical / contracts / QA foundation

### Objective

Make each identity dimension explicit without changing production lifecycle semantics.

### Scope

- Add or rename stable-ID comparison vocabulary to `HasSameStableId` or equivalent.
- Mark `HasSameIdentity` obsolete, internal-migration-only or otherwise unambiguous.
- Document direct reference comparison as authored-definition equality.
- Add focused QA fixtures for two distinct Route assets sharing one ID and two distinct Activity assets sharing one ID.
- Capture current behavior where needed to prevent accidental unreviewed changes.
- Add target-semantic assertions prepared for IF-ID-03 and IF-ID-04.

### Out of scope

- Route lifecycle behavior changes.
- Activity lifecycle behavior changes.
- Runtime-content owner redesign.
- Application catalog.
- FIRSTGAME work.

### Files likely affected

```text
Runtime/Authoring/RouteAsset.cs
Runtime/Authoring/ActivityAsset.cs
Documentation~/Architecture/ADRs/IF-ADR-014-...
QAFramework identity regression area
```

### Product surface affected

Advanced/Debug terminology only. No normal authoring behavior should change.

### Expected QA

```text
same reference + same stable ID
different references + different stable IDs
different references + same stable ID
rename preserves stable ID
move preserves stable ID
explicit regeneration changes stable ID
```

### Technical acceptance

- ambiguous identity vocabulary is no longer used in new code;
- no runtime behavior changes accidentally;
- compatibility is explicit and temporary;
- package and QA compile;
- tests do not depend on log parsing.

### Product acceptance

- terminology clearly distinguishes authored definition from stable external ID.

### Architectural gain

Establishes a shared language before runtime migration.

### Usability gain

Diagnostics stop calling multiple concepts simply “identity”.

### Suggested commit message

```text
refactor(identity): clarify stable-id equality and add migration QA baseline
```

---

## IF-ID-03 — Route Reference Authority

**Type:** technical runtime

### Objective

Make the exact `RouteAsset` reference authoritative for Route target equality.

### Scope

- `RouteLifecycleRuntime.IsRouteActive`.
- Route replacement and idempotence.
- Route entered/exited publication conditions.
- `FrameworkRuntimeHost` Route synchronization.
- Route context rollover consumed by `ActivityFlowRuntime`.
- Route side of `ActivityEntryReadinessActiveOperation` ownership.
- Explicit compatibility boundary for current runtime-content owner behavior.

### Out of scope

- final runtime-content owner redesign;
- Activity equality migration;
- validation UX;
- catalog/persistence.

### Expected QA

```text
same Route reference is idempotent
different Route reference with different ID transitions
different Route reference with same ID still transitions or is rejected explicitly by validation boundary
RouteAuthorityReplaced supersedes only the exact prior operation
enter/exit facts remain correct
previous Route scope cleanup remains deterministic
```

### Technical acceptance

- no Route target equality uses stable ID when both references are available;
- lifecycle, host state and ActivityFlow Route context agree on one authority;
- no silent fallback;
- current owner compatibility is documented and tested;
- cleanup remains deterministic.

### Product acceptance

No visible regression in Route requests or loading/reveal behavior.

### Suggested commit message

```text
refactor(route): use authored definition reference as runtime target authority
```

---

## IF-ID-04 — Activity Reference Authority

**Type:** technical runtime

### Objective

Make the exact `ActivityAsset` reference authoritative for Activity target equality and use occurrence identity for one concrete execution.

### Scope

- `ActivityFlowRuntime.IsActivityActive`.
- transaction setup and previous-Activity finalization.
- restart/reentry comparisons.
- Activity side of `ActivityEntryReadinessActiveOperation`.
- readiness invalidation/supersession correlation.
- Activity enter/exit facts and state updates.

### Out of scope

- final owner redesign;
- editor validation;
- application catalog.

### Expected QA

```text
same Activity reference is idempotent where policy requires
different Activity refs with same ID are not the same definition
previous Activity finalizes correctly
restart and reentry preserve occurrence rules
stale occurrence cannot complete current wait
colliding ID cannot cancel or supersede another Activity occurrence
```

### Technical acceptance

- all definition comparisons use references;
- all execution comparisons use occurrence/revision/transaction evidence;
- no stable ID substitutes for occurrence;
- cleanup and readiness remain explicit and diagnostic.

### Suggested commit message

```text
refactor(activity): align lifecycle and readiness with reference authority
```

---

## IF-ID-05 — Runtime Ownership Boundary

**Type:** technical architecture / runtime

### Objective

Separate stable definition identity from operational acquisition/release authority.

### Design questions to resolve first

- Does one Route entry require a unique owner occurrence?
- Does one Activity restart create a new owner or reuse an existing scope?
- Can two occurrences of the same Activity definition coexist?
- Are acquisition handles already sufficient to prove release authority?
- Which diagnostics must retain stable IDs?
- Which ownership keys cross persistence or external boundaries, if any?

### Candidate model

```text
Owner domain
+ stable definition ID for boundary evidence
+ runtime scope instance / occurrence token
+ explicit acquisition handle
```

This is a candidate, not a frozen implementation requirement.

### Scope

- `RuntimeContentOwner` semantics or its construction inputs.
- Route and Activity owner creation.
- instance/scope IDs.
- release and cleanup paths.
- reset/restart/reentry behavior.

### Expected QA

```text
two different refs with same ID never share release authority
same definition reentry follows documented ownership policy
restart follows documented ownership policy
out-of-order release cannot free another occurrence
stale handle is rejected explicitly
Route replacement cleanup is deterministic
Activity finalization cleanup is deterministic
```

### Technical acceptance

- operational owner identity encodes the required lifetime;
- stable ID remains boundary evidence where appropriate;
- no global registry or implicit lookup;
- release failures are explicit;
- ownership diagnostics include definition and occurrence evidence.

### Suggested commit message

```text
refactor(runtime-content): separate stable identity from scoped ownership
```

---

## IF-ID-06 — Validation Scope and Identity Product UX

**Type:** UX/product + editor tooling

### Objective

Make identity problems visible, correctly scoped and explicitly remediable.

### Scope

#### Definition-local validation

- missing/invalid ID;
- collision involving selected asset;
- directly owned configuration.

#### Game Application validation

- uniqueness within reachable resolvable graph;
- exactly-one resolution requirement;
- deep links to all colliding definitions.

#### Project identity audit

- all project collisions;
- unused/experimental/archive context;
- clearly marked as project-level evidence.

#### Product actions

- `Regenerate Stable ID...`;
- confirmation and before/after evidence;
- Undo where safe;
- validation refresh;
- Advanced/Debug identity panel.

### Out of scope

- automatic regeneration;
- global runtime catalog;
- save migration;
- mandatory `Duplicate as New` command.

### Expected QA

```text
selected asset collision is local blocking issue
unrelated project collision is not local blocking issue
application graph collision is blocking
project audit reports all collisions
automatic import/rename/move does not change ID
explicit regeneration changes only requested asset
```

### Product acceptance

A designer can identify the conflicting assets, understand the scope and repair a duplicated definition without editing serialized text manually.

### Suggested commit message

```text
feat(identity): separate validation scopes and add explicit regeneration UX
```

---

## IF-ID-07 — Application-Scoped Stable-ID Resolver

**Type:** technical integration

### Trigger condition

Implement only when save/progression or another real boundary must resolve stable IDs to authored definitions.

### Scope

- explicit Game Application catalog or deterministic graph-derived resolver;
- typed Route and Activity resolution;
- immutable lookup evidence;
- explicit `Resolved`, `NotFound`, `Ambiguous`, `Invalid` and scope errors.

### Rejected mechanisms

- static global registry;
- service locator;
- global project scan in runtime;
- filename/path/display-name fallback;
- arbitrary first match.

### Expected QA

```text
one match resolves
zero matches fails explicitly
two matches fail as ambiguous
outside application scope fails explicitly
catalog rebuild is deterministic
```

### Suggested commit message

```text
feat(identity): add application-scoped stable-id resolution boundary
```

---

## IF-ID-08 — FIRSTGAME Identity Workflow Proof

**Type:** real consumer integration / product proof

### Objective

Prove the official package workflow in a real game.

### Flow

```text
create valid Route or Activity
duplicate asset
observe collision
confirm references remain distinct
attempt application integration
receive scoped blocking diagnostic
regenerate copied stable ID explicitly
validate again
enter both definitions successfully
confirm ownership and cleanup
rename and move asset
confirm stable ID remains unchanged
```

### Out of scope

- consumer-owned identity framework;
- global event bus;
- local resolver that becomes permanent;
- direct mutation of package internals.

### Product acceptance

A user unfamiliar with internal contracts can understand and repair identity collisions through the official package surface.

### Suggested commit message

```text
test(firstgame): prove authored identity duplication and remediation workflow
```
