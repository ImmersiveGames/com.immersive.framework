# Camera — Canonical Rig Authoring Flow

Status: **canonical after C9B**

```text
PlayerComposer or explicit transforms
-> CameraRigComposer resolves Follow / LookAt
-> CinemachineRigMaterializer
-> Unity Camera + CinemachineBrain + CinemachineCamera
```

This is authoring and materialization only. No runtime camera authority exists in C9B. Future cuts introduce CameraRequest and CameraOutputContext; neither is present yet.
