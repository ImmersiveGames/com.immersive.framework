# Progression Save — Built-in JSON Backend

Status: **Official built-in minimum backend — ADR018-B technically certified**  
Backend: `JsonProgressionSaveStore`  
Concrete API maturity: **Experimental pending ADR018-C product composition**

## Purpose

The Framework ships a minimum local Progression Save backend so a game can persist
basic progression without requiring a third-party save asset.

The built-in backend is intentionally simple:

```text
local files
JSON metadata/records
manifest/catalog
multiple logical slots
Save / Load / Delete
explicit Missing / Corrupt / Failed states
```

It intentionally does not provide:

```text
encryption
anti-tamper
cloud synchronization
cross-device conflict resolution
compression
database transactions
multi-process write coordination
```

## Consistency model

A slot operation may need to update more than one physical artifact:

```text
slot JSON
manifest JSON
```

ADR018-B uses a recoverable transaction intent.

### Prepare

The backend writes transaction staging below:

```text
<root>/.transaction/
  slot.stage.json       # Write only
  manifest.stage.json   # when manifest changes
  intent.json           # written LAST
```

`intent.json` is the commit boundary.

Staging without a committed intent is not authoritative and may be discarded.

### Commit / recovery

Once `intent.json` exists, the backend validates all required staged data before
canonical mutation.

A Write transaction then converges to:

```text
canonical slot = staged slot
canonical manifest = staged manifest
```

A Delete transaction then converges to:

```text
canonical slot absent
canonical manifest = staged manifest, when required
```

The replay is idempotent.

If the process stops between the physical steps, the next public JSON backend
operation attempts recovery before exposing state.

## Fail-closed behavior

A committed transaction with an invalid intent, corrupt staged slot or corrupt staged
manifest blocks normal operations.

The backend returns typed `Failed` evidence and leaves the committed transaction for
diagnosis/recovery rather than silently using potentially inconsistent canonical data.

## Cleanup

After canonical state is consistent, the transaction directory is removed.

If cleanup alone fails, canonical state remains valid and the next operation may replay
the same transaction and retry cleanup.

## Concurrency

The built-in backend serializes JSON I/O inside the current process.

It does not claim safe concurrent writes from multiple operating-system processes to
the same root directory.

A game requiring that guarantee should supply a backend whose storage technology
supports it.

## Product boundary

Gameplay/framework consumers still depend on:

```text
IProgressionSaveStore
```

not on transaction files or JSON paths.

The transaction layout is an implementation detail of the official built-in backend.


## Certification

ADR018-B focused QA passed:

```text
18/18 recovery cases
Write recovery 3/3
Delete recovery 3/3
Fail-closed 6/6
Idempotent replay Passed
No transaction residue
```

This backend is an official first-party option.

Direct construction remains a technical integration path, not the final preferred
designer/product authoring flow. ADR018-C owns that surface.
