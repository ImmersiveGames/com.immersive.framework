# Camera Usage

Status: Current for the single-output product
Last updated: 2026-07-23

## Create a reusable rig

1. Create a `CameraRigRecipe`.
2. Add `CameraRigComposer` to the virtual-rig root.
3. Assign typed Follow/LookAt sources and framing.
4. Validate.
5. Run Apply/Rebuild.

Apply/Rebuild idempotently creates or repairs the Cinemachine virtual rig and
target pipeline. It does not create the physical output or choose the winner.

## Create the persistent output

In `UIGlobal`, create one output root with:

```text
Unity Camera
CinemachineBrain
CameraOutputSessionBinding
SessionCameraOverrideBinding
```

Use one explicit output id. Gameplay scenes do not serialize references to this
persistent object; Framework Core injects it into scoped consumers.

## Publish camera intent

- `PlayerGameplayCameraAuthoring` supplies the normal eligible Player request.
- `LocalPlayerCameraRequestBinding` is authoring/evidence; its scene
  auto-publisher is opt-in and must not duplicate gameplay admission publishing.
- `ActivityCameraOverrideBinding` and `RouteCameraOverrideBinding` publish only
  after explicit `RequestOverride()` and release with `ReleaseOverride()`.
- `SessionCameraOverrideBinding` covers transitions.

Default precedence:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

`CameraOutputContext` selects the winner. Owners do not toggle the physical
Camera or compete by editing Cinemachine priority.

## Diagnose

Use Advanced/Debug and the output snapshot to inspect output, request, owner,
scope, Slot, precedence, targets, rig, winner and last diagnostic.

Do not use `Camera.main`, name lookup, singleton, service locator or cross-scene
fallback.

## Manual validation

1. Compile Framework and QAFramework with Cinemachine installed.
2. Validate and Apply/Rebuild a rig twice; the second run must be idempotent.
3. Confirm Player → Activity → Route → Session precedence and reverse restoration.
4. Exercise Route/Activity exit and transition release.
5. Confirm one persistent output and no duplicate publisher for a Player.
6. Validate framing and restoration visually in FIRSTGAME.
