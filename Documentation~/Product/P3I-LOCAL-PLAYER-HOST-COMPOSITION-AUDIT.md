# P3I — Local Player Host Composition Audit and Decision

Status: Completed / decision gate closed  
Date: 2026-07-13  
Type: Technical and product architecture audit  
Package: `com.immersive.framework`  
Dependencies: P3G manual local join; P3H Session Actor selection  
Decision gates: DG-P3-05; DG-P3-06

## 1. Objective

Determine how a selected `ActorProfile` completes the stable local Player object created by `PlayerInputManager`, without:

```text
creating a second local Player root
replacing PlayerInput/InputUser during Actor replacement
turning PlayerComposer into runtime authority
collapsing Presentation into Actor identity
moving Route/Activity lifetime into Session state
```

## 2. Scope

Audited areas:

```text
ActorDeclaration
PlayerActorDeclaration
ActorProfile
PlayerInputManager provisioning contract
LocalPlayerJoinResult evidence
PlayerSlotDeclaration
PlayerSlotOccupancy
PlayerComposer and editor Apply/Rebuild
LocalPlayerCameraRequestBinding
RuntimeContent request/result/handle/release protocol
Activity readiness vocabulary
```

## 3. Out of scope

```text
runtime implementation
QA smoke implementation
Presentation / Skin port
occupancy/input/camera orchestration
Activity admission integration
leave/disconnect/reconnect
FIRSTGAME integration
```

## 4. Current evidence

### 4.1 Join and selection are already independent

P3G proves:

```text
explicit manual join
Slot reservation before PlayerInputManager.JoinPlayer
real PlayerInput creation
Session Joined commit
rollback on provisioning failure
```

P3H proves:

```text
Joined Slot may remain Unselected
ActorProfile selection occurs through a later explicit operation
default Actor selection is not automatic
selection can be replaced or cleared before logical preparation
```

Therefore the selected Actor cannot be assumed when `PlayerInputManager` chooses its shared `playerPrefab`.

### 4.2 Current provisioning prefab is temporarily modeled as an Actor

The current P3G contract requires the `PlayerInputManager.playerPrefab` to contain:

```text
PlayerInput
PlayerActorDeclaration
```

The join result returns `PlayerActorDeclaration` as admission evidence.

This was sufficient to prove the Unity provisioning bridge, but it cannot remain the final composition boundary because `ActorProfile.LogicalActorHostPrefab` is a separate selectable prefab.

### 4.3 Current declaration hierarchy is divergent

Current implementation:

```text
ActorDeclaration : MonoBehaviour, IActor, sealed
PlayerActorDeclaration : MonoBehaviour, IActor, sealed
```

`PlayerActorDeclaration` duplicates generic Actor fields and descriptor logic.

Accepted product architecture requires:

```text
ActorDeclaration : MonoBehaviour, IActor
PlayerActorDeclaration : ActorDeclaration
```

P3J cannot create reliable generic Player Actors until this divergence is corrected.

### 4.4 PlayerComposer is editor-first

Current `PlayerComposer` explicitly owns:

```text
authoring intent
Editor Apply/Rebuild
technical bindings and anchors
```

It explicitly does not:

```text
spawn
join
execute gameplay
act as a runtime manager
```

Its Apply/Rebuild path uses Editor APIs and writes fixed authored Actor/Slot strings. It is unsuitable for runtime ActorProfile replacement.

### 4.5 Existing camera path is pre-wired to PlayerComposer

`LocalPlayerCameraRequestBinding` requires an explicit `PlayerComposer` and reads:

```text
PlayerSlotId
ActorId
CameraTarget
LookAtTarget
```

This is valid for pre-placed players but not yet for a logical Actor child created after join. P3K must bind camera requests to post-materialization target evidence without requiring a scene-authored Composer.

P3I does not change camera arbitration.

### 4.6 RuntimeContent is reusable but not sufficient alone

Existing RuntimeContent provides:

```text
explicit owner and scope
materialization request identity
transition guard
handle lifecycle
logical release
stale/foreign owner rejection
```

It deliberately does not resolve or instantiate Unity assets by itself.

P3J needs a typed Unity adapter capable of attaching the `ActorProfile` prefab under one explicit local Player host mount while registering the result through RuntimeContent.

## 5. Options evaluated

### Option A — ActorProfile chooses `PlayerInputManager.playerPrefab`

Verdict: Rejected.

```text
cannot support selection after join
unsafe for concurrent joins
manager prefab is shared mutable configuration
Actor replacement would replace PlayerInput/InputUser
```

### Option B — Provisioning root is the Actor; ActorProfile adds arbitrary child modules

Verdict: Rejected.

```text
ActorProfile.LogicalActorHostPrefab would no longer be the logical Actor host
Actor identity stays tied to the technical input shell
module application requires uncontrolled runtime composition or a new parallel asset shape
```

