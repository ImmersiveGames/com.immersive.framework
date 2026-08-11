# IF-ADR-018 — Progression Save Stabilization Plan

**Date:** 2026-08-11  
**Type:** architecture → technical → product → integration  
**Inspected package baseline:** `dbae01cf2fce27d4cd7311233e32fa1dc034e057`

## Objective

Turn the existing Experimental Progression Save foundation into the intended product:

```text
one canonical Framework persistence contract
one official minimum built-in JSON backend
replaceable third-party/custom backends
no rewrite of Framework consumers when backend changes
```

## Current evidence

The package already contains:

```text
ProgressionSaveRuntime
ProgressionSaveRequest
ProgressionSaveMoment
IProgressionSaveStore
ProgressionSaveManifest
ProgressionSaveSlotRecord
ProgressionSavePayload
JsonProgressionSaveStore
```

The current runtime already receives `IProgressionSaveStore` explicitly and does not
know JSON, file paths, Snapshot participants or Route/Activity lifecycle.

This is the correct dependency direction.

## Critical review before Stable

Do not bulk-change `FrameworkApiStatus.Experimental` to Stable.

ADR018-A resolves the first contract-shape question:

```text
IProgressionSaveStore
  BackendId
  ReadSlot
  WriteSlot
  DeleteSlot

IProgressionSaveCatalog
  ReadManifest
```

Manifest mutation is backend-internal and `ContainsSlot` is not part of the core
compatibility promise.

ADR018-A Stable gate is complete.

The identical `ProgressionSaveRuntime` consumer suite passed against both built-in
JSON and a core-only in-memory backend. The core port and its transitive public
adapter primitives are now Stable.

Catalog, JSON implementation and runtime orchestration remain Experimental and are
not part of the ADR018-A compatibility promise.

## ADR018-A — Backend contract stabilization

### Objective

Prove that a backend can be replaced without changing Framework progression-save
consumers.

### Work

```text
DONE  reduce IProgressionSaveStore to BackendId + ReadSlot/WriteSlot/DeleteSlot
DONE  separate optional IProgressionSaveCatalog.ReadManifest
DONE  make built-in JSON manifest writes private backend responsibility
DONE  remove public ContainsSlot requirement
DONE  document adapter contract
DONE  create QA alternate in-memory backend
DONE  run identical request suite against JSON and in-memory backend
DONE  certify/promote the transitive core contract after QA
```

### Out of scope

```text
Project Settings UI
Snapshot orchestration
autosave lifecycle
third-party vendor SDK
```

### Technical smoke

At minimum:

```text
save -> load roundtrip
delete -> missing
missing load
backend unavailable
corrupt result projection
same ProgressionSaveRuntime code against two backends
```

### Acceptance

```text
consumer code unchanged across backends
no JSON knowledge in runtime request path
backend contract is small enough to implement externally
manifest/catalog ownership is explicit
result semantics are backend-independent
```

## ADR018-B — Built-in JSON minimum backend

### Objective

Make `JsonProgressionSaveStore` certifiable as the official minimum backend.

### Required minimum capability

```text
local persistence
manifest/catalog
multiple slots
save/load/delete
format version
explicit corrupt/missing/failure states
diagnostic backend identity
```

### Explicitly not required

```text
encryption
cloud
compression
anti-tamper
vendor backup
cross-device sync
```

### Consistency hardening

The current multi-artifact write/delete sequence must receive explicit commit/recovery
semantics.

The implementation may use, for example:

```text
temporary staging files
replace/rename commit
backup/rollback
recoverable transaction marker
manifest rebuild strategy
```

The exact choice should optimize for clarity and deterministic diagnostics rather than
simulate a database.

### Negative QA

Prove failure at each physical stage does not become silent partial success.

## ADR018-C — Product composition

### Objective

Make backend selection authorable without a global manager.

### Desired user flow

Conceptually:

```text
Project Settings / Progression Save configuration
  Backend
    Built-in JSON
    Custom Provider
```

The precise asset/provider shape must be designed during this cut.

Requirements:

```text
Built-in JSON may be the authored default
Custom backend selection is explicit
missing/broken custom provider fails explicitly
no silent fallback to JSON
runtime receives the selected store through typed composition
Advanced/Debug shows backend id/provider
```

### Runtime authority

Use an application/session-scoped owner with explicit lifetime.

Do not introduce:

```text
ProgressionSaveManager.Instance
service locator
scene lookup
Resources lookup from gameplay consumers
```

## ADR018-D — FIRSTGAME

### Objective

Prove real usability after the package product surface exists.

### Proof

```text
save basic progression with Built-in JSON
close/reopen game or Session as appropriate
load progression
replace backend with a QA/custom adapter
game-facing Progression Save requests remain unchanged
```

## Relationship to Snapshot

Snapshot orchestration is a separate future cut.

It may eventually produce `ProgressionSavePayload`, but backend adapters must never
discover Snapshot participants directly.

## Relationship to Preferences

Preferences remains completely independent.

A Frame Rate preference is not Progression Save data.

## Files in this architecture cut

```text
CREATE Documentation~/Architecture/ADRs/
  IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md

CREATE Documentation~/Architecture/Plans/
  IF-ADR-018-PROGRESSION-SAVE-STABILIZATION-PLAN-2026-08-11.md

CREATE Documentation~/Architecture/Reconciliation/
  IMMERSIVE-FRAMEWORK-ADR-018-RECONCILIATION-2026-08-11.md

EDIT Documentation~/Architecture/Tracking/
  IF-TRACK-Framework.md
```

No runtime code changes are included in this architecture cut.

## Suggested commit

```text
docs(architecture): define progression save backend independence
```
