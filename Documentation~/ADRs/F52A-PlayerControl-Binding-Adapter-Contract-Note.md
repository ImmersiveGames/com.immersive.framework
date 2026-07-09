# F52A — PlayerControl Binding Adapter Contract

Status: Accepted / package-first / QA-first / replanned from F51D safe point


## Roadmap alignment

F52A implements only the `PlayerControl Binding Adapter` step from the canonical roadmap.
The next implementation step is the optional Unity `PlayerInput` bridge. Movement, gameplay command execution and actor spawning are explicitly outside this lane.

## Objective

Introduce the first explicit PlayerControl binding adapter after the PlayerView camera chain is closed.

The adapter converts validated `PlayerControl` readiness evidence into explicit binding evidence stored on an `IPlayerControlBindingTarget`.

```text
PlayerBindingReadinessSummary
  -> PlayerControlBindingAdapter
  -> IPlayerControlBindingTarget
  -> PlayerControlBindingSnapshot
```

## Scope

F52A adds:

- `PlayerControlBindingStatus`
- `PlayerControlBindingFailureKind`
- `PlayerControlBindingSnapshot`
- `PlayerControlBindingResult`
- `IPlayerControlBindingTarget`
- `PlayerControlBindingTargetBehaviour`
- `PlayerControlBindingAdapter`

The adapter supports:

- bind first active ready `PlayerControl`
- bind explicit `PlayerControlSnapshot`
- clear existing binding
- explicit failure/no-op results
- passive boundary diagnostics

## Out of scope

F52A does not implement:

- Unity Input System binding
- `PlayerInput` action-map switching
- `InputAction` routing
- input activation
- movement enable/disable
- gameplay control execution
- camera activation
- actor spawning
- runtime lifecycle/coordinator
- FIRSTGAME integration

## Acceptance evidence

The QA smoke must prove:

```text
[F52A_PLAYERCONTROL_BINDING_ADAPTER_QA] status='Succeeded'
```

Expected success boundary:

```text
viewBinding='False'
controlBinding='True'
cameraActivation='False'
inputActivation='False'
movement='False'
actorSpawning='False'
```

## Architectural gain

F52A starts the control side with the same shape used by F51A:

```text
validated readiness
  -> explicit adapter
  -> explicit target
  -> immutable snapshot
```

This keeps control binding separate from input activation, Unity PlayerInput bridge policy and movement/gameplay execution.
