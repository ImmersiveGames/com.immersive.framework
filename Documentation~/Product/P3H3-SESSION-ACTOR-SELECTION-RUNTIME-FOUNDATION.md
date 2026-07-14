# P3H.3 — Session Actor Selection Runtime Foundation

Status: implementation cut; Unity compile and QA smoke pending.  
Type: runtime product API and synthetic technical QA.

## Objective

Extend the Session-scoped `PlayerParticipationRuntimeContext` with current `ActorProfile` selection state per configured `PlayerSlotId`, without coupling selection to join provisioning, logical Actor materialization, occupancy, input, camera or Activity readiness.

## Runtime authority

```text
PlayerParticipationRuntimeContext
  ordered Slot allocation state
  explicit Session Actor selection policy
  selected ActorProfile per Joined Slot
  selection revision/source/reason
```

Profiles remain immutable authoring inputs.

## Composition compatibility

The existing P3F/P3G initializer remains available:

```text
TryCreate(...)
  join-capable context
  no Actor selection policy
  selection requests reject RejectedPolicyMissing
```

P3H.3 adds:

```text
TryCreateWithActorSelectionPolicy(...)
  explicit non-null valid PlayerActorSelectionPolicyProfile
  selection-capable Session context
```

This avoids a silent `AllowDuplicates` fallback and preserves existing join/runtime-host integration until P3H.4 composes the policy officially.

## Product operations

```text
TrySelectActorProfile
TryReplaceActorSelection
TryClearActorSelection
TrySelectDefaultActor
TryGetActorSelection
```

Rules:

```text
only Joined Slots may change selection
select does not silently replace
replace requires an existing selection
clear preserves Joined allocation
same selection and repeated clear are idempotent
expected selection revision rejects stale UI/request state
unique policy compares ActorProfileId, not asset reference or name
default application is explicit and uses the canonical select transaction
```

## Slot snapshot evidence

`PlayerSlotRuntimeSnapshot` now exposes:

```text
SelectedActorProfile
SelectedActorProfileId
HasSelectedActor
SelectionRevision
SelectionSource
SelectionReason
```

Selection changes increment:

```text
Slot SelectionRevision
Slot general Revision
Session context Revision
```

Idempotent operations do not increment revisions.

`PlayerParticipationSnapshot` now exposes the policy plus selected/joined-unselected counts for later P3H.4 readiness evaluation.

## Failure policy

```text
invalid request/profile/policy
unconfigured or unjoined Slot
stale expected revision
duplicate ActorProfileId under UniqueAcrossJoinedSlots
missing explicit default
```

All failures preserve the previous selection atomically.

`RejectedLogicalActorAlreadyPrepared` is reserved in the typed status vocabulary for the later logical-materialization boundary; P3H.3 does not create or track logical Actor hosts.

## Files

Created:

```text
Runtime/PlayerParticipation/Contracts/PlayerActorSelectionRequest.cs
Runtime/PlayerParticipation/Contracts/PlayerActorSelectionResult.cs
Runtime/PlayerParticipation/Contracts/PlayerActorSelectionStatus.cs
```

Changed:

```text
Runtime/PlayerParticipation/Contracts/PlayerSlotRuntimeSnapshot.cs
Runtime/PlayerParticipation/Contracts/PlayerParticipationSnapshot.cs
Runtime/PlayerParticipation/Runtime/PlayerParticipationRuntimeContext.cs
```

## QA

Execute outside Play Mode:

```text
Immersive Framework/QA/Player/P3H.3 Run Actor Selection Runtime Smoke
```

Expected:

```text
[P3H3_ACTOR_SELECTION_RUNTIME_SMOKE] status='Passed' cases='20'
```

## Out of scope

```text
Runtime Host policy composition
public MonoBehaviour/UI request surface
SelectedActors Activity readiness
logical Actor host materialization
ActorId generation
Presentation/Skin
occupancy/input/camera
FIRSTGAME
```

## Suggested commit

```text
P3H.3 — add Session Actor selection runtime foundation
```
