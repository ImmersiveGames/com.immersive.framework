# F45-ADR-ACTOR-001 — Actor / PlayerSlot Identity Boundary

Status: Proposed / Planning ADR  
Phase: F45 — Actor, PlayerSlot and Runtime Gameplay Identity  
Type: Actor / PlayerSlot / Identity / Input / Camera / Runtime Gameplay Boundary  
Last updated: 2026-07-06

---

## 1. Context

The framework already has mature lifecycle and orchestration axes:

```text
Route
Activity
Route Content
Activity Content
RuntimeContent
ContentAnchor
Reset
Snapshot
InputMode
Gate
Pause
Transition
Loading
```

The recent FIRSTGAME integrations proved that Route/Activity lifecycle can drive consumer-side systems without coupling those systems back into framework core:

```text
Route/Activity BGM adapter
Route/Activity Camera adapter
Unity PlayerInput Gate adapter
Unity Reset Subject adapter
```

The next missing layer is not movement, combat, inventory or a full gameplay object system. The missing layer is stable runtime gameplay identity.

A historical ADR, `F16-ADR-PLAYER-001`, deferred Player/Participant work until Gate, Transition, Pause and gameplay object boundaries were more mature. That condition is now partly satisfied, but the framework must still avoid jumping directly into a Player framework.

F31 introduced an experimental actor identity surface under `Runtime/Actors`:

```text
ActorId
ActorKind
IActor
PlayerActorDeclaration
PlayerActorDescriptor
PlayerActorSet
PlayerActorValidator
```

That work established a useful primitive:

```text
Actor identity is separate from movement, input behavior, spawn policy, save ownership and lifecycle ownership.
```

However, F31 currently stops at static PlayerActor declaration and validation. It does not yet define:

```text
generic non-player Actor declaration
Actor role / gameplay role vocabulary
runtime Actor registration
PlayerSlot identity
PlayerSlot occupancy
relationship between PlayerInputManager, PlayerInput, local camera/listener/UI and current Actor
relationship between Actor identity and existing Participant/Executor patterns
Unity-native mapping from GameObject/components to identity/capabilities
future dynamic Actor materialization through RuntimeContent
```

FIRSTGAME currently has real player behavior components, such as movement and resettable state, but those components are not yet connected to the framework Actor identity surface. The game also needs a conceptual place for Player 1 / Player 2 / Player 3 / Player 4 before those slots are mapped to a concrete Actor in the world.

---

## 2. Problem

The framework must avoid these invalid equivalences:

```text
Player == Actor
Actor == Participant
Actor == Capability
PlayerInput == Player identity
PlayerInput == Actor identity
Camera target == Player
AudioListener == Actor
Reset subject == Actor
Runtime spawned object == Actor by default
```

Those shortcuts work only for a small single-player prototype. They break when the game needs:

```text
local multiplayer
split screen
remote players
player join/leave
runtime character swap
possession
vehicle/drone/body control
respawn with a replacement prefab
player-local UI
player-local camera/listener
bosses/NPCs spawned during an Activity
actor state saved independently from player slot state
```

The framework therefore needs a vocabulary that separates:

```text
who is playing
what entity exists in the world
which entity a player slot currently occupies
which systems/components participate in a pipeline
which capabilities a runtime entity exposes
which Unity components are merely evidence or execution surfaces
```

---

## 3. Decision

Define a conceptual boundary with five distinct terms:

```text
Actor
PlayerSlot
PlayerSlotOccupancy
ActorCapability
Framework Participant
```

This ADR does not mandate immediate implementation of all five. It defines the canonical model future work must follow.

Core decision:

```text
PlayerSlot is who plays.
Actor is what they currently play as.
PlayerSlotOccupancy is the relation between them.
Capabilities and Participants remain orthogonal components/systems.
```

---

## 4. Conceptual Layer Model

The architecture is separated into three layers:

```text
Unity Layer
  GameObject
  MonoBehaviour components
  PlayerInput
  PlayerInputManager
  Camera
  Cinemachine
  AudioListener

Framework Identity Layer
  ActorId
  ActorKind
  ActorRole
  IActor
  ActorDeclaration / PlayerActorDeclaration
  PlayerSlotId
  PlayerSlot
  PlayerSlotOccupancy

Capabilities / Participants Layer
  IActorCapability
  MovementCapability
  HealthCapability
  CameraTargetCapability
  ResetParticipant
  SnapshotParticipant
  ActivityContentExecutionParticipant
```

Important correction:

```text
PlayerSlotOccupancy points to an Actor.
Capabilities are resolved from the occupied Actor or related components.
PlayerSlotOccupancy must not point directly to arbitrary capabilities as its primary relation.
```

Preferred flow:

```text
PlayerInputManager
-> PlayerInput evidence
-> PlayerSlot
-> PlayerSlotOccupancy
-> Actor
-> Actor capabilities / framework participants
```

---

## 5. Canonical Definitions

### 5.1 Actor

An `Actor` is a logical gameplay entity with stable framework identity.

Canonical definition:

```text
Actor is a runtime gameplay entity identity. It names an entity that can exist, be declared, validated and later registered in a Route/Activity/Session context. It does not imply movement, input, camera, reset, save, combat or spawn behavior.
```

An Actor answers:

```text
Who is this gameplay entity?
What stable ActorId represents it?
What broad category is it?
What gameplay role does it have?
What diagnostic display name should identify it?
```

An Actor must not answer:

```text
How does it move?
Which action map is active?
Which camera follows it?
How is it spawned?
How is it reset?
How is it saved?
How does it take damage?
```

Those behaviors belong to separate components, adapters, capabilities or participant sources.

---

### 5.2 PlayerSlot

A `PlayerSlot` is the logical seat of a player.

Canonical definition:

```text
PlayerSlot is the stable identity of a player participation seat, such as Player 1, Player 2, Player 3 or Player 4. It owns player-local input/view concerns and can occupy an Actor, but it is not the Actor itself.
```

A PlayerSlot answers:

```text
Which player seat is this?
Which PlayerInput / device/user evidence is associated with this seat?
Which camera/view/listener/UI channel belongs to this seat?
Which Actor is this seat currently occupying, if any?
```

A PlayerSlot should be Session-scoped by default. It remains stable across:

```text
Route switch
Activity switch
actor respawn
character swap
vehicle possession
body replacement
presentation swap
temporary control transfer
```

Example:

```text
PlayerSlotId.Player1 -> ActorId.firstgame.player
PlayerSlotId.Player1 -> ActorId.firstgame.ship
PlayerSlotId.Player1 -> ActorId.firstgame.drone
```

The slot remains `Player1`; the occupied Actor may change.

---

### 5.3 PlayerSlotOccupancy

`PlayerSlotOccupancy` is the active relation between a PlayerSlot and an Actor.

Canonical definition:

```text
PlayerSlotOccupancy links a player seat to the Actor currently controlled, represented or viewed through that seat.
```

It answers:

```text
Which Actor does Player 1 currently occupy?
Which PlayerSlot controls or represents this Actor?
Was the occupancy accepted, rejected, cleared or replaced?
Why was the occupancy changed?
```

This relation is the correct place for possession and player-body replacement semantics.

`PlayerSlotOccupancy` is not a spawn operation by itself. It may reference an Actor instance that was scene-authored, spawned by a consumer, or later materialized through RuntimeContent.

---

### 5.4 ActorCapability

An `ActorCapability` is an optional behavior/contribution associated with an Actor.

Canonical definition:

```text
ActorCapability is a component or adapter that provides a specific gameplay-facing ability for an Actor while keeping Actor identity minimal.
```

Possible future capabilities:

```text
MovementCapability
HealthCapability
CameraTargetCapability
InputReceiverCapability
InteractableCapability
DamageableCapability
```

`IActorCapability` may be introduced later as a marker or query contract, but it must not become a God-interface and must not replace framework-specific participant contracts.

