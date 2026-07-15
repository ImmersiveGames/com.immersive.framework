# P3K.7C — Concurrent Target Activity Player Actor Staging

Status: **closed — Unity compile and QA PASS (43 cases)**  
Type: **runtime authority + physical coexistence boundary + architecture correction**

## Objective

Permit one target Activity Logical Player Actor candidate to exist inactive while
the current Activity Actor remains active and fully owned by P3J/P3K.

```text
current Activity Actor remains active
+ current P3J preparation remains current
+ target Activity RuntimeScopeContext
-> target Actor candidate materialized inactive
-> exact candidate token and diagnostics
-> rollback candidate without touching current Actor
```

## Why this cut precedes Activity lifecycle integration

P3K.7B proved a full pre-activation transaction, but the official P3J/P3K runtime
currently has one current preparation and one gameplay chain per Player Slot.
When an Activity is already active, preparing the target Activity Actor through
the same current authority is rejected as an owner conflict.

Therefore direct P3K.7B integration would be correct only when no previous
Activity Actor exists. It would not support the normal Activity-to-Activity
switch that the framework must own.

P3K.7C establishes the missing physical coexistence boundary before GameFlow or
ActivityFlow are changed.

## Runtime authority

`PlayerActorCandidateStageRuntimeContext` is Session-scoped and plain C#.

It receives explicit dependencies:

```text
PlayerParticipationRuntimeContext
PlayerActorPreparationRuntimeHostModule
AttachedPlayerActorMaterializationAdapter
```

It owns only target candidates. It does not own or modify the current P3J record.

`PlayerActorCandidateRuntimeHostModule` is an explicit same-host composition
adapter. It is not auto-discovered and is not a global registry or service
locator.

## Candidate contract

One candidate token contains:

```text
SessionContextId
Target Activity RuntimeContentOwner
PlayerSlotId
ActorProfileId
candidate ActorId
candidate RuntimeContentIdentity
CandidateRevision
```

The immutable snapshot additionally retains the current preparation token,
current ActorId and current owner as comparison evidence. These fields prove
that the candidate was staged alongside, rather than instead of, the current
Actor.

## Staging rules

```text
target scope must be Activity-scoped
Slot must be configured and Joined
ActorProfile selection must already be explicit
stable Local Player Host must be registered with P3J
candidate owner must differ from current prepared owner
one candidate maximum per Slot
same target/profile/host request is idempotent
candidate remains StagedInactive
current Actor remains active
```

The candidate reuses the stable Local Player Host and PlayerInput but receives a
new ActorId and RuntimeContent identity owned by the target Activity.

## Rollback

Rollback requires the exact current candidate token.

```text
candidate GameObject deactivated/destroyed
candidate PlayerInput diagnostic evidence cleared
candidate RuntimeContent handle released and unregistered
current P3J preparation untouched
current Actor remains active
stable host, PlayerInput, Slot and selection remain Session-owned
```

Successful rollback is idempotent. A failed rollback retains the candidate
record and exact token for retry.

## Scope

```text
candidate token/state/result contracts
Session-scoped candidate authority
explicit runtime-host composition
inactive target Actor materialization
current/candidate coexistence evidence
exact-token rollback
rollback retry state
runtime-host diagnostics
real Play Mode QA
```

## Out of scope

```text
candidate promotion
current Actor deactivation or release
P3K.2-P3K.5 candidate gameplay chain
camera/input winner handoff
ActivityFlowRuntime mutation
GameFlowRuntime mutation
scene/content transition
FIRSTGAME integration
```

## Product surface

No new designer-facing component, Profile or menu is introduced. Existing
product authoring remains authoritative:

```text
ActivityAsset participation Profiles
PlayerSlotProfile default Actor
ActorProfile Logical Actor Host prefab
PlayerGameplayBindingAuthoring on the Actor prefab
```

## Follow-up

```text
P3K.7D — Player Gameplay Chain Promotion and Handoff
```

P3K.7D consumes the inactive candidate through an exact synchronous cutover. It
releases the current P3K chain, swaps current P3J preparation, builds the
candidate chain and restores the previous Actor/chain on any pre-commit failure.

ActivityFlow/GameFlow integration remains deferred until P3K.7D passes QA.

## QA

Run in a fresh Play Mode session after normal Framework boot:

```text
Immersive Framework
  > QA
    > Player
      > P3K.7C Run Concurrent Target Actor Staging Smoke
```

Expected:

```text
[P3K7C_CONCURRENT_TARGET_ACTOR_STAGING_SMOKE]
status='Passed'
cases='43'
```

The smoke uses the real provisioning, selection, P3J preparation,
RuntimeContent and Unity materialization path. It proves two Actor declarations
coexist under one stable Actor Mount while only the current Actor is active.
