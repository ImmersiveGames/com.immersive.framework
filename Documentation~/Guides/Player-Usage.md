# Player Usage

Status: Current
Last updated: 2026-07-25

## Configure participation

1. Create `PlayerSlotProfile` assets for stable local participation seats.
2. Optionally assign each Slot a default `ActorProfile`.
3. Add the Profiles to `GameApplicationAsset` in allocation order.
4. Choose the explicit duplicate Actor-selection policy.
5. Configure each Activity participation requirement.

Slot order is product configuration. Unity player index, hierarchy order and
join callback order are not Slot identity.

## Manager-Provisioned Logical Player

In Persistent Content, configure one `LocalPlayerProvisioningAuthoring` with an
explicit manual-join `PlayerInputManager`, then reference it through
`LocalPlayerProvisioningHostRegistration`.

The Player prefab contains:

```text
PlayerInput
LocalPlayerHostAuthoring
empty Actor Mount
```

Do not pre-author a `PlayerSlotId`. Runtime admission associates the official
host with its logical Slot.

The empty Actor Mount rule belongs to this Manager-Provisioned source. It is not a
generic Local Player Host invariant.

## Scene-Provided Logical Player

Use the Scene-Provided Player Composer when a scene already owns the local Player
Host and Logical Actor.

The canonical product workflow is prefab-based. Do not use an empty creator object
as the primary authoring path.

### Prefab boundaries

Create or reuse one Actor prefab:

```text
Actor_PlayerSceneProvided
  PlayerActorDeclaration
  CharacterController or other Actor gameplay components
  movement component
  Anchors
  Visual
```

The Actor prefab must not contain `PlayerInput`.

Create one outer composed Player prefab:

```text
Player_SceneProvided
  PlayerInput
  LocalPlayerHostAuthoring
  SceneLocalPlayerAdmissionAuthoring

  Actor Mount
    Actor_PlayerSceneProvided
```

The component class remains `SceneLocalPlayerAdmissionAuthoring`; its Inspector is
displayed as **Scene-Provided Player Composer**.

### Actor Profile

Configure the selected `ActorProfile` as:

```text
Actor Kind
  Player

Actor Role
  Protagonist

Logical Actor Host Prefab
  Actor_PlayerSceneProvided
```

Do not assign the outer `Player_SceneProvided` prefab as the Logical Actor Host
prefab.

### Local Player Host

On the `Player_SceneProvided` root:

```text
Player Input
  same-root PlayerInput

Actor Mount
  explicit Actor Mount child
```

The Host Inspector validates shared technical invariants only. It accepts the
authored Actor under Actor Mount for a Scene-Provided composition.

Expected validation:

```text
Ready — shared Local Player Host invariants are valid.
```

### Scene-Provided Player Composer

On the same root, configure:

```text
Participation
  Player Slot Profile
    exact Slot used by the Activity

Logical Actor
  Actor Profile
    profile referencing Actor_PlayerSceneProvided

  Scene Logical Player Actor
    PlayerActorDeclaration from the nested Actor instance

Admission
  Admission Timing
    On Activity Enter
```

`Local Player Host` is not a manually assigned field. The composer requires and
resolves `LocalPlayerHostAuthoring` from the same GameObject.

### Apply and validate

Run:

```text
Apply / Rebuild
Validate
```

`Apply / Rebuild`:

```text
validates same-root Host
validates Actor under the exact Actor Mount
validates the nested Actor prefab source
compares it with ActorProfile.LogicalActorHostPrefab
stores typed evidence inside the composer
```

It does not:

```text
reserve a Slot
assign a runtime ActorId
start gameplay
create or destroy the Host
create or destroy the Actor
add a visible evidence component to the Actor
```

Expected authoring result:

```text
Scene-Provided Player authoring and internal profile evidence are valid.
```

### Example movement binding

Gameplay components remain consumer-owned. A movement component on the Actor may
receive:

```text
PlayerInput
  explicit reference to the outer Host PlayerInput

CharacterController
  explicit reference to the Actor-owned controller
```

Do not use a global lookup, object name, tag or first-found Player as binding
authority.

### Current runtime boundary

The accepted architecture allows a Scene-Provided Logical Player in a Route scene
or an Activity scene.

Current automatic lifecycle coverage is narrower: the implemented admission path
is declared covered for Scene-Provided authoring resolved through the active
Activity content scene set.

A composer located only in the Route Primary Scene is not yet documented as
runtime-complete. Treat it as a pending integration cut until focused Play Mode
evidence proves admission, Actor adoption, readiness and release.

## Activity participation

For a Scene-Provided Player already containing its Logical Actor, the practical
readiness levels are:

```text
Joined Slots
  proves Logical Player admission only

Logical Actors Prepared
  recommended baseline when the source already provides the Actor

Gameplay Ready
  additionally waits for gameplay, input and Camera eligibility
```

`Explicit Slots + Allowed` is valid when zero participants are temporarily
permitted. `No Slots` still requires the zero-participant policy to be Allowed.

## Pause integration

Physical Pause input belongs to the official Player:

```text
PlayerInput
UnityPlayerInputGateAdapter
PausePlayerInputBinding
```

`Global` is an action map of that PlayerInput, not a second global Player.

`PauseRequestTrigger` is not a Player component. It may live in Persistent
Content, Route scenes or Activity scenes and receives its request port from the
corresponding composition lifecycle.

Authored buttons can apply application-only Pause without an active Player
binding. In that mode the framework changes logical Pause, TimeScale and
presentation but does not modify action maps.

Therefore:

```text
Escape / Gamepad Start
  requires official Player binding

Pause / Resume / Toggle Button
  does not require a Player
  requires a composed PauseRequestTrigger
```

See [Pause Usage](Pause-Usage.md).

## Diagnose

Inspect Slot allocation, admission, Actor selection, Logical Actor preparation,
input eligibility and Camera eligibility as separate evidence.

For Scene-Provided authoring, inspect:

```text
Local Player Host
  Validate Host
  Advanced / Debug

Scene-Provided Player Composer
  Apply / Rebuild
  Validate
  Advanced / Debug
```

Authoring validity does not prove runtime admission.

For Pause, distinguish:

```text
PauseRequestTrigger.ProductRequestBindingStatus
PauseRequestTrigger.LastProductStatus
PauseRequestTrigger.LastExecutionMode
PausePlayerInputBinding.BindingStatus
```

A bound Trigger does not imply that a Player binding exists; it may legitimately
execute in `ApplicationOnly` mode.
