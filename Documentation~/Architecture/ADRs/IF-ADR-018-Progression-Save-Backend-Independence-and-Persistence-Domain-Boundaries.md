# IF-ADR-018 — Progression Save Backend Independence and Persistence Domain Boundaries

Status: **Accepted**  
Date: 2026-08-11  
Type: architecture / product direction / package-to-ADR reverse reconciliation  
Related decisions: IF-ADR-014, IF-ADR-017  
Source finding: Package → ADR Reverse Audit RA-02

## Context

The package contains three persistence-related domains:

```text
Immersive.Framework.Preferences
Immersive.Framework.Snapshot
Immersive.Framework.ProgressionSave
```

Their separation is intentional.

The product requirement for Progression Save is:

```text
the Framework owns one canonical progression-save contract
the Framework ships one minimum built-in local backend
a game may replace that backend with a third-party or custom save system
replacing the backend must not require rewriting Framework progression-save consumers
```

The current package already contains a significant part of this direction:

```text
IProgressionSaveStore
ProgressionSaveRuntime
ProgressionSaveRequest
ProgressionSaveMoment
ProgressionSaveSlotRecord
ProgressionSaveManifest
ProgressionSavePayload
JsonProgressionSaveStore
```

The current public surfaces are still Experimental.

This ADR accepts the architectural direction but does not automatically promote the
current API shape to Stable. A dedicated stabilization cut must first prove that the
backend contract is sufficiently small, semantic and implementable by external
adapters without inheriting JSON-specific assumptions.

## Decision

### 1. Persistence domains remain separate

```text
Preferences
  small user/application preference values

Snapshot
  capture and restore representation of gameplay/runtime state

Progression Save
  canonical framework contract for durable game progression persistence
```

They may collaborate, but none is a synonym or silent fallback for another.

### 2. Preferences

Preferences owns small values such as:

```text
language
volume
accessibility choices
display preferences
control preferences
future preferred Frame Rate option
```

Preferences does not own:

```text
gameplay-state capture
Snapshot envelopes
Progression Save slots
save-game manifests
Progression Save runtime authority
```

A feature that consumes Preferences owns its semantic key, validation, default
resolution and conversion into the feature's typed runtime request.

The store only persists/retrieves the preference value.

### 3. Snapshot

Snapshot owns capture/restore contracts.

A Snapshot participant answers:

```text
what local state can I capture?
what schema/identity describes it?
how do I restore it from a supplied representation?
```

Snapshot does not choose:

```text
save backend
save slot
physical path
cloud provider
encryption provider
autosave schedule
```

Future Snapshot orchestration may aggregate participant output into a
`ProgressionSavePayload`, but that orchestration remains a separate authority.

### 4. Progression Save is the canonical framework persistence contract

Progression Save is not merely an adapter experiment.

It is the framework-owned semantic boundary that consumers use for game progression
persistence.

The target dependency direction is:

```text
game/framework feature
        ↓
Progression Save semantic request/runtime
        ↓
backend-neutral store contract
        ↓
┌────────────────────────────────────┐
│ built-in JSON backend              │
│ third-party save-system adapter    │
│ custom game backend                │
│ future cloud/encrypted backend     │
└────────────────────────────────────┘
```

Framework consumers must not depend directly on:

```text
JsonProgressionSaveStore
file paths
JsonUtility
third-party save SDKs
cloud SDKs
encryption libraries
```

### 5. Runtime request authority

The current `ProgressionSaveRuntime` shape is directionally correct:

```text
ProgressionSaveRuntime
  receives IProgressionSaveStore explicitly
  executes explicit Save / Load / Delete requests
  projects typed results
```

It must remain:

```text
backend-agnostic
lifecycle-neutral unless a later ADR explicitly composes lifecycle policy
free from Snapshot discovery
free from implicit scene lookup
free from global singleton/service-locator access
```

The exact current class/API remains Experimental until stabilization.

### 6. Backend contract

The backend contract exists to isolate Framework consumers from storage technology.