---

### 5.5 Framework Participant

The framework already uses `Participant` as a pipeline execution idiom:

```text
ResetParticipant
SnapshotParticipant
CycleResetParticipant
ActivityContentExecutionParticipant
```

That term must not be reused as the main gameplay entity identity.

Canonical definition:

```text
Participant is a contribution to a specific system or pipeline. Actor is the entity identity. A component may be attached to an Actor and participate in Reset/Snapshot/ActivityContent/etc., but the Actor itself is not the participant contract.
```

Examples:

```text
PlayerActorDeclaration       -> Actor identity declaration
FirstGamePlayerResettable    -> Reset participant / reset capability
FirstGamePlayerMover         -> movement behavior
PlayerInput                  -> Unity input evidence for a PlayerSlot or current local player actor declaration
CameraTargetCapability       -> camera target contribution
SnapshotParticipant          -> save/snapshot contribution
```

The framework must preserve this separation.

---

## 6. Unity GameObject and Component Mapping

The Unity-native mapping should remain composition-first.

Preferred shape:

```text
GameObject
  + ActorDeclaration or PlayerActorDeclaration
  + optional Actor capabilities
  + optional framework participants
  + Unity execution components
```

Example FIRSTGAME player:

```text
PlayerPrototype
  + PlayerActorDeclaration
  + PlayerInput
  + FirstGamePlayerMover
  + FirstGamePlayerResettableState
  + future CameraTargetCapability
  + future PlayerSlotOccupant evidence
```

Example boss:

```text
BossRoot
  + ActorDeclaration
  + BossHealth
  + BossAi
  + BossResettableState
  + BossSnapshotState later
  + future CameraTargetCapability if boss framing is needed
```

Decision:

```text
ActorDeclaration is the Unity component that carries Actor identity.
Actor is not a mandatory MonoBehaviour base class for gameplay behaviors.
```

Do not require a class hierarchy such as:

```text
class Player : Actor
class Boss : Actor
class Enemy : Actor
```

The framework should prefer:

```text
IActor = minimal identity contract
ActorDeclaration = MonoBehaviour identity declaration
PlayerActorDeclaration = specialized identity/evidence declaration for current local player use cases
capabilities/participants = separate MonoBehaviours or adapters
```

This keeps Unity composition intact and avoids inheritance-heavy gameplay code.

---

## 7. ActorKind and ActorRole

F31 introduced `ActorKind` as:

```text
Unknown
Player
NonPlayer
```

This remains useful, but it must stay broad.

`ActorKind` answers:

```text
What broad category / authority class is this Actor?
```

It must not become a gameplay taxonomy.

This ADR proposes a future `ActorRole` axis.

`ActorRole` answers:

```text
What broad gameplay role does this Actor serve?
```

Initial vocabulary should remain small:

```text
Unknown
Protagonist
Enemy
Boss
Ally
Neutral
Objective
Interactable
```

Do not start with a large role enum containing specific enemy classes or project-specific taxonomy.

A boss should be modeled as:

```text
ActorKind.NonPlayer
ActorRole.Boss
```

A player avatar should be modeled as:

```text
ActorKind.Player
ActorRole.Protagonist
```

A player-controlled vehicle could be modeled as either:

```text
ActorKind.Player
ActorRole.Vehicle
```

or:

```text
ActorKind.NonPlayer
ActorRole.Vehicle
PlayerSlotOccupancy: Player1 -> VehicleActor
```

The latter may be preferable if the vehicle remains a world actor temporarily occupied by a player slot.

---

## 8. Player-Specific Inherent Concerns

Player has unavoidable concerns that generic Actors do not have:

```text
PlayerInput
PlayerInputManager participation
local player index
input device/user pairing
player-local camera or viewport
player-local audio listener policy
player-local UI
split-screen / view ownership
join/leave evidence
```

These concerns belong primarily to `PlayerSlot`, `PlayerSlot view`, or Unity input evidence, not to generic Actor identity.

