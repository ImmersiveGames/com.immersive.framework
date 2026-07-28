# Player Usage

Status: Current  
Last updated: 2026-07-28

## Choose the Logical Player source

The framework has one Session-scoped Logical Player participation authority and three accepted local source categories.

| Source | Use when | Runtime status |
|---|---|---|
| Manager-Provisioned Logical Player | an explicit join creates a `PlayerInput` Host through `PlayerInputManager` | implemented |
| Scene-Provided Logical Player | a Route or Activity scene already contains the Host and usually its Logical Actor | implemented |
| Session-Persistent Logical Player | Application/Session composition owns Logical Player identity outside Routes and Activities | architecture accepted; not implemented |

All sources converge into:

```text
PlayerParticipationRuntimeContext
-> typed PlayerSlotId
```

A Logical Player does not inherently imply:

```text
Local Player Host
Actor selection
Logical Actor
Actor materialization
input eligibility
Camera eligibility
gameplay readiness
```

Those are separate evidence stages.

## Configure participation

1. Create `PlayerSlotProfile` assets for stable local participation seats.
2. Optionally assign each Slot a default `ActorProfile`.
3. Add the Profiles to `GameApplicationAsset` in allocation order.
4. Choose the explicit duplicate Actor-selection policy.
5. Configure each Activity participation projection, zero-participant policy and requirement level.

Slot order is product configuration. Unity player index, hierarchy order and join callback order are not Slot identity.

---

## Manager-Provisioned Logical Player

Use this source when a join request must create the physical local Player Host.

### Persistent Content composition

Configure one explicit provisioning surface in Persistent Content:

```text
Local Player Provisioning
  PlayerInputManager
  LocalPlayerProvisioningAuthoring
  LocalPlayerProvisioningHostRegistration
```

`PlayerInputManager` must use the framework-supported manual join path. Do not enable an independent automatic join lane that bypasses Slot reservation and transaction authority.

### Player prefab

The Player prefab contains:

```text
PlayerInput
LocalPlayerHostAuthoring
empty Actor Mount
```

Do not pre-author a `PlayerSlotId`. Runtime admission associates the official Host with the reserved logical Slot.

The empty Actor Mount rule belongs to this Manager-Provisioned source. It is not a generic Local Player Host invariant.

### Join flow

```text
explicit framework-authorized join request
-> reserve first configured free PlayerSlot
-> PlayerInputManager creates one Host
-> validate LocalPlayerHostAuthoring
-> admit one Logical Player
-> bind typed PlayerSlotId
-> select/prepare Actor according to policy
-> commit or explicit rollback
```

`PlayerInputManager` is the technical Host provisioner. It does not select framework Slot identity, Actor policy, contextual lifetime or Activity readiness.

### Required negative proof

A consumer or QA test should verify:

```text
missing provisioning registration blocks explicitly
invalid Player prefab blocks explicitly
failed physical join releases Slot reservation
failed committed join releases Host evidence and owned physical content
playerIndex is never used as PlayerSlotId
exit permits later Slot reuse
```

### Consumer UX expectation

A development consumer should be able to locate, without reading runtime internals:

```text
where provisioning lives
which prefab PlayerInputManager creates
which Slots are available
which command requests join
which Actor policy applies
whether the transaction committed or rolled back
```

---

## Scene-Provided Logical Player

Use this source when a Route or Activity scene already owns the local Player Host and Logical Actor.

The canonical product workflow is prefab-based. Do not use an empty creator object as the primary authoring path.

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

The component class remains `SceneLocalPlayerAdmissionAuthoring`; its Inspector is displayed as **Scene-Provided Player Composer**.

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

Do not assign the outer `Player_SceneProvided` prefab as the Logical Actor Host prefab.

### Local Player Host

On the `Player_SceneProvided` root:

```text
Player Input
  same-root PlayerInput

Actor Mount
  explicit Actor Mount child
```

The Host Inspector validates shared technical invariants only. It accepts the authored Actor under Actor Mount for a Scene-Provided composition.

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

`Local Player Host` is not a manually assigned field. The composer requires and resolves `LocalPlayerHostAuthoring` from the same GameObject.

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

Gameplay components remain consumer-owned. A movement component on the Actor may receive:

```text
PlayerInput
  explicit reference to the outer Host PlayerInput

CharacterController
  explicit reference to the Actor-owned controller
```

Do not use a global lookup, object name, tag or first-found Player as binding authority.

### Route and Activity scene coverage

The accepted architecture and current runtime support Scene-Provided authoring in either:

```text
active Route Primary Scene
active Activity content scene set
```

The runtime consumes the exact Route/Activity lifecycle context supplied by Game Flow and checks the authored object's scene against those declared sources. It does not reconstruct authority through scene-wide discovery.

