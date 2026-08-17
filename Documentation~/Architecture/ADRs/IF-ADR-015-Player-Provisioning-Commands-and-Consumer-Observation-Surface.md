# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: **Accepted**  
Last updated: 2026-08-17  
Proposed reconciliation draft: **2026-08-11 — R6 / R7 / R8**  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016

> **Draft note:** this file is a proposed reconciliation of the accepted ADR after
> the R6/R7/R8 architecture review. It has not been applied to the repository yet.
> It extends the bounded consumer vocabulary with exact-Slot Join and explicit
> Actor Selection while preserving Session authority.

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Route- and Activity-owned consumers need to request supported Player operations
and inspect immutable Session evidence without becoming Player authority.

The existing core already separates:

```text
Join / Slot allocation
Actor selection
Actor preparation/materialization
```

The consumer surface must expose useful Player intent without bypassing those
authorities.

## Decision

The package exposes typed scoped consumer access, a bounded public command
vocabulary, immutable observation and optional designer command/status surfaces.
Existing Session and Player authorities execute requests and remain the single
mutable truth.

```text
Package Player Surface
  -> supported requests + immutable observation

Consumer UI / game code
  -> requests operations + presents observation

Session / Player runtime
  -> owns mutable Slot, Host, Actor and Joining state
```

## Public command vocabulary

Accepted consumer intent is:

```text
Open Joining
Close Joining
Request Join
Request Join To Slot
Request Default Actor Selection
Request Actor Selection
```

This reconciliation does not add Session Player Leave; that belongs to its
separate architecture decision when accepted/reconciled.

### Open / Close Joining

Joining state controls admission.

It does not select a Slot, select an Actor or mutate current Player physical
representation.

### Request Join

`Request Join` preserves IF-ADR-016 first-vacant-Supported-Slot semantics.

```text
Joining Open
  -> first eligible vacant Supported Slot in authored order
```

The consumer does not reserve a Slot directly.

### Request Join To Slot

`Request Join To Slot` expresses exact Slot intent.

Conceptually:

```text
Target Player Slot
  Player2

optional Input System hints
  device
  control scheme

request metadata
  source
  reason
```

The Session validates and owns the reservation/admission transaction.

If the requested Slot is unavailable or invalid, the command rejects explicitly.

There is no fallback to another Slot.

The exact public request DTO/type name may be finalized in the implementation
cut, but the operation must remain explicit rather than hiding targeted behavior
behind an ambiguous default Slot value.

`Request Join To Slot` does not accept `ActorProfile`.

### Request Default Actor Selection

This convenience operation applies the configured default Actor intent for one
exact Joined Slot through the canonical Actor-selection authority.

It remains distinct from Join.

### Request Actor Selection

`Request Actor Selection` expresses:

```text
Player Slot
ActorProfile
optional expected selection revision
source
reason
```

The Session remains the mutable authority.

The consumer does not prepare/materialize the Actor.

Direct selection is valid only when the Actor-selection/preparation authority
accepts it.

A currently prepared Actor blocks direct selection mutation; the command fails
explicitly instead of hot-swapping the physical representation.

A future physical Actor-switch operation is outside this consumer vocabulary
until explicitly accepted.

## Initialization boundary

IF-ADR-016 is the sole authored Session initialization source:

```text
PlayerSessionProfile
  Supported Slots
  Initial Joining
  Host Provisioning
  Actor Resolution
```

Commands operate on the created Session. They never mutate/reapply the Profile.

Different target Slots and different Actor choices do not alter the Session-wide
Host Provisioning decision.

## Scoped access

Consumer access is:

```text
typed
Route- or Activity-scoped
lifetime-explicit
stale-scope rejecting
diagnostic when unavailable
free of serialized cross-scene authority references
```

No public static registry, service locator, reflection, scene-wide authority
search or hierarchy/name inference is required.

Targeted Join and Actor Selection use the same scoped consumer philosophy.

They must not require a consumer to locate internal
`PlayerParticipationRuntimeContext`, preparation modules or provisioning
bridges directly.

## Observation

Observation is immutable evidence derived from runtime authorities. It may expose:

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
latest bounded consumer operation/result
```

Observation is evidence, not a mutable second state store.

## Current gameplay-input consumer boundary

The public `PlayerGameplayInputConsumerBinding` / `IPlayerGameplayInputReader` surface is
a downstream Activity-current gameplay consumer. It does not extend the provisioning
command vocabulary above and does not make consumer code a Player authority.

Its accepted integration shape is:

```text
Session / Player authority
  -> exact current Player occurrence
  -> current Activity Actor preparation
  -> current gameplay admission/input/camera chain
  -> GameplayReady
  -> PlayerGameplayInputConsumerBinding
  -> gameplay-owned Move/Look/etc. consumer
