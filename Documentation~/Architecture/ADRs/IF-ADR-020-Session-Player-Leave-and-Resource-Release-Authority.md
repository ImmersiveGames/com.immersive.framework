# IF-ADR-020 — Session Player Leave and Resource Release Authority

Status: **Accepted / Reconciled / Implemented**  
Technical QA: **Focused Manager-Provisioned public Leave certified — ADR020-H 26/26**  
Date: 2026-08-11  
Last updated: 2026-08-13  
Type: architecture / runtime authority / player product direction  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019  
Source finding: pre-FIRSTGAME architecture review — R3 Player Leave  
Reconciliation: [ADR-020 reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md)

> This ADR is the accepted authority for explicit Session Player Leave and its
> resource-release boundary. It does not promote Experimental APIs to Stable and does not
> claim FIRSTGAME real-consumer proof.
>
> The accepted architecture and package implementation are closed. Focused public
> Manager-Provisioned Leave QA passed 26/26. A dedicated Scene-Provided **Session Leave**
> certification is not separately evidenced by the closure record and therefore is not
> silently claimed as certified here.

## Context

The Player architecture already supports explicit Session admission and contextual
Activity release. IF-ADR-019 establishes the lifetime split:

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

IF-ADR-020 closes the missing inverse of Session Join:

```text
end exactly one joined Logical Player's current Session occurrence
```

Lower-level operations remain narrower and must not be confused with Leave. In
particular, Scene-Provided contextual release, admitted-Host bookkeeping and rejected
provisioning cleanup do not independently mean Session membership ended.

Consumers must not simulate Leave through:

```text
destroying a Player GameObject
releasing only a Scene-Provided contextual admission
clearing Slot state directly
closing Joining
unloading an Activity
disabling PlayerInput
requesting another Join over the same Slot
```

## Decision

### 1. Player Leave is an explicit Session command

Session Player Leave means:

```text
terminate exactly one currently joined Logical Player
from the current Session
```

It is a Session-level mutation. It is not Activity representation release, scene unload,
Actor despawn, `PlayerInput` disable, Joining policy change, device disconnect or Session
termination.

Successful Leave ends the current Session Player occurrence and makes its Slot vacant.

### 2. Leave targets an existing Player; it never allocates a target

The command identifies the intended joined Player explicitly by canonical Slot identity.

```text
PlayerSlotId
```

No Leave implementation may silently choose first joined Slot, last joined Player,
`PlayerInput.playerIndex`, first visible Player or first Actor found in scene.

Invalid, unsupported or vacant target rejects explicitly.

### 3. Leave mutation is occurrence-aware

A Slot can be reused after Leave:

```text
P1 occurrence A joins
A leaves
P1 occurrence B joins
```

A stale Leave created for A must never remove B. Destructive mutation therefore requires
both stable Slot identity and correlation to the current Session Player
occurrence/revision.

Conceptually:

```text
consumer intent
  exact Slot
        ↓
current joined occurrence
        ↓
Leave request
  Slot
  expected occurrence/revision
  source / reason metadata
        ↓
Session validates correlation
```

The concrete public DTO/type names are implementation surfaces; this ADR freezes the
semantic fields and occurrence-safe behavior, not an invented alternate API.

### 4. Session authority owns Leave

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
terminal Session commit
```

No global `PlayerManager`, singleton service, service locator or opportunistic scene-wide
lookup is introduced.

### 5. Joining policy controls entry, not exit

```text
Joining = Closed
P1 = Joined
Request Leave P1
  -> allowed
