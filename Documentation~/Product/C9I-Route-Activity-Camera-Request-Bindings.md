# C9I — Route and Activity Camera Request Bindings

Status: implementation ready for Unity compile and QA validation  
Type: real lifecycle integration and scene-authored product surface  
Package: `com.immersive.framework`

## Objective

Connect the C9 camera request runtime to the canonical Route and Activity lifecycle callbacks already present in the framework.

## Product surface

```text
CameraOutputSessionBinding
RouteCameraRequestBinding
ActivityCameraRequestBinding
```

### CameraOutputSessionBinding

Scene-authored owner of one output session:

```text
explicit output id
explicit Unity Camera
explicit CinemachineBrain
one CameraOutputContext
one CameraOutputRigApplicator
one CameraOutputSession
```

No global registry or lookup is introduced.

### RouteCameraRequestBinding

Inherits:

```text
RouteContentBehaviour
```

It receives:

```text
OnRouteContentEntered
OnRouteContentExited
```

from the canonical Route lifecycle.

### ActivityCameraRequestBinding

Inherits:

```text
ActivityContentBehaviour
```

It receives:

```text
OnActivityContentEntered
OnActivityContentExited
```

from the canonical Activity lifecycle.

## Identity decision

`RouteAsset` and `ActivityAsset` currently expose human-readable names but no persistent runtime IDs.

Therefore:

```text
RouteName and ActivityName are diagnostics only
scopeId is explicit and required
requestId is explicit and required
assigned RouteAsset/ActivityAsset proves lifecycle asset identity
```

No asset name, GameObject name or hierarchy path is used as fallback identity.

## Runtime flow

```text
Route lifecycle dispatch
-> RouteCameraRequestBinding
-> RouteCameraRequestPublisher
-> CameraOutputSession
-> CameraOutputContext
-> CameraOutputRigApplicator
```

```text
Activity lifecycle dispatch
-> ActivityCameraRequestBinding
-> ActivityCameraRequestPublisher
-> CameraOutputSession
-> CameraOutputContext
-> CameraOutputRigApplicator
```

## Required configuration

Route binding:

```text
assignedRoute
scopeId
requestId
outputSession
rigComposer
targetSource
tieBreakerId
```

Activity binding:

```text
assignedActivity
scopeId
requestId
outputSession
rigComposer
targetSource
tieBreakerId
```

Missing required configuration blocks explicitly.

## Canonical lifecycle integration

C9I is the official Route and Activity camera lifecycle integration.

```text
RouteContentBehaviour
-> RouteCameraRequestBinding

ActivityContentBehaviour
-> ActivityCameraRequestBinding
```

The temporary C9H pure lifecycle adapters were removed after C9I passed canonical runtime QA. They are not part of the supported product surface and must not be restored as an additional required layer.

## Out of scope

```text
persistent IDs added to RouteAsset/ActivityAsset
Player camera binding
runtime Recipe materialization
global output registry
automatic hierarchy discovery
custom inspector and Apply/Rebuild tooling
```

## Expected QA

```text
canonical Route enter publishes Route request
canonical Activity enter overrides Route
canonical Activity exit restores Route
canonical Route exit clears output
foreign Route/Activity asset blocks
missing scope/request/tie-breaker blocks
session binding rejects invalid Camera/Brain pairing
no manual lifecycle adapter calls
```

## Suggested commit

```text
Camera: bind Route and Activity requests to canonical lifecycle
```


## Validation closure

C9I canonical QA passed with eight cases:

```text
canonical-route-activity-enter
invalid-bindings-blocked
invalid-session-binding-blocked
canonical-activity-exit-restores-route
canonical-activity-reenter-overrides-route
pre-route-exit-route-only
unity-output-unchanged
canonical-route-exit-clears-output
```

The final Route exit evidence confirmed:

```text
bindingStatus = Released
bindingPublished = false
CameraOutputContext.HasWinner = false
CameraOutputRigApplicator.HasAppliedRequest = false
```
