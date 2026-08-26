# Player Usage

Status: **Accepted current Player baseline + current product authoring**  
Last updated: **2026-08-25**  
Decision sources: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Product-authoring record: [Scene-Provided Local Player Product Composition — 2026-08-17](../Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-SCENE-PROVIDED-LOCAL-PLAYER-PRODUCT-COMPOSITION-2026-08-17.md)  
Player Session public-surface record: [IF-ADR-015A — Player Session Observer and Explicit Command Surfaces — 2026-08-25](../Architecture/Reconciliation/IF-ADR-015A-Player-Session-Observer-and-Explicit-Command-Surfaces-2026-08-25.md)  
Current delivery authority: [IF-TRACK — Immersive Framework](../Architecture/Tracking/IF-TRACK-Framework.md)

## 1. Product model

Keep the authored and runtime responsibilities separate.

```text
PlayerSessionProfile
  initial Session Player configuration / provisioning policy

Player Slot Profile
  exact authored logical Slot identity

Activity Player participation policy
  which configured Players participate in the current Activity
  exact readiness level required by that Activity

Scene Player
  technical local Host already authored in a Scene

Player Provisioning
  Session-authorized authority that can create Local Player Hosts

ActorProfile
  exact Logical Player prefab authority

Logical Player
  gameplay-owned representation / Actor
  consumes current gameplay authority after it exists
```

A Local Player product is a composition of existing authorities. It is not a new global
Player manager or runtime service.

## 2. Scene Player — canonical creation path

For a Scene-Provided local Player using Unity Input, use the official Editor action:

```text
GameObject
  > Immersive Framework
    > Player
      > Scene
        > Create Local Player
```

The action creates the deterministic technical Host shape used by the Scene Player path.
Project-specific Slot, Actor and Input intent must still be authored explicitly.

Typical composition:

```text
Scene Player
├─ PlayerInput
├─ LocalPlayerHostAuthoring
├─ SceneLocalPlayerAdmissionAuthoring
├─ UnityPlayerInputGateAdapter
└─ ActorMount
```

The operation is Editor-owned and Undo-aware. It does not start runtime admission or
gameplay.

## 3. Configure explicit consumer intent

Configure the exact project values after creation:

```text
PlayerInput
  InputActionAsset

UnityPlayerInputGateAdapter
  Gameplay Action Map

SceneLocalPlayerAdmissionAuthoring
  Player Slot Profile
  Actor Profile
  Admission Timing
  Initial Placement policy when applicable
```

Do not treat a particular action map, Input Action Asset, Slot or Actor Profile as a
Framework default.

## 4. Local Player Host prefab and Logical Player prefab are different assets

The technical Local Player Host and the Logical Player representation are different
assets and different authorities.

```text
Local Player Host prefab
  PlayerInput / Host / Actor Mount / technical admission evidence

Logical Player prefab
  ActorProfile.LogicalActorHostPrefab
  gameplay / Actor representation
```

`ActorProfile.LogicalActorHostPrefab` remains the single authored prefab authority for
the Logical Player.

`PlayerInput` belongs to the Local Player Host, not to the Logical Player prefab.

## 5. ActorProfile owns the Logical Player prefab

Typical gameplay-owned Logical Player contents may include:

```text
PlayerActorDeclaration
PlayerGameplayInputConsumerBinding
CharacterController
locomotion / interaction behaviours
CameraMount or other gameplay-owned mounts
representation objects
```

Materialization remains fail-closed. Matching authored content may be preserved; missing
required content may be materialized only through its accepted authoring path; conflicting
consumer content must not be silently absorbed or replaced.

## 6. Scene Player admission timing

`SceneLocalPlayerAdmissionAuthoring` defaults to:

```text
OnActivityEnter
```

For this path, automatic Scene-Provided admission is owned by the Activity lifecycle.
Do not add a manual Join merely to make an ordinary Scene Player Activity entry work.

## 7. Activity Player readiness level

The authored Activity requirement is the exact Player lifecycle boundary required by
that Activity.

```text
None
JoinedSlots
SelectedActors
LogicalActorsPrepared
GameplayReady
```

The important boundary is:

```text
LogicalActorsPrepared != GameplayReady
```

If Activity gameplay code consumes current gameplay input/camera authority, author
`GameplayReady` explicitly. Do not auto-promote readiness because a consumer happens to
need a higher level.

## 8. Gameplay input consumption

Gameplay-owned Logical Player code consumes the public current-gameplay surface rather
than reading the Host's `PlayerInput` directly.

```text
PlayerGameplayInputConsumerBinding
  -> IPlayerGameplayInputReader
  -> GameplayReady
  -> TryReadValue<T>(InputActionReference, out value)
```

`InputActionReference` identifies authored intent. The live value comes from the exact
current runtime Player occurrence/binding.

Do not bypass that contract with direct `InputActionReference.action.ReadValue<T>()`,
scene scans, hierarchy guesses, names, tags or reflection.

## 9. Unity Input Gate requirement

For a Scene Player using Unity Input and an Activity requiring `GameplayReady`, the
stable Local Player Host requires the canonical Unity Input Gate endpoint:

```text
UnityPlayerInputGateAdapter
```

It targets the same Host `PlayerInput` and carries the exact authored Gameplay Action Map
intent.

## 10. Player Provisioning baseline

