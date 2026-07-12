# Camera — Canonical Authoring and Runtime Flow

Status: **canonical after C9R closure**

## Authoring

```text
CameraRigRecipe
-> CameraRigComposer
-> Validate / Apply / Rebuild
-> CinemachineCamera
-> CinemachineFollow and authored Follow Offset
```

The Composer owns virtual-rig intent and materialization only.

## Persistent output

```text
UIGlobal
-> Unity Camera
-> CinemachineBrain
-> CameraOutputSessionBinding
-> CameraOutputContext
-> CameraOutputRigApplicator
-> CameraOutputSession
```

The physical single-player output is session-owned and persists across Route
changes.

## Consumers

```text
LocalPlayerCameraRequestBinding
ActivityCameraOverrideBinding
RouteCameraOverrideBinding
SessionCameraOverrideBinding
        |
        v
typed publisher
-> CameraOutputSession
-> CameraOutputContext winner
-> selected materialized CinemachineCamera
```

Framework Core injects the `UIGlobal` output into loaded Route, Activity and
Player consumers. Cross-scene output references are not authored.

## Availability versus request

```text
Player eligible
-> publishes normal gameplay request

Activity lifecycle enter
-> override becomes available
-> does not publish

Route lifecycle enter
-> override becomes available
-> does not publish

RequestOverride()
-> publishes temporary override

ReleaseOverride()
-> removes temporary override
-> restores next valid request
```

## Transition Session authority

```text
transition surface covers source
-> Session override requested at 300
-> Route change executes while covered
-> Session override released
-> destination content is revealed
-> next valid request resumes
```

Default precedence:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

`CameraOutputContext` is the only winner-selection authority. Owners do not
toggle the physical Camera or compete by independently editing Cinemachine
priority.

Use explicit targets, output ids, scope ids, request ids and tie-breakers. There
is no `Camera.main` fallback, object-name lookup, global camera manager,
singleton or service locator.
