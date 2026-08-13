# IF-ADR-020 — Session Player Leave and Resource Release Authority

Status: **Proposed**  
Date: 2026-08-11  
Last updated: 2026-08-12  
Type: architecture / runtime authority / player product direction  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019  
Source finding: pre-FIRSTGAME architecture review — R3 Player Leave

> This ADR is a proposed architectural decision. It defines the intended Session Player
> Leave contract and resource-release boundary. It does not promote current Experimental
> APIs to Stable and does not claim that the package already implements Session Player
> Leave.
>
> IF-ADR-019 is now accepted, implemented and QA-certified. This ADR therefore depends
> on an established Session Player lifetime boundary rather than a proposed one.

## Context

The accepted Player architecture already supports explicit admission and contextual
release, but it does not yet define how one joined Player leaves the Session.

Current public provisioning commands cover:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

There is no canonical Session Player Leave command.

The package does contain lower-level release behavior, but those operations have narrower
meanings.

Examples include:

```text
Scene-Provided Player
  RequestRelease
    releases one scene/contextual admission

Local Player Host
  TryReleaseCommittedAdmission
    releases typed Host admission evidence

Unity Local Player provisioning backend
  RejectPlayer
    destroys a PlayerInput object created by a rejected provisioning attempt
```

None of these operations is equivalent to:

```text
end one joined Logical Player's Session lifetime
```

The distinction becomes mandatory once the Session Player lifetime is explicit.

IF-ADR-019 establishes the accepted model:

```text
Joined Logical Player
  Session-scoped

Activity Player Representation
  contextual / Activity-scoped

Manager-Provisioned Host
  Session-owned after successful Join

Scene-Provided Host + Actor
  externally scene-owned contextual occurrences
```

Player Leave must terminate the Session lifetime without collapsing these ownership
boundaries.

Without one explicit contract, consumers could incorrectly simulate Leave through:

```text
destroying a Player GameObject
releasing a Scene-Provided admission
clearing Slot state directly
closing Joining
unloading the Activity
disabling PlayerInput
requesting another Join over the same Slot
```

Those behaviors are architecturally different and must remain different.

## Decision

### 1. Player Leave is an explicit Session command

Session Player Leave means:

```text
terminate exactly one currently joined Logical Player
from the current Session
```

It is a Session-level mutation.

It is not:

```text
Activity representation release
scene unload
Actor despawn
PlayerInput disable
Joining policy change
device disconnect
Session termination
```

A successful Leave ends the current Session Player occurrence and makes its Slot vacant.

### 2. Leave targets an existing Player; it never allocates a target

Join may resolve a vacant Slot according to the accepted Session admission policy.

Leave has the opposite requirement:

```text
the target already exists
```

The command must identify the intended joined Player explicitly.

The canonical authored identity is:

```text
PlayerSlotId
```

No Leave implementation may silently choose:

```text
first joined Slot
last joined Player
PlayerInput index
first visible Player
first Actor found in scene
```

If the target Slot is invalid, unsupported or vacant, Leave fails explicitly.

### 3. Leave mutation must be occurrence-aware

A Slot may be reused after a successful Leave.

Therefore this sequence is possible:

```text
Slot P1
  occurrence A joins
  occurrence A leaves
  occurrence B joins
```

A delayed/stale Leave request created for occurrence A must never remove occurrence B.

The mutation boundary must therefore correlate the command to the current Session Player
occurrence/revision in addition to the stable Slot identity.

Conceptually:

```text
consumer intent
  exact Player Slot
        ↓
resolve current joined occurrence
        ↓
Leave request
  PlayerSlotId
  expected Session Player occurrence/revision
        ↓
Session authority validates current correlation
        ↓
release
```

The exact public token/type name is not frozen by this ADR.

The required invariant is:

```text
stable Slot identity
  is not sufficient to authorize destructive mutation
  after that Slot can be reused
```

### 4. Session authority owns Leave

Consumers do not directly mutate Slot state or destroy framework-owned Player resources.

