# Immersive Framework — Package → ADR Reverse Audit

**Date:** 2026-08-11  
**Type:** architecture reverse audit / source-to-decision reconciliation  
**Package:** `com.immersive.framework`  
**Package baseline inspected:** `8b7278e3683daef1b2eac6f78c1e0b156e4365da` (`07-fix`)  
**Repository access:** read-only  
**Purpose:** identify behavior, public contracts, authoring surfaces and runtime authorities that exist in the package but are not explicitly governed by the current ADR set.

---

## 1. Objective

Previous reconciliation work primarily asked:

```text
ADR
  ↓
does the package implement the accepted decision?
```

This audit reverses that direction:

```text
package
  ↓
what does it expose, author or execute?
  ↓
which ADR governs that behavior?
  ↓
is the relationship explicit enough?
```

The audit is intentionally not a "one namespace = one ADR" exercise.

A package implementation may legitimately exist without a dedicated ADR when it is:

- an internal implementation mechanism of an already accepted authority;
- a small adapter under an accepted feature contract;
- a utility or metadata helper;
- a deterministic technical realization that introduces no new product/runtime authority.

The architecture gap begins when package code establishes meaningful policy, ownership,
public extension semantics, application behavior or product authoring that is not
captured by an accepted decision.

---

## 2. Classification model

| Class | Meaning | Default action |
|---|---|---|
| **A — ADR-covered** | The package surface is a legitimate implementation of an existing accepted ADR. | Preserve; no new ADR. |
| **B — Under-recorded** | An ADR covers the parent domain, but the package has meaningful semantics that are not stated clearly enough. | Reconcile or amend the owning ADR when useful. |
| **C — Architecture gap** | Canonical product/runtime behavior exists without a sufficiently owning ADR. | Create or explicitly extend a normative ADR. |
| **D — Experimental foundation** | Public experimental contracts/adapters exist, but the canonical product does not compose or operate them as a feature. | Decide keep/formalize/defer/remove before adding more implementation. |
| **E — Support / implementation detail** | Internal utility, metadata or mechanism with no independent product authority. | No ADR required. |

A **D** finding must not automatically become a new ADR. Experimental foundations can be
removed or deferred if there is no real product need.

---

## 3. Documentation authority check

The current package documentation states:

```text
Architecture/ADRs/             normative architecture decisions
Architecture/Reconciliation/   current technical reconciliation and certification
Architecture/Tracking/         current mutable framework status
Guides/                        current product usage
```

and explicitly:

```text
ADRs decide.
```

That rule is important for this audit.

A Guide may explain an accepted feature, but a Guide should not be the only place that
defines a meaningful runtime authority or policy.

---

## 4. Executive result

The reverse audit does **not** find a broad parallel architecture outside the ADRs.

Most major runtime namespaces map cleanly to the accepted decisions:

```text
lifecycle / GameFlow / scenes / runtime content
  -> ADR-001

authoring model
  -> ADR-002 / ADR-010

Player / Actor
  -> ADR-003 / ADR-012 / ADR-015 / ADR-016

Camera
  -> ADR-004

Input / Pause / Gate / Reset
  -> ADR-005

Loading / Transition
  -> ADR-006

Activity readiness / reveal
  -> ADR-007

Persistent application composition
  -> ADR-008

Activity-local visibility
  -> ADR-009

participant-aware Loading progress
  -> ADR-011

optional Audio BGM
  -> ADR-013

stable identity / definition authority
  -> ADR-014
```

The important reverse findings are narrower:

```text
C — Application Frame Rate
    real product behavior exists and is not normatively owned

D — Preferences
D — Snapshot
D — ProgressionSave
    public experimental persistence rails exist without canonical composition

B/C — ObjectEntry
    public experimental authoring + host-side scoped snapshot exists,
    but the intended long-term product ownership is not explicit

B — FrameworkValidationMode
    stable public product policy exists; parent ADRs cover validation generally,
    but exact semantics are package-defined

B — FrameworkApiStatus
    stable API maturity governance exists as code metadata without one explicit
    architecture-governance decision
```

The strongest immediate architecture gap is **Application Frame Rate**.

---

## 5. Package-wide reverse matrix

