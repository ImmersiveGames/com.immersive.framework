# Camera — Rig Authoring Guide

Status: **canonical after C9B**  
Package: com.immersive.framework

## Create a rig

1. Create a CameraRigRecipe when reusable presentation defaults are useful.
2. Add CameraRigComposer to the rig root.
3. Assign an explicit PlayerComposer target source or explicit Follow/LookAt transforms.
4. Validate.
5. Apply/Rebuild.

Apply/Rebuild idempotently creates or repairs a Unity Camera, CinemachineBrain and CinemachineCamera, then assigns Follow and LookAt.

## Boundary

The Composer materializes a rig; it does not choose the active camera. Runtime requests, output contexts, arbitration and publishers are not implemented in C9B.

Do not configure priority, Route/Activity ownership, PlayerView activation, camera enable/disable, global lookups or fallback selection.
