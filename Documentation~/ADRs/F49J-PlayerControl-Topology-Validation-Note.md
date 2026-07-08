# F49J — PlayerControl Topology Validation

## Objective

Add passive validation between `PlayerTopologyValidationResult` and authored `PlayerControlSnapshot` evidence.

## Scope

This cut validates that player control declarations are coherent with the passive player topology:

- controls point to declared `PlayerSlotId` values;
- controls have matching `PlayerEntry` evidence in the topology;
- a participating slot has at most one `PlayerControl`;
- `Bound` controls require the topology entry to be `Active`;
- `Active` controls require the topology entry to be `Active` and ready for control;
- released controls do not participate;
- `PlayerTopology` issues are propagated.

## Out of scope

- `PlayerInputManager` integration;
- Input Action routing;
- action map switching;
- movement enablement;
- runtime `ControlBinding` lifecycle;
- camera activation;
- FIRSTGAME integration.

## Architectural gain

`PlayerControl` can now be validated against the same passive topology used by `PlayerEntry`, `PlayerTopology`, `PlayerView`, and `PlayerViewTopology` before real control binding is introduced.
