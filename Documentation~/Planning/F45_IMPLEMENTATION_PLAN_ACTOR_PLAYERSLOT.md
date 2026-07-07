# F45 Implementation Plan — Actor Identity, PlayerSlot and Adapter Identity Bridge

Status: **planning / ready for next chat**  
Scope: **Immersive Framework 1.0 + FIRSTGAME integration path**  
Related ADR: `F45-ADR-ACTOR-001-Actor-PlayerSlot-Identity-Boundary.md`

---

## 1. Purpose

This plan defines the incremental implementation path for the Actor / PlayerSlot architecture.

The goal is **not** to implement a full actor framework immediately. The goal is to create enough identity cohesion to stop relying on loose player strings and scene-specific assumptions, then use that foundation to connect existing adapters more safely.

The intended direction is:

```text
1. Consolidate minimal Actor identity.
2. Connect the real FIRSTGAME player to Actor identity.
3. Introduce PlayerSlot as the stable logical player seat.
4. Bridge existing adapters to ActorId / PlayerSlotId.
5. Support runtime player occupant replacement.
6. Prove that Actor is not only Player by adding a minimal NonPlayer Actor case.
```

---

## 2. Architectural premise

The core model is:

```text
PlayerSlot = who plays.
Actor      = what they currently play as.
```

The framework should not collapse these concepts into a single `Player` object.

### 2.1 Actor

`Actor` is the logical identity of a gameplay entity in the world.

Examples:

```text
firstgame.player
firstgame.player.ship
firstgame.enemy.basic.001
firstgame.boss.planet-core
```

An Actor may have capabilities and may participate in systems such as reset, snapshot, input, camera target, movement, health, interaction, or runtime content ownership.

An Actor should **not** become a god component.

### 2.2 PlayerSlot

`PlayerSlot` is the stable logical seat of a player.

Examples:

```text
Player1
Player2
Player3
Player4
```

The PlayerSlot owns player-local concerns such as:

```text
- player input evidence
- local camera channel / view
- local UI channel
- audio listener policy
- current Actor occupancy
```

The PlayerSlot remains stable while the occupied Actor can change.

### 2.3 PlayerSlotOccupancy

`PlayerSlotOccupancy` is the relationship:

```text
PlayerSlotId -> ActorId
```

Examples:

```text
Player1 occupies firstgame.player
Player1 occupies firstgame.player.ship
Player1 occupies firstgame.player.drone
```

This supports respawn, possession, vehicles, runtime replacement, and future local multiplayer without making `Player == Actor`.

### 2.4 Capabilities and Participants

Capabilities and participants remain orthogonal to Actor identity.

```text
ActorDeclaration = identity
ResetParticipant = reset participation
SnapshotParticipant = snapshot participation
CameraTargetCapability = camera target contribution
MovementCapability = movement contribution
HealthCapability = health/state contribution
```

A component may be both a capability and a participant when appropriate, but the concepts must not be merged.

---

## 3. Existing framework direction to preserve

The framework already has a consistent participant idiom:

```text
ResetParticipant
SnapshotParticipant
CycleResetParticipant
ActivityContentExecutionParticipant
```

These usually follow the pattern:

```text
descriptor / passive data
-> validation set with issues
-> executor / runtime dispatcher
-> aggregated result
```

The Actor work should reuse this style where applicable, but should not rename every gameplay entity as a “participant”.

Recommended vocabulary:

```text
Actor       = gameplay identity
Participant = system-specific execution contribution
Capability  = component-level behavior or contribution associated with an Actor
```

---

## 4. Implementation sequence

## F45A — Actor Identity Cohesion

### Goal

Consolidate the minimal Actor identity model inside the framework.

This is the foundation for everything else. It does not implement PlayerSlot yet.

### Scope

Framework package:

```text
Packages/com.immersive.framework/Runtime/Actors/
Packages/com.immersive.framework/Editor/Actors/
Packages/com.immersive.framework/Documentation~/ADRs/
```

Exact paths may vary depending on the current package structure.

### Concepts

Minimum Actor model:

```text
ActorId
ActorKind
ActorRole
IActor
ActorDeclaration
PlayerActorDeclaration
Actor validation
```

### ActorKind

Keep small:

```text
Unknown
Player
NonPlayer
```

