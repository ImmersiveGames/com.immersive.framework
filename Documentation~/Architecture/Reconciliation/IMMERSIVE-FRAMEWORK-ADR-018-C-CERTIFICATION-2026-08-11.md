# Immersive Framework — ADR-018-C Product Composition Certification

**Date:** 2026-08-11  
**ADR:** IF-ADR-018  
**Cut:** ADR018-C5  
**Type:** certification / reconciliation  
**Package baseline:** `79ff6ce6820263fb6a101dc0fed2f3958bf22780`  
**Package commit:** `feat(progression-save): add application backend authoring`

## Objective

Certify the ADR018-C product-composition boundary after focused QA and close the
accepted Stage A Progression Save composition work without prematurely freezing the
game-facing runtime-access API.

## Scope certified

```text
GameApplicationAsset Progression Save intent
ProgressionSaveProfile
Built-in JSON selection
Custom Provider selection
ProgressionSaveStoreProviderAsset materialization
ProgressionSaveApplicationComposition
ProgressionSaveRuntime creation
FrameworkRuntimeHost application-scope ownership
explicit failure/no-fallback semantics
```

## QA evidence

Unity terminal evidence:

```text
[ADR018_QA_PRODUCT_COMPOSITION]
status='Passed'
cases='12'
disabled='Passed'
builtIn='Passed'
custom='Passed'
negative='7/7'
noFallback='Passed'
selectionIsolation='Passed'
runtimeRequest='Passed'
composition='ProgressionSaveApplicationComposition'
```

## Certified positive behavior

```text
Disabled
  -> explicit Disabled
  -> no ProgressionSaveRuntime

BuiltInJson
  -> Ready
  -> JsonProgressionSaveStore
  -> valid BackendId

CustomProvider
  -> Ready
  -> alternate IProgressionSaveStore
  -> selected backend executes ProgressionSaveRuntime Save/Load
```

## Certified negative behavior

```text
enabled + missing Profile        Rejected
Custom Provider + missing asset  Rejected
invalid Provider configuration   Rejected before create
Provider create failure          Rejected
Provider success + null Store    Rejected
Store with invalid BackendId     Rejected
Provider exception               Rejected
```

All rejected Custom Provider paths preserve:

```text
Runtime = null
JSON fallback = absent
```

## Selection isolation

Built-in JSON selection does not invoke or validate an unselected stale Custom
Provider reference.

This proves backend intent has one active authority.

## Runtime ownership

The default framework bootstrap and QA share the canonical application composition
path:

```text
ProgressionSaveApplicationComposition.Resolve(GameApplicationAsset)
```

When configured, the resulting runtime belongs to:

```text
FrameworkRuntimeHost
  lifetime = application
```

No global singleton or service locator is introduced.

## API maturity decision

ADR018-C certification does **not** promote the product-facing composition API to
Stable.

Remain Experimental:

```text
ProgressionSaveProfile
ProgressionSaveStoreProviderAsset
ProgressionSaveApplicationComposition
ProgressionSaveRuntime
JsonProgressionSaveStore concrete API
catalog/manifest API
```

Remain Stable:

```text
IProgressionSaveStore
its certified transitive backend contract types
```

Reason:

```text
technical correctness is certified
real game-facing usability is not yet proven
```

FIRSTGAME may reveal that gameplay needs a typed receiver, binding, injection
component or another explicit access shape.

Freezing that API before real-consumer proof would turn an implementation detail into
a compatibility promise prematurely.

## Stage disposition

```text
ADR018-A
  CLOSED / CERTIFIED

ADR018-B
  CLOSED / CERTIFIED

ADR018-C
  CLOSED / CERTIFIED
  QA 12/12
  negative 7/7
  no fallback PASS

ADR018 Stage A accepted boundary
  CLOSED
  technical remaining 0%

ADR018-D
  NEXT
  FIRSTGAME real-consumer proof
```

## Next acceptance gate — ADR018-D

FIRSTGAME should prove:

```text
1. User can discover/create/configure Progression Save from Game Application.
2. Built-in JSON saves real game progression.
3. A later game launch/session can load that progression as intended.
4. Backend can be replaced by a custom/provider implementation.
5. Game-facing Progression Save request semantics do not change with the backend.
6. Scoped runtime delivery is understandable without global lookup.
7. Diagnostics are useful when the selected backend is unavailable or invalid.
```

If the real consumer requires a new binding/injection surface, prove its shape in
FIRSTGAME first and migrate the mature solution back to the package.

## Runtime/code changes in C5

None.

## Files in this certification cut

```text
EDIT Documentation~/Architecture/ADRs/
  IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md

EDIT Documentation~/Architecture/Plans/
  IF-ADR-018-PROGRESSION-SAVE-STABILIZATION-PLAN-2026-08-11.md

EDIT Documentation~/Architecture/Reconciliation/
  IMMERSIVE-FRAMEWORK-ADR-018-RECONCILIATION-2026-08-11.md

EDIT Documentation~/Architecture/Tracking/
  IF-TRACK-Framework.md

CREATE Documentation~/Architecture/Reconciliation/
  IMMERSIVE-FRAMEWORK-ADR-018-C-CERTIFICATION-2026-08-11.md
```

## Product surface affected

No new surface. This cut certifies the surface implemented by ADR018-C1-C3.

## Smoke expected

No new smoke.

The certification authority is the already-executed ADR018-C4 QA:

```text
12/12
negative 7/7
```

## Technical acceptance

```text
C4 terminal PASS
no package runtime changes
Stable backend contract unchanged
no fallback certified
scoped ownership certified
no singleton/service locator introduced
```

## Product acceptance

```text
authoring surface exists
configuration status exists
Advanced/Debug exists
Built-in vs Custom intent is explicit
FIRSTGAME usability remains a separate Stage B gate
```

## Architectural gain

The product now has one canonical authoring-to-runtime composition path while keeping
third-party backend integration behind the Stable backend port.

## Usability gain

A consumer selects storage intent rather than manually constructing backend runtime
objects.

## Suggested commit

```text
docs(architecture): certify ADR-018 product composition
```
