# Player Usage

Status: **Current Player product/runtime surface — ADR-023 composition certified / ADR-023A identity boundary reconciled**  
Last updated: **2026-08-31**  
Decision sources: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021, IF-ADR-023, IF-ADR-023A  
Actor-selection closure: [IF-ADR-015B — 2026-08-26](../Architecture/Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)  
Actor-runtime certification: [IF-ADR-023 — 2026-08-29](../Architecture/Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)  
Occurrence-identity reconciliation: [IF-ADR-023A — 2026-08-31](../Architecture/Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)  
Current delivery authority: [IF-TRACK — Immersive Framework](../Architecture/Tracking/IF-TRACK-Framework.md)

## 1. Product model

Keep Session intent, Slot identity, Local Player Host, Actor selection, Actor runtime composition and Activity context separate.

```text
PlayerSessionProfile
  Session initialization / provisioning policy

PlayerSlotProfile
  authored Slot identity/configuration

LocalPlayerHostAuthoring
  technical Player host
  PlayerInput boundary
  ActorMount

PlayerActorRuntimeHost
  reusable Actor-independent runtime shell
  PlayerActorDeclaration
  PresentationMount

ActorProfile
  Actor identity/classification
  PresentationPrefab

Activity Player participation policy
  projection
  readiness requirement
  optional relocation
```

Canonical transaction split:

```text
Join
!= Actor Selection
!= Activity Actor Preparation
!= Physical Materialization
```

## 2. Current Actor composition — IF-ADR-023 / IF-ADR-023A

```text
Local Player Host
├── PlayerInput
└── ActorMount
    └── PlayerActorRuntimeHost
        ├── PlayerActorDeclaration
        └── PresentationMount
            └── ActorProfile.PresentationPrefab
```

The Local Player Host composition owns reusable runtime infrastructure. `ActorProfile` owns Actor-specific presentation.

Do not restore `ActorProfile.LogicalActorHostPrefab`, a second Actor runtime prefab authority, or fallback from `PresentationPrefab` to removed serialization.

`LogicalActorsPrepared` remains valid current readiness terminology. It does not mean the old `LogicalActorHost` hierarchy is current.

### Player Actor occurrence identity

`PlayerActorDeclaration.ActorId` is a runtime physical occurrence identity, not a persistent prefab/template identity.

Canonical states:

```text
AUTHORED / UNPREPARED
  PlayerActorDeclaration.actorId = empty
  no valid typed occurrence ActorId

        ↓ physical preparation

IDENTITY ESTABLISHED / PREPARING
  runtime generates one occurrence ActorId
  PlayerActorDeclaration receives it
  typed ActorId becomes valid

        ↓ commit

PREPARED / COMMITTED
  physical preparation evidence is retained
  downstream runtime/gameplay may consume ActorId
```

For reusable Player Actor prefabs:

```text
PlayerActorDeclaration.ActorId
  authored template value = empty
  runtime value = physical occurrence identity
```

Do not generate a persistent Player Actor ID in the prefab. Do not use `ActorProfileId`, `PlayerSlotId`, GameObject name or another identity as a substitute.

`ActorId` and `FrameworkIdentityValue` remain strict: empty typed identity is invalid. Runtime consumers must not request typed Player Actor occurrence identity before physical preparation establishes it.

This rule is specific to `PlayerActorDeclaration`. Ordinary persistent `ActorDeclaration` keeps its existing persistent authored-ID requirement.

## 3. Scene-Provided Local Player

Two Editor operations serve different authoring jobs and must not be confused.

### Create a complete Scene-Provided Local Player

Use:

```text
GameObject
  > Immersive Framework
    > Player
      > Scene-Provided
        > Create Local Player
```

This action creates a complete Scene-Provided Local Player composition. Use it when starting from no existing Local Player Host.

Canonical full-creator shape:

```text
Scene-Provided Local Player
├── PlayerInput
├── LocalPlayerHostAuthoring
├── SceneProvidedLocalPlayerAuthoring
├── UnityPlayerInputGateAdapter
└── ActorMount
```

### Add Scene-Provided behavior to an existing Local Player Host

For direct component authoring, use:

```text
Add Component
  > Immersive Framework
    > Player
      > Scene-Provided
        > Local Player
```

When a reusable Local Player Host already exists, the Scene-Provided module may be authored on a child object and reference the ancestor Host.

Canonical reusable-variant shape:

```text
SceneProvidedPlayer
├── PlayerInput
├── LocalPlayerHostAuthoring
├── UnityPlayerInputGateAdapter
├── ActorMount
└── Scene-Provided Local Player
    └── SceneProvidedLocalPlayerAuthoring
```

Do not invoke the full `Create Local Player` action inside an existing Local Player Host merely to add Scene-Provided behavior; that creates another full Player composition instead of the module boundary.

The Scene-Provided composition may author the candidate `PlayerActorRuntimeHost` and selected Presentation. Runtime validates/adopts the exact deterministic composition and transfers successful physical Player lifetime to the Session occurrence.

`SceneProvidedLocalPlayerAuthoring` normally uses Activity lifecycle admission. Do not add a manual Join merely to compensate for ordinary Scene-Provided Activity entry.

During physical adoption, the authored `PlayerActorDeclaration` may still have an empty stored ActorId. The Scene-Provided physical preparation owner establishes the runtime occurrence identity before any consumer is allowed to require typed `ActorId`.

Framework authoring uses `LocalPlayerProvisioningAuthoring.LocalPlayerHostPrefab`.
`PlayerInputManager.playerPrefab` remains the Unity Input System property materialized
from that explicit framework intent; it is not a second framework prefab authority.

Scene-Provided and Manager-Provisioned are separate provisioning origins. They converge after successful admission; there is no silent fallback between modes.

## 4. Manager-Provisioned

Manager-Provisioned uses Session-authorized provisioning rather than a pre-authored Local Player Host instance.

```text
PlayerSessionProfile
  HostProvisioning = ManagerProvisioned
        ↓
Player provisioning authority
        ↓
Join
        ↓
Local Player Host + PlayerInput
        ↓
Slot Joined / technical Host evidence
        ↓
Actor selection
        ↓
Activity requires Actor preparation
        ↓
PlayerActorRuntimeHost
        ↓
ActorProfile.PresentationPrefab
        ↓
physical Player Actor occurrence identity established
        ↓
Activity preparation / relocation
        ↓
GameplayReady
```

Immediate Join is not Actor materialization. A newly joined Manager Player may legitimately expose `AssignmentOrigin=None` before contextual Activity preparation/reprojection.

Manager-Provisioned and Scene-Provided use different physical origins but obey the same identity invariant: typed `PlayerActorDeclaration.ActorId` is unavailable before occurrence establishment and valid after it.

## 5. Activity Player readiness

Current ordered levels remain:

```text
None
JoinedSlots
SelectedActors
LogicalActorsPrepared
GameplayReady
```

`LogicalActorsPrepared != GameplayReady`.

Current meaning:

```text
LogicalActorsPrepared
  required physical Player Actor occurrences are selected/prepared for the Activity projection

GameplayReady
  current contextual gameplay projection is established over retained prepared Session Players
```

`GameplayReady` does **not** by itself certify game-specific Presentation functionality such as locomotion, a concrete gameplay input consumer, camera composition or character visuals. Those remain gameplay-owned composition concerns.

If gameplay consumes current gameplay input/camera authority, request `GameplayReady`. Do not auto-promote readiness because a consumer happens to require a higher level, and do not treat readiness success as proof that every game-owned gameplay feature has been authored.

FIRSTGAME Scene-Provided Play Mode evidence on 2026-08-31 reached `Ready` with both `LogicalActorsPrepared` and `GameplayReady`, with one projected, selected and prepared Player and zero failures. That certifies the framework lifecycle/preparation contract, not the completeness of a game-owned First Person Presentation.

## 6. Gameplay input consumption

Gameplay-owned Actor code consumes the public current-gameplay binding:

```text
PlayerGameplayInputReader
  -> IPlayerGameplayInputReader
  -> GameplayReady
  -> TryReadValue<T>(InputActionReference, out value)
```

Do not bypass that binding with direct `InputActionReference.action.ReadValue<T>()`, Host hierarchy guesses, scene scans, names, tags, reflection or another fallback channel.

`PlayerInput` belongs to the Local Player Host. It is not Presentation authority.

The existence of `GameplayReady` does not create a concrete gameplay consumer automatically. The game still owns the Presentation/gameplay components that consume the public binding.

## 7. Player Session public surface

Use:

```text
PlayerSessionObserver = read
explicit Player Session command component = request/change
```

Current explicit command family:

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

Observation and requests are independently composable. The Observer is immutable presentation evidence, not a second Session state store.

## 8. Actor Selection

Actor selection is Session-owned logical intent for one exact Joined Slot.