ADR018-A resolves the previously open manifest/catalog question.

The core backend contract is:

```text
IProgressionSaveStore
  BackendId
  ReadSlot
  WriteSlot
  DeleteSlot
```

This is exactly the storage surface required by `ProgressionSaveRuntime`.

Catalog projection is a separate optional capability:

```text
IProgressionSaveCatalog
  ReadManifest
```

A third-party/custom backend does not need to implement catalog support in order to
serve Progression Save Save/Load/Delete requests.

The following operations are intentionally not part of the public core contract:

```text
WriteManifest
ContainsSlot
```

Rationale:

```text
manifest mutation is backend-maintained consistency work
ContainsSlot duplicates the semantic information already represented by ReadSlot -> Missing
external adapters should implement the smallest semantic persistence surface
```

The built-in JSON backend implements both the core store and the optional read-only
catalog capability. Its physical manifest mutation remains private implementation
detail.

ADR018-A backend-conformance QA passed on 2026-08-11 and the certified core
backend contract is promoted to Stable.

The Stable set is intentionally limited to:

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

The following remain Experimental:

```text
ProgressionSaveRuntime
IProgressionSaveCatalog
manifest/catalog model
JsonProgressionSaveStore
request orchestration model
```

`ProgressionSaveSlotRecord.ToManifestEntry()` is internalized so the Stable core
record does not leak the Experimental catalog model.

### 7. Built-in JSON backend is official minimum functionality

`JsonProgressionSaveStore` is the framework's built-in minimum local backend.

Its role is:

```text
always provide a basic first-party option
allow the framework to function without purchasing another save asset
provide a reference implementation of the backend contract
support local progression persistence
```

"Minimum" explicitly does not require:

```text
encryption
anti-tamper
cloud synchronization
cross-device conflict resolution
compression
platform-native save APIs
premium backup/version history
```

Those are backend capabilities, not core requirements.

The built-in backend must still provide correct explicit persistence semantics for the
contract it claims to implement.

### 8. Minimum does not mean best-effort partial writes

An official built-in backend must not report an operation as a clean failure while
silently leaving an unreported partial state.

The current JSON implementation writes multiple physical artifacts for some
operations, such as slot content plus manifest/catalog state.

Before JSON is certified as the official minimum backend, its write/delete consistency
and recovery semantics must be explicitly hardened and tested.

This does not require enterprise-grade transactions.

It does require a documented minimum such as:

```text
staged writes
explicit commit boundary
rollback or recoverable interrupted state
corruption/incomplete-state diagnostics
no silent data substitution
```

The exact mechanism is an implementation decision for the stabilization cut.

### 9. Explicit backend selection; no silent fallback

The framework may make the built-in JSON backend the default authored selection.

That is different from silent fallback.

Valid future product behavior:

```text
Project/default configuration explicitly selects Built-in JSON
```

Valid custom behavior:

```text
Project configuration explicitly selects Custom / Third-party provider
```

Invalid behavior:

```text
custom backend fails
  -> framework silently switches to JSON
```

If a required selected backend cannot be created or used, failure must be explicit and
diagnostic.

### 10. Third-party save systems are adapters, not alternate Progression Save domains

A third-party integration implements the Framework backend contract.

Conceptually:

```text
ThirdPartyProgressionSaveStoreAdapter : IProgressionSaveStore
```

The adapter may internally use any vendor API.

The rest of the framework must not change because the adapter changes.

An adapter must preserve Framework semantics for:

```text
slot identity
read/write/delete result meaning
missing/corrupt/unavailable distinctions required by the accepted contract
payload/record identity
backend identity
```

Vendor-specific advanced capabilities may remain adapter-specific unless promoted by a
future Framework capability contract.

### 11. Current API maturity

Architecture:

```text
Progression Save backend independence
  ACCEPTED
```

Current code:

```text
ProgressionSaveRuntime and contracts
  EXPERIMENTAL pending stabilization

JsonProgressionSaveStore
  EXPERIMENTAL pending minimum-backend hardening/certification

Preferences
  EXPERIMENTAL, separate domain

Snapshot
  EXPERIMENTAL, separate domain
```

