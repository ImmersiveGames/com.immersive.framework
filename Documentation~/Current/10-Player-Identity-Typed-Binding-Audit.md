# F53C0 - Player Identity, Typed Binding and Adapter Chain Audit

Status: Documented
Date: 2026-07-09
Scope: `com.immersive.framework`, QA Harness player proofs, FIRSTGAME real `PlayerPrototype`

## Summary

F53C0 audits the real player identity and binding chain before new adapters or an authoring facade are created. No runtime, contract, scene, prefab or asset change is part of this cut.

Current decision:

- `PlayerSlotDeclaration` is the canonical source for player slot identity.
- `PlayerActorDeclaration` is the canonical source for player actor identity.
- `UnityResetSubjectAdapter.sourcePlayerActor` is the correct reset identity source for the real player.
- `PlayerInput` must be a direct typed reference where it is used.
- `GameObject.name` is diagnostic only and must not be the primary object/component locator.
- Unity Input System action map/action names may remain strings only when validated against an `InputActionAsset`.
- IDs are stable identity/evidence/persistence text, not object lookup.

## Evidence

Audited files:

- `Runtime/PlayerSlots/PlayerSlotDeclaration.cs`
- `Runtime/Actors/PlayerActorDeclaration.cs`
- `Runtime/Reset/Unity/UnityResetSubjectAdapter.cs`
- `Runtime/UnityInput/UnityPlayerInputGateAdapter.cs`
- `Runtime/Pause/PauseInputActionTrigger.cs`
- `Runtime/PlayerBinding/*TargetBehaviour.cs`
- `Assets/ImmersiveFrameworkQA/Player/**`
- `Assets/_Project/Scenes/Gameplay/FG_Gameplay.unity`
- `Assets/_Project/Scenes/Menu/FG_UIGlobal.unity`
- `Assets/_Project/Scripts/Editor/FrameworkIntegration/FirstGameRealPlayerBindingValidator.cs`
- `Assets/_Project/Scripts/Editor/GameCamera/FirstGameCameraCutSetup.cs`
- `Assets/_Project/Scripts/Runtime/Player/FirstGamePlayerMover.cs`

## Main Matrix

| Area | Current source | Uses string? | Should be type? | Risk | Recommended action |
| --- | --- | --- | --- | --- | --- |
| Player object | FIRSTGAME object named `PlayerPrototype` | Yes | Yes | Fragile if renamed | Resolve by canonical component, not object name |
| Player slot | `PlayerSlotDeclaration.slotId = player.1` | ID | Partial | Good identity, weak as bind text | Keep ID; bind through `PlayerSlotDeclaration` reference |
| Actor id | `PlayerActorDeclaration.actorId = firstgame.player` | ID | Partial | Good reset/save identity | Keep as canonical actor identity; bind through declaration |
| PlayerInput | `PlayerInput` component reference | No | Yes | OK | Keep direct typed references |
| Object entry | `ObjectEntryDeclaration.objectEntryId = firstgame.player` | ID | No for lookup | Can be confused with actor id | Keep as object entry evidence, not actor lookup |
| Reset subject | `UnityResetSubjectAdapter.sourcePlayerActor` | No primary string | Yes | OK | Keep `sourcePlayerActor`; leave `subjectId` empty for real player |
| Reset subject id | resolved `Actor:firstgame.player` | ID | No for lookup | OK as evidence/persistence | Source must remain `PlayerActorDeclaration` |
| PlayerInput gate | `UnityPlayerInputGateAdapter.playerInput` + `sourceSlot` | Action map string only | Yes | OK if action map exists | Keep direct `PlayerInput`; validate `Player` map |
| Gate slot evidence | `sourceSlot = PlayerSlotDeclaration` | No primary string | Yes | OK | Keep as diagnostic identity bridge |
| Control target | `PlayerControlBindingTargetBehaviour` | Display string only | Yes | OK | Keep as explicit target; display name is diagnostic |
| PlayerInput bridge target | `UnityPlayerInputBridgeTargetBehaviour.playerInput` + `expectedPlayerSlotId` | Slot ID string | Prefer declaration reference later | Slot duplication can drift | Future facade should source slot from `PlayerSlotDeclaration` |
| PlayerInput activation target | `UnityPlayerInputActivationTargetBehaviour.playerInput` + `actionMapName` | Slot ID and action map string | Partial | Slot/action map can drift from gate | Validate action map; consolidate config before facade |
| Action map `Player` | `PlayerInput.defaultActionMap`, gate, activation, mover | Yes | Tolerated | Drift if duplicated | Keep only when validated against `InputActionAsset`; reduce duplication later |
| Pause action | `PauseInputActionTrigger` uses `Global/Pause` | Yes | Tolerated | Misconfig if asset changes | Already validates map/action against asset |
| Player view target | F51 targets exist in package/QA, not FIRSTGAME | No FIRSTGAME proof | Yes | FIRSTGAME camera is not proven through PlayerView chain | Add real FIRSTGAME proof before facade |
| Camera target | `FrameworkCameraAnchorHost.trackingTarget/lookAtTarget` -> player `Transform` | No in scene | Yes | OK in scene, setup tool finds by name | Keep typed scene refs; remove name lookup from setup |
| Camera activation | F51 explicit `Camera` target exists in package/QA, not FIRSTGAME | No FIRSTGAME proof | Yes | No real proof with player camera chain | Prove only after identity canonicalization |
| Editor validator | `FindObjectsByType<PlayerInput>` then `input.name == PlayerPrototype` | Yes | Yes | Can validate wrong object | Resolve by `PlayerActorDeclaration`/`PlayerSlotDeclaration` |
| Camera setup tool | `FindInScene(scene, "PlayerPrototype")` | Yes | Yes | Renaming breaks target assignment | Resolve by player declaration or explicit selection |
| QA synthetic scenes | scene builders set `player.1`, `Player`, object names | Yes | Mostly no | Acceptable in QA fixtures | Keep as fixture setup; do not copy to FIRSTGAME as contracts |

