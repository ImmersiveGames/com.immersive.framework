# 00 — Current State

Status: **canonical P3 source baseline frozen; Scene Local Player architecture accepted**  
Last reconciled: **2026-07-16**  
Decisions: `../ADRs/P3-ADR-Canonical-Player-Lane.md`, `../ADRs/Product/ADR-PROD-0006-camera-requests-output-contexts.md`, `../ADRs/Product/ADR-PROD-0013-scene-local-player-admission.md`

For the active execution block, read `05-Execution-Status.md`.

## Read-only source baseline

```text
com.immersive.framework
  commit: 385c957a8fefb53f0daf395c662ffa7d5fedc996
  package: 1.0.0-preview.15

QAFramework
  commit: 993f8e698edb8c826054c9f8faa8bd344fbc8013
```

This freezes source identity only. Unity import, compile and runtime smoke PASS are not inferred from Git inspection.

## Canonical Player lane

```text
PlayerSlotProfile
-> participation and Slot reservation
-> one explicit physical source
-> LocalPlayerHostAuthoring admission
-> Actor selection/preparation
-> gameplay occupancy
-> input and camera eligibility
-> Activity admission
```

The supported physical sources are:

```text
Manual local join
  PlayerInputManager provisions one runtime-created local Player Host.

Scene Local Player Admission
  an Activity admits one explicitly referenced existing scene Host without provisioning.
```

Both paths use the same Slot, participation, preparation, readiness and release domain. Neither path pre-authors `PlayerSlotId` or runtime `ActorId` on the physical Host.

## PreAuthored status

`PreAuthoredPlayerComposer` is an experimental alternative surface and is not canonical P3 authority.

It remains temporarily because Camera, shared Editors and QA still contain direct dependencies. The required transition is:

```text
P3M2 decouple consumers
-> P3M3 remove PreAuthored destructively
-> P3M4 promote Scene Local Player Admission
```

No compatibility alias or silent bridge is allowed.

## Camera product and runtime authority

```text
CameraRigRecipe
  reusable Cinemachine presentation intent

CameraRigComposer
  designer-facing rig instance and idempotent materialization

Camera target source
  typed provider independent of PreAuthoredPlayerComposer

CameraOutputSessionBinding
  explicit physical Unity Camera + CinemachineBrain output

CameraOutputContext
  sole winner-selection authority for one output
```

Camera does not create Player identity, decide Player admission or enable gameplay. Local Player camera publication occurs only after the Player is eligible through the P3 readiness path.

## Runtime authority rules

```text
Player participation authority is scoped and typed.
PlayerInputManager owns only runtime provisioning mechanics.
Scene admission owns no physical creation or destruction.
Activity owns contextual admission requirements.
CameraOutputContext owns camera winner selection.
Profiles remain immutable runtime inputs.
```

No singleton, service locator, functional name lookup, hierarchy fallback or silent required-state fallback is allowed.

## Current execution boundary

Architecture/documentation cuts `P3M0` and `P3M1` are complete when this patch is applied.

The next technical cut is:

```text
P3M2 — Decouple Camera, shared Editors and QA from PreAuthoredPlayerComposer
```

H5 remains a manual Unity gate and must not be marked Passed without import, compile and the required P3 aggregate smoke evidence.
