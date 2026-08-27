# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: **Accepted / Reconciled / Implemented**  
Last updated: **2026-08-26**  
Current public-surface reconciliation: [IF-ADR-015B — Player Actor Selection Public Surface Certification — 2026-08-26](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)  
Previous public-surface reconciliation: [IF-ADR-015A — Player Session Observer and Explicit Command Surfaces — 2026-08-25](../Reconciliation/IF-ADR-015A-Player-Session-Observer-and-Explicit-Command-Surfaces-2026-08-25.md)  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021

> The current implemented authoring surface is the `PlayerSessionObserver` plus
> eight explicit Player Session command components. The older enum-driven generic
> command trigger is no longer the product surface.
>
> Arbitrary Actor Selection is now delivered through explicit Select / Default /
> Replace / Clear commands. Exact-Slot public Join and Local Multiplayer
> Slot/device/input ownership remain separate future scope.

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage.

## Context

Route- and Activity-owned consumers need to request supported Player operations
and inspect immutable Session evidence without becoming Player authority.

The consumer surface must keep three responsibilities distinct:

```text
Session / Player runtime
  owns mutable Slot, Host, Actor, Joining and physical-lifetime truth

Observer
  reads published scoped Session evidence

Command component
  explicitly requests one supported operation
```

A consumer must not need a direct reference to the physically materialized Player
GameObject in order to inspect the Player Session.

## Decision

The package exposes typed scoped consumer access, immutable observation and explicit
command components over the existing Session / Player authorities.

Canonical product rule:

```text
PlayerSessionObserver = read
Player Session Command Trigger = request/change
```

Both use the existing scoped consumer access boundary. Neither becomes Session authority.

```text
Player Session authority
        ↓
scoped consumer access
   ┌────┴────┐
   │         │
 READ     REQUEST
   │         │
Observer   explicit command components
```

## Current implemented public command surface

The current designer-facing command components are:

```text
PlayerSessionOpenJoiningCommandTrigger
PlayerSessionCloseJoiningCommandTrigger
PlayerSessionJoinCommandTrigger
PlayerSessionSelectActorCommandTrigger
PlayerSessionDefaultActorSelectionCommandTrigger
PlayerSessionReplaceActorSelectionCommandTrigger
PlayerSessionClearActorSelectionCommandTrigger
PlayerSessionLeaveCommandTrigger
```

Each component represents exactly one operation and contains only its applicable
serialized intent.

The shared `PlayerSessionCommandTriggerBase` is internal implementation infrastructure.
It centralizes scoped access, invocation metadata, diagnostics and common result logging;
it is not a product-facing generic command selector and owns no Session state.

The former authoring model:

```text
PlayerSessionCommandTrigger
  Operation = PlayerProvisioningCommandOperation
```

is superseded. A serialized enum no longer changes the identity and complete semantic
shape of one MonoBehaviour.

## Open / Close Joining

Open and Close Joining mutate only the Session Joining posture through the canonical
consumer access surface.

They do not select a Slot, select an Actor or materialize Player representation.

## Join

`PlayerSessionJoinCommandTrigger` requests the existing ordinary Join contract.

Current public Join behavior is untargeted with respect to Slot selection: it uses the
Session's supported Slot order and current eligibility. It may carry the optional
Control Scheme hint already supported by the Join request.

The current command does not expose exact-Slot Join and does not expose a public paired
device/InputUser ownership contract.

Join does not select an arbitrary Actor directly and does not materialize Player
representation itself.

## Actor Selection

Actor selection is a Session-owned logical selection transaction for one exact Joined
Player Slot.

The four public Actor-selection commands are explicit and typed:

```text
Select Actor
  PlayerSessionSelectActorCommandTrigger

Select Default Actor
  PlayerSessionDefaultActorSelectionCommandTrigger

Replace Actor Selection
  PlayerSessionReplaceActorSelectionCommandTrigger

Clear Actor Selection
  PlayerSessionClearActorSelectionCommandTrigger
```

All four return `PlayerActorSelectionResult` evidence and execute through the same scoped
consumer-access boundary.

### Select Actor

`PlayerSessionSelectActorCommandTrigger` requests one explicit `ActorProfile` for one
exact Joined `PlayerSlotId`.

Typical authored intent:

```text
Player Slot
Actor Profile
Expected Selection Revision
Reason
```

The game may own which Actor choices are presented to the user. It does not own the
selection commit or Session mutation.

### Select Default Actor

`PlayerSessionDefaultActorSelectionCommandTrigger` requests the configured default Actor
for one exact Player Slot.

It is policy-aware:

```text
ResolveConfiguredDefault
  -> use configured DefaultActorProfile only

LeaveUnresolved
  -> RejectedDefaultResolutionDisabled
```

There is no silent fallback to another Actor.

### Replace Actor Selection

`PlayerSessionReplaceActorSelectionCommandTrigger` replaces already selected logical
Actor intent before the canonical preparation barrier.

It does not hot-swap or tear down an already prepared/admitted physical Actor.

### Clear Actor Selection

`PlayerSessionClearActorSelectionCommandTrigger` clears logical Actor selection before
the canonical preparation barrier.

