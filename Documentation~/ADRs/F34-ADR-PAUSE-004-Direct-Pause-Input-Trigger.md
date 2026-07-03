# F34-ADR-PAUSE-004 — Direct Pause Input Trigger for Consumer Keyboard Toggle

Status: Accepted / Amended by F35-ADR-PAUSE-005 and F36-ADR-PAUSE-006  
Phase: F34 / FIRSTGAME-2B — Pause Keyboard Toggle  
Type: Runtime / Authoring Boundary  
Last updated: 2026-07-03

## Context

The framework already has the logical Pause runtime, Pause surface adapters, Gate snapshot integration and the advanced PlayerInput/InputMode bridge path.

During FIRSTGAME-2, the consumer project validated the practical Pause model:

```text
PauseRequestTrigger
→ PauseRuntime
→ resident UIGlobal Pause surface
→ Gate blockers
```

During FIRSTGAME-2B, the first attempt to bind `Escape` through the advanced bridge path was too heavy for the minimal consumer use case. That path couples one authored key press to several concerns at once:

```text
Unity InputAction
→ Pause request
→ InputMode request
→ Unity PlayerInput action map application
→ Player/Actor/InputTarget evidence
```

That bridge remains useful for a later Player/InputMode integration cut, but it is not the correct default for a simple designer-authored Pause keyboard shortcut.

## Decision

Add and canonicalize a simpler component for direct Pause input:

```text
PauseInputActionTrigger
```

The direct trigger owns only this responsibility:

```text
Unity InputAction performed
→ FrameworkRuntimeHost.RequestPause(Pause / Resume / Toggle)
```

It does not own and must not perform:

```text
InputMode switching
Unity PlayerInput action-map switching
PlayerInputManager.JoinPlayer
Player/Actor spawning
UnityInputTarget validation
Gameplay command binding
Movement control
Time.timeScale policy in F34; amended by F35 for the basic simulation pause effect
```

## Canonical authoring model

For normal consumer Pause input, use:

```text
FG_UIGlobal
  PauseInput
    PlayerInput
    PauseInputActionTrigger
```

With an Input Actions asset containing a single global/system action:

```text
Global/Pause = <Keyboard>/escape
```

F36 supersedes the earlier `Player/Pause + UI/Pause` normal path. Same-frame dedupe may remain defensive, but it is no longer the expected Pause route.

## Runtime model

Successful keyboard Pause follows this route:

```text
Escape
→ PauseInputActionTrigger
→ FrameworkRuntimeHost.RequestPause(Toggle)
→ PauseRuntime
→ PauseSurfaceRuntime
→ UnityPauseResidentSurfaceAdapter
→ PauseGateBlockerPolicy snapshot
```

Expected results:

```text
Running → Paused
  gateBlockers = 2
  blocksInputAcceptance = True
  blocksInteractionAcceptance = True
  pauseSurfacePaused = True

Paused → Running
  gateBlockers = 0
  blocksInputAcceptance = False
  blocksInteractionAcceptance = False
  pauseSurfacePaused = False
```

## Relationship to the advanced bridge path

The following path remains valid but is no longer the default for a minimal keyboard Pause shortcut:

```text
PauseInputActionRuntimeBridgeTrigger
→ PauseInputModeUnityPlayerInputRuntimeBridge
```

Use the advanced bridge only when the cut explicitly validates typed `InputMode`, Unity `PlayerInput` action map application, player input ownership, or Player/Actor evidence.

For FIRSTGAME and normal designer setup, prefer:

```text
PauseInputActionTrigger
```

## Consequences

Accepted:

- Pause input has a small, readable authoring path.
- Game designers can bind `Escape` without configuring Player/Actor/InputTarget evidence.
- The Pause runtime stays the single owner of Pause state.
- The Pause surface remains resident in `UIGlobal`.
- Gate blockers continue to be produced by the Pause runtime result.
- Same-frame duplicate input is ignored when both `Player/Pause` and `UI/Pause` resolve from the same physical key press.

Rejected:

- Using `Input.GetKeyDown` or polling scripts as the canonical shortcut.
- Treating Pause as an Activity or Route.
- Making `Time.timeScale` the whole Pause contract. F35 later accepts it as the basic simulation effect, while Pause state remains owned by `PauseRuntime`.
- Requiring PlayerInputManager join/spawn for a global Pause shortcut.
- Hiding InputMode side effects inside a simple Pause trigger.

## Validation evidence

FIRSTGAME-2B passed with the following observable flow:

```text
Pause Input Action Trigger ready.
playerAction='Player/Pause'
uiAction='UI/Pause'
requestKind='Toggle'

Pause Request completed.
kind='Toggle'
source='PauseInputActionTrigger'
status='Applied'
previousState='Running'
currentState='Paused'
gateBlockers='2'
pauseSurfacePaused='True'

Pause Input Action Trigger ignored input.
reason='dedupe_same_frame'
action='UI/Pause'

Pause Request completed.
kind='Toggle'
source='PauseInputActionTrigger'
status='Applied'
previousState='Paused'
currentState='Running'
gateBlockers='0'
pauseSurfacePaused='False'
```

## Documentation

The canonical user guide must describe Pause through two layers:

1. UI-driven Pause using `PauseRequestTrigger` and `UnityPauseResidentSurfaceAdapter`.
2. Keyboard-driven Pause using `PauseInputActionTrigger`.

The guide should explicitly reserve the advanced bridge path for later InputMode/PlayerInput ownership cuts.


## F35 amendment

F35 keeps this ADR accepted for the simple input authoring path, but amends two boundaries:

- `PauseInputActionTrigger` may optionally switch the referenced `PlayerInput` between a gameplay action map and a Pause UI action map after a successful Pause request.
- `PauseRuntime` now applies a basic simulation pause effect by setting `Time.timeScale = 0` while paused and restoring the captured running value on resume.

The direct trigger remains the canonical FIRSTGAME path for `Escape -> Toggle Pause`. The advanced bridge path remains reserved for typed `InputMode` ownership work.


## F36 amendment

F36 keeps this ADR accepted for the simple direct Pause trigger, but amends the input track:

- The canonical action is now `Global/Pause`, not duplicate `Player/Pause` and `UI/Pause` actions.
- Same-frame dedupe is defensive only, not the normal success path.
- Optional PlayerInput map switching remains explicit and must not be used to keep UI buttons alive.
