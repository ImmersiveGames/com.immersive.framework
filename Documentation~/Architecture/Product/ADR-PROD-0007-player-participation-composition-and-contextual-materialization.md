# ADR-PROD-0007 — Player participation composition and contextual Actor materialization

Status: Accepted  
Date: 2026-07-12  
Amended: 2026-07-22 — reconciled with the P3 Local Player Host / Actor Mount model  
Package: `com.immersive.framework`  
Area: Player Product Surface / Participation / Runtime Materialization  
Related: `F45-ADR-ACTOR-001`, `F49-PLAYER-001`, `F49-PLAYER-003`, `ADR-PROD-0004`, `ADR-PROD-0006`, `ADR-PROD-0008`, `ADR-PROD-0009`, `ADR-PROD-0010`, `ADR-PROD-0011`, `ADR-PROD-0012`

## Context

The framework must support different game constructions without introducing separate foundational architectures for single-player and multiplayer games.

Representative products may require:

```text
one preconfigured Player Slot with no selection screen
multiple preconfigured Player Slots
optional join and selection flows
persistent player choices across Routes
menus controlled by several available Slots
Route-specific player participation
Activity-specific Actor activation
menu pointers or avatars that are not gameplay Actors
gameplay Actors that exist only while a Route or Activity is active
```

Treating all of these cases as direct `PlayerSlot -> Actor` authoring creates several invalid equivalences:

```text
a Slot exists
=
a player joined
=
an Actor was selected
=
the Slot participates in the current context
=
an Actor was materialized
=
an active occupancy exists
```

Those facts are independent.

The framework also needs designer-facing Slot identity without requiring the same `PlayerSlotId`, display name, color or icon to be typed repeatedly in different assets and components.

ScriptableObject references are the primary authoring link. Mutable Session state must remain outside those assets.

## Decision

Player participation is divided into five explicit layers:

```text
Game/Application Participation Configuration
  authored participation capacity using PlayerSlotProfile references
  required/optional policies and explicit defaults

Session Participation Composition
  mutable runtime availability, join/readiness and persistent selections

Route/Activity Participation Projection
  contextual subset and requirements

Contextual Actor Materialization and Activation
  physical instances owned by explicit Route or Activity lifetime

Effective PlayerSlot Occupancy
  confirmed relation between a participating Slot and an admitted active Actor
```

These layers must not be collapsed into one component, asset or runtime authority.

## Canonical model

### 1. PlayerSlotProfile

`PlayerSlotProfile` is the immutable product identity and designer-facing metadata for one participation seat.

It is a ScriptableObject referenced directly by authoring surfaces.

Representative shape:

```text
PlayerSlotProfile
  PlayerSlotId
  Display Name
  Color / Accent
  Icon
  Display Order
  optional static presentation metadata
```

The canonical serialized text for `PlayerSlotId` is authored only inside the Profile.

Consumers reference the asset:

```text
correct:
  Player Slot Profile: [Player 1]

incorrect:
  Player Slot Id: "player.1"
  Slot Color: blue
  Slot Name: Player 1
```

`PlayerSlotProfile` must not contain mutable runtime state such as:

```text
joined
ready
connected device
PlayerInput
current user
current Actor selection
current occupancy
current Route/Activity participation
```

A runtime Player Slot references its `PlayerSlotProfile` and uses the typed `PlayerSlotId` resolved from that Profile.

The Profile is not cloned or mutated to represent Session state.

### 2. Game/Application Participation Configuration

The game construction defines available participation capacity through an explicit
ordered array of `PlayerSlotProfile` references.

```text
Local Player Slots[]
  [0] Player 1
  [1] Player 2
  [2] Player 3
  [3] Player 4
```

The array order is the canonical default allocation order for local join.

It may also declare:

```text
participation policies
initial dynamic join capacity
optional explicit default ActorProfile per Slot
grouping or product-specific metadata
```

The default local allocation policy is defined by `ADR-PROD-0011`:

```text
First Available By Configured Order
```

The framework does not require the ordinary local join request to name a specific Slot.

A minimal single-player game uses the same model:

```text
one PlayerSlotProfile reference
optional explicit default ActorProfile
no mandatory selection screen
```

The runtime must not silently invent `player.1`, a default Actor or any other required participation state when authoring is missing.

A wizard, template or Create flow may generate the minimal assets and configuration, but the resulting authoring must be explicit, serialized and inspectable.

### 3. Session Participation Composition

The Session maintains mutable participation facts that may need to survive Route changes.

Representative facts include:

```text
which configured Slots are available
which Slots have joined
which input/user evidence is associated with each Slot
readiness state
optional persistent ActorProfile selection
current dynamic join capacity
whether new joins are currently allowed
```

Dynamic join capacity is mutable Session policy.

It is distinct from:

```text
the number of authored PlayerSlotProfile assets
the number of currently admitted Players
the technical maxPlayerCount configured on PlayerInputManager
```

The Session may increase or decrease its current join capacity within the authored
and technical ceilings.

Reducing capacity is non-destructive:

