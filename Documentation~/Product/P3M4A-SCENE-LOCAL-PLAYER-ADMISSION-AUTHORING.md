# P3M4A — Scene Local Player Admission Authoring

Status: Implementation delivered; Unity validation pending.

## Objective

Promote the designer-facing product surface for a local Player that already exists in an Activity scene, without introducing runtime identity or a second provisioning path.

## Product flow

```text
GameObject > Immersive Framework > Player > Scene Local Player Admission

Assign:
  Player Slot Profile
  Local Player Host
  Actor Profile
  Scene Logical Player Actor
  Admission Timing

Apply / Rebuild
Validate
```

`Apply / Rebuild` validates explicit references and materializes `SceneLogicalPlayerActorEvidence` on the exact Logical Actor. It does not reserve a Slot, call `PlayerInputManager.JoinPlayer`, generate a runtime ActorId, enable input/camera or start gameplay.

## Runtime boundary

The runtime may later consume only:

```text
PlayerSlotProfile
LocalPlayerHostAuthoring
ActorProfile
PlayerActorDeclaration
SceneLogicalPlayerActorEvidence
SceneLocalPlayerAdmissionTiming
```

Runtime must not use `AssetDatabase`, `PrefabUtility`, object names, tags or fallback discovery.

## Host contract

`LocalPlayerHostAuthoring` now supports two validation shapes:

```text
Provisioned host
  empty Actor Mount

Scene local Player host
  exact PlayerActorDeclaration under Actor Mount
  exactly one ActorDeclaration
  exactly one PlayerInput on the technical host
```

Existing provisioning methods and signatures remain unchanged.

## Next checkpoint

P3M4B connects this surface to exact Slot reservation, externally-owned Actor preparation, Activity enter/exit and release compensation. P3M5 then expands QA negative and rollback coverage.
