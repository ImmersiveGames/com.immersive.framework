# F49C — Actor Readiness Unity Adapter Note

Status: Implemented / Pending QA smoke  
Phase: F49 — Player Topology, Player Entry and PlayerView Ownership

## Objective

Expose the F49B pure `ActorReadiness` contract to authored Unity GameObjects without creating PlayerEntry, PlayerView, ControlBinding, PlayerInputManager integration or gameplay movement.

## Scope

F49C adds:

```text
Runtime/Actors/ActorReadinessBehaviour.cs
```

The component:

- implements `IActorReadiness`;
- owns one pure `ActorReadiness` instance;
- exposes explicit readiness methods for authored/runtime use;
- supports an optional configured initial state on `Awake`;
- keeps readiness lifecycle explicit and diagnostic.

## Out of scope

F49C does not add:

- PlayerEntry coordinator;
- PlayerView ownership;
- ControlBinding;
- PlayerInputManager bridge;
- character selection;
- movement;
- FIRSTGAME integration.

## Acceptance

The QA Harness must prove:

```text
ActorReadinessBehaviour exists on a GameObject.
ActorReadinessBehaviour implements IActorReadiness.
ReadyForView and ReadyForControl snapshots match the pure contract.
Invalid ReadyForControl without ReadyForView fails explicitly.
Release blocks readiness changes until BeginNewCycle.
Failed requires an explicit reason.
```

## Architectural gain

F49C allows scene-authored actors, prefabs and QA fixtures to expose readiness evidence while keeping the real readiness model pure and testable.
