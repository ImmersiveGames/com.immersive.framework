# IF-ADR-003 — Player Participation and Actor Lifecycle

Status: **Accepted / Reconciled / Implemented; current QA coverage is bounded and full Player recertification remains open**
Last updated: **2026-10-12**
Amended: **2026-10-12 — Stable GameApplicationAsset serialization and Experimental Actor-selection policy boundary.**
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021, IF-ADR-024, IF-ADR-038
Historical aggregate evidence: [Player Aggregate Recertification — 2026-08-24](../Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md); current QA-001 coverage: [Player Full Recertification — 2026-10-11](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md)
Actor-selection closure: [IF-ADR-015B — 2026-08-26](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)  
Actor occurrence spatial and Camera Subject authority: [IF-ADR-038](IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md)

## Current structural reconciliation

IF-ADR-003 remains authoritative for Player participation, Session Actor selection, Activity projection/readiness and Actor lifecycle semantics.

IF-ADR-023 is superseded by IF-ADR-038 for Actor composition and spatial authority.
This ADR remains authoritative for Player participation and Actor lifecycle. The
Actor occurrence root owns physical/spatial state; optional visual content is
subordinate and neither `VisualContentMount` nor `VisualContentPrefab` is required.

Current composition:

```text
Local Player Host
└── ActorMount
    └── PlayerActorRuntimeHost
        ├── PlayerActorDeclaration
        ├── physical Actor components / ActorCameraSubjectAuthoring
        ├── explicit ObservationTransform
        └── optional visual content
```

The removed `ActorProfile.LogicalActorHostPrefab` is **not** current authority. `LogicalActorsPrepared` remains current semantic readiness terminology.

## Decision

Player participation is Session-scoped and keyed by typed Slot identity.

```text
Session
  Slot configuration
  Joining / admission
  Player occurrence
  Actor selection
  admitted physical Player lifetime
  physical preparation evidence

Activity
  participation projection
  representation activation
  readiness contribution
  gameplay / input / camera authority
  contextual bindings
  Activity-owned RuntimeContent
```

Scene-Provided and Manager-Provisioned are provisioning modes. They converge on the same Session/Slot/Actor authority after successful admission.

## Physical Player vs Activity representation

The admitted physical Player occurrence and an Activity representation occurrence have different lifetimes.

```text
Physical Player occurrence
  Session-owned after successful admission

Activity representation occurrence
  Activity-scoped
```

Therefore Activity exit does not implicitly destroy/recreate the admitted Player and Activity entry is not a second Join.

An Activity may exclude a Joined Player while the Session Player and physical representation continue to exist.

## Provisioning

### Manager-Provisioned

```text
Framework creates Local Player Host / PlayerInput candidate
→ successful Join/admission
→ Session owns admitted physical Player occurrence
→ Actor selection remains separate
→ Activity preparation materializes/adopts PlayerActorRuntimeHost when required
```

Immediate Join may have technical/session Host evidence without contextual Activity assignment.

### Scene-Provided

```text
consumer scene authors exact Local Player Host
+ exact PlayerActorRuntimeHost / Actor occurrence
→ Framework validates/adopts
→ successful admission
→ Session owns admitted physical Player occurrence
```

A failed Scene-Provided admission does not transfer ownership. Supplying scene unload must not silently end an already admitted Session Player.

### Activity Player Admission Stage disposition — 2026-10-12

The P3K.7B `ActivityPlayerAdmissionStage*` implementation remains frozen as
Experimental/preparatory. This records neither functional replacement by
`ActivityPlayerLifecycleAdmissionRuntime` nor equivalence between the two
boundaries. The current GameFlow lifecycle-admission path remains authoritative
for its implemented prepare/commit/rollback behavior; the Stage runtime has no
current composition call site. Preserve the Stage sources and their existing
maturity until an explicit architecture decision establishes whether the
pre-activation staging responsibility is still required. This disposition does
not promote, reclassify or remove any API.

## Actor selection

