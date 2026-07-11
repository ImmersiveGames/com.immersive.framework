# Immersive Framework Documentation

Current camera architecture is defined by ADR-PROD-0006.

Current authoring:

```text
CameraRigRecipe
-> CameraRigComposer
-> Validate
-> Apply/Rebuild Cinemachine rig
```

Camera rig authoring resolves explicit targets and materializes a rig. Runtime request publication, output context and winner selection are pending later C9 cuts.
