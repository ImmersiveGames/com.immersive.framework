# IF-ADR-024 — Prepared Actor Replacement Technical Certification — 2026-09-02

Status: **Reconciled / implemented / Player QA certified**

## Scope

This record closes the Manager-Provisioned V1 implementation and technical QA
boundary accepted by IF-ADR-024. It records the implementation divergences found
while replacing the former post-preparation Actor-selection blocker with the
positive public prepared-Actor replacement proof.

This record does not broaden IF-ADR-024 to Scene-Provided physical replacement and
does not relabel older Player certification records as if they had executed this
later contract.

## Public contract proved

The certified operation is:

```text
IPlayerSessionScopedAccess.RequestReplacePreparedActor(...)
```

The positive proof starts from one fresh joined P1 occurrence with:

```text
QA_DefaultActor selected
Actor A prepared and physically materialized
GameplayReady
exactly one PlayerGameplayInputReader
reader A current and bound
valid current gameplay admission/input evidence
```

The successful replacement preserves Player-owned authority:

```text
same PlayerSlotId
same LocalPlayerHost
same PlayerInput
same Session
same Activity occurrence
```

and replaces/reprojects Actor-owned and gameplay-scoped authority:

```text
Actor selection / preparation
PlayerActorRuntimeHost
Presentation
PlayerGameplayInputReader
gameplay occupancy
input binding
camera binding
gameplay admission
readiness evidence
```

The authoritative positive terminal is:

```text
SucceededReplacedAndGameplayReady
ReplacementCommitted = true
GameplayReprojected = true
CleanupPending = false
```

## Reconciliation 1 — previous occupancy survived replacement

The first positive execution committed Actor B but gameplay reprojection failed in:

```text
EnsureCurrentGameplay
  -> ConfirmOccupancy
  -> RejectedSlotAlreadyOccupied
```

The Slot still carried Actor A's exact gameplay occupancy/preparation evidence.
`ConfirmOccupancy` was correctly rejecting a different preparation; the defect was
transaction ordering, not occupancy conflict detection.

The replacement orchestration now releases A's occupancy through the existing
occupancy authority before B assumes the Slot. The release is correlated to the
exact previous `PlayerActorPreparationToken`; absent, stale or foreign occupancy
is not silently overwritten.

Current positive ordering:

```text
release contextual gameplay A
  -> release occupancy A by exact preparation ownership
  -> materialize/select/activate B
  -> commit B as current prepared Actor
  -> release physical A
  -> ConfirmOccupancy B
  -> bind reader/input/camera B
  -> admit B
  -> GameplayReady
```

## Reconciliation 2 — pre-commit restoration

Releasing A's gameplay before B commits creates a transaction boundary. If the
physical replacement fails before B becomes the current prepared Actor, A remains
the authoritative preparation and its previous GameplayReady projection must be
restored.

The ADR-024 owner now restores the previous gameplay authority through the
canonical `TryEnsureCurrentGameplay` path after pre-commit failure when A is still
provably the current prepared Actor by exact preparation evidence.

The invariant is:

```text
before
  A Prepared + GameplayReady

replacement fails before B commit

after
  A Prepared + GameplayReady
```

Restoration rebuilds canonical occupancy, input, camera, admission and reader
binding evidence. If physical state or restoration cannot be proven safe, the
operation reports `FailedRollback` rather than forcing A into authority.

After B has committed, A is not reconstructed. A post-commit gameplay reprojection
failure remains an explicit B-side degraded terminal such as
`SucceededCommittedGameplayReprojectionFailed`, with the same Player and Activity
occurrences retained.

## Reconciliation 3 — QA revision semantics

After the Framework returned the successful ADR-024 terminal, the QA convergence
wait still timed out because it required the general Slot revision to remain equal
to its pre-replacement value.

That condition was invalid. Successful Actor selection changes increment both:

```text
SelectionRevision
Slot.Revision
```

`Slot.Revision` is mutable revision evidence; it is not immutable Player occurrence
identity. The QA now requires the revision to advance while stable continuity is
proved independently through Slot identity, Host, PlayerInput, Session and Activity
occurrence evidence.

No observer publication defect was found: scoped access and `PlayerSessionObserver`
read the live projection, and the canonical physical topology/reader proof remains
part of the replacement convergence check.

## Final Player QA evidence

The final one-button run completed:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
cases='16/16'
completed='access,join,observation,actor-default,actor-lifecycle,gameplay-ready-reader,reader-cardinality,actor-replace,second-player,joining-control,commands,leave,rejoin,negatives,spatial,relocation'
```

The same run preserved the reader-cardinality boundaries exercised by the suite:

```text
zero       -> readerCount = 0
one        -> readerCount = 1, current reader bound
ambiguous  -> readerCount = 2, no current reader bound, gameplay admission absent
```

Because the suite continued after `actor-replace` through second Player, Joining,
commands, Leave/Rejoin, negatives, spatial entry and relocation, the positive
replacement cut did not leave the immediately dependent Player lifecycle boundary
in a failed state.

## Implementation ownership

The reconciled runtime cut is owned by the existing Player lifecycle/gameplay
surfaces, principally:

```text
Runtime/PlayerParticipation/Runtime/ActivityPlayerActorLifecycleParticipant.PreparedActorReplacement.cs
Runtime/PlayerParticipation/Runtime/PlayerGameplayRuntimeHostModule.cs
```

The positive QA proof is in:

```text
Assets/ImmersiveFrameworkQA/Player/Scripts/Runtime/PlayerQaSuite.cs
```

No Leave + Join substitution, reflection, service locator, parallel gameplay
pipeline or global occupancy overwrite belongs to this contract.

## Current disposition

```text
IF-ADR-024 architecture                 ACCEPTED
Manager-Provisioned public operation    IMPLEMENTED
Prepared Actor A -> B replacement       PASS
Gameplay reprojection A -> B            PASS
Pre-commit restoration                  RECONCILED
Occupancy ownership transition          RECONCILED
Player QA revision semantics            RECONCILED
Full Player QA                          CERTIFIED 16/16
```

Scene-Provided prepared Actor replacement remains outside V1 and requires its own
explicit physical-ownership contract before the Framework may replace authored or
externally owned physical composition.