Preferred ownership model:

| Concern | Canonical owner |
|---|---|
| PlayerInputManager evidence | Session / Unity input integration evidence |
| PlayerInput instance | PlayerSlot / Unity input evidence |
| local player index | PlayerSlot |
| device pairing | PlayerSlot / Unity input evidence |
| player camera/view | PlayerSlot view/channel |
| audio listener policy | PlayerSlot view/channel or global single-player policy |
| UI local to a player | PlayerSlot |
| movement command execution | occupied Actor capability |
| health/state/position | Actor capability |
| reset behavior | reset participant attached to Actor or related object |
| save/snapshot behavior | snapshot participant attached to Actor or related object |

Therefore, the framework must not treat `PlayerActorDeclaration` as the whole player system.

A player in the world is:

```text
PlayerSlot + PlayerSlotOccupancy + Actor + capabilities
```

Informal rule:

```text
PlayerSlot is who plays.
Actor is what they currently play as.
```

---

## 9. PlayerInput Evidence and PlayerSlot Binding

Unity `PlayerInput` and `PlayerInputManager` are official Unity execution components. The framework must not replace them with a custom input manager.

Decision:

```text
PlayerInput is evidence for a local PlayerSlot.
PlayerInput is not PlayerSlot identity.
PlayerInput is not Actor identity.
```

Preferred future flow:

```text
PlayerInputManager / PlayerInput evidence
-> PlayerSlot identity
-> PlayerSlotOccupancy
-> occupied Actor input/movement capability
```

The current `UnityPlayerInputGateAdapter` may block/release gameplay action maps through Gate, but it does not define player identity, actor identity, join policy, spawn policy or movement behavior.

Current local FIRSTGAME may keep `PlayerActorDeclaration` requiring a same-GameObject `PlayerInput` as local evidence, but this must remain a current implementation constraint, not an eternal concept rule.

Future cases that must remain possible:

```text
remote player
AI possession
replay ghost
network proxy
spectator
vehicle possession
```

---

## 10. View Ownership: Camera, AudioListener and UI

Camera, AudioListener and player-local UI must not be modeled as inherent Actor ownership.

Decision:

```text
PlayerSlot owns player-local view/channel concerns.
Actor may expose camera targets or view-related capabilities.
Route/Activity camera policy remains able to override, retain or fallback.
```

Preferred future camera relation:

```text
PlayerSlot owns the player-local view/channel.
Actor exposes optional CameraTargetCapability.
PlayerSlotCamera binding resolves the current occupied Actor target.
Route/Activity camera policy remains able to override or fallback.
```

In single-player, this may look equivalent to following the player actor. Architecturally it is not equivalent.

Audio listener policy follows the same boundary:

```text
AudioListener belongs to a PlayerSlot view/channel or a single-player global view policy.
AudioListener does not belong to Actor identity by default.
```

---

## 11. Relationship to Existing F31 Actors

F31 remains valid as a minimal identity/evidence phase.

Current F31 shape:

```text
ActorId
ActorKind
IActor
PlayerActorDeclaration
PlayerActorDescriptor
PlayerActorSet
PlayerActorValidator
```

This ADR refines F31 as follows:

1. `ActorId` remains the canonical stable Actor identity.
2. `IActor` remains minimal and must not grow movement/input/spawn/save methods.
3. `PlayerActorDeclaration` remains a valid specialization for player actor evidence.
4. `PlayerActorDeclaration` requiring Unity `PlayerInput` is acceptable for current local FIRSTGAME evidence, but must not become a permanent rule that all player-like Actors always have a same-GameObject `PlayerInput`.
5. A future generic `ActorDeclaration` should support non-player Actors without requiring `PlayerInput`.
6. A future `ActorRole` should complement, not replace, `ActorKind`.
7. A future `PlayerSlot` model must be added beside Actor, not hidden inside Actor.

---

## 12. Actor Capabilities vs Framework Participants

