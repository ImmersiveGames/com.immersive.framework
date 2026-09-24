# IF-ADR-003 — Player Participation and Actor Lifecycle

Status: **Accepted / Reconciled / Implemented / Current Player QA PASS**  
Last updated: **2026-08-29**  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021, IF-ADR-023  
Current aggregate record: [Player Current Aggregate Recertification — 2026-08-24](../Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)  
Actor-selection closure: [IF-ADR-015B — 2026-08-26](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)  
Actor-runtime composition closure: [IF-ADR-023 — 2026-08-29](../Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)

## Current structural reconciliation

IF-ADR-003 remains authoritative for Player participation, Session Actor selection, Activity projection/readiness and Actor lifecycle semantics.

IF-ADR-023 supersedes the former monolithic Actor structural detail.

Current composition:

```text
Local Player Host
└── ActorMount
    └── PlayerActorRuntimeHost
        ├── PlayerActorDeclaration
        └── PresentationMount
            └── ActorProfile.PresentationPrefab
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
+ exact PlayerActorRuntimeHost / Presentation candidate where applicable
→ Framework validates/adopts
→ successful admission
→ Session owns admitted physical Player occurrence
```

A failed Scene-Provided admission does not transfer ownership. Supplying scene unload must not silently end an already admitted Session Player.

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

Expanded current aggregate:

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