```text
existing admitted Players remain admitted
no occupancy is cleared automatically
no Player is silently removed
new joins are blocked until the admitted count is below the current capacity
```

Removing or replacing existing Players requires an explicit leave or reconfiguration
operation with its own release and diagnostic evidence.

A Slot may exist and participate in Session-level UI without having:

```text
an ActorProfile selected
a gameplay Actor materialized
an active PlayerSlot occupancy
```

A joined Slot without an Actor selection is still allocated and is not available for
another join.

Its `PlayerSlotProfile` already provides stable participant presentation such as:

```text
display name
accent color
icon
pointer/UI identity
configured ordering evidence
```

Those presentation values do not replace `PlayerSlotId` as logical identity.

Session participation composition is scoped runtime state. It does not imply a singleton, service locator or application-global manager.

The Session retains references to immutable Profiles and runtime identity/state. It must not require persistent references to gameplay `GameObject` instances.

### 4. Route/Activity Participation Projection

A Route or Activity may project a contextual subset of the Session composition.

The projection may declare:

```text
which Slots are accepted
which Slots are required
which Slots are optional
which contextual representation is relevant
```

The Activity's required participation state is declared through the mandatory
`PlayerParticipationRequirementsProfile` defined by `ADR-PROD-0012`.

The product decides how missing state is resolved. The framework evaluates and gates
Activity admission; it does not force Actor selection during join.

Examples:

```text
a Menu Route accepts input from every joined Slot
a Gameplay Route uses Slots 1 and 2
a solo Activity temporarily activates only Slot 1
a spectator Route accepts a Slot without requiring a gameplay Actor
```

Route and Activity do not redefine Slot identity or mutate `PlayerSlotProfile`. They consume and constrain the Session composition for their own scope.

### 5. Contextual Actor materialization and activation

Physical Actor instances are owned by an explicit contextual lifetime.

Supported shapes include:

```text
Route-owned
  materialized when the Route enters
  released when the Route exits

Activity-owned
  materialized when the Activity enters
  released when the Activity exits

Route-owned with Activity activation
  materialized once for the Route
  activated, positioned or configured by Activities
```

Route and Activity decide:

```text
when an Actor representation is required
where it is materialized or activated
which contextual lifetime owns the physical instance
when the instance must be released
```

The physical creation mechanism may be specialized without transferring product or lifetime authority.

Canonical execution paths:

```text
generic Actor
  Route/Activity authority
  -> RuntimeContent materialization

local Player Actor
  Route/Activity/Session policy authorizes an explicit join
  -> PlayerInputManager provisions a technical Local Player Host
  -> framework admits the Slot as Joined
  -> Route/Activity resolves ActorProfile when required
  -> framework materializes the Logical Actor inside the host's Actor Mount
  -> framework admits and binds the Logical Actor to its contextual lifetime
```

For local Players, `PlayerInputManager` is the technical provisioner defined by `ADR-PROD-0010`. It does not select `PlayerSlotProfile`, `ActorProfile`, contextual lifetime or occupancy.

The Session retains logical participation and selected `ActorProfile` state, not mandatory physical Actor references.

Menu pointers, cursors and avatars are contextual representations. They do not automatically become gameplay Actors or gameplay occupancy.

### 6. Effective PlayerSlot occupancy

The following facts remain distinct:

```text
Slot Profile
Slot runtime availability
join state
ActorProfile selection
contextual participation
Actor materialization
effective occupancy
```

An effective occupancy represents:

```text
one participating PlayerSlot
-> one Actor admitted and active in the current context
```

Occupancy is not created merely because:

```text
a Slot Profile exists
a Slot is configured
a player joined
an ActorProfile was selected
a default selection exists
```

Selection expresses intent. Occupancy expresses an effective contextual relation.

The exact technical representation may use existing declarations/descriptors or a later runtime operation, but it must preserve the established `PlayerSlotId -> ActorId` contract and must not perform physical materialization itself.

## Common flow

```text
1. Author PlayerSlotProfile assets.
2. Build Game/Application participation configuration from Profile references.
3. Create mutable Session participation composition.
4. Join and/or apply explicit defaults.
5. Optionally update persistent ActorProfile selection.
6. Route or Activity projects participating Slots.
7. Required logical Actor hosts are resolved and materialized through the official contextual path:
   RuntimeContent for generic Actors or the Local Player Host Actor Mount for authorized local Players.
8. Effective occupancies are confirmed.
9. Input, camera and other runtime bindings attach to admitted instances.
10. Context exit releases bindings, occupancy and physical instances in order.
```

The game construction determines which steps are visible to the user. It does not select a separate framework architecture.

## Expected flows

### Minimal single-player

```text
Game configuration
-> PlayerSlotProfile: Player 1
-> optional default ActorProfile

Session starts
-> Player 1 Slot is available
-> explicit join provisions the technical Local Player Host
-> Player 1 Slot becomes Joined
-> explicit default may resolve selection

Gameplay Route enters
-> projects Player 1
-> materializes the selected/default logical Actor host
-> confirms effective occupancy
```

### Local multiplayer with selection

