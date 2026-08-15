# IF-ADR-004 — Camera Requests and Output Authority

Status: **Accepted / Reconciled / Camera QA Recertified 2026-08-15**  
Last updated: **2026-08-15**  
Package implementation: **Implemented**  
Technical QA: **Certified — Full Camera QA 53/53**  
FIRSTGAME integration: **Partial — real-consumer Camera proof remains separate**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-004A, IF-ADR-004B, IF-ADR-004C, IF-ADR-005, IF-ADR-006, IF-ADR-008, IF-ADR-010, IF-ADR-014, IF-ADR-021, IF-ADR-022  
Current reconciliation: [IF-ADR-004A](../Reconciliation/IF-ADR-004A-Camera-Authority-Normative-Reconciliation-2026-08-10.md), [IF-ADR-004B](../Reconciliation/IF-ADR-004B-Camera-Negative-Integrity-Certification-2026-08-10.md), [IF-ADR-004C](IF-ADR-004C-Camera-Owner-Lifetime-Integrity-2026-08-10.md), and [Camera Presentation Technical Certification — 2026-08-15](../Reconciliation/IMMERSIVE-FRAMEWORK-CAMERA-PRESENTATION-TECHNICAL-CERTIFICATION-2026-08-15.md).

> This ADR remains the normative Camera **request/output authority**.
> IF-ADR-022 extends local Camera rig presentation/materialization only.
> Presentation Model never becomes arbitration or physical output authority.

## 1. Context

Camera presentation requires one explicit physical output authority while Session,
Route, Activity and eligible Local Player scopes may request Camera presentation
without directly mutating the shared output or discovering an implicit current
Camera.

The accepted pipeline is:

```text
product authoring
  -> typed request publication
  -> logical arbitration
  -> transactional output synchronization
  -> physical Unity/Cinemachine projection
```

The accepted product supports **one persistent Camera output per Session**.
Multi-output, split-screen and concurrent per-player physical outputs are separate
future contracts.

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
- `CameraRigComposer` owns one local rig's presentation intent and materialized
  `CinemachineCamera`, never persistent application output authority.

No global Camera manager, service locator, static request registry,
`Camera.main` authority or hierarchy/name/tag discovery is accepted.

## 3. Physical output authority

For the single-output product boundary:

- exactly one persistent `CameraOutputSessionBinding` is authored in Session
  composition;
- it references exactly one explicit Unity `Camera` and one explicit
  `CinemachineBrain` on the same physical output GameObject;
- it exposes one explicit `CameraOutputId`;
- consumers receive that output through explicit typed composition/injection;
- duplicate persistent outputs are invalid composition and must block.

A local Camera rig must never create or claim:

```text
persistent Unity Camera
CinemachineBrain
CameraOutputSessionBinding
AudioListener
global Camera authority
```

## 4. Camera rig authoring

`CameraRigComposer` remains the designer-facing authority for **one local Camera
rig**.

IF-ADR-022 expands the accepted presentation family to:

```text
Fixed
Follow
Mounted
Third Person
```

The serialized identity of `Follow` remains:

```text
Follow = 10
```

The local Composer owns:

```text
Presentation Model
typed target source
model-valid target requirements
model-specific framing/settings
Editor materialization intent
materialization provenance
local CinemachineCamera reference
diagnostics
```

The Composer does **not** decide whether its rig wins.

The runtime output path remains presentation-agnostic:

```text
winning CameraRequest
  -> CameraRigComposer
  -> composer.CinemachineCamera
  -> CameraOutputRigApplicator
  -> persistent output
```

No `switch(Presentation)` belongs in output arbitration merely because additional
presentation models exist.

## 5. Local presentation semantics

### Fixed

```text
Position Control
  none

Rotation Control
  none
  or supported Look At behavior

Pose
  authored CinemachineCamera Transform
```

Apply/Rebuild preserves the authored pose.

### Follow

```text
Position Control
  CinemachineFollow

Rotation Control
  none when Look At does not participate
  CinemachineHardLookAt when Look At participates
```

`FollowOffset` remains Follow-specific authoring.

### Mounted

