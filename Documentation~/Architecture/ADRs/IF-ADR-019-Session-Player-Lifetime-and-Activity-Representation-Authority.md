# IF-ADR-019 — Session Player Lifetime and Activity Representation Authority

Status: **Accepted / Reconciled / Implemented / QA Certified**  
Date: 2026-08-11  
Last updated: 2026-08-12  
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

## Context

The accepted Player architecture already establishes two distinct concepts:

```text
Logical Player
  Session participant identity

Actor
  contextual gameplay representation
```

IF-ADR-001 and IF-ADR-003 intentionally place Logical Player identity at Session scope
while Route/Activity owns contextual projection, Actor preparation/materialization,
readiness and gameplay participation.

Before this decision, the architecture still left `Session-Persistent Player` as a future
contract. That omission became material as soon as a joined Player crossed an Activity
boundary.

Without an explicit lifetime decision, several incompatible interpretations are possible:

```text
A. the Player and its entire physical hierarchy persist between Activities

B. only Session participant identity persists while each Activity owns a contextual
   physical representation

C. persistence is a per-Player or per-Profile authored option
```

Those interpretations have very different consequences for:

```text
Player Slot occupancy
Local Player Host lifetime
PlayerInput lifetime
Actor selection
Scene-Provided Player
Manager-Provisioned Player
Activity participation
readiness
camera ownership
initial placement
Activity transitions
Player Leave
Session termination
```

This ADR establishes the canonical model required before FIRSTGAME treats cross-Activity
Player continuity as a normal product capability.

## Decision

### 1. A joined Logical Player is Session-scoped

A successful Player admission establishes a Logical Player participation lifetime owned
by the current Session.

The canonical model is:

```text
Session
  Player Slot
    Joined Logical Player
        ↓ contextual projection
  Activity
    Activity Player Representation
```

A joined Logical Player remains a Session participant across Route/Activity changes
until one of these explicit terminal events occurs:

```text
Session termination

or

future explicit Session Player Leave
```

Activity exit does not implicitly mean Player Leave.

Activity entry does not implicitly mean Player Join when the corresponding Logical
Player is already joined in the Session.

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

`PlayerSessionProfile` continues to describe initial Session intent such as:

```text
Supported Slots
Initial Joining
Host Provisioning
Actor Resolution
```

Once the Session has resolved that initial intent, mutable Player participation state is
owned by the Session runtime authority.

### 3. Session Player and Activity Player Representation are different lifetimes

The Framework distinguishes persistent Session state from contextual Activity state.

Session-scoped state includes, at minimum:

```text
Player Slot identity
Slot occupancy
joined Logical Player identity/state
Session admission state
valid selected Actor intent, when one exists
Session-visible Player observations
provisioning-specific Session resources explicitly owned by the Session
```

Activity-scoped state includes, at minimum:

```text
Activity participation
physical Actor occurrence
Activity Actor bindings
Activity readiness contribution
Activity-local gameplay admission
Activity-local camera requests
Activity-local contextual references
initial placement application
gameplay transform/state owned by the current occurrence
```

The core invariant is:

```text
same Logical Player
    does not imply
same physical Actor occurrence
```

### 4. A joined Player may validly have no representation in the current Activity

The following is a valid state:

```text
Session Player
  Joined = true

Current Activity
  Participating = false
  Representation = Absent
```

An Activity may exclude a joined Session Player through its accepted participation
policy without vacating the Session Slot or ending the Logical Player lifetime.

This preserves IF-ADR-012:

```text
Activity participation
  projects / qualifies current Session Players

Activity participation
  does not rewrite Session Supported Slots
  does not silently remove joined Players from the Session
```

Absence of an Activity representation is therefore not automatically an error.

It becomes an error only when the current Activity contract requires a representation
and that representation cannot be prepared.

## Provisioning-specific physical lifetime

### 5. Manager-Provisioned Local Player Host is Session-owned after successful Join

For Manager-Provisioned local Players, the technical Local Player Host represents the
Session-side local input/provisioning endpoint.

After successful admission, the intended lifetime is:

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

The Manager-Provisioned Host remains distinct from the Actor.

The Host:

```text
owns technical PlayerInput evidence
owns the explicit Actor Mount
carries typed admission evidence
does not become the Actor
does not select ActorProfile by itself
does not execute gameplay by itself
```

The Activity Actor occurrence may be released and recreated while the Session Host
remains alive.

The implementation must give this Host an explicit Session-owned lifetime. It must not
achieve persistence accidentally by leaving an arbitrary Activity GameObject alive or
by relying on scene-load side effects.