```

After success:

```text
P1 Slot = Vacant / Available
Joining = Closed
Request Join remains unavailable/rejected by Joining policy
```

Leave does not reopen Joining and Closing Joining does not lock existing Players into the
Session.

### 6. Activity requirement does not revoke Session Leave authority

An Activity may require a Player for readiness, but it does not own Session membership.
If the required Player Leaves, Activity readiness reconciles from current Session truth.

For the focused certified composition:

```text
Participation selection  ExplicitSlots
Requirement              GameplayReady
Zero-participant policy  Rejected
```

a successful Leave produces:

```text
explicit authored Slot projection remains
current Player occurrence is absent
Player contribution -> WaitingForJoin / Preparing
Activity Ready -> false
```

The Framework must not preserve Ready by pretending the departed Player remains joined,
weakening Required to Optional, auto-joining a replacement, selecting another Slot or
removing the authored explicit Slot projection.

### 7. Leave is control-plane behavior

Leave does not depend on the outgoing Actor continuing to accept gameplay input. Games
may expose it through pause UI, lobby/session UI, developer control-plane command or
another explicit scoped binding.

## Release transaction

### 8. Leave is staged; Slot vacancy is terminal commit

Canonical transaction:

```text
validate exact target + occurrence
        ↓
stage Leaving
        ↓
quiesce/release current Activity representation when present
        ↓
release provisioning-specific Session resources
        ↓
clear occurrence-owned Session associations
        ↓
commit Slot -> Vacant / Available
        ↓
publish terminal Leave result / observation
```

`Vacant`/`Available` means the departed occurrence no longer owns required authoritative
Player resources.

### 9. New contextual work is blocked once Leave is staged

After Leave for occurrence A is accepted/staged, A must not acquire new Activity Actor,
Camera, readiness or contextual binding authority. Existing release/reconcile work may
finish as part of the transaction.

### 10. Current Activity representation releases first

If present, current contextual authority is retired before terminal Session commit,
including as applicable:

```text
gameplay/input admission
Activity-local Camera requests
readiness contribution
contextual Actor bindings
physical Actor materialization ownership
Activity-local references and current evidence
```

Contextual release alone is still not Leave; terminal Session commit is the membership
change.

### 11. A Player without a current Activity representation may Leave

Valid precondition from IF-ADR-019:

```text
Session Player = Joined
Current Activity Representation = Absent
```

Leave skips contextual representation teardown and continues at the Session/provisioning
boundary. It must not fabricate a fake Activity representation merely to release it.

Released/baseline observations are valid after this path. Examples include:

```text
Admission  NotAdmitted
Camera     NotEvaluated
Occupancy  Vacant
```

Presence of those summary objects is not evidence of live gameplay authority.

### 12. Manager-Provisioned resource release

For Manager-Provisioned Player:

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
  Vacant / Available
```

Successful joined-Player release is a semantic provisioning operation distinct from
rejected-admission cleanup. `RejectPlayer` or any equivalent failed-admission cleanup is
not silently redefined as Session Leave merely because both paths can destroy a physical
`PlayerInput` object.

#### Logical release versus Unity destruction settle

The transaction may commit logical/provisioning release before Unity's deferred
`Object.Destroy` becomes observable through overloaded Unity-null semantics. This does not
weaken the Host ownership invariant.

Canonical QA therefore distinguishes:

```text
logical terminal Leave result
  -> provisioningReleased / terminalCommitted evidence

physical Unity destruction observation
  -> verified after the existing canonical settle boundary
```

The regression must not declare the Host alive merely because it checks before Unity's
physical destruction settle, and it must not remove the strong post-settle Host-release
assertion.

### 13. Scene-Provided physical ownership is preserved

For Scene-Provided Player, Host/`PlayerInput`/Actor are externally scene-owned physical
objects.

Successful Leave releases Framework authority:

```text
contextual admission/binding
Activity representation authority
Host admission evidence
Session Player association
Slot occupancy
```

It must not take ownership of destroying the external scene objects. A still-existing
scene object after Leave is not authoritative proof that the Player remains joined.

### 14. Scene-Provided contextual `RequestRelease` remains contextual release

```text
Scene contextual release
  -> release current contextual representation
  -> Logical Player may remain Joined

Session Player Leave
  -> contextual release when present
  -> terminate exact Session Player occurrence
  -> vacate Slot
```

Product naming and diagnostics must preserve that distinction.

