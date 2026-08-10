# Immersive Framework — IF-ADR-004 Camera Audit

Date: **2026-08-10**  
Scope: **IF-ADR-004 + current package Camera implementation + current QA Camera evidence**  
Purpose: **Historical audit/evidence record; IF-ADR-004 remains the normative authority**

## Source baselines

```text
com.immersive.framework
  2bb2c8bb44a1792ffc43f976e266712dc13d91b6

QAFramework
  8a61807f361f731c2095ed1e671d98c30a8afd56

FIRSTGAME / planet-devourer
  cf14c0faca8179e23ece4bebf71da3c278faa10d
```

Repositories were inspected read-only.

## Audit question

Does the current official Camera package still implement the authority described
by IF-ADR-004, and is the remaining work a Camera redesign, a product-surface
gap, a QA gap or a concrete runtime defect?

## Executive result

```text
IF-ADR-004 — Camera Requests and Output Authority

Architecture:
  ACCEPTED — normative reconciliation required and recorded as IF-ADR-004A

Package:
  SUBSTANTIALLY IMPLEMENTED

Authoring / Product Surface:
  STRONG / CONFORMANT

QA:
  PARTIAL — positive authority/restoration evidence exists;
  negative integrity certification remains pending

FIRSTGAME:
  PARTIAL — broader real-consumer Camera proof remains pending

Main technical uncertainty:
  abnormal owner-lifetime cleanup outside normal lifecycle paths

Confirmed package defect:
  NONE established by this audit
```

The main conclusion is that Camera does **not** currently justify a broad
redesign. The package architecture is more mature than the older ADR text. The
immediate work is normative reconciliation followed by focused negative QA.

## Method

The audit compared the normative ADR and related architecture decisions against
current source responsibilities in the package and current QA Camera surfaces.

Key package areas inspected included:

```text
Runtime/Camera/Output/CameraOutputContext.cs
Runtime/Camera/Output/CameraOutputSession.cs
Runtime/Camera/Bindings/CameraOutputSessionBinding.cs
Runtime/Camera/Bindings/ScopedCameraOverrideBinding.cs
Runtime/Camera/Bindings/ActivityCameraOverrideBinding.cs
Runtime/Camera/Bindings/RouteCameraOverrideBinding.cs
Runtime/Camera/Publishing/ScopedCameraRequestPublisher.cs
Runtime/Camera/LocalPlayerCameraRequestPublisher.cs
Runtime/CameraAuthoring/CameraRigComposer.cs
Editor/CameraAuthoring/CameraRigComposerEditor.cs
Editor/CameraAuthoring/CameraRigComposerApplyRebuildUtility.cs
persistent authoring/composition validation in FrameworkAuthoringValidator
```

Current QA evidence inspected included:

```text
Assets/ImmersiveFrameworkQA/Camera/Documentation/
  C9R-CAMERA-OVERRIDE-AUTHORITY-QA.md

Assets/ImmersiveFrameworkQA/Camera/Scripts/Runtime/
  QaC9RCameraOverrideAuthorityFixture.cs

Assets/ImmersiveFrameworkQA/Camera/Scripts/Editor/
  QaCameraOutputSessionBindingAuthoringRegression.cs
  QaPersistentCameraPresentationCompositionRegression.cs
```

## 1. Conformant strengths

### 1.1 Scoped runtime output authority

`CameraOutputContext` owns one explicit `CameraOutputId`, the admitted request
set and one logical winner. It does not rely on a global current Camera,
`Camera.main`, scene-name discovery, singleton or service locator.

Assessment: **strong / conformant with IF-ADR-001 and IF-ADR-004**.

### 1.2 Deterministic arbitration

The current package uses explicit precedence plus deterministic tie-break
evidence. Ambiguous equal-precedence admission blocks instead of using timing.

Assessment: **strong implementation; older ADR wording was stale**.

### 1.3 Transactional logical/physical integrity

`CameraOutputSession` coordinates logical mutation and physical projection. A
physical application failure triggers rollback; incomplete restoration is
reported explicitly.

Assessment: **stronger than the prior ADR description and important enough to be
normative**.

### 1.4 Explicit persistent physical output

`CameraOutputSessionBinding` owns explicit Unity Camera/CinemachineBrain
references for one scoped output. Persistent Content validation checks the
single-output composition boundary.

Assessment: **strong / aligned with IF-ADR-008**.

### 1.5 Legitimate CameraRigComposer product surface

`CameraRigComposer` represents real designer intent and deterministically
materializes local Cinemachine state. Its Inspector provides explicit validation,
Apply/Rebuild and Advanced/Diagnostics.

The Composer does not own or create the persistent output.

Assessment: **strong / aligned with IF-ADR-002 and IF-ADR-010**.

### 1.6 Exact Route/Activity lifecycle owner identity

Route/Activity Camera bindings validate the exact authored asset reference,
rather than treating textual/stable IDs as authored-definition equality.

Assessment: **aligned with IF-ADR-014**.

## 2. Normative drift found

### 2.1 Equal-priority semantics

The package no longer follows an implicit "newest request wins" interpretation.
The accepted behavior is deterministic tie-break evidence and explicit blocking
of ambiguity.

Resolution: **update ADR; do not regress package behavior**.

### 2.2 Transactional session responsibility was under-specified

The older ADR did not adequately distinguish:

```text
CameraOutputContext       logical arbitration
CameraOutputRigApplicator physical projection
CameraOutputSession       transactional synchronization
```

Resolution: **record these as normative roles**.

### 2.3 Persistent output vs local rig ownership needed stronger wording

The package already keeps these concerns separate. The normative ADR needed to
make the distinction explicit so future product work does not let
`CameraRigComposer` silently become application output authority.

Resolution: **record current boundary**.

### 2.4 Current presentation capability needed explicit scope

The current official Composer presentation intent is Follow. A generic claim of
arbitrary Cinemachine presentation would exceed source evidence.

Resolution: **document Follow as the current accepted capability**.

## 3. Unproven hardening boundaries

### 3.1 Abnormal owner loss

Normal Activity/Route lifecycle exit and output detachment explicitly release
owned requests. The audit did not establish a universal emergency cleanup path
for a component/GameObject that disappears before its normal lifecycle exit.

This creates a test question:

```text
admitted request
  + owner disabled/destroyed abnormally
  before expected lifecycle exit/release
    ↓
does the request survive beyond valid owner lifetime?
```

Classification: **unproven boundary, not confirmed package bug**.

Required action: IF-ADR-004B case 16.

### 3.2 Stale/out-of-order cleanup

The request context contains invalid-request pruning behavior during release.
The negative suite should prove that stale/out-of-order release cannot leave an
incorrect winner or corrupt restoration ordering.

Classification: **QA evidence gap**.

### 3.3 Deterministic rollback failure proof

The package has explicit rollback semantics in source, but the audit did not find
complete deterministic QA evidence for failed physical apply, successful
rollback and rollback failure.

Classification: **QA evidence gap**.

## 4. QA evidence currently present

Camera QA is not absent.

The existing C9R Camera Override Authority surface already proves the principal
positive ownership ladder and restoration behavior around:

```text
Local Player 50
Activity 100
Route 200
Session 300
```

with explicit request/release, restoration and lifecycle cleanup behavior.

Focused editor regressions also exist around output binding authoring and
persistent Camera composition.

Therefore IF-ADR-004B should extend the current canonical Camera QA surface
instead of creating a second parallel Camera QA architecture.

## 5. Negative certification still required

The strongest missing certification areas are:

- equal-precedence missing/duplicate tie-break blocking;
- duplicate RequestId;
- output mismatch;
- Publish/Release idempotence with no duplicate physical mutation;
- out-of-order release;
- physical admission failure + rollback;
- release replacement failure + rollback;
- explicit RollbackFailed;
- abnormal owner disable/destruction;
- persistent duplicate-output invalid authoring;
- missing/invalid physical output references.

The complete matrix is frozen in
`IF-ADR-004B-Camera-Negative-Integrity-Certification-2026-08-10.md`.

## 6. Product / UX assessment

The current Camera product surface is one of the clearer examples of the intended
framework model:

```text
Designer intent
  CameraRigComposer
        ↓
explicit local Apply / Rebuild when materialization is needed
        ↓
local Cinemachine technical state

Application composition
  CameraOutputSessionBinding
        ↓
physical output authority

Runtime scopes
  typed Camera requests
        ↓
scoped arbitration / transactional application
```

No Recipe/Profile/Wizard is currently justified merely to make Camera look more
"productized". The Composer exists because deterministic local materialization is
real, not because every feature needs the same authoring stack.

FIRSTGAME still needs to demonstrate the complete official consumer experience in
a real gameplay flow, including understandable request replacement/restoration
and technical debugging without local duplicate Camera authority.

## 7. Package changes recommended by this audit

**None before IF-ADR-004B.**

The audit found no evidence supporting a broad Camera rewrite. In particular, it
does not justify introducing:

```text
CameraManager
Camera service locator
static request registry
new global runtime context
CameraRecipe / CameraProfile
second Camera Composer
generic request broker
multi-output architecture
```

## 8. Conditional package cut

Open:

```text
IF-ADR-004C — Camera Owner Lifetime Integrity
```

only if IF-ADR-004B reproduces an orphan request beyond its accepted owner
lifetime.

If the existing lifecycle composition already guarantees cleanup and QA proves
that invariant, 004C must remain unopened.

## 9. Recommended execution order

```text
IF-ADR-004A
  normative reconciliation
  CLOSED by documentation
        ↓
IF-ADR-004B
  negative integrity certification in QAFramework
        ↓
if and only if a package defect is proven
  IF-ADR-004C
  narrow owner-lifetime package hardening
        ↓
re-run QA
        ↓
FIRSTGAME
  broader real-consumer Camera proof
```

## 10. Final classification

```text
Normative contract
  RECONCILED by IF-ADR-004A

Core runtime authority
  STRONG

Request model
  STRONG

Deterministic arbitration
  STRONG

Physical/logical consistency
  STRONG IN SOURCE — negative QA still required

Persistent output authority
  STRONG

CameraRigComposer product surface
  STRONG / ADR-002 + ADR-010 conformant

Route/Activity identity
  STRONG / ADR-014 conformant

Normal lifecycle cleanup
  IMPLEMENTED

Abnormal lifetime cleanup
  UNPROVEN — principal hardening question

Negative QA
  INCOMPLETE

FIRSTGAME
  BROADER REAL PRODUCT PROOF REQUIRED
```
