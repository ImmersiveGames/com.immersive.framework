# Immersive Framework — ADR-018-A Certification

**Date:** 2026-08-11  
**Type:** technical certification / API stabilization  
**ADR:** IF-ADR-018  
**Stage:** ADR018-A — Backend Contract Stabilization

## Objective

Certify that the Framework's Progression Save core backend contract is independent
from the built-in JSON implementation and small enough for third-party/custom
adapters.

## Certified contract

```text
IProgressionSaveStore
  BackendId
  ReadSlot
  WriteSlot
  DeleteSlot
```

Optional capability:

```text
IProgressionSaveCatalog
  ReadManifest
```

The optional capability is not required by the Stable core store contract.

## QA evidence

User-executed QAFramework terminal:

```text
[ADR018_QA_BACKEND_CONFORMANCE]
status='Passed'
contractCases='9'
jsonCoreCases='13'
alternateCoreCases='13'
catalogCases='5'
negativeCases='7'
jsonBackend='ProgressionSave:qa.json'
alternateBackend='ProgressionSave:qa.memory'
alternateCatalog='False'
consumerRuntime='ProgressionSaveRuntime'
semanticFingerprint='Missing>Saved>Loaded>Saved>Loaded>Deleted>Missing>Missing'
```

## What this proves

```text
same ProgressionSaveRuntime consumer path works with JSON and non-JSON backend
alternate backend implements only IProgressionSaveStore
catalog support is optional
save/load/overwrite/delete semantics match
missing semantics match
BackendUnavailable projection works
Corrupt projection works
Failed projection works
Rejected projection works
backend identity is preserved
no JSON-specific consumer dependency is required
```

## Stable API promotion

Promoted to Stable:

```text
IProgressionSaveStore
ProgressionSaveBackendId
ProgressionSaveSlotId
ProgressionSaveRecordId
ProgressionSavePayloadFormat
ProgressionSavePayload
ProgressionSaveSlotRecord
ProgressionSaveReadStatus
ProgressionSaveReadResult
ProgressionSaveWriteStatus
ProgressionSaveWriteResult
ProgressionSaveDeleteStatus
ProgressionSaveDeleteResult
```

The stable `ProgressionSaveSlotRecord` no longer publicly projects
`ProgressionSaveManifestEntry`; that projection is internal backend/catalog
maintenance.

## Still Experimental

```text
ProgressionSaveRuntime
ProgressionSaveRequest and request orchestration model
IProgressionSaveCatalog
ProgressionSaveManifest and catalog model
JsonProgressionSaveStore
```

Rationale:

```text
runtime/product composition is not yet finalized
catalog evolution must not constrain core adapters
JSON physical consistency/recovery is ADR018-B
```

## Technical acceptance

```text
core contract shape certified
alternate backend certified
core semantic result vocabulary certified
Stable boundary does not leak manifest/catalog types
no silent fallback introduced
no singleton/service locator introduced
```

## Product acceptance

Not applicable yet.

Designer-facing backend authoring belongs to ADR018-C.

## Next cut

```text
ADR018-B — Built-in JSON Minimum Backend Hardening
```

Focus:

```text
multi-artifact consistency
staged/commit semantics
failure injection
recovery behavior
corrupt/incomplete-state diagnostics
no silent partial success
```

## Suggested commit

```text
feat(progression-save): stabilize certified backend contract
```