## Session-scoped Player state

### 15. Leave clears state owned by the Session Player occurrence

At terminal success, at minimum these are no longer current for the departed occurrence:

```text
joined Logical Player state
Slot occupancy
current Session Player occurrence/revision
Session-scoped Actor selection intent/revision for that occurrence
provisioning association owned by the occurrence
Session-visible current Player authority
```

A later Player in the same Slot creates a new occurrence and does not silently inherit
mutable state from the departed occurrence.

### 16. Leave does not delete external game persistence

Leave does not mean delete save data, preferences, account/profile, progression or
cross-Session identity. It terminates only the current Session participation occurrence.

## Failure semantics

### 17. Validation failure is non-mutating

Before staging Leave, validate at least:

```text
target Slot identity valid
target Slot belongs to current Session configuration
target Slot currently Joined
expected occurrence/revision matches current occupant
required scoped runtime authority available
```

On failure:

```text
no contextual release begins
no Host destruction/release begins
no Slot mutation occurs
no Actor selection state is cleared
```

The result is explicit and diagnostic.

### 18. Required release failure does not commit Slot vacancy

```text
required release failed
  -> Leave != successful
  -> Slot != Vacant
```

Runtime preserves truthful correlation/evidence for the failed Leave path. Exact enum
names are implementation details unless public contracts freeze them.

### 19. Partial release is not silently rolled back

No generic compensation manager is introduced. If one irreversible step succeeds and a
later required step fails, runtime must fail explicitly, retain occurrence correlation,
report released versus remaining authority, support safe idempotent retry/reconcile where
implemented, avoid silently recreating released Actor/Host state and avoid reporting the
Slot vacant.

### 20. Retry remains correlated to the same occurrence

Retry/reconcile after failed Leave targets the same occurrence. It must never resolve
"whoever occupies the Slot now" or clear a later occurrence after Slot reuse.

### 21. Terminal commit is authoritative

After all required pre-commit release succeeds:

```text
Session Player occurrence ends
Slot -> Vacant / Available
```

`JoinedSlots` or equivalent current observations no longer include the departed
occurrence. Later diagnostic/UI publication failure cannot resurrect it.

## Rejoin semantics

### 22. Rejoin creates a new Session Player occurrence

If Joining later permits admission, the now-vacant Slot may be reused through accepted
Join policy. That creates a new occurrence/revision, new provisioning/adoption path, new
Actor selection lifecycle and new contextual Activity representation when applicable.

### 23. Stale Leave cannot affect a rejoined Player

```text
P1 occurrence A joins
Leave A succeeds
P1 occurrence B joins
stale Leave A executes/retries
        ↓
Rejected
P1 occurrence B remains Joined
```

This invariant is part of the focused certified Manager-Provisioned regression.

## Product surface

### 24. Extend the existing scoped command model; do not create a second control plane

Accepted bounded Session/Player intent includes:

```text
Open Joining
Close Joining
Request Join
Request Actor Selection where accepted by its own boundary
Request Leave
```

The package implementation exposes Leave through the existing scoped consumer model. No
parallel global Player controller is introduced.

### 25. Designer-facing Leave requires an explicit target

A designer surface may expose:

```text
Command
  Request Leave

Target
  exact Player Slot / explicitly bound current Player identity
```

No ambiguous no-target Leave is allowed merely because one game is currently single-
player.

### 26. Advanced/Debug should expose Leave evidence

Useful evidence includes:

```text
Slot
current occurrence/revision
provisioning mode
Leaving / terminal result
current Activity representation state
Host/resource release state
source/reason
stage diagnostic
terminal Slot state
```

Failure evidence must make unreleased authority visible instead of reporting clean
success.

### Current-authority observation rule

Diagnostic summaries may outlive the live authority they describe. Current-authority
observers must therefore evaluate current operational state and occurrence correlation,
not merely non-null summary presence.

A released/baseline summary is evidence of history/state, not a second mutable authority.

## Readiness and transition integration