Actor selection is Session-owned mutable logical intent for one exact Joined Slot.

Public operations:

```text
Select Actor
Select Default Actor
Replace Actor Selection
Clear Actor Selection
```

Selection is revision-aware. Repeating the same selection is idempotent. A different selected Actor requires Replace. Stale revisions reject without mutation.

The Session-wide duplicate-selection policy configured by
`GameApplicationAsset.playerActorSelectionDuplicatePolicy` is part of the
Experimental Actor-selection behavior contract. Its current values are
`Unspecified = 0`, `AllowDuplicates = 10`, and
`UniqueAcrossJoinedSlots = 20`. This policy does not promote `GameApplicationAsset`,
`ActorProfile`, the Player Session command surfaces, or any other Player
contract.

`GameApplicationAsset` remains a Stable authoring asset. Its existing
serialized data, including the duplicate-selection field and its stored enum
value, is protected as part of the Stable authoring data schema under
[IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md).
The Experimental behavior may evolve, but a change that makes existing
serialized data unreadable or silently changes its intent requires an explicit
ADR and migration. [IF-ADR-015B](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)
certifies the bounded public Actor-selection command surface; it does not
promote this policy or adjacent surfaces.

`PlayerActorSelectionDuplicatePolicy` currently has no
`FrameworkApiStatusAttribute`. This decision establishes the maturity of its
Actor-selection contract only; it does not authorize metadata or code changes.

### Preparation barrier

Selection and physical preparation are distinct.

Select / Replace / Clear are allowed only before the canonical prepared-Actor barrier. Once current preparation is established, logical selection commands do not become implicit physical hot-swap.

## Actor Resolution

Creation-time Session policy remains:

```text
ResolveConfiguredDefault
  -> configured DefaultActorProfile only

LeaveUnresolved
  -> Join may remain without selected Actor
  -> wait for explicit selection
```

There is no silent fallback Actor.

## Activity readiness

An Activity may project a required Slot before it joins and legitimately remain `WaitingForJoin` / `Preparing`.

Current readiness ordering remains:

```text
None
JoinedSlots
SelectedActors
LogicalActorsPrepared
GameplayReady
```

Activity readiness never becomes Session membership or physical lifetime authority.

## Spatial intent

IF-ADR-021 owns Route baseline spatial entry and optional Activity relocation.

Neither spatial operation creates a Player occurrence, transfers physical lifetime or turns Activity transition into Join/Leave.

## Session Player Leave

IF-ADR-020 owns explicit terminal Leave.

```text
Activity release != Session Player Leave

Session Player Leave
  -> retire current Activity context when present
  -> release admitted Session-owned physical Player resources
  -> end current Session Player occurrence
  -> Slot becomes Vacant/Available
```

## Observation invariant

Physical truth is observed from canonical Session/occurrence evidence. Hierarchy shape, scene membership and global lookup are not Player-lifetime authority.

`PlayerSessionObserver` remains read-only. Explicit command components request change.

## Rejected behavior

- Activity exit destroying/recreating admitted physical Player by default.
- Re-Join during ordinary Activity transitions.
- scene unload silently ending an adopted Scene-Provided Player.
- Manager/Scene provisioning modes diverging after admission.
- consumer direct Slot/Actor/session mutation.
- physical Actor hot-swap hidden behind logical Replace.
- restoring `ActorProfile.LogicalActorHostPrefab` as current runtime authority.
- scene/hierarchy/name/tag lookup as authority.
- silent fallback.
- global Player singleton/service locator.

## Certification

Historical Player physical-lifetime certification is preserved:

```text
Full Player 25/25
```

Historical expanded aggregate (2026-08):

```text
PLAYER CURRENT AGGREGATE COMPLETE
27/27 PASS
```

Current consolidated Manager-Provisioned functional proof:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
cases='14/14'
```

The 14-case path explicitly separates Join, Actor selection and dedicated Activity Actor preparation/materialization under the ADR-023 composition.
