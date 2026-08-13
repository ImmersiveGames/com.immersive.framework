# IF-ADR-019 — Session Player Lifetime and Activity Representation Authority

Status: **Accepted / Reconciled / Implemented / QA Certified**  
Date: 2026-08-11  
Last updated: 2026-08-13  
Type: architecture / runtime authority / player product direction  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-007, IF-ADR-011, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-020, IF-ADR-021  
Source finding: pre-FIRSTGAME architecture review — R2 Session-Persistent Player

> This ADR is the accepted authority for Session Player lifetime versus Activity
> representation lifetime. The package implementation and focused QA were completed on
> 2026-08-12. Acceptance/certification of this boundary does not promote Experimental
> APIs to Stable and does not claim FIRSTGAME real-consumer proof.
>
> Current reconciliation and certification evidence:
> [ADR-019 reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md)
>
> IF-ADR-020 was accepted/reconciled on 2026-08-13 and now owns the explicit individual
> Session Player Leave operation that this ADR previously left deferred.

## Context

The accepted Player architecture establishes two distinct concepts:

```text
Logical Player
  Session participant identity

Actor
  contextual gameplay representation
```

IF-ADR-001 and IF-ADR-003 place Logical Player identity at Session scope while
Route/Activity owns contextual projection, Actor preparation/materialization, readiness
and gameplay participation.

Before IF-ADR-019, `Session-Persistent Player` remained a future contract. That omission
became material as soon as a joined Player crossed an Activity boundary. Without an
explicit lifetime decision, incompatible interpretations were possible:

```text
A. Player and complete physical hierarchy persist between Activities
B. Session participant identity persists while Activities own contextual representation
C. persistence is a per-Player/per-Profile authored option
```

Those interpretations affect Slot occupancy, Local Player Host lifetime, `PlayerInput`,
Actor selection, both provisioning modes, readiness, Camera, placement, Activity
transitions, Leave and Session termination.

This ADR establishes the canonical lifetime model. IF-ADR-020 now establishes the
individual terminal operation for that lifetime.

## Decision

### 1. A joined Logical Player is Session-scoped

A successful Player admission establishes a Logical Player participation lifetime owned
by the current Session.

```text
Session
  Player Slot
    Joined Logical Player
        ↓ contextual projection
  Activity
    Activity Player Representation
```

A joined Logical Player remains a Session participant across Route/Activity changes until
one of these explicit terminal events occurs:

```text
Session termination
or
IF-ADR-020 Session Player Leave
```

Activity exit does not implicitly mean Player Leave. Activity entry does not implicitly
mean Player Join when the Logical Player is already joined.

### 2. "Session-Persistent Player" is not an authored persistence mode

This ADR does not introduce:

```text
Player Persistence Mode
  Activity
  Session Persistent
```

and does not add a `Session Persistent` boolean or equivalent policy to
`PlayerSessionProfile`.

Session persistence is the canonical semantic of a joined Logical Player.
`PlayerSessionProfile` continues to describe initial Session intent:

```text
Supported Slots
Initial Joining
Host Provisioning
Actor Resolution
```

Once resolved, mutable Player participation state is owned by Session runtime authority.

### 3. Session Player and Activity Player Representation are different lifetimes

Session-scoped state includes at minimum:

```text
Player Slot identity
Slot occupancy
joined Logical Player identity/state
Session admission state
current occurrence/revision
valid selected Actor intent, when one exists
Session-visible Player observations
Session-owned provisioning resources
```

Activity-scoped state includes at minimum:

```text
Activity participation
physical Actor occurrence
Activity Actor bindings
Activity readiness contribution
Activity-local gameplay admission
Activity-local Camera requests
Activity-local contextual references
initial placement application
gameplay transform/state owned by the current occurrence
```

Core invariant:

```text
same Logical Player
    does not imply
same physical Actor occurrence
```

### 4. A joined Player may validly have no representation in the current Activity