The command must flow through the scoped Session Player authority.

Conceptually:

```text
consumer/UI/control plane
        ↓
scoped Player command access
        ↓
Session Player Leave authority
        ↓
validated current occurrence
        ↓
resource release
        ↓
Slot/Logical Player terminal commit
```

This does not introduce:

```text
global PlayerManager
singleton Player service
service locator
scene-wide implicit lookup
```

The exact runtime service/module class is an implementation decision.

### 5. Joining policy controls entry, not exit

`Open Joining` and `Close Joining` govern whether new Player admission may occur.

They do not govern whether an existing Player may leave.

Therefore:

```text
Joining = Closed
Player P1 = Joined

Request Leave P1
  allowed
```

After successful Leave:

```text
Slot P1 = Vacant
Joining = Closed

Request Join
  remains rejected/unavailable according to current Joining policy
```

Leave does not automatically reopen Joining.

Closing Joining does not lock existing Players into the Session.

### 6. Activity requirement does not revoke Session Leave authority

An Activity may currently require a Player or Slot for readiness.

That does not make the Activity the owner of Session membership.

If an authorized Leave removes a Player required by the current Activity, the Activity
must reconcile against the new Session truth.

For example:

```text
Activity requires Slot P1 Joined
P1 leaves successfully
        ↓
Activity contribution reconciles
        ↓
Preparing / WaitingForJoin
```

or another state explicitly allowed by the Activity participation/readiness contract.

The Framework must not preserve readiness by:

```text
pretending P1 is still joined
marking the contribution Ready
changing Required to Optional
automatically joining a replacement Player
silently selecting another Slot
```

### 7. Leave is control-plane behavior

Leave is a Session command, not ordinary gameplay input.

Its availability must not depend on the outgoing Actor's gameplay gate or on the
Activity Player representation continuing to accept gameplay input.

A game may expose Leave through:

```text
pause UI
lobby UI
session UI
developer/control-plane command
other explicit consumer binding
```

The command remains scoped and authorized.

This preserves the principle that control-plane operations needed to manage Session
membership are distinct from gameplay input.

## Release transaction

### 8. Leave is staged; Slot vacancy is the terminal commit

A Leave operation must not make the Slot appear vacant before required Player-owned
resources have been released.

The canonical shape is:

```text
validate target + occurrence
        ↓
stage Leaving
        ↓
quiesce and release contextual Activity representation
        ↓
release provisioning-specific Session resources
        ↓
clear Session-scoped Player associations
        ↓
commit Slot -> Vacant
        ↓
publish terminal Leave result / observations
```

The exact implementation may split these operations into smaller typed stages.

The normative boundary is:

```text
Slot Vacant
  means the previous Session Player occurrence no longer owns
  required authoritative Player resources
```

### 9. New contextual work is blocked once Leave is staged

After Leave for occurrence A has been accepted/staged:

```text
occurrence A must not begin new Activity Actor preparation
occurrence A must not acquire new Activity-local camera authority
occurrence A must not create new readiness evidence
occurrence A must not be rebound as a fresh contextual representation
```

Already-running release/reconcile work may finish as part of the Leave transaction.

This prevents Leave from racing with Activity reprojection.

The exact concurrency primitive is an implementation decision.

### 10. Current Activity representation releases first

If the Player currently has an Activity representation, Leave must first retire that
contextual authority.

This includes, where applicable:

```text
stop gameplay participation
release Activity-local input/gameplay admission
release Activity-local camera requests
release readiness contribution
release contextual Actor bindings
release/materialization ownership for the physical Actor occurrence
release Activity-local references and observation evidence
```

This is contextual release.

It does not by itself mean the Session Player has Left.

Only the later Session terminal commit does that.

### 11. A Player without a current Activity representation may still Leave

IF-ADR-019 allows:

```text
Session Player
  Joined = true

Current Activity
  Representation = Absent
```

Leave remains valid in that state.

The operation skips contextual representation release and proceeds to the
Session/provisioning resource boundary.

This is important for:

