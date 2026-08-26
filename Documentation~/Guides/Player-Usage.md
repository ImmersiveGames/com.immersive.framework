# Player Usage

Status: **Accepted Stage B Player baseline + current Player product authoring**  
Last updated: **2026-08-25**  
Decision sources: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Product-authoring record: [Scene-Provided Local Player Product Composition — 2026-08-17](../Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-SCENE-PROVIDED-LOCAL-PLAYER-PRODUCT-COMPOSITION-2026-08-17.md)  
Player Session public-surface record: [IF-ADR-015A — Player Session Observer and Explicit Command Surfaces — 2026-08-25](../Architecture/Reconciliation/IF-ADR-015A-Player-Session-Observer-and-Explicit-Command-Surfaces-2026-08-25.md)  
Current delivery authority: [IF-TRACK — Immersive Framework](../Architecture/Tracking/IF-TRACK-Framework.md)

> This guide describes the currently accepted Player path. Proposed command
> extensions such as exact-Slot Join and arbitrary Actor Selection remain governed
> by IF-ADR-015 and the current Tracker until their public contracts are implemented.

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

Scene-Provided Local Player
  technical local Host product composition

ActorProfile
  exact Logical Player prefab authority

Logical Player
  gameplay-owned representation / Actor
  consumes current gameplay authority after it exists
```

A Local Player product is a composition of existing authorities. It is not a new global
Player manager or runtime service.

## 2. Scene-Provided Local Player — canonical creation path

For a Scene-Provided local Player using Unity Input, use the official Editor action:

```text
GameObject
  > Immersive Framework
    > Player
      > Create Scene-Provided Local Player
```

Package implementation:

```text
ImmersiveGames/com.immersive.framework
5c9dab5661c95cf712d8cfce124a5d730d0dd1f1
feat(player): replace development creator with canonical local player tool
```

The action creates this deterministic technical shape:

```text
Scene-Provided Local Player
├─ PlayerInput
├─ LocalPlayerHostAuthoring
├─ SceneLocalPlayerAdmissionAuthoring
├─ UnityPlayerInputGateAdapter
└─ ActorMount
```

The action wires:

```text
LocalPlayerHostAuthoring.playerInput
  -> same-root PlayerInput

LocalPlayerHostAuthoring.actorMount
  -> ActorMount

UnityPlayerInputGateAdapter.playerInput
  -> same-root PlayerInput
```

The operation is Editor-owned and Undo-aware. It does not start runtime admission or
gameplay.

## 3. Configure explicit consumer intent

The Create action intentionally leaves project-specific intent unassigned.

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
  Initial Placement policy available in the accepted component surface
```

Do not treat the action map name `Player`, a particular Input Action Asset, a Slot or an
Actor Profile as a framework default.

The canonical creator clears the legacy hidden gameplay-map name hint. Assigning an
`InputActionAsset` later therefore does not implicitly select a Gameplay Action Map.

## 4. Local Player prefab and Logical Player prefab are different assets

FIRSTGAME Sample 00 now proves the intended asset separation.

Current consumer evidence:

```text
ImmersiveGames/planet-devourer
facb6e2d9b763b7200e670a029c06100505d7c06
Prefab localPlayer
```

The Sample uses two distinct prefab roles:

```text
Scene-Provided Local Player.prefab
  technical Host product composition

Scene-Provided Logical Player.prefab
  ActorProfile.LogicalActorHostPrefab
  gameplay / Actor representation
```

The scene composes them as:

```text
Scene-Provided Local Player [prefab instance]
└─ ActorMount
   └─ Scene-Provided Logical Player [prefab instance]
```

`SceneLocalPlayerAdmissionAuthoring.sceneLogicalPlayerActor` must resolve to the exact
`PlayerActorDeclaration` of that scene Logical Player instance.

The two prefab names describe different responsibilities. Do not collapse them into one
asset authority.

### Reusable technical Local Player prefab

A consumer may save a configured Local Player as a project prefab, as FIRSTGAME does.
A future package-neutral prefab/template should instead preserve the reusable technical
shape and leave project-specific intent empty.

A neutral package product must not invent:

```text
Player Slot Profile
Actor Profile
InputActionAsset
Gameplay Action Map
Logical Player prefab
```

The official Create action remains the current canonical package creation path.

