# Camera Usage

Status: superseded redirect.

The current Camera authoring workflow is documented in
[Player Gameplay Camera Authoring](../Current/Guides/Player-Gameplay-Camera-Authoring.md).

`CameraRigRecipe` was removed. `CameraRigComposer` is the concrete authority
for targets, requirements, framing and local Cinemachine materialization. Use a
Unity Preset only when reusable Composer values are useful.

Apply / Rebuild creates or repairs the local Cinemachine rig; it never creates
a Unity Camera, Cinemachine Brain, AudioListener or Camera Output. Bindings
publish requests and `CameraOutputContext` arbitrates them.
