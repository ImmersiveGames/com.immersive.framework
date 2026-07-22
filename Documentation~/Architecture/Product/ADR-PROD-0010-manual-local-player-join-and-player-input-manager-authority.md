# ADR-PROD-0010 — Manual local Player join and PlayerInputManager authority

Status: Accepted  
Date: 2026-07-12  
Amended: 2026-07-22 — reconciled with the P3 Local Player Host / Actor Mount model  
Package: `com.immersive.framework`  
Area: Local Player Provisioning / Unity Input System / Session Admission  
Related: `ADR-PROD-0006`, `ADR-PROD-0007`, `ADR-PROD-0008`, `ADR-PROD-0009`, `ADR-PROD-0011`, `ADR-PROD-0012`

## Context

The package currently depends on:

```text
com.unity.inputsystem 1.19.0
```

under the Unity 6.x package baseline.

The Unity Input System already provides `PlayerInputManager` as the technical coordinator for local multiplayer Player provisioning.

It handles capabilities that the framework must not reimplement:

```text
creation of one PlayerInput instance per local Player
InputUser and device pairing
control scheme assignment
private Action copies per Player
playerIndex allocation
join limits
join and leave notifications
device-loss and device-regain integration through PlayerInput
split-screen allocation support
```

The framework still owns different product responsibilities:

```text
PlayerSlotProfile identity
Session participation composition
ActorProfile selection
Route/Activity participation and lifetime policy
ActorId assignment
framework admission/readiness
occupancy
camera/input/gameplay activation policy
diagnostics
```

Using both `PlayerInputManager` and a second framework Player spawner would create competing creation authorities and duplicate Unity Input System behavior.

Using automatic join would also allow arbitrary input to create Players without an explicit product operation, Slot decision or contextual authorization.

The local Player flow therefore needs to use `PlayerInputManager` fully, but under explicit framework/product orchestration.

## Decision

`PlayerInputManager` is the official technical provisioner for technical Local Player Host instances. It does not materialize the contextual Logical Player Actor.

It operates in manual join mode.

```text
Join Behavior:
  Join Players Manually

Automatic button/action join:
  not used
```

A local Player is created only after an explicit authorized join request.

The join request may originate from:

```text
Framework Core flow
Route or Activity orchestration
Session/lobby product surface
consumer adapter
explicit game command
```

All origins must use one official typed framework bridge. They must not call `PlayerInputManager` as an untracked side path.

## Authority boundary

### Framework or authorized product adapter

Decides or provides:

```text
when a local join is requested
which Session/context authorizes the operation
which PlayerSlotProfile is eligible under the configured allocation policy
whether an ActorProfile is already selected, explicitly defaulted or still unresolved
whether capacity and participation policy allow the join
whether joining is currently open
what diagnostic reason initiated the operation
```

### PlayerInputManager

Executes Unity local-player provisioning:

```text
instantiates its configured local Player prefab
creates/configures PlayerInput
creates LocalPlayerHostAuthoring and its explicit Actor Mount from the configured prefab
assigns playerIndex
pairs InputUser and devices
selects/uses control scheme according to the request and Unity policy
applies local-player join limits
supports split-screen technical allocation
publishes joined/left notifications
```

### Framework admission after provisioning

Receives and validates the created instance:

```text
correlates it with the pending authorized join
finds LocalPlayerHostAuthoring
validates required PlayerInput and Actor Mount
validates that the provisioned Actor Mount starts without a Logical Actor
binds PlayerSlotProfile
records creation source as PlayerInputManager
admits the Player to Session participation
marks the Slot as Joined
retains selected ActorProfile state when one is already resolved, without materializing it during join
defers Activity-specific readiness to the Activity admission gate
materializes and binds the contextual Logical Actor later when Route/Activity requirements demand it
confirms occupancy only when an Activity requires and admits an active Actor
enables gameplay/camera behavior only after Activity requirements are satisfied
```

`PlayerInputManager` does not decide:

```text
PlayerSlotProfile identity
ActorProfile selection
ActorId
Route/Activity lifetime
occupancy
camera winner
gameplay readiness
```

## Canonical manual join flow

