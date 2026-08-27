# IF-ADR-003 — Player Participation and Actor Lifecycle

Status: **Accepted / Reconciled / Implemented / Current Player QA PASS**  
Last updated: **2026-08-26**  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Reopen record: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)  
Historical closure record: [2026-08-15 Player Physical Lifetime Recertification](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)  
Current aggregate record: [Player Current Aggregate Recertification — 2026-08-24](../Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)  
Actor-selection public-surface closure: [IF-ADR-015B — 2026-08-26](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)  
Initial Placement reconciliation: [2026-08-23 Player Authority and Initial Placement](../Reconciliation/IF-ADR-021-Player-Authority-and-Initial-Placement-Reconciliation-2026-08-23.md)

## Context

A Logical Player is a Session participant while Activity participation is contextual gameplay authority.

The framework keeps these decisions distinct:

```text
Host Provisioning
Slot Assignment
Session Join
Actor Selection
Actor Preparation
physical Player acquisition/adoption
Activity projection / activation
readiness
gameplay admission
Session Player Leave
```

The former interpretation incorrectly treated Activity ownership of presentation as ownership of the physical Actor lifetime.

## Decision

Player participation is Session-scoped and keyed by typed Slot identity.

```text
Session
  Slot configuration
  Joining / admission
  Logical Player occurrence
  Actor selection
  admitted physical Player representation
  physical preparation evidence

Activity
  participation projection
  representation activation
  readiness contribution
  gameplay / input / camera authority
  contextual bindings
  Activity-owned RuntimeContent scope
```

Scene-Provided and Manager-Provisioned remain peer provisioning modes. They converge on the same Session/Slot/Actor authority after successful admission.

## Physical Player versus Activity representation

The admitted physical Player occurrence and an Activity representation occurrence are different lifetimes.

```text
Physical Player occurrence
  Session-owned after successful admission

Activity representation occurrence
  Activity-scoped
```

Therefore:

```text
Activity A exits
  -> release A readiness/gameplay/camera/context
  -> do not implicitly destroy admitted physical Player

Activity B enters
  -> bind/project the existing admitted physical Player
  -> begin new Activity representation occurrence
  -> do not re-Join
  -> do not implicitly recreate physical Player
```

An Activity may exclude a Joined Player. In that case:

```text
Logical Player = Joined
Physical Player = Exists
Activity representation = Absent / Inactive
```

Likewise, a current Activity may be committed but NotReady because its Player contextual admission failed. The Activity may still own its own RuntimeContent scope; that scope is not a physical Player occurrence and is released by Activity lifecycle, not Player rollback.

## Provisioning

### Manager-Provisioned

```text
Framework creates candidate Host / PlayerInput / Actor composition
        ↓
successful admission
        ↓
Session owns admitted physical Player representation
```

### Scene-Provided

```text
supplying consumer scene authors candidate Host / PlayerInput
and selects exact Player Slot + ActorProfile intent
        ↓
Scene-Provided Player authoring materializes or preserves
ActorProfile.LogicalActorHostPrefab under the exact Actor Mount
        ↓
Framework validates/adopts the exact resulting physical candidate
        ↓
successful admission
        ↓
runtime ownership transfers to Session Player occurrence
```

`ActorProfile.LogicalActorHostPrefab` is the single authored prefab authority for the Scene-Provided Logical Actor. The Scene Actor reference is derived technical evidence of the exact matching prefab instance, not a second consumer-authored Actor authority.

Editor materialization must be deterministic, non-destructive and conflict-safe. A missing Actor may be materialized from the selected Actor Profile; an existing matching prefab instance is preserved; mismatched, unpacked or conflicting Actor content is rejected explicitly rather than silently replaced.

A failed Scene-Provided admission does not transfer Player ownership.

After successful adoption, unloading the supplying consumer scene must not implicitly destroy the admitted Player. The implementation moves/attaches the admitted composition to the canonical Session-owned runtime scope before the supplying scene can invalidate it. Scene location does not make that scene Player-lifetime authority or require Activity-owned content.

## Spatial intent

IF-ADR-021 separates spatial intent from this lifecycle boundary. Route owns the
baseline spatial entry of the Session-owned Player for its current Route occurrence;
Activity may own only an explicit contextual relocation. Neither operation creates a
new Player occurrence, transfers physical lifetime, or turns Activity transition into
Join/Leave/recreation. Scene-Provided and Manager-Provisioned Players retain the same
post-admission Session authority; Scene-Provided may preserve its authored/current
pose when the explicit Route policy chooses it.

## Slot Join and assignment

The Session remains the authority over Slot allocation and assignment.

Current ordinary public Join uses the supported Slot order:

```text
Untargeted Join
  -> first eligible vacant Supported Slot in authored order
```