## String Classification

Allowed: log, reason, display and evidence text.

- `displayName`, `diagnosticTag`, `reason`, target names and log fields.
- `GameObject.name` only for diagnostics.

Tolerated: Unity Input System names, with validation.

- `Player`, `Global`, `UI`, `Pause`, `Move`.
- Acceptable because `InputActionAsset.FindActionMap` / `FindAction` validates them before use.

Wrong for future binding: object/component location by name.

- `FirstGameRealPlayerBindingValidator` prefers `PlayerInput.name == "PlayerPrototype"` after selection fallback.
- `FirstGameCameraCutSetup` assigns camera anchors by `FindInScene(scene, "PlayerPrototype")`.
- These are editor-only today, but they are the exact pattern the facade must not preserve.

Risky duplication: identity/action config repeated in several components.

- `expectedPlayerSlotId = player.1` appears on bridge and activation targets while `PlayerSlotDeclaration` already owns the slot.
- `actionMapName/gameplayActionMapName = Player` appears on mover, gate and activation target.
- This is not a hard runtime bug because current components validate before use, but it is facade input drift.

## Canonical Player Identity

| Question | Answer |
| --- | --- |
| Which component declares the slot? | `PlayerSlotDeclaration` |
| Which component declares the actor id? | `PlayerActorDeclaration` |
| Which component should source reset identity? | `UnityResetSubjectAdapter.sourcePlayerActor -> PlayerActorDeclaration` |
| Which component should feed future save/progression? | `PlayerActorDeclaration` for actor identity; `PlayerSlotDeclaration` only for player seat |
| Which data is visual/human? | `GameObject.name`, `displayName`, target names |
| Which data is stable identity? | `PlayerSlotId`, `ActorId`, `ResetSubjectId`, `ObjectEntryId` in their own domains |
| Which data is runtime reference? | `PlayerInput`, `Transform`, `Camera`, declaration component references |

Do not compare identities from different domains. `player.1`, `firstgame.player`, `Actor:firstgame.player` and `ObjectEntry:firstgame.player` are related evidence, not interchangeable keys.

## PlayerView Chain

Package/QA already has F51 contracts:

- `PlayerViewBindingTargetBehaviour`
- `PlayerViewCameraTargetBindingTargetBehaviour`
- `PlayerViewCameraActivationTargetBehaviour`

FIRSTGAME real player does not currently have those F51 target components. The real camera path is:

```text
FirstGameCameraAnchors
  FrameworkCameraAnchorHost
    trackingTarget = PlayerPrototype.Transform
    lookAtTarget = PlayerPrototype.Transform
```

This is a typed scene reference and does not depend on name at runtime. The editor setup tool that creates/repairs this wiring still depends on names.

Recommended before facade:

1. Prove `PlayerView` target binding on the real `PlayerPrototype`.
2. Decide whether the camera target is the player root `Transform` or a child anchor.
3. Avoid duplicating a generic view target and camera target unless they express different user intent.

## PlayerControl / Input Chain

Current FIRSTGAME real chain:

```text
PlayerSlotDeclaration.slotId = player.1
PlayerInput = direct reference
PlayerControlBindingTargetBehaviour
UnityPlayerInputBridgeTargetBehaviour.playerInput = direct reference
UnityPlayerInputActivationTargetBehaviour.playerInput = direct reference
UnityPlayerInputGateAdapter.playerInput = direct reference
UnityPlayerInputGateAdapter.sourceSlot = PlayerSlotDeclaration
```

The slot used by the gate comes from `PlayerSlotDeclaration`. The F52 bridge/activation targets still hold `expectedPlayerSlotId` as serialized ID text. That is acceptable as guard evidence for the current cut, but the next authoring layer should derive it from the declaration reference.

`UnityPlayerInputGateAdapter.gameplayActionMapName` and `UnityPlayerInputActivationTargetBehaviour.actionMapName` can diverge because they are separate fields. The current scene sets both to `Player`. Future facade work should make this one authored source and validate it against the `InputActionAsset`.

## Reset Subject / Player Identity

FIRSTGAME real scene:

```text
UnityResetSubjectAdapter
  idGeneration = AuthoredStableId
  subjectId = empty
  sourcePlayerActor = PlayerActorDeclaration
  scope = Activity
```

`UnityResetSubjectAdapter` resolves the subject id from `PlayerActorDeclaration.ActorId`, producing `Actor:firstgame.player`. This is correct: the ID is evidence/persistence text, while the source is a typed component reference.

`scope = Activity` is correct for the current FIRSTGAME gameplay proof because the player participates in Activity reset/restart. If the player becomes persistent across routes or save/progression later, scope must be revisited before lifecycle automation.

## PlayerPrototype Ergonomics

Keep canonical now:

- `PlayerInput`
- `FirstGamePlayerMover`
- `PlayerSlotDeclaration`
- `PlayerActorDeclaration`
- `PlayerSlotOccupancy`
- `UnityPlayerInputGateAdapter`
- `UnityResetSubjectAdapter`
- `UnityTransformResetParticipant`
- `FirstGamePlayerResettableState`

Keep as current proof targets:

- `PlayerControlBindingTargetBehaviour`
- `UnityPlayerInputBridgeTargetBehaviour`
- `UnityPlayerInputActivationTargetBehaviour`

Needed later:

- `PlayerViewBindingTargetBehaviour`
- `PlayerViewCameraTargetBindingTargetBehaviour`
- `PlayerViewCameraActivationTargetBehaviour`
- explicit camera target child/anchor, if root `Transform` is not the intended look target

Candidates to move to child objects:

- visual root
- camera target / look target anchor
- model-specific render hierarchy

Candidates for future facade:

- one `CanonicalPlayerBindingAuthoring` style component that references `PlayerSlotDeclaration`, `PlayerActorDeclaration`, `PlayerInput`, view target and camera target, then validates/applies existing target components.

Do not create the facade in F53C0.

## Final Recommendations

1. Remove string-based object lookup for `PlayerPrototype` from FIRSTGAME editor validation/setup.
2. Create typed references from editor tools/future facade to `PlayerSlotDeclaration`, `PlayerActorDeclaration`, `PlayerInput`, camera `Transform` and optional `Camera`.
3. Keep canonical identity as `PlayerSlotDeclaration` for slot and `PlayerActorDeclaration` for actor/reset/save evidence.
4. Prove real FIRSTGAME `PlayerView` / camera target wiring before full facade work.
5. Canonicalize identity before adding an authoring facade.
6. Later facade should be authoring-only, fail fast, avoid hidden fallback and consume existing F51/F52 targets instead of replacing them.

Recommended sequence:

```text
F53C1 - FIRSTGAME Player Identity Canonicalization
F53C2 - FIRSTGAME Real PlayerView / Camera Wiring Proof
F53C3 - FIRSTGAME Full Player Binding Chain Proof
F53D  - Canonical Player Binding Authoring Facade
```

## What Not To Change Now

- Do not create movement, gameplay command routing, actor spawning or save/progression runtime.
- Do not create new runtime contracts in F53C0.
- Do not modify frozen technical packages.
- Do not move FIRSTGAME assets into the package.
- Do not use FIRSTGAME as the primary QA lab for new contracts.