`ActorKind` describes the broad nature/category of the Actor. It must not become a gameplay-role enum.

### ActorRole

Add broad gameplay role classification:

```text
Unknown
Protagonist
Enemy
Boss
Ally
Neutral
Objective
```

`ActorRole` should not encode every game-specific type.

Avoid early roles such as:

```text
Vendor
Minion
FlyingEnemy
TankEnemy
Door
Collectible
Projectile
```

Those are game-specific types or capabilities, not foundational roles.

### ActorDeclaration

`ActorDeclaration` should be the generic Unity component carrying Actor identity.

Conceptual shape:

```text
ActorDeclaration : MonoBehaviour, IActor
```

It should expose:

```text
ActorId
ActorKind
ActorRole
DisplayName / diagnostic name
```

Important rule:

```text
ActorDeclaration declares identity.
ActorDeclaration does not own lifetime by itself.
```

Lifetime belongs to scene ownership, Route/Activity ownership, RuntimeContent ownership, or future registry/materialization policy.

### PlayerActorDeclaration

`PlayerActorDeclaration` remains a specialization of Actor identity for local player evidence.

Current constraint may remain:

```text
PlayerActorDeclaration requires PlayerInput in the current local-player implementation.
```

But conceptually:

```text
PlayerInput is evidence of local control.
PlayerInput is not PlayerSlotId.
PlayerInput is not ActorId.
```

### Out of scope

Do not implement yet:

```text
ActorRegistry
PlayerSlot
PlayerSlotManager
PlayerSlotOccupancy runtime manager
possession system
NPC lifecycle
spawn manager
save progression
camera/input rewrite
```

### PASS criteria

```text
- Existing PlayerActorDeclaration continues compiling.
- Generic ActorDeclaration exists for non-player Actors.
- ActorRole exists and is validated.
- Validator detects empty ActorId.
- Validator detects duplicated ActorId in inspected scope.
- No FIRSTGAME behavior changes are required yet.
```

---

## F45B — FIRSTGAME PlayerPrototype Actor Identity

### Goal

Connect the real FIRSTGAME player to framework Actor identity.

This makes the player a concrete logical Actor without changing gameplay behavior yet.

### FIRSTGAME target

Likely target object:

```text
PlayerPrototype
```

Current related components include:

```text
FirstGamePlayerMover
FirstGamePlayerResettableState
UnityResetSubjectAdapter
UnityPlayerInputGateAdapter
PlayerInput
```

### Expected authoring

```text
PlayerPrototype
+ PlayerActorDeclaration
+ ActorId = firstgame.player
+ ActorKind = Player
+ ActorRole = Protagonist
```

### Important rule

Do not rewrite reset, input, camera, or spawn behavior in this cut.

This cut only gives the existing player a stable logical Actor identity.

### PASS criteria

```text
- FIRSTGAME compiles.
- PlayerPrototype has PlayerActorDeclaration.
- Actor validation passes for PlayerPrototype.
- Existing reset/input/camera/audio behavior remains unchanged.
- Smoke logs show no new blocking issues.
```

---

## F45C — PlayerSlot Identity Foundation

### Goal

Introduce the stable logical player seat, without building a full manager yet.

This is the first step toward removing hardcoded player strings and direct instance coupling.

### Scope

Framework package, likely new module:

```text
Runtime/PlayerSlots/
Editor/PlayerSlots/
```

### Concepts

Minimum model:

```text
PlayerSlotId
PlayerSlotDeclaration
PlayerSlotOccupancy
```

### PlayerSlotId

Initial design options:

Option A — enum:

```text
Player1
Player2
Player3
Player4
```

Option B — struct/string identity:

```text
player.1
player.2
player.3
player.4
```

Recommended initial decision:

```text
Use a small stable identity type that can express Player1..Player4 and can later support non-local/remote/custom slots if needed.
```

Avoid tying `PlayerSlotId` permanently to Unity `PlayerInput.playerIndex`.

### PlayerSlotDeclaration

A lightweight component or asset that declares a slot in a scene or prefab context.

It should express:

```text
slotId = Player1
local player evidence = optional PlayerInput reference or resolver evidence
```

### PlayerSlotOccupancy

The minimal relation:

```text
slotId = Player1
occupiedActor = ActorDeclaration / ActorId
```

In this cut, the relation can be authored explicitly.