```text
Game configuration
-> PlayerSlotProfile: Player 1
-> PlayerSlotProfile: Player 2

Session
-> players join
-> selection UI assigns ActorProfile references

Gameplay Route
-> projects joined/ready Slots
-> materializes selected logical Actor hosts
-> confirms one occupancy per admitted Slot
```

### Menu controlled by several Slots

```text
Session
-> Player 1 and Player 2 are joined

Menu Route
-> accepts UI participation from both Slots
-> may use Profile color/icon for UI differentiation
-> may materialize contextual pointers
-> does not require gameplay Actor occupancy
```

## Product authoring direction

```text
Create Player Slot Profile
-> set canonical PlayerSlotId
-> set display name, color, icon and order

Create Participation Configuration
-> add PlayerSlotProfile references
-> set required/optional policy
-> set optional default ActorProfile

Session diagnostics
-> Profile
-> PlayerSlotId
-> available/joined/ready
-> selected ActorProfile

Route/Activity diagnostics
-> projected Slots
-> materialized Actors
-> effective occupancies
```

The minimal one-player construction should be creatable through a clear Create menu, wizard or template. It must not depend on hidden runtime fallback.

## Guardrails

```text
Do not create separate SinglePlayer and Multiplayer core flows.

Do not equate Slot Profile, Slot existence, join, selection, contextual
participation, Actor materialization or occupancy.

Do not require every Session Slot to have an ActorProfile selected.

Do not require every UI participant to have a gameplay Actor.

Do not mutate PlayerSlotProfile at runtime.

Do not duplicate PlayerSlotId strings in authoring consumers.

Do not store mutable Session state or gameplay GameObject references in Profiles.

Do not let Route or Activity redefine PlayerSlot identity.

Do not let PlayerInputManager select Slot/Profile or become contextual lifetime authority.

Do not create a second local Player spawner beside PlayerInputManager.

Do not let PlayerSlotOccupancy perform physical materialization.

Do not use PlayerInput.playerIndex, device id, GameObject name, tag or hierarchy
path as PlayerSlot or Actor identity.

Do not introduce a singleton, service locator or implicit global Player manager.

Do not silently create a default Slot or Actor when required authoring is absent.

Do not treat a joined Slot without an ActorProfile as an available Slot.

Do not derive Slot allocation from color, icon, name or Unity playerIndex.

Do not activate an Activity whose required participation Profile is missing or unsatisfied.

Do not remove existing Players merely because dynamic join capacity was reduced.

Do not treat dynamic capacity reduction as an implicit leave command.
```

## Out of scope

This ADR does not decide:

```text
the final Participation Configuration asset/type
the exact Session composition API
the exact join-device policy
explicit/non-default Slot targeting beyond the initial ordered allocation policy
the exact occupancy operation/result type
the exact explicit leave/reconfiguration operation
network replication or authority
online Player admission and synchronization
split-screen viewport assignment
```

Those decisions must preserve the Profile/static-data and runtime-state separation.

## Technical acceptance criteria

```text
PlayerSlotProfile is an immutable ScriptableObject authoring reference.

PlayerSlotId is authored once inside its Profile and exposed as a typed identity.

Session state references Profiles without mutating them.

A Session Slot can exist without ActorProfile selection or physical instance.

A Route/Activity can use a subset of Session Slots.

A menu can accept several Slot inputs without gameplay Actor occupancy.

Physical Logical Actor lifetime is explicitly Route-owned or Activity-owned.

A technical Local Player Host may be provisioned by PlayerInputManager and admitted to
the Session without a Logical Actor. The Logical Actor remains owned and admitted by
the explicit Route/Activity context.

Effective occupancy is confirmed only for an admitted active Actor.

Missing required configuration fails explicitly and diagnostically.

Dynamic capacity can change without mutating PlayerSlotProfile assets.

Reducing capacity blocks future joins and does not silently evict admitted Players.
```

## Product acceptance criteria

```text
A designer creates and differentiates Slots through PlayerSlotProfile assets.

Slot name, color, icon and identity are not duplicated across authoring surfaces.

A minimal explicit one-player configuration is easy to create.

The same flow evolves to several Slots without replacing the architecture.

A selection screen is optional, not foundational.

Advanced/Debug distinguishes:
Profile,
runtime Slot state,
selection,
projection,
materialization,
occupancy.
```

## Consequences

### Positive

```text
Single-player and multiplayer share one product model.

Slots become product-customizable without mutable ScriptableObject state.

Menus can use Profile metadata for consistent player differentiation.

Selection is no longer confused with occupancy.

PlayerSlotId has one canonical authoring source.

Route and Activity retain explicit lifecycle authority.

Session capacity can change without destructive Player removal.
```

### Cost

```text
The framework needs PlayerSlotProfile and explicit participation configuration.

Existing Slot authoring fields must be reconciled with Profile references.

Runtime Session composition must keep mutable state separate from assets.

Validators must detect duplicate PlayerSlotId values across Profiles.
```

## Suggested commit message

```text
Docs: define Player Slot Profiles and contextual participation
```
