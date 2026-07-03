# F21 — Save / Snapshot / Preferences / Progression Usage

> Status: `F21 CLOSED / F21H QA PASS + USAGE`  
> Scope: framework Save foundation only.

F21 defines the canonical Save foundation as three separate boundaries:

```text
Snapshot
Preferences
Progression Save
```

This phase deliberately does not create a save menu, save UI, scene object setup, prefab setup, ScriptableObject setup, Route/Activity autosave hook, Snapshot orchestration runtime or gameplay adapter.

---

## 1. What F21 provides

### Snapshot

Snapshot is the runtime state capture/restore contract. It is backend-agnostic.

```text
Runtime/Snapshot/SnapshotEnvelopeId
Runtime/Snapshot/SnapshotScope
Runtime/Snapshot/SnapshotSchemaId
Runtime/Snapshot/SnapshotSchemaVersion
Runtime/Snapshot/SnapshotPayloadFormat
Runtime/Snapshot/SnapshotPayload
Runtime/Snapshot/SnapshotEnvelope
Runtime/Snapshot/SnapshotParticipantId
Runtime/Snapshot/SnapshotParticipantRequiredness
Runtime/Snapshot/SnapshotParticipantResultStatus
Runtime/Snapshot/SnapshotParticipantDescriptor
Runtime/Snapshot/SnapshotCaptureContext
Runtime/Snapshot/SnapshotRestoreContext
Runtime/Snapshot/SnapshotParticipantCaptureResult
Runtime/Snapshot/SnapshotParticipantRestoreResult
Runtime/Snapshot/ISnapshotParticipant
```

Diagnostics:

```text
Show Save / Snapshot diagnostics
  Run Snapshot Participant Diagnostics Smoke
```

### Preferences

Preferences are user/application settings. They are not gameplay progression and do not use progression slots.

```text
Runtime/Preferences/PreferenceKey
Runtime/Preferences/PreferenceValueKind
Runtime/Preferences/PreferenceValue
Runtime/Preferences/PreferenceReadStatus
Runtime/Preferences/PreferenceWriteStatus
Runtime/Preferences/PreferenceReadResult
Runtime/Preferences/PreferenceWriteResult
Runtime/Preferences/IPreferencesStore
Runtime/Preferences/PlayerPrefsPreferencesStore
```

Diagnostics:

```text
Show Save / Snapshot diagnostics
  Run Preferences Store Diagnostics Smoke
```

### Progression Save

Progression Save is the playable progression persistence boundary. It owns logical slots, manifests, request contracts and a replaceable store port.

```text
Runtime/ProgressionSave/ProgressionSaveSlotId
Runtime/ProgressionSave/ProgressionSaveRecordId
Runtime/ProgressionSave/ProgressionSaveBackendId
Runtime/ProgressionSave/ProgressionSavePayloadFormat
Runtime/ProgressionSave/ProgressionSavePayload
Runtime/ProgressionSave/ProgressionSaveSlotRecord
Runtime/ProgressionSave/ProgressionSaveManifestEntry
Runtime/ProgressionSave/ProgressionSaveManifest
Runtime/ProgressionSave/ProgressionSaveReadStatus
Runtime/ProgressionSave/ProgressionSaveWriteStatus
Runtime/ProgressionSave/ProgressionSaveDeleteStatus
Runtime/ProgressionSave/ProgressionSaveReadResult
Runtime/ProgressionSave/ProgressionSaveWriteResult
Runtime/ProgressionSave/ProgressionSaveDeleteResult
Runtime/ProgressionSave/ProgressionSaveManifestReadResult
Runtime/ProgressionSave/ProgressionSaveManifestWriteResult
Runtime/ProgressionSave/IProgressionSaveStore
Runtime/ProgressionSave/JsonProgressionSaveStore
Runtime/ProgressionSave/ProgressionSaveRequestId
Runtime/ProgressionSave/ProgressionSaveMomentId
Runtime/ProgressionSave/ProgressionSaveRequestKind
Runtime/ProgressionSave/ProgressionSaveMomentKind
Runtime/ProgressionSave/ProgressionSaveRequestStatus
Runtime/ProgressionSave/ProgressionSaveMoment
Runtime/ProgressionSave/ProgressionSaveRequest
Runtime/ProgressionSave/ProgressionSaveRequestResult
Runtime/ProgressionSave/ProgressionSaveRuntime
```

