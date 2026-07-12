# C9P — Cinemachine Follow Pipeline Correction

Status: implementation delivered; QA and FIRSTGAME rerun pending.

## Objective

Make the existing `CameraRigPresentationIntent.Follow` materially follow its
resolved target instead of only assigning `CinemachineCamera.Follow`.

## Product correction

`CinemachineRigMaterializer` now requires one `CinemachineFollow` position
pipeline whenever a Follow target participates. Apply/Rebuild:

```text
creates CinemachineFollow when missing
preserves the existing component when valid
creates no duplicate on the second Apply/Rebuild
blocks when Follow participates but component creation is disabled
```

The component is local to the materialized virtual Camera. No Unity Camera,
Brain, AudioListener, singleton, lookup or runtime authority is added.

## QA gate

Run:

```text
Immersive Framework QA/Camera/C9M Run Follow Pipeline Smoke
```

Expected:

```text
[QA][C9M Follow Pipeline] PASS. status='Passed' cases='4'
```

## FIRSTGAME gate

After QA PASS, run Apply/Rebuild on Route Rig, Player Rig and Activity Override
Rig in `FG_Gameplay`. The Player virtual Camera must contain exactly one
`CinemachineFollow`, then the manual C9M sequence must be repeated visually.
