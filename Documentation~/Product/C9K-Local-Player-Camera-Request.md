# C9K — Local Player Camera Request Publisher and Binding

Status: implementation ready for Unity compile and QA  
Type: runtime integration / product authoring  
Package: `com.immersive.framework`  
ADR: `ADR-PROD-0006-camera-requests-output-contexts.md`

## Objective

Complete the missing Player request path:

```text
PlayerComposer
-> LocalPlayerCameraRequestBinding
-> LocalPlayerCameraRequestPublisher
-> CameraOutputSession
```

## Product surface

`LocalPlayerCameraRequestBinding` is attached to the concrete local Player or an explicit Player-owned technical object.

The designer configures:

```text
PlayerComposer
CameraOutputSessionBinding
CameraRigComposer
eligibility scope id
request id
precedence
tie-breaker id
eligible-on-enable policy
```

## Runtime authority

```text
PlayerComposer
  supplies stable Actor/Slot identity and Follow/LookAt targets

LocalPlayerCameraRequestBinding
  translates explicit local eligibility into publish/release

LocalPlayerCameraRequestPublisher
  owns idempotent publish/release state

CameraOutputSession
  coordinates request admission, winner application and release
```

No component in this cut discovers the local Player or selects a winner.

## Eligibility API

```csharp
binding.SetLocalPlayerEligible(true);
binding.SetLocalPlayerEligible(false);
```

Repeated calls are idempotent.

`eligibleOnEnable` is an explicit single-player convenience policy. Projects with a real local-player eligibility runtime should disable it and call the typed API.

## Request shape

```text
Owner.Kind = LocalPlayer
Owner.Id = PlayerComposer.PlayerSlotId

Lifetime.Kind = LocalPlayerEligibility
Lifetime.ScopeId = explicit eligibilityScopeId

Target source = PlayerComposer.CameraTarget
LookAt requirement = PlayerComposer.LookAtTarget must exist

Release condition = ExplicitRelease
```

No identity is derived from GameObject names.

## Blocking configuration

```text
missing PlayerComposer
Player camera binding disabled
missing ActorId
missing PlayerSlotId
missing CameraTarget
missing LookAtTarget
missing eligibility scope
missing request id
missing tie-breaker id
missing output session
missing rig composer
```

All failures are explicit and diagnostic.

## Out of scope

```text
Player spawning
Player join/leave authority
automatic local-player discovery
split-screen output creation
online local/remote classification
Activity override QA
FIRSTGAME integration
```

## Expected QA

```text
Route request published
Player eligibility publishes Player request
Player wins over Route
Activity publishes and wins over Player
Activity release restores Player
Player eligibility false releases Player
Route is restored
duplicate eligibility calls are preserved
invalid Player binding blocks explicitly
```

## Suggested commit

```text
Camera: add Local Player camera request publisher and binding
```