```text
menus
spectator-like Activities
Activities that exclude one joined Slot
covered transitions between representations
```

### 12. Manager-Provisioned resource release

For a Manager-Provisioned Player, successful Leave must release the Session-owned
technical Host/input endpoint after contextual Actor release.

Conceptually:

```text
Activity Actor occurrence
        ↓ release

Manager-Provisioned Local Player Host
  PlayerInput
  Actor Mount
        ↓ release through provisioning authority

Session Player occurrence
        ↓ terminal commit

Slot
  Vacant
```

The provisioning backend needs a semantic release operation for an already admitted
Player.

The current `RejectPlayer` behavior is not that contract.

`RejectPlayer` exists for failed/rejected provisioning and must not be silently promoted
into Session Player Leave merely because both paths may eventually destroy a
`PlayerInput` GameObject.

The Leave implementation must explicitly differentiate:

```text
rejected admission cleanup
successful joined Player release
```

### 13. Scene-Provided physical ownership is preserved

For a Scene-Provided Player:

```text
Host
PlayerInput
Actor
```

are externally scene-owned physical objects.

Successful Session Leave must release:

```text
framework contextual admission/binding
Activity representation authority
Host admission evidence
Session Player association
Slot occupancy
```

It must not take ownership of destroying the external scene objects.

Conceptually:

```text
Scene-owned Host + Actor
  remain consumer-owned objects

Framework
  unbinds / releases their Player participation authority

Slot
  becomes Vacant only after required framework release succeeds
```

After successful Leave, the still-existing scene object is not authoritative proof that
a Player remains joined.

### 14. Scene-Provided `RequestRelease` remains contextual release

The existing Scene-Provided authoring surface may release its Activity/scene admission.

That operation must remain semantically distinct from Session Player Leave.

```text
Scene-Provided RequestRelease
  release current contextual representation
  Logical Player may remain Joined

Session Player Leave
  release contextual representation if present
  terminate Logical Player Session occurrence
  vacate Slot
```

Product naming, Inspector text and diagnostics must make that distinction understandable.

The Framework must not silently reinterpret an Activity release as Leave.

## Session-scoped Player state

### 15. Leave clears state owned by the Session Player occurrence

When the terminal Leave commit succeeds, state owned by that Session Player occurrence
must no longer remain current.

At minimum this includes:

```text
joined Logical Player state
Slot occupancy
current Session Player occurrence/revision
Session-scoped Actor selection intent for that occurrence
provisioning association owned by the occurrence
Session-visible Player observation state
```

A future/new Player joining the same Slot creates a new occurrence.

The new occurrence does not silently inherit the old occurrence's Actor selection or
other mutable Session Player state.

If a game wants identity/preferences/progression to survive Leave and Rejoin, that must
come from a separate accepted persistence/profile identity contract.

### 16. Leave does not delete external game persistence

Leave does not mean:

```text
delete save data
delete preferences
delete account/profile
delete progression
clear cross-Session identity
```

It terminates the current Session participation occurrence only.

## Failure semantics

### 17. Validation failure is non-mutating

Before staging Leave, the authority must validate at least:

```text
target Slot identity is valid
target Slot belongs to the current Session configuration
target Slot is currently Joined
expected occurrence/revision matches current occupant
required scoped runtime authority is available
```

If validation fails:

```text
no contextual release begins
no Host is destroyed
no Slot mutation occurs
no Actor selection state is cleared
```

The result is explicit and diagnostic.

### 18. Required release failure does not commit Slot vacancy

If a required release fails before the terminal Slot commit:

```text
Leave != successful
Slot != Vacant
```

The operation must preserve explicit evidence that the occurrence is in a failed Leave
path.

Conceptually:

```text
Joined
  ↓
Leaving
  ↓
LeaveFailed
```

or an equivalent typed runtime representation.

The exact enum is not frozen.

The important invariant is that consumers must not observe a clean successful Leave
while authoritative resources from that occurrence remain unreleased.

### 19. Partial release is not silently rolled back

