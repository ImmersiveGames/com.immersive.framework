# F49-ADR-PLAYER-003 — Unity PlayerInput and PlayerInputManager Integration Boundary

Status: Proposed / Planning ADR  
Phase: F49 — Player Topology, Player Entry and PlayerView Ownership  
Type: Unity Input System / PlayerInput / PlayerInputManager / Integration Boundary  
Last updated: 2026-07-07

---

## 1. Context

Unity already provides execution components for local player input and join mechanics:

```text
PlayerInput
PlayerInputManager
InputUser
PlayerInput.camera
PlayerInput.uiInputModule
action maps
split-screen support
player joined/left events
```

The framework has identity primitives:

```text
PlayerSlotId
PlayerSlotDeclaration
PlayerSlotOccupancy
PlayerActorDeclaration
UnityPlayerInputGateAdapter
```

The framework must integrate with Unity components without replacing them.

---

## 2. Problem

Invalid directions:

```text
custom framework PlayerInputManager replacement
framework-owned device pairing
framework-owned split-screen layout
framework-owned UI event routing
framework-owned action map implementation
PlayerInput treated as PlayerSlot identity
PlayerInput treated as Actor identity
PlayerInputManager auto join treated as gameplay assignment
```

Unity `PlayerInput` does not provide framework identity by itself. It does not know ActorId, PlayerSlotId, occupancy, Route/Activity ownership, Reset subject identity, actor initialization state or character selection rules.

---

## 3. Decision

Unity owns input mechanics. The framework owns identity, ownership, validation and lifecycle-facing integration.

```text
PlayerInput is Unity operational evidence for a local player.
PlayerInputManager is an optional Unity source of PlayerInput join/leave events.
PlayerSlot is the stable framework identity of a player seat.
Actor is the stable framework identity of the controlled entity.
Game/model supplies assignment intent.
Framework validates and coordinates the entry boundary.
```

---

## 4. Ownership Boundary

| Concern | Owner |
|---|---|
| Device pairing | Unity Input System / InputUser |
| Action maps | Unity PlayerInput / Input Actions |
| Activate/deactivate input | Unity PlayerInput, optionally through framework gates |
| Join detection | Unity PlayerInputManager or deterministic game policy |
| Split-screen viewport layout | Unity PlayerInputManager |
| Player camera reference | PlayerInput.camera |
| Player UI input reference | PlayerInput.uiInputModule |
| Player identity | Framework PlayerSlot |
| Actor identity | Framework Actor |
| Player-to-Actor relation | Framework PlayerSlotOccupancy |
| Assignment intent | Game/model |
| Assignment admission/validation | Framework |
| Reset subject | Framework Reset via Actor identity |
| Route/Activity camera flow | Framework Camera |
| Movement behavior | Game/model-specific code |

---

## 5. PlayerInput

`PlayerInput` is local Unity operational evidence.

It may provide:

```text
InputUser
devices
control scheme
action map
action callbacks
camera reference
uiInputModule reference
player index / split-screen evidence
```

It must not be used as:

```text
PlayerSlotId
ActorId
save identity
network identity
actor ownership authority
character selection authority
```

The framework may enrich diagnostics with `PlayerInput` data. These are diagnostics/evidence, not canonical identity.

---

## 6. PlayerInput Volatility

`PlayerInput` is operational and may be created, destroyed, disabled, re-enabled or rebound.

Rules:

```text
PlayerInput rebinding must not imply PlayerSlotId change.
PlayerInput destruction must not silently destroy stable PlayerSlot identity.
Controller disconnect may suspend PlayerEntry without invalidating Actor identity.
```

---

## 7. PlayerInputManager

`PlayerInputManager` is optional.

Supported modes:

```text
NoPlayerInputManager
SceneAuthoredPlayerInput
PlayerInputManagerManualJoin
PlayerInputManagerAutoJoin
```

### NoPlayerInputManager

Valid for single-player scene-authored player, online local player controlled by custom bootstrap, tests and QA.

### SceneAuthoredPlayerInput

Valid for single-player, controlled QA scenes and fixed local co-op scenes.