```text
Position Control
  CinemachineHardLockToTarget

Rotation Control
  CinemachineRotateWithFollowTarget
```

The explicit Tracking target/mount owns the pose. Camera does not own gameplay
input that moves or rotates that mount.

### Third Person

```text
Position / presentation
  CinemachineThirdPersonFollow

Separate generic Aim stage
  none in the accepted first contract
```

The accepted authored settings include Shoulder Offset, Vertical Arm Length,
Camera Side, Camera Distance and Damping.

These presentation semantics are normative in IF-ADR-022.

## 6. Typed request contract

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

Presentation Model is **not** request priority evidence.

## 7. Deterministic arbitration

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

## 8. Transactional logical / physical integrity

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

IF-ADR-022 materialization does not change this transactional boundary.

## 9. Scope ownership and component lifetime

Camera ownership has two distinct lifetime layers.

### 9.1 Logical owner lifetime

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

### 9.2 Publication/component lifetime

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

For Route and Activity this does not synthesize a Route/Activity exit and does
not clear their logical-owner state. Re-enable does not silently re-publish.

`SessionCameraOverrideBinding` is intentionally different: the component itself
owns Session availability, so disable/destroy ends that owner scope through
`EndOwnerScope(...)`.

Normal lifecycle exit, abnormal component loss, repeated cleanup and re-enable
without silent republish remain certified by IF-ADR-004C.

## 10. Target resolution

Target resolution remains explicit and typed.

Current target-source architecture may resolve:

```text
Explicit Transform
Player Composer
Player Slot
Route
Activity
Player Group
```

Required target failures block.

Runtime target resolution must not use:

```text
GameObject.Find
object-name lookup
tag lookup as authority
hierarchy guessing
Camera.main
first compatible Player
nearest Actor
global service lookup
```

Presentation materializers consume already-resolved targets and do not perform
their own scene discovery.

## 11. Runtime / Editor boundary

Editor tooling may:

- validate one local rig;
- materialize one supported presentation pipeline;
- reconcile Framework-owned technical components;
- expose provenance and diagnostics;
- validate persistent Camera composition.

Editor tooling never becomes runtime output authority.

Runtime Camera code must not depend on Editor assemblies or Editor-only state.

`CameraOutputRigApplicator` remains presentation-agnostic.

## 12. Materialization ownership

IF-ADR-022 adds explicit local materialization provenance.

Conceptually one Composer retains evidence for:

```text
materialized Presentation Model
CinemachineCamera
Framework-owned Position Control
Framework-owned Rotation Control
materialization revision
last materialization result
```

Ownership is conservative:

```text
exact previously recorded Framework reference
  -> FrameworkOwned

compatible pre-existing component without provenance
  -> ExternalOrUnknown
```

A compatible external component may be used where valid, but is not silently
adopted as Framework-owned.

An incompatible external/unknown component blocks Apply/Rebuild.

Framework-owned incompatible controls may be replaced during an explicit model
switch.

No local component is deletable merely because of:

```text
component type
object name
hierarchy location
"looks generated"
```

## 13. Product surface and diagnostics

Supported product-facing surfaces include:

- `CameraRigComposer`;
- `CameraOutputSessionBinding`;
- Session / Route / Activity Camera override bindings;
- typed Local Player Camera publication;
- authoring/composition validation;
- model-specific Apply/Rebuild;
- Advanced / Diagnostics evidence.

The Composer Inspector is model-specific rather than a generic Cinemachine graph
editor.

Normal authoring exposes product intent. Advanced/Diagnostics may expose:

```text
Presentation
CinemachineCamera
Position Control
Rotation Control
Framework-owned / ExternalOrUnknown
resolved targets
materialization revision
last materialization result
blocking conflict
```

Output diagnostics remain separate:

```text
Output ID
Request ID
Owner / Lifetime
Precedence / Tie-Breaker
admitted request set
current winner
physical apply result
rollback evidence
```

## 14. Technical certification — 2026-08-15

The current Camera technical boundary is certified by one aggregate run.

### ADR-022 presentation materialization