Diagnostics:

```text
Show Save / Snapshot diagnostics
  Run Progression Save JSON Backend Smoke
  Run Progression Save Runtime Request Smoke
```

---

## 2. What F21 does not provide

F21 intentionally does not create:

```text
save menu
load menu
save slot UI
pause menu integration
loading screen integration
scene object setup
prefab setup
ScriptableObject authoring
Route/Activity autosave hook
autosave scheduler
Snapshot participant discovery
Snapshot aggregation/orchestration runtime
Snapshot-to-ProgressionSave bridge
Time.timeScale behavior
gameplay adapters
service locator
singleton/global save manager
```

The current save runtime is explicit and local to the caller. A future adapter may decide when Route/Activity lifecycle should call save/load, but F21 does not make that decision.

---

## 3. Boundary rules

Keep these rules stable when adding future Save work:

```text
Snapshot captures/restores state. It does not persist files.
Preferences stores settings. It does not use progression slots.
Progression Save stores playable progress. It does not discover gameplay state by itself.
PlayerPrefs is only a Preferences adapter.
JSON is only the initial Progression Save backend adapter.
IProgressionSaveStore is the backend replacement port.
ProgressionSaveMoment is a passive descriptor, not a scheduler.
```

Known runtime/diagnostic snapshot types remain outside the Save Snapshot namespace:

```text
PauseSnapshot
GateSnapshot
TransitionSnapshot
TransitionEffectSnapshot
ObjectEntryRuntimeContextSnapshot
CycleReset immutable copy helpers
```

These are not persistence envelopes and must not be treated as Save Snapshot participants unless a future migration cut explicitly changes that.

---

## 4. Using Snapshot participants

Current Snapshot usage is contract-level. There is no participant discovery or orchestrator yet.

A participant should expose stable descriptor data, capture local state into a `SnapshotEnvelope`, and restore from an envelope provided by the caller.

```csharp
using Immersive.Framework.Identity;
using Immersive.Framework.Snapshot;

public sealed class ExampleSnapshotParticipant : ISnapshotParticipant
{
    private readonly FrameworkIdentityKey activityOwner = FrameworkIdentityKey.From(
        FrameworkIdentityDomain.Activity,
        "example.activity.owner");

    public SnapshotParticipantDescriptor GetSnapshotDescriptor()
    {
        return SnapshotParticipantDescriptor.Required(
            SnapshotParticipantId.From("example.player.stats"),
            SnapshotScope.Activity,
            activityOwner,
            SnapshotSchemaId.From("example.player.stats.schema"),
            SnapshotSchemaVersion.Initial(),
            order: 0,
            displayName: "Player Stats",
            source: "ExampleSnapshotParticipant",
            reason: "Example usage");
    }

    public SnapshotParticipantCaptureResult CaptureSnapshot(SnapshotCaptureContext context)
    {
        var descriptor = GetSnapshotDescriptor();
        if (!descriptor.SupportsCaptureContext(context))
        {
            return SnapshotParticipantCaptureResult.RejectedResult(
                descriptor,
                "Capture context does not match this participant.");
        }

        var payload = SnapshotPayload.FromText("{\"hp\":10}", "application/json");
        var envelope = SnapshotEnvelope.Create(
            "example.player.stats.envelope",
            descriptor.Scope,
            descriptor.OwnerIdentity,
            "example.player.stats.schema",
            descriptor.SchemaVersion,
            payload,
            context.CapturedUtcTicks,
            context.Source,
            context.Reason);

        return SnapshotParticipantCaptureResult.CapturedResult(
            descriptor,
            envelope,
            "Captured example state.");
    }

    public SnapshotParticipantRestoreResult RestoreSnapshot(SnapshotRestoreContext context)
    {
        var descriptor = GetSnapshotDescriptor();
        if (!descriptor.SupportsEnvelope(context.Envelope))
        {
            return SnapshotParticipantRestoreResult.RejectedResult(
                descriptor,
                "Envelope does not match this participant.");
        }

        // Decode context.Envelope.Payload and apply local state here.
        return SnapshotParticipantRestoreResult.RestoredResult(
            descriptor,
            context.Envelope,
            "Restored example state.");
    }
}
```

