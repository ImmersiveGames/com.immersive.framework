# Immersive Framework — RA-03 Object Entry Ownership Reconciliation

**Date:** 2026-08-11  
**Type:** package reverse audit / normative ownership reconciliation  
**Package baseline inspected:** `cd4ac4adff3ebd013126345b3d29f8d115b6b432`  
**Historical source:** `F13-ADR-OBJECT-001 — Object Entry Foundation`

## Objective

Resolve the architectural ownership of the current `Runtime/ObjectEntry` package
surface without creating a new feature authority or prematurely deleting
Experimental APIs.

## Scope

```text
ObjectEntry stable identity ownership
authored declaration semantics
runtime-context ownership
relationship to Reset
historical F13 disposition
API-maturity disposition
tracking/reconciliation
```

## Out of scope

```text
new Object Entry runtime behavior
new Composer/Profile/Wizard
physical GameObject binding
spawn/materialization
Reset implementation changes
Player/Actor integration
new Object Entry readiness model
FIRSTGAME proof
deleting Experimental request/result APIs
new ADR number
```

## Evidence reviewed

### Historical F13

The historical F13 decision defined Object Entry as a passive logical catalog with:

```text
typed ObjectEntryId
scope and typed ownership
passive authored declaration
immutable descriptor/set projection
runtime snapshot controlled by FrameworkRuntimeHost
```

It explicitly excluded:

```text
physical GameObject/Transform/Component binding
Reset execution
spawn/materialization
public mutable registry
service locator
```

F13 also stated that the Runtime Host snapshot is not a live registry, physical
binding or reset inventory.

### Current package declaration source

The current `ObjectEntryDeclarationSource` still documents itself as passive and
explicitly states that it does not:

```text
bind GameObjects
materialize prefabs
spawn actors
register services
perform reset
create lifecycle authority
```

The current implementation therefore remains directionally aligned with F13.

### Current lifecycle integration

`FrameworkRuntimeHost` owns lifecycle state and derives the current Object Entry
runtime-context snapshot from Route/Activity scope.

Object Entry context is refreshed/invalidated as lifecycle authority changes.

Therefore:

```text
lifecycle authority
  -> produces Object Entry context

Object Entry context
  != produces lifecycle authority
```

### Reset relationship

Reset may use Object Entry scope/owner metadata to constrain collection to the
current valid lifecycle context.

That does not make Object Entry the Reset authority.

Reset execution, participant behavior, single-flight semantics and result authority
remain under IF-ADR-005 and the current lifecycle occurrence remains under
IF-ADR-001.

## Ownership decision

### ADR-014 owns stable Object Entry identity semantics

```text
ObjectEntryId
ObjectEntryDeclaration identity metadata
ObjectEntryDescriptor stable semantic identity
duplicate identity rejection
diagnostic-vs-functional identity distinction
```

Object Entry is an object-centric semantic addressing primitive.

It is not a replacement for authored Route/Activity definition identity.

### ADR-001 owns runtime lifecycle authority

```text
Game Application
Session
Route
Activity
runtime occurrence
lifetime
```

`ObjectEntryRuntimeContextSnapshot` is a derivative read-only projection of that
authority.

It cannot select an arbitrary active owner, keep an owner alive or replace current
occurrence identity.

### ADR-005 is a downstream consumer

Reset can consume scoped Object Entry metadata.

This is dependency, not ownership.

```text
ADR-001 lifecycle authority
        ↓
Object Entry scoped projection
        ↓
ADR-005 Reset selection/collection
```

## Historical F13 disposition

Do not revive F13 as a parallel active ADR.

Its accepted architectural content is absorbed into the current normative model:

```text
F13 stable/passive identity semantics
  -> IF-ADR-014

F13 lifecycle-owned runtime snapshot
  -> IF-ADR-001

F13 downstream Reset use
  -> IF-ADR-005
```

This closes the "orphan ownership" finding without adding another authority layer.

## API hygiene observation

`ObjectEntryRequest` remains Experimental and its source documentation states that
the request is not executed by the original F13 foundation.

`ObjectEntryResult` is likewise an Experimental request-shaped/result-shaped surface.

RA-03 does not remove or promote these types.

Reason:

```text
ownership is now resolved
but code-reference evidence from the available repository search is not reliable
enough to prove that the public types are unconsumed
```

Their disposition is moved to RA-04 Architecture Governance Hygiene, where API
status/public-surface necessity can be audited explicitly.

## Product-surface decision

No new Object Entry product layer is justified by this reconciliation.

Do not create:

```text
Object Entry Profile
Object Entry Composer
Object Entry Wizard
Apply/Rebuild flow
Object Entry Manager
global runtime browser as primary UX
```

The existing declaration is passive authored metadata. Advanced technical evidence
belongs diagnostics rather than a new designer-facing system.

## Files changed by RA-03

```text
EDIT Documentation~/Architecture/ADRs/
  IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md

EDIT Documentation~/Architecture/ADRs/
  IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md

EDIT Documentation~/Architecture/Tracking/
  IF-TRACK-Framework.md

CREATE Documentation~/Architecture/Reconciliation/
  IMMERSIVE-FRAMEWORK-RA-03-OBJECT-ENTRY-OWNERSHIP-RECONCILIATION-2026-08-11.md
```

## Runtime files changed

None.

## Technical smoke expected

No new focused smoke.

RA-03 changes no runtime behavior and makes no new technical promise.

Ordinary package compilation/import remains sufficient for this documentation-only
cut.

## Technical acceptance

```text
Object Entry is not a lifecycle authority
Object Entry stable identity has a normative owner
runtime context is explicitly derivative
Reset relationship is explicit
no new global authority
no silent fallback
no runtime contract changed
Experimental request/result types are not promoted accidentally
```

## Product acceptance

```text
no artificial Composer/Profile is introduced
declaration remains passive authored metadata
ownership is understandable from current ADRs
debug/diagnostics remain technical rather than primary authoring UX
```

## Architectural gain

Removes an orphaned historical feature boundary and maps Object Entry to the current
authority model without duplicating lifecycle ownership.

## Usability gain

Prevents future consumers from treating Object Entry as a manager/registry that must
be configured or discovered globally.

## Status

```text
RA-CUT-03
  CLOSED / RECONCILED

runtime change
  NONE

focused QA
  NOT REQUIRED

new ADR
  NOT REQUIRED

next
  RA-CUT-04 — Architecture Governance Hygiene
```

## Suggested commit

```text
docs(architecture): reconcile ObjectEntry ownership
```