The exact Session-owned hierarchy/container is an implementation decision and must not
become a global Player manager or service locator.

### 6. Manager-Provisioned Host persistence does not imply world-position persistence

A persistent technical Host must not silently carry the outgoing Activity's world
placement into the incoming Activity.

This is invalid as an implicit rule:

```text
Activity A exit
  Host world position = outgoing gameplay position
        ↓
Activity B enter
  reuse that transform as the new spawn position
```

World placement belongs to the Initial Placement / Spawn authority opened separately by
R1.

This ADR establishes only the integration boundary:

```text
incoming Activity representation prepared
        ↓
Initial Placement authority resolves/applies placement
        ↓
readiness may complete
```

Until the Initial Placement contract is accepted, no implementation may invent a silent
fallback placement policy.

### 7. Scene-Provided Host and Actor remain externally scene-owned occurrences

Scene-Provided Player authoring currently provides an explicit scene object containing:

```text
LocalPlayerHostAuthoring
PlayerInput
Actor Mount
Scene Logical Player Actor
exact Player Slot intent
exact Actor Profile intent
```

That physical hierarchy is owned by the consumer scene.

This ADR does not convert it into a Session-persistent GameObject.

For Scene-Provided Player continuity:

```text
Session
  Player Slot P1
    Logical Player P1 persists

Activity A scene
  Scene-Provided Host A
  Scene Actor A
        ↓
  contextual bind/adopt to P1
        ↓
  Activity A exits
        ↓
  contextual release
  scene retains ownership of physical destruction/unload

Activity B scene
  Scene-Provided Host B
  Scene Actor B
        ↓
  contextual bind/adopt to the same P1
```

Therefore:

```text
same Logical Player
    may use
different Scene-Provided Host occurrences
    and
different physical Actor occurrences
```

The physical Scene-Provided Host is Activity/scene-owned even though the Logical Player
it represents is Session-owned.

### 8. Scene-Provided reprojection is not a second Join

When a Scene-Provided surface targets a Slot that is already occupied by the same
Session Logical Player, incoming Activity projection must not be modeled as a new Player
Join.

The runtime needs a distinct contextual bind/adopt/reprojection path.

Conceptually:

```text
first admission into vacant Slot
  establishes Session Player

later Activity with same occupied Slot
  binds contextual Scene-Provided representation
  to existing Session Player
```

The runtime must not:

```text
allocate another fallback Slot
silently vacate and rejoin the Slot
create a second Logical Player
treat the expected occupied Slot as an ordinary duplicate-Join error
```

A Scene-Provided surface that conflicts with another Player identity or incompatible
Session state must fail explicitly.

The exact public/internal API for this reprojection is not frozen by this ADR.

### 9. Device disconnect/reconnect semantics remain separate

This ADR defines Player lifetime across Activity boundaries.

It does not define:

```text
device disconnect
device reconnect
device reassignment
automatic control-scheme migration
network player reconnection
```

For Manager-Provisioned local Players, a Session-owned Host may preserve the existing
`PlayerInput` relationship across normal Activity transitions.

For Scene-Provided Players, the next scene-owned Host may require explicit
reconciliation of its local input evidence with the already joined Session Player.

The detailed device continuity/reconnection contract remains deferred and must not be
silently inferred from this ADR.

## Actor continuity

### 10. Actor selection intent may persist; physical Actor occurrence does not

When a valid Actor selection exists for a joined Session Player, the selected Actor
intent is Session-scoped unless another accepted contract explicitly changes it.

Example:

```text
Session Player P1
  Selected Actor = Knight

Activity A
  Knight physical occurrence A

Activity B
  Knight physical occurrence B
```

The selection survives the Activity boundary.

The physical occurrence does not.

This ADR does not define the complete Explicit Actor Selection command/API. R7 remains
responsible for the exact selection request, validation and mutation semantics.

In particular, this ADR does not authorize silent Actor swapping while an Activity
representation is active.

### 11. Activity-specific Actor preparation remains contextual

For each participating joined Player, the incoming Activity is responsible for obtaining
a valid contextual Actor representation through the accepted provisioning path:

```text
Scene-Provided
  validate authored evidence
  bind/adopt the scene-owned representation

Manager-Provisioned
  resolve selected/default Actor intent
  materialize a contextual Actor occurrence
  attach under the explicit Actor Mount
```

Actor preparation failure does not end the Session Player lifetime.

It is an Activity representation/readiness failure and must remain explicit and
diagnosable.

## Activity transition boundary

### 12. Outgoing Activity state is released before incoming contextual state becomes authoritative

The canonical transition semantics are:

```text
Current Activity A
        ↓
Cover / transition boundary
        ↓
stop outgoing gameplay participation
        ↓
release outgoing Activity representation
        ↓
release outgoing Activity-local bindings
release outgoing Activity-local camera requests
release outgoing readiness evidence
release outgoing contextual references
        ↓
retain Session Player state
        ↓
incoming Activity B becomes contextual authority
        ↓
evaluate Activity participation
        ↓
for each participating joined Player:
  Scene-Provided      -> bind/adopt contextual representation
  Manager-Provisioned -> materialize contextual representation
        ↓
Initial Placement integration point
        ↓
Activity readiness
        ↓
Reveal
```

The exact internal ordering may be implemented transactionally/staged where existing
lifecycle contracts require it, but the ownership boundary above is normative.

Outgoing Activity objects must not remain authoritative merely because their Unity
objects still exist temporarily during a covered transition.

### 13. WaitCovered control-plane accessibility remains required

Session persistence must not regress IF-ADR-003 / IF-ADR-007 readiness semantics.

If an incoming Activity is covered and a Session-level operation is required to advance
Player readiness, that control-plane operation must remain reachable.

The Framework must not solve a blocked transition by:

```text
marking the Player Ready
making a Required Player Optional
performing an automatic Join
discarding the persistent Slot
silently changing Actor selection
```

## Leave and termination boundaries

### 14. Player Leave is a separate command contract

This ADR defines the lifetime that a future Session Player Leave operation will end.

It does not define the public Leave command itself.

R3 / the future Player Leave cut must define explicit Session-authorized termination of
one joined Player, including release ordering.

At minimum, Leave will need to reconcile:

```text
current Activity representation, if any
provisioning-specific Host/input resources
Slot occupancy
Logical Player state
Actor selection state
consumer observations
```

No consumer may simulate Leave by directly clearing Slot state or destroying random
Player objects.

### 15. Session termination ends all Session Player lifetimes

Session termination must explicitly release all Player-owned resources according to
their lifetime.

Conceptually:

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

After Session termination, no stale resource from the terminated Session may remain
authoritative.

## Runtime authority and state model

### 16. Session authority owns truth; physical occurrences provide evidence

The Session runtime authority owns:

```text
which Slots are supported
which Slots are occupied
which Logical Players are joined
which Session-scoped Player state is current
```

Activity/contextual systems may provide evidence for:

```text
current representation
current Actor occurrence
current readiness
current bindings
current camera participation
```

A physical `LocalPlayerHostAuthoring`, `PlayerActorDeclaration` or other scene object is
not by itself the authoritative proof that a Session Player exists.

This prevents physical lifetime from silently replacing the typed Session state model.

### 17. Minimal conceptual state separation

This ADR does not require one monolithic state machine.

At minimum, implementations must be able to distinguish:

```text
Session Player State
  Vacant
  Joined

Activity Representation State
  Absent
  Preparing
  Active/Ready
  Releasing
  Failed, when required by implementation diagnostics
```

These states are orthogonal.

Valid example:

```text
Session = Joined
Activity Representation = Absent
```

Invalid examples include:

```text
Session = Vacant
Activity Representation = Active for that Slot

Session = Joined
two authoritative Activity representations for the same Slot/occurrence scope
```

The exact enum/type names are not frozen by this ADR.

## Diagnostics and failure policy

### 18. Session/Activity lifetime mismatches fail explicitly

The Framework must diagnose, rather than silently repair, cases such as:

```text
required Scene-Provided representation is missing
two representations claim the same Slot in one Activity scope
outgoing Activity representation remains bound after contextual release
Manager-Provisioned Session Host disappears unexpectedly
selected Actor cannot be prepared for the incoming Activity
outgoing Actor still owns an Activity-local camera request
a Join attempts to consume an already persistently occupied Slot
incoming Scene-Provided evidence conflicts with the existing Session Player
Activity binding references an occurrence from the previous Activity
contextual release fails
Session termination leaves authoritative Player resources behind
```

Mandatory failures must not silently:

```text
create another Player
choose another Slot
choose another Actor
auto-recreate a missing Host without contract
mark readiness complete
reuse stale Activity evidence
downgrade Required participation
```

### 19. Occurrence-aware evidence remains required

Activity representation diagnostics and bindings must distinguish different physical
occurrences of the same Logical Player across Activities.

Conceptually:

```text
Logical Player P1
  Activity A occurrence #A
  Activity B occurrence #B
```

An evidence/binding/request produced by occurrence `#A` must not be accepted as current
evidence for occurrence `#B`.

