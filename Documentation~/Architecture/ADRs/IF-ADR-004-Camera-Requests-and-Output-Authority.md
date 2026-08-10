# IF-ADR-004 — Camera Requests and Output Authority

Status: **Accepted — current single-output boundary implemented and technically certified**  
Last updated: **2026-08-10**  
Package implementation: **Implemented**  
Technical QA: **Certified**  
FIRSTGAME integration: **Partial — broader real-consumer proof remains separate**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-004A, IF-ADR-004B, IF-ADR-004C, IF-ADR-005, IF-ADR-006, IF-ADR-008, IF-ADR-010, IF-ADR-014
Current reconciliation: [IF-ADR-004A](../Reconciliation/IF-ADR-004A-Camera-Authority-Normative-Reconciliation-2026-08-10.md) and [IF-ADR-004B](../Reconciliation/IF-ADR-004B-Camera-Negative-Integrity-Certification-2026-08-10.md).

> This ADR is the normative Camera authority. Mutable portfolio status lives in
> `../Tracking/IF-TRACK-Framework.md`. The 2026-08-10 certification sequence is
> preserved by IF-ADR-004B, IF-ADR-004C and the Camera QA certification record.

## 1. Context

Camera presentation requires one explicit physical output authority while
Session, Route, Activity and eligible Local Player scopes may request Camera
presentation without directly mutating the shared output or discovering an
implicit current Camera.

The accepted pipeline is:

```text
product authoring
  -> typed request publication
  -> logical arbitration
  -> transactional output synchronization
  -> physical Unity/Cinemachine projection
```

The current accepted product supports **one persistent Camera output per
Session**. Multi-output, split-screen and concurrent per-player physical outputs
are separate future contracts.

## 2. Decision — authority chain

```text
Camera request source
  Session / Route / Activity / eligible Local Player
        ↓
typed CameraRequest + explicit ownership/lifetime evidence
        ↓
ScopedCameraRequestPublisher
        ↓
CameraOutputSession
        ↓
CameraOutputContext
  admission + deterministic winner
        ↓
CameraOutputRigApplicator
  physical projection
        ↓
CameraOutputSessionBinding
        ↓
explicit Unity Camera + CinemachineBrain
```

Responsibilities:

- `CameraOutputContext` owns admitted requests, deterministic arbitration,
  logical winner and next-winner restoration for one `CameraOutputId`;
- `CameraOutputRigApplicator` owns projection of the logical winner to the
  concrete output;
- `CameraOutputSession` is the transactional mutation boundary between logical
  state and physical projection;
- `CameraOutputSessionBinding` owns the scene-authored physical output and its
  explicit Unity Camera/CinemachineBrain references;
- request publishers translate one already-owned scope into publish/release;
- `CameraRigComposer` owns local rig intent/materialization, never persistent
  application output authority.

No global Camera manager, service locator, static request registry, `Camera.main`
authority or hierarchy/name/tag discovery is accepted.

## 3. Physical output authority

For the single-output product boundary:

- exactly one persistent `CameraOutputSessionBinding` is authored in the Session
  composition;
- it references exactly one explicit Unity `Camera` and one explicit
  `CinemachineBrain` on the same physical output GameObject;
- it exposes one explicit `CameraOutputId`;
- consumers receive that output through explicit typed composition/injection;
- duplicate persistent outputs are invalid composition and must block
  validation.

## 4. Camera rig authoring

`CameraRigComposer` is the designer-facing authority for one local Camera rig.
It owns presentation intent, typed target source, Follow/Look At requirements,
local framing and local Cinemachine materialization.

Apply/Rebuild may create or repair the local `CinemachineCamera`. It must never
create or claim:

```text
persistent Unity Camera
CinemachineBrain
CameraOutputSessionBinding
AudioListener
global Camera authority
```

The currently accepted presentation capability is **Follow**. Other Cinemachine
presentation models require separate product work.

## 5. Typed request contract

Every Camera request must carry explicit, valid evidence for:

- request identity;
- output identity;
- owner kind and owner scope;
- lifetime kind and lifetime scope;
- rig reference;
- target source;
- arbitration policy;
- release semantics;
- diagnostic source/description where applicable.

Missing or invalid mandatory evidence blocks explicitly. Runtime does not guess
identity, ownership, target or output state.

## 6. Deterministic arbitration

Publication timing is not policy.

```text
higher precedence
  -> wins

equal precedence
  -> both requests require distinct deterministic tie-break evidence
  -> deterministic ordinal tie-break ordering selects the winner
```

Missing or duplicate equal-precedence tie-break evidence blocks the conflicting
admission. Duplicate `CameraRequestId` also blocks.

Current product convention:

```text
Local Player   50
Activity      100
Route         200
Session       300
```

The normative contract is explicit precedence + deterministic tie-break evidence,
not those four values hard-coded into output authority.

## 7. Transactional logical / physical integrity

Logical mutation is not successful until physical application succeeds.

Admission:

```text
context.Admit(request)
  -> applicator.Apply(context)
     success       -> commit
     failure       -> remove admitted request
                   -> re-apply previous state
                   -> RolledBack or RollbackFailed
```

Release:

```text
context.Release(request)
  -> apply replacement/cleared state
     success       -> commit
     failure       -> re-admit released request
                   -> re-apply previous state
                   -> RolledBack or RollbackFailed
```

