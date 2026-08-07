# Immersive Framework — IF-ID-01 Identity Authority Audit

**Date:** 2026-08-06  
**ADR:** IF-ADR-014 — Authored Definition and Stable Identity Authority  
**Audit type:** Technical architecture / static source review  
**Package baseline:** `ImmersiveGames/com.immersive.framework@9ed698e55b48077c54be5056c6951b7e52dac51b`  
**QA baseline:** `rinnocenti/QAFramework@0521d1f1804dff2806e06b1e095d47023a062b9e`  
**FIRSTGAME baseline:** `ImmersiveGames/planet-devourer@e551643ce1b154fdb2744f97b039b4ce73bc6bf5`

---

## 1. Executive verdict

The direction defined by IF-ADR-014 is valid and necessary.

The package is not starting from zero. It already has several foundations required by the target model:

- authoring and request surfaces carry typed `RouteAsset` and `ActivityAsset` references;
- runtime state objects retain the exact authored definition reference;
- `RouteId` and `ActivityId` are typed stable identity primitives;
- Activity readiness occurrences already combine the exact Activity reference with an occurrence sequence;
- explicit stable-ID validation exists;
- display names are generally used for presentation and diagnostics rather than target selection.

The critical problem is that runtime authority is split after those references enter the package:

```text
Authoring/request
  typed asset reference

Lifecycle/idempotence/cleanup
  frequently reduced to stable-ID equality

Readiness occurrence
  exact reference + sequence

Runtime content ownership
  stable ID as the complete operational owner key
```

Consequently, two different authored assets carrying the same stable ID can be treated as the same active definition in some modules while remaining different objects in others.

This can affect:

- `already active` and request idempotence;
- Route and Activity replacement;
- previous-context finalization;
- enter/exit fact publication;
- Route scope-root cleanup;
- readiness cancellation and supersession;
- runtime-content ownership and release.

### Audit conclusion

**IF-ADR-014 remains approximately 25% complete.**

This audit closes the discovery portion of the first migration cut, but it does not change runtime behavior. The next safe cut is vocabulary clarification plus focused QA baselines. Route, Activity and ownership migrations must remain separate cuts.

---

## 2. Authority model used by this audit

Every inspected use was classified using the following authority categories.

| Category | Question answered | Required authority |
|---|---|---|
| Definition reference | Which exact authored definition is selected or active? | Typed `RouteAsset` / `ActivityAsset` reference |
| Stable boundary identity | How is a definition represented where a Unity reference cannot cross? | Typed `RouteId` / `ActivityId` |
| Runtime occurrence | Which execution of a definition is being observed? | Reference + occurrence/revision/handle |
| Operational ownership | Which scoped runtime acquisition may release a resource? | Explicit scoped owner identity; decision must include lifetime semantics |
| Presentation | What human-readable text is displayed? | Display name / asset name, never functional identity |
| Ambiguous | The code name or behavior does not reveal which identity dimension is intended | Must be renamed or redesigned |

The target rules are:

```text
Reference equality
  controls in-process definition equality and lifecycle target equality.

Stable-ID equality
  controls explicit boundary identity and catalog resolution.

Occurrence/revision/handle
  controls one concrete runtime execution.

Display name
  controls presentation only.
```

---

## 3. Scope and method

### Inspected package areas

- `Runtime/Authoring`
- `Runtime/ApplicationLifecycle`
- `Runtime/GameFlow`
- `Runtime/RouteLifecycle`
- `Runtime/ActivityFlow`
- `Runtime/RuntimeContent`
- `Runtime/ProgressionSave`
- `Editor/Authoring`
- current IF-ADR-014

### Inspected consumer evidence

- current QAFramework tree after the `Clear QA` reorganization;
- latest FIRSTGAME Demo03 commit available during the audit.

### Method

1. Inspect canonical identity types and authored definitions.
2. Trace typed Route/Activity requests into runtime ports.
3. Trace active-target, transition, readiness and finalization comparisons.
4. Trace owner-key construction and owner equality.
5. Inspect local, application-adjacent and project-wide identity validation surfaces.
6. Classify each use by intended authority.
7. Identify QA and FIRSTGAME proof gaps.