```text
Session Player
  Joined = true

Current Activity
  Participating = false
  Representation = Absent
```

An Activity may exclude a joined Session Player through participation policy without
vacating the Session Slot or ending the Logical Player lifetime.

```text
Activity participation
  projects / qualifies current Session Players
  does not rewrite Supported Slots
  does not silently remove joined Players
```

Absence of an Activity representation is an error only when the current Activity contract
requires that representation and it cannot be prepared.

## Provisioning-specific physical lifetime

### 5. Manager-Provisioned Local Player Host is Session-owned after successful Join

After successful Manager-Provisioned admission:

```text
Session
  Player Slot
    Logical Player
    Manager-Provisioned Local Player Host
      PlayerInput
      Actor Mount

Activity A
  Actor occurrence A under Actor Mount

Activity B
  Actor occurrence B under Actor Mount
```

The Host owns technical `PlayerInput` evidence, the explicit Actor Mount and typed
admission evidence. It does not become the Actor, select `ActorProfile` by itself or own
Activity gameplay.

Normal Activity transition may release/recreate the Actor while preserving the
Session-owned Host. The Host must have explicit Session ownership; persistence must not be
an accidental `DontDestroyOnLoad` or stale Activity object side effect.

IF-ADR-020 adds the complementary terminal rule: explicit Leave of that Manager-
Provisioned Session Player releases the Session-owned Host/input endpoint through
provisioning authority before terminal Slot vacancy is committed.

### 6. Manager-Provisioned Host persistence does not imply world-position persistence

The outgoing Activity Host/world pose is not the incoming Activity placement source.

```text
Activity A world position
  != implicit Activity B initial position
```

World placement belongs to IF-ADR-021 Initial Placement. Until that decision is accepted
and implemented, no silent placement fallback is authorized.

### 7. Scene-Provided Host and Actor remain externally scene-owned occurrences

Scene-Provided Player authoring provides explicit scene objects such as:

```text
LocalPlayerHostAuthoring
PlayerInput
Actor Mount
Scene Logical Player Actor
exact Player Slot intent
exact Actor Profile intent
```

That physical hierarchy remains consumer-scene-owned.

```text
Session
  P1 Logical Player persists

Activity A
  Scene Host/Actor A
  contextual bind/adopt to P1
  contextual release on Activity exit

Activity B
  Scene Host/Actor B
  contextual bind/adopt to same P1
```

The same Logical Player may therefore use different Scene-Provided Host and Actor
occurrences across Activities.

IF-ADR-020 preserves that physical ownership on Leave: Framework authority is released,
but external scene-owned objects are not destroyed merely because the Session occurrence
ended.

### 8. Scene-Provided reprojection is not a second Join

First admission into a vacant Slot may establish the Session Player. A later Activity
surface for that already occupied Slot binds/reprojects a contextual representation to
the existing Session Player.

It must not:

```text
allocate another fallback Slot
silently vacate/rejoin
create a second Logical Player
treat expected occupancy as ordinary duplicate Join
```

Conflicting scene evidence fails explicitly.

### 9. Device disconnect/reconnect semantics remain separate

This ADR and IF-ADR-020 do not define device/network continuity:

```text
device disconnect/reconnect
device reassignment
automatic control-scheme migration
network reconnection
```

Those contracts remain separate and must not be inferred from Session persistence or
Leave.

## Actor continuity

### 10. Actor selection intent may persist; physical Actor occurrence does not

```text
Session P1
  Selected Actor = Knight

Activity A
  Knight occurrence A

Activity B
  Knight occurrence B
```

Valid Session Actor selection can survive Activity boundaries. Physical occurrence does
not.

IF-ADR-020 terminal Leave clears mutable Session Player state owned by the departed
occurrence; a later rejoin is a new occurrence and does not silently inherit that state.

### 11. Activity-specific Actor preparation remains contextual

For each participating joined Player:

