# F49H — PlayerView Topology Passive Validation Note

## Objective

Add a passive validation layer that checks authored `PlayerView` evidence against an already validated `PlayerTopologyValidationResult`.

## Scope

This cut adds:

- `PlayerViewTopologyIssueKind`
- `PlayerViewTopologyIssue`
- `PlayerViewTopologyValidationResult`
- `PlayerViewTopologyValidator`

The validator receives a `PlayerTopologyValidationResult` and a set of `PlayerViewSnapshot` values.

## Rules

The validator checks that:

- every participating `PlayerView` points to a declared `PlayerSlot`;
- a participating `PlayerView` has a matching `PlayerEntry` in the topology;
- a `PlayerSlot` has at most one participating `PlayerView`;
- `PlayerView` PlayerEntry evidence is not stale compared to the topology result;
- `Bound` and `Active` views require topology `PlayerEntry` state `ViewBound` or `Active`;
- released `PlayerView` snapshots do not participate as active view candidates;
- blocking issues from `PlayerTopologyValidationResult` are propagated.

## Out of scope

This cut does not:

- activate cameras;
- drive Cinemachine;
- select camera priority;
- bind input;
- bind control;
- create a PlayerView coordinator;
- integrate FIRSTGAME.

## Architectural gain

This creates the passive bridge between PlayerTopology and PlayerView without introducing runtime ownership.
