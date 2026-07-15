# P3K.5 — Gameplay Admission and Camera Publication

Status: **implementation delta ready for Unity compile and QA**  
Type: **runtime integration, camera publication, readiness aggregation and rollback**

## Objective

```text
P3K.2 current effective occupancy
+ P3K.3 current gameplay input binding
+ P3K.4 Eligible or SkippedOptional camera decision
-> one typed gameplay admission
-> optional local Player CameraRequest publication
-> derived GameplayReady
```

## Runtime authority

`PlayerGameplayAdmissionRuntimeContext` is Session-scoped and receives the three
existing live authorities explicitly. It is not a singleton, Activity gate,
Actor materializer, camera winner or movement owner.

The current identity chain is retained in one `PlayerGameplayAdmissionToken`:

```text
SessionContextId
RuntimeContentOwner
PlayerSlotId
ActorProfileId
ActorId
RuntimeContentIdentity
PlayerActorPreparationToken
PlayerGameplayOccupancyToken
PlayerGameplayInputBindingToken
PlayerGameplayCameraEligibilityToken
AdmissionRevision
```

## Camera publication

For `Eligible` camera evidence the context:

```text
resolves the explicit CameraOutputSessionBinding
retrieves the P3K.4 internal rig/target evidence
creates a typed LocalPlayer CameraRequest
creates LocalPlayerCameraRequestPublisher
publishes through CameraOutputSession
retains publisher/request evidence for reverse release
```

`CameraOutputContext` remains the only winner-selection authority. Cinemachine
remains the presentation engine.

For `SkippedOptional` no camera request or output is required.

## GameplayReady

Admission and temporary input availability are distinct:

```text
Ready
  admission is current and gameplay input is Allowed

BlockedByInputGate
  admission and camera request remain current
  gameplay input is Bound but temporarily blocked by Gate
  GameplayReady is false
```

`TryRefreshReadiness` updates this derived state without replacing the admission
token or republishing the camera request.

## Reverse release

The context owns the complete reverse order:

```text
camera request release
-> camera eligibility release
-> gameplay input binding release
-> effective occupancy release
```

Every step is exact-token guarded. A partial failure becomes `ReleaseFailed`,
retains progress evidence and may be retried with the same admission token.

## Admission failure rollback

Once the exact current chain is accepted, any camera output/request/publication
failure rolls back the admitted prerequisites in the same reverse order.

```text
rollback complete
  no admission record remains

rollback incomplete
  ReleaseFailed remains with exact progress evidence
```

No failure is silently converted to readiness.

## Product boundary

This is a technical integration cut. It does not add another designer-facing
surface. P3K.4 authoring remains the product camera endpoint.

The Activity admission gate does not consume `GameplayReady` in this cut. That
connection belongs to the following Activity integration cut.

## Files

```text
Runtime/PlayerParticipation/Contracts/
  PlayerGameplayAdmissionState.cs
  PlayerGameplayAdmissionStatus.cs
  PlayerGameplayAdmissionToken.cs
  PlayerGameplayAdmissionSummary.cs
  PlayerGameplayAdmissionSnapshot.cs
  PlayerGameplayAdmissionResult.cs

Runtime/PlayerParticipation/Runtime/
  PlayerGameplayAdmissionRuntimeContext.cs
```

## Technical acceptance

```text
context initializes only from matching P3K.2/P3K.3/P3K.4 rosters
vacant, unbound or NotEvaluated evidence is rejected
live authorities reject stale supplied tokens
SkippedOptional admits without CameraOutputSession
Eligible camera requires explicit initialized output
camera request identity comes from P3K.4 eligibility evidence
CameraOutputSession publishes and applies the materialized rig
admission is idempotent
BlockedByInputGate retains camera/admission identity
refresh changes only derived readiness state
release is exact-token guarded and idempotent
release order is camera -> eligibility -> input -> occupancy
partial release is retryable
failed camera admission rolls back prerequisites
re-admission generates a new token
public snapshots retain no Unity object references
```

## Next

The next cut connects `PlayerParticipationRequirementLevel.GameplayReady` to the
Activity admission pipeline using the P3K.5 snapshot. It must consume this
truthful readiness evidence rather than rebuilding Player contracts.

## P3K.5 FIX1 — compact admission identity and stack-safe validation

The initial implementation recursively embedded every prerequisite token inside
`PlayerGameplayAdmissionToken` and copied three large summaries through multiple
value/out parameters during admission validation. Under the Unity Mono runtime,
that value-type expansion produced an immediate `StackOverflowException`.

The corrected shape is:

```text
PlayerGameplayAdmissionToken
  compact identity + prerequisite revisions + admission revision

PlayerGameplayAdmissionSummary / internal AdmissionRecord
  retain exact prerequisite tokens

CurrentChainEvidence (internal class)
  carries live summaries on the managed heap during validation/refresh
```

No authority or release ordering changed.
