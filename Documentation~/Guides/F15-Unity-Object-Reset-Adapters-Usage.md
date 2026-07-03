# F15 — Unity Object Reset Adapters Usage

Status: F15 closed/applied. This guide covers the closed Transform Reset adapter path.

## What lives where

Framework scripts live in the package:

```text
Packages/com.immersive.framework/
```

Scene or prefab configuration lives on game objects:

```text
Scene object or prefab
├─ ObjectEntryDeclaration
└─ Transform Reset Participant
```

The framework provides the component types. The game scene/prefab owns the configured component instances.

Do not copy framework scripts into an `Assets/Framework` folder as part of the canonical setup.

## Required scene shape

A resettable Transform object needs:

```text
Object that needs Transform reset
├─ ObjectEntryDeclaration
└─ Transform Reset Participant
```

A Route or Activity authoring context also needs an explicit participant source:

```text
Framework Runtime / Route Runtime / Activity Runtime object
└─ Object Reset Unity Participant Source
   └─ Participants:
      └─ Object that needs Transform reset / Transform Reset Participant
```

The source is only a registrar. It does not reset objects itself.

## Identity rule

Object Reset uses the logical Object Entry identity:

```text
ObjectEntryId + Scope + OwnerIdentity
```

The following are not functional identity:

```text
GameObject.name
Hierarchy path
Scene path
InstanceID
Sibling index
Tag
Layer
```

Names may appear in diagnostics only.

## Configure a Transform Reset Participant

1. Add `ObjectEntryDeclaration` to the object.
2. Add `Transform Reset Participant` to the same object, or to a component object that points to a specific Target Transform Override.
3. Assign `Target Declaration` to the `ObjectEntryDeclaration`.
4. Choose `Requiredness`:
   - `Required`: failure blocks Object Reset.
   - `Optional`: failure completes Object Reset with warnings.
5. Choose the reset axes:
   - Reset Local Position
   - Reset Local Rotation
   - Reset Local Scale
6. Set the object to the desired local baseline.
7. Click `Capture Current Local Transform Baseline`.
8. Add the participant to `Object Reset Unity Participant Source > Participants`.

## Baseline behavior

`Transform Reset Participant` restores authored local state:

```text
localPosition
localRotation
localScale
```

It does not restore world position directly. World position is a result of local state and parent transforms.

## Missing baseline behavior

If baseline is not configured:

```text
Required participant -> Object Reset fails with blocking issue
Optional participant -> Object Reset completes with warnings
```

This is intentional. F15 must not hide missing required adapters or required baselines behind `SucceededNoParticipants`.

## Trigger usage

A UI Button can call:

```text
Object Reset Trigger > RequestObjectReset()
```

Recommended authoring shape:

```text
Object Reset Button or control object
└─ Object Reset Trigger
   └─ Target Declaration = the reset target ObjectEntryDeclaration
```

The trigger resolves the current scope and owner from the Object Entry snapshot.

## Bridge usage

`Object Reset Trigger Unity Event Bridge` is optional.

Use it only when you need Inspector `UnityEvent` callbacks such as:

```text
RequestSucceeded
RequestSucceededNoParticipants
RequestCompletedWithWarnings
RequestFailed
RequestCompleted
```

A UI Button does not require the bridge if it can call `RequestObjectReset()` directly.

## Current limitations

F15 covers Transform reset only.

Still outside this guide:

```text
Rigidbody reset
Animator reset
GameObject active reset
Player/Actor reset
Pool return
Save/checkpoint restore
Scene reload
```

## Validation smokes

For the closed F15 implementation, run:

```text
Run Object Reset Unity Adapters Closure Smoke
```

For broader regression after future changes, run:

```text
Run Object Reset Foundation Closure Smoke
Run Object Entry Foundation Closure Smoke
Run Standard Smoke
```

---

## F16 update — GameObject Active Reset

F16 adds a second primitive adapter:

```text
ObjectResetGameObjectActiveParticipant
```

It restores only `GameObject.activeSelf` from an authored baseline. Use it as a primitive piece for simple props or content state, not as a replacement for contextual Player, Actor, Timer, NPC, Pickup, Door or gameplay reset.

For details, see:

```text
Documentation~/Guides/F16-GameObject-Active-Reset-Usage.md
```
