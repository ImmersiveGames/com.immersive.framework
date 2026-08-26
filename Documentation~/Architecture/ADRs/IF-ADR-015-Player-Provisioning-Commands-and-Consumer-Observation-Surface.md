# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: **Accepted**  
Last updated: **2026-08-25**  
Current public-surface reconciliation: **2026-08-25 — Player Session Observer + explicit command components**  
Proposed extension draft: **2026-08-11 — R6 / R7 / R8**  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021

> The current implemented authoring surface is the `PlayerSessionObserver` plus
> explicit command components. The older enum-driven generic command trigger is
> no longer the product surface.
>
> The R6/R7/R8 exact-Slot Join and arbitrary Actor-selection extensions remain
> separate proposed scope unless and until their implementation status is
> explicitly promoted by the current Framework Tracker.

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
PlayerSessionDefaultActorSelectionCommandTrigger
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

### Open / Close Joining

Open and Close Joining mutate only the Session Joining posture through the canonical
consumer access surface.

They do not select a Slot, select an Actor or materialize Player representation.

### Join

`PlayerSessionJoinCommandTrigger` requests the existing ordinary Join contract.

It may carry the optional Control Scheme hint already supported by the Join request.
It does not select an Actor directly and does not materialize Player representation.

### Default Actor Selection

`PlayerSessionDefaultActorSelectionCommandTrigger` requests the configured default Actor
for one exact Player Slot through the existing Actor-selection authoring authority.

It remains separate from Join.

### Leave

`PlayerSessionLeaveCommandTrigger` requests Leave for one explicit Player Slot and uses
the current scoped observation to correlate the joined occurrence when the advanced
revision override is left at its default.

Leave authority and release semantics remain governed by IF-ADR-020.

## Proposed command extensions

The following vocabulary remains proposed extension scope and must not be confused with
the current implemented explicit component set:

```text
Request Join To Slot
Request Actor Selection (arbitrary ActorProfile)
```

### Request Join To Slot

Exact-Slot Join would express an explicit target Slot plus optional Input System hints.
The Session would validate and own the reservation/admission transaction. Failure must
not silently fall back to another Slot.

### Request Actor Selection

Arbitrary Actor Selection would express:

```text
Player Slot
ActorProfile
optional expected selection revision
source
reason
```

The Session / Actor-selection authority remains mutable authority. The consumer must not
prepare/materialize the Actor or hot-swap a prepared physical representation directly.

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

Default Actor Selection
  -> PlayerActorSelectionResult

Leave
  -> SessionPlayerLeaveResult
```

Current Session observation and the result of one transient command invocation are
separate concepts.

Therefore the Observer does not expose `LastOperation*` and no replacement global
"last Player command" aggregator is introduced.

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

This separation preserves diagnostics and avoids moving lifecycle authority into game UI.

## No-fallback rules

The consumer surface must fail explicitly rather than:

```text
invalid scope -> search for another Session
missing scoped access -> global lookup
targeted Join -> choose another Slot
Actor selection -> silently choose another Actor
Actor selection -> prepare/materialize Actor directly
Leave -> infer a different Player Slot
```

## Serialization and migration reconciliation

The 2026-08-25 public-surface cut records:

```text
PlayerSessionStatus
  -> PlayerSessionObserver

PlayerSessionCommandTrigger + PlayerProvisioningCommandOperation
  -> explicit command components
```

The `PlayerSessionStatus` script GUID was preserved for the Observer so existing serialized
references to that script identity can migrate without inventing a parallel observation
surface.

Known serialized generic command usages in the Player sample were migrated explicitly to
Join and Leave components and their UnityEvents now call the corresponding `Invoke()`.
No automatic migrator was introduced because no additional local serialized usages were
found that justified one.

Detailed implementation/migration evidence is recorded in:

`../Reconciliation/IF-ADR-015A-Player-Session-Observer-and-Explicit-Command-Surfaces-2026-08-25.md`.

## Deferred command-surface readiness issue

A consumer integration run exposed one non-blocking follow-up:

```text
first Join interaction
  -> scoped command access still Unbound
  -> RejectedRuntimeUnavailable

binding completes

subsequent Join
  -> Bound
  -> SucceededJoined
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

## Integration and product improvement

The current explicit command composition is implemented in Framework commit:

```text
08e1f655a344b71d0d5ef37c7e41ebb58807aa00
PLAYER SESSION PUBLIC SURFACE
```

FIRSTGAME / Player Provisioning consumer integration is recorded independently from
technical certification. The sample may compose an Observer when it needs read-only
Session presentation and explicit command components when it needs requests; neither is
a prerequisite for the other.

Future exact-Slot Join or arbitrary Actor-selection work must preserve this same product
principle: explicit consumer intent, typed scoped access, no parallel authority and no
silent fallback.