It does not tear down an already prepared/admitted physical Actor.

## Actor-selection lifecycle barrier

Actor selection remains separate from Actor preparation/materialization.

The public Actor commands flow through the canonical preparation context before mutable
Session selection authority:

```text
explicit Actor command
  -> scoped consumer access
  -> PlayerActorPreparationRuntimeContext
  -> PlayerParticipationRuntimeContext
```

Select / Replace / Clear reject once a Logical Player Actor is already prepared or when a
retained preparation/release failure is acting as the canonical barrier.

The public rejection is:

```text
RejectedLogicalActorAlreadyPrepared
```

This prevents a selection command from becoming an implicit physical Actor hot-swap.
The internal prepared-Actor replacement transaction remains internal and is not exposed
as a consumer command.

## Revision, idempotency and duplicate policy

Selection is revision-aware.

Canonical behavior includes:

```text
Select A with no selected Actor
  -> SucceededSelected
  -> Selection / Slot / Session revisions advance once

Select A when A is already selected
  -> idempotent success
  -> revisions unchanged

Select B while A is selected
  -> reject; use Replace
  -> no mutation

Replace B before preparation
  -> SucceededReplaced
  -> revisions advance once

Clear before preparation
  -> SucceededCleared
  -> revisions advance once

stale expected selection revision
  -> RejectedStaleSelectionRevision
  -> no mutation
```

Duplicate Actor selection remains governed by the Session duplicate-selection policy.

## Leave

`PlayerSessionLeaveCommandTrigger` requests Leave for one explicit Player Slot and uses
the current scoped observation to correlate the joined occurrence when the advanced
revision override is left at its default.

Leave authority and release semantics remain governed by IF-ADR-020.

## Remaining proposed command extensions

The following remains outside the current delivered command set:

```text
Request Join To Exact Slot
```

Exact-Slot Join would express an explicit target Slot plus optional Input System hints.
The Session would validate and own the reservation/admission transaction. Failure must
not silently fall back to another Slot.

Local Multiplayer additionally requires a sufficient public Slot/device/input ownership
and observation contract; that is not implied by the current ordinary Join command.

## Initialization boundary

IF-ADR-016 remains the authored Session initialization source:

```text
PlayerSessionProfile
  Supported Slots
  Initial Joining
  Host Provisioning
  Actor Resolution
```

Commands operate on the created Session. They never mutate or reapply the Profile.

`Actor Resolution = LeaveUnresolved` is an intentional valid policy for flows where Actor
selection must remain pending after Join, including Character Selection.

## Scoped access

Consumer access remains:

```text
typed
Route- or Activity-scoped
lifetime-explicit
stale-scope rejecting
diagnostic when unavailable
free of serialized cross-scene authority references
```

Route- or Activity-scoped consumer lifetime is an access boundary only. It does not
transfer provisioning, Slot, Actor or physical-lifetime authority from Session to the
consumer scene.

A valid authored component may temporarily have no live runtime binding. Therefore:

```text
TryValidateConfiguration()
  = authoring/configuration validity

BindingState / IsScopedAccessAvailable / TryGetAccess
  = runtime scoped-access availability
```

These are separate contracts.

Likewise, physical location in Route-discovered content does not force Route ownership.
An Activity-scoped consumer may be discovered from Route content and bind later during
the Activity lifecycle.

`PlayerSessionScopedAccessConsumer.TryBind(...)` remains the canonical scope-match
boundary and rejects actual/authored scope mismatch.

No public static registry, service locator, reflection, scene-wide authority search or
hierarchy/name inference is required.

## PlayerSessionObserver

`PlayerSessionObserver` is the read-only scene/prefab surface for current Player Session
observation.

It is intentionally suitable for:

```text
Hub
UI
presentation
prefabs
another scene than the physically materialized Player
```

Its contract is:

```text
Player Session authority
        ↓
scoped public observation
        ↓
PlayerSessionObserver
        ↓
consumer presentation
```

The Observer may expose published or derived presentation evidence such as:

```text
Session initialization evidence
Joining state
Supported Slot occupancy
Session/applied revision
Activity owner/occurrence
Host correlation
selected Actor
Actor selection revision
Logical Actor preparation
physical Actor materialization
gameplay admission
```

Observation is immutable evidence, not a mutable second state store.

The Observer does not:

```text
execute commands
own Player truth
materialize a Player
locate the physical Player GameObject
aggregate the result of command components
```

The previous `PlayerSessionStatus` name was replaced because `Observer` more accurately
communicates that this MonoBehaviour can observe the scoped Session from a different
scene or presentation surface without implying runtime authority.

## Command result ownership

Each explicit command component owns only its own result evidence.

```text
Open Joining
  -> PlayerParticipationOperationResult

Close Joining
  -> PlayerParticipationOperationResult

Join
  -> LocalPlayerJoinResult

Select Actor
  -> PlayerActorSelectionResult

Select Default Actor
  -> PlayerActorSelectionResult

Replace Actor Selection
  -> PlayerActorSelectionResult

Clear Actor Selection
  -> PlayerActorSelectionResult

Leave
  -> SessionPlayerLeaveResult
```