### 27. Leave invalidates old Player readiness evidence

Any readiness contribution tied to the leaving occurrence or its Activity representation
stops being current after contextual release. Activity reconciles from current Session
truth.

For an explicit authored Slot, Leave does not delete authored projection intent. Under
the certified explicit-slot policy, the Slot remains projected and waits for a new Join.

### 28. Covered transitions do not defer Session truth silently

If Leave occurs between Activity representations, incoming Activity preparation observes
the resulting Session membership. It must not materialize an incoming representation for
an occurrence whose Leave has staged/committed. Conversely, failed Leave does not permit
incoming Activity to assume the Slot is vacant.

## Session termination

### 29. Session termination is a separate aggregate lifecycle operation

Session termination ends all remaining Player lifetimes and may use stronger aggregate
cleanup semantics. It is not specified as a loop that issues arbitrary public Leave
commands one by one.

Both paths preserve the same ownership principles:

```text
contextual representation authority releases
Session-owned Manager-Provisioned resources release
external Scene-Provided physical ownership remains external
Session Player state clears
no stale authority survives
```

## Rejected behavior

- Treating Scene-Provided contextual `RequestRelease` as Session Player Leave.
- Treating rejected-admission cleanup as the semantic Leave contract.
- Destroying a Player GameObject as proof that Slot is vacant.
- Direct consumer Slot mutation.
- Implicit first-joined/first-visible Leave targeting.
- Blocking Leave because Joining is Closed.
- Leave automatically opening Joining.
- Leave automatically selecting/joining a replacement.
- Slot vacancy before required release succeeds.
- Silent success after partial release failure.
- Generic rollback/compensation manager.
- Silent recreation of released resources to simulate rollback.
- Stale Leave affecting a new occurrence in reused Slot.
- Destroying externally scene-owned Scene-Provided Host/Actor objects.
- Stale Activity Ready evidence after a required Player left.
- Removing authored Explicit Slot projection merely to make readiness pass.
- Treating baseline/released summary presence as live authority.
- Global Player manager/service locator.
- Hidden fallback between provisioning modes.

## Deferred / separate contracts

Not defined by this ADR:

```text
device disconnect/reconnect
network disconnect/reconnect
administrative kick / remote authority
cross-network replication of Leave
Leave All as public consumer command
Player replacement / hot swap
per-Slot Host Provisioning
targeted Join / generalized Slot assignment beyond separately accepted decisions
full Explicit Actor Selection mutation contract beyond separately accepted decisions
IF-ADR-021 Initial Placement / Spawn
checkpoint / respawn
Player save-game persistence
account/profile identity
cross-Session Player identity
controller reassignment
grace-period Slot reservation
generic exceptional post-commit compensation
```

## Consequences

### Positive

```text
Join
  establishes one Session Player occurrence

Leave
  terminates that exact occurrence
```

Activity contextual release remains distinct from membership mutation. Provisioning
ownership remains correct. Open/Closed Joining controls entry; Leave controls individual
exit. Slot reuse is occurrence-safe. Failure remains truthful.

### Cost

The public scoped consumer surface requires Leave capability. Manager-Provisioned runtime
requires semantic release for already-admitted Host/input resources. Session runtime must
carry occurrence-aware Leave evidence. Active Activity projection/readiness must reconcile
when Session membership changes. Scene-Provided product UX must distinguish contextual
release from Session Leave.

## Reconciliation applied

Acceptance is reconciled into:

```text
IF-ADR-003
  individual Session Player lifetime now terminates through IF-ADR-020
  Activity exit remains contextual only

IF-ADR-012
  successful Leave invalidates old readiness
  explicit authored Slot projection remains when policy requires it

IF-ADR-015
  Request Leave is part of the accepted scoped consumer command model
  observation distinguishes current authority from retained summaries

IF-ADR-016
  Joining policy controls admission only
  successful Leave returns Slot to Vacant / Available without reapplying initial profile

IF-ADR-019
  formerly deferred individual Leave boundary is now IF-ADR-020
  Session termination remains separate aggregate lifecycle
```