| Package area | Classification | Current ADR owner / interpretation | Audit disposition |
|---|---|---|---|
| `ActivityFlow` | A | ADR-007, ADR-011, ADR-001 | Covered |
| `ActivityRestart` | A | ADR-005 | Covered |
| `Actors` | A | ADR-003, ADR-012 | Covered |
| `ApiStatus` | B | Cross-cutting governance; partially implicit in ADR-002/010 | Under-recorded |
| `ApplicationLifecycle` | A + C | ADR-001 generally; Frame Rate subfeature is not owned | Split finding |
| `Audio` | A | ADR-013 | Covered |
| `Authoring` | A + B | ADR-002, ADR-010, ADR-014 + feature ADRs | ValidationMode under-recorded |
| `Bootstrap` | A | ADR-001 | Covered |
| `Camera` | A | ADR-004 | Covered |
| `CameraAuthoring` | A | ADR-004, ADR-010 | Covered |
| `Common` | E | Shared implementation utilities | No ADR needed |
| `ContentFlow` | A/E | ADR-001 content ownership; ADR-014 identity rules | Implementation mechanism |
| `CycleReset` | A | ADR-005 | Covered |
| `Diagnostics` | A | ADR-001, ADR-006, ADR-010 | Covered cross-cutting |
| `GameFlow` | A | ADR-001, ADR-006, ADR-007, ADR-011 | Covered |
| `Gate` | A | ADR-005, ADR-006, ADR-007 | Covered |
| `GlobalUi` | A | ADR-001/008 composition + owning feature ADRs | Integration mechanism |
| `Identity` | A | ADR-014 | Covered |
| `InputMode` | A | ADR-005 | Covered |
| `Loading` | A | ADR-006, ADR-011 | Covered |
| `LocalContribution` | A/E | ADR-001/014 implementation detail | No standalone ADR |
| `ObjectEntry` | B/C | Partly compatible with ADR-001/014; no explicit owner | Needs disposition |
| `ObjectReset` | A | ADR-005 | Covered |
| `Pause` | A | ADR-005 | Covered |
| `Performance` | **C** | No current feature ADR | **Architecture gap** |
| `PlayerParticipation` | A | ADR-003/012/015/016 | Covered |
| `PlayerSlots` | A | ADR-003/012/016 | Covered |
| `Preferences` | **D** | No canonical product owner | Experimental foundation |
| `ProgressionSave` | **D** | No canonical product owner | Experimental foundation |
| `Properties` | E | Assembly metadata | No ADR needed |
| `Reset` | A | ADR-005 | Covered |
| `RouteLifecycle` | A | ADR-001 | Covered |
| `RuntimeContent` | A/E | ADR-001 + ADR-014 implementation | No standalone ADR by default |
| `SceneLifecycle` | A | ADR-001 | Covered |
| `SessionLifecycle` | A | ADR-001 | Covered |
| `Snapshot` | **D** | No canonical product owner | Experimental foundation |
| `Transition` | A | ADR-006 | Covered |
| `TransitionEffects` | A | ADR-006 | Covered |
| `UnityInput` | A | ADR-005 | Covered |

---

# 6. Finding RA-01 — Application Frame Rate is a real architecture gap

**Classification:** C — Architecture gap  
**Priority:** P0  
**Current cut identifier in source/docs:** `IF-APPLICATION-FRAME-RATE-01`

## 6.1 What exists

The package contains a complete application-level frame pacing feature:

```text
Runtime/Performance/
  ApplicationFrameRateMode
  ApplicationFrameRatePolicy
  ApplicationFrameRatePolicyApplier
  ApplicationFrameRateApplicationResult
  ApplicationFrameRateApplicationStatus
```

The policy is publicly authored through `GameApplicationAsset`.

The `GameApplicationAsset` is itself marked **Stable** and describes itself as the
public authoring root whose breaking changes require ADR/migration.

Its current application intent includes:

```text
startup Route
Player Session
Actor-selection duplicate policy
Frame Rate policy
Persistent Content
Validation Mode
```

The custom `GameApplicationAssetEditor` exposes a designer-facing:

```text
Performance
  Frame Rate
    Mode
    Target Frame Rate
    VSync Count
```

