# IF-ADR-004C — Camera Owner Lifetime Integrity

Status: **ACCEPTED — IMPLEMENTED — CERTIFIED 10/10**  
Date: **2026-08-10**  
Type: **Narrow package hardening + technical QA**  
Primary system: **Camera**  
Triggered by: **IF-ADR-004B case 16**  
Normative parent: **IF-ADR-004 — Camera Requests and Output Authority**

## 1. Trigger

IF-ADR-004B proved that canonical Camera composition could leave an admitted
Route Camera request orphaned when an active `RouteCameraOverrideBinding` was
disabled before the normal Route-exit callback:

```text
operation='DisableRouteOwner'
admittedBefore='2'
admittedAfter='2'
orphan='True'
```

Normal Activity/Route exit behavior still passed, isolating the problem to an
abnormal Unity component-lifetime boundary rather than Game Flow ownership.

## 2. Ownership analysis

The package has two distinct lifetimes that must not be conflated.

### Logical owner lifetime

```text
Route    -> Route lifecycle enter/exit
Activity -> Activity lifecycle enter/exit
Session  -> SessionCameraOverrideBinding availability
```

### Publication/component lifetime

```text
ScopedCameraOverrideBinding
  -> publisher
  -> overrideActive
```

A temporary disable of a Route/Activity Camera binding does not mean that the
logical Route/Activity has exited. Therefore a fix that called
`EndOwnerScope(...)` generically from the base would invent lifecycle semantics.

## 3. Decision

Publication lifetime is hardened in the existing scoped publication owner:

```text
ScopedCameraOverrideBinding.OnDisable
  -> release owned publication only

ScopedCameraOverrideBinding.OnDestroy
  -> final idempotent publication release
```

For Route/Activity these hooks do not clear `ownerActive` and never silently
re-publish on re-enable.

Session remains different because the component itself owns Session availability:

```text
SessionCameraOverrideBinding.OnDisable
  -> EndOwnerScope("SessionBindingDisabled")

SessionCameraOverrideBinding.OnDestroy
  -> EndOwnerScope("SessionBindingDestroyed")
```

## 4. Package changes

```text
Runtime/Camera/Bindings/ScopedCameraOverrideBinding.cs
Runtime/Camera/Bindings/SessionCameraOverrideBinding.cs
```

No new service, manager, context, registry, runtime host, fallback, helper
architecture or lifecycle orchestrator is introduced.

## 5. Behavioral contract

The accepted owner-lifetime rules are:

1. normal Activity exit releases the Activity publication;
2. normal Route exit releases the Route publication;
3. Session disable/destroy ends Session owner scope and releases publication;
4. abnormal Route/Activity component disable releases publication without
   synthesizing logical owner exit;
5. abnormal destruction has a final idempotent release safety net;
6. removing a non-winning owner removes only its request;
7. removing a winning owner restores the next valid request;
8. repeated cleanup is idempotent;
9. re-enable never silently publishes;
10. explicit publication may occur again only while the corresponding logical
    owner is valid.

## 6. QA strategy

No new setup or lifecycle fixture was created. The existing C9R fixture remains
the owner of real Camera composition and keeps its canonical 11-case count.
Additional 004C probes record owner-lifetime evidence inside that same lifecycle.

The 004C regression consumes that evidence in the same Play Mode session.

## 7. Certified matrix

```text
01 Activity normal exit
02 Route normal exit
03 Session disable cleanup
04 Route abnormal disable cleanup
05 Activity abnormal disable cleanup
06 Activity destruction cleanup
07 non-winner owner-only cleanup
08 winning owner restores next
09 cleanup idempotent
10 re-enable without silent republish
```

Final verdict:

```text
[QA_CAMERA_ADR004C]
status='Passed'
cases='10/10'
failed='0'
verdict='ADR-004C CAMERA OWNER LIFETIME INTEGRITY CERTIFIED'
```

The original 004B decision-gate probe also changed from:

```text
admittedAfter='2' orphan='True'
```

to:

```text
admittedAfter='1' orphan='False'
```

## 8. Regression safety

The package correction is accepted only because all three gates passed together:

```text
C9R
  11/11 PASS

IF-ADR-004C
  10/10 PASS

IF-ADR-004B
  18/18 PASS
```

This proves that abnormal-lifetime hardening did not regress the normal Camera
authority/restoration lifecycle.

## 9. Non-goals

004C does not authorize:

- global Camera cleanup manager;
- static liveness registry;
- service locator;
- hidden hierarchy discovery;
- automatic request publication on enable;
- synthetic Route/Activity exit on component disable;
- multi-output/split-screen architecture;
- changes to CameraRigComposer authoring intent.

## 10. Result

```text
Root cause
  publication lifetime could outlive disabled/destroyed scoped binding

Canonical fix owner
  ScopedCameraOverrideBinding publication lifetime

Logical lifecycle ownership
  unchanged

Package
  IMPLEMENTED

QA
  CERTIFIED 10/10

004B re-certification
  CERTIFIED 18/18

Current blocker
  NONE for accepted single-output owner lifetime
```
