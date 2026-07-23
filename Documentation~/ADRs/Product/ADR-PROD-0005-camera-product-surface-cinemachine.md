> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# ADR-PROD-0005 — Camera Product Surface requires Cinemachine

Status: Superseded by `ADR-PROD-0006`  
Date: 2026-07-09  
Superseded: 2026-07-10  
Package: `com.immersive.framework`  
Area: Camera Product Surface

## Historical decision

This ADR established the still-valid decision that Cinemachine is mandatory for the official Camera Product Surface and that the framework must not maintain a parallel pure-Unity-Camera product path.

It also introduced the first Cinemachine-first product vocabulary:

```text
Camera Recipe
Camera Composer
explicit target sources
Cinemachine rig materialization
future scoped runtime authority
```

## Why this ADR was superseded

The ADR left runtime camera coordination unresolved and permitted legacy/compatibility paths to remain:

```text
FrameworkCameraDirector
Route/Activity camera bindings
PlayerView camera activation adapters
raw Unity Camera activation evidence
```

The later C3–C8 implementation proved local Cinemachine materialization, but it did not establish a complete model for:

```text
Route -> Activity -> Player camera succession
temporary overrides and release
previous-request restoration
multiple outputs and split-screen
local versus remote online players
spectator outputs
```

`ADR-PROD-0006` replaces the incomplete ownership model with:

```text
CameraTargetSource
CameraRigRecipe
CameraRigComposer
CameraRequest
CameraOutputContext
```

and requires destructive removal of the superseded Director/binding/activation architecture.

## Decisions preserved by ADR-PROD-0006

```text
Cinemachine is mandatory.
The framework does not reimplement Follow, LookAt, damping or blending.
Target sources are explicit and typed.
An explicit typed Player target source may expose CameraTarget and LookAtTarget.
Apply/Rebuild must be idempotent.
No Camera.main fallback.
No object-name or hierarchy-path functional identity.
No global singleton or service locator.
```

## Decisions no longer current

Do not use this ADR as current guidance for:

```text
camera ownership
runtime camera activation
Route/Activity binding architecture
priority-based cross-owner selection
legacy compatibility
single-player CameraComposer as final runtime shape
```

Read:

```text
ADR-PROD-0006-camera-requests-output-contexts.md
```