### Limitations

- This is a static source audit. Unity import, compilation and Play Mode were not executed.
- GitHub code search returned incomplete results during the audit. Canonical runtime and editor paths were inspected directly, but the report must not be treated as proof that no additional incidental use exists.
- No repository was modified.

---

## 4. Critical findings

## IF-ID-A01 — `HasSameIdentity` is an ambiguous stable-ID comparison

**Severity:** Critical  
**Files:**

- `Runtime/Authoring/RouteAsset.cs`
- `Runtime/Authoring/ActivityAsset.cs`

Both assets expose an API named `HasSameIdentity`, but the implementation compares only `RouteId` or `ActivityId`.

This name hides which identity dimension is being compared. Callers can reasonably read it as “same authored definition” even though it means “same stable ID”.

### Required direction

- introduce an explicit vocabulary such as `HasSameStableId`;
- deprecate or remove `HasSameIdentity` after migration;
- use direct reference comparison where definition equality is intended;
- do not add a generic replacement named only `Identity`.

### Immediate QA requirement

Prove that:

```text
same reference + same ID
  same authored definition

different references + different IDs
  different authored definitions

different references + same ID
  different authored definitions + stable-ID collision
```

---

## IF-ID-A02 — Route active-target equality uses stable ID

**Severity:** Critical  
**File:** `Runtime/RouteLifecycle/RouteLifecycleRuntime.cs`  
**Primary use:** `IsRouteActive`

The active Route decision delegates to `CurrentRoute.HasSameIdentity(route)`.

### Risk

Two distinct Route assets with the same ID can produce an `already active` result even when their scenes, Activities, policies or content differ.

### Target authority

Exact typed `RouteAsset` reference.

### Required cut

IF-ID-03 — Route Reference Authority.

---

## IF-ID-A03 — Route events and scope cleanup also use stable-ID equality

**Severity:** Critical  
**File:** `Runtime/RouteLifecycle/RouteLifecycleRuntime.cs`

Stable-ID equality participates in decisions about:

- whether Route enter/exit events are published;
- whether the previous Route scope root is removed;
- whether a replacement is treated as the same Route.

### Risk

Changing only `IsRouteActive` would leave event publication and cleanup using a different authority. That would create a split-brain migration.

### Target authority

- authored definition comparison: Route reference;
- runtime cleanup: explicit runtime scope/owner semantics;
- diagnostics: stable ID may remain as evidence.

### Required cut

Keep all Route lifecycle comparison changes in one coordinated cut, while preserving ownership behavior behind an explicit compatibility boundary until IF-ID-05.

---

## IF-ID-A04 — Activity active-target equality uses stable ID

**Severity:** Critical  
**File:** `Runtime/ActivityFlow/ActivityFlowRuntime.cs`  
**Primary use:** `IsActivityActive`

The active Activity decision uses `Activity.HasSameIdentity(activity)`.

### Risk

Different Activity assets with the same ID can be treated as the same active Activity, suppressing a real transition or restart path.

### Target authority

Exact typed `ActivityAsset` reference.

### Required cut

IF-ID-04 — Activity Reference Authority.

---

## IF-ID-A05 — Activity transactions currently mix reference and stable-ID authority

**Severity:** Critical  
**File:** `Runtime/ActivityFlow/ActivityFlowRuntime.Transaction.cs`

The transaction already uses `ReferenceEquals` in important places, including transaction setup and enter/exit facts. However, previous-Activity finalization still uses `HasSameIdentity`.

### Risk

A transaction can classify two assets as different during one stage and the same during finalization. This can suppress release/finalization for the wrong reason.

### Target authority

- definition equality: exact Activity reference;
- runtime execution: Activity occurrence or transaction identity;
- release: explicit owner/handle semantics.

### Required cut

Migrate all Activity transaction comparison points together in IF-ID-04. Do not patch only the active-target check.

---

## IF-ID-A06 — Route and Activity runtime-content owners collapse to stable ID

**Severity:** Critical  
**Files:**