## 5. ActorProfile owns the Logical Player prefab

`ActorProfile.LogicalActorHostPrefab` is the single authored prefab authority for the
Logical Player.

Typical gameplay-owned contents may include:

```text
PlayerActorDeclaration
PlayerGameplayInputConsumerBinding
CharacterController
locomotion / interaction behaviours
CameraMount or other gameplay-owned mounts
representation objects
```

`PlayerInput` does **not** belong on the Logical Player prefab. It belongs to the Local
Player Host.

The existing Scene-Provided Actor `Apply / Rebuild` path may materialize or preserve the
exact `ActorProfile.LogicalActorHostPrefab` instance under `ActorMount` and bind its
`PlayerActorDeclaration`. That utility is Actor materialization/evidence tooling; it is
not the owner of the complete Local Player product composition.

Materialization remains fail-closed:

```text
Actor missing
  -> materialize the exact ActorProfile prefab when the authoring path permits it

matching Actor present
  -> preserve and bind it

mismatched / unpacked / conflicting Actor content
  -> reject explicitly
  -> do not silently replace consumer content
```

## 6. Scene-Provided admission timing

`SceneLocalPlayerAdmissionAuthoring` defaults to:

```text
OnActivityEnter
```

For this path, automatic Scene-Provided admission is owned by the Activity lifecycle.
Do not add a manual Join merely to make an ordinary Scene-Provided Activity entry work.

The accepted baseline also distinguishes the dedicated Scene-Provided lifecycle path
from dynamic/command Join policy. A closed general Joining posture is not, by itself,
evidence that the dedicated Scene-Provided admission path should be bypassed.

## 7. Activity Player readiness level

The authored Activity requirement is the exact Player lifecycle boundary required by
that Activity.

Current ordered levels are:

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

`LogicalActorsPrepared` means the current Logical Actor preparation requirement has been
satisfied. It does not imply that current gameplay input/camera admission and consumer
bindings exist.

If Activity gameplay code consumes the current gameplay-input surface, request:

```text
GameplayReady
```

FIRSTGAME Sample 00 demonstrated this directly: authoring at
`LogicalActorsPrepared` (`30`) legitimately produced no current gameplay binding;
changing the Activity requirement to `GameplayReady` (`40`) caused the lifecycle to
enter the gameplay admission/input/camera chain.

Do not auto-promote readiness because a consumer happens to require a higher level.
Activity intent must remain explicit.

## 8. Gameplay input consumption

Gameplay-owned Logical Player code consumes the public current-gameplay surface rather
than reading the Host's `PlayerInput` directly.

Canonical consumer shape:

```text
PlayerGameplayInputConsumerBinding
  -> IPlayerGameplayInputReader
  -> GameplayReady
  -> TryReadValue<T>(InputActionReference, out value)
```

`InputActionReference` identifies authored intent. The live value comes from the exact
current runtime Player occurrence/binding.

Gameplay code must not use the authored reference as a bypass such as:

```text
inputActionReference.action.ReadValue<T>()
```

and must not resolve a `PlayerInput` through scene scans, hierarchy guesses, names,
tags, reflection or another fallback channel.

When `GameplayReady == false`, gameplay consumers remain unavailable and should fail
closed.

## 9. Unity Input Gate requirement

For a Scene-Provided Local Player using Unity Input and an Activity requiring
`GameplayReady`, the stable Local Player Host requires the canonical Unity Input Gate
endpoint:

```text
UnityPlayerInputGateAdapter
```

It targets the same Host `PlayerInput` and carries the exact authored Gameplay Action
Map intent.

This is why the canonical Create action includes the Gate adapter. Consumers should not
have to infer this runtime prerequisite from framework internals.

## 10. Manager-Provisioned baseline

Manager-Provisioned and Scene-Provided are separate provisioning paths. Do not silently
fallback from one to the other.

For Manager-Provisioned flows, use the public provisioning/session surfaces defined by
the accepted Player baseline. Do not copy the Scene-Provided scene-object composition
and reinterpret it as Manager provisioning.

The current explicit Player Session command components are:

```text
PlayerSessionOpenJoiningCommandTrigger
PlayerSessionCloseJoiningCommandTrigger
PlayerSessionJoinCommandTrigger
PlayerSessionDefaultActorSelectionCommandTrigger
PlayerSessionLeaveCommandTrigger
```

