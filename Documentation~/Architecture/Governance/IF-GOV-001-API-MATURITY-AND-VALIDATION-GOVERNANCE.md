# IF-GOV-001 — API Maturity and Validation Governance

Status: **Active**  
Date: 2026-08-11  
Classification: **Cross-cutting architecture governance**  
Feature authority created: **None**  
Primary related decision: **IF-ADR-010 — Editor and Inspector Product Surface Authority**

## 1. Purpose

This record makes two already-existing framework-wide policies explicit:

1. API maturity metadata expressed by `FrameworkApiStatus` and
   `FrameworkApiStatusAttribute`.
2. Product authoring diagnostic strictness expressed by
   `FrameworkValidationMode` and `FrameworkValidationModePolicy`.

These concerns are governance primitives. They do not justify a new feature ADR,
runtime service, manager, singleton, Composer or authoring authority.

## 2. API maturity metadata

`FrameworkApiStatusAttribute` is source-level documentation/validation metadata.
It does not alter runtime behavior.

The canonical categories are:

| Status | Contract |
|---|---|
| `Stable` | May be consumed by games and external modules. Breaking changes require an ADR/migration decision. |
| `Experimental` | Available for controlled development use. Shape may change without compatibility guarantees. |
| `Internal` | Framework implementation detail. Game code must not depend on it. |
| `Deferred` | Retained for planning/frozen source; not part of the active baseline. |
| `DevelopmentTooling` | QA, Editor or development tooling; not product API. |
| `Removed` | Removed or scheduled for removal. It is not an active consumer contract. |

### Governance rule

API status describes **maturity and compatibility expectations**. It does not
create runtime authority, instantiate services, select implementations or change
execution semantics by itself.

A surface marked `Stable` must not be broken merely by changing its attribute.
The architectural/API change and its migration consequence must be handled
explicitly.

## 3. Validation mode ownership

`FrameworkValidationMode` is a Stable product-authoring policy used by the
application/route/activity validation path. It belongs to ADR-010's product
surface and diagnostics boundary.

It is not a separate validation architecture. The existing authoring validation
infrastructure remains canonical.

The exact policy is:

| Mode | Required configuration | Warnings | Info diagnostics |
|---|---|---|---|
| `Strict` | Fail | Promote to errors | Include |
| `Standard` | Fail | Keep as warnings | Include |
| `Release` | Fail | Keep as warnings | Suppress |

`Standard` remains the authored default where the product surface does not
explicitly select another valid mode.

## 4. Unknown validation values

An unknown serialized enum value is invalid configuration. It must not silently
weaken diagnostics.

Until the authored asset is corrected, an unknown value uses conservative
`Strict` diagnostic semantics:

```text
required configuration  -> fail
warnings                -> errors
info diagnostics         -> included
known mode               -> false
```

This is not a fallback that repairs or rewrites user intent. The invalid authored
value remains invalid and diagnosable; the policy only prevents a corrupted or
obsolete enum value from reducing validation severity.

## 5. Runtime and product boundaries

This governance record creates no runtime behavior beyond the existing
validation policy.

It explicitly does **not** introduce:

```text
global validation manager
service locator
runtime validation session
automatic asset mutation
silent enum normalization
new authoring authority
new Composer/Profile/Recipe
```

## 6. QA expectations

Technical QA is appropriate for the deterministic policy matrix because a change
can alter validation severity without a visible Inspector-layout change.

The focused regression must prove:

```text
Strict    known, required fails, warnings -> errors, info included
Standard  known, required fails, warnings stay warnings, info included
Release   known, required fails, warnings stay warnings, info suppressed
Unknown   not known, required fails, warnings -> errors, info included
```

No synthetic Inspector-understandability test is required.

## 7. Change discipline

- Changes to `FrameworkApiStatus` meanings are architecture-governance changes.
- Breaking changes to `Stable` consumer surfaces require explicit ADR/migration handling.
- Changes to validation-mode semantics affect ADR-010 product diagnostics and require focused technical QA.
- Adding a new validation mode requires defining its severity/noise semantics explicitly before adoption.
- Unknown values must continue to fail conservatively; no silent downgrade is allowed.

## 8. Disposition

This record closes the reverse-audit documentation gap around the two governance
primitives without inventing ADR-019 or another product subsystem.
