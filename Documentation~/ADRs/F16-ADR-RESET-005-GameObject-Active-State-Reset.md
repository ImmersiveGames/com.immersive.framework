# F16-ADR-RESET-005 - GameObject Active State Reset Adapter

Status: Closed / Applied through F16  
Phase: F16 - GameObject Active State Reset Adapter  
Type: Unity Adapter / Object Reset  
Last updated: 2026-06-26

---

## 1. Context

F15 closed the first concrete Object Reset Unity adapter: `ObjectResetTransformParticipant`, with authored local baseline and explicit Unity source.

F16 closed the second primitive adapter: `ObjectResetGameObjectActiveParticipant`.

This adapter restores only `GameObject.activeSelf` from an authored baseline. It is useful for simple props, Activity-local content, triggers, visual objects and other cases where active state itself is the local state to restore.

It is not contextual gameplay reset.

---

## 2. Decision

F16 adds only:

```text
ObjectResetGameObjectActiveParticipant
```

It restores only:

```text
GameObject.activeSelf
```

Functional identity still comes from:

```text
ObjectEntryDeclaration
ObjectEntryId
Scope
OwnerIdentity
```

The adapter is registered through:

```text
ObjectResetUnityParticipantSource
```

---

## 3. Included Scope

F16 includes:

```text
ObjectResetGameObjectActiveParticipant
authored activeSelf baseline
Capture Current Active State Baseline
simple Inspector/authoring UX
required/optional baseline guardrails
canonical closure smoke
roadmap/docs/guide updaté
```

---

## 4. Excluded Scope

F16 excludes:

```text
activeInHierarchy reset
children active state reset
component enabled reset
Renderer/Collider reset
Rigidbody reset
Animator reset
Player reset
Actor/NPC reset
Timer reset
Pickup/Door/Hazard contextual reset
Pooling
Save/checkpoint restore
gameplay state mutation
```

---

## 5. Rules

### 5.1 Baseline

The baseline is explicit:

```text
Baseline Configured
Baseline Active Self
```

`activeInHierarchy` is not a valid baseline because it depends on parent state.

### 5.2 Required/Optional

Same rule as F15:

```text
Required without baseline -> Failed / blocking issue
Optional without baseline -> CompletedWithWarnings / non-blocking issue
```

### 5.3 Active Source

A participant may reset its own GameObject to inactive or active, but `ObjectResetUnityParticipantSource` must remain on an active organizational object. The source cannot depend on auto-discovery by the target object.

### 5.4 Forbidden Identity

The adapter cannot use these as functional identity:

```text
GameObject.name
hierarchy path
scene path
InstanceID
sibling index
tag/layer
```

---

## 6. Closed Result

F16 closed the adapter in one cut, with canonical smoke validating:

```text
reset to activeSelf=false
reset to activeSelf=true even after target inactive
explicit Unity source
required adapter absence blocks when policy requires participant
required baseline absence blocks
optional baseline absence becomes warning
```

---

## 7. Usage Note

This adapter is a primitive piece.

For real gameplay objects, contextual reset remains deferred until after Gate, Transition, Pause and a mature gameplay object model:

```text
Player reset
Actor reset
Timer reset
Pickup reset
Door reset
NPC reset
Encounter reset
```

Future consumers may use Transform/GameObject Active adapters internally, but they must not be replaced by over-generic technical adapters or pulled into F17-F21.
