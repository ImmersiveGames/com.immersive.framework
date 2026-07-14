# P3J.6 — Activity-Scoped Player Actor Lifecycle

Status: implementation delta ready for Unity compile and QA.  
Type: runtime lifecycle integration and technical product contract.

## Objective

Connect Session Player Actor preparation to the established Activity Content Execution lifecycle.

```text
ActivityFlow creates Activity RuntimeScopeContext
-> required Player Actor participant resolves projected Slots
-> selected/default Actors are prepared under the Activity owner
-> participant result contributes to Activity readiness
-> Activity exit releases only that owner's preparation tokens
-> stable Local Player Host, PlayerInput, Slot and selection remain Session-owned
```

## Product surface affected

No new gameplay component or diagnostic-first authoring flow is introduced. The designer continues to edit:

```text
ActivityAsset
  Projection Profile
  Requirements Profile

PlayerSlotProfile
  Default Actor Profile

ActorProfile
  Logical Actor Host Prefab
```

`LogicalActorsPrepared` now has real Activity runtime behavior. `GameplayReady` remains explicitly unsupported until its separate authorities exist.

## Runtime composition

`ActivityPlayerActorLifecycleParticipant` is a required `IActivityContentExecutionParticipant` supplied explicitly by the `FrameworkRuntimeHost`-scoped Player Actor preparation module.

The preparation module creates the participant during Session composition. The provisioning binding, which occurs after `GameFlowRuntime` exists, registers the participant explicitly on the runtime host. Registration is repeated idempotently after successful host admission so Activity operations never depend on a hidden global lookup or join ordering accident.

## Enter transaction

```text
1. Validate exact Activity RuntimeScopeContext.
2. Resolve Projection Profile in deterministic order.
3. Validate Requirements Profile.
4. Require Joined state when the progressive level needs it.
5. Select the Slot default Actor when SelectedActors or higher is required and no selection exists.
6. Prepare the selected Actor when LogicalActorsPrepared is required.
7. Commit one Activity owner record containing only preparation tokens for that owner.
8. Return required participant evidence to ActivityFlow.
```

A failure rolls back Actors created by the current enter transaction and default selections applied by that transaction. Pre-existing same-owner Actors are not destroyed by rollback of another Slot.

## Exit transaction

```text
1. Validate Activity and owner identity against the retained record.
2. Release each retained preparation token before Activity scope tail cleanup.
3. Preserve Local Player Host, PlayerInput, Joined Slot and Actor selection.
4. Clear the owner record only when every release succeeds.
5. Retain explicit failure evidence when release fails.
```

Foreign or stale Activity owners are rejected; no silent release is attempted.

## Readiness

`ActivityReadinessState` now includes blocking evidence from `ActivityContentExecutionLifecycleResult`.

```text
required Actor prepare succeeds -> Ready
required Actor prepare fails -> NotReady
participant source rejection -> NotReady
GameplayReady requested in P3J.6 -> NotReady with explicit unsupported diagnostic
```

## Files created

```text
Runtime/PlayerParticipation/Contracts/
├── ActivityPlayerActorLifecycleStatus.cs
├── ActivityPlayerActorSlotLifecycleSnapshot.cs
└── ActivityPlayerActorLifecycleSnapshot.cs

Runtime/PlayerParticipation/Runtime/
└── ActivityPlayerActorLifecycleParticipant.cs

Documentation~/Product/
└── P3J6-ACTIVITY-SCOPED-PLAYER-ACTOR-LIFECYCLE.md
```

## Files changed

```text
Runtime/ActivityFlow/
├── ActivityContentExecutionRequest.cs
├── ActivityReadinessState.cs
└── ActivityFlowStartResult.cs

Runtime/PlayerParticipation/Authoring/
└── LocalPlayerProvisioningAuthoring.cs

Runtime/PlayerParticipation/Runtime/
└── PlayerActorPreparationRuntimeHostModule.cs
```

## Out of scope

```text
gameplay input enablement
PlayerSlotOccupancy
Actor Presentation / Skin
camera target binding
local Player leave/disconnect/reconnect
preparing a Player that joins after the current Activity has already entered
FIRSTGAME integration
```

## Technical acceptance

```text
package compiles in Unity 6.5
ActivityFlow supplies the exact Activity RuntimeScopeContext
required failures produce blocking execution evidence
readiness becomes NotReady on required preparation failure
Activity exit releases before scope tail cleanup
restart creates a new ActorId, RuntimeContent identity and preparation token
stale preparation tokens are rejected without disturbing the current Actor
Activity owner remains logical Activity-scope evidence, not a restart-generation token
no Runtime -> Editor dependency
no reflection or global lookup in runtime
no silent fallback
```

## Product acceptance

```text
designer selects projection and requirement through existing Activity Profiles
LogicalActorsPrepared now materializes real Actors automatically on Activity entry
clear/restart does not destroy the stable Local Player Host or PlayerInput
Advanced diagnostics expose owner, Slot, selection, token and release evidence
GameplayReady does not report false success
```

## Suggested commits

```text
Framework:
P3J.6 — integrate Player Actors with Activity lifecycle

QAFramework:
P3J.6 — validate Activity-scoped Player Actor enter exit and restart
```
