# F21-ADR-SAVE-001 - Save Snapshot Preferences Progression Boundary

Status: Accepted / Closed F21H  
Phase: F21 - Save / Snapshot / Preferences / Progression Save Foundation  
Type: Framework Core + Save Module Boundary  
Last updated: 2026-06-26

---

## 1. Context

F20 closed Pause State/Gate as logical core. The next framework axis opens Save before Pause visual/gameplay work so state capture, player preferences and progression persistence have explicit contracts before overlay/input/gameplay adapters start depending on them.

F10 already decided that Snapshot is different from Reset and that Reset Baseline is not Save Snapshot. F21 keeps that separation and adds the missing persistence-side boundaries.

`NewScripts` is reference-only for concepts. F21 does not copy code, assets, configs, ProjectSettings or runtime architecture from the old project.

---

## 2. Decision

F21 is:

```text
Save / Snapshot / Preferences / Progression Save Foundation
```

F21 defines three separate ownership areas:

```text
Snapshot
Preferences
Progression Save
```

Snapshot is an envelope and participant contract boundary. It captures/restores runtime state and does not know or select a persistence backend.

Preferences is the user/application settings boundary. It is independent from progression slots and independent from gameplay snapshot capture.

Progression Save owns progression save requests, save slots, slot manifests, autosave/load moment contracts and backend port contracts. Progression Save depends on a replaceable backend port, not on a concrete storage engine.

The future JSON backend is the initial adapter only, not the canonical save contract. A future premium backend must replace JSON behind the same interface/port without changing framework-level request/adaptation code.

---

## 3. Snapshot Boundary

Snapshot may define:

```text
snapshot envelope identity
snapshot owner/scope
schema/version metadata
payload records
participant capture/restore contracts
snapshot set/collection contracts
diagnostic facts
```

Snapshot must not define:

```text
file paths
PlayerPrefs keys
JSON format as canonical contract
cloud/premium backend details
progression slot selection
autosave policy
menu/UI flow
```

---

## 4. Preferences Boundary

Preferences may define:

```text
preference keys/records
load/save result contracts
store port
PlayerPrefs adapter behind the Preferences store port
diagnostic facts
```

Preferences must not define:

```text
progression slots
save manifests
checkpoint/progression identity
snapshot participant lifecycle
gameplay progression payloads
```

---

## 5. Progression Save Boundary

Progression Save may define:

```text
save slot identity
save record identity
save slot manifest
progression save request/result
progression load request/result
autosave/load moment contracts
backend port
diagnostic facts
```

Progression Save must not define a concrete backend as the canonical contract. JSON and premium storage are adapters behind the same port.

---

## 6. F21 Plan

| Cut | Status | Objective |
|---|---|---|
| F21A | `APPLIED / DOCS ONLY` | Save / Snapshot / Preferences / Progression ADR Plan and roadmap realignment. |
| F21B | `APPLIED / PRIMITIVES` | Snapshot Envelope Primitives. |
| F21C | `APPLIED / PARTICIPANT CONTRACTS + SYNTHETIC SMOKE` | Snapshot Participant Contracts + Diagnostics Smoke. |
| F21D | `APPLIED / PREFERENCES STORE + PLAYERPREFS ADAPTER` | Preferences Store Contracts + PlayerPrefs Backend. |
| F21E | `APPLIED / PROGRESSION PORT + SLOT/MANIFEST PRIMITIVES` | Progression Save Port + Slot/Manifest Primitives. |
| F21F | `APPLIED / JSON BACKEND + DIAGNOSTICS SMOKE` | JSON Progression Backend + Diagnostics Smoke. |
| F21G | `APPLIED / RUNTIME REQUEST PATH + AUTOSAVE MOMENTS` | Progression Save Runtime Request Path + Autosave Moment Contracts. |
| F21H | `CLOSED / QA PASS + USAGE` | Closure + Usage Guide. |

---

## 7. F21B Applied Shape

F21B adds passive Snapshot envelope primitives under `Runtime/Snapshot`:

```text
SnapshotEnvelopeId
SnapshotScope
SnapshotSchemaId
SnapshotSchemaVersion
SnapshotPayloadFormat
SnapshotPayload
SnapshotEnvelope
```

The envelope shape records:

```text
envelope identity
scope
owner identity
schema identity
schema version
payload format
payload bytes or explicit empty payload
captured UTC ticks
source/reason diagnostics
```

F21B does not add participant contracts, capture execution, restore execution, backend ports, PlayerPrefs, JSON, progression slots, save manifests, autosave moments, UI, scene objects, prefabs, ScriptableObjects or asmdef changes.

