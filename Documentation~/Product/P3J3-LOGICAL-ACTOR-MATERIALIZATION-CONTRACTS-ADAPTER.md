# P3J.3 — Logical Actor Materialization Contracts and Adapter

Status: **implementation delta ready for Unity compile and QA**  
Type: **technical package cut**

## Objective

Introduce the typed contracts and Unity adapter required to materialize one
`ActorProfile.LogicalActorHostPrefab` below an already joined
`LocalPlayerHostAuthoring.ActorMount` without replacing the stable
`PlayerInput` host.

## Product/runtime boundary

```text
PlayerInputManager
  provisions one stable Local Player Host

LocalPlayerHostAuthoring
  owns PlayerInput, joined Player Slot evidence and ActorMount

ActorProfile
  provides immutable Logical Actor Host prefab intent

AttachedPlayerActorMaterializationAdapter
  stages one contextual Logical Actor below ActorMount

RuntimeContentRuntime
  validates scope, registers the materialized handle and owns logical release evidence
```

The adapter does not choose an Actor Profile and does not mutate Session Actor
selection. It consumes an explicitly authorized Profile and Joined Slot snapshot.

## Created contracts

```text
PlayerActorMaterializationOperationId
  framework-generated identity for one materialization attempt

PlayerActorMaterializationRequest
  immutable owner, Slot, Profile, host and generated identity evidence

PlayerActorMaterializationStatus
  explicit rejection/failure/success vocabulary

PlayerActorMaterializationState
  physical staged/active/release state vocabulary

PlayerActorMaterializationSnapshot
  immutable non-physical identity and state evidence

PlayerActorMaterializationResult
  typed RuntimeContent and Unity physical evidence

PlayerActorMaterializationHandle
  internal physical handle retained for P3J.4 orchestration
```

## Materialization transaction

```text
1. Validate Runtime Scope Context.
2. Validate Joined Player Slot snapshot.
3. Validate stable Local Player Host and exact Slot binding.
4. Validate Actor Profile classification and Logical Actor prefab shape.
5. Generate operation id, runtime ActorId and RuntimeContentId.
6. Request materialization through RuntimeContentRuntime transition guards.
7. Instantiate below an inactive staging parent.
8. Keep the Logical Actor instance inactive and attach it below ActorMount.
9. Require exactly one PlayerActorDeclaration and no PlayerInput.
10. Configure generated ActorId and bind the host PlayerInput explicitly.
11. Produce and register a materialized RuntimeContentHandle.
12. Return a typed StagedInactive handle and immutable snapshot.
```

## Required prefab shape

```text
Logical Actor Host Prefab
└── exactly one PlayerActorDeclaration : ActorDeclaration
```

Rejected shapes:

```text
no PlayerActorDeclaration
more than one PlayerActorDeclaration
additional ActorDeclaration authorities
any PlayerInput in the Logical Actor hierarchy
non-Player or non-Protagonist ActorProfile classification
```

`PlayerInput` remains owned by the stable `LocalPlayerHostAuthoring` root. The
Logical Actor receives only explicit runtime binding evidence.

## Generated identities

The adapter generates identities from explicit runtime evidence:

```text
Session context identity
RuntimeContent owner scope and owner identity
PlayerSlotId
monotonic materialization sequence
```

`ActorProfileId` identifies reusable product intent. The generated `ActorId`
identifies one concrete contextual Actor instance. They are never the same
functional identity.

## Rollback

The adapter contains an internal rollback primitive for failed staging and later
P3J.4 orchestration:

```text
deactivate instance
clear injected PlayerInput evidence
destroy the attached Logical Actor instance
release and unregister the RuntimeContent handle
preserve explicit failure diagnostics
```

Rollback never destroys or replaces the stable Local Player Host or its
`PlayerInput`.

## Files created

```text
Runtime/PlayerParticipation/Contracts/
├── PlayerActorMaterializationOperationId.cs
├── PlayerActorMaterializationRequest.cs
├── PlayerActorMaterializationResult.cs
├── PlayerActorMaterializationSnapshot.cs
├── PlayerActorMaterializationState.cs
└── PlayerActorMaterializationStatus.cs

Runtime/PlayerParticipation/Runtime/
├── AttachedPlayerActorMaterializationAdapter.cs
└── PlayerActorMaterializationHandle.cs

Documentation~/Product/
└── P3J3-LOGICAL-ACTOR-MATERIALIZATION-CONTRACTS-ADAPTER.md
```

## Out of scope

```text
Session prepared-Actor state per Slot
TryPrepareSelectedActor public/runtime-host operation
TryReleasePreparedActor
TryReplacePreparedActor
selection mutation guards while prepared
Actor Presentation or Skin
PlayerSlotOccupancy
post-materialization gameplay input activation
camera request publication
Activity admission timing
local Player leave/disconnect/reconnect
```

These belong to P3J.4 and later cuts.

## Technical acceptance

```text
package compiles in Unity 6.5
no Runtime -> Editor dependency
no reflection in runtime
no hierarchy lookup by name, tag or scene search
PlayerInputManager remains the only local Player root provisioner
Logical Actor is staged inactive below the explicit ActorMount
exactly one PlayerActorDeclaration is required
runtime ActorId is framework-generated
RuntimeContent request/result/handle identities agree
failed staging performs explicit rollback
successful rollback leaves no RuntimeContent handle
```

## Product acceptance for this technical cut

```text
ActorProfile remains the reusable authoring intent
Local Player Host remains stable and Actor-less after join
Logical Actor composition has one explicit technical adapter
advanced diagnostics expose Slot, Profile, Actor and RuntimeContent identities
no gameplay behavior is activated accidentally by the adapter
```

## Expected QA

```text
Immersive Framework
  > QA
    > Player
      > P3J.3 Run Logical Actor Materialization Adapter Smoke
```

Expected log:

```text
[P3J3_LOGICAL_ACTOR_MATERIALIZATION_ADAPTER_SMOKE]
status='Passed'
```

## Suggested commit

```text
P3J.3 — add Logical Player Actor materialization contracts and adapter
```