```text
1. Product/framework issues Local Player Join Request.
2. Framework validates context and dynamic capacity.
3. Framework resolves the first eligible Available Slot by configured array order.
4. Framework reserves that Slot and creates a pending join operation.
5. The pending operation becomes observable before JoinPlayer is called.
6. Framework enables/uses the authorized manual-join path.
7. Framework calls PlayerInputManager.JoinPlayer(...).
8. PlayerInputManager creates and configures the local PlayerInput instance.
9. JoinPlayer returns the newly created PlayerInput or null on failure.
10. The returned PlayerInput is correlated directly with the pending operation.
11. joined notification confirms the provisioning event and detects external divergence.
12. Framework validates LocalPlayerHostAuthoring, PlayerInput and the empty Actor Mount.
13. Framework binds PlayerSlotProfile to the technical host; no ActorId is assigned yet.
14. Framework retains ActorProfile selection when already explicitly resolved, without collapsing join and Actor preparation.
15. Slot becomes Joined and may participate in Session/UI flows.
16. Activity admission later evaluates whether Actor selection/preparation is required.
```

The pending operation must exist before the call because Unity lifecycle callbacks and
the manager's joined notification may occur during the synchronous provisioning call.

The framework must not depend on an arbitrary one-frame delay.

After `JoinPlayer(...)` returns a non-null `PlayerInput`, its Unity-assigned
`playerIndex` is valid technical evidence for that instance. It remains distinct from
`PlayerSlotId`.

Components on the instantiated prefab must not use their own `OnEnable` ordering as
the framework admission authority. `PlayerInput` performs initialization in `OnEnable`,
so framework binding begins from the explicit join operation/result rather than from
sibling component timing.

No step may infer product identity from GameObject name, prefab path or hierarchy.

## Correlation with PlayerSlot

`playerIndex` is Unity technical evidence. It is not `PlayerSlotId`.

```text
playerIndex
  Unity local Player ordering/index

PlayerSlotId
  framework participation identity
```

The framework should correlate the returned `PlayerInput` with the pending join request directly.

The official default policy is explicit:

```text
First Available By Configured Order
```

The framework evaluates the ordered `PlayerSlotProfile[]` configuration and reserves
the first Slot whose runtime allocation state is `Available`.

It must not derive the Slot from:

```text
PlayerInput.playerIndex
device id
Slot color or icon
GameObject name
hierarchy order
```

`playerIndex` may differ from configured Slot order and remains Unity technical evidence.

## Join completion and Activity readiness

Local join completion and gameplay readiness are different facts.

Canonical local join states:

```text
JoinRequested
SlotReserved
Provisioning
Provisioned
BindingSlot
Joined
Rejected
Released
```

`Joined` means:

```text
PlayerInput exists
PlayerSlotProfile is bound
the participant belongs to the Session
Slot presentation is available
Session/UI input may be used according to product policy
```

`Joined` does not universally require:

```text
ActorProfile selection
Actor-specific logical composition
gameplay occupancy
gameplay camera participation
gameplay input
```

Those requirements are evaluated by the Activity admission gate defined in
`ADR-PROD-0012`.

Before `Joined`, the Player must not participate as an admitted Session participant.

After `Joined`, the Player may participate in lobby, pointer or character-selection
flows while ActorProfile remains unresolved, provided the current Activity requirements
allow it.

## Local Player host requirements

The current executable P3 minimum for the provisioned technical host is:

```text
Unity PlayerInput
LocalPlayerHostAuthoring
explicit empty Actor Mount
```

The final Logical Player Actor product minimum also requires:

```text
Presentation / Skin
```

as defined by `ADR-PROD-0008`.

`PlayerInputManager` creates the configured technical Local Player Host. The framework validates it, admits its Slot as `Joined`, and leaves logical Actor preparation to the owning Route or Activity.

The canonical boundary is:

```text
fixed technical Local Player Host content
ActorProfile-specific Logical Actor content materialized below Actor Mount
Presentation/Skin content
```

The exact component APIs used by contextual materialization remain an implementation decision, but these three layers must not be collapsed.

## RuntimeContent boundary

The framework does not create a second local Player instance through RuntimeContent after `PlayerInputManager` has provisioned one.

Canonical distinction:

```text
generic Actor host
  physical creation through RuntimeContent

local Player host
  technical provisioning through PlayerInputManager
  framework Session admission afterward

local Player Logical Actor
  contextual materialization inside the host's Actor Mount
  Route/Activity ownership and ActorId assignment afterward
```

A local Player receives its ActorProfile-specific Logical Actor as contextual child content inside the Actor Mount after provisioning. Presentation remains a later layer below the Logical Actor.

The creation source must be diagnostic and explicit.

## Technical ceiling and dynamic Session capacity

`PlayerInputManager.maxPlayerCount` is a public read-only property in Input System
1.19.0 and is configured through serialized manager authoring.