### PlayerInputManagerManualJoin

Preferred when the game/framework needs deterministic admission and character assignment.

### PlayerInputManagerAutoJoin

Allowed but not canonical. If used, framework assignment must still validate slot availability, max local player count, actor assignment policy, view requirements and UI/camera requirements.

---

## 8. Deterministic Assignment

Preferred model:

```text
PlayerInput / PlayerInputManager evidence
-> PlayerJoin admission policy
-> PlayerSlot assignment
-> Actor selection intent from game/model
-> Actor initialization
-> PlayerView binding
-> Control permission release
```

Unity auto-join must not silently decide framework game rules.

---

## 9. Runtime Assignment API Boundary

Current `Declaration` components are authoring-facing.

Future runtime assignment should not simply expose test-only diagnostic configuration methods as public gameplay APIs.

Preferred conceptual shape:

### PlayerEntryAssignmentRequest

```text
requestedSlot
assignmentSource / reason
playerTopology
localPlayerInput evidence, optional
inputUser evidence, optional
selectedActor candidate, optional
selectedActorPrefab/content reference, optional
characterSelection context, optional
requestedView policy, optional
requestedControl policy, optional
```

### PlayerEntryAssignmentResult

```text
status
assignedSlot
assignedActor
actorInstantiationStatus
actorReadinessStatus
viewBindingStatus
controlBindingStatus
suspensionReason, optional
issues
blockingIssueCount
```

Guardrail:

```text
Do not turn ConfigureForDiagnostics into the canonical runtime API without a separate decision.
```

---

## 10. Max Player Count

If `PlayerInputManager` is used, its maximum player count must align with framework topology policy.

Examples:

```text
SinglePlayer -> max local players 1
LocalMultiplayer 4-player game -> max local players 4
Online one-local-player game -> max local players 1, remote players are not local PlayerInputs
```

---

## 11. PlayerInput.camera and uiInputModule

Rules:

```text
SinglePlayer may leave PlayerInput.camera empty if global camera policy is used.
LocalMultiplayer with split-screen should require PlayerInput.camera.
Online remote participants should not require PlayerInput.camera.
```

```text
SinglePlayer may use global UI.
LocalMultiplayer with per-player UI should require uiInputModule / MultiplayerEventSystem policy.
Online remote participants should not require local UI input.
```

---

## 12. Input Actions and Movement Scripts

The framework should not own game movement.

Game scripts may use:

```text
PlayerInput action callbacks
PlayerInput currentActionMap
Generated C# class from Input Actions asset
Custom movement/controller model
```

Guardrails:

```text
Avoid stringly-typed action lookup in production samples when a generated Input Actions class is available.
In PlayerInput workflows, respect the PlayerInput instance and its private action copy where applicable.
Framework input gates may block action maps, but do not define movement behavior.
```

---

## 13. Join/Leave During Lifecycle

Player join/leave may occur during transition, loading, pause, activity restart or cinematic.

Rule:

```text
PlayerInputManager events must be admitted through lifecycle policy.
During transition/loading/cinematic, assignment may be deferred or admitted as Suspended, but must not produce silent partial wiring.
```

---

## 14. Diagnostics

Useful future diagnostics:

```text
PlayerSlotId
ActorId
PlayerInput object name
InputUser id/evidence
current control scheme
devices
PlayerInput.camera
PlayerInput.uiInputModule
split-screen index
assignment source
suspension reason
```

These are evidence, not identity.

---

## 15. Consequences

Positive:

```text
The framework does not duplicate Unity Input System.
Unity split-screen remains available.
Deterministic assignment remains possible.
Scene-authored single-player remains simple.
Online/remote actors are not forced to have PlayerInput.
```

Tradeoffs:

```text
A future assignment API is needed for runtime join.
Validators need topology and join-policy awareness.
Auto join must be guarded if used.
```

---

## 16. Deferred

```text
Concrete PlayerInputManager bridge
Concrete PlayerEntryAssignmentRequest/Result C# API
Concrete runtime assignment component/service
Generated Input Actions integration in FIRSTGAME
Per-player UI setup
Split-screen QA scenario
```
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
