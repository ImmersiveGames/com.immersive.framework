# IF-ADR-018-B — Built-in JSON Hardening Cut

**Date:** 2026-08-11  
**Type:** technical / backend hardening  
**Package Git baseline:** `bc6159efc95c46fc1f34a706d24dfd9fda243222`

## Objective

Make `JsonProgressionSaveStore` a recoverable minimum first-party backend without
changing the Stable `IProgressionSaveStore` contract.

## Scope

```text
single-process JSON I/O serialization
transaction staging
intent-written-last commit boundary
idempotent write recovery
idempotent delete recovery
staged-data validation before canonical mutation
fail-closed committed-corrupt state
successful recovery diagnostics
focused QA handoff
```

## Out of scope

```text
encryption
cloud
compression
anti-tamper
database transactions
multi-process write coordination
autosave
Snapshot orchestration
backend selection authoring
FIRSTGAME
```

## Files created

```text
Documentation~/Guides/Progression-Save-Built-In-Json-Backend.md
Documentation~/Architecture/Plans/IF-ADR-018-B-JSON-HARDENING-CUT-2026-08-11.md
```

## Files edited

```text
Runtime/ProgressionSave/JsonProgressionSaveStore.cs
Documentation~/Architecture/ADRs/
  IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md
Documentation~/Architecture/Plans/
  IF-ADR-018-PROGRESSION-SAVE-STABILIZATION-PLAN-2026-08-11.md
Documentation~/Architecture/Reconciliation/
  IMMERSIVE-FRAMEWORK-ADR-018-RECONCILIATION-2026-08-11.md
Documentation~/Architecture/Tracking/
  IF-TRACK-Framework.md
```

## Files removed

None.

## Surface affected

Backend implementation and technical diagnostics only.

No new designer-facing authoring surface is introduced.

## Runtime flow

```text
public JSON operation
  -> recover pending transaction first
  -> reject committed-invalid transaction
  -> execute requested operation
```

Write:

```text
serialize staged slot
serialize staged manifest
write intent last
replay committed transaction
cleanup
```

Delete:

```text
serialize staged manifest when needed
write intent last
replay delete
cleanup
```

## Technical smoke expected

Package:

```text
compiles
existing Progression Save diagnostics smoke still passes
Stable IProgressionSaveStore signatures unchanged
```

Focused QA:

```text
normal write/delete leaves no transaction residue
write recovery before canonical apply
write recovery after slot apply
write recovery after manifest apply
delete recovery before canonical apply
delete recovery after slot delete
delete recovery after manifest apply
uncommitted staging is discarded
corrupt committed intent fails closed
corrupt staged slot fails closed
corrupt staged manifest fails closed
Write/Delete/Read do not bypass a corrupt committed transaction
```

## Technical acceptance

```text
no silent fallback
no silent partial-success state exposed through the backend API
replay is idempotent
committed invalid staging blocks access explicitly
Stable backend port remains unchanged
JSON remains Experimental until focused QA passes
```

## Product acceptance

```text
Framework still ships a simple built-in local backend
user is not forced to install another save asset
hardening does not expose transaction mechanics to gameplay consumers
advanced guarantees remain backend capabilities
```

## Architectural gain

```text
before:
slot + manifest changed sequentially with an untracked partial-state window

after:
committed operation has durable replay evidence until both canonical artifacts converge
```

## Usability gain

A basic game can use the built-in backend without accepting an undiagnosed
slot/manifest mismatch after an interrupted operation.

## Suggested commit

```text
fix(progression-save): add recoverable json commit semantics
```


## B5 certification evidence

Executed in QAFramework:

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

The previously certified backend-independence suite also remains PASS:

```text
[ADR018_QA_BACKEND_CONFORMANCE]
status='Passed'
contractCases='9'
jsonCoreCases='13'
alternateCoreCases='13'
catalogCases='5'
negativeCases='7'
alternateCatalog='False'
consumerRuntime='ProgressionSaveRuntime'
semanticFingerprint='Missing>Saved>Loaded>Saved>Loaded>Deleted>Missing>Missing'
```

## B6 disposition

```text
ADR018-B technical behavior        CERTIFIED
Built-in JSON implementation       OFFICIAL / CERTIFIED
Concrete JsonProgressionSaveStore  EXPERIMENTAL API
Catalog/manifest public model      EXPERIMENTAL
ADR018-B                           CLOSED / 100%
```

The concrete API is not promoted because ADR018-C still needs to define the intended
product composition surface.

The framework should prefer:

```text
authored backend selection
  -> typed materialization/provider
  -> scoped Progression Save runtime
  -> IProgressionSaveStore
```

rather than making direct `new JsonProgressionSaveStore(...)` the primary user flow.

## Next cut

```text
ADR018-C — Product Composition / Backend Authoring
```
