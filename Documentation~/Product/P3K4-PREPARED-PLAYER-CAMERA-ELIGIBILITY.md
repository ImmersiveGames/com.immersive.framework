# P3K.4 — Prepared Player Camera Eligibility

Status: **implementation delta ready for Unity compile and QA**  
Type: **runtime contracts, explicit Actor authoring and transactional eligibility cut**

## Scope correction

P3K.4 ends at **typed camera eligibility**.

It does not publish a `CameraRequest`. Request publication, rollback against the
camera output and `GameplayReady` aggregation remain one transaction in P3K.5.
This avoids a temporary published-camera state that is not yet owned by the full
admission/release coordinator.

## Objective

```text
P3J Active prepared Logical Player Actor
+ P3K.2 current effective occupancy
+ P3K.3 current gameplay input binding
+ explicit Actor-owned camera authoring
-> current prepared Player camera eligibility
```

## Product authoring

`PlayerGameplayCameraAuthoring` belongs to the contextual Logical Actor
hierarchy and carries only:

```text
Optional | Required
explicit CameraRigComposer
explicit Follow target
optional LookAt target
precedence intent
```

It carries no:

```text
PlayerInput
PlayerSlotId string
ActorId string
request id
eligibility scope id
tie-break string
winner policy
OnEnable publication
```

The referenced `CameraRigComposer` must use `ExplicitTransform`, must not retain
a `PlayerComposer`, and its explicit targets must exactly match the authoring
endpoint.

## Runtime authority

`PlayerGameplayCameraEligibilityRuntimeContext` is Session-scoped and validates
live evidence from both downstream authorities:

```text
PlayerGameplayOccupancyRuntimeContext
PlayerGameplayInputBindingRuntimeContext
```

A camera may become Eligible only while the exact current identity chain remains
coherent:

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
```

Gate blocking does not remove eligibility because the input binding identity
remains Bound while availability is temporarily blocked.

## Optional camera

A shared-camera Activity may explicitly record:

```text
SkippedOptional
```

Required policy cannot use the optional skip operation.

## Derived request identity

P3K.4 derives and records the future request identity from the current owner and
preparation evidence:

```text
request id
local-player eligibility lifetime scope
tie-break id
```

No arbitrary serialized identity participates. The internal physical evidence
boundary retains the exact rig and target references for P3K.5.

## State

```text
NotEvaluated
SkippedOptional
Eligible
```

Both `Eligible` and `SkippedOptional` receive a functional eligibility token.
Release requires the exact current token, is idempotent and returns the Slot to
`NotEvaluated`.

## Files

```text
Runtime/PlayerParticipation/Authoring/
  PlayerGameplayCameraAuthoring.cs

Runtime/PlayerParticipation/Contracts/
  PlayerGameplayCameraRequiredness.cs
  PlayerGameplayCameraEligibilityState.cs
  PlayerGameplayCameraEligibilityStatus.cs
  PlayerGameplayCameraEligibilityToken.cs
  PlayerGameplayCameraEligibilitySummary.cs
  PlayerGameplayCameraEligibilitySnapshot.cs
  PlayerGameplayCameraEligibilityResult.cs

Runtime/PlayerParticipation/Runtime/
  PlayerGameplayCameraEligibilityRuntimeContext.cs
```

## Technical acceptance

```text
context initializes only from matching occupancy and input rosters
vacant occupancy cannot become camera eligible
unbound or stale input evidence is rejected
stale occupancy evidence is rejected
required camera cannot use optional skip
optional skip is explicit, idempotent and token guarded
ActorId must match effective occupancy
authoring, rig and targets must belong to the prepared Actor hierarchy
rig must use ExplicitTransform and no PlayerComposer
rig and authoring targets must match exactly
request/lifetime/tie-break identities are owner/preparation-derived
eligibility is idempotent
release is exact-token guarded and idempotent
re-eligibility creates a new token
public contracts retain no Unity object references
no CameraRequest is published in P3K.4
no GameplayReady is aggregated in P3K.4
```

## Next

P3K.5 consumes the current occupancy, input and camera eligibility tokens,
publishes the optional/required camera request when applicable, aggregates
`GameplayReady`, and owns full reverse release and rollback.