surface with explanatory messages and validation.

## 6.2 Runtime authority

The feature is not passive configuration.

`FrameworkRuntimeHost` applies the policy during startup.

Current behavior includes:

```text
UseUnityDefaults
  -> preserve Application.targetFrameRate
  -> preserve QualitySettings.vSyncCount

TargetFrameRate
  -> QualitySettings.vSyncCount = 0
  -> Application.targetFrameRate = authored target

VerticalSync
  -> Application.targetFrameRate = -1
  -> QualitySettings.vSyncCount = authored interval
```

The implementation:

- validates before mutation;
- avoids partial mutation for an invalid policy;
- stores a typed result;
- logs summary and detailed diagnostics;
- detects platform-limited Vertical Sync;
- is idempotent when values already match.

The Guide also states that an invalid policy causes framework startup to fail.

These are architecture-level semantics because they define:

- application-level ownership;
- interaction with Unity process-global frame pacing state;
- startup failure policy;
- VSync-vs-target precedence;
- platform-limitation behavior;
- explicit absence/default behavior.

## 6.3 Why existing ADRs are insufficient

ADR-001 explains the application/session composition root but does not decide frame
pacing authority.

ADR-002/010 explain how product authoring should be presented, but do not decide the
runtime policy itself.

No current ADR title or accepted boundary owns application performance/frame pacing.

The current product Guide therefore contains meaningful decision semantics that are
not backed by a dedicated normative decision, even though package documentation says
"ADRs decide."

## 6.4 Recommended disposition

Create a narrow normative decision, preferably the next available ADR:

```text
IF-ADR-017 — Application Frame Rate and Frame Pacing Authority
```

Suggested accepted boundary:

```text
GameApplicationAsset owns application-level intent.

UseUnityDefaults is explicit valid absence of framework override.

TargetFrameRate and VerticalSync are mutually exclusive authored modes.

FrameworkRuntimeHost applies the policy once during application startup.

Invalid authored policy cannot partially mutate Unity state.

No scene-local framework manager is required.

Dynamic FPS, thermal policy, Adaptive Performance and XR refresh selection remain
outside the current boundary.

Platform limitations are explicit diagnostics, not silent policy substitution.
```

Do **not** broaden this ADR into a generic performance manager.

---

# 7. Finding RA-02 — Preferences / Snapshot / ProgressionSave are unowned experimental rails

**Classification:** D — Experimental foundation  
**Priority:** P1 decision, not P1 implementation

These three namespaces form a coherent conceptual persistence boundary, but the current
canonical product does not compose them.

## 7.1 Preferences

`IPreferencesStore` explicitly defines a store for:

```text
user/application Preferences
```

and explicitly rejects conflation with:

```text
Progression Save
Snapshot envelopes
gameplay state capture
```

`PlayerPrefsPreferencesStore` is a concrete `PlayerPrefs` adapter with typed markers
to avoid silent type fallback.

This is a usable public library surface, but no current `GameApplicationAsset`
configuration or `FrameworkRuntimeHost` composition was found for it.

## 7.2 Snapshot

`ISnapshotParticipant` explicitly states:

```text
no backend
no discovery
no orchestration runtime
```

The participant only:

```text
describes local state ownership
captures local state into an envelope
restores from a supplied envelope
```

It explicitly does not persist or load the envelope.

This is a clear architectural foundation, but not a functioning framework Snapshot
feature.

## 7.3 ProgressionSave

`IProgressionSaveStore` defines slots, manifests and storage operations.

`JsonProgressionSaveStore` provides a concrete local JSON backend under
`Application.persistentDataPath`.

However, the package does not currently expose a canonical Game Application authoring
or host lifecycle that chooses the backend, captures Snapshots, selects progression
slots or restores game state.

Therefore:

```text
JSON backend exists
!=
framework progression-save product exists
```

## 7.4 Important status drift

`IProgressionSaveStore` still carries API-status text saying:

```text
F21E Progression Save backend port; no concrete backend.
```

but `JsonProgressionSaveStore` is a concrete backend in the same current package.

That metadata is stale and should be corrected regardless of the final persistence
strategy.

