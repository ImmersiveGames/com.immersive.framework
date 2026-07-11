# C9N — Camera Rig Composer Output Separation

## Problem

`CameraRigComposer` still exposed and defaulted:

```text
Create Unity Camera If Missing = true
```

Its Apply/Rebuild request therefore materialized:

```text
Unity Camera
CinemachineBrain
Cinemachine Camera
```

inside each rig.

That contradicts the C9 camera architecture:

```text
CameraOutputSessionBinding
  owns one physical output

CameraRigComposer
  owns one virtual Cinemachine rig
```

## Decision

`CameraRigComposer` now materializes only:

```text
CinemachineCamera
Follow target
LookAt target
```

It never creates:

```text
UnityEngine.Camera
CinemachineBrain
AudioListener
CameraOutputSessionBinding
```

## Generic materializer

`CinemachineRigMaterializer` retains explicit technical support for output creation through:

```text
MaterializeUnityOutput = true
```

The default is false.

`CameraRigComposerApplyRebuildUtility` always sends:

```text
MaterializeUnityOutput = false
CreateUnityCameraIfMissing = false
```

## Product result

Fresh Player Rig:

```text
Player Rig
├── CameraRigComposer
└── Cinemachine Camera
```

Separate output:

```text
Gameplay Camera
├── Unity Camera
├── CinemachineBrain
└── CameraOutputSessionBinding
```

## Acceptance

```text
fresh composer Apply creates one Cinemachine Camera only
no Unity Camera under rig
no CinemachineBrain under rig
no AudioListener under rig
second Apply creates zero objects
Follow and LookAt use explicit PlayerComposer anchors
existing CameraOutputSessionBinding remains untouched
```

## Suggested commit

```text
Camera: separate rig materialization from output creation
```