```

The binding must fail closed when no current gameplay binding exists. It must not:

```text
request Join
open Joining
select an Actor
prepare/materialize an Actor
create gameplay admission
change Action Map posture
read authored InputActionReference.action as the live runtime source
perform global, hierarchy, reflection or name fallback
```

`InputActionReference` remains authored action identity; live values are resolved against
the exact current runtime input occurrence. Binding availability therefore follows the
current Activity gameplay occurrence rather than Session initialization alone.

In the current implementation, `BindingRevision == 0` means no runtime binding has yet
been committed by that consumer instance. Combined with `GameplayReady == false`, this
is compatible with an Activity whose authored requirement stops at
`LogicalActorsPrepared`; it is not sufficient evidence of a locomotion or provisioning
regression.

This clarification records the implemented consumer boundary only. It does not accept or
otherwise promote the R6/R7/R8 draft command extensions.

For targeted Join, diagnostics should expose at least:

```text
requested Slot
actual committed Slot when successful
rejection status/reason when unsuccessful
```

For Actor Selection, diagnostics should expose at least:

```text
target Slot
previous Actor selection
requested/current Actor selection
previous/current selection revision
result status
```

## Authoring boundary

`PlayerProvisioningCommandTrigger` executes only explicit user/game operations;
it does not provision or select an Actor from `Awake`, `OnEnable`, `Start` or
`OnValidate`.

The designer-facing trigger may expose operation-specific fields.

Example targeted Join:

```text
Operation
  Request Join To Slot

Player Slot
  Player2

Control Scheme
  optional
```

Example explicit Actor Selection:

```text
Operation
  Request Actor Selection

Player Slot
  Player2

Actor Profile
  Mage

Expected Selection Revision
  -1 or explicit current revision
```

`PlayerProvisioningStatusBinding` remains read-only and may correlate current
observation with the latest explicit trigger result.

Normal Inspector information is designer-facing. Deeper revisions,
owner/occurrence correlation and technical evidence belong in Advanced / Debug.

The implementation may keep smaller typed internal/public ports instead of
forcing every command into one oversized interface, but the product must present
one coherent bounded Player control surface.

## Transaction boundaries

Join, Actor Selection and Actor Preparation remain separate transactions.

Valid flow:

```text
Request Join To Slot Player2
        ↓
Player2 Joined
        ↓
Request Actor Selection Player2 -> Mage
        ↓
Activity/Framework preparation authority
        ↓
Mage representation prepared when required
```

The consumer surface must not combine those stages into:

```text
Join Player2 As Mage And Materialize
```

as one opaque command.

This separation preserves failure diagnostics and avoids making consumer UI
Player lifecycle authority.

## No-fallback rules

The consumer surface must fail explicitly rather than:

```text
targeted Join -> choose another Slot
Actor selection -> choose configured default automatically
Actor selection -> hot-swap a prepared Actor
Actor selection -> prepare/materialize Actor directly
invalid scope -> search for another Session
missing runtime binding -> global lookup
```

## Rejected scope

- Consumer direct Slot reservation/mutation.
- Consumer Actor preparation/materialization authority.
- Consumer physical Actor hot-swap authority.
- Consumer gameplay admission or Activity reconcile authority.
- Readiness mutation from game UI.
- Automatic Join, fake readiness or silent fallback.
- Targeted Join fallback to another Slot.
- Combined Join + Actor Selection + materialization command.
- Capacity commands or a second Session limit.
- Separate provisioning Profile.
- Per-Slot Host Provisioning override.
- Generic character roster/unlock/store/selection-flow system.
- Global Player command manager/service locator.

## Integration and product improvement

The architectural decision is accepted independently of mutable implementation
status. Technical certification and FIRSTGAME real-integration status are tracked
in the framework Tracker.

The R7/R8 implementation cut must prove both code and designer-facing access:

```text
Request Join
Request Join To Slot
Request Default Actor Selection
Request Actor Selection
```

FIRSTGAME should demonstrate a real flow where the developer can deliberately
choose the occupied Slot and later choose its Actor without reconstructing
internal runtime contracts.

UX friction observed during that work may justify optional product improvement.
A Wizard/Composer/Create flow is not automatically required; the primary
requirement is a coherent explicit command surface with useful diagnostics.
