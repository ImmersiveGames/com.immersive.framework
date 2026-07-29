# Player Usage

Status: Current  
Last updated: 2026-07-28  
Validated reference: `PLAYER-DIAG-1`

## 1. Product model

The framework owns one Session-scoped Player participation authority:

```text
PlayerParticipationRuntimeContext
```

A local Player composition is not one monolithic object. Keep these facts separate:

```text
Player Slot
  stable logical seat configured by the Game Application

Local Player Host
  physical local input host, normally containing PlayerInput

Logical Player admission
  association between one Host and one Player Slot

Actor selection
  selected ActorProfile for a joined Slot

Logical Actor
  prepared, instantiated or adopted Actor identity

Gameplay eligibility
  input, Camera and gameplay-action evidence required by an Activity
```

`PlayerInput.playerIndex`, join order, hierarchy order and object name are not `PlayerSlotId`.

## 2. Choose the Logical Player source

| Source | Use when | Product status |
|---|---|---|
| Manager-Provisioned | an explicit join creates a physical Host through `PlayerInputManager` | Implemented |
| Scene-Provided | a Route or Activity scene already contains the Host and Logical Actor | Implemented and FIRSTGAME-validated |
| Session-Persistent | Logical Player identity must outlive Route and Activity scopes | Accepted architecture; not implemented |

All implemented sources converge into the same Session `PlayerParticipationRuntimeContext` and typed `PlayerSlotId` authority.

## 3. Configure shared participation

1. Create stable `PlayerSlotProfile` assets.
2. Add them to `GameApplicationAsset` in allocation order.
3. Choose the duplicate Actor-selection policy explicitly.
4. Configure each `ActivityAsset`:
   - participation projection;
   - zero-participant policy;
   - readiness requirement.
5. Use the lowest readiness level the Activity genuinely requires.

Readiness is cumulative:

```text
Joined Slots
→ Selected Actors
→ Logical Actors Prepared
→ Gameplay Ready
```

For a Scene-Provided Player that already contains its Actor, `Logical Actors Prepared` is usually the minimum useful gameplay baseline. Use `Gameplay Ready` only when the Activity must also wait for input, Camera and gameplay eligibility.

---

## 4. Scene-Provided Logical Player

Use this source when the Route Primary Scene or an Activity content scene already owns the physical Host and its Actor.

### 4.1 Canonical prefab boundaries

Actor prefab:

```text
Actor_PlayerSceneProvided
  PlayerActorDeclaration
  CharacterController or other gameplay components
  movement component
  Camera targets
  Visual
```

The Actor prefab does not contain `PlayerInput`.

Outer Player prefab:

```text
Player_SceneProvided
  PlayerInput
  LocalPlayerHostAuthoring
  SceneLocalPlayerAdmissionAuthoring

  Actor Mount
    Actor_PlayerSceneProvided
```

The component class remains `SceneLocalPlayerAdmissionAuthoring`. Its Inspector is displayed as **Scene-Provided Player Composer**.

### 4.2 Actor Profile

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

### 4.3 Local Player Host

On the outer Player root:

```text
Player Input
  same-root PlayerInput

Actor Mount
  explicit Actor Mount child
```

A Scene-Provided Host may already contain one authored Actor under Actor Mount. The empty Actor Mount rule belongs to the Manager-Provisioned source.

### 4.4 Scene-Provided Player Composer

Configure:

```text
Participation
  Player Slot Profile
    exact Slot used by the Activity

Logical Actor
  Actor Profile
    profile referencing Actor_PlayerSceneProvided

  Scene Logical Player Actor
    PlayerActorDeclaration from the nested Actor

Admission
  Admission Timing
    On Activity Enter
```

`Local Player Host` is resolved from the same GameObject. It is not a free-form cross-scene reference.

### 4.5 Apply and validate

Run:

```text
Apply / Rebuild
Validate
```

`Apply / Rebuild` may:

- validate the same-root Host;
- validate the Actor under the exact Actor Mount;
- compare the nested prefab source with `ActorProfile.LogicalActorHostPrefab`;
- store typed authoring evidence.