Rollback failure is terminal diagnostic evidence and is never reported as normal
success.

## 8. Scope ownership and component lifetime

Camera ownership has two distinct lifetime layers.

### 8.1 Logical owner lifetime

```text
Route
  -> canonical Route enter/exit lifecycle

Activity
  -> canonical Activity enter/exit lifecycle

Session
  -> SessionCameraOverrideBinding component availability

Local Player
  -> explicit Player eligibility/publication boundary
```

Route/Activity binding owner identity follows the exact authored `RouteAsset` or
`ActivityAsset` reference. Stable IDs remain persistence/diagnostic evidence and
do not replace authored-definition identity authority.

### 8.2 Publication/component lifetime

`ScopedCameraOverrideBinding` owns the publication object and active publication
state. Abnormal Unity component lifetime must not leave an admitted request
orphaned.

Accepted behavior:

```text
ScopedCameraOverrideBinding.OnDisable
  -> release owned publication only

ScopedCameraOverrideBinding.OnDestroy
  -> final idempotent publication release
```

For Route and Activity this **does not** synthesize a Route/Activity exit and does
not clear their logical-owner state. Re-enable does not silently re-publish;
publication remains explicit while the already-entered logical owner is still
valid.

`SessionCameraOverrideBinding` is intentionally different: the component itself
owns Session availability, so disable/destroy ends that owner scope through
`EndOwnerScope(...)`.

Normal lifecycle exit, abnormal component loss, repeated cleanup and re-enable
without silent republish are certified by IF-ADR-004C.

## 9. Target resolution

Target resolution is explicit and typed. Required target failures block.
Runtime target resolution must not use GameObject-name lookup, tags as authority,
hierarchy guessing, `Camera.main` or global service lookup.

## 10. Runtime / Editor boundary

Editor tooling may validate and materialize local rig state and validate
persistent Camera composition. It never becomes runtime output authority.

Runtime Camera code must not depend on Editor assemblies or Editor-only state.

## 11. Product surface and diagnostics

Supported product-facing surfaces include:

- `CameraRigComposer`;
- `CameraOutputSessionBinding`;
- Session / Route / Activity Camera override bindings;
- typed Local Player Camera publication through Player integration;
- authoring/composition validation;
- Advanced / Diagnostics evidence.

Diagnostics may expose output/request IDs, owner/lifetime evidence, precedence,
tie-break ID, admitted request set, winner, physical apply, rollback and explicit
blocking issues.

## 12. Technical certification

The current accepted boundary is backed by three complementary proofs.

### C9R — positive authority lifecycle

```text
[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]
status='Passed'
cases='11'
```

C9R preserves the supported positive lifecycle ladder, restoration, duplicate
request/release behavior and normal Activity/Route cleanup.

### IF-ADR-004C — owner lifetime integrity

```text
[QA_CAMERA_ADR004C]
status='Passed'
cases='10/10'
failed='0'
verdict='ADR-004C CAMERA OWNER LIFETIME INTEGRITY CERTIFIED'
```

This certifies normal exit, Session disable, Route/Activity abnormal disable,
Activity destruction, winner/non-winner cleanup, idempotence and explicit-only
re-enable behavior.

### IF-ADR-004B — negative integrity

```text
[QA_CAMERA_ADR004B]
status='Passed'
cases='18/18'
failed='0'
blocked='0'
verdict='ADR-004B CAMERA NEGATIVE INTEGRITY CERTIFIED'
```

The 18-case matrix covers deterministic arbitration, identity/output validation,
publish/release idempotence, restoration ordering, transactional rollback,
normal lifecycle cleanup, abnormal owner loss and persistent-output authoring
integrity.

## 13. Certification history

The certification was intentionally evidence-driven:

```text
004A
  reconcile normative architecture
      ↓
004B first execution
  17/18
  case 16 reproduced Route-owner orphan
      ↓
004C
  narrow owner-lifetime package correction
  10/10 certified
      ↓
004B re-certification
  18/18 certified
```

The initial failure is preserved as evidence that 004C was justified by a real
package defect rather than speculative architecture.

## 14. Non-goals

This ADR does not authorize:

- global `CameraManager` / service locator / static request registry;
- timing-based priority;
- multiple simultaneous physical outputs;
- split-screen;
- concurrent per-player physical output ownership;
- generic cross-feature request broker;
- new Recipe/Profile layer solely for symmetry;
- second Composer around the same local rig intent;
- automatic creation of persistent output from a local rig;
- Camera ownership of AudioListener behavior;
- arbitrary Cinemachine presentation modes beyond accepted product support.

## 15. Current disposition

```text
Architecture
  ACCEPTED

Package — current single-output boundary
  IMPLEMENTED

Product Surface / Diagnostics
  IMPLEMENTED / CONFORMANT

Technical QA
  CERTIFIED
  C9R 11/11
  IF-ADR-004C 10/10
  IF-ADR-004B 18/18

FIRSTGAME
  PARTIAL
  broader real-consumer Camera proof remains separate

Current technical blocker
  NONE for the accepted single-output boundary
```

Future multi-output/split-screen work is a new contract and does not reopen this
certified single-output boundary by default.
