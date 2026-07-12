# Camera — Product Usage Guide

Status: **canonical after C9N**
Package: `com.immersive.framework`

## Author a reusable rig

1. Create a `CameraRigRecipe` when presentation intent is reusable.
2. Add `CameraRigComposer` to a rig root.
3. Assign `PlayerComposer` or explicit Follow/LookAt transforms.
4. Validate, then Apply/Rebuild.

Apply/Rebuild idempotently creates or repairs only the rig's
`CinemachineCamera` and targets. It does not create a Unity Camera,
`CinemachineBrain`, `AudioListener`, output binding or runtime authority.

## Author one output

Create one physical output GameObject with:

```text
UnityEngine.Camera
CinemachineBrain
CameraOutputSessionBinding
```

Assign an explicit output id. Do not duplicate a binding for the same output.

## Publish presentation intent

- Put `RouteCameraOverrideBinding` on canonical Route content.
- Put `ActivityCameraOverrideBinding` on canonical Activity content.
- Put `LocalPlayerCameraRequestBinding` on the local Player (or explicit
  Player-owned object), and call `SetLocalPlayerEligible` from real eligibility
  authority when `eligibleOnEnable` is not appropriate.

Player eligibility publishes its normal gameplay request. Route and Activity only
become available on lifecycle entry: call `RequestOverride()` to publish and
`ReleaseOverride()` to restore the next valid request. They never publish merely
because a Route or Activity entered.

The main output belongs to `UIGlobal`. Route, Activity and Player consumers are
explicitly injected with that session-scoped output by Framework Core; they do
not serialize cross-scene references or perform global lookup. A
`SessionCameraOverrideBinding` stays in `UIGlobal` and is requested after the
transition curtain closes, then released before it opens. Higher precedence wins;
release restores the next request.

## Boundary

The bindings publish/release, `CameraOutputContext` arbitrates, and the
applicator enables the selected `CinemachineCamera`. No owner selects a winner,
toggles the physical Unity Camera, uses `Camera.main`, or relies on global
lookup. For evidence limits, read `../Current/Camera-Delivery-Reconciliation.md`.
