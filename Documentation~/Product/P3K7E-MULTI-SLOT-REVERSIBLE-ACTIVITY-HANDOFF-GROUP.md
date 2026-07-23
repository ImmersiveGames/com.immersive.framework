# P3K.7E — Multi-Slot Reversible Activity Handoff Group

Status: **closed — Unity compile and QA PASS (45 cases)**  
Type: **runtime transaction + local multiplayer atomicity + admission integration foundation**

## Objective

Coordinate every projected local Player Slot as one Activity-scoped reversible
handoff group before any per-Slot promotion crosses its ownership commit boundary.

```text
ordered target candidate requests
-> begin P3K.7D for every Slot
-> all candidate P3K chains current but rollbackable
-> evaluate P3K.6 / P3K.7A for the complete Activity
-> prevalidate every per-Slot commit
-> commit every target ownership
```

## Scope correction

P3K.7D proved a complete reversible cutover for one Slot. An Activity can project
multiple Slots. Calling the original one-step `TryPromote` sequentially would be
unsafe:

```text
Slot 1 commits and releases previous Actor
Slot 2 fails
-> Slot 1 can no longer rollback
-> Activity has partial Player ownership
```

P3K.7E therefore adds a two-phase surface to P3K.7D and coordinates all Slots
before irreversibility. GameFlow/ActivityFlow integration is deferred to P3K.7F.

## P3K.7D extension

```text
TryBeginPromotion
  releases current chain
  swaps current P3J preparation
  builds candidate P3K.2-P3K.5 chain
  stops at CandidateChainReady

TryCommitPromotion
  completes candidate ownership
  releases previous physical Actor
```

`TryPromote` remains as a compatibility wrapper that performs Begin then Commit.

## Group authority

`ActivityPlayerHandoffGroupRuntimeContext` is plain C# and receives:

```text
IPlayerGameplayChainPromotionRuntime
IActivityPlayerHandoffEvidenceSource
```

It does not use scene lookup, global registries, reflection, ActivityFlow or
GameFlow.

## Begin transaction

```text
validate Activity owner and ordered unique Slots
begin every Slot in authored projection order
capture current Participation / P3J / P3K.5 snapshots
run ActivityPlayerAdmissionFlowGate
require projected Slot count and order to match the group
Proceed -> ReadyToCommit
non-Proceed/failure -> rollback all begun Slots in reverse order
```

## Commit transaction

Immediately before commit the group captures fresh evidence and evaluates P3K.6
again. It then prevalidates every per-Slot commit before committing the first Slot.

```text
fresh P3K.6/P3K.7A Proceed
-> validate every handoff token and candidate state
-> commit Slots in deterministic order
```

Previous Actor release failure occurs after target ownership completion. The
group retains `CommitCleanupFailed`, rejects rollback and retries only the pending
previous Actor cleanup.

## Rollback

Before any target ownership commit:

```text
last begun Slot -> first begun Slot
```

Successful rollback clears the active group. A failure retains exact per-Slot
progress for explicit retry.

## Product surface

No new designer-facing component or Profile is introduced. Existing authoring
remains authoritative:

```text
ActivityAsset
  Activity-owned Projection configuration
  Requirements Profile

PlayerSlotProfile / ActorProfile
stable Local Player Host / PlayerInput
```

The group contracts are Advanced runtime composition surfaces.

## Out of scope

```text
GameFlowRuntime mutation
ActivityFlowRuntime mutation
P3J.6 participant adoption
route startup integration
scene composition/release
TransitionSurface/loading presentation
automatic polling
FIRSTGAME integration
```

## QA

Run in Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3K.7E Run Multi-Slot Activity Handoff Group Smoke
```

Expected:

```text
[P3K7E_MULTI_SLOT_ACTIVITY_HANDOFF_GROUP_SMOKE]
status='Passed'
cases='45'
```

## Technical acceptance

```text
P3K.7D Begin/Commit split exists
TryPromote compatibility wrapper remains
ordered unique Slot requests
all begins remain rollbackable
P3K.6/P3K.7A evaluated after begin and before commit
projection count/order must match group
partial begin failure rolls back prior Slots
non-Proceed admission rolls back all Slots
all commits prevalidated before first commit
commit cleanup failure retains target-authoritative evidence
rollback rejected after target ownership commit
public group contracts retain no Unity object references
no Runtime -> Editor dependency
no Runtime reflection
no GameFlow/ActivityFlow coupling
```

## Next cut

```text
P3K.7F — Session Player Gameplay Runtime Composition
```

P3K.7F must integrate the validated group before transition presentation and
Activity lifecycle side effects, and must formally transfer ownership to the
P3J.6 Activity lifecycle participant.
