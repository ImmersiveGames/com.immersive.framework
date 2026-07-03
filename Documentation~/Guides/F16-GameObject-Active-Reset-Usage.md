# F16 — GameObject Active Reset Usage

F16 adds one primitive Unity Object Reset adapter:

```text
ObjectResetGameObjectActiveParticipant
```

It restores only:

```text
GameObject.activeSelf
```

It does not reset `activeInHierarchy`, children, components, physics, animation or gameplay state.

---

## Where things live

Framework scripts live in the package:

```text
Packages/com.immersive.framework
```

Configured components live in scenes or prefabs:

```text
Scene object / prefab
├─ ObjectEntryDeclaration
└─ GameObject Active Reset Participant
```

The participant source lives on an active organizational object:

```text
Framework Runtime / Object Reset Unity Participant Source
└─ Participants:
   - SomeObject.GameObjectActiveResetParticipant
```

Do not copy framework scripts into `Assets/Framework`.

---

## Basic setup

On the object that should restore active state:

```text
1. Add ObjectEntryDeclaration.
2. Add GameObject Active Reset Participant.
3. Assign Target Declaration.
4. Choose Required or Optional.
5. Capture Current Active State Baseline, or set Baseline Active Self manually.
6. Add the participant to Object Reset Unity Participant Source.
```

Then request reset through:

```text
ObjectResetTrigger
FrameworkRuntimeHost.RequestObjectResetAsync(...)
```

---

## Baseline rule

Use:

```text
Baseline Active Self
```

Do not use `activeInHierarchy` as baseline. `activeInHierarchy` depends on parents and is not local object state.

---

## Required vs Optional

```text
Required without baseline -> blocks Object Reset.
Optional without baseline -> Object Reset completes with warnings.
```

---

## Important source rule

If a participant can reset its own object to inactive, the source that registers it should stay active elsewhere.

Recommended:

```text
Framework Runtime Object
└─ Object Reset Unity Participant Source

Door_01
├─ ObjectEntryDeclaration
└─ GameObject Active Reset Participant
```

Avoid relying on a disabled target object to register itself.

---

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

For gameplay objects, contextual reset remains deferred until after Gate, Transition, Pause and a mature gameplay object model. Use this adapter only as a primitive piece when `activeSelf` itself is the correct local state to restore.

---

## Smoke

Use:

```text
Run Object Reset GameObject Active Closure Smoke
```

Expected coverage:

```text
reset to activeSelf=false
reset to activeSelf=true
required missing adapter blocks
required missing baseline blocks
optional missing baseline warns
```