---

## 8. F21C Applied Shape

F21C adds backend-agnostic Snapshot participant contracts under `Runtime/Snapshot`:

```text
SnapshotParticipantId
SnapshotParticipantRequiredness
SnapshotParticipantResultStatus
SnapshotParticipantDescriptor
SnapshotCaptureContext
SnapshotRestoreContext
SnapshotParticipantCaptureResult
SnapshotParticipantRestoreResult
ISnapshotParticipant
```

The contract shape is intentionally local and passive:

```text
participant exposes descriptor
participant captures local state into SnapshotEnvelope
participant restores local state from a provided SnapshotEnvelope
framework can validate descriptor/result shape
future orchestration may aggregate participants
future Progression Save may persist envelopes through a backend port
```

F21C also adds `Run Snapshot Participant Diagnostics Smoke` under `Show Save / Snapshot diagnostics` in the QA Canvas. The smoke is synthetic and validates:

```text
descriptor/context owner matching
required participant capture result
restore result from matching envelope
foreign envelope rejection
optional participant skip behavior
known diagnostic/runtime snapshot types remain outside canonical Save Snapshot namespace
```

F21C does not add participant discovery, Snapshot orchestration runtime, backend, PlayerPrefs, JSON, progression slots/manifests, autosave/load moments, UI, scene object, prefab, ScriptableObject or asmdef changes.

---

## 9. Snapshot Orphan / Ghost Boundary Audit

F21C makes `Immersive.Framework.Snapshot` the canonical namespace for Save Snapshot envelope and participant contracts. Earlier or parallel types that use the word `Snapshot` remain outside this canonical Save Snapshot boundary unless explicitly migrated in a future cut.

Known non-save snapshot types after F21C:

```text
PauseSnapshot
GateSnapshot
TransitionSnapshot
TransitionEffectSnapshot
ObjectEntryRuntimeContextSnapshot
CycleResetPlan.SnapshotEntries/SnapshotIssues methods
CycleResetResult.SnapshotParticipantResults/SnapshotIssues methods
```

These are diagnostic/runtime state snapshots or immutable copy helpers. They are not persistence envelopes, not participants, not backend records and not Progression Save slots.

The older F10 Snapshot ADR remains as conceptual history only. The operational canonical Save Snapshot trail is F21.

---

## 10. F21D Applied Shape

F21D adds the Preferences boundary under `Runtime/Preferences`:

```text
PreferenceKey
PreferenceValueKind
PreferenceValue
PreferenceReadStatus
PreferenceWriteStatus
PreferenceReadResult
PreferenceWriteResult
IPreferencesStore
PlayerPrefsPreferencesStore
```

The contract shape is intentionally separate from Snapshot and Progression Save:

```text
Preferences stores user/application settings only
Preferences keys are storage-agnostic identities
IPreferencesStore is the framework port
PlayerPrefsPreferencesStore is one adapter behind that port
PlayerPrefs physical keys are not canonical PreferenceKey values
PlayerPrefs reads use explicit value-kind markers to prevent silent fallback
```

F21D also adds `Run Preferences Store Diagnostics Smoke` under `Show Save / Snapshot diagnostics` in the QA Canvas. The smoke writes and deletes namespaced QA PlayerPrefs keys and validates:

```text
PreferenceKey domain
typed PreferenceValue primitives
write/read for string, float and bool
missing key result
type mismatch result
PlayerPrefs type-marker guard for orphaned/unmarked keys
delete cleanup
canonical boundary excludes Snapshot, Progression Slot, JSON and UI
```

F21D does not add Snapshot backend usage, Progression Save slots/manifests, JSON, autosave/load moments, runtime save request path, UI, scene object, prefab, ScriptableObject or asmdef changes.


## 11. F21E Applied Shape

F21E adds the Progression Save boundary under `Runtime/ProgressionSave`:

```text
ProgressionSaveSlotId
ProgressionSaveRecordId
ProgressionSaveBackendId
ProgressionSavePayloadFormat
ProgressionSavePayload
ProgressionSaveSlotRecord
ProgressionSaveManifestEntry
ProgressionSaveManifest
ProgressionSaveReadStatus
ProgressionSaveWriteStatus
ProgressionSaveDeleteStatus
ProgressionSaveReadResult
ProgressionSaveWriteResult
ProgressionSaveDeleteResult
ProgressionSaveManifestReadResult
ProgressionSaveManifestWriteResult
IProgressionSaveStore
```