```text
Scene-Provided
  validate authored evidence
  bind/adopt scene-owned representation

Manager-Provisioned
  resolve Actor intent
  materialize contextual Actor occurrence
  attach below explicit Actor Mount
```

Actor preparation failure is an Activity representation/readiness failure, not implicit
Session Player termination.

## Activity transition boundary

### 12. Outgoing Activity authority releases before incoming contextual authority

Canonical transition:

```text
Current Activity A
  -> cover / transition boundary
  -> stop outgoing gameplay
  -> release outgoing representation
  -> release Activity-local bindings / Camera / readiness / contextual refs
  -> retain Session Player state
  -> incoming Activity B becomes contextual authority
  -> evaluate participation
  -> bind/adopt or materialize each required current representation
  -> Initial Placement integration point
  -> readiness
  -> reveal
```

Outgoing Unity objects must not remain authoritative merely because destruction/unload is
not yet physically observable.

### 13. WaitCovered control-plane accessibility remains required

If Session-level work is required to advance an incoming Activity while covered, that
control-plane work must remain reachable. The Framework must not resolve a blocked
transition by fake Ready, Required→Optional weakening, automatic Join, discarding the
Session Slot or silently changing Actor selection.

## Leave and termination boundaries

### 14. IF-ADR-020 owns individual Session Player Leave

The previously deferred individual terminal operation is now accepted in IF-ADR-020.

Session Player Leave:

```text
targets one exact currently joined Slot + occurrence/revision
releases current Activity representation when present
releases provisioning-specific Session resources
clears occurrence-owned Session state
commits Slot -> Vacant / Available
```

Activity contextual release remains narrower than Leave. Consumers must not simulate
Leave through direct Slot mutation, arbitrary `GameObject` destruction or Scene-Provided
contextual `RequestRelease`.

### 15. Session termination ends all Session Player lifetimes

Session termination remains a separate aggregate lifecycle operation:

```text
active Activity representations
        ↓ release
Session-owned Manager-Provisioned Hosts / input endpoints
        ↓ release
joined Slot occupancy / Logical Player state
        ↓ clear
Session Player observations and scoped runtime state
        ↓ clear
Session authority
        ↓ dispose
```

After termination, no stale resource from the terminated Session remains authoritative.
Session termination is not defined as repeatedly issuing public individual Leave commands.

## Runtime authority and state model

### 16. Session authority owns truth; physical occurrences provide evidence

Session runtime authority owns:

```text
which Slots are supported
which Slots are occupied
which Logical Players are joined
which Session-scoped Player occurrence/state is current
```

Activity/contextual systems provide evidence for current representation, Actor occurrence,
readiness, bindings and Camera participation.

A physical `LocalPlayerHostAuthoring`, `PlayerActorDeclaration` or other scene object is
not by itself proof that a Session Player exists.

### 17. Session and Activity state are orthogonal

Conceptually:

```text
Session Player
  Vacant
  Joined
  Leaving / failed-release evidence where required by IF-ADR-020 implementation

Activity Representation
  Absent
  Preparing
  Active / Ready
  Releasing
  Failed
```

Valid:

```text
Session = Joined
Activity Representation = Absent
```

Invalid:

```text
Session = Vacant
Activity Representation = authoritative Active for departed occurrence
```

Exact enum names are implementation details unless separately frozen by public contracts.

## Diagnostics and failure policy

### 18. Session/Activity lifetime mismatches fail explicitly

Diagnose rather than silently repair:

```text
required Scene-Provided representation missing
duplicate current representation for one Slot
outgoing representation still bound after contextual release
unexpected Manager-Provisioned Session Host loss
selected Actor cannot be prepared
stale Activity-local Camera/readiness/binding evidence
Join attempts already occupied Slot
incoming Scene-Provided evidence conflicts with Session Player
contextual release fails
Session termination leaves authoritative Player resources
Leave reports success while required resources remain authoritative
```

