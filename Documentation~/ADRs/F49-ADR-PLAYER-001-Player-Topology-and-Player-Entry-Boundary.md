# F49-ADR-PLAYER-001 — Player Topology and Player Entry Boundary

Status: Proposed / Planning ADR  
Phase: F49 — Player Topology, Player Entry and PlayerView Ownership  
Type: Player / Actor / Entry / Topology / Lifecycle Boundary  
Last updated: 2026-07-07

---

## 1. Context

The framework has proven Route, Activity, Transition, Loading, Pause, Gate, Reset, BGM Route/Activity integration, Camera Route/Activity integration, PlayerSlot identity, PlayerActor identity, PlayerInput gate bridge and Reset subject Actor bridge.

The latest FIRSTGAME validation established the single-player authoring bridge:

```text
PlayerSlot: player.1
Actor: firstgame.player
PlayerSlotOccupancy: player.1 -> firstgame.player
PlayerInput evidence: PlayerPrototype
ResetSubject: Actor:firstgame.player
```

This closes the first player identity bridge, but it does not define a complete player entry model.

---

## 2. Problem

The framework currently has valid identity declarations, but it does not explicitly distinguish:

```text
a configured player slot
a joined local input
a selected actor
an instantiated actor object
an initialized/ready actor
a bound player view
an active player view
a suspended player view
a controllable actor
```

Those states are not equivalent. A Route may enter before a player object is active. An Activity may begin with a presentation camera before control is released. A PlayerInput may exist before character selection. A remote actor may exist without local input.

---

## 3. Decision

Introduce `PlayerTopology` as a framework-level concept and `PlayerEntry` as the canonical vocabulary for entering playable participation.

```text
PlayerTopology defines the rule set.
PlayerSlot defines who participates.
PlayerInput is Unity local operational evidence.
Actor defines what is played as.
PlayerEntry connects slot, input evidence, actor assignment, actor readiness, view binding and control permission.
```

This ADR defines boundaries and does not require immediate runtime implementation.

---

## 4. PlayerTopology

Initial vocabulary:

```text
SinglePlayer
LocalMultiplayer
Online
Hybrid
```

### 4.1 SinglePlayer

```text
One local player is expected.
A global camera and global UI may be valid.
PlayerInputManager is optional.
PlayerInput.camera may be optional if the game uses one global camera.
Only player.1 is expected unless explicitly declared otherwise.
```

### 4.2 LocalMultiplayer

```text
Multiple local players may exist on the same machine.
Each local player should have a PlayerSlot.
Each local player should have PlayerInput evidence.
PlayerInputManager is an allowed/preferred Unity mechanism for join/leave and split-screen, but not mandatory.
Per-player camera and UI requirements depend on selected view/UI policy.
```

### 4.3 Online

```text
Local and remote participants are different.
Only local player slots are expected to have local PlayerInput.
Remote actors may exist without PlayerInput, PlayerInput.camera or local UI input.
Network authority and replication are out of scope.
```

### 4.4 Hybrid

```text
Local multiplayer and online participation may coexist.
The framework must not assume every player slot is local.
Detailed network policy is deferred.
```

---

## 5. Validation Severity Depends on Topology

The same authoring shape can be valid or invalid depending on `PlayerTopology`.

| Case | SinglePlayer | LocalMultiplayer | Online |
|---|---:|---:|---:|
| No PlayerInputManager | OK | OK/Warning depending join policy | OK |
| PlayerInput.camera missing | OK/Warning | Error if split-screen/player camera policy requires it | OK for remote |
| PlayerInput.uiInputModule missing | OK | Error if per-player UI policy requires it | OK for remote |
| Remote actor without local PlayerInput | N/A | N/A | OK |
| Slot count exceeds local policy | Error | Error | Depends local/remote policy |
| Duplicate PlayerSlot | Error | Error | Error |

---

## 6. PlayerEntry Phases

Canonical phases:

```text
Configured
Joined
Assigned
Instantiated
ActorReady
ViewBound
Active
Suspended
Released
```

### Configured

A `PlayerSlot` or slot policy exists. No local player object is required yet.

### Joined

A local input participant is known through scene-authored `PlayerInput`, manual join, or `PlayerInputManager` evidence. This applies only to local players.

### Assigned

A `PlayerSlot` is assigned to an `Actor` or to pending actor selection.

Examples:

```text
player.1 -> firstgame.player
player.2 -> selected.character.b
player.1 -> pending character button selection
```

### Instantiated

The Actor has a runtime object or scene-authored object.

### ActorReady

The Actor completed enough `ActorInitialization` for the current policy.

