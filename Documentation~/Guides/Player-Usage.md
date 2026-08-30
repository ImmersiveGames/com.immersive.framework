# Player Usage

Status: **Current Player product/runtime surface — ADR-023 composition certified**  
Last updated: **2026-08-29**  
Decision sources: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021, IF-ADR-023  
Actor-selection closure: [IF-ADR-015B — 2026-08-26](../Architecture/Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)  
Actor-runtime certification: [IF-ADR-023 — 2026-08-29](../Architecture/Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)  
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

## 2. Current Actor composition — IF-ADR-023

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

## 3. Scene Player / Scene-Provided

Use the official Editor action:

```text
GameObject
  > Immersive Framework
    > Player
      > Create Scene-Provided Local Player
```

Canonical technical shape:

```text
Scene-Provided Local Player
├── PlayerInput
├── LocalPlayerHostAuthoring
├── SceneLocalPlayerAdmissionAuthoring
├── UnityPlayerInputGateAdapter
└── ActorMount
```

The Scene-Provided composition may author the candidate `PlayerActorRuntimeHost` and selected Presentation. Runtime validates/adopts the exact deterministic composition and transfers successful physical Player lifetime to the Session occurrence.

`SceneLocalPlayerAdmissionAuthoring` normally uses Activity lifecycle admission. Do not add a manual Join merely to compensate for ordinary Scene-Provided Activity entry.

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
Activity preparation / relocation
        ↓
GameplayReady
```

Immediate Join is not Actor materialization. A newly joined Manager Player may legitimately expose `AssignmentOrigin=None` before contextual Activity preparation/reprojection.

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

If gameplay consumes current gameplay input/camera authority, request `GameplayReady`. Do not auto-promote readiness because a consumer happens to require a higher level.

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

## 13. Anti-patterns

Do not add:

- global Player manager/service locator;
- scene/hierarchy/name/tag lookup as authority;
- direct Session mutation from game UI;
- direct PlayerInput reads that bypass current gameplay binding;
- manual Join as fallback for normal Scene-Provided admission;
- hidden default Actor fallback;
- a second Player Actor runtime/presentation prefab authority;
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