## 7.5 Historical maturity signal

The inspected Git history shows `Snapshot`, `Preferences` and `ProgressionSave`
already present in the package baseline commit:

```text
1f1c09586c8854be36590a38323b2327c90e0594
chore: prepare framework package 1.0.0-preview.1
2026-07-03
```

No later path-specific implementation evolution was returned for those namespaces in
the inspected history.

By contrast, Application Frame Rate has a dedicated later product commit on
2026-08-04.

That difference supports treating the persistence cluster as retained experimental
foundation rather than a currently promoted product feature.

## 7.6 Consumer check

Repository code searches performed during this audit did not find imports of:

```text
Immersive.Framework.Preferences
Immersive.Framework.ProgressionSave
Immersive.Framework.Snapshot
```

in the inspected QAFramework or FIRSTGAME searches.

The GitHub code-search endpoint reported incomplete indexing, so this is supporting
evidence rather than an absolute proof of zero references.

The stronger evidence remains:

- no canonical authoring root integration;
- no dedicated host composition;
- explicit Snapshot comment saying no orchestration runtime;
- no current Guide for Preferences/Snapshot/ProgressionSave;
- no current ADR owning the subsystem.

## 7.7 Recommended disposition

Do **not** create three ADRs just because three namespaces exist.

First open a **Persistence Disposition** cut answering:

```text
Do we intend to ship these rails as an official near-term framework subsystem?

YES
  -> define one persistence architecture first
  -> freeze Preferences / Snapshot / ProgressionSave responsibility boundaries
  -> define authority/lifetime/discovery/restore semantics
  -> then decide adapters and product surface
  -> then QA
  -> then FIRSTGAME

NO / NOT YET
  -> remove, defer or clearly quarantine the unused experimental rails
  -> do not keep growing public APIs with no canonical product lifecycle
```

This follows the current framework rule against anticipating complete systems before
a real product boundary exists.

---

# 8. Finding RA-03 — ObjectEntry is under-recorded and needs an ownership decision

**Classification:** B, potentially C later  
**Priority:** P1/P2

## 8.1 What exists

`ObjectEntryDeclaration` is a public Experimental scene-authored component with an
official Add Component menu.

It authors:

```text
Object Entry Id
Scope: Session / Route / Activity
explicit Route or Activity owner
Requiredness
Display Name
```

It has a dedicated custom Inspector and explicit validation guardrails.

The component is intentionally passive: it does not itself spawn, bind gameplay
objects, reset them or create Player/Actor semantics.

The host also maintains an Object Entry runtime-context snapshot/scoped collection
used as part of current ownership context.

Therefore ObjectEntry is more than a private DTO, but less than an autonomous gameplay
system.

## 8.2 Current architectural fit

Parts of ObjectEntry fit existing decisions:

```text
scope / runtime ownership
  -> ADR-001

stable functional identity / no GameObject-name authority
  -> ADR-014

product authoring principles
  -> ADR-002 / ADR-010
```

However, no current ADR explains why `ObjectEntry` exists as its own public logical
object taxonomy, what features are allowed to depend on it, or whether it is intended
to become a cross-system product abstraction.

## 8.3 Recommended disposition

Do **not** create a dedicated ObjectEntry ADR yet.

First decide:

```text
Option A — implementation support
  ObjectEntry remains narrow identity/ownership metadata used by existing systems.
  -> document it as a subordinate contract of ADR-001/014
  -> keep public surface only if consumers genuinely need to author it

Option B — cross-system game-object abstraction
  Multiple framework systems are expected to address logical objects through it.
  -> this becomes a real architecture boundary
  -> create a dedicated ADR before expanding dependencies
```

Current source is closer to **Option A**.

---

# 9. Finding RA-04 — FrameworkValidationMode exact semantics are under-recorded

**Classification:** B — Under-recorded  
**Priority:** P2

`FrameworkValidationMode` is a Stable public API with exact policy:

```text
Strict
  required configuration fails
  warnings promoted to errors
  info included

Standard
  required configuration fails
  warnings remain warnings
  info included

Release
  required configuration fails
  warnings remain warnings
  info suppressed
```

Unknown mode is treated conservatively.