The participant/executor idiom remains the correct pattern for pipeline execution:

```text
descriptor/passive request
validation set with issues
executor
ordered execution
exception capture
aggregated result
```

Future Actor/PlayerSlot systems should reuse that idiom when executing operations such as:

```text
register actor declarations
validate player slot declarations
resolve player slot occupancy
apply actor registration/unregistration
query actor capability descriptors
```

But `Actor` itself should not be renamed to `Participant`.

Reason:

```text
Participant already means a contribution to a specific system.
Actor means an entity identity.
```

`IActorCapability` may be added later as an optional capability contract, but it must not replace existing participant contracts.

Correct separation:

```text
Actor Capabilities
  MovementCapability
  HealthCapability
  CameraTargetCapability
  InputReceiverCapability

Framework Participants
  ResetParticipant
  SnapshotParticipant
  CycleResetParticipant
  ActivityContentExecutionParticipant
```

A component can implement both a capability and a participant interface when that is meaningful, but the concepts remain distinct.

---

## 13. Future ActorRegistry Guardrails

This ADR does not implement `ActorRegistry`, but the model must remain compatible with it.

`FindObjectsByType` is acceptable for:

```text
editor setup
validation
QA smoke discovery
one-off diagnostics
```

It is not acceptable as the canonical runtime query path for active Actors.

A future ActorRegistry must be registration-based:

```text
Register / Unregister
RegistrationHandle
Dictionary<ActorId, IActor>
optional indices by ActorKind / ActorRole / PlayerSlot occupancy
registration result with status/issues
unregistration result with status/issues
change events or notifications
```

Future queries may include:

```text
all active Actors
Actor by ActorId
current Actor occupied by PlayerSlot1
all Actors with ActorRole.Boss
all Actors in current Activity context
```

Do not implement this registry before a runtime need appears.

Acceptable triggers:

```text
boss spawned/despawned at runtime
wave enemies need active query
player body replacement needs stable relation
camera/input must resolve current occupant dynamically
save/snapshot requires active actor enumeration
```

---

## 14. Relationship to RuntimeContent / ContentAnchor

Runtime-spawned NPCs, bosses, pickups and temporary player bodies should eventually be materialized through the existing runtime content language rather than through isolated raw `Instantiate` / `Destroy` paths.

Preferred future direction:

```text
RuntimeContent request
-> materialization adapter
-> RuntimeContentHandle
-> owner / scope / release policy
-> optional ActorDeclaration / ActorRegistration
-> optional capabilities / participants
```

This avoids two disconnected worlds:

```text
content known by the framework
content spawned by game code outside lifecycle ownership
```

Guardrail:

```text
Instantiate/Destroy is acceptable for local prototypes.
It must not become the canonical path for framework-known NPC/Boss/Player runtime materialization.
```

This ADR does not reopen the superseded F44 runtime object participation layer. Runtime reset remains handled by the current Unity reset subject/participant model. Actor materialization must be planned as a distinct future cut.

---

## 15. Relationship to Reset and Snapshot

Actor identity must not absorb Reset or Snapshot.

Correct model:

```text
ActorDeclaration declares identity.
Reset participant resets state.
Snapshot participant contributes persistence state.
```

Examples:

```text
Player Actor
+ Player reset participant
+ Player snapshot participant later

Boss Actor
+ Boss health component
+ Boss reset participant
+ Boss snapshot participant later

Door Actor or Interactable Actor
+ Door interaction component
+ Door snapshot participant
+ optional Door reset participant
```

Reset/Snapshot can reference Actor identity for diagnostics or ownership, but Actor must not become a reset interface or save interface.

Save implication for future progression work:

```text
PlayerSlot is appropriate for player-seat state.
ActorId is appropriate for world-entity state.
Snapshot participants decide what state is captured.
```

---

## 16. Identity Stability Decisions

### 16.1 PlayerSlotId stability

`PlayerSlotId` is stable across runtime context changes.

Initial vocabulary may be:

