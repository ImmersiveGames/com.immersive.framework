# F16-ADR-PLAYER-001 - Player/Participant Entry Baseline

Status: Deferred / Historical Proposal  
Original phase: F16 - Player/Participant Entry Baseline  
Current phase: F22+ / Future - after Gate, Transition, Pause and a mature gameplay object model  
Type: Controlled Consumer / Player / Participation  
Last updated: 2026-06-26

---

## 1. Context

This ADR preserves a histórical proposal: Player/Participant as a future consumer of Object Entry, Object Reset, Unity Reset Adapters, Input ownership and Route/Activity lifecycle context.

It is not the current F16 plan. F16 was closed as `GameObject Active State Reset Adapter`.

It is also not the current F17 plan. F17 now starts with Gate Foundation, and F17A is documentation/ADR only.

---

## 2. Current Decision

Do not implement Player/Participant now.

Contextual reset for Player/Actor/NPC/Timer/Door/Pickup remains deferred because the framework does not yet have a mature gameplay object/actor/player model.

Before this ADR can become active again, the framework must first plan:

```text
F17 - Gate Foundation
F18 - Transition Orchestration Foundation
F19 - Transition Effects / Loading and Fade Adapters
F20 - Pause State and Pause Gate
F21 - Pause Content / Overlay / Input Boundary
```

---

## 3. Historical Proposal Kept For Reference

A future Player/Participant could consume:

```text
Object Entry
Local/Object Reset
Unity Reset Adapters
Input ownership
Gate
Pause/Transition boundaries
Activity/Route lifecycle context
```

A future phase may evaluate:

```text
typed Player/Participant identity
Player Object Entry usage
Player participation descriptor
Player readiness facts
Player reset participation
Player input binding as consumer
Player smoke
```

---

## 4. Still Excluded

This deferred ADR still excludes:

```text
Combat
Damage
complete Attributes
Powerup system
Inventory
advanced character controller
NPC framework
complete Actor framework
Projectile
gameplay Pooling
Save progression
```

---

## 5. Guardrails

- Player must not define lifecycle core.
- Player must not create a service locator.
- Player must not discover Route/Activity by itself as source of truth.
- Player must not use `GameObject.name`, tag, hierarchy path or scene path as canonical identity.
- Player must not become required for the framework to work.
- Player reset must not replace Gate, Transition or Pause boundaries.
- Player/Actor/NPC/Timer/Door/Pickup reset must not become F17-F21 work.

---

## 6. Relationship To Future Phases

Advanced Consumers, Gameplay Capabilities and contextual reset can be reconsidered in F22+ only after Gate, Transition and Pause are planned and a mature gameplay object model exists.
