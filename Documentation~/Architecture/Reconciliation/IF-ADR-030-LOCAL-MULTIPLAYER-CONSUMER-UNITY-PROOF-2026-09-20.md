# IF-ADR-030 Local Multiplayer Consumer Unity Proof — 2026-09-20

Status: **IMPLEMENTED = YES / CONSUMER UNITY TESTED = YES / INTEGRATED = YES / VALIDATED = NO**

Scope: IF-ADR-030 CAMERA-030-A consumer integration through the current Local Multiplayer Group Camera sample.

Related:
- [IF-ADR-029 — Camera Composition, Group Presentation and Camera View Removal](../ADRs/IF-ADR-029-Camera-Composition-Group-Presentation-and-Camera-View-Removal.md)
- [IF-ADR-030 — Camera Subject Framing Evidence](../ADRs/IF-ADR-030-Camera-Subject-Framing-Evidence.md)

## 1. Purpose

This record preserves the 2026-09-20 manual consumer Play Mode evidence for the current
Group Camera path without relabeling package-local tests or historical Camera certification
as current QA proof.

The current consumer composition is:

```text
Local Multiplayer Player Slot
  -> dedicated Group ActorProfile
  -> dedicated Group Actor Presentation
     -> local multiplayer movement
     -> ActorCameraSubjectAuthoring
        Observation = presentation-owned Group framing anchor
        Framing Radius = presentation-owned Subject extent
            ↓
CameraSharedComposition
  -> Group Camera Rig
  -> CinemachineTargetGroup
  -> CinemachineGroupFraming
  -> Camera Output
```

## 2. Manual Unity evidence

Manual Play Mode on the current Local Multiplayer consumer confirmed:

```text
P1 joins
  -> actor-profile.farmer.group selected
  -> Actor prepared and physically materialized
  -> gameplay admitted / GameplayReady
  -> shared Group Camera consumer is functional

P2 joins
  -> actor-profile.cow.group selected
  -> Actor prepared and physically materialized
  -> gameplay admitted / GameplayReady
  -> Activity readiness completes with both Players

dedicated Local Multiplayer movement
  -> functional for the new Group Presentations

per-Subject Camera framing
  -> current Group consumer uses the Presentation-authored observation/framing evidence
  -> resulting framing behavior is functional
```

The consumer was manually judged functional. Remaining Camera work in this sample is visual
tuning of authored Group Camera behavior values, not a missing runtime architecture contract.

## 3. Scope of the proof

This evidence supports:

```text
CAMERA-030-A consumer integration = PASS
Local Multiplayer Group Camera consumer = PASS
dedicated Group Actor Presentations = PASS
dedicated Local Multiplayer movement = PASS
per-Subject framing evidence = PASS
```

It does not claim:

```text
package-local Unity Test Framework execution
QAFramework execution for CAMERA-030
full negative-path Camera certification
technical validation of the complete CAMERA-029/030 boundary
final Camera certification
```

Therefore the correct disposition remains:

```text
IMPLEMENTED          = YES
CONSUMER UNITY TESTED = YES
INTEGRATED           = YES
VALIDATED            = NO
CERTIFIED            = NO
```
