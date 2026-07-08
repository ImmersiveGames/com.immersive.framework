# F49F — PlayerTopology Passive Validation Note

Status: Implemented
Date: 2026-07-07

## Objective

Add passive validation for the Player topology chain without introducing runtime coordination.

The cut validates coherence between:

- `PlayerSlotSet`
- `PlayerSlotDescriptor`
- `PlayerSlotOccupancyDescriptor`
- `PlayerEntrySnapshot`

## Scope

Created:

- `Runtime/PlayerTopology/PlayerTopologyIssueKind.cs`
- `Runtime/PlayerTopology/PlayerTopologyIssue.cs`
- `Runtime/PlayerTopology/PlayerTopologyValidationResult.cs`
- `Runtime/PlayerTopology/PlayerTopologyValidator.cs`

## Out of scope

This cut does not create:

- scene discovery;
- runtime lifecycle;
- player join;
- spawn/materialization;
- PlayerView;
- ControlBinding;
- PlayerInputManager bridge;
- movement;
- FIRSTGAME integration.

## Expected smoke

QA should validate:

- a coherent authored topology succeeds;
- duplicate PlayerEntry slot fails;
- duplicate Actor occupation fails;
- PlayerEntry/occupancy Actor mismatch fails;
- missing occupancy fails;
- orphan occupancy fails;
- PlayerSlotSet blocking issues propagate.

Expected final log:

```text
[F49F_PLAYER_TOPOLOGY_QA] status='Succeeded'
```

## Acceptance criteria

- Unity compiles.
- QA Hub opens the PlayerTopology QA scene.
- Final smoke status is `Succeeded`.
- The framework still does not own input behavior, actor spawning, view binding or control binding.

## Architectural gain

The framework can now validate whether authored PlayerSlot, occupancy and PlayerEntry evidence form a coherent passive topology before future PlayerView or ControlBinding cuts consume it.

## Suggested commit message

```text
F49F: add passive PlayerTopology validation
```