Do not mark the current API Stable merely because this ADR accepts the product
direction.

Stable promotion requires the conformance/stabilization gates defined below.

## Desired product model

When Progression Save becomes a complete authorable product, the expected shape is:

```text
Progression Save configuration/profile
  authored backend selection
        ↓
explicit backend/provider materialization
        ↓
scoped ProgressionSaveRuntime
        ↓
IProgressionSaveStore-compatible backend
        ↓
Built-in JSON OR third-party adapter
```

The exact Profile/Provider type is intentionally not defined in this ADR.

It must be designed when the authoring/composition cut is implemented.

## Future Snapshot integration

The intended relationship is:

```text
Snapshot participants
        ↓
future Snapshot orchestration
        ↓
ProgressionSavePayload
        ↓
ProgressionSaveRuntime
        ↓
IProgressionSaveStore
        ↓
selected backend
```

The selected backend never discovers gameplay Snapshot participants itself.

## Future Preferences integration

Preferences remains independent:

```text
Preferences store
  -> user preference resolution
  -> owning feature runtime request
```

For Frame Rate:

```text
Preferences
  -> Frame Rate preference resolver
  -> Session-scoped Frame Rate override
  -> Frame Rate runtime authority
```

Progression Save is not involved.

## Stabilization gates

### ADR018-A — Contract stabilization

Implementation shape:

```text
IProgressionSaveStore = core Save/Load/Delete backend port
IProgressionSaveCatalog = optional read-only manifest capability
manifest write = backend internal responsibility
ContainsSlot = not part of the public core contract
```

Current state:

```text
A1 core contract reduced                 IMPLEMENTED
A2 optional catalog capability split     IMPLEMENTED
A3 built-in JSON aligned                 IMPLEMENTED
A4 focused alternate-backend QA          CERTIFIED
A5 Stable core promotion/certification   IMPLEMENTED
ADR018-A                                 CLOSED / 100%
```

Focused QA ran the same `ProgressionSaveRuntime` request suite against:

```text
JsonProgressionSaveStore
QA in-memory store implementing only IProgressionSaveStore
```

Certified terminal evidence:

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

The alternate QA backend proved catalog support is optional and that the core
consumer semantics are backend-independent.

### ADR018-B — Built-in JSON minimum backend

Harden and certify the built-in JSON implementation.

Prove:

```text
save
load
delete
missing
corrupt
manifest/catalog consistency
interrupted/failed write behavior
no silent partial-success state
explicit diagnostics
```

### ADR018-C — Product composition

Define the designer/developer-facing backend selection surface.

Required qualities:

```text
explicit built-in JSON selection
explicit custom/third-party selection
no silent fallback
typed/scoped runtime composition
Advanced/Debug evidence of selected backend
```

### ADR018-D — Real consumer

Use FIRSTGAME when a real progression-save flow is ready to prove:

```text
basic built-in JSON use
backend replacement does not change game-facing progression-save code
authoring/configuration is understandable
```

## Out of scope for this ADR

```text
encryption implementation
cloud-save implementation
vendor-specific adapters
Snapshot orchestration implementation
autosave lifecycle policy
checkpoint policy
save-slot UI
schema migration implementation
cross-device conflict resolution
```

## Acceptance of this architecture cut

```text
Preferences remains separate from Progression Save
Snapshot remains separate from backend storage
Progression Save is recognized as canonical framework persistence contract
JSON is recognized as official built-in minimum backend
third-party systems replace the backend through adapters
no framework consumer depends on a concrete backend
no silent fallback to JSON
current Experimental APIs are not prematurely frozen
JSON consistency hardening is explicitly required before certification
```

## Suggested commits

Architecture:

```text
docs(architecture): define progression save backend independence
```

Future stabilization:

```text
refactor(progression-save): stabilize backend contract
fix(progression-save): harden built-in json backend
feat(progression-save): add explicit backend authoring
```