ADR-010 owns the general validation/product-surface direction and says required
configuration cannot be hidden or silently repaired, but it does not explicitly freeze
these three mode semantics.

This does not require a new ADR.

Recommended action:

```text
add the exact ValidationMode contract to ADR-010
or
add a normative cross-reference from ADR-010 to an accepted application-validation policy
```

Only do this when ADR-010 is next touched; it is not an urgent runtime defect.

---

# 10. Finding RA-05 — FrameworkApiStatus is architecture governance encoded as code

**Classification:** B — Under-recorded governance  
**Priority:** P2

`FrameworkApiStatus` is itself Stable and defines:

```text
Stable
  consumable by games/external modules
  changes require ADR/migration

Experimental
  controlled development use
  no compatibility guarantee

Internal
  implementation detail

Deferred
  retained/frozen source, not active baseline

DevelopmentTooling
  QA/editor/development tooling

Removed
  removed or scheduled for removal
```

This metadata is useful and aligned with current architecture practice, but the governance
rule is mostly encoded in source attributes/comments rather than one explicit architecture
policy.

A new feature ADR is unnecessary.

Recommended action:

```text
record API maturity/status semantics in an architecture governance document
or a small section of ADR-002/010
```

This becomes especially useful for reverse audits because it lets us distinguish:

```text
public Stable product contract
vs
public Experimental foundation
vs
internal implementation rail
```

---

# 11. Areas that do NOT justify new ADRs

## 11.1 RuntimeContent / ContentFlow

These contain substantial internal lifecycle machinery:

```text
typed content identity
scope
owner
handles
materialization
release planning
release results
runtime registry
```

The code volume is large, but code volume is not architecture independence.

Current interpretation:

```text
ADR-001
  owns lifecycle/content authority and scoped runtime composition

ADR-014
  owns definition/stable identity distinctions
```

Unless RuntimeContent becomes a direct public extension/product surface with new
independent semantics, it should remain implementation of those decisions.

## 11.2 LocalContribution

The current discovery implementation is explicitly `Internal`.

It:

- discovers composition-scoped contributions;
- requires explicit IDs;
- emits structured issues;
- refuses object-name/hierarchy fallback;
- does not own materialization, release, loading, reset, snapshot or lifecycle.

This is a textbook implementation mechanism under ADR-001/014.

No standalone ADR is justified.

## 11.3 GlobalUi

`GlobalUi` is persistent-scene integration plumbing for feature surfaces such as:

```text
Camera
Loading
Transition
Pause
Player provisioning
request triggers
```

Its authority remains distributed to the actual owning systems.

ADR-001 + ADR-008 + the individual feature ADRs are sufficient.

Do not create a "Global UI Manager" ADR or promote the integration container into a new
runtime authority.

## 11.4 ObjectReset / CycleReset / Reset

These are explicitly covered by ADR-005.

No reverse gap.

## 11.5 Common / Properties

Shared helpers and assembly metadata do not require architectural decisions simply
because they are top-level folders.

---

# 12. Product-surface maturity check

A useful reverse-audit test is:

```text
Does a normal user encounter this feature through the official product flow?
```

Current result:

| Surface | Product authoring | Runtime integration | Current Guide | ADR | Interpretation |
|---|---:|---:|---:|---:|---|
| Application Frame Rate | YES | YES | YES | **NO** | **True normative gap** |
| Preferences | NO canonical root | NO host composition | NO | NO | Experimental library rail |
| Snapshot | NO | explicitly no orchestration | NO | NO | Experimental contract rail |
| ProgressionSave | NO | NO host composition | NO | NO | Experimental library/backend rail |
| ObjectEntry | YES component | partial host context | NO current Guide | NO dedicated | Under-recorded experimental abstraction |
| ValidationMode | YES | YES in validation policy | general docs | parent ADR only | Under-recorded stable policy |
| ApiStatus | metadata | cross-cutting | no product flow needed | no dedicated | Governance gap, not feature gap |

This table is the main reason not to treat every reverse finding equally.

---

# 13. Recommended architecture backlog

## P0 — RA-CUT-01: Formalize Application Frame Rate

**Type:** architecture / technical reconciliation

### Objective