Current Session observation and the result of one transient command invocation are
separate concepts.

Therefore the Observer does not expose a global `LastOperation*` aggregator and no
replacement global "last Player command" store is introduced.

## Authoring boundary

Command components execute only through explicit consumer invocation. They do not issue
commands from `Awake`, `OnEnable`, `Start` or `OnValidate`.

Normal Inspector composition follows IF-ADR-010:

```text
PlayerSessionObserver
  Scope
  runtime observation when applicable
  Validation
  Advanced / Debug

explicit command component
  Scope
  command-specific intent
  Validation
  Advanced / Debug
```

`Reason`, revision/occurrence overrides, detailed runtime evidence and manual Play Mode
`Invoke` testing belong under `Advanced / Debug`.

There is no Apply/Rebuild contract for these surfaces.

Full authoring validation is explicit; it must not be recomputed as a hidden full
validation operation on every Inspector repaint.

## Current gameplay-input consumer boundary

The public `PlayerGameplayInputConsumerBinding` / `IPlayerGameplayInputReader` surface is
a downstream Activity-current gameplay consumer. It does not extend the provisioning
command vocabulary and does not make consumer code a Player authority.

```text
Session / Player authority
  -> exact current Player occurrence
  -> current Activity Actor preparation
  -> current gameplay admission/input/camera chain
  -> GameplayReady
  -> PlayerGameplayInputConsumerBinding
  -> gameplay-owned Move/Look/etc. consumer
```

It must fail closed when no current gameplay binding exists and must not request Join,
change Joining, select an Actor, prepare/materialize an Actor or perform global lookup.

## Transaction boundaries

Join, Actor Selection and Actor Preparation remain separate transactions.

The consumer surface must not collapse them into one opaque command such as:

```text
Join Player As Actor And Materialize
```

Likewise, `Replace Actor Selection` is not equivalent to replacing a prepared physical
Actor.

This separation preserves diagnostics and avoids moving lifecycle authority into game UI.

## No-fallback rules

The consumer surface must fail explicitly rather than:

```text
invalid scope -> search for another Session
missing scoped access -> global lookup
targeted Join -> choose another Slot
Actor selection -> silently choose another Actor
Actor selection -> prepare/materialize Actor directly
Actor replacement intent -> hot-swap prepared Actor implicitly
Leave -> infer a different Player Slot
```

## Serialization and migration reconciliation

The 2026-08-25 public-surface cut recorded:

```text
PlayerSessionStatus
  -> PlayerSessionObserver

PlayerSessionCommandTrigger + PlayerProvisioningCommandOperation
  -> explicit command components
```

The `PlayerSessionStatus` script GUID was preserved for the Observer so existing serialized
references to that script identity could migrate without inventing a parallel observation
surface.

The 2026-08-26 Actor-selection cut added explicit Select / Replace / Clear command
components, routed Default through the same scoped command surface, and removed the old
public Actor-selection authoring/binder path after confirming no active serialized
consumers required it.

Detailed records:

- [IF-ADR-015A — Player Session Observer and Explicit Command Surfaces — 2026-08-25](../Reconciliation/IF-ADR-015A-Player-Session-Observer-and-Explicit-Command-Surfaces-2026-08-25.md)
- [IF-ADR-015B — Player Actor Selection Public Surface Certification — 2026-08-26](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)

## Deferred command-surface readiness issue

A consumer integration run exposed one non-blocking follow-up:

```text
first interaction
  -> scoped command access may still be Unbound
  -> RejectedRuntimeUnavailable

binding completes

subsequent interaction
  -> Bound
  -> command may proceed
```

This is tracked as **PLAYER-COMMAND-SURFACE-READINESS / DEFERRED**.

The future goal is to make command availability distinguishable before normal UI
interaction is enabled, without adding fallback, polling-based alternate authority or a
second Session-discovery channel.

This follow-up does not change the command/observer composition defined by this ADR.

## Rejected scope

- Consumer direct Slot reservation/mutation.
- Consumer Actor preparation/materialization authority.
- Consumer physical Actor hot-swap authority.
- Consumer gameplay admission or Activity reconcile authority.
- Readiness mutation from game UI.
- Automatic Join, fake readiness or silent fallback.
- Combined Join + Actor Selection + materialization command.
- Global Player command manager/service locator.
- Observer as a second Player state store.
- Observer dependency on physical Player GameObject lookup.
- Reintroducing one serialized enum-driven command component for semantically distinct commands.

## Certification and product consequence

The public Actor-selection extension is certified by the 2026-08-26 Full Player aggregate:

```text
PLAYER CURRENT AGGREGATE COMPLETE
mandatoryContracts = 27
executedContracts = 27
passedContracts = 27
actor = PASS
publicSurface = PASS
```

The historical Full Player `25/25` remains dated evidence for its earlier boundary.

The public arbitrary Actor-selection blocker for the Character Selection sample is
therefore closed. Character Selection may now proceed using normal game-owned UI plus the
public explicit Actor-selection command surface.

Exact-Slot Join and public Slot/device/input ownership remain separate future work and
continue to block the canonical Local Multiplayer sample.