Other split-screen layout constraints such as:

```text
fixedNumberOfSplitScreens
maintainAspectRatioInSplitScreen
splitScreenArea
```

are also exposed as read-only public configuration.

The framework must not mutate those fields through reflection, runtime
`SerializedObject`, private-field access or Editor-only code.

Canonical separation:

```text
PlayerInputManager.maxPlayerCount
  serialized technical hard ceiling for local PlayerInput instances

authored PlayerSlotProfiles
  product participation seats available to the game

Session dynamic join capacity
  mutable current policy controlling how many local Players may be admitted now
```

The dynamic Session capacity may increase or decrease while the Session is alive,
provided it does not exceed:

```text
available authored Player Slots
PlayerInputManager technical hard ceiling, when enabled
```

A request that exceeds either ceiling fails before `JoinPlayer(...)` is called.

If `maxPlayerCount` is negative, Unity's technical player-count limit is disabled;
the framework's authored Slot capacity and Session policy still apply.

### Capacity reduction policy

Reducing dynamic capacity below the number of already admitted Players is
non-destructive.

Example:

```text
4 Players are Ready
-> Session capacity changes to 2
-> all 4 remain admitted
-> new joins are blocked
```

The Session is temporarily over its join capacity until explicit leave or
reconfiguration reduces the admitted count.

Capacity changes must never silently:

```text
destroy PlayerInput instances
clear PlayerSlot occupancy
release Actor presentation
remove camera/input bindings
choose which Players must leave
```

Any enforced removal requires a separate explicit operation and policy.

## Join window

Manual join does not mean joining is always allowed.

The product/framework may explicitly open or close join availability for contexts such as:

```text
startup
lobby
character selection
mid-game join
pause
closed gameplay section
```

The exact join-window API and policy are deferred.

Automatic join by arbitrary unpaired input remains disabled.

## Unexpected or external join

A `PlayerInput` joined without a matching authorized pending operation is invalid.

Examples:

```text
consumer code called PlayerInputManager directly
automatic join was accidentally enabled
another component instantiated the Player prefab
a stale callback arrived after cancellation
```

Required response:

```text
do not admit the Player
do not assign occupancy
keep gameplay inactive
emit explicit diagnostic evidence
reject/release through the official operation policy
```

There is no silent adoption of an untracked Player.

## Leave, disconnection and recovery

`PlayerInputManager` and `PlayerInput` remain the Unity technical sources for local leave and device lifecycle notifications.

The framework must consume those notifications and reconcile:

```text
Session participation
Slot state
readiness
occupancy
camera/input bindings
release
diagnostics
```

The exact leave, disconnect, reconnect and device-replacement state machines are out of scope for this ADR.

## Single-player use

Single player uses the same manual provisioning flow.

```text
explicit startup/menu command
-> request Slot 1 join
-> PlayerInputManager provisions one technical Local Player Host
-> framework admits Slot 1 as Joined
-> target Activity resolves the configured/default ActorProfile when required
-> framework materializes the Logical Actor inside Actor Mount
-> Player becomes Ready when the Activity requirement is satisfied
```

No separate single-player spawning architecture is introduced.

## Unity singleton constraint

`PlayerInputManager` is a Unity Input System technical singleton-style component.

The framework must not create another global Player manager or service locator.

Framework integration rules:

```text
reference PlayerInputManager explicitly through authoring/injection
centralize access in the typed local-player provisioning bridge
do not spread PlayerInputManager.instance lookups through runtime code
do not treat the Unity singleton as framework product authority
```

## Guardrails

```text
Do not use automatic join.

Do not recreate PlayerInputManager device pairing, InputUser, Action Map,
player-index, join-limit or split-screen responsibilities.

Do not create a second Local Player Host after PlayerInputManager provisioning.

Do not place PlayerActorDeclaration on the provisioned technical host root.

Do not pre-populate the provisioned host's Actor Mount with a Logical Actor.

Do not assign ActorId or confirm occupancy merely because join completed.

Do not let PlayerInputManager choose PlayerSlotProfile or ActorProfile.

Do not equate playerIndex with PlayerSlotId.

Do not admit a joined Player without a correlated authorized request.

Do not activate gameplay, occupancy or camera participation before Ready.

Do not silently adopt unexpected joins.

Do not introduce a framework singleton, service locator or global Player manager.

Do not use name, path, tag or hierarchy lookup for Player correlation.

Do not wait one arbitrary frame as a required join-correlation mechanism.

Do not start framework admission from sibling component OnEnable ordering.

Do not mutate read-only PlayerInputManager limits through reflection or Editor APIs.

Do not silently remove admitted Players when Session capacity decreases.

Do not force future online/network admission through the local PlayerInputManager path.
```

