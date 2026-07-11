# 02 — Usage Map

## Camera rig authoring

| Need | Current surface |
|---|---|
| Reusable rig presentation intent | CameraRigRecipe |
| Materialize a local Cinemachine rig | CameraRigComposer -> Validate -> Apply/Rebuild |
| Follow an explicit Player target | assign PlayerComposer as target source |
| Use non-Player targets | explicit Follow and LookAt transforms |

The Composer only resolves targets and materializes a rig. There is no current Route, Activity or Player runtime camera publisher, no CameraRequest, and no CameraOutputContext.

Do not use camera enable/disable, priority competition, object-name lookup, Camera.main or a global manager as selection policy.