Consumer documentation must still distinguish:

```text
runtime path implemented
authoring validation passed
focused Play Mode admission/release passed
```

The FIRSTGAME repository tracks the manual consumer proof separately. Source presence alone is not a Play Mode pass.

### Release and persistent diagnostics

Activity exit and Route exit release the Scene-Provided admission. Activity Restart
performs that exit before a later reentry. Runtime teardown repeats the same release
contract safely: an already released admission is an explicit idempotent result, not
an error and never authorizes release of a different Host.

During Gameplay, inspect the **Scene-Provided Player Composer** for the active
admission, Host, Slot and Actor. After its scene unloads, select the persistent
`FrameworkRuntimeHost` and open **Advanced / Debug**. The `Scene-Provided
Admissions` projection records the last real operation, its source/reason/status,
whether release succeeded or was already complete, the typed Slot/Actor when
available, Host-evidence presence and the active/occupied counts after the operation.

This projection is diagnostic only. It does not retain scene objects, assign Slots,
admit Players or replace `PlayerParticipationRuntimeContext` authority. A missing
Hierarchy object is not sufficient proof of release; the persistent diagnostic is
the direct release evidence. `PlayerInput.playerIndex` is never a `PlayerSlotId`.

---

## Session-Persistent Logical Player

Application/Session composition may eventually provide a Logical Player outside any Route or Activity.

```text
Game Application / Session
  -> Session-Persistent Logical Player
  -> PlayerParticipationRuntimeContext

Route / Activity
  -> projects and consumes participation
  -> never owns Logical Player Session identity or lifetime
```

This source is not currently an available product workflow.

Do not simulate it by placing an arbitrary Player prefab in Persistent Content. A persistent GameObject alone does not establish:

```text
Logical Player admission
Slot assignment authority
physical ownership evidence
Actor correlation
contextual release policy
materialization reconciliation
```

The package still needs an explicit authoring surface, request/result contract, validation, runtime admission and QA proof before consumer use.

---

## Activity participation

For a Scene-Provided Player already containing its Logical Actor, the practical readiness levels are:

```text
Joined Slots
  proves Logical Player admission only

Logical Actors Prepared
  recommended baseline when the source already provides the Actor

Gameplay Ready
  additionally waits for gameplay, input and Camera eligibility
```

For a Manager-Provisioned Player, readiness may progress through Host provisioning, Actor selection/preparation and later gameplay eligibility.

`Explicit Slots + Allowed` is valid when zero participants are temporarily permitted. `No Slots` still requires the zero-participant policy to be Allowed.

## Pause integration

Physical Pause input belongs to the official Player:

```text
PlayerInput
UnityPlayerInputGateAdapter
PausePlayerInputBinding
```

`Global` is an action map of that `PlayerInput`, not a second global Player.

`PauseRequestTrigger` is not a Player component. It may live in Persistent Content, Route scenes or Activity scenes and receives its request port from the corresponding composition lifecycle.

Authored buttons can apply application-only Pause without an active Player binding. In that mode the framework changes logical Pause, TimeScale and presentation but does not modify action maps.

Therefore:

```text
Escape / Gamepad Start
  requires official Player binding

Pause / Resume / Toggle Button
  does not require a Player
  requires a composed PauseRequestTrigger
```

See `Pause-Usage.md` and `../Current/Guides/Pause-Input-Authoring.md`.

## Gameplay Camera integration

A Player gameplay Camera is separate from the persistent physical Camera Output.

Inside the Actor hierarchy:

```text
PlayerActorDeclaration
PlayerGameplayCameraAuthoring
Camera targets
CameraRigComposer
local Cinemachine Camera materialization
```

Persistent Content owns the physical output and request arbitration surface. The Player publishes contextual eligibility/request evidence and releases it with its scope.

See `../Current/Guides/Player-Gameplay-Camera-Authoring.md`.

## Diagnose

Inspect these as separate evidence:

```text
Slot allocation and current assignment
Logical Player admission
Host identity and physical ownership
Actor selection
current Actor correlation
Logical Actor preparation/adoption
input eligibility
Camera eligibility
Activity gameplay readiness
```

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

For Manager-Provisioned joins, additionally inspect reservation, physical provisioning, commit/rollback and Slot reuse evidence.

Authoring validity does not prove runtime admission.

For Pause, distinguish:

```text
PauseRequestTrigger.ProductRequestBindingStatus
PauseRequestTrigger.LastProductStatus
PauseRequestTrigger.LastExecutionMode
PausePlayerInputBinding.BindingStatus
```

A bound Trigger does not imply that a Player binding exists; it may legitimately execute in `ApplicationOnly` mode.
