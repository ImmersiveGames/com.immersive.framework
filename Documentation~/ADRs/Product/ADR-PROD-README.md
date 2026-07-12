# Product ADRs — Immersive Framework 1.0

Status: active product-direction chain  
Date: 2026-07-09

This folder defines product-direction ADRs for Immersive Framework 1.0. They do
not replace technical ADRs; they define product-surface rules for technical
work.

## Current chain

1. [ADR-PROD-0001 — Product Surface Model](ADR-PROD-0001-product-surface-model.md)
2. [ADR-PROD-0002 — Diagnostics Are Not Product UX](ADR-PROD-0002-diagnostics-are-not-product-ux.md)
3. [ADR-PROD-0003 — Domain Runtime Context Policy](ADR-PROD-0003-domain-runtime-context-policy.md)
4. [ADR-PROD-0004 — First Reference Product Surface](ADR-PROD-0004-first-reference-product-surface.md)
5. [ADR-PROD-0005 — Camera Product Surface requires Cinemachine](ADR-PROD-0005-camera-product-surface-cinemachine.md) — superseded by ADR-PROD-0006.
6. [ADR-PROD-0006 — Camera requests and output-scoped runtime authority](ADR-PROD-0006-camera-requests-output-contexts.md) — current Camera decision.

## Product rule

A recurring framework feature is not product-ready solely because it compiles,
validates, passes a smoke, or emits correct logs. When applicable it must offer:

```text
Recipe / Profile / Template
+ Composer / Authoring
+ Apply / Rebuild
+ Technical materialization
+ Runtime Context when real Play Mode behavior needs authority
+ Diagnostics
+ Sample / Template
```

Use these ADRs before planning new cuts, especially when a feature risks
becoming loose components plus validators and smokes. The expected direction is
authorable product plus technical contracts, real runtime and diagnostics.
