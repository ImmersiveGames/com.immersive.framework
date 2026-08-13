# IF-ADR-003 — Player Participation and Actor Lifecycle

Status: **Accepted**  
Last updated: 2026-08-13  
Proposed reconciliation draft: **2026-08-11 — R6 / R7 / R8**  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020  
Current reconciliation: [ADR-003 / ADR-012 technical reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-003-012-RECONCILIATION-2026-08-10.md)  
ADR-020 follow-up: [ADR-020 reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md)

> **Draft note:** the R6/R7/R8 portions remain a proposed reconciliation of the accepted
> ADR. The accepted IF-ADR-020 Session Player Leave boundary below is a separate completed
> reconciliation and does not promote the remaining R6/R7/R8 draft deltas by association.
>
> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

A Logical Player is a Session participant while an Actor is contextual gameplay
content. Joining, Host provisioning/adoption, Actor selection, logical
preparation, physical materialization, gameplay admission, readiness contribution,
contextual release and Session Leave must remain distinct and diagnosable.

The R6/R7/R8 review additionally clarifies three independent decisions:

```text
Host Provisioning
  how the technical Player Host is provided for the Session

Slot Assignment
  which configured Player Slot a joining Player occupies

Actor Selection
  which ActorProfile is selected for one Joined Player Slot
```

These dimensions must not be collapsed into one per-Slot provisioning schema.

## Decision

Player participation is Session-scoped and keyed by typed Slot identity.
Route/Activity may project eligible Players and own contextual Actor
materialization, but they do not own Session participant identity.

```text
Session Slot configuration
Joining / admission
Local Player Host provisioning or adoption
Logical Player participation
Actor selection
Logical Actor preparation
physical Actor materialization
input / camera / gameplay admission
Activity readiness contribution
contextual release / reconcile
Session Player Leave
```

Scene-Provided and Manager-Provisioned are peer provisioning modes. They converge
on the same Session/Slot/Actor authority without collapsing Host and Actor
identity.

Reconciliation is idempotent, occurrence-aware and revision-correlated.
Consumers do not invoke internal preparation or reconcile authority.

## Session Player lifetime boundary

IF-ADR-019 is authoritative for the lifetime split between Session participation and
Activity representation. IF-ADR-020 is authoritative for explicit termination of one
exact joined Session Player occurrence.

```text
Session
  Joined Logical Player
  Slot occupancy
  valid Actor selection intent
  Manager-Provisioned Host after successful Join

Activity
  participation projection
  physical Actor occurrence
  readiness contribution
  gameplay/input/camera bindings
  contextual release
```

A Joined Slot may validly have no current Activity representation. Activity exit releases
contextual occurrence state but does not implicitly Leave the Session, vacate the Slot or
clear valid Session Actor selection. Activity entry for an already Joined Player is a
projection/reprojection operation, not a second Join.

Explicit Session Player Leave is different:

```text
exact joined Slot + current occurrence correlation
  -> IF-ADR-020 Leave transaction
  -> current Activity representation released when present
  -> provisioning-specific Session resources released
  -> Session Player occurrence ends
  -> Slot becomes Vacant / Available
```

The fact that contextual representation release is a stage of Leave does not merge the
Activity and Session lifecycles. Contextual release alone is still not Leave.

For Scene-Provided provisioning, a later Activity may bind a distinct scene-owned
Host/Actor occurrence to the same Joined Session Player. For Manager-Provisioned
provisioning, the Session-owned technical Host/`PlayerInput` survives normal Activity
transitions while the contextual Actor occurrence may be released and recreated. When
that Manager-Provisioned Session Player explicitly Leaves, IF-ADR-020 authorizes release
of the Session-owned Host/input endpoint through provisioning authority.

## Player Session dependency

IF-ADR-016 owns initial Session intent:

```text
PlayerSessionProfile
  Supported Slots
  Initial Joining
  Host Provisioning
  Actor Resolution
```

There is no independent Session Capacity and no per-Slot Host Provisioning
override in the current model.

Host Provisioning remains one Session-wide decision even when:

```text
different Players intentionally occupy different Slots
different Slots select different ActorProfiles
```

Choosing an Actor or targeting a Slot does not imply heterogeneous Host
Provisioning.

## Slot Join and assignment

The Session remains the authority over Slot allocation and assignment.

Two bounded Join intents are accepted by the R6/R7/R8 reconciliation draft:

```text
Untargeted Join
  -> first eligible vacant Supported Slot in authored order

Targeted Join
  -> exact requested Supported Slot when that Slot is eligible
```

For Targeted Join, the consumer expresses desired Slot identity but does not
reserve or mutate Slot state directly.

The Session validates at least:

```text
Joining is open
requested Slot identity is valid
requested Slot is configured/supported
requested Slot is vacant/eligible
Host Provisioning is compatible
request scope is current
```

Targeted Join has no fallback.

```text
request Player2
Player2 unavailable

-> explicit rejection
-> never Player1
-> never Player3
```

Untargeted Join retains current first-vacant-Supported-Slot semantics.