## Out of scope

This ADR does not decide:

```text
the exact LocalPlayerJoinRequest fields
which request values are mandatory or optional
device-selection policy
control-scheme policy
playerIndex allocation policy
splitScreenIndex policy
join-window API
join cancellation
leave transaction
disconnect/reconnect state machine
the final field-level API of LocalPlayerHostAuthoring and Actor Mount binding
the final contextual materialization request/result API
Presentation/Skin implementation
camera-output creation for split-screen
online/network Player provisioning
online identity, authority, replication and reconnection
cross-client Player admission
```

Online/network participation is expected to require a more robust and separate
admission path. It must reuse the stable Slot/Profile/Actor identity boundaries where
appropriate, but it is not forced through the local `PlayerInputManager` join shape.

These decisions must preserve the authority boundary established here.

## Technical acceptance criteria

```text
PlayerInputManager is configured for manual join.

No automatic input action creates a Player.

Every local Player creation has a pending authorized framework operation.

PlayerInput returned by JoinPlayer is correlated to the reserved Slot and pending operation.

LocalPlayerHostAuthoring, PlayerInput and an empty Actor Mount are validated on the created technical host.

PlayerActorDeclaration is validated only on the contextual Logical Actor after materialization.

ActorProfile is optional for join completion and mandatory only when required by the
target Activity Profile.

playerIndex remains technical evidence, not framework Slot identity.

No second local Player instance is created through RuntimeContent.

The Player cannot become Session-joined before framework join admission completes.

Gameplay readiness is evaluated separately through Activity requirements.

Unexpected joins fail explicitly and diagnostically.

The pending join operation exists before PlayerInputManager.JoinPlayer is called.

A non-null JoinPlayer result is admitted without an arbitrary one-frame delay.

Framework admission does not depend on sibling OnEnable execution order.

PlayerInputManager.maxPlayerCount is treated as a serialized technical ceiling.

Session join capacity is mutable and cannot exceed authored/technical ceilings.

Reducing Session capacity blocks new joins and does not evict existing Players.

No framework singleton or service locator is introduced.
```

## Product acceptance criteria

```text
A product can request Player join from startup, lobby, UI or a game adapter.

The ordinary request automatically receives the first Available configured Slot.

Single player and local multiplayer use the same manual join operation.

The framework benefits from Unity device pairing, Action Maps and split-screen support.

A designer can understand which manager prefab and technical player ceiling are
used for local provisioning.

A product can increase or decrease Session join capacity without mutating the manager
or silently removing Players.

Advanced/Debug shows:
join request,
pending operation,
PlayerInput,
playerIndex,
PlayerSlotProfile,
ActorProfile,
ActorId,
admission state,
failure/release reason.
```

## Consequences

### Positive

```text
Unity Input System capabilities are reused instead of rebuilt.

Local Player creation has one technical provisioner.

Product identity and Session participation remain framework-controlled.

Automatic accidental joins are prevented.

Single-player and multiplayer share one explicit flow.

Future split-screen work can build on PlayerInputManager rather than replacing it.

Session capacity can change independently from Unity's serialized technical ceiling.

Join correlation no longer depends on historical one-frame timing assumptions.
```

### Cost

```text
The framework needs a typed provisioning/admission bridge.

A pending join correlation operation is required.

The local Player exists briefly before framework admission is complete.

The configured manager prefab and ActorProfile-specific Logical Actor require
separate authoring surfaces and explicit contextual materialization.

Leave, disconnect and reconnect need later scoped state-machine design.

The framework needs explicit over-capacity diagnostics when capacity is reduced below
the admitted Player count.

Online/network joining will require a separate future admission design.
```

## References

```text
Package dependency:
com.unity.inputsystem 1.19.0

Unity Input System 1.19.0 — PlayerInputManager manual:
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/PlayerInputManager.html

Unity Input System 1.19.0 — PlayerInput manual:
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/PlayerInput.html

Unity Input System 1.19.0 — PlayerInputManager API:
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.PlayerInputManager.html

Unity Input System 1.19.0 — PlayerInput API:
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.PlayerInput.html
```

## Suggested commit message

```text
Docs: align local Player provisioning with Input System 1.19 timing and capacity
```
