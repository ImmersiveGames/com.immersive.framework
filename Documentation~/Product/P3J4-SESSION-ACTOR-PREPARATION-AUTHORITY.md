# P3J.4 — Session Actor Preparation Authority

Status: **implementation delta ready for Unity compile and QA**  
Type: **technical runtime and orchestration cut**

## Objective

Introduce the Session-scoped authority that coordinates selected `ActorProfile`
state with the P3J.3 attached Logical Player Actor materialization adapter.

```text
PlayerParticipationRuntimeContext
  owns Joined Slot and selected ActorProfile

PlayerActorPreparationRuntimeContext
  owns prepared/unprepared summary per Slot
  guards selection while prepared
  orchestrates prepare, release and replace

AttachedPlayerActorMaterializationAdapter
  owns typed Unity staging and physical release

RuntimeContentRuntime
  owns scoped handle registration and logical release evidence
```

## Product surface affected

P3J.4 is a runtime authority cut. It does not add a new designer component or
menu. The affected product surfaces remain:

```text
GameApplication Actor selection policy
PlayerSlotProfile / ActorProfile intent
LocalPlayerHostAuthoring stable technical host
Runtime Scope owner supplied by Route or Activity authority
```

P3J.5 will compose these operations into the runtime host and prove the real
PlayerInputManager flow. No diagnostic menu becomes the principal product UX.

## Expected use flow

```text
join local Player
-> select ActorProfile for Joined Slot
-> Route/Activity owner requests Prepare Selected Actor
-> Logical Actor becomes active below ActorMount
-> owner may Release or transactionally Replace
-> joined Local Player Host and PlayerInput remain stable
```

## Session evidence

Each configured Player Slot exposes one immutable preparation summary:

```text
PlayerActorPreparationSummary
  SessionContextId
  PlayerSlotId
  Unprepared / Prepared / ReleaseFailed
  selected ActorProfileId and selection revision
  materialization snapshot when present
  functional PlayerActorPreparationToken
  source / reason / message
```

Public snapshots do not retain `GameObject`, `PlayerInput`,
`PlayerActorDeclaration` or mutable physical handles.

## Prepare transaction

```text
1. Validate scope, configured Joined Slot and explicit selected ActorProfile.
2. Validate exact Local Player Host and Slot binding.
3. Return AlreadyPrepared when owner, Profile, host and identities already match.
4. Stage the Logical Actor through the P3J.3 adapter.
5. Activate the staged Actor.
6. Commit the per-Slot Session preparation summary.
```

Activation failure releases the staged Actor and RuntimeContent handle. A failed
rollback is explicit and retained diagnostically.

## Release transaction

```text
1. Validate optional expected preparation token.
2. Deactivate and physically release the current Logical Actor.
3. Release and unregister RuntimeContent evidence.
4. Return the Slot summary to Unprepared.
5. Preserve Joined host and ActorProfile selection.
```

Repeated release without an expected stale token returns
`SucceededAlreadyReleased`.

## Replacement transaction

```text
1. Validate current preparation token and owner scope.
2. Preserve current Actor, selection and preparation summary.
3. Stage the replacement Actor inactive.
4. Commit ActorProfile replacement with exact selection revision.
5. Activate the replacement Actor.
6. Commit the replacement preparation summary.
7. Release the previous Actor.
```

Failure before activation rolls back the staged replacement and preserves the
previous Actor and selection. If previous release fails after the replacement is
current, the new Actor remains authoritative and the old handle is retained in
immutable diagnostic evidence instead of disappearing silently.

## Selection guard

After this context is composed, official selection mutation routes through:

```text
TrySelectActorProfile
TryReplaceActorSelection
TryClearActorSelection
TrySelectDefaultActor
```

A Slot with `Prepared` or `ReleaseFailed` evidence rejects direct selection
mutation with `RejectedLogicalActorAlreadyPrepared`. Actor replacement must use
`TryReplacePreparedActor`.

The lower-level participation context remains the selection store. It is not a
Unity materializer and does not own physical Actor references.

## Files created

```text
Runtime/PlayerParticipation/Contracts/
├── PlayerActorPreparationResult.cs
├── PlayerActorPreparationSnapshot.cs
├── PlayerActorPreparationState.cs
├── PlayerActorPreparationStatus.cs
├── PlayerActorPreparationSummary.cs
└── PlayerActorPreparationToken.cs

Runtime/PlayerParticipation/Runtime/
└── PlayerActorPreparationRuntimeContext.cs

Documentation~/Product/
└── P3J4-SESSION-ACTOR-PREPARATION-AUTHORITY.md
```

## Files changed

```text
Runtime/PlayerParticipation/Runtime/
└── AttachedPlayerActorMaterializationAdapter.cs
```

The adapter now exposes `TryReleaseMaterialization` for normal release semantics
while retaining `TryRollbackMaterialization` as the P3J.3 compatibility path.

## Out of scope

```text
FrameworkRuntimeHost composition and provisioning-host automatic registration
real PlayerInputManager end-to-end preparation QA
Route/Activity lifecycle automatic prepare/release timing
PlayerSlotOccupancy
post-materialization gameplay input activation
camera request publication
Actor Presentation / Skin
local Player leave/disconnect/reconnect
FIRSTGAME integration
```

Runtime-host and real integration evidence remains P3J.5.

## Technical acceptance

```text
package compiles in Unity 6.5
Session context is plain C# and scoped, not static or a MonoBehaviour
prepare is idempotent
release is idempotent
selection cannot mutate directly while prepared
foreign and stale preparation tokens are rejected
replacement preserves the stable Local Player Host and PlayerInput
failed replacement preserves previous Actor and selection
RuntimeContent handles do not leak on successful rollback/release
physical references remain inside internal runtime records
no Runtime dependency on Editor
no reflection, singleton, service locator or global lookup in runtime
```


## Product acceptance

```text
runtime caller prepares only the ActorProfile already selected for the Slot
joined Local Player Host remains stable across prepare/release/replace
repeated prepare and release are safe and idempotent
invalid or stale requests return typed diagnostics
replacement does not require changing PlayerInputManager.playerPrefab
Advanced diagnostics can inspect immutable Slot/Profile/Actor/RuntimeContent identities
```

## Architectural gain

The Session now has one explicit authority that coordinates selection and
physical Logical Actor state without moving Unity references into the passive
participation snapshot or turning `RuntimeContentRuntime` into a Player manager.

## Usability gain

Future runtime-host and authoring surfaces can expose `Prepare`, `Release` and
`Replace` as coherent operations. Consumers no longer need to manually sequence
selection mutation, prefab instantiation, PlayerInput binding and cleanup.

## Expected QA

Outside Play Mode:

```text
Immersive Framework
  > QA
    > Player
      > P3J.4 Run Session Actor Preparation Smoke
```

Expected:

```text
[P3J4_SESSION_ACTOR_PREPARATION_SMOKE]
status='Passed'
```

## Suggested commits

```text
Framework:
P3J.4 — add Session Logical Player Actor preparation authority

QAFramework:
P3J.4 — validate Session Actor preparation transactions
```
