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

## C9H status

The pure C9H lifecycle adapters remain temporarily available because the existing C9H QA scripts reference them.

They are no longer required by the scene-authored integration path and should be removed together with C9H QA after C9I passes.

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
