# F15 - Unity Object Reset Adapters Usage

Status: historical. The old F15 authoring path was superseded by preview.12 reset architecture.

## Current canonical shape

Use `UnityResetSubjectAdapter` plus concrete `UnityResetParticipantBehaviour` components:

```text
ResettableObject
  UnityResetSubjectAdapter
    Subject Id = firstgame.resettable.object
    Scope = Route / Activity / Runtime
  UnityTransformResetParticipant
```

The reset target is a `ResetSubject`, not an `ObjectEntryDeclaration`.

## Transform reset

`UnityTransformResetParticipant` restores authored local state:

```text
localPosition
localRotation
localScale
```

If baseline is not configured:

```text
Required participant -> ResetExecutor fails with blocking issue
Optional participant -> ResetExecutor completes with warnings
```

## Trigger usage

A UI Button can call:

```text
ObjectResetTrigger.RequestObjectReset()
```

Recommended authoring shape:

```text
Object Reset Button or control object
  ObjectResetTrigger
    Target Subject = ResettableObject/UnityResetSubjectAdapter
```

## Current limitations

Still outside this guide:

```text
Rigidbody reset
Animator reset
Player/Actor reset
Pool return
Save/checkpoint restore
Scene reload
```

## Validation smokes

Run the current reset smokes from `FrameworkQaCanvas`:

```text
Run Reset Registry Synthetic Smoke
Run Unity Reset Subject Adapter Synthetic Smoke
Run Reset Executor Synthetic Smoke
Run Object Reset Trigger Rewrite Synthetic Smoke
Run Runtime Prefab Reset Synthetic Smoke
```
