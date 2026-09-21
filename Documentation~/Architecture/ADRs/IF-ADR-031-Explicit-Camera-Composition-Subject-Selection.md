# IF-ADR-031 — Explicit Camera Composition Subject Selection

Status: **Accepted**
Proposed: **2026-09-21**
Accepted: **2026-09-21**
Type: architecture / Camera composition / Subject selection / Player integration
Reconciles: IF-ADR-026, IF-ADR-029 and IF-ADR-030
Preserves: IF-ADR-028 physical split-screen ownership and Player Slot → Camera Output integration

Implementation:
- CAMERA-031-A — **Implemented**
- CAMERA-031-B — **Implemented**
- CAMERA-031-C — **Not Started**

Unity tested: **NO**
QAFramework tested: **NO**
Consumer integrated: **NO**
Technically validated: **NO**
Certified: **NO**

## 1. Context

IF-ADR-029 establishes the current Camera chain:

```text
Camera Subject(s)
  -> Camera Composition
  -> CameraRigComposer
  -> CameraRequest
  -> CameraOutputSession
  -> Camera Output
```

The current `CameraSharedComposition` supports one Subject selection policy:

```text
AllAvailableSubjects
```

That policy is correct for a shared Group Camera because every currently available Subject
belongs to the same presentation.

A new consumer now requires a different composition:

```text
Character Selection
  -> local multiplayer
  -> two Player Slots
  -> independent Actor selection per Slot
  -> independent Third Person Camera per Player
  -> two explicit Camera Outputs
  -> PlayerInputManager automatic split-screen
```

The desired physical result is:

```text
Player Slot P1
  -> current prepared Actor occurrence
  -> current Camera Subject P1
  -> Composition P1
  -> Third Person Rig P1
  -> Output P1
  -> Unity Camera P1

Player Slot P2
  -> current prepared Actor occurrence
  -> current Camera Subject P2
  -> Composition P2
  -> Third Person Rig P2
  -> Output P2
  -> Unity Camera P2
```

`AllAvailableSubjects` cannot represent this because both compositions would consume both
Subjects.

Camera core must not solve the problem by learning about:

```text
PlayerSlotId
PlayerInput
Player index
ActorProfile
device ownership
Player hierarchy
```

The missing boundary is therefore not a Player-aware Camera policy. It is an explicit,
occurrence-safe Camera-domain Subject selection input for one Composition.

## 2. Decision

> **A Camera Composition may select either all currently available Subjects or an explicitly supplied ordered set of current Camera Subject identities.**

The accepted selection modes are:

```text
AllAvailableSubjects
ExplicitSelection
```

`ExplicitSelection` consumes Camera-domain evidence only.

The Composition never interprets Player identity and never derives Subject identity from
names, hierarchy, indices or Actor metadata.

## 3. Camera-domain explicit selection evidence

Introduce a narrow Camera-domain selection source for one Composition.

Conceptually:

```text
ICameraCompositionSubjectSelectionSource
  ContextId
  Revision
  CurrentSnapshot
  SelectionChanged
```

Snapshot:

```text
CameraCompositionSubjectSelectionSnapshot
  ContextId
  Revision
  ordered CameraSubjectId[]
```

Required properties:

- immutable snapshot;
- deterministic Subject ordering;
- monotonically increasing revision;
- exact typed `CameraSubjectId` values;
- no Player, Actor or Input types;
- no Transform discovery;
- no string parsing of Subject IDs;
- no global registry.

The selection source does not make a Subject available.

It only states:

> these exact currently available Camera Subject identities are desired by this Composition.

Availability remains owned by `CameraSubjectAvailabilityContext`.

## 4. Composition semantics

`CameraSharedCompositionSubjectPolicyKind` gains:

```text
AllAvailableSubjects = 10
ExplicitSelection    = 20
```

For `AllAvailableSubjects`, behavior remains unchanged.

For `ExplicitSelection`:

```text
selection snapshot
  -> exact desired CameraSubjectId set
  -> intersect against current Camera Subject availability
  -> build current Composition membership
  -> project presentation input
  -> apply Rig
  -> publish/release CameraRequest
```

No fallback from `ExplicitSelection` to `AllAvailableSubjects` is allowed.

If the selection source is absent, invalid or stale, the Composition is not considered
correctly configured and must report explicit blocking evidence.

If a selected Subject is not currently available, that Subject is absent from presentable
input. Existing presentation cardinality rules decide whether the Composition remains
presentable.

For the first consumer:

```text
ThirdPerson
  -> exactly one selected/current Subject required

selected Subject unavailable
  -> ThirdPerson presentation unavailable
  -> Composition request released
  -> Output restores another winner or Default
```

A later fresh Subject occurrence is not the same selection merely because it belongs to the
same Player. The integration boundary must publish the fresh current `CameraSubjectId`.

## 5. Ownership and lifetime

Important state has one writer.

For one explicit Composition binding:

```text
selection source/context
  owner  = integration boundary that resolves the desired Camera Subject occurrence
  writer = exactly one runtime adapter
  reader = exactly one Camera Composition
```

The Composition:

- does not mutate external selection evidence;
- subscribes to selection revisions;
- owns its own membership and request lifetime as established by IF-ADR-029;
- releases its request when the selected evidence is no longer presentable.

The integration owner clears its current selection when its source occurrence ends.

Teardown must be idempotent.

## 6. Player → Camera integration boundary

Player-specific knowledge remains outside Camera core.

The first adapter is a narrow Session integration that maps explicit authored Player Slot
bindings to explicit Camera Compositions.

Authoring concept:

```text
PlayerCameraCompositionPolicyAuthoring

Binding
  PlayerSlotProfile
  CameraSharedComposition
```

The authoring relation means:

> the current Camera Subject produced by the prepared Actor occurrence of this Player Slot
> is the desired Subject for this exact Composition.

Runtime concept:

```text
Player Slot
  -> current prepared Actor occurrence
  -> current Player-Actor Camera Subject publication
  -> exact CameraSubjectId
  -> Camera-domain explicit selection source
  -> bound Camera Composition
```

The adapter may depend on Player and Camera because it is an integration boundary.

Camera core must not depend on Player types.

The adapter must consume exact current occurrence evidence already produced by
`PlayerActorCameraSubjectIntegrationRuntime`; it must not rediscover the Actor or
Presentation through scene lookup.

## 7. Relationship to Camera Output split-screen policy

This ADR does not replace or merge with `PlayerCameraOutputPolicyAuthoring`.

The two policies answer different questions.

```text
PlayerCameraOutputPolicyAuthoring
  Player Slot -> physical Camera Output

PlayerCameraCompositionPolicyAuthoring
  Player Slot -> Composition whose explicit Subject selection follows that Slot
```

For a two-Player Third Person split-screen consumer:

```text
P1
  -> Composition P1
  -> Output P1

P2
  -> Composition P2
  -> Output P2
```

Each relation remains explicit.

No Output is inferred from the Composition and no Composition is inferred from Output order.

## 8. Split-screen ownership remains unchanged

IF-ADR-028 remains authoritative:

```text
Framework Player/Camera integration
  -> PlayerInput.camera = exact Output Unity Camera

PlayerInputManager
  -> owns split count
  -> owns Camera.rect
  -> owns split recomposition
```

IF-ADR-031 does not add:

- Camera-domain viewport layout;
- `Camera.rect` writes;
- Player-count-to-Output inference;
- Player-index-to-Camera inference.

Subject selection and physical screen layout remain independent concerns.

## 9. Character Selection consumer

The first consumer is the Character Selection sample extended to local multiplayer.

Expected Session composition:

```text
PlayerSessionProfile_CharacterSelection
  -> Slot P1
  -> Slot P2
  -> HostProvisioning = ManagerProvisioned
  -> ActorResolution = LeaveUnresolved
```

Each Slot independently selects:

```text
Farmer
or
Cow
```

The same `ActorProfile` may be selected by both Slots because each prepared Actor occurrence
has independent runtime identity.

Expected Camera composition:

```text
P1 current Actor Subject
  -> explicit selection source P1
  -> ThirdPerson Composition P1
  -> ThirdPerson Rig P1
  -> Output P1

P2 current Actor Subject
  -> explicit selection source P2
  -> ThirdPerson Composition P2
  -> ThirdPerson Rig P2
  -> Output P2
```

Expected physical binding:

```text
PlayerCameraOutputPolicy
  P1 -> Output P1
  P2 -> Output P2

PlayerInputManager
  Split Screen = ON
```

## 10. Lifecycle requirements

Required behavior:

```text
P1 joins
  -> P1 WaitingForActorSelection
  -> no P1 Camera Subject selected yet
  -> Output P1 remains Default

P1 selects Actor
  -> fresh Actor occurrence
  -> fresh Camera Subject
  -> P1 selection source publishes exact SubjectId
  -> ThirdPerson Composition P1 becomes presentable
  -> Output P1 presents P1

P2 joins
  -> same independent lifecycle for P2

P2 selects Actor
  -> Composition P2 becomes presentable
  -> Output P2 presents P2
  -> PlayerInputManager displays both physical Cameras

P1 leaves
  -> P1 Actor occurrence ends
  -> P1 Subject becomes unavailable
  -> P1 explicit selection clears
  -> Composition P1 request releases
  -> P1 physical binding releases according to existing Player integration
  -> P2 remains valid

P1 rejoins
  -> fresh Actor occurrence required
  -> explicit Actor selection required again
  -> fresh CameraSubjectId only
  -> stale P1 Subject never reactivates
```

## 11. Transactional and stale-evidence requirements

Selection revisions participate in the existing reconciliation boundary.

A selection update must never produce:

```text
new membership
with old Rig targets

or

new Rig targets
with stale request/output state
```

Existing IF-ADR-029 transactional rollback remains authoritative for membership,
presentation and request/output mutation.

Additional requirement:

> a stale explicit-selection snapshot must not reactivate an older Subject occurrence.

Selection identity and revision checks must occur before mutating current Composition
membership.

## 12. Rejected alternatives

Rejected:

```text
add PlayerSlotId to CameraSubject
encode Player Slot in CameraSubjectId and parse the string
CameraSharedComposition policy values such as Player1 / Player2
select Subject by Player index
select Subject by ActorProfile
select Subject by GameObject name or hierarchy
use Camera.main
discover PlayerInput from Camera core
infer one Composition per Output order
merge Slot->Output and Slot->Composition into one ambiguous authority
create a global Camera/Player registry
make PlayerInputManager choose Camera Subjects
duplicate the Actor Camera Subject publication per Composition
```

Also rejected for this cut:

```text
generic tag/query language for Camera Subjects
arbitrary predicates over Subject metadata
new Camera Composition definition asset
global event bus
service locator
```

Those abstractions are not required by the current consumer.

## 13. Implementation cuts

### CAMERA-031-A — Camera explicit Subject selection

Status: **Implemented**

Required result:

```text
Camera-domain selection context/snapshot/source
ExplicitSelection policy
selection revision/stale protection
composition reconciliation from exact selected Subject IDs
AllAvailableSubjects behavior unchanged
```

### CAMERA-031-B — Player Slot adapter

Status: **Implemented**

Required result:

```text
explicit PlayerSlotProfile -> CameraSharedComposition authoring bindings
current prepared Actor occurrence -> current CameraSubjectId
single-writer selection publication
leave/replacement/rejoin clears or replaces exact selection
no Camera-core Player dependency
```

### CAMERA-031-C — Character Selection split-screen consumer

Status: **Not Started**

Required result:

```text
two configured Player Slots
independent Farmer/Cow selection per Slot
two explicit Camera Outputs
two independent ThirdPerson Compositions/Rigs
PlayerCameraOutputPolicy P1->OutputP1 / P2->OutputP2
PlayerInputManager split-screen enabled
join / select / leave / rejoin lifecycle proven
```

## 14. Validation requirements

Framework Edit Mode coverage:

```text
ExplicitSelection accepts exact current Subject
ExplicitSelection ignores unrelated available Subjects
missing selected Subject does not substitute another Subject
stale selection revision rejected
selection replacement uses fresh Subject occurrence
AllAvailableSubjects regression preserved
```

Framework/QA integration coverage:

```text
P1 selection never drives P2 Composition
P2 selection never drives P1 Composition
two Outputs remain independent
leave clears exact selected occurrence
rejoin requires fresh exact Subject
Default restored when selected ThirdPerson Subject unavailable
```

Consumer Play Mode:

```text
P1 joins -> selects Farmer/Cow -> P1 ThirdPerson Camera
P2 joins -> selects Farmer/Cow -> split-screen
both may select same ActorProfile
P1 leave -> P2 remains valid
P1 rejoin -> fresh selection -> split-screen restored
repeat symmetrically for P2
```

## 15. Disposition

```text
explicit Camera-domain Subject selection      ACCEPTED / CAMERA-031-A IMPLEMENTED
Camera core Player dependency                 REJECTED
Player Slot -> Composition adapter            ACCEPTED / CAMERA-031-B IMPLEMENTED
Player Slot -> Camera Output policy           RETAINED
PlayerInputManager split layout authority     RETAINED
Character Selection split-screen consumer     NOT STARTED (CAMERA-031-C)
implementation                                CAMERA-031-A/B IMPLEMENTED; CAMERA-031-C NOT STARTED
Unity tested                                  NO
QAFramework tested                            NO
Consumer integrated                           NO
technically validated                         NO
certified                                     NO
```