Examples:

```text
ReadyForView: camera target/HUD source can bind.
ReadyForControl: control receiver may accept input if permission allows it.
```

`ActorReady` is the PlayerEntry observation of `ActorInitialization`. It is not a duplicate lifecycle.

### ViewBound

A player view/channel is connected to the slot: `PlayerInput.camera`, `PlayerInput.uiInputModule`, HUD bindings and/or camera target are coherent according to topology.

### Active

The PlayerView may participate as active player view and may win camera precedence. Control may also be released if control policy allows it.

### Suspended

The player exists but view/control must not win due to a suspension reason.

### Released

The entry is no longer active. References should no longer be treated as current player participation.

---

## 7. Suspension Reason

`Suspended` must carry diagnostic reason evidence.

Initial vocabulary:

```text
CinematicOverride
Transition
Loading
Pause
Respawn
ActorNotReady
ViewUnavailable
InputUnavailable
ControlBlocked
RouteExit
Manual
Unknown
```

Rules:

```text
Suspended without a reason is diagnostically invalid.
Suspension may block view, control, or both.
Suspended PlayerView must not win camera precedence unless an explicit policy says otherwise.
```

---

## 8. Assignment Authority

Canonical split:

```text
Unity supplies operational evidence: PlayerInput, InputUser, devices, callbacks.
Game/model supplies assignment intent: selected actor, character choice, gameplay rule.
Framework admits, validates, diagnoses and coordinates the assignment boundary.
```

The framework should not decide game-specific character selection by itself.

Example:

```text
Player 1 presses a character button.
Game/model creates assignment intent for Character A.
Framework validates player.1 availability, Actor identity, Actor readiness requirements, view requirements and control policy.
Unity PlayerInput supplies local evidence for player.1.
```

---

## 9. Deterministic Join Policy

Auto join is not the canonical framework rule.

Preferred direction:

```text
The framework/game uses deterministic assignment.
The game decides when participation is admitted.
The game decides which Actor is selected.
The framework validates and coordinates the entry boundary.
PlayerInputManager may provide PlayerInput evidence but does not decide gameplay assignment.
```

Allowed paths:

```text
Scene-authored single-player
Scene-authored local multiplayer
PlayerInputManager manual join
PlayerInputManager auto join, with framework validation after join
```

---

## 10. Character Selection Scenario

Character selection is the preferred future validation scenario for these boundaries.

Example:

```text
Player 1 selects Character A -> player.1 assigned to Actor A.
Player 2 selects Character B -> player.2 assigned to Actor B.
Actor A/B initialize skins, attributes, HUD data and camera targets.
PlayerView binds each local player to its camera/UI/target.
Control is released only after ActorReady + ViewBound + policy allowed.
```

This scenario proves:

```text
PlayerSlot != PlayerInput
PlayerSlot != Actor
PlayerInput != character choice
Actor identity != Actor readiness
PlayerView ownership != camera target
Control permission != movement implementation
```

---

## 11. Lifecycle Admission

Join/assignment may occur during Route/Activity lifecycle phases.

Risk cases:

```text
player joins during loading
player leaves during transition
player selects character during cinematic
actor respawns during Activity restart
controller disconnects during pause
```

Rule:

```text
Player entry changes must be admitted through lifecycle policy.
During transition/loading/cinematic, assignment may be deferred or admitted as Suspended, but must not produce silent partial wiring.
```

---

## 12. Save/Snapshot Boundary

Save/snapshot format is out of scope, but the boundary is reserved:

```text
PlayerSlot state is not Actor state.
Actor state is not PlayerView state.
PlayerView state is not necessarily progression state.
```

Examples:

```text
PlayerSlot: local profile, slot identity, possibly device preference.
Actor: selected character, position, attributes, health, inventory.
PlayerView: camera/HUD/UI/viewport state, often transient.
```

---

## 13. Consequences

Positive:

```text
The framework can validate player entry without owning Unity input mechanics.
Single-player remains simple.
Local multiplayer and online are not blocked by identity mistakes.
Character selection has a clean future path.
Camera/view/control readiness can be sequenced explicitly.
```

Tradeoffs:

```text
More states must be documented.
Validators need topology-aware severity.
A future coordinator may be needed for runtime orchestration.
```

---

## 14. Deferred

```text
Concrete PlayerEntryCoordinator implementation
Concrete PlayerEntryAssignmentRequest/Result C# API
Concrete ActorInitializer interface
Concrete PlayerViewDeclaration component
Concrete ControlBinding component
Save/snapshot schema
Online/network authority
```
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
