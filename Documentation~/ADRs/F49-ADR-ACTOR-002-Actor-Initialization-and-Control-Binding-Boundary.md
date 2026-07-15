# F49-ADR-ACTOR-002 — Actor Initialization and Control Binding Boundary

Status: Proposed / Planning ADR  
Phase: F49 — Player Topology, Player Entry and PlayerView Ownership  
Type: Actor Initialization / Control Binding / Movement Boundary  
Last updated: 2026-07-07

---

## 1. Context

The framework now separates Actor identity, PlayerSlot identity, PlayerSlot occupancy, PlayerInput evidence, Reset subject identity, Input Gate behavior and Route/Activity camera behavior.

FIRSTGAME currently has game-specific movement in `FirstGamePlayerMover` and game-specific reset state in `FirstGamePlayerResettableState`. This is acceptable. The framework should not become a gameplay movement system.

Future player entry flows need a clear place for internal Actor initialization:

```text
skin setup
attributes
HUD binding
camera target setup
movement/controller binding
input receiver setup
reset/snapshot participant setup
```

---

## 2. Problem

Actor identity alone does not mean the runtime object is ready.

Invalid assumptions:

```text
ActorDeclaration exists -> Actor is initialized.
PlayerSlotOccupancy exists -> Actor can move.
PlayerInput exists -> Actor can receive movement.
PlayerView exists -> HUD is bound.
Reset participant exists -> full actor state is ready.
Skin/presentation exists -> gameplay attributes are ready.
```

These assumptions break with character selection, skin variants, runtime attributes, loadout, HUD binding, spawn/respawn, vehicle possession, drone/body swap, delayed Activity activation, cinematic before control and save/snapshot restoration.

---

## 3. Decision

Define `ActorInitialization` as a separate lifecycle concept and `ControlBinding` as a separate ownership/binding concept.

```text
Actor identity declares who the actor is.
Actor initialization prepares the actor to be usable.
Control binding connects a PlayerSlot/Input source to the occupied Actor capability.
Movement remains game/model-owned.
```

This ADR defines the boundary. It does not implement runtime initialization.

---

## 4. Actor Initialization

`ActorInitialization` prepares an Actor instance after identity is known.

Potential responsibilities:

```text
skin / presentation selection
attribute initialization
loadout initialization
HUD data source binding
portrait/icon/name binding
camera target/anchor binding
movement component binding
input receiver binding
reset participant registration readiness
future snapshot participant readiness
```

Actor initialization may be triggered by scene-authored object activation, prefab instantiation, character selection, route/activity entry, respawn, load game, actor possession or runtime content materialization.

---

## 5. Actor Readiness

Possible readiness vocabulary:

```text
Declared
Instantiated
Initializing
ReadyForView
ReadyForControl
Suspended
Failed
Released
```

`ReadyForView` means the Actor can serve as camera target/HUD source.

`ReadyForControl` means the Actor can receive player control according to policy.

Readiness evidence must eventually be consultable by:

```text
PlayerEntry
PlayerView
ControlBinding
Camera target binding
HUD binding
```

Concrete API shape is deferred.

---

## 6. Relationship to PlayerEntry.ActorReady

`PlayerEntry.ActorReady` is not a duplicate Actor lifecycle. It is the PlayerEntry-level observation that the occupied Actor has completed enough initialization for the current entry policy.

Examples:

```text
Camera intro only -> Actor may need ReadyForView, not ReadyForControl.
Control release -> Actor must satisfy ReadyForControl and control permission.
HUD bind -> Actor must expose required HUD/status data.
```

---

## 7. Control Binding

`ControlBinding` connects a PlayerSlot or local input evidence to an occupied Actor control capability.

Canonical relation:

```text
PlayerSlot
-> PlayerSlotOccupancy
-> Actor
-> ControlReceiver / MovementCapability / game-specific controller
```

The framework may eventually validate:

```text
A local active PlayerSlot has input evidence.
The occupied Actor exposes a compatible control receiver.
Control is not allowed during transition/loading/pause/cinematic unless policy allows it.
Control permission is not movement implementation.
```

The framework should not define how the Actor moves.

---

## 8. Movement Remains Game/Model-Owned

Movement examples:

```text
FIRSTGAME player mover
physics character controller
grid movement
vehicle driving
flying drone
turn-based command
click-to-move
tactical selection cursor
```

Decision:

```text
The framework may expose permissions, binding diagnostics and lifecycle gates.
The game/model implements concrete movement.
```

A future optional package or sample may provide movement examples, but framework core must not depend on one movement model.

---

## 9. Relationship to PlayerInput

Correct chain:

```text
PlayerInput evidence
-> PlayerSlot
-> PlayerSlotOccupancy
-> Actor
-> Actor initialization/readiness
-> ControlBinding accepted
-> Control permission allowed
-> Game-specific movement executes
```

Incorrect chain:

```text
PlayerInput -> movement always allowed
```

---

## 10. Relationship to PlayerView

PlayerView activation may depend on Actor readiness.

Examples:

```text
Actor initialized enough for view target -> PlayerView may bind camera target.
Actor not initialized -> Activity camera remains active.
Actor ready for view but not control -> camera can follow while control is still blocked.
Actor ready for control -> input/movement may be released.
```

---

## 11. Relationship to HUD

HUD binding belongs to view/Actor initialization boundary, not Actor identity.

Potential relation:

```text
PlayerView HUD
-> occupied Actor status source
-> attributes/health/loadout
```

HUD implementation is out of scope.

---

## 12. Relationship to Reset and Snapshot

Reset participants are separate from Actor identity. Future snapshot/save must follow the same boundary.

```text
Actor identity is not save state.
Actor initialization may create/reset/load state participants.
Snapshot participant contributes serializable state.
```

Save/snapshot format is deferred until PlayerEntry and Actor readiness are clearer.

---

## 13. Actor Swaps and Possession

Actor occupancy may change while PlayerView ownership remains stable.

Example:

```text
player.1 occupies firstgame.player.
player.1 view targets firstgame.player.
player.1 enters vehicle.
player.1 occupies firstgame.vehicle.
PlayerView owner remains player.1.
ControlBinding and camera target must re-resolve to the occupied Actor.
```

---

## 14. Validation Examples

Blocking examples:

```text
PlayerEntry requests control but occupied Actor is not ReadyForControl.
PlayerView requests camera target but occupied Actor is not ReadyForView.
ControlBinding targets an Actor different from PlayerSlotOccupancy without explicit policy.
```

Non-blocking or topology-dependent examples:

```text
Actor ReadyForView but not ReadyForControl during intro camera.
Remote Actor without local control receiver in Online topology.
Actor initialized for HUD but movement component missing in non-controllable NPC context.
```

---

## 15. Consequences

Positive:

```text
Actor identity stays small and stable.
Actor readiness becomes explicit.
Character selection, respawn and possession have room to evolve.
Movement remains flexible per game.
PlayerView and control can wait for the correct readiness gate.
```

Tradeoffs:

```text
A future readiness API is needed.
Validators need to reason about readiness separately from identity.
Game-specific initialization must be integrated explicitly.
```

---

## 16. Deferred

```text
Concrete ActorInitializer interface/component
Concrete readiness result type
Concrete ControlBinding component
Concrete movement/control adapter
Concrete HUD binding contract
Save/snapshot state schema
```
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
