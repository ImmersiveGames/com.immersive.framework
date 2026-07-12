# C9Q — Camera Follow Framing Authoring

Status: implementation delivered; QA and FIRSTGAME visual rerun pending.

## Objective

Make Follow framing designer-authored and idempotent instead of leaving every
materialized `CinemachineFollow` at the same package default.

## Product surface

`CameraRigRecipe` and `CameraRigComposer` now expose `Follow Offset` as
designer intent. Apply/Rebuild writes the value to the local
`CinemachineFollow`, reports created/repaired/already-valid state, and does not
touch the physical Camera output.

## QA

The C9M Follow Pipeline smoke now proves:

```text
virtual Camera materialization
CinemachineFollow materialization
target assignment
Follow Offset application
second Apply/Rebuild creates zero objects
```

## FIRSTGAME

The consumer scene uses deliberately distinct framing:

```text
Route    (0, 12, -14)
Player   (0, 5, -8)
Activity (8, 10, -8)
```
