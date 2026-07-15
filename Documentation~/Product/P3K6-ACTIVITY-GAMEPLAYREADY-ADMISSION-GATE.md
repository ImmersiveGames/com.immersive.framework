# P3K.6 — Activity GameplayReady Admission Gate

Status: **implementation delta ready for Unity compile and QA**  
Type: **Activity-scoped runtime evaluation / technical integration**

## Objective

```text
ActivityAsset
+ mandatory ActivityParticipationProjectionProfile
+ mandatory PlayerParticipationRequirementsProfile
+ current Session participation snapshot
+ current Actor preparation snapshot when required
+ current P3K.5 gameplay admission snapshot when required
-> Satisfied | PendingResolution | Blocked | Failed
```

## Product surface

P3K.6 consumes the existing Activity authoring surface. It does not add another
Profile, Composer or parallel Activity component.

```text
Activity Inspector > Player Participation
  Projection Profile
  Requirements Profile
```

## Progressive evaluation

```text
None
  no Player readiness evidence required

JoinedSlots
  projected Slots must be Session Joined

SelectedActors
  includes JoinedSlots
  current ActorProfile selection required

LogicalActorsPrepared
  includes SelectedActors
  current Active P3J preparation required

GameplayReady
  includes LogicalActorsPrepared
  current P3K.5 admission must be Ready
```

## Projection

```text
NoSlots
  produces an empty set
  valid only with Requirements=None

AllJoinedSlots
  projects current Joined Slots in canonical Session configured order

ExplicitSlots
  projects the exact authored PlayerSlotProfile order
  a configured but unjoined Slot remains in the result
```

Zero-participant policy is evaluated after projection.

## Result semantics

```text
Satisfied
  Activity may proceed from the Player participation perspective

PendingResolution
  product-owned work may still resolve the state
  examples: join, Actor selection, preparation, Gate release, P3K.5 admission

Blocked
  current policy/state refuses activation
  examples: zero participants rejected, projected Slot unavailable/leaving

Failed
  invalid authoring or incoherent/stale runtime evidence
```

Every projected Slot receives immutable evidence containing:

```text
PlayerSlotId
configured and projected order
required progressive level
status and diagnostic code
selected ActorProfileId
prepared ActorId
joined/selected/prepared/ready facts
```

Public results retain no Unity object references.

## Authority boundary

`ActivityPlayerAdmissionEvaluator`:

```text
reads immutable snapshots
resolves projection
checks progressive requirements
reports evidence
```

It does not:

```text
mutate Session participation
join a Player
choose an Actor
prepare or release Actors
create occupancy/input/camera bindings
publish CameraRequest
change P3K.5 admission
activate or cancel an Activity transition
```

The transition pipeline integration is a later cut.

## Technical acceptance

```text
null Activity/Profile fails explicitly
NoSlots + non-None fails explicitly
AllJoined order follows Session configured order
ExplicitSlots order follows Profile authoring order
zero allowed/rejected is explicit
lower progressive levels do not require later snapshots
Session snapshot identities must match
snapshot roster mismatches fail
missing resolvable state returns PendingResolution
Gate-blocked P3K.5 admission returns PendingResolution
ReleaseFailed or stale evidence returns Failed
all projected current Ready admissions return Satisfied
result contains no Unity object references
```

## Next

The next integration cut may inject this evaluator into the Activity transition
pipeline and map the result to transition continuation, pending handling or
explicit cancellation. It must not duplicate this evaluation inside GameFlow.