- `Runtime/RouteLifecycle/RouteLifecycleRuntime.cs`
- `Runtime/ActivityFlow/ActivityFlowRuntime.cs`
- `Runtime/RuntimeContent/RuntimeContentOwner.cs`

Route and Activity owners are built from their stable IDs, and owner equality is based on owner scope plus the framework identity key.

### Risk

Two different assets with the same ID can produce the same operational owner. This may cause:

- one definition releasing another definition's resources;
- acquisition deduplication across distinct definitions;
- suppressed cleanup;
- incorrect ownership diagnostics;
- reentry/restart collisions when occurrence is not represented.

### Important distinction

`RuntimeContentOwner` is internally coherent as a stable key type. The defect is not that stable IDs exist. The risk is treating stable definition ID as the complete operational owner identity.

### Pending design decision

Determine whether the final owner key requires:

```text
stable definition ID
+ runtime scope instance or occurrence
+ explicit owner domain
```

or whether existing acquisition handles already provide the required occurrence authority.

### Required cut

IF-ID-05 — Ownership Boundary Redesign. Do not combine this with the first Route/Activity equality patch unless compilation forces a narrowly documented compatibility adapter.

---

## IF-ID-A07 — Readiness occurrence is correct, but active-operation ownership regresses to stable ID

**Severity:** Critical  
**Files:**

- `Runtime/ActivityFlow/ActivityReadinessOccurrence.cs`
- `Runtime/GameFlow/ActivityEntryReadinessActiveOperation.cs`

`ActivityReadinessOccurrence` correctly matches exact Activity reference plus occurrence sequence.

However, the active-operation wrapper uses `HasSameIdentity` to answer whether it owns a Route or Activity.

### Risk

A different asset carrying the same stable ID can be considered the owner of the current wait and can incorrectly cancel, invalidate or supersede it.

This area is especially sensitive after the recent `RouteAuthorityReplaced` interruption/supersession correction.

### Target authority

- Route ownership of the operation: exact Route reference or explicit Route runtime transaction identity;
- Activity ownership: exact Activity occurrence;
- interruption correlation: occurrence/revision, not stable ID alone.

### Required cut

Route side in IF-ID-03 and Activity side in IF-ID-04, with dedicated readiness supersession regression tests.

---

## IF-ID-A08 — Runtime host state synchronization uses stable-ID equality

**Severity:** High  
**File:** `Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs`

The host contains state synchronization that calls `CurrentRoute.HasSameIdentity(targetRoute)`.

### Risk

The host can retain or reconcile state as though a different Route reference were the same definition.

### Target authority

Exact reference for selected/active Route. Stable ID remains available for structured evidence.

### Required cut

Include in IF-ID-03, not as a standalone patch.

---

## IF-ID-A09 — Route context rollover in ActivityFlow uses stable-ID equality

**Severity:** High  
**File:** `Runtime/ActivityFlow/ActivityFlowRuntime.cs`

Route context and discovery-scope rollover include stable-ID comparison.

### Risk

An Activity runtime can retain context from a different Route asset that shares the same stable ID.

### Target authority

Exact Route reference for the selected context. Runtime discovery scope must have explicit scoped lifetime identity.

### Required cut

Coordinate with IF-ID-03. Do not leave ActivityFlow consuming old Route equality after RouteLifecycle migrates.

---

## IF-ID-A10 — Local Inspectors absorb project-wide identity findings

**Severity:** High — product correctness  
**Files:**

- `Editor/Authoring/RouteAssetEditor.cs`
- `Editor/Authoring/ActivityAssetEditor.cs`
- `Editor/Authoring/ActivityIdAuthoringValidator.cs`

The Route and Activity Inspectors combine local validation with project-wide `ValidateProjectAssets` results.

### Risk

A selected asset can appear locally invalid because two unrelated experimental or archived assets collide elsewhere in the project.

This contradicts the ADR's required validation scopes:

```text
Definition-local validation
Application graph validation
Project identity audit
```

### Target behavior

- local Inspector: missing/invalid ID and collisions involving the selected definition;
- Game Application validation: collisions reachable in the application's resolvable graph;
- project audit: all project collisions, clearly labeled as global findings.