Make the already-implemented application frame pacing behavior normatively owned.

### Scope

- new narrow ADR for application-level frame pacing;
- link existing Guide;
- reconcile existing package implementation;
- add tracker entry;
- identify focused QA required to certify the current boundary.

### Out of scope

- dynamic frame-rate switching;
- Adaptive Performance;
- thermal management;
- XR refresh policy;
- Route/Activity FPS overrides;
- FPS HUD;
- generic performance service.

### Package implementation expected

Ideally:

```text
no runtime change
```

unless focused QA finds a divergence.

### Suggested ADR

```text
IF-ADR-017 — Application Frame Rate and Frame Pacing Authority
```

### Suggested commit

```text
docs(architecture): formalize application frame rate authority
```

---

## P1 — RA-CUT-02: Persistence Foundation Disposition

**Type:** architecture/product inventory decision

### Objective

Decide whether the existing experimental:

```text
Preferences
Snapshot
ProgressionSave
```

belong to the active future product.

### First decision

```text
retain + formalize
or
defer/remove
```

### Do not do yet

- do not add Save Manager;
- do not add a global persistence service;
- do not connect GameApplication automatically;
- do not add save slots UI;
- do not add Snapshot discovery;
- do not create QA merely to justify existing unused rails.

### Mandatory cleanup regardless of disposition

Correct stale `IProgressionSaveStore` API-status text that says there is no concrete
backend while `JsonProgressionSaveStore` exists.

### Suggested commit if docs-only disposition

```text
docs(architecture): decide experimental persistence foundation boundary
```

---

## P1/P2 — RA-CUT-03: ObjectEntry Ownership Reconciliation

**Type:** architecture clarification

### Objective

Decide whether ObjectEntry is:

```text
subordinate scoped identity metadata
or
future cross-system logical-object abstraction
```

### Preferred current direction

Keep it subordinate unless a real consumer feature proves the broader abstraction is
needed.

### Suggested commit

```text
docs(architecture): clarify ObjectEntry ownership boundary
```

---

## P2 — RA-CUT-04: Architecture Governance Hygiene

**Type:** documentation

### Scope

- record exact `FrameworkApiStatus` semantics;
- record exact `FrameworkValidationMode` semantics in the appropriate existing
  architecture authority;
- do not create new runtime systems.

### Suggested commit

```text
docs(architecture): record API maturity and validation policy semantics
```

---

# 14. Acceptance criteria for this reverse audit

The audit is complete when it can answer, for every meaningful package domain:

```text
what is it?
who owns it?
is it public?
is it actually composed by the framework?
what ADR governs it?
if no ADR governs it, is that acceptable?
if not acceptable, what is the smallest next decision?
```

Current answer:

```text
broad hidden parallel architecture
  NOT FOUND

major operational feature without adequate ADR
  APPLICATION FRAME RATE

experimental public foundations without canonical product adoption
  PREFERENCES
  SNAPSHOT
  PROGRESSION SAVE

under-recorded experimental abstraction
  OBJECT ENTRY

under-recorded stable cross-cutting policies
  VALIDATION MODE
  API STATUS

large internal namespaces requiring new ADR solely due to size
  NONE
```

---

# 15. Recommended sequence

```text
1. Formalize Application Frame Rate.
2. Decide Persistence foundation: retain/formalize vs defer/remove.
3. Clarify ObjectEntry ownership before expanding it.
4. Record API-status / ValidationMode governance opportunistically.
5. Do not reopen already reconciled ADRs for implementation-only namespaces.
```

This sequence avoids two failure modes:

```text
under-documenting real product authority
and
over-producing ADRs for implementation details
```

---

# 16. Architectural gain

The reverse-audit process should become a recurring architecture check:

```text
ADR -> package reconciliation
  proves decisions are implemented

package -> ADR reverse audit
  proves implementation has decision ownership
```

Together they prevent:

```text
dead ADRs
undocumented product authority
experimental rails silently becoming de facto public architecture
internal mechanisms being promoted into unnecessary systems
Guides becoming accidental normative authority
```

The desired invariant is:

```text
Every meaningful official product/runtime authority is owned by an accepted decision.

Not every class or namespace needs an ADR.
```
