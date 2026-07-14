# P3H.4 — GameApplication Policy Composition and Runtime Host Integration

Status: implementation cut; Unity compile and QA pending.  
Type: product authoring, runtime composition and real-host technical QA.

## Objective

Make the Player Actor selection policy an explicit required GameApplication decision and compose it into the Session-owned Player participation runtime used by the FrameworkRuntimeHost.

## Product surface

```text
GameApplicationAsset
  Local Player Participation
    Actor Selection Policy (required)
    Ordered Player Slot Profiles
```

`null` is invalid. The framework does not silently select `AllowDuplicates`.

## Runtime composition

```text
GameApplicationAsset.PlayerActorSelectionPolicyProfile
  -> PlayerParticipationRuntimeHostModule.Initialize
  -> PlayerParticipationRuntimeContext.TryCreateWithActorSelectionPolicy
  -> PlayerParticipationSnapshot.ActorSelectionPolicyProfile
```

The policy Profile remains immutable. Current Actor selections remain Session state.

## Explicit operations

The host-scoped module exposes typed operations over its one Session context:

```text
TrySelectActorProfile
TryReplaceActorSelection
TryClearActorSelection
TrySelectDefaultActor
TryGetActorSelection
```

The module is an adapter, not a second authority. No static registry or global lookup is introduced.

## Default selection

Slot defaults are not applied at boot or during join.

```text
join -> Slot Joined and Unselected
explicit TrySelectDefaultActor -> selected default through canonical transaction
```

This preserves the frozen separation between join and Actor selection.

## Failure policy

```text
missing/invalid GameApplication policy
  authoring validation error
  Session runtime initialization failure
  boot blocked explicitly

runtime unavailable
  typed RejectedRuntimeUnavailable selection result
```

## Files

Changed:

```text
Runtime/Authoring/GameApplicationAsset.cs
Runtime/PlayerParticipation/Contracts/PlayerActorSelectionResult.cs
Runtime/PlayerParticipation/Runtime/PlayerParticipationRuntimeHostModule.cs
Editor/Authoring/GameApplicationAssetEditor.cs
Editor/PlayerParticipation/PlayerParticipationAuthoringValidator.cs
```

## QA

First, outside Play Mode, run:

```text
Immersive Framework/QA/Player/P3H.4 Run Game Application Policy Authoring Smoke
```

Expected: `status=Passed`, `cases=6`.

Then apply the real-host fixture.

1. Outside Play Mode:

```text
Immersive Framework/QA/Player/P3H.4 Apply Runtime Host Actor Selection Fixture
```

2. Enter Play Mode and wait for successful framework boot.
3. Execute:

```text
Immersive Framework/QA/Player/P3H.4 Run Runtime Host Actor Selection Smoke
```

Expected:

```text
[P3H4_RUNTIME_HOST_ACTOR_SELECTION_SMOKE] status='Passed' cases='13'
```

The smoke is one-shot per Play Mode because local leave remains outside this cut.

## Out of scope

```text
SelectedActors Activity readiness
logical Actor host materialization
ActorId generation
Presentation/Skin
occupancy
input/camera orchestration
FIRSTGAME
```

## Suggested commit

```text
P3H.4 — compose Actor selection policy into the runtime host
```