No silent new Player, fallback Slot, fallback Actor, fake Ready, auto-recreate Host or
stale evidence reuse is allowed.

### 19. Occurrence-aware evidence remains required

```text
Logical P1
  Activity A occurrence #A
  Activity B occurrence #B
```

Evidence from `#A` is not current evidence for `#B`.

IF-ADR-020 extends this occurrence safety to destructive Session mutation:

```text
P1 Session occurrence A leaves
P1 Session occurrence B later joins same Slot
stale Leave A -> rejected
```

## Authoring and product surface

### 20. No new "Persistent Player" authoring switch

Existing surfaces continue to express their existing intent:

```text
PlayerSessionProfile
  Session initial configuration

Scene-Provided Player Composer
  explicit scene-owned representation

Manager-Provisioned authoring
  Session-authorized technical Host provisioning
```

No new Profile/Composer/checkbox is required merely to expose canonical Session lifetime.

### 21. Advanced/Debug should expose both scopes

Useful diagnostics separate:

```text
PLAYER SESSION
Slot
Joined / Available
Session occurrence/revision
Provisioning mode
Session Host when Manager-Provisioned
Actor selection
latest Leave evidence

CURRENT ACTIVITY
Activity
Participating
Representation state
Actor occurrence
Readiness
```

For a joined but excluded Player, Session remains Joined while current Activity
representation is Absent. After successful Leave, the old Activity/Host summaries may
remain diagnosable only as non-authoritative released/baseline evidence.

## Relationship to Initial Placement

### 22. Initial Placement is a separate authority

This ADR does not define Spawn Points, placement anchors, reset or respawn. It only
establishes the integration point after current Activity representation preparation and
before readiness where placement is required.

IF-ADR-021 owns proposed Activity initial placement. It must not own Join, Player identity,
Actor selection, Leave or Session lifetime.

## Alternatives considered

### 23. Persist the complete Player/Actor GameObject hierarchy — rejected

Keeping an arbitrary full Player/Actor hierarchy alive collapses Session and Activity
lifetimes and creates stale scene/physics/Camera/readiness/binding/placement authority.
It is also incompatible with Scene-Provided external physical ownership.

### 24. Persist Logical Player identity and reproject contextual representation — accepted

```text
Session Logical Player persists
  -> Activity A representation
  -> contextual release
  -> Activity B representation
```

Provisioning-specific physical resources have different lifetimes only where the accepted
ADRs explicitly say so.

### 25. Per-Player authored physical persistence mode — deferred

Do not add `Persist Physical Actor Across Activities` until a concrete game requirement
proves the need and defines scene ownership, physics/world migration, placement,
contextual invalidation, Camera, readiness and save/restore interactions.

## Rejected behavior

- Treating Activity exit as implicit Player Leave.
- Treating Activity entry as a second Join for an already joined Logical Player.
- Keeping arbitrary Activity GameObjects alive to simulate Session persistence.
- Treating physical Actor as authoritative Session Player identity.
- Reusing outgoing Activity readiness/Camera/binding evidence in incoming Activity.
- Silently carrying outgoing world position into the next Activity.
- Consumer Slot mutation to simulate persistence or Leave.
- Automatic replacement Slot/Actor selection when reprojection fails.
- Global Player manager/service locator.
- Hidden fallback between Scene-Provided and Manager-Provisioned.
- Adding a `Persistent Player` toggle for canonical Session lifetime.
- Treating Scene-Provided contextual release as IF-ADR-020 Session Leave.

## Deferred / separate contracts

The following remain separate:

```text
device disconnect/reconnect
network reconnection
per-Slot Host Provisioning
targeted Join / generalized Slot assignment where not yet accepted
complete Explicit Actor Selection mutation contract where not yet accepted
IF-ADR-021 Initial Placement / Spawn authority
physical Actor persistence across Activities
checkpoint / respawn
save-game persistence of Player state
cross-Session Player identity
```

