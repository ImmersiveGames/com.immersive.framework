# 00 — Current State

Status: **P3M3 source prepared; Unity validation pending**
Last reconciled: **2026-07-17**
Decisions: `../ADRs/P3-ADR-Canonical-Player-Lane.md`, `../ADRs/Product/ADR-PROD-0006-camera-requests-output-contexts.md`, `../ADRs/Product/ADR-PROD-0013-scene-local-player-admission.md`

For the active execution gate, read `05-Execution-Status.md`.

## Validated predecessor

P3M2 is closed from supplied Unity evidence:

```text
C9M Follow Pipeline
  PASS — cases=6

C9R Camera Override Authority
  PASS — cases=11

P3 Canonical Pre-FIRSTGAME
  PASS — phases=2, cases=31

P3B alternative surface regression
  PASS — cases=5 before destructive removal
```

The P3B result was sequencing evidence only. It did not make the alternative surface canonical.

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

Supported physical sources remain:

```text
Manual local join
  PlayerInputManager provisions a runtime-created Local Player Host.

Scene Local Player Admission
  an Activity admits an explicitly referenced existing scene Host without provisioning.
```

Scene Local Player Admission remains an ordered future product cut. P3M3 does not promote staging code or create a second runtime authority.

## Camera target boundary

```text
explicit Follow / Look At transforms
or an Actor-owned ICameraTargetSource
-> CameraRigComposer.ResolveCameraTargets
-> Player camera eligibility verifies resolved evidence
-> CameraRequest
-> CameraOutputContext
```

Camera eligibility no longer checks, names or depends on the removed Player authoring surface. Required target failures remain explicit.

## P3M3 removal boundary

The following alternative product lane is removed:

```text
Pre-authored Player Composer component
Pre-authored Player Recipe asset
Composer Inspector
Composer Apply / Rebuild utility
P3B alternative smoke
```

No alias, wrapper, compatibility facade or null bridge remains in runtime or Editor code.

The existing canonical surfaces remain:

```text
LocalPlayerProvisioningAuthoring
LocalPlayerProvisioningHostRegistration
LocalPlayerHostAuthoring
PlayerActorDeclaration
PlayerGameplayCameraAuthoring
CameraRigComposer
```

## Runtime authority rules

```text
Player participation authority is scoped and typed.
GameApplication resolves provisioning through the explicit UIGlobal Host Registration; global authoring discovery is not a supported bootstrap path.
PlayerInputManager owns only runtime provisioning mechanics.
Scene admission owns no physical creation or destruction.
Activity owns contextual admission requirements.
CameraOutputContext owns camera winner selection.
Camera target providers supply evidence only.
Profiles and Recipes remain immutable runtime inputs.
```

No singleton, service locator, functional name lookup, hierarchy fallback or silent required-state fallback is allowed.

## Active validation gate

P3M3 closes only after Unity proves:

```text
Framework import and compile
QAFramework import and compile
C9R setup repairs any old Missing Script evidence
C9M PASS
C9R PASS
canonical P3 aggregate PASS
no P3B menu remains
no Missing Script remains
```

## P3M4A — Scene Local Player Admission authoring

The designer-facing Scene Local Player Admission surface, typed Actor source evidence and dual-shape Local Player Host validation are implemented. Unity import/compile and the P3M4A authoring smoke are pending. Runtime Activity admission remains P3M4B.
