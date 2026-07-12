# Camera — Canonical Authoring and Runtime Flow

Status: **canonical after C9N**

```text
CameraRigRecipe
-> CameraRigComposer
-> Validate / Apply / Rebuild
-> virtual CinemachineCamera

explicit Unity Camera + CinemachineBrain
-> CameraOutputSessionBinding
-> CameraOutputContext + CameraOutputRigApplicator + CameraOutputSession

Route / Activity / Local Player request binding
-> typed publisher
-> CameraOutputSession
-> selected virtual CinemachineCamera
```

`CameraRigComposer` owns virtual-rig materialization only. The separate
`CameraOutputSessionBinding` owns one physical output. `CameraOutputContext`
is the single winner-selection authority; the applicator only presents its
winner through Cinemachine.

Use explicit targets, output ids, scopes, request ids and tie-breakers. There
is no `Camera.main` fallback, global manager or owner-controlled priority
competition.