No full `PlayerSlotManager` is required yet.

### Out of scope

```text
PlayerInputManager join policy
split-screen layout
remote player slots
occupancy history
possession rules
runtime spawn replacement
```

### PASS criteria

```text
- PlayerSlotId exists.
- PlayerSlotOccupancy can link Player1 to firstgame.player.
- FIRSTGAME still runs the same behavior.
- No existing adapter is required to depend on PlayerSlot yet.
```

---

## F45D — Adapter Identity Bridge

### Goal

Begin replacing loose strings and implicit player references in existing adapters with concrete identity links.

This is the first cut that produces practical cleanup value.

### Existing problem examples

Current gameplay integration can involve loose references such as:

```text
subjectId = firstgame.player
playerInput = PlayerPrototype
camera target assigned manually
reset subject ids as strings
```

The goal is not to rewrite every system. The goal is to create identity bridges.

### Bridge concepts

Possible lightweight helpers:

```text
ActorIdentityReference
PlayerSlotReference
ActorIdentitySource
PlayerSlotIdentitySource
```

Names should be chosen after inspecting the actual framework style.

### Reset bridge

Current style:

```text
UnityResetSubjectAdapter.subjectId = firstgame.player
```

Target style:

```text
UnityResetSubjectAdapter.sourceActor = ActorDeclaration
resolved subjectId = sourceActor.ActorId
```

Important:

```text
Do not break existing authored subjectId immediately.
Allow explicit Actor identity to become the canonical path.
```

### Input bridge

Current input integration should become able to identify:

```text
PlayerInput evidence -> PlayerSlotId
```

But input should not control arbitrary Actor instances directly.

Target relation:

```text
PlayerInput evidence
-> PlayerSlotId
-> PlayerSlotOccupancy
-> Actor occupant
-> movement/input capability
```

### Camera bridge

Current FIRSTGAME camera can stay route/activity based for now.

Future target:

```text
PlayerSlotId.Player1
-> occupied Actor
-> CameraTargetCapability
```

This cut should not rewrite the camera system unless required. It should only make the identity path available.

### PASS criteria

```text
- Existing adapters remain compatible.
- FIRSTGAME can configure at least one adapter through ActorDeclaration or PlayerSlot identity.
- Loose strings remain only as compatibility/authoring fallback, not the new canonical model.
- No behavior regression in reset, input, camera, audio, route/activity transitions.
```

---

## F45E — Runtime Occupancy / Instantiated Player Support

### Goal

Support the key architectural requirement:

```text
PlayerSlot remains stable while the occupied Actor can change at runtime.
```

This enables future scenarios:

```text
respawn with a new prefab
vehicle possession
drone control
body swap
spectator mode
runtime replacement of player content
```

### Minimal runtime operation

A minimal explicit API is enough:

```text
PlayerSlotOccupancy.SetOccupant(actor)
PlayerSlotOccupancy.ClearOccupant(actor or reason)
```

or equivalent.

No full registry is required yet.

### Expected flow

```text
PlayerSlotId.Player1
-> occupies Actor A
-> Actor A is removed/replaced
-> PlayerSlotId.Player1 occupies Actor B
```

Adapters should gradually target the slot rather than a specific old Actor instance.

### PASS criteria

```text
- PlayerSlot identity survives occupant change.
- Actor occupant can be changed during runtime.
- Existing input/camera/reset can continue pointing to the slot or resolve the current actor through the slot path.
- No full ActorRegistry is required.
```

---

## F45F — NonPlayer Actor Proof

### Goal

Prove that Actor is not synonymous with Player.

Add a minimal NonPlayer Actor case in FIRSTGAME or QA.

### Candidate examples

```text
BossPrototype
+ ActorDeclaration
+ ActorKind = NonPlayer
+ ActorRole = Boss
```

or:

```text
EnemyPrototype
+ ActorDeclaration
+ ActorKind = NonPlayer
+ ActorRole = Enemy
```

### Important rule

Do not implement AI, combat, health, damage, boss logic, inventory, or save in this cut.

This is an identity proof only.

### PASS criteria

```text
- Player Actor and NonPlayer Actor coexist.
- Validator detects duplicated ActorId.
- No system assumes every Actor is Player.
- No system assumes every Actor has PlayerInput.
```

---

## 5. Deferred work

