# Immersive Framework — ADR-018 Reconciliation

**Date:** 2026-08-11  
**ADR:** IF-ADR-018 — Progression Save Backend Independence and Persistence Domain Boundaries  
**Type:** package-to-ADR reverse reconciliation  
**Package baseline inspected:** `dbae01cf2fce27d4cd7311233e32fa1dc034e057`

## Disposition

```text
Architecture direction:        ACCEPTED / RECONCILED

Preferences:
  separate domain              CONFIRMED
  current API                  EXPERIMENTAL

Snapshot:
  separate capture domain      CONFIRMED
  current API                  EXPERIMENTAL

Progression Save:
  canonical Framework domain   CONFIRMED
  backend independence         ACCEPTED
  current API                  EXPERIMENTAL pending stabilization

Built-in JSON:
  official minimum backend     ACCEPTED AS PRODUCT DIRECTION
  current implementation       EXPERIMENTAL pending hardening/certification

Third-party backends:
  integration model            IProgressionSaveStore-compatible adapter
  consumer rewrite required    NO by architecture
```

## Important correction to the initial RA-02 interpretation

The first reverse-audit disposition treated Progression Save too conservatively as
only a dormant storage port/reference adapter.

That is not the intended architecture.

The package already contains a backend-neutral request runtime and request model in
addition to the storage port and JSON adapter.

The corrected product direction is:

```text
Framework Progression Save
  owns semantic requests/results/slot/payload contract
        ↓
replaceable backend contract
        ↓
Built-in JSON OR external/custom adapter
```

## Current package alignment

### Runtime

`ProgressionSaveRuntime` receives an `IProgressionSaveStore` explicitly.

It:

```text
validates the supplied store/backend id
accepts explicit Save / Load / Delete requests
maps backend results into ProgressionSaveRequestResult
does not discover Snapshot participants
does not schedule autosave
does not observe Route/Activity lifecycle
does not own UI
```

This aligns with the accepted backend-independence direction.

### Requests

`ProgressionSaveRequest` is backend-agnostic.

It carries semantic data:

```text
request identity
operation kind
slot identity
record identity for Save
payload for Save
display name
logical save moment
source/reason
```

It has no JSON/path/vendor knowledge.

### Built-in JSON

`JsonProgressionSaveStore` is the intended minimum first-party backend.

Its current implementation already provides local manifest/slot persistence and
format-version validation.

It intentionally does not define advanced capabilities such as encryption/cloud.

### Current stabilization concern

The current JSON backend performs multi-artifact updates.

For example, a slot write is followed by manifest update.

This creates a potential partial-state window if the second stage fails after the
first stage mutates disk.

That is acceptable as an Experimental implementation issue, but not as an unexamined
contract for the official minimum backend.

ADR018-B therefore requires explicit consistency/recovery semantics before
certification.

### Store contract review

The current `ProgressionSaveRuntime` directly needs:

```text
BackendId
WriteSlot
ReadSlot
DeleteSlot
```

The current `IProgressionSaveStore` additionally exposes:

```text
ReadManifest
WriteManifest
ContainsSlot
```

Before external compatibility is frozen, determine whether those operations are
mandatory backend semantics or should be split into a catalog/capability contract.

No interface change is made by this reconciliation.

## Maturity

Do not confuse:

```text
architecture accepted
```

with:

```text
current API frozen
```

The backend-independence architecture is accepted.

The current API remains Experimental until the contract and built-in JSON backend pass
the stabilization plan.

## No silent fallback

The built-in JSON backend may become the default authored backend.

If a project explicitly selects another backend and that backend fails, the framework
must not silently switch to JSON.

This preserves diagnosable configuration and avoids loading/saving progression in an
unexpected storage domain.

## Future product composition

A later product cut should expose explicit backend selection and materialize a scoped
`ProgressionSaveRuntime`.

No global singleton/service locator is authorized by this ADR.

## Closure of RA-02

RA-02 is closed at the architecture-definition level:

```text
domain separation              decided
Progression Save role          decided
JSON role                      decided
third-party adapter model      decided
API stabilization              planned
JSON hardening                 planned
product authoring              planned
real consumer proof            planned
```

RA-02 does not claim that Progression Save implementation is complete.

## Suggested commit

```text
docs(architecture): define progression save backend independence
```


## ADR018-A contract-shape resolution

The package cut resolves the external adapter boundary as:

```text
IProgressionSaveStore
  BackendId
  ReadSlot
  WriteSlot
  DeleteSlot
```

Optional catalog capability:

```text
IProgressionSaveCatalog
  ReadManifest
```

Removed from the public core compatibility requirement:

```text
WriteManifest
ContainsSlot
```

`JsonProgressionSaveStore` implements both core persistence and the optional catalog.

Its manifest mutation path is private/internal backend maintenance.

The former public `ProgressionSaveManifestWriteResult` surface is reduced to an
Internal implementation detail.

API maturity remains Experimental until focused alternate-backend conformance passes.


## ADR018-A certification — 2026-08-11

Focused backend conformance passed:

```text
contractCases=9
jsonCoreCases=13
alternateCoreCases=13
catalogCases=5
negativeCases=7
alternateCatalog=False
consumerRuntime=ProgressionSaveRuntime
semanticFingerprint=Missing>Saved>Loaded>Saved>Loaded>Deleted>Missing>Missing
```

Disposition:

```text
ADR018-A architecture             ACCEPTED
core backend contract             STABLE
backend independence              CERTIFIED
optional catalog split            CERTIFIED
JSON backend implementation       EXPERIMENTAL — ADR018-B
ProgressionSaveRuntime            EXPERIMENTAL
product authoring                 PENDING — ADR018-C
FIRSTGAME                         PENDING — ADR018-D
```

The Stable compatibility promise is limited to the core backend port and the
transitive semantic types required to implement it.

The catalog capability and built-in JSON backend remain outside that promise.
