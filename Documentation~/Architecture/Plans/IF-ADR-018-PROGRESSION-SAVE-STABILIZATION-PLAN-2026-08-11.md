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

Selected model:

```text
transaction staging
intent written last
committed transaction replay before every public JSON operation
validate staged transaction before canonical mutation
idempotent canonical convergence
fail closed on committed-invalid staging
```

State:

```text
B1 model defined              DONE
B2 WriteSlot hardening        DONE
B3 DeleteSlot hardening       DONE
B4 recovery/fail-closed path  DONE
B5 focused QA                 CERTIFIED — 18/18
B6 certification             CLOSED
```

Certification decision:

```text
implementation role          OFFICIAL BUILT-IN / CERTIFIED
core dependency              IProgressionSaveStore (Stable)
concrete JSON API            Experimental
catalog capability/model     Experimental
next cut                     ADR018-C Product Composition
```

Physical layout:

```text
<root>/.transaction/
  slot.stage.json
  manifest.stage.json
  intent.json
```

`intent.json` is the commit boundary.

The model intentionally does not claim database-grade atomicity or safe concurrent
writers from multiple processes.

### Negative QA

Focused QA must simulate interrupted committed transactions at each canonical apply
boundary and prove replay.

It must also prove that corrupt committed intent/staging blocks Read/Write/Delete
rather than bypassing recovery or silently substituting canonical state.

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


## ADR018-C implementation cut

### Objective

Turn the certified persistence foundation into an explicit application product
composition without freezing a global gameplay access pattern prematurely.

### C1 — Authoring intent

```text
GameApplicationAsset
  ProgressionSaveEnabled
  DefaultProgressionSaveProfile

ProgressionSaveProfile
  BuiltInJson
  CustomProvider
```

Status: IMPLEMENTED.

### C2 — Runtime materialization

```text
ProgressionSaveApplicationComposition.Resolve(GameApplicationAsset)
  -> explicit Disabled / Ready / Rejected result
  -> selected Profile creates IProgressionSaveStore
  -> ProgressionSaveRuntime
  -> FrameworkRuntimeHost application lifetime
```

Status: IMPLEMENTED.

### C3 — Product surface

```text
Game Application Inspector Progression Save section
Create/Open/Replace Profile
Profile Inspector
Configuration Status
Advanced / Debug
no Apply/Rebuild
no fallback
```

Status: IMPLEMENTED.

### C4 — QA gate

Required:

```text
disabled application produces Disabled/no runtime
Built-in JSON Profile produces JsonProgressionSaveStore
Custom Provider produces alternate IProgressionSaveStore
missing custom provider rejected
invalid custom provider rejected
provider create failure rejected
provider null store rejected
invalid BackendId rejected
Custom Provider failure never produces JSON
same public application composition path used by QA and bootstrap
```

Status: CERTIFIED — 12/12.

Terminal evidence:

```text
[ADR018_QA_PRODUCT_COMPOSITION]
status='Passed'
cases='12'
disabled='Passed'
builtIn='Passed'
custom='Passed'
negative='7/7'
noFallback='Passed'
selectionIsolation='Passed'
runtimeRequest='Passed'
composition='ProgressionSaveApplicationComposition'
```

### C5 — Certification

```text
product composition behavior      CERTIFIED
technical QA                      CERTIFIED — 12/12
no-fallback behavior              CERTIFIED
application-scoped ownership      CERTIFIED
runtime/code changes in C5        NONE
API Stable promotion              DEFERRED TO FIRSTGAME
ADR018-C                          CLOSED
```

### FIRSTGAME handoff

ADR018-D is now the active gate. FIRSTGAME must prove:

```text
a developer can find/create/configure the Profile
Built-in JSON configuration is understandable
Custom Provider intent is understandable
gameplay code can receive/use the scoped runtime without global lookup
```

If gameplay injection needs a new package binding surface, prove the shape there first
and migrate the mature solution back into the package.


## ADR018-C closure decision

ADR018-C is complete without introducing a global gameplay runtime accessor.

This is intentional:

```text
technical composition correctness  proven in QA
game-facing usability              not yet proven
```

The next step is therefore not another package abstraction.

The next step is a real consumer:

```text
FIRSTGAME
  create/configure Profile
  run Built-in JSON
  persist/load real game progression
  replace backend
  keep game-facing Progression Save request semantics unchanged
  evaluate how gameplay receives the scoped ProgressionSaveRuntime
```

Only after that proof should the package freeze or extend a game-facing binding/
injection surface.
