# F51A — PlayerView Binding Adapter Contract

Status: Accepted / package + QA smoke pending

## Objective

Introduce the first explicit PlayerView binding adapter contract after the passive F49/F50 foundation.

The cut allows a validated active `PlayerViewSnapshot` to be applied to an explicit `IPlayerViewBindingTarget` and later cleared. The operation stores binding evidence only.

## Scope

- `PlayerViewBindingAdapter` explicit `Bind`, `BindFirstReadyView` and `Clear` operations.
- `IPlayerViewBindingTarget` contract.
- `PlayerViewBindingTargetBehaviour` Unity target that stores current binding evidence.
- Result/snapshot/status/failure diagnostics.
- QA smoke proving success, failures, no-op clear and passive boundary preservation.

## Out of scope

- Cinemachine.
- Camera activation or priority changes.
- CameraDirector integration.
- Input activation or action routing.
- Control binding.
- Movement enable/disable.
- Actor spawning.
- Runtime lifecycle/coordinator.
- FIRSTGAME integration.

## Decision

F51A starts real binding through a deliberately small adapter contract. Binding means:

```text
validated active PlayerView evidence
-> explicit binding target
-> stored binding snapshot
```

Binding does not mean camera activation yet. Camera activation remains a later adapter cut.

## Acceptance

The QA smoke must pass with:

```text
[F51A_PLAYERVIEW_BINDING_ADAPTER_QA] status='Succeeded'
```

The successful binding result may report `viewBinding='True'`, but it must keep:

```text
controlBinding='False'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Architectural gain

F51A creates the first safe handoff point between passive PlayerView readiness and future camera/view presentation adapters without introducing a global manager or hidden lifecycle.