This ADR does not introduce a generic rollback or compensation manager.

If some release steps are irreversible and a later required step fails, the Framework
must:

```text
fail explicitly
retain correlation to the same Session Player occurrence
report what released and what remains authoritative
allow a safe idempotent retry/reconcile where the implementation supports it
avoid silently recreating already released Actor/Host state
avoid reporting Slot Vacant
```

Example:

```text
Activity Actor released
Manager-Provisioned Host release fails

Result
  LeaveFailed

Slot
  still correlated to the same Session Player occurrence

Activity representation
  Absent

Diagnostic
  explicit Host release failure
```

That state is degraded but truthful.

A later implementation cut may define focused recovery for concrete failure paths.

It must not introduce the deferred generic exceptional post-commit compensation model.

### 20. Retry must remain correlated to the same occurrence

A retry/reconcile after `LeaveFailed` must target the exact same Session Player
occurrence.

It must not:

```text
resolve whatever Player now occupies the Slot
allocate a new target
clear a newly joined occurrence
```

Once a Leave has committed and the Slot is later reused, any stale operation from the
old occurrence is rejected.

### 21. Terminal commit is authoritative

After all required pre-commit release succeeds:

```text
Session Player occurrence ends
Slot transitions to Vacant
```

That commit is the authoritative membership change.

Consumers observing `JoinedSlots` or equivalent Session Player snapshots must no longer
see the departed occurrence after terminal commit.

A post-commit diagnostic/UI publication problem must not resurrect the departed Player
or reoccupy the Slot.

This ADR does not define exceptional post-commit compensation.

## Rejoin semantics

### 22. Rejoin creates a new Session Player occurrence

After successful Leave:

```text
Slot P1
  Vacant
```

If Joining policy later allows admission, Request Join may use that Slot according to the
accepted allocation contract.

That creates:

```text
new Session Player occurrence
new occurrence/revision correlation
new provisioning/adoption path
new Actor resolution/selection lifecycle
new contextual Activity representation when applicable
```

It is not a continuation of the departed occurrence.

### 23. Stale Leave cannot affect a rejoined Player

Required negative invariant:

```text
P1 occurrence A joins
Leave A succeeds
P1 occurrence B joins
stale Leave A executes/retries
        ↓
Rejected
P1 occurrence B remains Joined
```

This is a mandatory QA case for the eventual technical implementation.

## Product surface

### 24. Extend the existing scoped command model; do not create a second Player control plane

The intended product direction is to add Session Player Leave to the existing scoped
Player command family.

Conceptually:

```text
Player Provisioning / Session command access
  Open Joining
  Close Joining
  Request Join
  Request Actor Selection
  Request Leave
```

The exact API placement must be reconciled with current Player provisioning and Actor
selection surfaces during implementation.

The ADR does not require one oversized interface if smaller typed contracts produce a
cleaner boundary.

It does require one coherent consumer model.

### 25. Designer-facing Leave requires an explicit Player target

A product surface such as a command trigger may expose:

```text
Command
  Request Leave

Target
  exact Player Slot
  or another explicitly bound current Player identity
```

It must not expose an ambiguous no-target Leave command if multiple Players can be
joined.

For a single-player game, authoring may make the sole supported Slot convenient to
select, but the technical command remains explicitly targeted.

No Apply/Rebuild is required merely for a runtime command trigger.

### 26. Advanced/Debug should expose Leave evidence

A useful diagnostic surface should make the transition visible.

Example:

```text
PLAYER SESSION

Slot                  Player1
State                 Leaving
Occurrence            12
Provisioning           Manager-Provisioned
Activity Representation Releasing
Host                  LocalPlayerHost(Clone)
Last Leave Request    PauseMenu
Last Leave Diagnostic Releasing Session-owned Host
```

Failure example:

```text
Slot                  Player1
State                 LeaveFailed
Occurrence            12
Activity Representation Absent
Host                  LocalPlayerHost(Clone)
Failure               Host release failed
Slot Vacant           No
```

The exact Inspector/report implementation is not defined here.

