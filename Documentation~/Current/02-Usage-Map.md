# 02 — Usage Map

## Camera rig and output authoring

| Need | Current surface |
|---|---|
| Reusable presentation intent | `CameraRigRecipe` |
| Materialize a virtual rig | `CameraRigComposer` → Validate → Apply/Rebuild |
| Physical output | `CameraOutputSessionBinding` with explicit Unity Camera and `CinemachineBrain` |
| Route presentation | `RouteCameraRequestBinding` |
| Activity override | `ActivityCameraRequestBinding` |
| Local Player presentation | `LocalPlayerCameraRequestBinding` plus explicit eligibility |
| Inspect arbitration | binding diagnostics and `CameraOutputContext` snapshot |

`CameraRigComposer` materializes only a `CinemachineCamera` and explicit
targets. It does not create an output and never chooses the active rig.

```text
Route / Activity / Local Player binding
-> typed publisher
-> CameraOutputSession
-> CameraOutputContext
-> CameraOutputRigApplicator
-> materialized CinemachineCamera
```

Do not use Unity Camera enable/disable, Cinemachine priority competition,
object-name lookup, `Camera.main` or a global manager as selection policy. See
`00-Current-State.md` and `Camera-Delivery-Reconciliation.md`.
