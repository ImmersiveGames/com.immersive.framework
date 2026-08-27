# Player Usage

Status: **Accepted Stage B Player baseline + current Player public product surface**  
Last updated: **2026-08-26**  
Decision sources: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Product-authoring record: [Scene-Provided Local Player Product Composition — 2026-08-17](../Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-SCENE-PROVIDED-LOCAL-PLAYER-PRODUCT-COMPOSITION-2026-08-17.md)  
Player Session public-surface record: [IF-ADR-015A — Player Session Observer and Explicit Command Surfaces — 2026-08-25](../Architecture/Reconciliation/IF-ADR-015A-Player-Session-Observer-and-Explicit-Command-Surfaces-2026-08-25.md)  
Actor-selection closure: [IF-ADR-015B — Player Actor Selection Public Surface Certification — 2026-08-26](../Architecture/Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)  
Current delivery authority: [IF-TRACK — Immersive Framework](../Architecture/Tracking/IF-TRACK-Framework.md)

> This guide describes the currently accepted Player path. Arbitrary Actor Selection is
> now part of the delivered public surface. Exact-Slot public Join and the Local
> Multiplayer Slot/device/input contract remain future scope.

## 1. Product model

Keep authored intent, Session authority, physical Player lifetime and Activity context
separate.

```text
PlayerSessionProfile
  initial Session Player configuration / provisioning policy

Player Slot Profile
  exact authored logical Slot identity

Activity Player participation policy
  which configured Players participate in the current Activity
  exact readiness level required by that Activity

Local Player Host
  technical host for one local Player

ActorProfile
  exact Logical Player prefab authority

Logical Player
  gameplay-owned representation / Actor
  consumes current gameplay authority after it exists
```

A Local Player product is a composition of existing authorities. It is not a new global
Player manager or runtime service.

## 2. Scene Player — canonical Scene-Provided creation path

For a Scene-Provided local Player using Unity Input, use the official Editor action:

```text
GameObject
  > Immersive Framework
    > Player
      > Create Scene-Provided Local Player
```

The action creates the deterministic technical shape:

```text
Scene-Provided Local Player
├─ PlayerInput
├─ LocalPlayerHostAuthoring
├─ SceneLocalPlayerAdmissionAuthoring
├─ UnityPlayerInputGateAdapter
└─ ActorMount
```

The operation is Editor-owned and Undo-aware. It does not start runtime admission or
gameplay.

Configure project-specific intent after creation:

```text
PlayerInput
  InputActionAsset

UnityPlayerInputGateAdapter
  Gameplay Action Map

SceneLocalPlayerAdmissionAuthoring
  Player Slot Profile
  Actor Profile
  Admission Timing
  Initial Placement policy
```

Do not treat a particular Slot, Actor, Input Action Asset or action-map name as a
framework default.

## 3. Local Player Host and Logical Player are different responsibilities

Scene-Provided consumer composition keeps the technical Host and Logical Player Actor
separate.

```text
Scene-Provided Local Player.prefab
  technical Host product composition

Scene-Provided Logical Player.prefab
  ActorProfile.LogicalActorHostPrefab
  gameplay / Actor representation
```

The scene composes them through the Actor mount:

```text
Scene-Provided Local Player [prefab instance]
└─ ActorMount
   └─ Scene-Provided Logical Player [prefab instance]
```

`ActorProfile.LogicalActorHostPrefab` remains the single authored prefab authority for
the Logical Player.

`PlayerInput` belongs to the Local Player Host, not to the Logical Player prefab.

## 4. Scene-Provided admission timing

`SceneLocalPlayerAdmissionAuthoring` defaults to:

```text
OnActivityEnter
```

For this path, automatic Scene-Provided admission is owned by the Activity lifecycle.
Do not add a manual Join merely to make ordinary Scene-Provided Activity entry work.

Scene-Provided and Manager-Provisioned are separate provisioning origins. They converge
on the same Session/Slot/Actor authority after successful admission; do not silently
fallback from one mode to the other.

## 5. Activity Player readiness

Current ordered Player readiness levels are:

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

If Activity gameplay consumes current gameplay input/camera authority, request:

```text
GameplayReady
```

