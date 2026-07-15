> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# F49-ADR-PLAYER-002 — PlayerView Ownership and Camera Precedence

Status: Proposed / Planning ADR  
Phase: F49 — Player Topology, Player Entry and PlayerView Ownership  
Type: PlayerView / Camera / UI / AudioListener Boundary  
Last updated: 2026-07-07

---

## 1. Context

The framework currently has Route/Activity camera support through `FrameworkCameraDirector`, `FrameworkRouteCameraBinding`, `FrameworkActivityCameraBinding` and optional Cinemachine integration.

This is valuable for:

```text
menu camera
gameplay route camera
activity intro camera
activity-specific camera policy
retained activity camera until route exit
route fallback camera
```

The framework also now has PlayerSlot and PlayerActor identity. However, Route/Activity camera and player-owned camera are different concepts.

---

## 2. Problem

Invalid assumptions:

```text
The active Route/Activity camera is always the player's camera.
PlayerInput.camera can replace all Route/Activity camera rules.
The Actor owns the camera.
A Transform target is a stable camera identity.
A configured PlayerSlot means the player camera should win.
An occupied Actor means the player camera should win.
```

These fail with activity intro cameras, character selection, player spawn delay, actor initialization delay, respawn, cinematic override, local multiplayer, remote players and possession/body swap.

---

## 3. Decision

Introduce `PlayerView` as the ownership concept for local player view.

```text
PlayerView belongs to PlayerSlot.
PlayerInput.camera is the Unity player viewport camera reference.
PlayerInput.uiInputModule is the Unity player-local UI input reference.
FrameworkCameraDirector remains the Route/Activity flow camera system.
Actor provides target/readiness, not view ownership.
```

This ADR defines boundary and precedence. It does not require immediate runtime implementation.

---

## 4. PlayerView Definition

Conceptual fields:

```text
ownerSlot
local PlayerInput evidence, optional depending topology
playerCamera / PlayerInput.camera
uiInputModule / PlayerInput.uiInputModule, optional depending UI policy
HUD binding, optional
camera target source
view state
suspension reason, optional
```

`PlayerView` is not the same as Actor and not the same as Route/Activity camera.

---

## 5. PlayerView State

Suggested vocabulary:

```text
Missing
Declared
Bound
Active
Suspended
Released
```

Rules:

```text
Missing/Declared PlayerView must not win camera precedence.
Bound PlayerView has coherent references but may not yet win.
Active PlayerView may win camera precedence.
Suspended PlayerView must not win camera precedence unless an explicit policy says otherwise.
Released PlayerView must not be used as current ownership.
```

---

## 6. Camera Precedence

Default precedence:

```text
Explicit Cinematic Override
> Active PlayerView Camera
> Activity Camera
> Route Camera
> Default Camera
```

### PlayerView Wins by Default

If a PlayerView is Bound + Active, it may win over Activity/Route camera.

### Activity/Route Camera Remains Necessary

Activity/Route camera covers valid cases:

```text
menu route
route intro
activity intro
room presentation
cutscene
boss reveal
character selection before spawn
player death/respawn
player object not yet instantiated
actor not yet initialized
loading/transition coverage
```

### Cinematic Override

A cinematic override may beat PlayerView, but it must be explicit.

Examples:

```text
boss intro
dialogue lock
death camera
killcam
tutorial camera
forced route camera
activity-authored camera sequence
```

---

## 7. Relationship to PlayerEntry

Camera precedence depends on PlayerEntry state.

```text
Configured/Joined/Assigned/Instantiated -> Activity/Route/default camera may cover.
ActorReady -> PlayerView may bind if ReadyForView evidence exists.
ViewBound -> PlayerView is coherent but may still be blocked.
Active -> PlayerView camera may win.
Suspended -> PlayerView camera must not win unless explicit policy allows it.
Released -> PlayerView must not win.
```

---

## 8. Relationship to PlayerInput.camera

Unity `PlayerInput.camera` is the official Unity reference for a player's camera/viewport.

```text
PlayerInput.camera is the Unity player viewport camera.
FrameworkCameraDirector is the Route/Activity flow camera system.
```