```text
Player1
Player2
Player3
Player4
```

The slot should not be recreated merely because the occupied Actor respawned, changed prefab, entered a vehicle or changed presentation.

---

### 16.2 ActorId stability

`ActorId` is stable while the logical gameplay entity remains the same.

Recommended policy:

```text
Respawn of the same logical character keeps ActorId.
Replacement by a different logical body/entity may change ActorId.
Pure presentation/skin swap does not change ActorId.
Temporary possession may change PlayerSlotOccupancy without changing either ActorId.
```

Examples:

```text
Same protagonist respawns -> ActorId.firstgame.player remains.
Player enters a ship -> PlayerSlot.Player1 occupies ActorId.firstgame.ship.
Player changes skin -> ActorId.firstgame.player remains.
Boss phase swaps visual prefab -> ActorId.boss.planet-core remains if it is same boss entity.
```

---

### 16.3 ActorDeclaration and lifetime

`ActorDeclaration` does not own lifetime by itself.

Lifetime belongs to one of these contexts:

```text
scene-authored GameObject lifetime
Route-owned content
Activity-owned content
RuntimeContent-owned materialization
consumer-owned prototype lifetime
```

Actor identity, instance lifetime and registration are distinct concepts.

---

## 17. Proposed Incremental Path

This ADR accepts an incremental path, not a single large Actor system implementation.

### Step 1 — Connect existing F31 PlayerActorDeclaration to FIRSTGAME player

Goal:

```text
The real FIRSTGAME PlayerPrototype carries framework Actor identity evidence.
```

Expected scope:

```text
Add/validate PlayerActorDeclaration on PlayerPrototype.
Keep FirstGamePlayerMover separate.
Keep FirstGamePlayerResettableState separate.
Do not create ActorRegistry yet.
Do not change movement behavior.
Do not change PlayerInputManager ownership.
Do not change camera/BGM adapters.
```

### Step 2 — Add generic ActorDeclaration

Goal:

```text
Non-player actors can declare Actor identity without PlayerInput.
```

Expected examples:

```text
NPC
Boss
Objective
Interactable
```

### Step 3 — Add ActorRole

Goal:

```text
Classify gameplay role without overloading ActorKind.
```

### Step 4 — Add PlayerSlot identity/evidence model

Goal:

```text
Represent Player1 / Player2 / Player3 / Player4 as stable player seats.
```

Initial scope should be evidence/validation first:

```text
PlayerSlotId
PlayerSlotDeclaration or PlayerSlotEvidence
PlayerInput evidence
PlayerInputManager session evidence relationship
no join/spawn implementation yet
```

### Step 5 — Add PlayerSlotOccupancy

Goal:

```text
Represent PlayerSlot -> Actor relation explicitly.
```

Initial scope can remain passive:

```text
validate one PlayerSlot occupies one Actor
diagnose missing Actor
diagnose duplicate occupancy
no possession behavior yet
```

### Step 6 — Add IActorCapability only when it has a real consumer

Goal:

```text
Allow actor-attached behaviors to advertise capabilities without bloating IActor.
```

Initial scope should be narrow:

```text
marker/descriptor contract
no global capability registry unless needed
no replacement of Reset/Snapshot participants
```

### Step 7 — Add ActorRegistry only when runtime need appears

Do not implement a full ActorRegistry before the game demonstrates the need for dynamic Actor queries.

### Step 8 — Integrate dynamic actor materialization with RuntimeContent/ContentAnchor

Only after ActorDeclaration/PlayerSlot/Occupancy are stable.

---

## 18. Excluded from this ADR

This ADR explicitly excludes:

```text
movement controller
character controller
combat
damage model
health model
inventory
complete NPC framework
AI framework
boss behavior framework
official spawn manager
pooling integration
progression save implementation
network player model
split-screen implementation
action-map switching implementation
PlayerInputManager join policy
camera director rewrite
audio listener implementation
runtime content materialization implementation
ActorRegistry implementation
full IActorCapability implementation
```

