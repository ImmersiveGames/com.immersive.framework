# C9E — Camera output rig application

Status: implementation ready for Unity compile and QA validation  
Type: technical runtime application  
Package: `com.immersive.framework`

## Objective

Apply the winner selected by `CameraOutputContext` to one explicit Cinemachine output.

```text
CameraOutputContext winner
-> CameraOutputRigApplicator
-> materialized CinemachineCamera enabled
```

## Scope

```text
explicit CameraOutputBinding
Unity Camera + CinemachineBrain validation
materialized CameraRigComposer winner
disable previous CinemachineCamera
enable current CinemachineCamera
clear output when context has no winner
idempotent preserve when winner is unchanged
explicit blocked results
```

## Out of scope

```text
request publication
runtime Recipe materialization
target rebinding
Cinemachine priority policy
custom blends
multiple-output registry
automatic Apply after every context mutation
lifetime observation
```

## Important boundary

`CameraOutputContext` remains the only winner-selection authority.

The applicator does not compare precedence, inspect owners or choose a request. It consumes only `context.Winner`.

## Materialized-rig requirement

A recipe-only request remains valid for arbitration, but cannot be presented by C9E.

Application requires:

```text
winner.Rig.Composer != null
winner.Rig.Composer.CinemachineCamera != null
```

Otherwise application blocks explicitly.

## Presentation mechanism

C9E toggles `CinemachineCamera.enabled`.

It does not toggle `UnityEngine.Camera.enabled`, mutate owner-authored priorities or implement blending.

## Expected QA

```text
valid output binding
first winner applied
higher winner replaces previous rig
release restores previous rig
unchanged winner preserved
clear disables current rig
recipe-only winner blocked
missing CinemachineCamera blocked
foreign context blocked
Unity Camera remains enabled and unchanged
```

## Suggested commit

```text
Camera: apply output winner to materialized Cinemachine rig
```