Player Provisioning and Scene Player are separate product compositions over the two
Host Provisioning modes. Do not silently fall back from one to the other.

For Manager-Provisioned flows, use the public Session/provisioning surfaces rather than
copying the Scene Player object composition and reinterpreting it as provisioning.

The current explicit control surfaces are:

```text
PlayerSessionOpenJoiningCommandTrigger
PlayerSessionCloseJoiningCommandTrigger
PlayerSessionJoinCommandTrigger
PlayerSessionDefaultActorSelectionCommandTrigger
PlayerSessionLeaveCommandTrigger
```

Each component represents one command and owns only its own typed result evidence.
There is no generic serialized command enum in the current product surface.

## 11. Diagnostics and fail-closed behavior

Useful diagnostics distinguish at least:

```text
Activity Player requirement level
exact Player Slot identity
Actor Profile / exact Logical Player instance
Local Player Host / PlayerInput evidence
current scoped consumer binding
current gameplay binding
GameplayReady
command outcome / diagnostic when a command was invoked
```

If a scoped consumer has not yet bound, command invocation fails explicitly. Do not
search globally for another Session or fabricate availability.

## 12. Player Session public surfaces

Use the scoped public surface according to intent:

```text
PlayerSessionObserver = read
explicit Player Session Command Trigger = request/change
```

### PlayerSessionObserver

`PlayerSessionObserver` is read-only. Use it when Hub, UI, presentation or another scene
needs current Player Session information without locating the physically materialized
Player GameObject.

Conceptually:

```text
Player Session authority
        ↓
scoped public observation
        ↓
PlayerSessionObserver
        ↓
Hub / UI / presentation / other scene
```

The Observer may expose published Session, Slot, Actor, preparation/materialization,
gameplay-admission and Activity-occurrence evidence. It does not execute commands and
does not own Player truth.

The older `PlayerSessionStatus` name was replaced by `PlayerSessionObserver` so the
component's cross-scene/read-only purpose is explicit.

The Observer does not aggregate command results.

### Explicit command components

Use the component matching the requested operation:

```text
Open Joining
  -> PlayerSessionOpenJoiningCommandTrigger.Invoke()

Close Joining
  -> PlayerSessionCloseJoiningCommandTrigger.Invoke()

Join
  -> PlayerSessionJoinCommandTrigger.Invoke()

Default Actor Selection
  -> PlayerSessionDefaultActorSelectionCommandTrigger.Invoke()

Leave
  -> PlayerSessionLeaveCommandTrigger.Invoke()
```

The command component may live on a Button/control object or another explicit consumer.
It uses the same scoped consumer access boundary as the Observer, but it is independent
from the Observer.

### Example Hub composition

```text
Hub Scene
├─ PlayerSessionObserver
│    read-only Session presentation
│
├─ Join Button
│    └─ PlayerSessionJoinCommandTrigger.Invoke()
│
└─ Leave Button
     └─ PlayerSessionLeaveCommandTrigger.Invoke()
```

Rules:

```text
need only observation
  -> use PlayerSessionObserver

need only a request
  -> use the matching explicit command component

need both
  -> compose them independently
```

The Hub does not need a reference to the physical Player.

## 13. Command Inspector contract

The command and Observer Inspectors follow IF-ADR-010.

Normal surface:

```text
Scope
command-specific authored intent when applicable
Validation
Advanced / Debug
```

`Reason`, optimistic revisions, Leave occurrence correlation, detailed result evidence
and manual Play Mode `Invoke` testing belong under `Advanced / Debug`.

There is no Apply/Rebuild operation for these components.

Validation is explicit and should not be treated as a hidden full-validation pass on
every Inspector repaint.

## 14. Deferred command-surface readiness follow-up

A current Player Provisioning consumer run exposed a timing window where UI interaction
can occur before scoped command access has bound:

```text
first Join
  -> bindingStatus = Unbound
  -> RejectedRuntimeUnavailable

binding completes

subsequent Join
  -> bindingStatus = Bound
  -> SucceededJoined
```

This is recorded as **PLAYER-COMMAND-SURFACE-READINESS / DEFERRED**.

It is not a reason to add global fallback or another authority. A future cut should make
command availability distinguishable before normal interaction is enabled.

## 15. Anti-patterns

Do not add:

- direct `PlayerInput` reads from Logical Player gameplay code;
- `InputActionReference.action.ReadValue<T>()` as a live-runtime bypass;
- `PlayerInput` on the Logical Player prefab;
- manual Join to compensate for ordinary `OnActivityEnter` Scene Player admission;
- automatic readiness promotion;
- hidden default Gameplay Action Map selection;
- global Player manager/service locator;
- scene scans or hierarchy/name/tag lookup as authority;
- silent fallback between provisioning modes;
- a second Logical Player prefab authority outside `ActorProfile`;
- a second mutable Player Session state store in `PlayerSessionObserver`;
- a generic enum-driven Player Session command MonoBehaviour that changes semantic identity by enum selection.

## 16. Future Player expansions

The current Tracker is authoritative for delivery status beyond the implemented surface.

Proposed exact-Slot Join and arbitrary Actor-selection commands remain separate from the
current explicit command set until their public contracts are implemented and promoted.
Do not infer their availability from the existence of `PlayerSessionJoinCommandTrigger`
or `PlayerSessionDefaultActorSelectionCommandTrigger`.