Targeted Join does not carry `ActorProfile`. Actor choice remains the separate
Actor Selection transaction.

Framework `PlayerSlotId` is domain identity and must not be collapsed into Unity
Input System `PlayerInput.playerIndex`.

Scene-Provided admission may continue to author an exact Slot directly. The
Manager-Provisioned consumer surface gains equivalent exact-Slot intent without
creating a second Slot authority.

## Actor selection

Actor selection is Session-scoped mutable intent for one exact Joined Player
Slot.

The consumer may request:

```text
Select ActorProfile
Select configured default ActorProfile
```

while Session authority validates and commits the selection.

The consumer does not directly mutate `SelectedActorProfile`, prepare a Logical
Actor or materialize physical content.

### Selection precondition

Actor selection changes require the target Slot to be Joined.

```text
Available / Reserved
  -> selection rejected

Joined
  -> selection may be evaluated
```

### Select semantics

If no Actor is selected:

```text
Select ActorProfile A
  -> A becomes selected
```

Selecting the same current ActorProfile is idempotent.

If another ActorProfile is already selected, a plain Select does not silently
replace it.

Replacement/clear remain explicit runtime semantics and must remain
revision-correlated.

### Prepared Actor boundary

Direct Actor selection mutation is not a physical Actor swap.

When one current Logical Actor is already prepared, direct:

```text
Select Actor
Replace Actor Selection
Clear Actor Selection
Select Default Actor
```

must reject rather than create divergence between Session selection and the
prepared physical Actor.

A physical hot-swap flow is a separate product operation and is not granted to
ordinary consumers by this reconciliation.

Consumers therefore still do not own Actor preparation/materialization authority.

### Actor selection revision

Selection mutation remains revision-aware. A request carrying an expected
selection revision must reject when that revision is stale.

This prevents old UI/control-plane intent from overwriting newer Session state.

### Duplicate Actor policy

Existing Session Actor-selection duplicate policy remains authoritative.

The framework may allow duplicate ActorProfile selection or require uniqueness
across Joined Slots according to the configured runtime policy.

No separate duplicate policy is introduced by targeted Join.

## Actor Resolution dependency

IF-ADR-016 `Actor Resolution` remains independent from Host Provisioning and
Slot Join.

```text
Resolve Configured Default
  permits the configured Slot default to be selected through the canonical
  selection operation

Leave Unresolved
  permits the Joined Slot to remain without Actor selection until an explicit
  consumer selection occurs
```

This bounded explicit selection contract is not a generic character-selection
system.

## Readiness and control-plane boundary

An Activity may project a required Slot before that Slot has Joined. When the
requirement is `JoinedSlots` or stronger, the Player contribution may remain:

```text
Preparing / WaitingForJoin
```

This is not failure and must not be silently converted to Ready, optional
participation or timeout success.

IF-ADR-020 adds the symmetric runtime case: a required Player may Leave an active
Activity because Session Leave authority is not owned by the Activity. The Activity must
then reconcile from current Session truth. Under the certified
`ExplicitSlots + GameplayReady + zero-participant Rejected` composition, the authored
Slot remains projected and returns to `WaitingForJoin`; stale `Ready` evidence from the
departed occurrence is invalid.

For `WaitCovered`, any operation required to advance readiness must remain
reachable through an external/control-plane path.

Depending on the authored composition, that may include:

```text
Request Join
Request Join To Slot
Request Actor Selection
Request Default Actor Selection
Request Leave
```

These operations are distinct from normal gameplay input.

Validation may warn about unreachable compositions but must not auto-change
readiness policy, participation requirement, Slot projection, Joining state,
target Slot or Actor selection.

## Rejected behavior

- Capacity as a second Session admission limit.
- Separate Player provisioning Profile.
- Per-Slot Host Provisioning overrides in the current Session model.
- Treating different ActorProfile per Slot as a reason for per-Slot Host Provisioning.
- Consumer direct Slot reservation/mutation.
- Consumer direct Actor selection state mutation.
- Targeted Join falling back to another Slot.
- Targeted Join carrying ActorProfile as an implicit combined transaction.
- Using Unity `playerIndex` as the Framework Slot identity.
- Consumer Actor preparation/materialization authority.
- Direct Actor selection replacing a currently prepared Actor.
- Treating Activity representation release as Session Player Leave.
- Destroying a Player GameObject or clearing a Slot directly to simulate Leave.
- Generic character roster/unlock/store/selection-flow authority in the Framework.
- Fake readiness, automatic Join or silent fallback.
- Global Player manager/service locator.

## Separate / future contracts

Session Player lifetime is resolved by IF-ADR-019.

Explicit Session Player Leave and the terminal lifetime operation for one joined Player
are resolved by accepted IF-ADR-020. Device disconnect/reconnect remains a separate
contract and must not be inferred from Leave.

Mixed/per-Slot Host Provisioning remains deferred until a concrete game
requirement demonstrates different provisioning ownership for different Slots.

A consumer-facing physical Actor hot-swap operation remains separate from bounded
Actor Selection.
