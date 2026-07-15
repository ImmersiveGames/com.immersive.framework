# P3K.7D — Player Gameplay Chain Promotion and Handoff

Status: **implementation delta ready for Unity compile and QA**  
Type: **runtime transaction + reversible authority cutover + technical integration**

## Objective

Promote one exact P3K.7C target Activity Actor candidate into the current P3J
preparation and P3K.2-P3K.5 gameplay chain without yielding a frame in an
intermediate two-Player state.

```text
current active Actor + current gameplay admission
+ exact inactive target candidate
-> release current gameplay chain
-> activate candidate and deactivate previous Actor
-> make candidate the current P3J preparation
-> create candidate P3K.2-P3K.5 chain
-> commit candidate ownership and release previous Actor
```

Any failure before candidate ownership completion restores the previous Actor
and rebuilds its gameplay chain.

## Why the handoff is synchronous

P3K.7C permits the physical candidate to coexist with the current Actor, but the
stable Local Player Host owns one `PlayerInput`. Two gameplay input bindings for
that same authority cannot coexist.

P3K.7D therefore performs the cutover as one synchronous operation:

```text
no await
no coroutine
no frame yield
no scene operation
no visual transition
```

The current binding is released before the candidate binding is applied. If the
candidate chain fails, the previous binding is rebuilt before control returns to
the caller.

## Authorities

`PlayerGameplayChainHandoffRuntimeContext` is Session-scoped and plain C#.

It coordinates existing authorities without replacing them:

```text
PlayerActorPreparationRuntimeHostModule       P3J current preparation
PlayerActorCandidateRuntimeHostModule         P3K.7C candidate ownership
PlayerGameplayOccupancyRuntimeContext         P3K.2
PlayerGameplayInputBindingRuntimeContext      P3K.3
PlayerGameplayCameraEligibilityRuntimeContext P3K.4
PlayerGameplayAdmissionRuntimeContext         P3K.5
```

Physical endpoints are supplied through the narrow advanced contract:

```text
IPlayerGameplayChainHandoffEndpointSource
```

The canonical `ExplicitPlayerGameplayChainHandoffEndpointSource` starts from the
exact current P3J preparation token and only inspects the typed stable host and
Actor subtree. It does not search scenes, names, tags or global registries.

## Handoff token

One exact token retains:

```text
SessionContextId
PlayerSlotId
candidate token
previous P3J preparation token
previous P3K.5 admission token
handoff revision
```

Foreign or stale candidate, admission and handoff evidence is rejected.

## Nominal sequence

```text
1. Reserve exact candidate promotion ownership.
2. Release current P3K.5 admission in canonical reverse order.
3. Activate candidate and deactivate previous Actor.
4. Swap the current P3J preparation record.
5. Confirm candidate P3K.2 occupancy.
6. Bind candidate P3K.3 input.
7. establish candidate P3K.4 camera decision.
8. Admit candidate through P3K.5.
9. Complete candidate ownership.
10. Release the previous physical Actor.
```

The committed result retains the exact candidate preparation and admission
tokens that became current.

## Rollback

A failure before candidate ownership completion executes:

```text
release any candidate P3K chain progress
-> deactivate candidate
-> reactivate previous Actor
-> restore previous P3J preparation record
-> return candidate to P3K.7C staged ownership
-> rebuild previous P3K.2-P3K.5 gameplay chain
```

The restored chain receives new functional tokens. The previous Actor identity
and preparation token remain unchanged.

Rollback failures retain one active handoff token. `TryRetryRollback` resumes
from the remaining release/restoration step instead of replaying completed work.

## Commit cleanup failure

Candidate ownership completion is the irreversible boundary for this cut.

If the candidate is current and its gameplay chain is ready but releasing the
previous physical Actor fails:

```text
state = CommitCleanupFailed
candidate remains current and functional
rollback is rejected
TryRetryCommitCleanup retries only previous Actor release
```

The previous handle is retained by the exact handoff lease rather than also
being inserted into the generic retained-release list. A successful retry
therefore leaves no duplicate failure diagnostic.

## Scope

```text
candidate promotion ownership
P3J current-record handoff
P3K.2-P3K.5 current-chain cutover
exact handoff token and immutable progress snapshot
synchronous rollback to previous Actor and chain
retryable rollback progress
retryable post-commit previous Actor cleanup
idempotent repeated committed request
real Play Mode QA
```

## Out of scope

```text
ActivityFlowRuntime mutation
GameFlowRuntime mutation
scene composition or release
TransitionSurface or loading presentation
automatic polling or background retry
Activity request result integration
FIRSTGAME integration
new designer-facing authoring
```

## Product surface

No new designer-first component or Profile is introduced. Existing authoring
remains authoritative:

```text
PlayerSlotProfile / ActorProfile
stable Local Player Host and PlayerInput
UnityPlayerInputGateAdapter
optional PlayerGameplayCameraAuthoring
CameraOutputSessionBinding when camera publication is required
```

The handoff endpoint source is an Advanced/runtime composition contract, not the
primary designer workflow.

## QA

Run in a fresh Play Mode session after normal Framework boot:

```text
Immersive Framework
  > QA
    > Player
      > P3K.7D Run Player Gameplay Chain Promotion Handoff Smoke
```

Expected:

```text
[P3K7D_PLAYER_GAMEPLAY_CHAIN_PROMOTION_HANDOFF_SMOKE]
status='Passed'
cases='52'
```

The smoke proves two independent transactions:

```text
forced candidate endpoint failure
-> exact rollback
-> previous Actor active
-> previous gameplay chain rebuilt with new P3K tokens

second candidate
-> committed promotion
-> candidate current and active
-> previous Actor physically released
-> candidate admission authoritative
```

## Next cut

```text
P3K.7E — Activity Lifecycle Admission Integration
```

P3K.7E may connect P3K.7B-P3K.7D to the real Activity request lifecycle. It must
stage and validate before transition side effects, invoke the synchronous handoff
at the commit boundary, retain the committed gameplay lease for Activity exit,
and keep failures explicit.
