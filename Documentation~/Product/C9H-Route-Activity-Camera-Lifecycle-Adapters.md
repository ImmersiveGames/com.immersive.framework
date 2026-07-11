# C9H — Route and Activity camera lifecycle adapters

Status: implementation ready for Unity compile and QA validation  
Type: scoped runtime lifecycle integration boundary  
Package: `com.immersive.framework`

## Objective

Provide the official typed boundary between Route/Activity lifecycle transitions and the C9G request publishers.

```text
Route lifecycle enters route-id
-> RouteCameraLifecycleAdapter.Enter(route-id)
-> RouteCameraRequestPublisher.Publish()
```

```text
Activity lifecycle exits activity-id
-> ActivityCameraLifecycleAdapter.Exit(activity-id)
-> ActivityCameraRequestPublisher.Release()
```

## Identity rule

The request must carry one coherent lifecycle identity:

```text
Owner.LogicalOwnerId == Lifetime.ScopeId
```

The lifecycle callback must provide the same scope id.

Foreign or missing ids block explicitly.

## State rule

```text
Enter twice
-> second result = Preserved

Exit twice
-> second result = Preserved

failed Publish
-> adapter remains exited

failed Release
-> adapter remains entered
```

## Authority boundaries

```text
Lifecycle pipeline
  decides when Enter/Exit occurs

Lifecycle adapter
  validates scope identity and forwards the operation

Publisher
  owns publish/release state

CameraOutputSession
  coordinates arbitration and presentation
```

The adapter does not discover the active Route or Activity and does not mutate Cinemachine.

## Current integration boundary

This cut does not patch `FrameworkRuntimeHost` or another pipeline host without the current source shape being part of the implementation input.

The runtime pipeline should call:

```csharp
routeAdapter.Enter(routeId);
routeAdapter.Exit(routeId);

activityAdapter.Enter(activityId);
activityAdapter.Exit(activityId);
```

## Out of scope

```text
global session lookup
automatic scene hierarchy discovery
Player lifecycle
MonoBehaviour UnityEvent bridges
runtime Recipe materialization
```

## Expected QA

```text
Route Enter publishes and applies Route rig
Activity Enter overrides Route
Activity Exit restores Route
Route Exit clears output
duplicate Enter/Exit preserved
missing scope blocked
foreign scope blocked
owner/lifetime identity mismatch blocks creation
failed publisher operation preserves adapter state
```

## Suggested commit

```text
Camera: add typed Route and Activity lifecycle adapters
```