Do not auto-promote readiness because a consumer happens to require a higher level.
Activity intent must remain explicit.

## 6. Gameplay input consumption

Gameplay-owned Logical Player code consumes the public current-gameplay surface rather
than reading the Host's `PlayerInput` directly.

Canonical shape:

```text
PlayerGameplayInputConsumerBinding
  -> IPlayerGameplayInputReader
  -> GameplayReady
  -> TryReadValue<T>(InputActionReference, out value)
```

Do not bypass the runtime binding with direct reads such as:

```text
inputActionReference.action.ReadValue<T>()
```

and do not resolve `PlayerInput` through scene scans, hierarchy guesses, names, tags,
reflection or another fallback channel.

## 7. Manager-Provisioned baseline

Manager-Provisioned uses Session-authorized provisioning authority rather than a
scene-authored Local Player Host instance.

Typical high-level flow:

```text
PlayerSessionProfile
  HostProvisioning = ManagerProvisioned
        ↓
Local Player Provisioning authority
        ↓
explicit Join
        ↓
Local Player Host created/admitted
        ↓
Actor selection / preparation
        ↓
Activity representation
        ↓
GameplayReady
```

The provisioning authority is not itself a Player Host.

## 8. Player Session public surface

Use the scoped public surface according to intent:

```text
PlayerSessionObserver = read
explicit Player Session Command Trigger = request/change
```

`PlayerSessionObserver` is read-only. It is appropriate for Hub, UI, presentation,
prefabs and another scene than the physically materialized Player.

It exposes published scoped evidence such as Session state, Slot occupancy, selected
Actor, selection revision, preparation/materialization/admission and gameplay readiness.
It does not execute commands or own Player truth.

The current explicit public command family is:

```text
PlayerSessionOpenJoiningCommandTrigger
PlayerSessionCloseJoiningCommandTrigger
PlayerSessionJoinCommandTrigger
PlayerSessionSelectActorCommandTrigger
PlayerSessionDefaultActorSelectionCommandTrigger
PlayerSessionReplaceActorSelectionCommandTrigger
PlayerSessionClearActorSelectionCommandTrigger
PlayerSessionLeaveCommandTrigger
```

Each component represents exactly one request and owns only its own typed result.

Example composition:

```text
Hub / UI
├─ optional PlayerSessionObserver
│    read-only Session / Slot / Actor presentation
│
├─ Join Button
│    └─ PlayerSessionJoinCommandTrigger.Invoke()
│
├─ Character choice A
│    └─ PlayerSessionSelectActorCommandTrigger.Invoke()
│
└─ Leave Button
     └─ PlayerSessionLeaveCommandTrigger.Invoke()
```

The Observer is not required for commands to work. Compose observation and requests
independently according to consumer intent.

## 9. Actor Selection

Actor selection is Session-owned logical intent for one exact Joined Slot.

Public operations are:

```text
Select Actor
Select Default Actor
Replace Actor Selection
Clear Actor Selection
```

All four return `PlayerActorSelectionResult` evidence.

### Select Actor

Use `PlayerSessionSelectActorCommandTrigger` when game-owned UI chooses an explicit
`ActorProfile`.

The command may author:

```text
Player Slot
Actor Profile
Expected Selection Revision
Reason
```

The game owns the presented choice set. The Framework owns validation and the selection
commit.

### Select Default Actor

Use `PlayerSessionDefaultActorSelectionCommandTrigger` only when the Session policy
allows default resolution.

```text
ActorResolution = ResolveConfiguredDefault
  -> configured DefaultActorProfile only

ActorResolution = LeaveUnresolved
  -> default request rejects
```

There is no silent Actor fallback.

### Replace / Clear

`Replace Actor Selection` and `Clear Actor Selection` operate only before the canonical
Actor preparation barrier.

They are **not** physical hot-swap commands.

Once a Logical Player Actor is prepared, or a retained preparation/release failure is
holding the preparation barrier, Select / Replace / Clear reject with canonical Actor
selection failure evidence rather than mutating physical state.

## 10. Character Selection flow

The public arbitrary Actor-selection blocker is closed.

A canonical Character Selection application may now use:

```text
PlayerSessionProfile
  ActorResolution = LeaveUnresolved
        ↓
Join
  Slot Joined
  Actor unresolved
        ↓
game-owned Character Selection UI
        ↓
PlayerSessionSelectActorCommandTrigger
        ↓
PlayerActorSelectionResult
        ↓
existing Framework Actor preparation
        ↓
Manager-Provisioned materialization / Activity lifecycle
        ↓
GameplayReady
```

The sample/game must not:

```text
mutate Session state directly
call internal Actor-selection runtime ports
prepare/materialize the Actor itself
perform global Player discovery
add a fallback Actor
turn Replace into hot swap
```

Character Selection is a consumer of the public Player surface, not a second Player
architecture.

## 11. Revision and idempotency

Actor selection is revision-aware.

Expected behavior includes:

```text
Select A first time
  -> selection succeeds
  -> revisions advance once

Select A again
  -> idempotent success
  -> revisions unchanged

Select B while A selected
  -> reject; use Replace

Replace B before preparation
  -> succeeds
  -> revisions advance once

Clear before preparation
  -> succeeds
  -> revisions advance once

stale expected revision
  -> reject
  -> no mutation
```

Duplicate Actor selection remains governed by Session policy.

## 12. Authoring validation vs runtime binding

For `PlayerSessionObserver` and explicit command components, keep these concepts separate:

```text
TryValidateConfiguration()
  authoring/configuration validity

BindingState / IsScopedAccessAvailable / TryGetAccess
  current runtime scoped-access availability
```

A valid `Route`- or `Activity`-authored component may temporarily be runtime-unbound.
That does not make the authored configuration invalid.

Likewise, a component physically present in Route-discovered content may legitimately be
`Activity` scoped and bind only during the Activity lifecycle.

Do not infer runtime ownership from GameObject location alone.

## 13. Deferred command-surface readiness

A valid authored command can still be invoked before its scoped runtime access becomes
available.

Current fail-closed behavior is:

```text
valid authoring
+ no live scoped access
  -> runtime command rejects
  -> no fallback
  -> no Session mutation
```

This remains tracked as:

```text
PLAYER-COMMAND-SURFACE-READINESS / DEFERRED
```

A future product cut may expose command availability more directly for UI gating. It must
not add a second Session authority or global lookup.

## 14. Diagnostics

Useful Player diagnostics distinguish at least:

```text
Session availability / revisions
Joining posture
exact Player Slot identity
selected Actor / selection revision
Actor preparation state
physical materialization
Activity owner / occurrence
Local Player Host / PlayerInput evidence
GameplayReady
scoped consumer binding state / scope / owner
last command-specific typed result
```

Do not use an Observer as a global last-command aggregator.

## 15. Anti-patterns

Do not add:

- direct `PlayerInput` reads from Logical Player gameplay code;
- `InputActionReference.action.ReadValue<T>()` as a live-runtime bypass;
- `PlayerInput` on the Logical Player prefab;
- manual Join to compensate for ordinary `OnActivityEnter` Scene-Provided admission;
- automatic readiness promotion;
- hidden default Gameplay Action Map selection;
- global Player manager/service locator;
- scene scans or hierarchy/name/tag lookup as authority;
- silent fallback between provisioning modes;
- a second Logical Player prefab authority outside `ActorProfile`;
- a mutable second Player Session state store inside `PlayerSessionObserver`;
- one enum-driven command MonoBehaviour whose serialized operation changes its semantic identity;
- sample/game-owned Actor selection commit authority;
- physical Actor hot-swap hidden behind `Replace Actor Selection`.

## 16. Current certification and future Player expansions

The current integrated Player public/runtime boundary is certified by:

```text
PLAYER CURRENT AGGREGATE COMPLETE
mandatoryContracts=27
executedContracts=27
passedContracts=27
actor=PASS
publicSurface=PASS
```

The historical Full Player `25/25` remains dated evidence for its earlier boundary.

Still outside the delivered public Player surface:

```text
exact-Slot public Join
public Slot/device/InputUser/control-scheme ownership observation
canonical Local Multiplayer device/input contract
consumer-facing prepared physical Actor hot-swap
```

Arbitrary Actor Selection is **not** future scope anymore. Character Selection may
proceed using the delivered public command surface.
