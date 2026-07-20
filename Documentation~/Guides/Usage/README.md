# Immersive Framework - User Guide

The current camera authoring flow is:

```text
CameraRigRecipe
-> CameraRigComposer
-> Validate
-> Apply/Rebuild
```

This creates or repairs a Cinemachine rig from explicit targets. Runtime winner
selection is a separate closed C9 surface:

```text
persistent UIGlobal output
-> CameraOutputSession
-> CameraOutputContext
-> Player / Activity / Route / Session requests
-> selected Cinemachine rig
```

H2.4 does not add a user-facing authoring workflow. It removes static runtime
host discovery from package code; required runtime capabilities are supplied by
explicit bindings. For the current delivery and validation state, read
`../../Current/05-Execution-Status.md`.