Do not save the envelope from inside `CaptureSnapshot`. Persistence belongs to Progression Save or another future backend adapter.

---

## 5. Using Preferences

Use `IPreferencesStore` as the boundary. `PlayerPrefsPreferencesStore` is the current built-in adapter.

```csharp
using Immersive.Framework.Preferences;

var store = new PlayerPrefsPreferencesStore("my.game.preferences");
var volumeKey = PreferenceKey.From("audio.master.volume");

var write = store.Write(volumeKey, PreferenceValue.FromFloat(0.8f));
store.Flush();

var read = store.Read(volumeKey, PreferenceValueKind.Float);
if (read.Found && read.Value.TryGetFloat(out var volume))
{
    // Apply volume to your options UI or audio adapter.
}
```

`PlayerPrefsPreferencesStore` writes a type marker beside each value. A missing marker or mismatched expected kind returns `TypeMismatch` instead of silently returning a default value.

Use Preferences for settings such as:

```text
language
fullscreen
master volume
subtitles/accessibility toggles
control sensitivity options
```

Do not use Preferences for:

```text
current level
checkpoint
inventory
quest progress
Snapshot envelopes
progression slots
```

---

## 6. Using Progression Save store directly

Use `IProgressionSaveStore` when you are testing backend behavior or writing a backend adapter. Most runtime callers should prefer `ProgressionSaveRuntime`.

```csharp
using System.IO;
using Immersive.Framework.ProgressionSave;
using UnityEngine;

var root = Path.Combine(Application.temporaryCachePath, "ExampleProgressionSave");
IProgressionSaveStore store = new JsonProgressionSaveStore(
    root,
    ProgressionSaveBackendId.From("json.example"));

var slot = ProgressionSaveSlotId.From("slot.primary");
var record = new ProgressionSaveSlotRecord(
    slot,
    ProgressionSaveRecordId.From("record.primary"),
    ProgressionSavePayload.FromText("{\"level\":1}", "application/json"),
    System.DateTime.UtcNow.Ticks,
    System.DateTime.UtcNow.Ticks,
    "Primary Slot",
    "Example",
    "Manual test");

var write = store.WriteSlot(record);
var read = store.ReadSlot(slot);
```

The physical path is an adapter detail. Do not expose it as gameplay identity or slot identity.

---

## 7. Using ProgressionSaveRuntime

`ProgressionSaveRuntime` executes explicit save/load/delete requests against an injected `IProgressionSaveStore`.

```csharp
using System.IO;
using Immersive.Framework.ProgressionSave;
using UnityEngine;

var store = new JsonProgressionSaveStore(
    Path.Combine(Application.temporaryCachePath, "ExampleRuntimeSave"),
    ProgressionSaveBackendId.From("json.runtime.example"));

var runtime = new ProgressionSaveRuntime(store);
var slot = ProgressionSaveSlotId.From("slot.primary");
var moment = ProgressionSaveMoment.Manual(
    "manual.save.button",
    "ExampleSaveButton",
    "User requested save");

var saveRequest = ProgressionSaveRequest.Save(
    "request.save.primary",
    slot,
    ProgressionSaveRecordId.From("record.primary"),
    ProgressionSavePayload.FromText("{\"level\":1}", "application/json"),
    "Primary Slot",
    moment,
    "ExampleSaveButton",
    "Manual save");

var saveResult = runtime.Request(saveRequest);
```

Load:

