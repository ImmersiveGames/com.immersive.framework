# IF-ADR-009 — Activity Local Visibility Rules

Status: **Accepted — CLOSED for current accepted boundary**  
Last updated: 2026-08-10  
Package implementation: **Implemented**  
Technical QA: **Certified**  
FIRSTGAME: **Not required for current accepted boundary**  
Related decisions: IF-ADR-006, IF-ADR-007, IF-ADR-010, IF-ADR-014  
QA evidence: `IMMERSIVE-FRAMEWORK-ADR-009-QA-CERTIFICATION-2026-08-10.md`

## Context

Activity-owned content may need to remain hidden, disabled or
presentation-gated until lifecycle/readiness conditions are satisfied.

Visibility must be explicit and scoped rather than inferred from scene load,
hierarchy position or object naming.

## Decision

Activity local visibility is expressed through explicit authored/adapted
configuration bound to Activity lifecycle/readiness.

Visibility authority is contextual and occurrence-aware.

Required visibility failures are blocking and diagnostic.

Optional presentation behavior must not silently weaken required readiness.

Stable authored identity identifies authored definitions; it is not by itself
runtime occurrence, ownership, release or restoration authority.

## Architectural constraints

- Runtime authority is scoped, typed and lifetime-explicit.
- Required invalid configuration fails explicitly before commit.
- Optional invalid visibility configuration remains non-mutating and diagnostic.
- Visibility is not inferred from scene load.
- Object names and hierarchy paths are not fallback identity.
- Lifecycle occurrence/replacement semantics remain explicit.
- Stale occurrences cannot apply, release or restore state owned by the current occurrence.
- Release/restoration affects only context-owned state.
- Editor authoring does not become gameplay authority.
- Distinct authored definitions colliding on the same stable `ActivityId` are invalid.

## Accepted runtime model

The package uses `ActivityLocalVisibilityAdapter` with Activity lifecycle and
framework-owned discovery scoped to supplied framework roots.

The accepted model is:

```text
authored/local visibility intent
        ↓
Activity occurrence/lifecycle
        ↓
scoped visibility application
        ↓
release/restoration/disposal evidence
```

Occurrence/revision, replacement and disposal are governed by the existing
serialized transaction model and `RuntimeDefinitionToken`. Events are
post-transition facts and do not independently apply visibility.

No global scene search is part of the authority model.

## Closure audit — 2026-08-10

The focused audit identified two concrete gaps.

### Gap 1 — invalid Required binding could proceed

Previously, an invalid `Required` visibility binding could be diagnosed as a
warning and still continue toward commit.

The package now rejects the transition before commit. The previous Activity
retains authority when the incoming Activity contains an invalid required
visibility binding.

Invalid `Optional` bindings remain non-mutating and diagnostic and do not weaken
required behavior.

Application results distinguish invalid required and optional bindings, with
diagnostics that include the target, `LocalContentId`, requiredness, configured
list and failure reason.

### Gap 2 — stable ActivityId collision

Two distinct authored definitions using the same stable `ActivityId` were not
rejected.

The package now treats this collision as invalid. Stable ID remains authored
identity and does not become runtime occurrence or ownership authority.

## Product surface

No new Profile, Composer, Wizard or Apply/Rebuild layer is required for the
current accepted boundary.

Direct component authoring remains valid because consumers do not need to
reconstruct hidden runtime authority manually.

The product requirement is therefore:

```text
Add Component
    ↓
clear target configuration
    ↓
explicit required/optional semantics
    ↓
validation and actionable diagnostics
    ↓
runtime occurrence-owned behavior
```

Additional authoring layers remain conditional on future demonstrated consumer
friction, not on ADR-009 technical closure.

## Technical QA certification

The corrected boundary was executed in Unity and certified by QAFramework.

```text
QA_ACTIVITY_LOCAL_VISIBILITY_RULE
status='Passed'
cases='28'
completed='positive,negative,no-active,invalid,idempotent,single-owner'

QA_ACTIVITY_LOCAL_VISIBILITY_LIFECYCLE
status='Passed'
cases='18'
completed='positive-single,positive-multiple,negative-single,negative-multiple,no-active-visible,required-invalid-blocks,optional-invalid-diagnostic,clear,idempotence'
```

The certification proves the current accepted boundary for:

```text
positive and negative rule evaluation
no-active behavior
invalid configuration handling
idempotence
single-owner authority
required-invalid pre-commit blocking
optional-invalid non-mutating diagnostics
clear/release lifecycle
single and multiple target lifecycle behavior
```

## FIRSTGAME

FIRSTGAME is not required to close the technical ADR-009 boundary.

Future real-game use may still reveal UX friction around discoverability,
terminology or debugging. Such findings are Consumer UX Evidence and may justify
a separate product improvement without reopening the current technical contract.

## Completion criteria

The current accepted boundary is closed because evidence now confirms:

```text
visibility never becomes implicit scene-load authority
required invalid targets fail explicitly before commit
optional invalid targets remain non-mutating and diagnostic
stable-ID collisions between distinct definitions are rejected
occurrence ownership remains runtime-scoped and diagnosable
release/restoration affects only context-owned state
replacement/disposal does not leak visibility authority
normal authoring does not require hidden internal contracts
negative lifecycle regressions are covered in QAFramework
```

## Current disposition

```text
Architecture: Accepted
Package: Implemented
QA: Certified
FIRSTGAME: Not Applicable for current accepted boundary
Status: CLOSED — current accepted boundary
```

## Normative summary

```text
Keep visibility explicit, scoped and occurrence-aware.
Required invalid configuration blocks before commit.
Optional invalid configuration is non-mutating and diagnostic.
Stable authored identity is not runtime ownership authority.
Do not add authoring layers without demonstrated product need.
Technical closure is established by package behavior plus QA evidence.
```
