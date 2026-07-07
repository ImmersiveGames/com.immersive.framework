# F49 ADR Set — Player Topology, Player Entry and PlayerView Ownership

Status: Draft set / ready for review  
Phase: F49  
Last updated: 2026-07-07

## Included ADRs

0. `F49-ADR-000-Player-Topology-Entry-and-View-Ownership-Overview.md`
1. `F49-ADR-PLAYER-001-Player-Topology-and-Player-Entry-Boundary.md`
2. `F49-ADR-PLAYER-002-PlayerView-Ownership-and-Camera-Precedence.md`
3. `F49-ADR-PLAYER-003-Unity-PlayerInput-and-PlayerInputManager-Integration-Boundary.md`
4. `F49-ADR-ACTOR-002-Actor-Initialization-and-Control-Binding-Boundary.md`

## Canonical summary

```text
PlayerTopology defines the rule set.
PlayerSlot is the stable logical identity of a participant/player seat.
PlayerInput is local Unity operational evidence, not identity.
Actor is what the slot currently plays as or controls.
ActorInitialization prepares an Actor instance to be used.
PlayerEntry turns slot + input evidence + actor assignment + actor readiness + view binding + control permission into a playable participant.
PlayerView belongs to PlayerSlot.
PlayerInput.camera is the Unity player viewport camera.
Route/Activity Camera remains the flow/context/cinematic fallback.
Movement remains game/model-owned.
```

## Frozen decisions

```text
Unity Input System owns input execution, device pairing, PlayerInput, PlayerInputManager, UI input modules and split-screen mechanics.
Immersive Framework owns player identity, actor identity, occupancy, view ownership semantics, validation, diagnostics and lifecycle-facing orchestration.
The game/model owns concrete movement, actor behavior, assignment intent and character-specific initialization content.
PlayerInput is evidence, not identity.
PlayerInputManager is optional integration, not a mandatory framework dependency.
Auto join is allowed only as an optional Unity path; deterministic assignment is the canonical framework/game policy.
PlayerSlot is stable logical identity.
Actor identity is separate from Actor initialization/readiness.
ActorInitialization must expose readiness evidence consumable by PlayerEntry, PlayerView and ControlBinding.
PlayerView camera wins only when Bound + Active.
Activity/Route cameras remain valid fallback/context/cinematic cameras.
Explicit cinematic override may beat PlayerView camera.
Remote/online actors are not required to have local PlayerInput.
Validation severity depends on PlayerTopology.
No canonical runtime setup should depend on GameObject name search.
Save/snapshot format is deferred, but Slot/Actor/View state boundaries are reserved.
```

## Deferred implementation decisions

```text
PlayerEntryCoordinator concrete component/service
PlayerView runtime registry
CameraDirector-per-player / CameraDirectorRegistry
Cinemachine channel assignment per PlayerSlot
MultiplayerEventSystem setup policy
Generated Input Actions class integration
Movement/control capability implementation
Save/snapshot schema
Online ownership/network authority
```
