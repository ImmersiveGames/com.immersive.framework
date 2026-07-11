> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# C5 — CameraComposer SinglePlayer MVP Manifest

Status: Product implementation delta
Date: 2026-07-09
Package: `com.immersive.framework`
Surface: `Camera Recipe / Camera Composer`

## Objective

Create the first official Camera Product Surface MVP after the Cinemachine architecture reset.

This cut proves the product relationship:

```text
PlayerComposer owns player intent and exposes CameraTarget / LookAtTarget.
CameraComposer owns camera intent and explicitly consumes PlayerComposer targets.
Cinemachine rig materialization executes the camera presentation.
```

## Files created

```text
Runtime/CameraAuthoring/CameraRecipe.cs
Runtime/CameraAuthoring/CameraComposer.cs
Runtime/CameraAuthoring/CameraComposerDebugSnapshot.cs
Editor/CameraAuthoring/CameraComposerApplyRebuildResult.cs
Editor/CameraAuthoring/CameraComposerApplyRebuildUtility.cs
Editor/CameraAuthoring/CameraComposerEditor.cs
Documentation~/Product/C5-CAMERA-COMPOSER-SINGLEPLAYER-MVP-MANIFEST.md
```

## Files changed

```text
none
```

## Files removed

```text
none
```

## Product surface affected

```text
CameraRecipe
CameraComposer
SinglePlayerFollowCamera
```

## Flow of use expected

```text
1. Add CameraComposer to a camera rig root.
2. Set Mode = SinglePlayerFollowCamera.
3. Set Ownership = SinglePlayer.
4. Set Target Source Kind = PlayerComposer.
5. Assign the explicit PlayerComposer from the player prefab/scene instance.
6. Click Validate.
7. Click Apply / Rebuild.
8. CameraComposer materializes or repairs the local Cinemachine rig.
9. Debug shows resolved PlayerComposer source, Follow target, LookAt target, Unity Camera, Cinemachine Camera and materialization summary.
```

## Out of scope

```text
FIRSTGAME changes
QAFramework smoke
RouteCamera runtime rewrite
ActivityCamera runtime rewrite
LocalPlayerCamera
SharedPlayerGroupCamera
split-screen
spectator/debug runtime camera
CameraRuntimeContext
CameraManager
service locator
Camera.main fallback
PlayerSlot runtime resolution
```

## Technical decisions

- `CameraComposer` is a runtime authoring component, not runtime authority.
- `CameraComposer` supports only `SinglePlayerFollowCamera` in this MVP.
- `CameraComposer` supports only `PlayerComposer` and `ExplicitTransform` target sources in this MVP.
- `PlayerComposer` source is explicit. There is no scene lookup, hierarchy lookup or name/path fallback.
- Cinemachine materialization is delegated to the C4 editor-only utility.
- Advanced/Debug fields expose technical evidence but do not become the designer's primary workflow.

## Technical acceptance criteria

```text
Unity compiles.
Runtime has no Editor dependency.
CameraComposer does not use Camera.main.
CameraComposer does not create CameraManager, singleton or service locator.
Missing PlayerComposer blocks validation/apply explicitly.
Unsupported camera modes block explicitly.
Apply/Rebuild is idempotent.
Second Apply/Rebuild does not duplicate Unity Camera, CinemachineBrain or CinemachineCamera.
Debug fields show resolved follow/look-at targets and materialization summary.
```

## Product acceptance criteria

```text
Designer can add CameraComposer from Add Component menu.
Designer can assign PlayerComposer explicitly.
Designer can click Validate and Apply/Rebuild from the Inspector.
Camera follows PlayerComposer.CameraTarget through Cinemachine.
Camera LookAt uses PlayerComposer.LookAtTarget when configured.
Priority is visible and editable.
Technical rig details are under Advanced/Debug.
```

## Smoke expected

Manual editor smoke:

```text
1. Use a scene with a configured PlayerComposer.
2. Create an empty GameObject named Camera Rig.
3. Add CameraComposer to Camera Rig.
4. Assign PlayerComposer.
5. Click Validate.
6. Click Apply/Rebuild.
7. Confirm log blocked='0'.
8. Click Apply/Rebuild again.
9. Confirm second run created='0' and blocked='0'.
```

Expected first run example:

```text
[Immersive.Framework][CameraComposer] Apply/Rebuild completed. camera='Camera Rig' mode='SinglePlayerFollowCamera' source='PlayerComposer' priority='10' created='3' repaired='3' alreadyValid='0' skipped='0' blocked='0'
```

Expected second run example:

```text
[Immersive.Framework][CameraComposer] Apply/Rebuild completed. camera='Camera Rig' mode='SinglePlayerFollowCamera' source='PlayerComposer' priority='10' created='0' repaired='0' alreadyValid='6' skipped='0' blocked='0'
```

## Risks

```text
Cinemachine serialized API may require adjustment if Unity changes Cinemachine 3.x members.
PlayerComposer currently provides fallback targets, so missing explicit camera anchors may not fail until stricter policy is added.
Route/Activity camera skeleton remains separate until a later Cinemachine-aware rewrite.
```

## Next expected cut

```text
C6 — FIRSTGAME proof
```

## Suggested commit message

```text
Product: add Single Player Cinemachine CameraComposer
```
