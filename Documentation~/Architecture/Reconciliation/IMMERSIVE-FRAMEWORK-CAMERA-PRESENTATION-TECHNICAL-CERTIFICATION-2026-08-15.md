# Immersive Framework — Camera Presentation Technical Certification — 2026-08-15

Status: **Technical Closure / Camera QA Certified / FIRSTGAME Promotion Pending**  
Date: **2026-08-15**  
Primary ADRs: IF-ADR-004, IF-ADR-010, IF-ADR-022  
Package implementation baseline: `b645f8db57673cbdc3531ce12b6d399225a4d0cb` (`ADR22`)  
Consumer validation: **FIRSTGAME C6 pending**

## 1. Purpose

This record closes the technical implementation/certification portion of
IF-ADR-022 — Camera Rig Presentation Models and Materialization Authority.

It records that the Framework expanded local Camera presentation beyond Follow
without reopening or regressing the already accepted IF-ADR-004 Camera
request/output authority.

This is not a claim that every future Camera feature is complete.

It certifies the accepted boundary:

```text
single persistent Camera output
+
existing typed Camera request authority
+
Fixed / Follow / Mounted / Third Person local presentation
+
ownership-safe Editor materialization
+
model-specific Inspector / diagnostics
```

## 2. Frozen authority split

```text
CameraOutputContext
  owns admitted requests and deterministic winner

CameraOutputSession
  owns transactional logical/physical synchronization

CameraOutputRigApplicator
  projects the winner

CameraOutputSessionBinding
  owns explicit persistent Unity Camera + CinemachineBrain

CameraRigComposer
  owns one local rig's Presentation intent/materialization
```

IF-ADR-022 does not move arbitration/output authority into the Composer.

## 3. Implemented presentation family

```text
Undefined = 0
Follow = 10
Fixed = 20
Mounted = 30
ThirdPerson = 40
```

`Follow = 10` is preserved for Unity serialized compatibility.

### Fixed

```text
Body
  none

Aim
  none or CinemachineHardLookAt

Pose
  authored CinemachineCamera Transform
```

### Follow

```text
Body
  CinemachineFollow

Aim
  none or CinemachineHardLookAt
```

### Mounted

```text
Body
  CinemachineHardLockToTarget

Aim
  CinemachineRotateWithFollowTarget
```

### Third Person

```text
Body / presentation
  CinemachineThirdPersonFollow

extra generic Aim
  none
```

## 4. C1 — Presentation contracts — CLOSED

Implemented:

- explicit Presentation identities;
- model-valid target semantics;
- model-specific serialized settings;
- preserved Follow identity;
- one Composer remains canonical.

No Camera output/runtime arbitration change was introduced.

## 5. C2 — Safe materialization ownership — CLOSED

The implementation extends the existing Cinemachine materialization evidence.

Durable evidence includes:

```text
Presentation
materialization revision
CinemachineCamera
CinemachineCamera ownership
Position Control
Position ownership
Rotation Control
Rotation ownership
```

Ownership classes:

```text
FrameworkOwned
ExternalOrUnknown
```

Framework ownership requires exact retained provenance.

Pre-existing compatible content is not silently adopted.

Unknown incompatible content blocks.

## 6. C3 — Model materializers — CLOSED

Implemented in order:

```text
Follow completion/repair
Fixed
Mounted
Third Person
```

Materialization uses explicit model dispatch.

There is no runtime reflection registry.

Body and Aim stages are preflighted before model-switch mutation.

Only proven Framework-owned incompatible controls may be removed/replaced.

## 7. C4 — Inspector / UX — CLOSED

`Presentation` is designer-editable.

The Inspector shows only model-meaningful targets/settings.

Advanced / Diagnostics exposes:

```text
materialized Presentation
current Body / Aim
Framework-owned references
ExternalOrUnknown classification
materialization revision
last result
blocking issue
```

The normal surface does not become a generic Cinemachine graph editor.

## 8. C5 — QA — CERTIFIED

### 8.1 Existing Follow compatibility

```text
[QA][C9M Follow Pipeline]
PASS
cases='6'
```

### 8.2 ADR-022 presentation materialization

```text
[QA][ADR022 Presentation Models]
PASS
cases='14/14'
```

Cases:

```text
follow-existing-compatibility
follow-lookat-rotation-materialized
fixed-authored-pose-preserved
fixed-lookat-rotation-materialized
mounted-pipeline-materialized
third-person-pipeline-materialized
switch-follow-thirdperson-follow
idempotent-rebuild
external-compatible-not-adopted
unknown-conflict-blocks
blocked-switch-no-partial-mutation
external-component-not-deleted
no-output-authority-mutation
unsupported-model-no-fallback
```

### 8.3 Canonical C9R authority lifecycle

```text
[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]
status='Passed'
phase='canonical-override-fixture'
cases='11'
```

Completed:

```text
player-default
activity-request
route-request
session-request
session-release-restores-route
route-release-restores-activity
activity-release-restores-player
duplicate-request
duplicate-release
activity-lifecycle-cleanup
route-lifecycle-cleanup
```

