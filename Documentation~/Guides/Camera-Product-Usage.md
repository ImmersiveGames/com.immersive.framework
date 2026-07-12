# Camera — Product Usage Guide

Status: **canonical after C9R closure**  
Package: `com.immersive.framework`

## 1. Author a reusable rig

1. Create a `CameraRigRecipe` when presentation intent is reusable.
2. Add `CameraRigComposer` to a rig root.
3. Assign a `PlayerComposer` or explicit Follow/LookAt transforms.
4. Configure `Follow Offset`.
5. Validate.
6. Run Apply/Rebuild.

Apply/Rebuild idempotently creates or repairs the rig's `CinemachineCamera`,
`CinemachineFollow`, targets and Follow framing.

It does not create a physical Unity Camera, `CinemachineBrain`, output binding
or runtime winner-selection authority.

## 2. Author the persistent single-player output

In the canonical `UIGlobal` scene, create one output root with:

```text
UnityEngine.Camera
CinemachineBrain
CameraOutputSessionBinding
SessionCameraOverrideBinding
```

Assign one explicit output id, normally:

```text
camera.output.main
```

The Session override references the Game Application and the persistent output.
Do not duplicate a binding for the same output.

## 3. Author the normal Player camera

Place `LocalPlayerCameraRequestBinding` on the local Player or an explicit
Player-owned object.

The Player request publishes when eligibility is active. Use
`SetLocalPlayerEligible(bool)` when locality or control eligibility is owned by
another runtime authority.

Default Player precedence:

```text
50
```

## 4. Author temporary Activity and Route overrides

Use:

```text
ActivityCameraOverrideBinding
RouteCameraOverrideBinding
```

Lifecycle entry validates the owner and makes the override available. It does
not publish automatically.

Call:

```text
RequestOverride()
ReleaseOverride()
```

Default precedence:

```text
Activity 100
Route 200
```

Release restores the next valid request.

## 5. Session transition camera

`SessionCameraOverrideBinding` lives with the persistent output in `UIGlobal`.

Framework Core wraps the transition orchestrator so that Session:

```text
requests after the transition cover is established
runs at precedence 300
releases before destination content is revealed
```

The Session camera is not the normal gameplay winner.

## 6. Output injection

Route, Activity and Player consumers receive the persistent output through
Framework Core injection.

Do not serialize a reference from a gameplay scene to the `UIGlobal` output.
Do not use name lookup, `Camera.main`, singleton or service locator.

## 7. Inspect and diagnose

Use the binding Inspector Advanced/Debug sections and
`CameraOutputContext` snapshot to inspect:

```text
output
registered requests
winner
owner
scope
precedence
targets
rig
last status
last diagnostic
```

Current default precedence:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

## 8. Proven consumer behavior

FIRSTGAME proves:

- persistent output initialization;
- Session availability from `GameApplicationAsset`;
- transition-scoped Session request/release;
- runtime injection into Route, Activity and Player;
- explicit Activity, Route and Session override/release;
- restoration to Player;
- visually distinct Follow framing.

## Boundary

Bindings publish and release intent. `CameraOutputContext` arbitrates.
Cinemachine presents the selected rig.

No owner directly selects a winner, toggles the physical Unity Camera, uses
`Camera.main`, relies on global lookup or becomes a camera manager.