The runtime domain can represent targeted Slot intent where applicable, but an exact-Slot designer-facing public Join command is not part of the current delivered command surface. A future exact-Slot consumer command must reject rather than silently fall back to another Slot.

`PlayerSlotId` is domain identity and is not `PlayerInput.playerIndex`.

## Actor selection

Actor selection is Session-scoped mutable logical intent for one exact Joined Slot.

The delivered public command surface now supports four explicit Actor-selection requests:

```text
Select Actor
Select Default Actor
Replace Actor Selection
Clear Actor Selection
```

These operations are routed through the canonical Player Actor preparation boundary before the Session mutation authority.

Actor selection is revision-aware and stale mutation rejects. Duplicate selection remains governed by Session policy. Repeating the same selection is idempotent and does not advance revisions.

Direct Actor selection mutation is not an implicit physical hot-swap.

### Preparation barrier

Select / Replace / Clear are allowed only while logical selection can still change without replacing a prepared physical Actor.

Once the canonical preparation context reports a prepared Actor or a retained preparation/release failure barrier, those mutations reject:

```text
RejectedLogicalActorAlreadyPrepared
```

The rejection does not change selection, Slot or Session revisions.

Replacing a currently prepared/admitted physical Actor would require a separate explicit physical replacement workflow. The existing internal prepared-Actor replacement transaction remains internal and is not a public Player 1.0 command.

## Actor Resolution policy

Actor Resolution remains creation-time Session intent owned by `PlayerSessionProfile`:

```text
ResolveConfiguredDefault
  -> configured DefaultActorProfile only

LeaveUnresolved
  -> Join may leave the Slot without a selected Actor
```

`LeaveUnresolved` is intentionally valid for flows such as Character Selection. A later public `Select Actor` request can commit the chosen Actor; the game owns presentation of choices while the Framework owns selection validity and commit.

There is no silent default Actor fallback.

## Activity readiness boundary

An Activity may project a required Slot before it joins.

```text
WaitingForJoin / Preparing
```

is valid current evidence.

Activity readiness never becomes Session membership or physical lifetime authority.

A Route/Activity commit and Activity readiness are separate truths. A current Activity may be `Active + NotReady` with blocking failure without implying that Session physical lifetime should roll back.

## Session Player Leave

IF-ADR-020 owns the explicit terminal operation.

```text
Activity release
  !=
Session Player Leave

Session Player Leave
  -> retires current Activity context when present
  -> releases admitted physical Player resources owned by occurrence
  -> ends Session Player occurrence
  -> Slot becomes Vacant / Available
```

No current Activity representation is also a valid Leave precondition when the Session-owned physical Player remains prepared.

## Observation invariant

Physical truth must be observed from canonical Session/occurrence evidence. Hierarchy shape, scene membership or global lookup are not Actor-lifetime authority.

The public `PlayerSessionObserver` remains read-only. Explicit command components request change; the Observer does not become a command router or mutable state store.

## Rejected behavior

- Activity exit destroying/recreating the admitted physical Player by default.
- Treating Activity representation absence as physical Player absence.
- Re-Join when moving an already Joined Player between Activities.
- Scene unload silently ending an adopted Scene-Provided Player.
- Manager/Scene provisioning modes having divergent post-admission lifetime semantics.
- Treating Activity-owned RuntimeContent as Player physical ownership.
- Consumer direct Slot mutation.
- Consumer direct Actor-selection state mutation outside the scoped public command surface.
- Consumer direct materialization/reconcile authority.
- Physical Actor hot-swap hidden behind logical `Replace Actor Selection`.
- A second consumer-authored Scene Actor prefab authority beside `ActorProfile.LogicalActorHostPrefab`.
- Silent replacement of mismatched or conflicting Scene-Provided Actor content during Editor materialization.
- `playerIndex` as Slot identity.
- Silent fallback.
- Global Player manager/service locator.

## Certification

Historical Player physical-lifetime recertification on 2026-08-15 remains preserved:

```text
PLAYER QA CERTIFIED
mandatoryContracts = 25
executedContracts = 25
passedContracts = 25
```

The later current aggregate records the expanded Player boundary:

```text
PLAYER CURRENT AGGREGATE COMPLETE
mandatoryContracts = 27
executedContracts = 27
passedContracts = 27
```

The 2026-08-26 integrated rerun after the explicit Actor-selection public-surface cut again completed all current mandatory contracts with:

```text
actor = PASS
publicSurface = PASS
managerProvisioned = PASS
sceneProvided = PASS
leave = PASS
noPhysicalHandoff = PASS
```

This certification includes the public Actor-selection lifecycle and preparation barrier without rewriting the historical `25/25` evidence.

The package-local Actor-selection Editor tests added with the runtime cut are a separate Unity Test Framework evidence lane and are not claimed as executed by this ADR unless a dedicated result is recorded.
