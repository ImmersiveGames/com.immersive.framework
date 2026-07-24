# Persistent Content Camera Baseline

Status: Current  
Last updated: 2026-07-24

## Purpose

Prepare the minimum Session Camera composition inside the application Persistent
Content Scene before that scene becomes the source of the official Scene Template.

The scene remains the concrete authority. The helper only creates the first
baseline explicitly and never becomes a runtime owner.

## Workflow

```text
Game Application
  Persistent Content
    Content Scene
```

1. Assign the Content Scene.
2. Press `Open Content Scene`.
3. Return to the Game Application asset.
4. Press `Add Minimum Camera to Open Scene`.
5. Review the created hierarchy.
6. Save the Content Scene manually.
7. Press `Validate Configuration`.
8. After the scene passes, create the official Scene Template from it.

The action does not open or save scenes automatically.

## Created hierarchy

```text
Persistent Camera
  Camera Output
    Unity Camera
    CinemachineBrain
    CameraOutputSessionBinding
    SessionCameraOverrideBinding

  Session Camera Target

  Session Camera Rig
    CinemachineCamera
    CinemachineFollow
    CinemachineRotationComposer
    CameraRigComposer
```

## Explicit identities

```text
Output ID:
  camera.output.main

Session Scope ID:
  camera.scope.session.default

Session Request ID:
  camera.request.session.default

Session Tie Breaker ID:
  camera.tie.session.default

Session precedence:
  300
```

These values establish a valid default Session request. A product may later replace
the default rig or publish Player, Route or Activity requests through their
respective owners.

## Safety behavior

The helper is non-destructive:

```text
complete existing baseline
  preserved without changes

empty Camera composition
  baseline created with Undo

partial or conflicting Camera composition
  blocked explicitly
  no merge
  no repair
  no duplicate creation
```

## Validation

`Validate Configuration` now verifies:

```text
one Unity Camera
one CinemachineBrain
one CameraOutputSessionBinding
one SessionCameraOverrideBinding

output ID
Camera reference
Brain reference
Camera and Brain on the same GameObject

assigned Game Application
persistent output reference
Scope ID
Request ID
Tie Breaker ID
CameraRigComposer reference
Target Source reference
CinemachineCamera reference
required explicit targets
```

Presentation validation remains independent:

```text
at least one Canvas
at least one Transition adapter
at least one Loading adapter
```

## Out of scope

```text
SceneTemplateAsset creation
package template source scene
Player Camera request
Route Camera request
Activity Camera request
multiple outputs
split screen
AudioListener ownership
render-pipeline-specific Camera components
```