In `SinglePlayer`, both may refer to the same camera. In `LocalMultiplayer`, each local player may have a distinct `PlayerInput.camera`, and Unity `PlayerInputManager` may manage split-screen viewports.

The framework validates ownership. It does not reimplement Unity split-screen mechanics.

---

## 9. Relationship to PlayerInput.uiInputModule

Unity `PlayerInput.uiInputModule` is the preferred player-local UI input bridge when a player has local UI.

```text
SinglePlayer may use global UI.
LocalMultiplayer with per-player UI should require uiInputModule / MultiplayerEventSystem policy.
Online remote participants should not require local UI input.
```

UI routing implementation remains Unity/UI-system owned. The framework validates ownership and coherence.

---

## 10. AudioListener Policy

AudioListener does not belong to Actor identity.

```text
AudioListener belongs to a PlayerView/local-view policy or to a global single-player policy.
```

Initial policy should remain conservative:

```text
SinglePlayer: one active AudioListener.
LocalMultiplayer: explicit policy required before multiple-listener behavior is supported.
Online: only local player/spectator view should own listener policy.
```

Rules:

```text
The framework must not silently enable multiple AudioListeners.
PlayerView may own or reference AudioListener policy.
Actor ownership must not imply AudioListener ownership.
```

---

## 11. Camera Target Resolution

Canonical relation:

```text
PlayerView owner: PlayerSlot
PlayerView target: occupied Actor or Actor CameraTarget capability
Final Unity endpoint: Transform
```

`Transform` is acceptable as the final endpoint, but not as the canonical identity.

Runtime setup must move away from GameObject name search.

---

## 12. Actor Occupancy vs View Ownership

Changing the occupied Actor changes target, not PlayerView owner.

Example:

```text
player.1 owns PlayerView.
player.1 occupies firstgame.player.
Camera targets firstgame.player.
player.1 enters ship.
player.1 now occupies firstgame.ship.
PlayerView owner remains player.1.
Camera target changes to firstgame.ship.
```

---

## 13. Relationship to FrameworkCameraDirector

The current `FrameworkCameraDirector` remains valid as Route/Activity flow camera.

It owns:

```text
Route camera selection
Activity camera selection
Activity camera policy
Default/fallback camera
Optional Cinemachine rig application
```

It does not own:

```text
PlayerSlot identity
PlayerInput.camera identity
split-screen viewport layout
per-player UI input routing
character selection
Actor readiness
```

Future local multiplayer may require per-player adapters or a registry, but this ADR does not require a multi-director implementation now.

---

## 14. Validation Examples

### SinglePlayer

```text
PlayerInput.camera missing while global camera exists -> OK/Warning depending policy.
Active PlayerView camera same as Route/Activity camera -> OK.
Only one active AudioListener -> OK.
```

### LocalMultiplayer

```text
PlayerInput.camera missing while split-screen required -> Error.
Two PlayerViews share the same player camera unexpectedly -> Error/Warning depending policy.
Multiple AudioListeners active without explicit policy -> Error.
Per-player UI policy enabled but uiInputModule missing -> Error.
```

### Online

```text
Remote actor without PlayerInput.camera -> OK.
Local player without required PlayerView -> Error/Warning depending policy.
Remote actor with local UI input unexpectedly -> Warning/Error depending policy.
```

---

## 15. Consequences

Positive:

```text
Single-player can use existing Route/Activity camera cleanly.
Activity intro/cinematic cameras remain valid.
Player camera can take precedence only when safe.
Local multiplayer remains compatible with Unity PlayerInputManager.
View ownership survives Actor swaps.
```

Tradeoffs:

```text
PlayerView state must be validated.
Camera diagnostics need topology awareness.
Future local multiplayer may need additional adapters.
```

---

## 16. Deferred

```text
Concrete PlayerViewDeclaration component
Camera target capability contract
PlayerView registry
CameraDirector-per-player or CameraDirectorRegistry
Cinemachine channel assignment per PlayerSlot
Concrete AudioListener policy implementation
Concrete per-player HUD/UI policy implementation
```
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
