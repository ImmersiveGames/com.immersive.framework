# IF-ADR-004A — Camera Authority Normative Reconciliation

Status: **CLOSED — reconciled into IF-ADR-004**  
Date: 2026-08-10  
Type: **Architecture / Documentation**  
Primary system: **Camera**  
Normative target: **IF-ADR-004 — Camera Requests and Output Authority**

## Source baselines inspected

```text
com.immersive.framework
  2bb2c8bb44a1792ffc43f976e266712dc13d91b6

QAFramework
  8a61807f361f731c2095ed1e671d98c30a8afd56

FIRSTGAME / planet-devourer
  cf14c0faca8179e23ece4bebf71da3c278faa10d
```

The Git repositories were treated as read-only during the audit.

## Objective

Reconcile IF-ADR-004 with the Camera architecture already implemented in the
official package so that the normative contract accurately describes:

- scoped logical Camera output authority;
- deterministic arbitration;
- transactional logical/physical synchronization;
- persistent physical output ownership;
- designer-facing rig authoring;
- exact lifecycle ownership boundaries;
- current single-output limitations;
- current QA and FIRSTGAME proof status.

The reconciliation must not invent a broader Camera redesign or convert an
unproven hardening concern into a package defect.

## Scope

This cut records the accepted current architecture around:

```text
CameraRigComposer
ScopedCameraRequestPublisher
CameraOutputSession
CameraOutputContext
CameraOutputRigApplicator
CameraOutputSessionBinding
Session / Route / Activity / Local Player request producers
Persistent Content Camera composition validation
```

It also records the existing QA evidence and the missing negative certification
that becomes IF-ADR-004B.

## Out of scope

This cut does not:

- change runtime code;
- change Editor code;
- change QA code;
- change FIRSTGAME;
- add new Camera features;
- add multi-output or split-screen;
- add a global Camera manager;
- add a service locator or static registry;
- add Recipe/Profile authoring layers;
- create another Composer;
- redesign Cinemachine integration;
- change Local Player participation contracts;
- move AudioListener ownership into Camera;
- implement speculative abnormal-lifetime cleanup.

## Audit findings incorporated into the ADR

### 1. Logical authority is scoped and explicit

`CameraOutputContext` owns the admitted request set and deterministic logical
winner for one `CameraOutputId`. It does not discover Camera authority globally.

### 2. Arbitration is stricter than the older normative shorthand

The package uses explicit precedence plus deterministic tie-break evidence.
Equal-precedence ambiguity blocks instead of allowing callback timing or
"newest request wins" semantics.

The ADR was updated to make the package behavior normative rather than
regressing the package to the older shorthand.

### 3. CameraOutputSession is a transactional boundary

A logical admission/release is synchronized with physical projection through the
output applicator. Failed physical application triggers rollback; incomplete
rollback produces explicit `RollbackFailed` evidence.

This is now a normative integrity rule.

### 4. Persistent Content owns the physical output

Exactly one persistent `CameraOutputSessionBinding` is required for the current
single-output application composition. The binding owns explicit Unity Camera
and CinemachineBrain references.

### 5. CameraRigComposer owns only the local rig

The Composer is a justified ADR-002 / ADR-010 materialized authoring surface. It
may validate and Apply/Rebuild its local Cinemachine rig but does not own or
create the persistent physical Camera output.

The currently accepted presentation capability is Follow.

### 6. Route/Activity ownership follows authored-definition identity

Route and Activity Camera bindings validate exact authored asset references,
aligning Camera lifecycle ownership with IF-ADR-014.

### 7. Normal cleanup is implemented; abnormal owner loss remains unproven

Activity/Route lifecycle exit and output detachment provide normal explicit
release paths. The audit did not certify every abnormal Unity disable/destroy
path before lifecycle exit.

This remains a QA question. It is not recorded as a confirmed package bug.

## Files created / altered / removed by this documentation cut

### Edited

- `IF-ADR-004-Camera-Requests-and-Output-Authority.md`
- `IF-TRACK-Framework.md`

### Created

- `IF-ADR-004A-Camera-Authority-Normative-Reconciliation-2026-08-10.md`
- `IF-ADR-004B-Camera-Negative-Integrity-Certification-2026-08-10.md`
- `IMMERSIVE-FRAMEWORK-ADR-004-CAMERA-AUDIT-2026-08-10.md`
- delivery `MANIFEST.md`

### Removed

- none

## Product surface affected

No product behavior changes.

The documentation now names the existing official product surface accurately:

```text
CameraRigComposer
CameraOutputSessionBinding
Session Camera Override
Route Camera Override
Activity Camera Override
Local Player Camera publication
Advanced / Diagnostics
```

## Expected usage flow

The accepted user-facing flow remains:

```text
1. Persistent Content owns the physical Camera Output.
2. A designer authors a local Camera rig through CameraRigComposer.
3. Apply / Rebuild materializes only the local Cinemachine rig when needed.
4. A supported scope publishes an explicit typed Camera request.
5. The scoped output authority selects one deterministic winner.
6. The output session applies the winner transactionally.
7. Releasing/ending the scope restores the next valid request.
8. Advanced / Diagnostics exposes technical evidence when required.
```

## Technical smoke expected

No new runtime smoke is required to close **004A**, because this is a normative
reconciliation cut and introduces no code changes.

The source audit must establish that the documented responsibilities actually
exist in the current package, while existing Camera QA establishes that a
positive authority/restoration harness already exists.

All additional negative proof belongs to IF-ADR-004B.

## Technical acceptance

004A is accepted when:

- IF-ADR-004 names the current logical/physical authority boundaries;
- deterministic tie-break arbitration is normative;
- transactional output apply/rollback semantics are normative;
- persistent physical output ownership is explicit;
- local Composer vs persistent output ownership is explicit;
- exact Route/Activity definition ownership is recorded;
- single-output limitation is explicit;
- abnormal owner loss is classified as unproven rather than confirmed broken;
- 004B is identified as the next technical gate;
- no speculative runtime authority is introduced by documentation.

## Product acceptance

004A is accepted when:

- existing CameraRigComposer authoring remains the primary local-rig surface;
- existing physical output composition remains explicit;
- Apply/Rebuild remains limited to local rig materialization;
- Advanced/Diagnostics remains available for technical evidence;
- documentation does not require a Recipe/Profile/Wizard merely for symmetry;
- documentation does not imply generic Cinemachine presentation beyond the
  current Follow capability.

## Architectural gain

The Camera ADR now reflects the actual authority architecture and makes the
transactional consistency rule explicit. This prevents future changes from
silently weakening deterministic arbitration or treating logical winner state as
successful physical presentation.

## Usability gain

The product boundary is clearer: designers configure local Camera intent in the
Composer, while the persistent Camera output remains a separate application
composition concern. The documentation no longer suggests that consumers should
manually construct the internal context/session machinery.

## Result

```text
IF-ADR-004A
Architecture/documentation: CLOSED
Package code change: NONE
QA code change: NONE
FIRSTGAME code change: NONE
Next gate: IF-ADR-004B
Conditional follow-up: IF-ADR-004C only if 004B proves a lifetime defect
```

## Suggested commit message

```text
docs(camera): reconcile ADR-004 with current output authority
```
