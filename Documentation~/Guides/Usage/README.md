# Immersive Framework — User Guide

The current camera flow is:

```text
CameraRigRecipe
-> CameraRigComposer
-> Validate
-> Apply/Rebuild
```

This creates or repairs a Cinemachine rig from explicit targets. It does not select a runtime winner. Route, Activity and Player request publishers, output contexts and arbitration are pending later C9 cuts.