### Option C — RuntimeContent creates a second complete local Player root

Verdict: Rejected.

```text
duplicates PlayerInputManager provisioning authority
may create a second PlayerInput/InputUser
creates ambiguous Player root and Actor ownership
```

### Option D — Runtime invokes PlayerComposer Apply/Rebuild

Verdict: Rejected.

```text
Editor-only APIs
fixed authored identities
not a replacement transaction
not a scoped RuntimeContent materializer
```

### Option E — Stable provisioning host plus attached contextual Logical Actor

Verdict: Accepted.

```text
PlayerInputManager creates one stable input host
ActorProfile supplies one contextual logical Actor prefab
RuntimeContent tracks owner/identity/handle/release
specialized typed Unity adapter attaches the Actor under an explicit mount
binding contract connects PlayerInput and Slot evidence to PlayerActorDeclaration
Presentation remains separate
```

## 6. Accepted dependency graph

```text
GameApplication
  ordered PlayerSlotProfiles
  Actor selection policy

Session PlayerParticipationRuntimeContext
  Joined Slot
  selected ActorProfile
  logical preparation summary only

PlayerInputManager
  creates LocalPlayerHost
    PlayerInput
    LocalPlayerHostAuthoring
    explicit ActorMount

Route/Activity owner
  explicit RuntimeScopeContext
  requests selected Actor preparation

Player Actor materialization runtime
  validates Slot + Profile + owner + LocalPlayerHost
  invokes attached Unity prefab adapter
  registers RuntimeContent handle
  configures PlayerActorDeclaration : ActorDeclaration
  records logical preparation summary

P3K later
  occupancy
  gameplay input
  camera request
  GameplayReady evidence
```

## 7. Component classification

| Current or planned element | Fixed Host | Actor Logical Composition | Presentation | Context Binding | Game-owned Optional |
| --- | ---: | ---: | ---: | ---: | ---: |
| `PlayerInputManager` | provisioner |  |  |  |  |
| `PlayerInput` | yes |  |  |  |  |
| `LocalPlayerHostAuthoring` | yes |  |  |  |  |
| Actor mount Transform | yes |  |  |  |  |
| `PlayerSlotDeclaration` after join | yes |  |  |  |  |
| input gate adapter | optional |  |  | yes |  |
| `ActorProfile` | intent | intent |  |  |  |
| `PlayerActorDeclaration` |  | yes |  |  |  |
| `ActorDeclaration` generic authority |  | yes |  |  |  |
| movement motor |  | may live here |  |  | yes |
| combat/abilities/attributes |  | may live here |  |  | yes |
| reset endpoints |  | may live here |  | yes | yes |
| camera targets |  | yes |  | consumed later |  |
| `LocalPlayerCameraRequestBinding` |  |  |  | yes |  |
| `PlayerSlotOccupancy` |  |  |  | yes |  |
| Presentation/Skin |  |  | yes |  |  |
| `PlayerComposer` | editor authoring | editor authoring |  | editor materialization |  |

## 8. Required contract corrections

### Actor declarations

```text
ActorDeclaration must become inheritable.
PlayerActorDeclaration must inherit ActorDeclaration.
PlayerActorDeclaration must stop duplicating generic identity.
PlayerInput evidence must be explicitly injected from the provisioning host.
```

### Local join evidence

Current:

```text
LocalPlayerJoinResult.PlayerActorDeclaration
```

Required final shape:

```text
LocalPlayerJoinResult.LocalPlayerHost
LocalPlayerJoinResult.PlayerInput
LocalPlayerJoinResult.PlayerSlot snapshot
```

A successful join proves the stable host, not that a contextual logical Actor is prepared.

### ActorProfile validation

For Player Profiles:

```text
Logical Actor Host contains exactly one PlayerActorDeclaration.
Logical Actor Host does not contain PlayerInput.
PlayerActorDeclaration is valid as a runtime-bindable Actor template.
```

The shared local Player host prefab separately validates `PlayerInput` and its actor mount.

## 9. Idempotency and replacement

### Prepare

Same Slot + Profile + owner + current handle:

```text
AlreadyPrepared
no new object
no new ActorId
no revision change
```

### Release

Repeated release:

```text
AlreadyReleased
no exception
no hidden lookup
no stale Session preparation evidence
```

### Replace

Replacement is staged before the current Actor is released.

The old Actor and selection remain authoritative until the new Actor has passed:

```text
prefab instantiation
one-declaration validation
runtime ActorId configuration
PlayerInput/Slot binding
RuntimeContent handle validation and registration
```

Failure releases only staged content and preserves the old state.

## 10. Product surface affected

P3J will introduce or revise:

```text
PlayerInputManager player prefab
  LocalPlayerHostAuthoring inspector
  explicit PlayerInput
  explicit Actor Mount
  host validation

ActorProfile inspector
  canonical Logical Actor Host
  Player-host compatibility summary
  no fixed runtime ActorId

Advanced/Debug
  joined Slot
  selected ActorProfile
  local host evidence
  owner scope
  ActorId
  materialization handle/state/revision
  rollback/release diagnostics
```

## 11. Expected use flow

```text
Designer creates one reusable Local Player Host prefab.
Designer assigns it to PlayerInputManager.
Designer creates several ActorProfiles with distinct Logical Actor Host prefabs.
Player joins and receives a stable Slot and PlayerInput host.
Player may remain without an Actor while in lobby/selection UI.
Product selects an ActorProfile.
Route/Activity explicitly prepares the selected logical Actor.
Framework attaches and binds it below the stable host.
Later Route/Activity release removes only the contextual Actor.
Explicit leave later removes the stable local Player host.
```

## 12. P3J implementation file map

Candidate final areas:

```text
Runtime/Actors/
  ActorDeclaration.cs
  PlayerActorDeclaration.cs

Runtime/PlayerParticipation/Authoring/
  LocalPlayerHostAuthoring.cs

Runtime/PlayerParticipation/Contracts/
  PlayerActorMaterializationOperationId.cs
  PlayerActorMaterializationStatus.cs
  PlayerActorMaterializationRequest.cs
  PlayerActorMaterializationResult.cs
  PlayerActorMaterializationSnapshot.cs

Runtime/PlayerParticipation/Runtime/
  PlayerActorMaterializationRuntime.cs
  PlayerActorMaterializationRuntimeHostModule.cs
  AttachedPlayerActorPrefabMaterializationAdapter.cs

Runtime/PlayerParticipation/Runtime existing:
  LocalPlayerProvisioningBridge.cs
  LocalPlayerProvisioningRuntimeHostModule.cs
  PlayerParticipationRuntimeContext.cs
  PlayerParticipationRuntimeHostModule.cs

Editor/PlayerParticipation/
  LocalPlayerHostAuthoringEditor.cs
  LocalPlayerHostAuthoringValidator.cs
  PlayerActorSelectionAuthoringValidator.cs

Documentation~/Product/
  P3J manifests and short usage guide
```

Exact splitting may be refined to avoid duplicate helpers already present in `Runtime/Common`.

## 13. Technical smoke expected for P3J

```text
stable host joins without a logical Actor
PlayerInputManager remains sole host provisioner
selected Profile materializes one child Actor
PlayerActorDeclaration inherits ActorDeclaration
generic Actor discovery sees Player Actors
same prepare is idempotent
foreign/stale handle rejected
two Slots create independent Actor instances
replacement preserves old Actor on staging failure
successful replacement releases old Actor in order
context release removes Actor but preserves Joined host
no RuntimeContent handle leaks
no second PlayerInput created
```

## 14. Technical acceptance

```text
compiles
P3G join correlation and rollback remain valid after host-evidence migration
P3H selection remains immutable and explicit
no silent Actor default
no runtime reflection
no hierarchy/name/tag lookup
no second local Player root
explicit failure and rollback evidence
RuntimeContent owner and handle contracts preserved
```

## 15. Product acceptance

```text
user authors one shared local Player host
user authors several reusable ActorProfiles
join can complete before Actor selection
selection visibly prepares the expected Actor
replacement does not re-pair devices or recreate PlayerInput
Inspector separates host, Actor and Presentation responsibilities
Advanced/Debug exposes current materialization evidence
```

## 16. Architectural gain

```text
separates local input-user lifetime from gameplay Actor lifetime
preserves ActorProfile as canonical logical content
closes the declaration inheritance divergence
reuses RuntimeContent instead of adding a global Player spawner
creates an explicit path toward LogicalActorsPrepared readiness
```

## 17. Usability gain

```text
one manager prefab supports many selectable Actors
lobby and character-selection flows no longer require gameplay Actors
single-player default and multiplayer selection use the same preparation path
Actor replacement is understandable and diagnostic
```

## 18. Files in this cut

Created:

```text
Documentation~/Product/ADR-PROD-0014-local-player-provisioning-host-and-contextual-logical-actor-composition.md
Documentation~/Product/P3I-LOCAL-PLAYER-HOST-COMPOSITION-AUDIT.md
```

Altered: none.  
Removed: none.  
Runtime/Editor/QA changes: none.

## 19. Decision result

```text
DG-P3-05: CLOSED
  fixed host, Actor composition, Presentation and context bindings classified

DG-P3-06: CLOSED
  accepted mechanism = stable PIM host + RuntimeContent-tracked attached Actor prefab + typed host binding
```

## 20. Suggested commit

```text
P3I — decide Local Player host and Actor composition boundary
```