### Required cut

IF-ID-06 — Validation Scope and Product UX.

---

## IF-ID-A11 — Explicit regenerate-existing-ID UX was not found in the inspected editors

**Severity:** Medium  
**Files:**

- `Editor/Authoring/RouteAssetEditor.cs`
- `Editor/Authoring/ActivityAssetEditor.cs`

The inspected Advanced/Debug surfaces expose read-only IDs and generation when empty, but an explicit regenerate-existing-ID action was not identified in these editor files.

### Product gap

A duplicated asset can retain its copied ID and needs an explicit, safe remediation path.

### Required product surface

```text
Regenerate Stable ID...
  confirmation
  old ID
  new ID
  collision context
  Undo support where safe
  validation refresh
```

A future `Duplicate as New Route/Activity` operation may be added after the authority migration, but it is not required for the first correction.

---

## IF-ID-A12 — No application-scoped ID resolver currently proves the boundary model

**Severity:** Deferred architectural gap  
**Area:** progression/save and future integrations

The inspected save area does not provide the RouteId/ActivityId-to-asset catalog required for external resolution.

This is not an immediate runtime defect because the current cut does not require save/progression resolution. It becomes mandatory before a public save or external integration stores stable IDs as references to authored definitions.

### Required constraints

- explicit Game Application scope;
- exactly-one resolution;
- explicit `NotFound` and `Ambiguous` results;
- no global search, name fallback or service locator.

### Required cut

IF-ID-07, only when a real boundary use case is ready.

---

## IF-ID-A13 — Current QA proof does not certify the critical collision semantics

**Severity:** Critical QA gap

The QAFramework was recently cleared/reorganized. In the current inspected tree, no dedicated identity-named regression proving different references with the same stable ID was identified.

### Required QA matrix

At minimum:

```text
Route: same reference / same ID
Route: different references / different IDs
Route: different references / same ID
Activity: same reference / same ID
Activity: different references / different IDs
Activity: different references / same ID
rename preserves ID
move preserves ID
explicit regenerate changes ID
local collision diagnostic
application collision blocking
project audit unrelated collision labeling
readiness supersession with colliding IDs
ownership acquisition/release with colliding IDs
```

QA must prove behavior through the same public or package-internal contract used by runtime, not through log parsing.

---

## IF-ID-A14 — FIRSTGAME does not yet prove the identity-authoring workflow

**Severity:** High product-proof gap

The latest inspected FIRSTGAME work advances Demo03 multiplayer/provisioning. It does not yet constitute a focused proof of:

- duplicate Route/Activity asset;
- collision presentation;
- explicit regeneration;
- application graph validation;
- two formerly colliding definitions operating correctly after regeneration;
- rename/move preservation.

### Required proof

Create one isolated identity demonstration after the package contract and QA are stable. FIRSTGAME should not own a permanent compatibility identity system.

---

## 5. Positive foundations

These areas should be preserved.

### IF-ID-P01 — Typed request surfaces already carry references

`RouteRequestTrigger` and `ActivityRequestTrigger` serialize typed assets and pass those references into runtime request ports.

**Implication:** no redesign of normal request authoring is required for ADR-014.

### IF-ID-P02 — Runtime state retains exact definitions

`RouteRuntimeState` and `ActivityRuntimeState` retain exact asset references while also exposing stable identity evidence.

**Implication:** the state model can support the target authority without replacing all state structures.

### IF-ID-P03 — Activity readiness occurrence already has the correct execution identity

Activity readiness uses exact Activity reference plus sequence.

**Implication:** migrate wrappers and callers toward this model rather than replacing occurrence with stable ID.

### IF-ID-P04 — Typed stable ID primitives are appropriate boundary types

`RouteId` and `ActivityId` remain valid. ADR-014 does not call for their removal.

### IF-ID-P05 — Project-wide duplicate detection already exists

The project audit capability can be retained, but it must be separated from local blocking validation.

---

## 6. Consolidated risk matrix