Detailed record:
[IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md)

## Technical validation evidence

### Focused Manager-Provisioned public Leave

Terminal proof:

```text
[QA_ADR020_H_LEAVE]
status='Passed'
verdict='ADR020_H_PASS'
cases='26'
proof='PublicLeave,ManagerProvisioned,JoiningClosed,TerminalAvailable,ResourceRelease,ReadinessInvalidation,Rejoin,StaleOccurrence,NoActivityLeave'
```

The 26-case regression proves, among other cases:

```text
public scoped Leave succeeds
Leave succeeds while Joining is Closed
Joining remains Closed after Leave
required Activity representation authority releases
Manager-Provisioned Host authority releases
physical Host destruction is verified after canonical Unity settle
Slot becomes Available only after terminal release
stale Activity Ready is cleared
Explicit Slot projection returns to WaitingForJoin
Join remains blocked while Joining is Closed
Joining may later reopen
same Slot may rejoin as a new occurrence
stale Leave for old occurrence is rejected
new occurrence survives stale Leave
Leave succeeds when no Activity representation exists
public scan sees no live authority for departed occurrence
```

### Stage evidence

The successful Leave result exposes typed stage evidence equivalent to:

```text
status                SucceededLeft
activityReleased      True
provisioningReleased  True
terminalCommitted     True
partialRelease        False
```

The exact public type names remain package authority; documentation does not invent
parallel DTOs.

### Readiness evidence

After the required Player leaves the certified explicit-slot Activity:

```text
Slot projection remains authored/current
projected participant exists as waiting Slot intent
current Player occurrence absent
Activity Ready = false
state = WaitingForJoin / Preparing
```

This closes the stale `Ready + zero current Player` divergence without erasing authored
Activity composition.

### No-Activity Leave evidence

A joined Session Player with no current Activity representation can Leave without fake
representation creation. Released/baseline summaries are permitted and do not count as
current gameplay authority.

### Certification scope not overclaimed

The normative Scene-Provided Leave ownership contract is accepted and implemented as part
of IF-ADR-020 architecture/package closure. This record does **not** contain a separate
dedicated Scene-Provided **Session Leave** terminal regression comparable to ADR020-H.
Therefore the tracker records focused Manager-Provisioned certification rather than
claiming a cross-mode 100% certification that is not evidenced here.

## Real consumer — Stage B pending

FIRSTGAME should prove normal product use:

```text
Player participates in real gameplay
Player requests Leave through normal consumer surface
Activity representation releases correctly
Slot visibly becomes Available
Joining Closed still permits Leave
optional rejoin creates new occurrence
developer can diagnose transition without internal runtime inspection
```

This Stage B product proof is separate from architecture/implementation closure.

## Accepted architecture boundary

```text
Player Leave is explicit Session mutation
Leave terminates one exact joined occurrence
Leave target is explicit and occurrence-aware
Joining Open/Closed controls entry, not exit
Activity requirement does not block Session Leave authority
Activity representation release != Leave
Scene-Provided contextual release != Session Leave
Manager-Provisioned Host release is a real Leave resource step
external Scene-Provided objects remain externally owned
Slot vacancy is terminal commit after required release
partial release failure is explicit
stale Leave cannot remove rejoined occurrence
successful Leave clears occurrence-owned Session state
rejoin creates a new Session Player occurrence
Activity readiness reconciles without fake Ready
explicit authored Slot projection is not erased to hide absence
current authority is derived from operational state/correlation, not summary existence
no global Player manager or silent fallback is introduced
```

## Current disposition

```text
Architecture decision                         Accepted
Package implementation                        Implemented
Documentation reconciliation                  Completed
Focused Manager-Provisioned public QA         Certified — ADR020-H 26/26
Dedicated Scene-Provided Session Leave QA      Not separately evidenced in this record
FIRSTGAME real-consumer proof                  Pending Stage B
API maturity promotion                         Not implied
```