It does not:

- reserve a Slot;
- assign a runtime Actor occurrence;
- start gameplay;
- create or destroy the Host;
- create or destroy the Actor;
- replace runtime admission.

Expected authoring result:

```text
Scene-Provided Player authoring and internal profile evidence are valid.
```

### 4.6 Route and Activity coverage

Current runtime supports Scene-Provided authoring in:

```text
active Route Primary Scene
active Activity content scene set
```

The runtime consumes the explicit Route/Activity lifecycle context supplied by Game Flow. It does not infer authority from name, tag, first-found object or `Camera.main`.

### 4.7 Runtime lifecycle

Expected lifecycle:

```text
Activity enter
→ resolve exact authored Host, Slot and Actor evidence
→ admit the Scene-Provided Player
→ join the configured Slot
→ adopt the existing Logical Actor
→ publish contextual input/Camera/gameplay evidence

Activity exit or Route exit
→ release Host evidence
→ release canonical Slot assignment
→ release the admission
→ remove contextual Actor/gameplay evidence

Activity Restart
→ execute Activity exit
→ execute reset policy
→ reenter the Activity
→ restore a valid active admission

Runtime teardown
→ perform safe best-effort release
→ treat an already completed release as explicit idempotence
```

A release token never authorizes releasing a different Host or Slot.

---

## 5. Persistent Scene-Provided diagnostics

### 5.1 During Gameplay

Inspect the **Scene-Provided Player Composer**:

```text
Runtime = Ready
Admission = Admitted
Host Joined = true
Active Admission = true
Player Slot ID = PlayerSlot:player.1
Actor Ownership = ExternalSceneOwned
Adoption Status = SucceededAdopted
```

This surface explains the active scene composition. It is not the runtime authority.

### 5.2 After scene unload

Select the persistent `FrameworkRuntimeHost` and open:

```text
Advanced / Debug
  Scene-Provided Admissions
```

The projection is a Session-local immutable snapshot of the last real operation. It does not retain `GameObject`, `Component`, `Transform`, `PlayerInput` or other scene references.

### 5.3 Field semantics

| Field | Meaning |
|---|---|
| Active Count | active Scene-Provided admissions after the recorded operation |
| Occupied Slot Count | joined/occupied Slots after the recorded operation |
| Last Operation | last admit or release operation |
| Last Status | typed result such as `SucceededAdmitted`, `SucceededReleased` or `SucceededAlreadyReleased` |
| Last Slot | typed Slot involved in the operation, when valid |
| Last Actor | stable authored `PlayerActorDeclaration.ActorId`; it is not necessarily the Activity-scoped runtime occurrence ID shown in detailed logs |
| Last Source / Reason | caller and operation reason |
| Release Succeeded | the recorded release completed normally |
| Already Released | the release request was idempotent because the admission was already complete |
| Host Evidence Present | Host evidence present after the operation; normally `Yes` after admit and `No` after release |

Counts and Host-evidence presence describe the state **after** the recorded operation.

### 5.4 Expected active snapshot

```text
Active Count = 1
Occupied Slot Count = 1
Last Operation = AdmitSceneLocalPlayer
Last Status = SucceededAdmitted
Last Slot = PlayerSlot:player.1
Release Succeeded = No
Already Released = No
Host Evidence Present = Yes
```

### 5.5 Expected released snapshot

```text
Active Count = 0
Occupied Slot Count = 0
Last Operation = ReleaseSceneLocalPlayer
Last Status = SucceededReleased
Last Slot = PlayerSlot:player.1
Release Succeeded = Yes
Already Released = No
Host Evidence Present = No
```

Expected duplicate-release result:

```text
Last Status = SucceededAlreadyReleased
Release Succeeded = No
Already Released = Yes
```

A missing Hierarchy object is not sufficient proof of release. Use the persistent snapshot.

### 5.6 Validated manual matrix

The FIRSTGAME Scene-Provided baseline is approved for:

```text
Menu → Gameplay → Menu → Stop

Menu → Gameplay → Menu → Gameplay → Menu → Stop

Menu → Gameplay → Activity Restart → Menu → Stop
```

The manual validation confirmed:

- one active admission during Gameplay;
- Slot `player.1`;
- active Host evidence;
- zero active admissions after release;
- zero occupied Slots after release;
- valid reentry/readmission after Activity Restart;
- no `ArgumentException` during runtime teardown.

The current QA formatting regression is an Editor menu smoke:

```text
Immersive Framework
  QA
    Regressions
      Game Flow
        Run Player Host Evidence Diagnostic Formatting Smoke
```

It is not expected to appear as an NUnit Test Runner test.

---

## 6. Manager-Provisioned Logical Player

Use this source when an explicit join request must create the physical Host.

### 6.1 Persistent Content composition

Configure one explicit provisioning surface:

```text
PlayerInputManager
LocalPlayerProvisioningAuthoring
LocalPlayerProvisioningHostRegistration
```

`PlayerInputManager` must use the framework-authorized manual join path. Do not enable an independent automatic join lane that bypasses Slot reservation.

### 6.2 Player prefab

```text
PlayerInput
LocalPlayerHostAuthoring
empty Actor Mount
```

Do not pre-author `PlayerSlotId` on the prefab. Runtime admission binds the official reserved Slot.

### 6.3 Join transaction

```text
authorized join request
→ reserve first configured free Slot
→ PlayerInputManager creates one Host
→ validate Host
→ admit Logical Player
→ commit typed Slot assignment
→ select/prepare Actor
→ commit or explicit rollback
```

Required negative proof:

- missing provisioning registration blocks explicitly;
- invalid prefab blocks explicitly;
- failed physical join releases reservation;
- failed committed join releases Host evidence and owned physical content;
- `playerIndex` is never used as `PlayerSlotId`;
- exit permits later Slot reuse.

FIRSTGAME should test this source in a separate Route and scene so the UX comparison does not change movement, Camera, Pause or reset composition at the same time.

---

## 7. Session-Persistent Logical Player

This source is not currently available as a product workflow.

Do not simulate it by placing an arbitrary Player prefab in Persistent Content. A persistent GameObject alone does not establish:

- Logical Player admission;
- Slot assignment authority;
- physical ownership;
- Actor correlation;
- contextual release;
- materialization reconciliation.

Wait for an official package authoring/runtime cut.

---

## 8. Pause integration

Physical Pause input belongs to the official Player:

```text
PlayerInput
UnityPlayerInputGateAdapter
PausePlayerInputBinding
```

`Global` is an action map of that PlayerInput, not a second global Player.

Application-only authored Pause controls may work without an admitted Player binding. See `Pause-Usage.md`.

---

## 9. Gameplay Camera integration

A Player gameplay Camera request is separate from the persistent physical Camera Output.

Inside the Actor hierarchy:

```text
PlayerActorDeclaration
PlayerGameplayCameraAuthoring
Camera targets
CameraRigComposer
local Cinemachine Camera materialization
```

Persistent Content owns the physical output and request arbitration. The Player publishes contextual eligibility and releases it with its scope.

See `../Current/Guides/Player-Gameplay-Camera-Authoring.md`.

---

## 10. Diagnose in the correct order

Inspect these as separate evidence:

```text
Slot configuration and current assignment
Logical Player admission
physical Host identity
Actor selection
current Actor correlation
Logical Actor preparation/adoption
input eligibility
Camera eligibility
gameplay eligibility
Activity readiness
```

Do not infer runtime success from authoring validity alone.

## 11. Anti-patterns

Do not add:

- static host access;
- global service locators;
- scene-wide name/tag lookup;
- `FindObjectOfType` admission authority;
- `playerIndex` to Slot conversion;
- silent fallback to another Slot;
- automatic Actor replacement;
- a second physical Camera output;
- hidden release repair;
- diagnostic snapshots as a second authority.
