# Immersive Framework — ADR-018-B Built-in JSON Certification

**Date:** 2026-08-11  
**Type:** technical certification / architecture reconciliation  
**ADR:** IF-ADR-018  
**Stage:** ADR018-B

## Objective

Certify the built-in minimum JSON backend after focused interrupted-operation and
recovery QA.

## Evidence

### Backend independence

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

### Built-in JSON recovery

```text
[ADR018_QA_JSON_RECOVERY]
status='Passed'
cases='18'
writeRecovery='3/3'
deleteRecovery='3/3'
uncommittedStaging='Discarded'
failClosed='6/6'
idempotentReplay='Passed'
normalWriteDelete='Passed'
transactionResidue='None'
backend='JsonProgressionSaveStore'
```

## Certified guarantees

```text
core store replacement does not change consumer semantics
normal JSON Write/Delete works
committed Write recovery converges at all tested apply boundaries
committed Delete recovery converges at all tested apply boundaries
staging without intent is discarded
invalid committed staging fails closed
corrupt transaction cannot be bypassed by normal access
already-applied committed transaction replay is idempotent
successful operations/recovery leave no transaction residue
```

## Explicit non-guarantees

```text
database-grade transactions
multi-process concurrent write coordination
cloud synchronization
encryption
anti-tamper
compression
device/filesystem guarantees beyond the tested recovery model
```

## API maturity decision

```text
IProgressionSaveStore + core adapter primitives
  STABLE / CERTIFIED

JsonProgressionSaveStore implementation
  OFFICIAL BUILT-IN MINIMUM / TECHNICALLY CERTIFIED

JsonProgressionSaveStore concrete API
  EXPERIMENTAL

IProgressionSaveCatalog
ProgressionSaveManifest
ProgressionSaveManifestEntry
ProgressionSaveManifestReadResult
  EXPERIMENTAL
```

The concrete JSON class is intentionally not promoted in ADR018-B.

ADR018-C must first decide the official authored selection/materialization surface.
Freezing direct construction before that product work would turn an implementation
detail into the primary integration contract.

## Stage result

```text
ADR018-B1 Recovery model       IMPLEMENTED
ADR018-B2 Write hardening      IMPLEMENTED
ADR018-B3 Delete hardening     IMPLEMENTED
ADR018-B4 Fail-closed recovery IMPLEMENTED
ADR018-B5 Recovery QA          CERTIFIED — 18/18
ADR018-B6 Disposition          CLOSED

ADR018-B                       CLOSED / 100%
```

## Next

```text
ADR018-C — Product Composition / Backend Authoring
```

Required next questions:

```text
How does a user select Built-in JSON?
How is a custom IProgressionSaveStore provider authored?
What object materializes the selected backend?
What owns ProgressionSaveRuntime lifetime?
How does Advanced/Debug show the active backend?
What happens when a selected custom provider is invalid?
```

## Suggested commit

```text
docs(architecture): certify ADR-018 built-in json backend
```