The existing occurrence/revision correlation principles from IF-ADR-003 remain in force.

## Authoring and product surface

### 20. No new "Persistent Player" authoring switch

This ADR does not require a new Profile, Composer or checkbox merely to expose the
Session lifetime.

The existing product surfaces continue to express their existing intent:

```text
PlayerSessionProfile
  Session initial configuration

Scene-Provided Player Composer
  explicit scene-owned representation for an exact Slot/Actor intent

Manager-Provisioned Player authoring
  Session-authorized technical Host provisioning
```

New authoring should be introduced only where a real materialization or configuration
decision exists.

### 21. Advanced/Debug should expose both scopes

The product should eventually make the Session/Activity distinction visible without
requiring users to inspect internal contracts.

A useful diagnostic projection is:

```text
PLAYER SESSION

Slot                  Player1
Joined                Yes
Provisioning           Manager-Provisioned
Session Host           LocalPlayerHost(Clone)
Actor Selection        Knight


CURRENT ACTIVITY

Activity               Gameplay_02
Participating          Yes
Representation State   Ready
Actor Instance         Knight(Clone)
Readiness              Ready
```

For a non-participating joined Player:

```text
PLAYER SESSION

Slot                  Player1
Joined                Yes
Actor Selection        Knight


CURRENT ACTIVITY

Participating          No
Representation State   Absent
Actor Instance         None
```

The exact Inspector/report class is not defined here.

## Relationship to Initial Placement / Spawn

### 22. Initial Placement is a separate authority

This ADR deliberately does not define Spawn Points, placement anchors or respawn.

It only establishes that a newly prepared incoming Activity representation has an
explicit placement integration point before gameplay readiness/reveal when placement is
required.

R1 must define:

```text
where a contextual Player/Actor representation appears
who authors that spatial intent
how Scene-Provided Preserve Authored Placement differs from Apply Placement
how Manager-Provisioned Host/Actor placement is applied
```

R1 must not take ownership of:

```text
Player Join
Player identity
Actor selection
Player Leave
Session lifetime
```

## Alternatives considered

### 23. Persist the complete Player/Actor GameObject hierarchy

Rejected as the canonical model.

Example:

```text
Player GameObject
  DontDestroyOnLoad
  Host
  PlayerInput
  Actor
  Activity bindings
  camera state
```

This collapses Session and Activity lifetimes and creates stale-reference risks across:

```text
scene ownership
physics
camera requests
readiness
Activity bindings
placement
interaction targets
contextual gameplay state
```

It is especially incompatible with the accepted Scene-Provided model where the consumer
scene owns the physical representation.

### 24. Persist Logical Player identity and reproject contextual representation

Accepted.

```text
Session
  Logical Player persists
        ↓
Activity A representation
        ↓ release
Activity B representation
        ↓ prepare
```

Provisioning-specific physical resources may have different lifetimes only where this
ADR explicitly says so.

### 25. Per-Player authored physical persistence mode

Deferred.

Do not introduce a policy such as:

```text
Persist Physical Actor Across Activities
```

until a real game requirement proves that carrying a physical Actor occurrence across
Activity boundaries is necessary and cannot be represented through Session state plus
contextual reprojection.

Such a feature would require a separate contract for:

```text
scene ownership
physics/world migration
placement
contextual binding invalidation
camera ownership
readiness
save/restore interactions
```

## Rejected behavior

- Treating Activity exit as implicit Player Leave.
- Treating Activity entry as a second Join for an already joined Logical Player.
- Keeping arbitrary Activity GameObjects alive to simulate Session persistence.
- Treating the physical Actor as the authoritative Session Player identity.
- Reusing outgoing Activity readiness/camera/binding evidence in the incoming Activity.
- Silently carrying outgoing world position into the next Activity.
- Consumer mutation of Slot occupancy to simulate persistence or Leave.
- Automatic replacement Slot/Actor selection when reprojection fails.
- Global Player manager/service locator.
- Hidden fallback from Scene-Provided to Manager-Provisioned or the inverse.
- Adding a `Persistent Player` authoring toggle for the canonical Session lifetime.

## Deferred / separate contracts

The following remain separate from this ADR:

```text
Session Player Leave public command and final release semantics
device disconnect/reconnect
network reconnection
per-Slot Host Provisioning
targeted Join / generalized Slot assignment
complete Explicit Actor Selection mutation contract
Initial Placement / Spawn authority
physical Actor persistence across Activities
checkpoint / respawn
save-game persistence of Player state
cross-Session Player identity
```

## Consequences

### Positive

The Framework gains one coherent Player lifetime:

