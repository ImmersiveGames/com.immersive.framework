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

## Persistent Camera Output Inspector

`CameraOutputSessionBinding` exposes Unity Camera and Cinemachine Brain as the
primary authoring fields. A new component receives a stable Output ID without
replacing IDs already authored. `Advanced / Diagnostics` contains the stable
identity, initialization and logging settings, read-only runtime evidence and
the last explicit validation report.

`Validate Configuration` checks the Output ID, both explicit component
references and the requirement that Unity Camera and Cinemachine Brain share
the same GameObject. Validation never creates, discovers or repairs components.