| Risk | Probability before migration | Impact | Priority |
|---|---:|---:|---:|
| Different Route references treated as already active due to same ID | Medium | Critical | P0 |
| Different Activity references treated as already active due to same ID | Medium | Critical | P0 |
| Wrong previous-context finalization | Medium | Critical | P0 |
| Readiness wait cancelled/superseded by colliding definition | Medium | Critical | P0 |
| Runtime content released by colliding owner | Medium | Critical | P0 |
| Route/Activity lifecycle use different authorities during partial migration | High | Critical | P0 |
| Local Inspector blocked by unrelated project collision | High | High | P1 |
| Missing explicit remediation for duplicated asset | High | Medium | P1 |
| Save/external resolver ambiguity | Low today, high when introduced | Critical | P2 before persistence launch |
| Additional incidental identity use missed by static audit | Low–Medium | High | P1 verification gate |

---

## 7. Recommended implementation order

```text
IF-ID-02  Vocabulary and QA baseline
IF-ID-03  Route reference authority
IF-ID-04  Activity reference authority
IF-ID-05  Ownership boundary redesign
IF-ID-06  Validation scopes and product UX
IF-ID-07  Scoped external resolver, when needed
IF-ID-08  FIRSTGAME identity workflow proof
```

### Why this order

- Vocabulary and tests must define the semantics before behavior changes.
- Route and Activity are separated to keep regression scope reviewable.
- Ownership is delayed because it requires lifetime/occurrence decisions beyond equality.
- Product validation follows the official runtime contract.
- External resolution is not anticipated without a real consumer requirement.
- FIRSTGAME proves the accepted package workflow rather than inventing a parallel identity system.

---

## 8. What must not be changed in the first patch

The first patch after this audit must not:

- permit duplicate IDs in an application-resolvable scope;
- remove `RouteId` or `ActivityId`;
- replace every stable ID with a Unity reference;
- redesign runtime-content ownership in the same cut as API renaming;
- introduce a global asset catalog;
- add name/path fallback;
- regenerate IDs automatically;
- redefine `Equals` or `GetHashCode` globally on authored assets;
- build a save system;
- modify FIRSTGAME before the package contract has QA coverage.

---

## 9. Acceptance criteria for closing IF-ID-01

This audit cut is complete when:

- canonical Route and Activity identity paths are classified;
- ambiguous equality APIs are identified;
- lifecycle, readiness and ownership risks are separated;
- editor validation scope problems are identified;
- current QA and FIRSTGAME proof gaps are recorded;
- implementation cuts are sequenced;
- no Git repository is modified;
- the next patch can be scoped without guessing authority semantics.

These criteria are satisfied by this report and its accompanying matrix.

---

## 10. Recommended next cut

### IF-ID-02 — Identity Vocabulary and QA Baseline

**Objective:** make the current and target semantics explicit without changing production lifecycle behavior.

**Package work:**

- add explicit stable-ID comparison vocabulary;
- mark `HasSameIdentity` obsolete/internal-migration-only;
- document direct reference equality as definition authority;
- add temporary compatibility annotations where old semantics remain;
- avoid ownership changes.

**QA work:**

- create collision fixtures with two distinct assets sharing one stable ID;
- prove current behavior as a regression baseline where necessary;
- add target-semantic tests that will be enabled with IF-ID-03/04;
- test rename, move and explicit regeneration behavior;
- avoid reflection when a package-visible contract can be tested directly.

**FIRSTGAME work:** none in this cut.

**Suggested commit message:**

```text
refactor(identity): clarify stable-id equality and add migration QA baseline
```

---

## 11. Final architectural statement

The core problem is not the presence of stable IDs. The problem is authority substitution:

```text
A typed asset reference enters the runtime,
then stable-ID equality is used to decide whether it is the same definition.
```

The safe correction is:

```text
Typed reference
  selects and compares authored definitions.

Stable ID
  represents those definitions across explicit boundaries.

Occurrence/revision/handle
  identifies one runtime execution.

Scoped owner identity
  controls acquisition and release.
```

ADR-014 should not be implemented as a string-field cleanup. It is a coordinated authority migration across lifecycle, readiness, ownership, validation and product UX.