```text
Join once at Session scope
project into zero or more Activities
Leave once or terminate with Session
```

Scene-Provided and Manager-Provisioned remain peer product modes without forcing them to
share the same physical Host lifetime.

Activity participation becomes easier to reason about because a Player can remain joined
while having no current representation.

Actor selection can persist as Session intent without requiring the same GameObject to
survive.

Initial Placement receives a clean boundary and does not become Player lifecycle
authority.

Player Leave can later terminate a well-defined Session lifetime instead of inferring
what to destroy from scene objects.

### Cost

The implementation required reconciliation of runtime paths that previously coupled
physical Host admission/release too closely to Session Player creation/destruction.

Scene-Provided Player now requires and uses an explicit contextual reprojection/bind path
for an already occupied Session Slot.

Manager-Provisioned Host ownership is explicitly Session-scoped after successful Join,
which requires a semantic admitted-resource release path at Session termination.

Diagnostics and observation surfaces must continue to distinguish Session Player state
from current Activity representation state.

## Reconciliation applied

The 2026-08-12 reconciliation updates the directly affected architecture records:

```text
IF-ADR-001
  former Session-Persistent Player future direction resolved by IF-ADR-019

IF-ADR-003
  Session Player vs Activity representation lifetime made explicit
  Player Leave remains separate under IF-ADR-020

IF-ADR-007 / IF-ADR-011 / IF-ADR-012
  Player readiness now distinguishes Session-only evidence from
  representation-required evidence

IF-ADR-015
  no automatic command expansion from IF-ADR-019
  observation distinguishes Session state from current Activity occurrence
  Leave remains separate under IF-ADR-020

IF-ADR-016
  no authored Persistent Player mode
  Session persistence is canonical runtime semantics
  per-Slot Host Provisioning remains deferred

IF-ADR-020
  dependency updated from proposed IF-ADR-019 to accepted IF-ADR-019
```

The detailed compatibility and certification record lives in
`../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md`.

## Validation evidence

The implementation/QA cuts completed on 2026-08-12 prove the required boundary:

### Session lifetime

```text
joined Slot remains occupied across Activity A -> Activity B
Logical Player identity remains stable across the transition
Activity exclusion does not vacate the Slot
Session termination clears the Player lifetime
```

### Manager-Provisioned

```text
Session-owned Host survives normal Activity transition
outgoing Actor occurrence is contextually released
incoming Actor occurrence is prepared independently
outgoing world position is not silently reused as placement policy
no stale Activity bindings/readiness/camera requests survive
```

### Scene-Provided

```text
Activity A scene-owned Host/Actor can represent joined Slot P1
Activity A contextual representation releases without ending P1
Activity B distinct scene-owned Host/Actor can bind to the same P1
reprojection is not treated as a second Join
conflicting scene evidence fails explicitly
scene ownership of physical destruction/unload remains preserved
```

### Negative cases

```text
duplicate current representation for one Slot is rejected
stale previous-Activity occurrence evidence is rejected
missing required representation does not fake Ready
unexpected loss of required Session-owned Host is diagnostic
release failure does not report clean success
occupied persistent Slot is not silently reassigned
```

### Real consumer — pending Stage B

FIRSTGAME still needs to prove:

```text
a joined Player crosses at least two Activities
the developer can understand what persisted and what was recreated/adopted
Scene-Provided and Manager-Provisioned semantics are explainable
Advanced/Debug evidence makes Session vs Activity state visible
no manual reconstruction of internal contracts is required
```

## Accepted architecture boundary

```text
joined Logical Player lifetime is Session-scoped
Activity Actor is a contextual representation, not the Player identity
Activity exit does not imply Leave
Activity entry does not imply re-Join for an existing Session Player
Manager-Provisioned Host is explicitly Session-owned after successful admission
Scene-Provided Host/Actor remain scene-owned contextual occurrences
same Logical Player may have different physical occurrences across Activities
joined Player may validly have no representation in the current Activity
Actor selection intent may persist independently of physical Actor occurrence
Initial Placement remains a separate authority
no arbitrary persistent GameObjects are used as Session authority
no silent fallback or fake readiness is introduced
Player Leave, disconnect/reconnect and physical Actor persistence remain separate contracts
```

## Current disposition

```text
Architecture decision           Accepted
Package implementation          Implemented
Focused technical QA            Certified
Full Player QA                  Certified
FIRSTGAME real-consumer proof   Pending Stage B
API maturity promotion          Not implied
```

IF-ADR-020 remains the separate proposed contract for explicit Session Player Leave.
IF-ADR-021 remains the separate proposed contract for Activity Player initial placement.
