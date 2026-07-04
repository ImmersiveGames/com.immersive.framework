# F16 - GameObject Active Reset Usage

Status: historical. The old F16 participant/source path was superseded by preview.12 reset architecture.

## Current canonical shape

`UnityGameObjectActiveResetParticipant` restores only:

```text
GameObject.activeSelf
```

Use it under a `UnityResetSubjectAdapter`:

```text
ResettableObject
  UnityResetSubjectAdapter
    Subject Id = firstgame.resettable.object
    Scope = Route / Activity / Runtime
  UnityGameObjectActiveResetParticipant
```

It does not reset `activeInHierarchy`, children, components, physics, animation or gameplay state.

## Basic setup

On the object that should restore active state:

```text
1. Add UnityResetSubjectAdapter.
2. Add UnityGameObjectActiveResetParticipant.
3. Choose Required or Optional.
4. Capture Current Active State Baseline, or set Baseline Active Self manually.
5. Request reset through ObjectResetTrigger, ObjectResetGroupTrigger or ActivityRestartTrigger.
```

## Required vs Optional

```text
Required without baseline -> blocks reset.
Optional without baseline -> reset completes with warnings.
```

## What F16 is not

F16 is not:

```text
Player reset
Actor reset
NPC reset
Timer reset
Door gameplay reset
Pickup reset
Pooling
Save/checkpoint restore
```

Use this adapter only as a primitive piece when `activeSelf` itself is the correct local state to restore.

## Smoke

Use:

```text
Run Reset Executor Synthetic Smoke
Run Runtime Prefab Reset Synthetic Smoke
```