Each component represents one request and owns only its own typed result. The former
generic `PlayerSessionCommandTrigger` + `PlayerProvisioningCommandOperation` authoring
model is superseded.

Proposed exact-Slot Join and arbitrary Actor-selection commands remain separate extension
scope; do not infer those capabilities from the existence of the current Join or Default
Actor Selection components.

## 11. Diagnostics and fail-closed behavior

Useful diagnostics distinguish at least:

```text
Activity Player requirement level
Scene-Provided admission state
exact Player Slot identity
Actor Profile / exact Logical Player instance
Local Player Host / PlayerInput evidence
Unity Input Gate endpoint
current gameplay binding
GameplayReady
binding revision / diagnostic
```

For `PlayerGameplayInputConsumerBinding` in the current implementation:

```text
BindingRevision == 0
```

means that instance has not yet committed a runtime gameplay binding. Combined with
`GameplayReady == false`, this is compatible with an Activity intentionally stopping at
`LogicalActorsPrepared`; it is not by itself a package regression.

If an Activity explicitly requires `GameplayReady` and the lifecycle still fails, use
the first exact lifecycle/readiness prerequisite failure as the diagnostic boundary.
Do not patch locomotion or fabricate readiness to hide that failure.

## 12. Player Session public surfaces

Use the scoped public surface according to intent:

```text
PlayerSessionObserver = read
explicit Player Session Command Trigger = request/change
```

`PlayerSessionObserver` is read-only. It is appropriate for Hub, UI,
presentation, other scenes than the physically materialized Player, and any
consumer that must consult the current Player Session without locating a
Player GameObject. It exposes only the published scoped observation and its
derived presentation labels; it never executes commands or owns Player truth.

The former `PlayerSessionStatus` name was replaced by `PlayerSessionObserver` to make
that read-only, cross-scene usage explicit. The Observer does not aggregate command
results.

Commands are separate explicit components:

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

Assign the component matching the requested operation to a UnityEvent and invoke it
explicitly. Each trigger owns only its own typed result.

Example Hub composition:

```text
Hub Scene
├─ PlayerSessionObserver
│    read-only Session / Slot / Actor presentation
│
├─ Join Button
│    └─ PlayerSessionJoinCommandTrigger.Invoke()
│
└─ Leave Button
     └─ PlayerSessionLeaveCommandTrigger.Invoke()
```

The Observer is not required for Join/Leave. Compose observation and commands
independently according to consumer intent.

### Deferred command-surface readiness

A current Manager-Provisioned consumer run exposed a non-blocking timing window:

```text
first Join
  -> bindingStatus = Unbound
  -> RejectedRuntimeUnavailable

scoped access binds

subsequent Join
  -> bindingStatus = Bound
  -> SucceededJoined
```

This is recorded as `PLAYER-COMMAND-SURFACE-READINESS / DEFERRED`. A future cut should
make command availability distinguishable before normal interaction is enabled without
adding global lookup, alternate Session authority or hidden fallback.

## 13. Anti-patterns

Do not add:

- direct `PlayerInput` reads from Logical Player gameplay code;
- `InputActionReference.action.ReadValue<T>()` as a live-runtime bypass;
- `PlayerInput` on the Logical Player prefab;
- manual Join to compensate for ordinary `OnActivityEnter` Scene-Provided admission;
- automatic readiness promotion from `LogicalActorsPrepared` to `GameplayReady`;
- hidden default Gameplay Action Map selection;
- global Player manager/service locator;
- scene scans or hierarchy/name/tag lookup as authority;
- silent fallback between provisioning modes;
- a second Logical Player prefab authority outside `ActorProfile`;
- a Local Player prefab that silently bakes project-specific Slot / Actor / Input intent
  and presents those values as framework defaults;
- a mutable second Player Session state store inside `PlayerSessionObserver`;
- one enum-driven command MonoBehaviour whose serialized operation changes its complete semantic identity.

## 14. Future Player expansions

The current Tracker is authoritative for delivery status beyond this accepted Player
path.

Proposed exact-Slot Join and arbitrary Actor-selection commands remain separate from the
current explicit command set until their public contracts are implemented and promoted.
Do not infer their availability from `PlayerSessionJoinCommandTrigger` or
`PlayerSessionDefaultActorSelectionCommandTrigger`.
