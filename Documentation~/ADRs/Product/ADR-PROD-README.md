# Product ADRs — Immersive Framework 1.0

Status: active product-direction chain  
Date: 2026-07-09

This folder defines the product-direction ADRs for Immersive Framework 1.0.

These ADRs do not replace technical ADRs. They define the product surface rules that future technical work must respect.

## Current chain

1. [ADR-PROD-0001 — Product Surface Model](ADR-PROD-0001-product-surface-model.md)
2. [ADR-PROD-0002 — Diagnostics Are Not Product UX](ADR-PROD-0002-diagnostics-are-not-product-ux.md)
3. [ADR-PROD-0003 — Domain Runtime Context Policy](ADR-PROD-0003-domain-runtime-context-policy.md)
4. [ADR-PROD-0004 — First Reference Product Surface](ADR-PROD-0004-first-reference-product-surface.md)

## Product rule

A recurring framework feature is not considered product-ready only because it compiles, validates, passes smoke, or emits correct logs.

When applicable, it must expose a usable product surface:

```text
Recipe / Profile / Template
+ Composer / Authoring
+ Apply / Rebuild
+ Technical materialization
+ Runtime Context when real Play Mode behavior needs authority
+ Diagnostics
+ Sample / Template
```

## Intended use

Use these ADRs before planning new framework cuts, especially when a feature risks becoming:

```text
loose components + validators + smokes
```

The expected direction is:

```text
authorable product + technical contracts + real runtime + diagnostics
```