Session Player Leave public command and final individual release semantics are no longer
deferred here; they are owned by accepted IF-ADR-020.

## Consequences

### Positive

```text
Join once at Session scope
project into zero or more Activities
Leave once through IF-ADR-020 or terminate with Session
```

Scene-Provided and Manager-Provisioned remain peer product modes without forcing the same
physical Host lifetime. Activity participation can exclude a joined Player. Actor
selection may persist independently of physical occurrence. Initial Placement remains a
separate spatial authority.

### Cost

The implementation must keep Session truth separate from contextual physical state,
provide Scene-Provided reprojection, preserve Manager-Provisioned Session Host ownership,
and maintain diagnostics that distinguish current authority from stale/released evidence.

## Reconciliation applied

The 2026-08-12 ADR-019 reconciliation updated the directly affected architecture records.
The 2026-08-13 IF-ADR-020 closure resolves the formerly deferred individual Leave boundary:

```text
IF-ADR-003
  Session vs Activity lifetime remains explicit
  IF-ADR-020 now supplies individual terminal Leave

IF-ADR-012
  Activity readiness reconciles after Leave from current Session truth

IF-ADR-015
  Request Leave is now part of the accepted scoped consumer command model

IF-ADR-016
  Joining policy controls admission; Leave does not reapply initial configuration

IF-ADR-020
  exact occurrence-aware individual Session termination
```

Detailed evidence:

- [ADR-019 reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md)
- [ADR-020 reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md)

## Validation evidence

ADR-019 technical certification completed on 2026-08-12 proves:

### Session lifetime

```text
joined Slot remains occupied across Activity A -> B
Logical Player identity remains stable
Activity exclusion does not vacate Slot
Session termination clears Player lifetime
```

### Manager-Provisioned

```text
Session-owned Host survives normal Activity transition
outgoing Actor occurrence releases contextually
incoming occurrence prepares independently
outgoing world position is not placement policy
no stale Activity bindings/readiness/Camera authority survives
```

### Scene-Provided

```text
Activity A scene Host/Actor represents joined P1
contextual release does not end P1
Activity B distinct scene Host/Actor binds to same P1
reprojection is not a second Join
conflicting evidence fails explicitly
scene ownership remains preserved
```

### Negative cases

```text
duplicate current representation rejected
stale previous-Activity occurrence evidence rejected
missing required representation does not fake Ready
unexpected required Session Host loss is diagnostic
release failure does not report clean success
occupied persistent Slot is not silently reassigned
```

IF-ADR-020 focused Manager-Provisioned public Leave proof completed separately on
2026-08-13 and does not rewrite the ADR-019 certification scope.

### Real consumer — pending Stage B

FIRSTGAME still needs to prove normal product use across Activities and through a normal
Leave consumer surface. Stage B remains separate from this technical architecture closure.

## Accepted architecture boundary

```text
joined Logical Player lifetime is Session-scoped
Activity Actor is contextual representation, not Player identity
Activity exit != Leave
Activity entry != re-Join for an existing Session Player
Manager-Provisioned Host is Session-owned after admission
Scene-Provided Host/Actor remain scene-owned contextual occurrences
joined Player may have no current Activity representation
Actor selection intent may persist independently of physical occurrence
IF-ADR-020 owns exact individual Session Player Leave
Session termination remains separate aggregate lifecycle
Initial Placement remains separate authority
no arbitrary persistent GameObjects, global manager, silent fallback or fake readiness
```

## Current disposition

```text
Architecture decision           Accepted
Package implementation          Implemented
Focused technical QA            Certified
Full Player QA                  Certified for ADR-019 boundary
FIRSTGAME real-consumer proof   Pending Stage B
API maturity promotion          Not implied
```

IF-ADR-020 is the accepted/reconciled/implemented contract for explicit Session Player
Leave. IF-ADR-021 remains the separate Proposed contract for Activity Player initial
placement.
