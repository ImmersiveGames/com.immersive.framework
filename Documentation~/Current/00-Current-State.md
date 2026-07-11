# 00 — Current State

Status: **canonical after camera architecture reset decision**

For the active block and exact next cut, read [Execution Status](05-Execution-Status.md).

## Closed product baseline

| Area | Current state |
|---|---|
| Player product | `PlayerRecipe` and `PlayerComposer` are the designer-first Player surface. Validate and Apply/Rebuild materialize the canonical technical topology. |
| Player control | Explicit `PlayerInput`, validated gameplay action map and `UnityPlayerInputGateAdapter` are proven in QA and FIRSTGAME. Movement remains game-owned. |
| Player technical QA | P2D runtime baseline passed 13/13. P2E Gate/Pause/Transition block and restoration passed 14/14. |
| FIRSTGAME Player | P2G proved real Move input and real displacement through a game-owned movement component. |
| Camera presentation engine | Cinemachine remains mandatory. The framework must not implement its own Follow, LookAt, damping, framing or blending runtime. |
| Camera target source | `PlayerComposer` may expose explicit `CameraTarget` and `LookAtTarget`. Target-source evidence remains valid. |
| Camera runtime architecture | ADR-PROD-0006 is canonical. Camera selection must use typed requests and one scoped `CameraOutputContext` per output/viewport. |
| Existing C3–C8 camera implementation | Superseded pending destructive removal. It must not be treated as the supported final Camera Product Surface. |
| Route/Activity/Player camera coordination | Not yet implemented in the canonical request/output model. |
| Legacy camera | No compatibility layer is authorized. Director, Route/Activity bindings, PlayerView activation and raw Camera enable/disable selection must be removed. |

## Current authoring model

### Player

```text
PlayerRecipe (optional)
-> PlayerComposer
-> Validate
-> Apply/Rebuild
```

### Camera — target architecture

```text
CameraRigRecipe
-> CameraRigComposer
-> Validate
-> Apply/Rebuild Cinemachine rig

Route / Activity / Local Player
-> CameraRequest

CameraOutputContext
-> arbitrates requests for one output
-> applies winning rig through Cinemachine
```

`PlayerComposer` is not camera authority. It may provide explicit targets and may declare a default player-camera intent, but it does not own a Cinemachine Camera, select the active camera or mutate priority.

A rig Composer materializes presentation. A request owner declares intent. A `CameraOutputContext` selects the winner for one output.

## Accepted Player runtime boundary

```text
Framework:
  explicit PlayerInput reference
  validated gameplay action map
  Gate/Pause/Transition availability
  identity and diagnostics

Consumer:
  action interpretation
  movement execution
  tuning and gameplay rules
```

## Camera boundary

```text
Framework policy:
  explicit camera requests
  scoped output authority
  deterministic arbitration
  explicit release and restoration
  diagnostics

Cinemachine:
  Follow
  LookAt
  framing
  damping
  position/rotation algorithms
  blending
  Unity Camera presentation through CinemachineBrain
```

No Route, Activity, Player, Composer or adapter may independently become camera-selection authority.

## Known gaps

```text
destructive removal of superseded camera architecture = next implementation cut
camera request/output contracts = pending
single-output CameraOutputContext runtime = pending
Cinemachine winning-request application = pending
Route/Activity/Player request publishers = pending
QA arbitration/restoration proof = pending
FIRSTGAME manual camera integration = pending
progression save runtime = later
multiplayer gameplay systems = candidates
```

## Active block

```text
C9 — Camera Requests and Output Contexts
```

First completed cut:

```text
C9A — Camera architecture ADR and documentation reset
```

Next cut:

```text
C9B — Destructive removal of superseded camera architecture
```

See [Execution Status](05-Execution-Status.md), [Roadmap](01-Roadmap.md), [Usage Map](02-Usage-Map.md) and `ADR-PROD-0006`.