The following should remain explicitly deferred until there is real pressure from FIRSTGAME or QA scenarios.

## 5.1 ActorRegistry

Do not implement a full registry until runtime queries are needed.

Examples of valid pressure:

```text
Which actors are currently alive?
Who occupies Player1?
Which bosses exist in the active Activity?
Which enemies are active in this wave?
Which actors have a certain role?
```

When implemented, registry must be registration-based:

```text
Register / Unregister
RegistrationHandle
Dictionary<ActorId, IActor>
optional indices by Role / Kind / Slot / scope
events for changed registration/occupancy
operation results with issues
```

Do not use:

```text
FindObjectsByType every frame
scene name lookups
tag/name/prefab path as canonical identity
```

## 5.2 RuntimeContent actor materialization

Dynamic Actor creation should eventually align with framework ownership:

```text
RuntimeContent request
-> materialization adapter
-> RuntimeContentHandle
-> owner / scope / release policy
-> optional ActorDeclaration / ActorRegistration
```

`Instantiate` / `Destroy` may remain acceptable for local prototypes, but should not become the canonical path for NPC/Boss/Player runtime materialization.

## 5.3 Formal ActorCapabilities

Do not build a large capability framework early.

Potential future capabilities:

```text
CameraTargetCapability
MovementCapability
HealthCapability
InteractionCapability
InputReceiverCapability
DamageableCapability
SaveStateCapability
```

Capabilities must remain compositional and optional.

## 5.4 Save / progression

Do not decide save format here.

Future principle:

```text
PlayerSlotId should identify player-local progression/state.
ActorId should identify logical actor state.
SnapshotParticipant can provide serializable state contributions.
```

## 5.5 Multiplayer / remote players

Do not implement remote player support now.

Keep the model compatible with:

```text
local multiplayer
remote players
AI takeover
replay ghosts
spectators
network proxies
```

But do not implement these until they are needed.

---

## 6. Guardrails

## 6.1 Do not make Actor a god component

Do not add movement, input, reset, save, health, inventory, combat, camera, or AI directly into Actor identity.

Correct model:

```text
ActorDeclaration = identity
Capabilities / Participants = behavior and system contributions
```

## 6.2 Do not equate Player with Actor

Avoid:

```text
Player == Actor
```

Use:

```text
PlayerSlot + PlayerSlotOccupancy + Actor = player embodied in the world
```

## 6.3 Do not equate PlayerInput with Player identity

Correct model:

```text
PlayerInput is Unity evidence for local control.
PlayerSlotId is the stable logical player seat.
ActorId is the stable logical world entity.
```

## 6.4 Do not equate Camera with Actor

Correct model:

```text
Camera / AudioListener / local UI belong to PlayerSlot/View.
Camera target can come from the occupied Actor.
```

## 6.5 Do not make runtime scene search canonical

Use scene search only for:

```text
editor validators
QA setup
one-time authoring diagnostics
```

Runtime should move toward explicit references, handles, registrations, or context-provided identity.

---

## 7. Recommended next chat starting point

Start next chat with this scope:

```text
We are continuing Immersive Framework 1.0 after F45 ADR Actor/PlayerSlot planning.
Use F45 Implementation Plan as the source of truth.
Start with F45A — Actor Identity Cohesion.
Do not implement PlayerSlot yet.
Do not implement ActorRegistry yet.
Do not rewrite FIRSTGAME adapters yet.
First inspect the current Runtime/Actors module and propose the minimal patch for ActorRole + generic ActorDeclaration + validation alignment.
```

Expected uploaded/referenced sources:

```text
- latest com.immersive.framework package or repo
- latest planet-devourer/FIRSTGAME if needed only for validation context
```

---

## 8. Current recommended order

```text
F45A — Actor Identity Cohesion
F45B — FIRSTGAME PlayerPrototype Actor Identity
F45C — PlayerSlot Identity Foundation
F45D — Adapter Identity Bridge
F45E — Runtime Occupancy / Instantiated Player Support
F45F — NonPlayer Actor Proof
```

Primary near-term goal:

```text
Give the real FIRSTGAME player a concrete logical Actor identity first, then introduce PlayerSlot as the stable player seat, then clean up existing adapters through ActorId / PlayerSlotId instead of loose strings or instance-specific assumptions.
```
