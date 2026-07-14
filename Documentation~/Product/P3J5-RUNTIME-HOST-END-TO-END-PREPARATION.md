# P3J.5 — Runtime Host Integration and End-to-End Player Actor Preparation

Status: implementation delta ready for Unity compile and QA.  
Type: technical integration cut.

## Objective

Compose the P3J.4 Session Actor preparation authority into the official
`FrameworkRuntimeHost` lifetime and provide one coherent runtime path from real
local Player join to contextual Logical Actor preparation.

```text
FrameworkRuntimeHost
├── PlayerParticipationRuntimeHostModule
├── LocalPlayerProvisioningRuntimeHostModule
└── PlayerActorPreparationRuntimeHostModule
    ├── registered joined Local Player Hosts by PlayerSlotId
    ├── PlayerActorPreparationRuntimeContext
    └── AttachedPlayerActorMaterializationAdapter
```

## Official runtime flow

```text
open joining
-> real PlayerInputManager manual join
-> register the returned stable Local Player Host
-> select explicit/default ActorProfile
-> caller supplies explicit RuntimeScopeContext
-> prepare selected Logical Actor
-> release/replace through the same Session authority
```

The module does not infer Route or Activity ownership. Preparation requires an
explicit valid `RuntimeScopeContext` supplied by the lifecycle authority that
owns the materialized Actor.

## Host registration

A successful `LocalPlayerJoinResult` is accepted only when:

```text
Slot snapshot is valid and Joined
LocalPlayerHostAuthoring is Joined to the same PlayerSlotId
host owns the PlayerInput returned by provisioning
one PlayerSlotId is not registered to another host
```

Registration is idempotent for the same Slot and host. There is no scene search,
name lookup, static host registry or `PlayerInputManager.instance` access.

## Runtime-host operations

```text
TryOpenJoining
TryCloseJoining
TryJoinLocalPlayer
TryRegisterJoinedHost
TrySelectActorProfile
TrySelectDefaultActor
TryReplaceActorSelection
TryClearActorSelection
TryPrepareSelectedActor
TryReleasePreparedActor
TryReplacePreparedActor
TryReleaseAllPreparedActors
TryGetSnapshot
```

`TryJoinLocalPlayer` uses the local provisioning module already attached to the
same `FrameworkRuntimeHost`, then registers the returned host before preparation
can proceed. The public `LocalPlayerProvisioningAuthoring.RequestJoin` endpoint
uses the same registration bridge, so successful product joins cannot bypass the
preparation authority.

## Shutdown

The host-scoped module explicitly attempts release for every Prepared or
ReleaseFailed Slot during its own teardown. Failures are retained in the P3J.4
preparation diagnostics; they are not silently discarded.

## Diagnostics

`PlayerActorPreparationRuntimeHostSnapshot` exposes only immutable non-physical
evidence:

```text
Session context identity
registered host count
join/preparation request counts
last join status
preparation snapshot and counts
last diagnostic
```

Unity object references remain internal to runtime records and the explicit host
registry.

## Files created

```text
Runtime/PlayerParticipation/Contracts/
└── PlayerActorPreparationRuntimeHostSnapshot.cs

Runtime/PlayerParticipation/Runtime/
└── PlayerActorPreparationRuntimeHostModule.cs

Documentation~/Product/
└── P3J5-RUNTIME-HOST-END-TO-END-PREPARATION.md
```

## Files changed

```text
Runtime/PlayerParticipation/Authoring/
└── LocalPlayerProvisioningAuthoring.cs

Runtime/PlayerParticipation/Contracts/
├── PlayerActorPreparationResult.cs
└── PlayerActorPreparationStatus.cs

Runtime/PlayerParticipation/Runtime/
└── PlayerParticipationRuntimeHostModule.cs
```

## Out of scope

```text
automatic Route/Activity ownership selection
Activity admission/readiness integration
local Player leave/disconnect/reconnect
post-materialization gameplay input activation
PlayerSlotOccupancy
camera request publication
Actor Presentation / Skin
FIRSTGAME integration
```

## Technical acceptance

```text
package compiles in Unity 6.5
runtime host composition fails explicitly when dependencies are unavailable
real PlayerInputManager join reaches the preparation host registry
prepare resolves only an explicitly registered joined host
selection and preparation use one Session context identity
explicit RuntimeScopeContext is required
prepare/release are idempotent
selection mutation remains guarded while prepared
shutdown attempts explicit release
no Runtime -> Editor dependency
no reflection in Runtime
no global lookup or service locator
no RuntimeContent leaks after release
```

## Product acceptance

```text
stable Local Player Host remains the PlayerInput owner
Logical Actor is created only after explicit selection and preparation
caller does not manually instantiate or bind the Logical Actor prefab
runtime-host diagnostics explain missing host, selection, scope and cleanup state
prior P3G/P3H/P3J contracts remain intact
```

## Expected QA

Outside Play Mode:

```text
Immersive Framework/QA/Player/P3J.5 Apply Runtime Host Preparation Fixture
```

Enter Play Mode with normal Framework startup, then run:

```text
Immersive Framework/QA/Player/P3J.5 Run Runtime Host End-to-End Preparation Smoke
```

Expected:

```text
[P3J5_RUNTIME_HOST_END_TO_END_PREPARATION_SMOKE] status='Passed'
```

## Suggested commits

```text
Framework:
P3J.5 — integrate Player Actor preparation with runtime host

QAFramework:
P3J.5 — validate runtime-host end-to-end Player Actor preparation
```