## Readiness and transition integration

### 27. Leave invalidates old Player readiness evidence

Any readiness contribution tied to the leaving occurrence or its current Activity
representation must not remain valid after contextual release.

The Activity must reconcile from current Session truth.

This prevents:

```text
departed Player
  leaving stale Ready evidence
        ↓
Activity reveals as though Player were still present
```

Occurrence/revision correlation remains mandatory.

### 28. Covered transitions do not silently defer Session truth

If Leave occurs while the application is covered between Activity representations, the
Session command remains authoritative.

The incoming Activity must observe the resulting Session membership when preparing its
Player projections.

It must not materialize an incoming representation for an occurrence whose Leave has
already been staged/committed.

Conversely, if Leave fails, the incoming Activity must not assume the Slot is Vacant.

## Session termination

### 29. Session termination remains a separate aggregate lifecycle operation

Session termination ends all remaining Player lifetimes.

It is not implemented by issuing arbitrary consumer Leave commands one by one.

However, both paths must preserve the same ownership principles:

```text
contextual representations release
Session-owned provisioning resources release
external Scene-Provided objects remain externally owned
Player Session state clears
no stale authority survives
```

A Session termination path may need stronger aggregate cleanup semantics than one
ordinary Player Leave operation.

Those semantics remain owned by Session lifecycle architecture.

## Rejected behavior

- Treating Scene-Provided `RequestRelease` as Session Player Leave.
- Treating `RejectPlayer` as the canonical Leave contract.
- Destroying a Player GameObject as proof that the Slot is vacant.
- Direct consumer Slot mutation.
- Leave selecting the first joined Player implicitly.
- Leave being blocked because Joining is Closed.
- Leave automatically opening Joining.
- Leave automatically selecting or joining a replacement Player.
- Slot becoming Vacant before required release succeeds.
- Silent success after partial release failure.
- Generic rollback/compensation manager.
- Recreating released resources silently to simulate rollback.
- Stale Leave affecting a new occurrence in a reused Slot.
- Destroying externally scene-owned Scene-Provided Host/Actor objects.
- Activity readiness remaining Ready through stale evidence after the required Player left.
- Global Player manager/service locator.
- Hidden fallback between Manager-Provisioned and Scene-Provided ownership models.

## Deferred / separate contracts

The following are not defined by this ADR:

```text
device disconnect/reconnect
network disconnect/reconnect
administrative kick / remote authority
cross-network replication of Leave
Leave All as a public consumer command
Player replacement / hot swap
per-Slot Host Provisioning
targeted Join / generalized Slot assignment
full Explicit Actor Selection mutation contract
Initial Placement / Spawn
checkpoint / respawn
Player save-game persistence
account/profile identity
cross-Session Player identity
controller reassignment
grace-period Slot reservation
generic rollback / exceptional post-commit compensation
```

## Consequences

### Positive

The Framework gains a precise inverse to Session Join:

```text
Join
  establishes one Session Player occurrence

Leave
  terminates that exact occurrence
```

Activity representation release remains contextual instead of accidentally becoming
membership mutation.

Manager-Provisioned and Scene-Provided ownership stay correct.

Joining policy becomes simpler to explain:

```text
Open/Closed Joining controls entry
Leave controls exit
```

Slot reuse becomes safe because destructive commands are occurrence-aware.

Failure states remain truthful: a Slot is not advertised as vacant while required
resources are still authoritative.

### Cost

The current public consumer command surface needs a new Leave capability.

Manager-Provisioned runtime needs a semantic release operation for an already admitted
Host; rejected-admission cleanup is insufficient.

Session Player runtime needs explicit Leaving/failure correlation or equivalent evidence.

Current Activity projection/readiness must reconcile correctly when Session membership
changes underneath an active Activity.

Scene-Provided product UX must distinguish contextual `RequestRelease` from Session
`Request Leave`.

## Required reconciliation after acceptance

This draft intentionally does not edit existing ADRs yet.

When IF-ADR-020 is accepted, architecture documentation should be reconciled as follows:

```text
IF-ADR-003
  replace Session Player Leave as an unresolved future contract
  with a reference to IF-ADR-020

IF-ADR-012
  clarify that successful Session Leave changes the Session projection
  and Activity readiness must reconcile without fake readiness

IF-ADR-015
  add Session Player Leave to the accepted consumer command surface
  preserve scoped access and no direct Slot mutation
  define exact command/request/result API during the implementation cut

IF-ADR-016
  clarify that Joining policy controls admission only
  a successful Leave returns the Slot to Vacant
  future RequestJoin may reuse it only when Joining policy permits

IF-ADR-019
  replace the deferred R3 boundary with IF-ADR-020
  use IF-ADR-020 as the terminal operation for one Session Player lifetime
```

No reconciliation should be committed until IF-ADR-020 is reviewed and accepted.

## Validation requirements

Acceptance of the architecture requires later package/QA cuts to prove at least:

### Core Leave

```text
joined Player can Leave
exact Slot/occurrence is targeted
successful Leave ends the occurrence
successful Leave makes Slot Vacant
Joined Player observations update after terminal commit
```

### Joining policy

```text
Leave succeeds while Joining is Closed
successful Leave does not reopen Joining
RequestJoin remains unavailable while Joining is Closed
after Joining reopens, the vacant Slot can be reused
```

### Occurrence safety

```text
invalid Slot is rejected
unsupported Slot is rejected
vacant Slot is rejected explicitly
stale occurrence is rejected
Leave A -> Join B -> stale retry A cannot remove B
```

### Manager-Provisioned

```text
current Activity Actor occurrence releases
Activity-local camera/readiness/bindings release
Session-owned Host/input endpoint releases
Slot does not become Vacant before required release succeeds
no orphan authoritative PlayerInput/Host remains after success
```

### Scene-Provided

```text
contextual admission releases
Session Player occurrence ends
Slot becomes Vacant
external Host still exists
external Actor still exists unless the consumer scene independently destroys/unloads it
framework no longer treats those objects as joined Player authority
```

### Player absent from Activity

```text
joined Session Player with no current Activity representation can Leave
no fake representation is created merely to release it
```

### Failure paths

```text
contextual release failure -> no successful Leave / no Vacant Slot
Manager-Provisioned Host release failure -> no successful Leave / no Vacant Slot
failure diagnostics identify the stage
retry remains correlated to the same occurrence
no silent rollback/recreation
```

### Readiness

```text
Activity requiring the departed Player does not retain stale Ready evidence
no Required -> Optional weakening
no replacement auto-Join
Activity returns to the state required by its declared participation/readiness policy
```

### Real consumer

FIRSTGAME should eventually prove:

```text
Player joins
Player participates in real gameplay
Player requests Leave through a normal consumer surface
Activity representation releases correctly
Slot visibly becomes Vacant
Joining Closed still permits Leave
optional rejoin creates a new occurrence
developer can diagnose the transition without inspecting internal runtime classes
```

## Acceptance of this architecture cut

```text
Player Leave is an explicit Session mutation
Leave terminates one exact joined Session Player occurrence
Leave target is explicit
destructive mutation is occurrence-aware
Joining Open/Closed controls entry, not exit
Activity requirement does not block Session Leave authority
Activity representation release is distinct from Leave
Scene-Provided RequestRelease remains contextual only
Manager-Provisioned Host release is a real Leave resource step
RejectPlayer is not reused as the semantic Leave contract
Scene-Provided external objects are not destroyed by Leave
Slot Vacant is the terminal commit after required release
partial release failure is explicit and not reported as success
stale Leave cannot remove a rejoined Player
successful Leave clears Session-scoped state for the departed occurrence
rejoin creates a new Session Player occurrence
no global Player manager or silent fallback is introduced
```

## Suggested commits

Architecture:

```text
docs(architecture): define session player leave authority
```

Future runtime/QA cuts should use their own scoped commit messages after the accepted ADR
defines the implementation plan.