---

## 19. Guardrails

- Do not make Player the center of the framework lifecycle.
- Do not make Actor the center of every gameplay system.
- Do not make Actor a mandatory base class for gameplay behaviors.
- Do not rename the existing participant/executor idiom to Actor.
- Do not make Actor implement Reset/Snapshot/Input/Camera directly.
- Do not make `IActor` grow movement/input/spawn/save methods.
- Do not store PlayerInputManager ownership inside Actor.
- Do not store PlayerInput ownership directly inside generic Actor.
- Do not require every player-like Actor to always have same-GameObject `PlayerInput` forever.
- Do not treat `PlayerInput` as PlayerSlot identity.
- Do not treat `PlayerInput` as Actor identity.
- Do not treat Camera, AudioListener or player-local UI as Actor identity concerns.
- Do not use GameObject name, tag, scene path or prefab path as canonical Actor identity.
- Do not use PlayerSlotId as ActorId.
- Do not use ActorId as PlayerSlotId.
- Do not make PlayerSlotOccupancy imply spawn/materialization by itself.
- Do not make PlayerSlotOccupancy point directly to capabilities as its primary relationship.
- Do not use global runtime searches as the canonical runtime path for future ActorRegistry.
- Do not make `IActorCapability` replace Reset/Snapshot/ActivityContent participant contracts.
- Do not revive superseded F44 runtime object participation by mixing Actor with reset participation.

---

## 20. Acceptance Criteria for the Concept

The concept is accepted when the documentation consistently distinguishes:

```text
Actor identity
PlayerSlot identity
PlayerSlotOccupancy relation
Actor capabilities
Framework participants
Unity PlayerInput evidence
Unity camera/listener/view surfaces
RuntimeContent materialization
Reset/Snapshot participation
```

A future implementation should be considered correct only if these examples can be represented without identity collapse:

```text
Single-player protagonist:
PlayerSlot.Player1 occupies Actor.firstgame.player

Player swaps to ship:
PlayerSlot.Player1 remains stable; occupancy changes to Actor.firstgame.ship

Boss:
ActorKind.NonPlayer + ActorRole.Boss, no PlayerSlot required

Activity-local enemy:
ActorKind.NonPlayer + ActorRole.Enemy, Activity-owned lifetime

Resettable player:
Actor identity remains separate from reset participant

Camera follow:
PlayerSlot owns view; occupied Actor contributes target

PlayerInput evidence:
PlayerInput supports local PlayerSlot binding; it is not the player identity itself
```

---

## 21. Future ADRs / Cuts Expected

This ADR is expected to lead to smaller follow-up cuts:

```text
F45A — FIRSTGAME PlayerActorDeclaration connection
F45B — Generic ActorDeclaration + ActorRole
F45C — PlayerSlot identity/evidence planning
F45D — PlayerSlotOccupancy passive model
F45E — IActorCapability boundary, only if a concrete consumer appears
F45F — ActorRegistry planning, only if dynamic runtime query need is proven
F45G — RuntimeContent actor materialization boundary, only after registry/materialization need is proven
```

The first implementation should be deliberately small:

```text
connect the real FIRSTGAME player to the existing Actor identity primitive
without implementing a full player framework
without implementing a full ActorRegistry
without changing movement/input/camera/reset behavior
```

---

## 22. Final Decision Statement

The framework will treat `Actor` and `PlayerSlot` as separate identities.

```text
Actor identifies a gameplay entity.
PlayerSlot identifies a player seat.
PlayerSlotOccupancy relates a player seat to the Actor it currently occupies.
ActorCapability describes optional Actor-attached gameplay-facing abilities.
Framework Participants remain system-specific contributions such as Reset/Snapshot/ActivityContent.
```

This preserves the F31 Actor identity work, avoids turning Player into the root of framework lifecycle, respects Unity composition, and creates room for local multiplayer, runtime character swap, possession, NPCs, bosses, reset, snapshot, camera and input without collapsing all of them into one component.