```text
Select Actor
Select Default Actor
Replace Actor Selection
Clear Actor Selection
```

All return typed `PlayerActorSelectionResult` evidence.

`Replace` and `Clear` are logical selection operations before the preparation barrier. They are not prepared physical Actor hot-swap commands.

`LeaveUnresolved` is a valid Session policy for Character Selection. The game owns which choices are presented; the Framework owns validation, revision and commit.

## 9. Scoped access

Route and Activity are Framework lifecycle scopes:

```text
Route scope     = Route lifecycle ownership
Activity scope  = Activity lifecycle ownership
scene location  != scope authority
```

A component physically present in Route-discovered content may legitimately be Activity-scoped and bind while the Activity lifecycle is active.

Keep authoring validity separate from runtime access availability:

```text
TryValidateConfiguration()
  = authored configuration validity

BindingState / TryGetAccess
  = current runtime scoped-access availability
```

A valid authored consumer may temporarily be unbound and must fail closed without global fallback.

### Teardown

Unity teardown order is not access authority.

```text
consumer OnDestroy
  -> releases its local binding

persistent runtime owner destroyed later
  -> destroyed Unity consumer wrapper is tolerated
  -> no second release-side MissingReferenceException
  -> diagnostics do not dereference destroyed Unity objects
```

## 10. Spatial intent

IF-ADR-021 keeps spatial intent separate from Session lifetime:

```text
RoutePlayerSpatialEntryAuthoring
  RouteId + PlayerSlotId -> baseline anchor

ActivityPlayerRelocationAuthoring
  ActivityId + PlayerSlotId -> optional contextual anchor
```

Route spatial entry and Activity relocation do not Join, recreate or transfer Player lifetime.

Route spatial entry is resolved by Slot identity and applies pose to the physical Player Actor Transform. It does not require a pre-existing Player Actor occurrence `ActorId` merely to resolve/apply the authored spatial intent.

## 11. Character Selection

Canonical public flow:

```text
PlayerSessionProfile
  ActorResolution = LeaveUnresolved
        ↓
Join
  Slot Joined / Actor unresolved
        ↓
game-owned UI
        ↓
PlayerSessionSelectActorCommandTrigger
        ↓
Session Actor selection
        ↓
Activity Actor preparation
        ↓
PlayerActorRuntimeHost + selected Presentation
        ↓
GameplayReady
```

FIRSTGAME FG-ADR-002 Revision 4 records Character Selection as Play Mode proven.

## 12. Current certification

Current evidence is layered:

```text
Historical Full Player            25/25 preserved
Current aggregate                 27/27 PASS
Manager functional Player QA      14/14 PASS
Pause/Input/Gate composition       8/8 PASS
Scene-Provided identity boundary  FIRSTGAME Play Mode PASS
```

The 14-case consolidated functional run covers:

```text
access
join
observation
actor-default
actor-replace
actor-lifecycle
joining-control
second-player
commands
leave
rejoin
negatives
spatial
relocation
```

The QA harness explicitly shares the Editor keyboard for P2 Join/Rejoin. That proves deterministic technical provisioning in the one-keyboard Editor environment; it does not certify a production Local Multiplayer Slot/device/InputUser/control-scheme contract.

On 2026-08-31, FIRSTGAME Scene-Provided evidence additionally proved the post-IF-ADR-023A occurrence-identity boundary at both `LogicalActorsPrepared` and `GameplayReady` with `blockingIssues=0`.

## 13. Anti-patterns

Do not add:

- global Player manager/service locator;
- scene/hierarchy/name/tag lookup as authority;
- direct Session mutation from game UI;
- direct PlayerInput reads that bypass current gameplay binding;
- manual Join as fallback for normal Scene-Provided admission;
- hidden default Actor fallback;
- a second Player Actor runtime/presentation prefab authority;
- persistent authored `PlayerActorDeclaration.ActorId` for reusable Player Actor templates;
- pre-preparation typed `PlayerActorDeclaration.ActorId` reads;
- Presentation-owned Session/Slot/lifetime state;
- physical hot-swap hidden behind logical Replace;
- sample-owned Slot/device/input authority to bypass Local Multiplayer product gaps.

## 14. Future Player scope

Still outside the delivered public Player surface:

```text
exact-Slot public Join
public Slot/device/InputUser/control-scheme ownership observation
canonical Local Multiplayer device/input contract
consumer-facing prepared physical Actor hot-swap
```

Arbitrary Actor Selection is delivered and consumer-proven. Local Multiplayer remains a separate future product contract.
