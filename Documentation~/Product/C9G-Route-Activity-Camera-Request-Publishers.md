# C9G — Route and Activity camera request publishers

Status: implementation ready for Unity compile and QA validation  
Type: scoped runtime publishing  
Package: `com.immersive.framework`

## Objective

Introduce typed publishers that connect Route and Activity lifecycle boundaries to one explicit `CameraOutputSession`.

```text
Route enter
-> RouteCameraRequestPublisher.Publish()

Route exit
-> RouteCameraRequestPublisher.Release()
```

```text
Activity enter
-> ActivityCameraRequestPublisher.Publish()

Activity exit
-> ActivityCameraRequestPublisher.Release()
```

## Authority boundaries

```text
Publisher
  owns publish/release idempotency

CameraOutputSession
  coordinates mutation and application

CameraOutputContext
  selects the winner

CameraOutputRigApplicator
  applies Cinemachine presentation
```

Publishers do not select winners and do not mutate Cinemachine directly.

## Creation validation

Route publisher requires:

```text
Owner.Kind = Route
Lifetime.Kind = Route
Request.OutputId = Session.OutputId
```

Activity publisher requires:

```text
Owner.Kind = Activity
Lifetime.Kind = Activity
Request.OutputId = Session.OutputId
```

Invalid configuration blocks explicitly.

## Idempotency

```text
Publish twice
-> second result = Preserved

Release twice
-> second result = Preserved
```

## Out of scope

```text
MonoBehaviour lifecycle adapters
automatic discovery of active Route or Activity
global output/session lookup
Player publisher
request authoring asset
runtime Recipe materialization
```

This cut establishes the typed runtime publishers. Lifecycle adapters may call them in the next integration cut.

## Expected QA

```text
Route publish applies Route rig
Activity publish overrides Route rig
Activity release restores Route rig
Route release clears output
duplicate Publish is preserved
duplicate Release is preserved
wrong owner kind blocks creation
wrong lifetime kind blocks creation
foreign output blocks creation
failed session operation does not flip publisher state
```

## Suggested commit

```text
Camera: add scoped Route and Activity request publishers
```
