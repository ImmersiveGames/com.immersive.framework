# 00 — Current State

Status: **canonical after C9B destructive removal**

ADR-PROD-0006 defines the active camera model. C9B deliberately leaves runtime camera authority absent.

```text
CameraTargetSource -> Follow / LookAt evidence
CameraRigRecipe -> reusable presentation intent
CameraRigComposer -> target resolution and local rig materialization
CinemachineRigMaterializer -> idempotent Camera, Brain and CinemachineCamera setup
```

CameraRigComposer does not select a winning camera, own an output, mutate priority or control runtime lifetime. Route, Activity and Player do not yet publish CameraRequest; CameraOutputContext does not yet exist.