```csharp
var loadResult = runtime.Request(
    ProgressionSaveRequest.Load(
        "request.load.primary",
        slot,
        ProgressionSaveMoment.Manual("manual.load.button", "ExampleLoadButton", "User requested load"),
        "ExampleLoadButton",
        "Manual load"));
```

Delete:

```csharp
var deleteResult = runtime.Request(
    ProgressionSaveRequest.Delete(
        "request.delete.primary",
        slot,
        ProgressionSaveMoment.Manual("manual.delete.button", "ExampleDeleteButton", "User requested delete"),
        "ExampleDeleteButton",
        "Manual delete"));
```

Autosave moments are passive:

```csharp
var autosaveMoment = ProgressionSaveMoment.Autosave(
    "autosave.activity.complete",
    "ExampleActivityAdapter",
    "Activity completed");
```

Creating this moment does not schedule or run autosave. A future lifecycle adapter must explicitly decide when to create and submit the request.

---

## 8. Replacing the JSON backend later

A premium, encrypted, cloud or platform backend should implement:

```text
IProgressionSaveStore
```

The replacement must preserve the same high-level behavior:

```text
valid BackendId
ReadManifest / WriteManifest
ContainsSlot
ReadSlot / WriteSlot / DeleteSlot
explicit Missing / Corrupt / Rejected / BackendUnavailable / Failed statuses
no fallback by file name, path or UI label
```

Framework request code should keep depending on:

```text
ProgressionSaveRuntime
IProgressionSaveStore
ProgressionSaveRequest
ProgressionSaveRequestResult
```

It should not depend on `JsonProgressionSaveStore` unless the cut is specifically testing or configuring the JSON adapter.

---

## 9. QA usage

Open the QA Canvas and expand:

```text
Show Save / Snapshot diagnostics
```

Run these F21 smokes:

```text
Run Snapshot Participant Diagnostics Smoke
Run Preferences Store Diagnostics Smoke
Run Progression Save JSON Backend Smoke
Run Progression Save Runtime Request Smoke
```

Recommended regression smoke:

```text
Run Standard Smoke
```

---

## 10. Expected smoke evidence

### Snapshot Participant Diagnostics Smoke

Expected steps:

```text
descriptor-context
capture
restore
foreign-envelope-rejected
optional-skip
canonical-boundary
```

This validates Snapshot participant contracts without backend, PlayerPrefs, progression slot, JSON or UI.

### Preferences Store Diagnostics Smoke

Expected steps:

```text
contracts
write-read
missing
type-mismatch
playerprefs-type-marker-guard
delete-cleanup
canonical-boundary
```

This validates Preferences and PlayerPrefs adapter behavior only. PlayerPrefs is not Progression Save.

### Progression Save JSON Backend Smoke

Expected steps:

```text
contracts
missing
write-read
manifest
corrupt-slot
delete-cleanup
canonical-boundary
```

This validates the initial JSON adapter behind `IProgressionSaveStore`. JSON and physical paths are adapter details.

### Progression Save Runtime Request Smoke

Expected steps:

```text
contracts
manual-save-request
load-request
autosave-moment-contract
missing-load-request
delete-request-cleanup
canonical-boundary
```

This validates explicit save/load/delete requests and passive save moments. It does not validate autosave scheduling or Route/Activity lifecycle hooks because those do not exist in F21.

---

## 11. Screenshot placeholders

Use these placeholders when expanding the designer manual later:

```text
[SCREENSHOT: QA Canvas with Show Save / Snapshot diagnostics expanded]
[SCREENSHOT: Console log showing Snapshot Participant Diagnostics Smoke completed]
[SCREENSHOT: Console log showing Preferences Store Diagnostics Smoke completed]
[SCREENSHOT: Console log showing Progression Save JSON Backend Smoke completed]
[SCREENSHOT: Console log showing Progression Save Runtime Request Smoke completed]
```

---

## 12. Next phase

F21 is closed. The next phase is:

```text
F22A — Loading Architecture ADR Plan
```

F22 must define Loading as operation/progress/readiness reporting. It must not replace SceneLifecycle, Transition Effects, fade/curtain adapters or visual loading screen authoring.
