# IF-ADR-018-A — Backend Contract Cut

**Date:** 2026-08-11  
**Type:** technical / contract stabilization  
**Package Git baseline inspected:** `dbae01cf2fce27d4cd7311233e32fa1dc034e057`

## Objective

Reduce the Progression Save backend compatibility promise to the minimum semantics
needed by `ProgressionSaveRuntime`, while retaining catalog projection as an optional
capability.

## Scope

```text
IProgressionSaveStore core contract
optional IProgressionSaveCatalog
built-in JSON alignment
manifest write internalization
adapter guide
focused QA handoff
```

## Out of scope

```text
JSON atomicity/recovery hardening
Stable API promotion before QA
Project Settings/backend authoring
Snapshot orchestration
autosave lifecycle
third-party vendor implementation
```

## Files created

```text
Runtime/ProgressionSave/IProgressionSaveCatalog.cs
Documentation~/Guides/Progression-Save-Backend-Adapter-Contract.md
Documentation~/Architecture/Plans/IF-ADR-018-A-BACKEND-CONTRACT-CUT-2026-08-11.md
```

## Files edited

```text
Runtime/ProgressionSave/IProgressionSaveStore.cs
Runtime/ProgressionSave/JsonProgressionSaveStore.cs
Runtime/ProgressionSave/ProgressionSaveManifestWriteResult.cs
Runtime/ProgressionSave/ProgressionSaveWriteResult.cs
Runtime/ProgressionSave/ProgressionSaveWriteStatus.cs
ADR-018
ADR-018 stabilization plan
ADR-018 reconciliation
IF-TRACK-Framework.md
```

## Files removed

None.

`ProgressionSaveManifestWriteResult` remains as an Internal implementation detail,
not a public product/API surface.

## Product surface affected

Developer integration surface only.

No designer-facing authoring is added in ADR018-A.

## Expected developer flow

```text
custom backend
  implements IProgressionSaveStore
  receives Framework slot/record/payload semantics
  returns typed Framework results

optional listing/catalog
  additionally implements IProgressionSaveCatalog
```

## Technical smoke expected

```text
package compiles
IProgressionSaveStore exposes only BackendId/ReadSlot/WriteSlot/DeleteSlot
IProgressionSaveCatalog exposes ReadManifest
JsonProgressionSaveStore implements both
alternate QA backend implements only core store
same ProgressionSaveRuntime suite passes on both
```

## Technical acceptance

```text
no concrete-backend knowledge in ProgressionSaveRuntime
catalog not required by core backend
manifest mutation not public contract
ContainsSlot not public contract
no silent fallback
API remains Experimental until QA evidence
```

## Product acceptance

```text
external save-system adapter can stay small
consumer code does not change by backend
built-in JSON keeps optional catalog capability
advanced backend capabilities remain outside the core contract
```

## Architectural gain

```text
before:
one broad store interface mixed persistence + catalog maintenance

after:
core persistence port
+
optional read-only catalog capability
```

## Usability gain

A third-party adapter implementer does not need to reproduce Framework manifest
maintenance merely to support Save/Load/Delete.

## Suggested commit

```text
refactor(progression-save): split core store and catalog contract
```
