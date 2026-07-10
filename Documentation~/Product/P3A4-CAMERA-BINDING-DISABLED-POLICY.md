# P3A.4 — Camera Binding Disabled Policy

## Correction

`PlayerComposerApplyRebuildUtility` previously called camera-anchor materialization even when `Camera Binding Required` was disabled.

The corrected policy is:

### Camera Binding enabled

- create or reuse `Anchors/CameraTarget`;
- create or reuse `Anchors/LookAtTarget` when explicit LookAt is selected;
- assign generated references to `PlayerComposer`;
- fail explicitly if required anchor materialization does not produce valid references.

### Camera Binding disabled

- do not create `Anchors`;
- do not create `CameraTarget`;
- do not create `LookAtTarget`;
- do not rewrite camera references;
- preserve already existing authored/generated anchors non-destructively;
- report `camera-anchors:camera-binding-disabled`.

This preserves editor safety while ensuring a new Player without camera integration does not receive unused camera objects.

## Expected QA

Fresh Player, camera binding enabled:

```text
Anchors/
  CameraTarget
  LookAtTarget
```

Fresh Player, camera binding disabled:

```text
no Anchors container created by PlayerComposer
```

Existing Player that already has anchors, then camera binding disabled:

```text
anchors remain untouched
no new anchors are created
```
