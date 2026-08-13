# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: **Accepted**  
Last updated: 2026-08-13  
Proposed reconciliation draft: **2026-08-11 — R6 / R7 / R8**  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016, IF-ADR-019, IF-ADR-020

> **Draft note:** the R6/R7/R8 targeted-Join and explicit Actor Selection portions remain
> a proposed reconciliation of the accepted ADR. The IF-ADR-020 `Request Leave` boundary
> below is separately accepted/reconciled/implemented and must not be read as promoting
> unrelated draft deltas.
>
> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Route- and Activity-owned consumers need to request supported Player operations
and inspect immutable Session evidence without becoming Player authority.

The core separates:

```text
Join / Slot allocation
Actor selection
Actor preparation/materialization
Session Player Leave
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
  -> owns mutable Slot, Host, Actor, Joining and Leave state
```

## Public command vocabulary

Accepted consumer intent includes:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
Request Leave
```

The R6/R7/R8 reconciliation draft additionally defines the proposed bounded intents:

```text
Request Join To Slot
Request Actor Selection
```

`Request Leave` is accepted by IF-ADR-020 and targets an existing joined Session Player;
it is not another admission operation and it never allocates a target.

### Open / Close Joining

Joining state controls admission.

It does not select a Slot, select an Actor or mutate current Player physical
representation. Joining Closed does not block an existing Player from Leaving.

### Request Join

`Request Join` preserves IF-ADR-016 first-vacant-Supported-Slot semantics.

```text
Joining Open
  -> first eligible vacant Supported Slot in authored order
```

The consumer does not reserve a Slot directly.

### Request Join To Slot — R6/R7/R8 draft

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

`Request Join To Slot` does not accept `ActorProfile`.

### Request Default Actor Selection

This convenience operation applies the configured default Actor intent for one
exact Joined Slot through the canonical Actor-selection authority.

It remains distinct from Join.

### Request Actor Selection — R6/R7/R8 draft

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
accepts it. A currently prepared Actor blocks direct selection mutation; the command
fails explicitly instead of hot-swapping the physical representation.

### Request Leave

IF-ADR-020 extends the same scoped command model with explicit Session Player Leave.
The accepted semantic request identifies:

```text
exact Player Slot
expected current Session Player occurrence/revision
source
reason
```

The current package implementation may express those fields through its concrete typed
request/result contracts; this ADR fixes the semantics rather than inventing alternate
DTO names.

The Session validates that the Slot is supported, currently Joined and still correlated
to the expected occurrence before destructive release begins.

A successful request:

```text
may release current Activity representation
releases provisioning-specific Session resources
terminates the exact Session Player occurrence
commits Slot -> Vacant / Available
```

A stale request for occurrence A must reject after the same Slot has been reused by
occurrence B. Joining may be Closed during Leave; successful Leave does not reopen it.

## Session lifetime boundary

IF-ADR-019 defines how Join and observation are interpreted across Activity boundaries.
A successful Join establishes Session membership once. Activity
projection/reprojection for that same Joined Player is not another consumer Join
operation, and contextual Activity release is not a Leave command.

IF-ADR-020 defines the explicit terminal command for one Session Player occurrence.
Therefore:

```text
Activity exit
  -> release contextual representation
  -> Session Player remains Joined

Request Leave
  -> release contextual representation when present
  -> release Session-owned resources according to provisioning mode
  -> terminate Session Player occurrence
  -> Slot becomes Vacant / Available

Session termination
  -> separate aggregate lifecycle operation
```

Observation should distinguish, where the product surface exposes the evidence:

```text
Session
  Slot Joined / Available
  current occurrence/revision
  Actor selection/revision
  Session-owned Manager-Provisioned Host evidence

Current Activity
  participating / excluded
  representation state
  current Actor occurrence
  readiness / gameplay / camera bindings
```

For Scene-Provided Players, contextual release may leave the Slot Joined while the
scene-owned Host/Actor occurrence is removed. For Manager-Provisioned Players, normal
Activity exit must not imply release of the admitted Session-owned Host/`PlayerInput`.
Explicit Leave is the accepted individual Session termination path.

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

Join, Actor Selection and Leave use the same scoped consumer philosophy.
They must not require a consumer to locate internal runtime contexts, preparation
modules or provisioning bridges directly.

## Observation

Observation is immutable evidence derived from runtime authorities. It may expose:

```text
Session initialization evidence
Joining state
Supported Slot occupancy
current Session Player occurrence/revision
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

A retained summary or snapshot is also not automatically current authority. A diagnostic
object may legitimately remain present after release with an operational state such as:

```text
Admission  NotAdmitted
Camera     NotEvaluated
Occupancy  Vacant
```

or another released/baseline state. Consumers and QA must use current operational state
and occurrence correlation rather than object existence to infer live authority.

For Leave, diagnostics should expose enough typed evidence to establish at least:

```text
target Slot
expected/current occurrence correlation
result status
whether Activity representation release completed
whether provisioning release completed
whether terminal Session commit completed
whether partial release occurred
```

## Authoring boundary

`PlayerProvisioningCommandTrigger` executes only explicit user/game operations;
it does not provision, select an Actor or Leave from `Awake`, `OnEnable`, `Start` or
`OnValidate`.

The designer-facing trigger may expose operation-specific fields. A Leave command must
have an explicit Player target; it must not silently select the first joined Player.

`PlayerProvisioningStatusBinding` remains read-only and may correlate current
observation with the latest explicit trigger result.

Normal Inspector information is designer-facing. Deeper revisions,
owner/occurrence correlation and technical evidence belong in Advanced / Debug.

The implementation may keep smaller typed internal/public ports instead of
forcing every command into one oversized interface, but the product must present
one coherent bounded Player control surface.

## Transaction boundaries

Join, Actor Selection, Actor Preparation and Leave remain distinct transactions.

Leave is additionally a staged release transaction:

```text
validate exact Slot + occurrence
-> stage Leaving
-> release current Activity representation when present
-> release provisioning-specific Session resources
-> terminal Session commit
-> Slot Available / Vacant
```

No consumer may replace that transaction with direct Slot mutation or physical object
destruction.

## No-fallback rules

The consumer surface must fail explicitly rather than:

```text
targeted Join -> choose another Slot
Actor selection -> choose configured default automatically
Actor selection -> hot-swap a prepared Actor
Actor selection -> prepare/materialize Actor directly
Leave -> choose whichever Player currently occupies some other Slot
stale Leave -> remove a later occurrence that reused the Slot
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
- Simulating Leave by destroying a Player GameObject or calling contextual Scene-Provided release.
- Leave without explicit Slot/current-occurrence correlation.
- Capacity commands or a second Session limit.
- Separate provisioning Profile.
- Per-Slot Host Provisioning override.
- Generic character roster/unlock/store/selection-flow system.
- Global Player command manager/service locator.

## Integration and product improvement

Technical certification and FIRSTGAME real-integration status are tracked in the
framework Tracker.

IF-ADR-020 focused technical proof validates the public Manager-Provisioned Leave command
through the same scoped consumer boundary. FIRSTGAME still needs to demonstrate a normal
consumer surface and usability; this does not change the accepted command authority.

UX friction observed during real-product work may justify optional product improvement.
A Wizard/Composer/Create flow is not automatically required; the primary requirement is
a coherent explicit command surface with useful diagnostics.