The contract shape is intentionally backend-port first:

```text
ProgressionSaveSlotId is a logical slot id, not a file path or UI label
ProgressionSaveRecordId identifies the stored record behind a slot
ProgressionSaveManifest and ProgressionSaveManifestEntry are metadata only
ProgressionSavePayload carries bytes without selecting JSON or another serializer
IProgressionSaveStore is the replaceable framework port
concrete JSON/cloud/encrypted/premium backends must sit behind the port
```

F21E adds `FrameworkIdentityDomain.ProgressionSave`. It does not add a concrete backend, JSON serialization, file paths, PlayerPrefs, autosave/load moments, runtime save request path, UI, scene object, prefab, ScriptableObject or asmdef changes.

Snapshot remains capture/restore envelope and participant contracts. Preferences remains user/application settings. Progression Save owns slots, manifest metadata and backend port shape.

## 12. F21F Applied Shape

F21F adds the initial concrete Progression Save backend adapter under `Runtime/ProgressionSave`:

```text
JsonProgressionSaveStore
```

It implements `IProgressionSaveStore` and stores records/manifests as JSON files. This is an adapter detail, not the canonical contract. Physical file paths, manifest file names, slot file names and JSON DTOs are not framework identities and must not be consumed by gameplay/framework request paths.

F21F also adds `Run Progression Save JSON Backend Smoke` under the Save / Snapshot diagnostics group. The smoke validates:

```text
backend id and adapter boundary
missing manifest and missing slot status
slot write/read roundtrip
manifest entry update
corrupt slot detection
delete cleanup
canonical boundary excludes Snapshot, Preferences, PlayerPrefs, runtime request path and UI
```

F21F does not add autosave/load moments, runtime save request path, UI, scene object, prefab, ScriptableObject, PlayerPrefs or asmdef changes.

## 13. F21G Applied Shape

F21G adds the Progression Save runtime request path under `Runtime/ProgressionSave`:

```text
ProgressionSaveRequestId
ProgressionSaveMomentId
ProgressionSaveRequestKind
ProgressionSaveMomentKind
ProgressionSaveRequestStatus
ProgressionSaveMoment
ProgressionSaveRequest
ProgressionSaveRequestResult
ProgressionSaveRuntime
```

`ProgressionSaveRuntime` is explicit and store-backed. It executes `Save`, `Load` and `Delete` requests against an injected `IProgressionSaveStore`. The concrete JSON store remains replaceable behind the port.

`ProgressionSaveMoment` is a passive contract for manual, autosave, checkpoint and lifecycle-boundary save intent. It does not schedule autosave, observe Route/Activity lifecycle, or call a backend by itself.

F21G adds `Run Progression Save Runtime Request Smoke` under the Save / Snapshot diagnostics group. The smoke validates:

```text
request/moment contracts
manual save request
load request
passive autosave moment contract
missing load request
delete cleanup
canonical boundary separation
```

F21G does not add Snapshot capture orchestration, Preferences usage, PlayerPrefs usage, autosave scheduler, Route/Activity lifecycle hook, UI, scene object, prefab, ScriptableObject or asmdef changes.

## 14. F21H Closure

F21H closes the phase after the F21G runtime request smoke passed. The closure adds `Documentation~/Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md` and does not add runtime code, backend behavior, UI, scene objects, prefabs, ScriptableObjects or asmdef changes.

Closed F21 evidence:

```text
Run Snapshot Participant Diagnostics Smoke: PASS
Run Preferences Store Diagnostics Smoke: PASS
Run Progression Save JSON Backend Smoke: PASS
Run Progression Save Runtime Request Smoke: PASS
```

The next phase is F22 Loading Operation / Progress / Readiness Boundary.

---

## 15. Excluded in F21A

F21A does not implement:

```text
runtime code
backend
PlayerPrefs
JSON
UI
scene object
prefab
ScriptableObject
asmdef changes
Unity object setup
save runtime request path
autosave execution
```

---

## 16. Consequences

Save work can evolve without binding Snapshot to JSON, PlayerPrefs or premium storage.

Preferences can remain a lightweight settings boundary and cannot accidentally consume progression slots.

Progression Save now has an initial JSON adapter while preserving the ability to replace the backend behind the same port.

Pause visual/content/input moves to F23/F27. F24 is Unity Build Surface / Lifecycle Wiring. Adapter Module Foundation is deferred to the F28/F29 planning spine after Activity operation stability.

---

## 17. Next Cut

```text
F22A - Loading Architecture ADR Plan
```
