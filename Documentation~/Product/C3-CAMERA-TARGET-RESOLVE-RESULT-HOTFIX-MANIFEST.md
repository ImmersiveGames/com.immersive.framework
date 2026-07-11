> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# C3 — CameraTargetResolveResult Compile Hotfix Manifest

Status: compile hotfix
Package: `com.immersive.framework`
Surface: `Camera Product Contracts`
Date: 2026-07-09

## Objective

Fix the C3 compile error caused by member name collisions in `CameraTargetResolveResult`.

The original C3 file declared boolean properties and static factory methods with the same names:

```text
Succeeded
Blocked
```

C# does not allow members with the same name in the same type, even when one is a property and the other is a method.

## Files changed

```text
Runtime/Camera/Product/CameraTargetResolveResult.cs
```

## Fix

```text
Succeeded property -> IsSucceeded
Blocked property -> IsBlocked
```

The static factories remain:

```text
CameraTargetResolveResult.Succeeded(...)
CameraTargetResolveResult.Blocked(...)
```

## Files created

```text
Documentation~/Product/C3-CAMERA-TARGET-RESOLVE-RESULT-HOTFIX-MANIFEST.md
```

## Files removed

```text
none
```

## Out of scope

```text
no new camera contracts
no Cinemachine implementation
no CameraComposer
no Editor code
no asmdef changes
no package.json changes
no FIRSTGAME changes
no QAFramework changes
```

## Acceptance criteria

```text
CameraTargetResolveResult no longer has duplicate member definitions.
C3 contracts compile.
Factory methods remain available.
Result-state booleans remain available as IsSucceeded and IsBlocked.
```

## Suggested commit message

```text
Runtime: fix CameraTargetResolveResult member collision
```