```text
[QA][ADR022 Presentation Models]
status='Passed'
cases='14/14'
```

Coverage:

```text
Follow existing compatibility
Follow Look At rotation materialization
Fixed authored pose preservation
Fixed Look At rotation materialization
Mounted materialization
Third Person materialization
Follow -> Third Person -> Follow switching
idempotent rebuild
external compatible component not adopted
unknown conflict blocks
blocked switch has no partial mutation
external component not deleted
no output-authority mutation
unsupported model has no fallback
```

The existing Follow pipeline also passed its supporting `6/6` smoke.

### C9R — positive authority lifecycle

```text
[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]
status='Passed'
phase='canonical-override-fixture'
cases='11'
```

C9R proves the canonical authority ladder, restoration behavior, duplicate
publish/release handling and Activity/Route lifecycle cleanup.

### IF-ADR-004B — negative integrity

```text
[QA_CAMERA_ADR004B]
status='Passed'
cases='18/18'
failed='0'
blocked='0'
verdict='ADR-004B CAMERA NEGATIVE INTEGRITY CERTIFIED'
```

### IF-ADR-004C — owner lifetime integrity

```text
[QA_CAMERA_ADR004C]
status='Passed'
cases='10/10'
failed='0'
verdict='ADR-004C CAMERA OWNER LIFETIME INTEGRITY CERTIFIED'
```

### Full Camera

```text
[QA_CAMERA_FULL]
status='Completed'
verdict='CAMERA QA CERTIFIED'
adr022Presentation='PASS'
canonicalAuthority='PASS'
adr004NegativeIntegrity='PASS'
adr004OwnerLifetime='PASS'
mandatoryCases='53'
executedCases='53'
passedCases='53'
```

The aggregate `53/53` is the current technical Camera certification authority.

## 15. Certification history

The Camera authority certification remains evidence-driven:

```text
004A
  reconcile normative authority
      ↓
004B first execution
  owner-loss defect reproduced
      ↓
004C
  owner-lifetime correction
  10/10
      ↓
004B
  18/18
      ↓
ADR-022
  presentation expansion C1-C4 implemented
      ↓
ADR-022 presentation QA
  14/14
      ↓
Full Camera QA
  C9R 11/11
  ADR-004B 18/18
  ADR-004C 10/10
  aggregate 53/53
```

The historical IF-ADR-004B/C records remain historical evidence and are not
rewritten to pretend they tested IF-ADR-022.

## 16. Non-goals / deferred work

This ADR does not authorize:

- global `CameraManager` / service locator / static request registry;
- timing-based priority;
- multiple simultaneous physical outputs;
- split-screen;
- concurrent per-player physical output ownership;
- generic cross-feature request broker;
- second Composer around the same local rig intent;
- automatic creation of persistent output from a local rig;
- Camera ownership of AudioListener behavior;
- arbitrary Cinemachine graph authoring as the Framework product contract;
- Camera ownership of Player lifecycle or Initial Placement.

Presentation features deliberately deferred by IF-ADR-022 include Orbital /
Free Look input authority, spline/dolly, group framing product models, 2D framed
follow, shake/noise/impulse product authoring, Third Person Aim, advanced camera
collision policy, cinematic sequencing, advanced blend policy, multi-output,
split-screen and XR Camera authority.

## 17. Current disposition

```text
Architecture
  ACCEPTED

Package — current single-output authority
  IMPLEMENTED

Package — ADR-022 local presentation expansion
  IMPLEMENTED

Product Surface / Diagnostics
  IMPLEMENTED / CONFORMANT

Technical QA
  CAMERA QA CERTIFIED
  ADR-022 Presentation 14/14
  C9R 11/11
  IF-ADR-004B 18/18
  IF-ADR-004C 10/10
  Full Camera 53/53

FIRSTGAME
  PENDING broader Camera consumer proof / ADR-022 C6

Current technical blocker
  NONE for the accepted single-output + presentation boundary
```

FIRSTGAME consumer proof may promote confidence/maturity of the new presentation
models, but it does not reopen the technically certified ADR-004 output authority
or ADR-022 C1-C5 implementation by default.
