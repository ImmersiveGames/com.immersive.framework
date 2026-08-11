# Progression Save Backend Adapter Contract

Status: ADR018-A implementation candidate  
API maturity: Experimental until focused conformance QA passes

## Purpose

A game may replace the Framework's built-in JSON persistence backend without changing
its Progression Save consumer code.

Framework consumers depend on:

```text
ProgressionSaveRuntime
  -> IProgressionSaveStore
```

They do not depend on a concrete storage technology.

## Core backend contract

A custom or third-party adapter implements:

```csharp
public interface IProgressionSaveStore
{
    ProgressionSaveBackendId BackendId { get; }

    ProgressionSaveReadResult ReadSlot(ProgressionSaveSlotId slotId);

    ProgressionSaveWriteResult WriteSlot(ProgressionSaveSlotRecord record);

    ProgressionSaveDeleteResult DeleteSlot(ProgressionSaveSlotId slotId);
}
```

This is the minimum storage contract used by `ProgressionSaveRuntime`.

## Optional catalog capability

A backend that can project/list its known slots may additionally implement:

```csharp
public interface IProgressionSaveCatalog
{
    ProgressionSaveManifestReadResult ReadManifest();
}
```

Catalog support is optional.

A backend must not be rejected merely because it does not implement
`IProgressionSaveCatalog`.

## Why manifest writing is not public

The manifest/catalog represents backend-maintained evidence.

Consumers must not mutate it independently from slot persistence because that can
create disagreement between slot data and catalog data.

The built-in JSON backend therefore owns its physical manifest mutation internally.

## Adapter responsibilities

An adapter must preserve Framework semantics for:

```text
BackendId
slot identity
record identity
payload bytes/format
Found / Missing / Corrupt / BackendUnavailable / Failed / Rejected
Written / BackendUnavailable / Failed / Rejected
Deleted / Missing / BackendUnavailable / Failed / Rejected
```

Expected storage/runtime conditions should be returned through typed results.

Programmer-contract violations such as invalid Framework identifiers may throw.

## Third-party example shape

```csharp
public sealed class VendorProgressionSaveStoreAdapter
    : IProgressionSaveStore
{
    // Translate Framework slot/record/payload semantics
    // into the vendor SDK and translate results back.
}
```

The adapter may internally use any vendor API.

Framework gameplay code must not know which vendor is active.

## Built-in JSON

`JsonProgressionSaveStore` implements:

```text
IProgressionSaveStore
IProgressionSaveCatalog
```

It is the built-in minimum local backend.

Its physical manifest write path remains private backend implementation detail.

ADR018-B separately covers JSON write/delete consistency and recovery hardening.

## No fallback

If a project explicitly selects a custom backend and it is unavailable, return/fail
explicitly according to the owning product contract.

Do not silently switch to the built-in JSON backend.