### 8.4 IF-ADR-004B negative integrity

```text
[QA_CAMERA_ADR004B]
status='Passed'
cases='18/18'
failed='0'
blocked='0'
verdict='ADR-004B CAMERA NEGATIVE INTEGRITY CERTIFIED'
```

### 8.5 IF-ADR-004C owner lifetime

```text
[QA_CAMERA_ADR004C]
status='Passed'
cases='10/10'
failed='0'
verdict='ADR-004C CAMERA OWNER LIFETIME INTEGRITY CERTIFIED'
```

### 8.6 Full Camera terminal

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

This is the terminal technical certification result.

## 9. C9R fixture reconciliation

During certification preparation, the canonical C9R setup exposed:

```text
cinemachine-camera:multiple-local-candidates
```

The package behavior was correct: ambiguous local Camera candidates must block.

The causal defect was QA authoring: the C9R Session rig retained one legacy
`CinemachineCamera` component in addition to the canonical child camera.

Correction was QA-only.

The C9R installer now normalizes its own QA-owned subtree to exactly one canonical
local `CinemachineCamera` before Apply/Rebuild.

Observed repair:

```text
status='Repaired'
repair='RemovedExtraQaOwnedCinemachineCameraComponents'
count='1'
```

followed by:

```text
[_CAMERA_OVERRIDE_AUTHORITY_SETUP]
status='Succeeded'
```

The package `multiple-local-candidates` guard was not weakened.

## 10. Full Camera orchestration reconciliation

The old manual flow was replaced with one aggregate certification entry.

The Full Camera QA:

```text
1. runs ADR-022 Presentation QA
2. uses the authored QA Hub Camera RouteRequestTrigger
3. enters C9R
4. lets the canonical coordinator start the C9R fixture
5. waits for the C9R scene lifecycle/evidence
6. runs ADR-004B certification
7. runs ADR-004C certification
8. emits one terminal Camera verdict
```

The installer is authoring/setup repair, not part of normal repeated
certification.

Historical revision hashes were removed from ADR-004B/C runtime verdicts. Git
history/documentation records source revision; runtime certification records
contracts.

## 11. Non-blocking QA hygiene

The final certified run emitted exactly three Unity warnings during C9R teardown:

```text
The referenced script (Unknown) on this Behaviour is missing!
```

No Camera QA case was `Failed` or `Blocked`.

The C9R lifecycle completed, ADR-004B/C passed and Full Camera ended 53/53.

Classification:

```text
QA FIXTURE AUTHORING HYGIENE
not Package failure
not ADR-022 failure
not Camera certification blocker
```

A later QA-only hygiene cut should remove the missing serialized script
references so normal certification logs are clean.

## 12. Source-control traceability

Package implementation is synchronized at:

```text
ImmersiveGames/com.immersive.framework
master
b645f8db57673cbdc3531ce12b6d399225a4d0cb
ADR22
```

At documentation time the public QA branch still points to:

```text
rinnocenti/QAFramework
main
02f2d5589ba9bee88ac512d429f435e1dd1ba584
```

The certification run used the active QA working tree containing the new
ADR-022/C9R/Full Camera QA files.

Therefore:

```text
technical behavior
  CERTIFIED

package source traceability
  SYNCHRONIZED

QA remote traceability
  REQUIRES PUSH/SYNC OF TESTED WORKING TREE
```

This is source-control hygiene, not an implementation gap.

## 13. FIRSTGAME boundary

C6 remains pending.

FIRSTGAME should prove real consumer usage for:

```text
Fixed Route/Activity shot
Follow gameplay camera
Mounted camera mount
Third Person gameplay camera
runtime override between separate rigs
broken-configuration diagnostics
```

C6 is consumer integration/ergonomics proof.

It does not require new Framework implementation unless it exposes a concrete
defect or missing accepted contract.

## 14. Deferred Camera capabilities

This closure does not promote:

```text
Orbital / Free Look input authority
Spline / Dolly
Group Framing
2D Framed Follow
noise / impulse / shake authoring
Third Person Aim
advanced collision policy
Timeline/cinematic sequencing
advanced blend policy
multi-output
split-screen
per-player physical output
XR Camera authority
```

Those remain future product decisions.

## 15. Closure statement

```text
IF-ADR-004 request/output authority
  PRESERVED / RECERTIFIED

IF-ADR-010 Camera Class C product surface
  RECONCILED

IF-ADR-022 architecture
  ACCEPTED

IF-ADR-022 C1
  CLOSED

IF-ADR-022 C2
  CLOSED

IF-ADR-022 C3
  CLOSED

IF-ADR-022 C4
  CLOSED

IF-ADR-022 C5
  CAMERA QA CERTIFIED
  53/53

IF-ADR-022 C6
  FIRSTGAME CONSUMER PROOF PENDING

Current package implementation blocker
  NONE

Current technical Camera certification blocker
  NONE
```
