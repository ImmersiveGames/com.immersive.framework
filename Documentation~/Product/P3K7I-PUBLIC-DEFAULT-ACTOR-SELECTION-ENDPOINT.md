# P3K.7I-A — Public Default Actor Selection Endpoint

Status: implementation ready for Unity compile and QA  
Type: product runtime API prerequisite for FIRSTGAME integration

## Objective

Expose one public typed request surface that lets a real game apply the
`PlayerSlotProfile.DefaultActorProfile` after a successful local Player join.

```text
LocalPlayerProvisioningAuthoring.RequestJoin
-> Slot Joined and Unselected

LocalPlayerActorSelectionRequestAuthoring.RequestDefaultActorSelection
-> canonical P3H/P3J selection transaction
-> Slot Joined and Selected
```

Join never silently selects an Actor.

## Product surface

Add the component beside `LocalPlayerProvisioningAuthoring`:

```text
Immersive Framework
  Player
    Local Player Actor Selection Requests
```

Required authoring:

```text
same GameObject
  PlayerInputManager
  LocalPlayerProvisioningAuthoring
  LocalPlayerActorSelectionRequestAuthoring
```

The component exposes:

```text
ProvisioningAuthoring
RuntimeReady
RequestDefaultActorSelection
LastResult
LastDiagnostic
RequestCount
```

## Authority

The component is a request boundary only.

```text
public request component
-> current FrameworkRuntimeHost
-> exact PlayerActorPreparationRuntimeHostModule
-> PlayerActorPreparationRuntimeContext
-> PlayerParticipationRuntimeContext
```

Current Actor selection state remains in the Session participation runtime.
No second registry or selection state is introduced.

## Failure policy

Requests fail explicitly when:

```text
provisioning reference is missing
components do not share one GameObject
provisioning runtime is not ready
FrameworkRuntimeHost is unavailable
P3J preparation authority is unavailable
Slot identity or revision is invalid/stale
Slot is not Joined
Slot has no default ActorProfile
selection policy rejects the request
```

No default Profile is inferred and no reflection is used.

## Scope

```text
public explicit default Actor selection
typed result and diagnostics
same-authoring validation
runtime-host delegation
technical QA
FIRSTGAME consumption
```

## Out of scope

```text
automatic selection during join
Actor picker UI
replace/clear UI
logical Actor materialization
Activity request
FIRSTGAME scene migration
```
