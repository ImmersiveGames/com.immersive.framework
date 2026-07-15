# P3K.7F — Session Player Gameplay Runtime Composition

Status: **closed — Unity compile and QA PASS 48/48**  
Type: **runtime composition + product authority wiring + technical QA**

## Objective

Compose the proven P3K.2-P3K.7E authorities under the real
`FrameworkRuntimeHost` lifetime.

```text
FrameworkRuntimeHost
-> P3J preparation module
-> P3K.7C candidate module
-> P3K.2 occupancy
-> P3K.3 input binding
-> P3K.4 camera eligibility
-> P3K.5 gameplay admission
-> P3K.7D reversible per-Slot handoff
-> P3K.7E atomic multi-Slot group
```

Before this cut, those authorities were real and QA-proven but P3K.2-P3K.5 and
the handoff/group contexts were constructed only by technical smokes. Wiring
them directly into Activity lifecycle would therefore depend on non-product
composition.

## Scope correction

The planned Activity lifecycle integration is deferred to P3K.7G.

Two blockers were found:

1. No official Session-scoped owner composed P3K.2-P3K.5 and P3K.7D/P3K.7E.
2. The original P3K.7D endpoint source retained one fixed
   `UnityPlayerInputGateAdapter`, which is unsuitable for multiple local Player
   Slots.

P3K.7F closes both gaps without mutating `GameFlowRuntime` or
`ActivityFlowRuntime`.

## Runtime host module

`PlayerGameplayRuntimeHostModule` is a host-scoped composition component. It is
not a domain singleton and does not expose global lookup.

The module is attached idempotently when the P3J preparation module becomes
ready. Initialization fails explicitly when any authority cannot be composed.

## Multi-Slot endpoint source

`HostScopedPlayerGameplayChainEndpointSource` starts from exact current P3J
physical evidence.

For each Slot it resolves:

```text
exact LocalPlayerHostAuthoring
exact PlayerActorDeclaration
exact UnityPlayerInputGateAdapter on that stable host
optional/required PlayerGameplayCameraAuthoring under that Actor
current FrameworkRuntimeHost CameraOutputSessionBinding
```

It performs no scene, name, tag, hierarchy-root or static service lookup.

Exactly one gate adapter must exist on the stable host and target that host's
own `PlayerInput`.

## Current gameplay chain operations

The host module exposes internal typed operations for the next lifecycle cut:

```text
TryEnsureCurrentGameplay
  P3J preparation
  -> P3K.2
  -> P3K.3
  -> P3K.4
  -> P3K.5

TryReleaseCurrentGameplay
  exact P3K.5 token
  -> canonical reverse release
```

A failed build rolls back only steps created by that operation. Rollback failure
is distinct from build failure.

## Candidate and handoff operations

The same module owns and delegates:

```text
TryStageCandidate
TryBeginActivityHandoffGroup
TryCommitActivityHandoffGroup
TryRollbackActivityHandoffGroup
TryRetryActivityHandoffCommitCleanup
```

P3K.7G can therefore integrate one typed host authority rather than manually
assembling contexts.

## Camera output

`FrameworkRuntimeHost` retains the exact `CameraOutputSessionBinding` already
resolved during boot and exposes it only to the same-host gameplay composition.

No camera output fallback is created. A Player Actor with camera authoring fails
explicitly when the Session output is unavailable.

## Diagnostics

`PlayerGameplayRuntimeHostSnapshot` includes:

```text
Session identity
configured Slot count
P3K.2 occupancy snapshot
P3K.3 input snapshot
P3K.4 camera snapshot
P3K.5 admission snapshot
P3K.7C candidate snapshot
P3K.7E group snapshot
active P3K.7D handoff count
last operation status and diagnostic
```

Public snapshots retain no Unity object references.

## Product surface

No new designer-facing component or Profile is introduced.

Existing authoring remains authoritative:

```text
PlayerSlotProfile
ActorProfile
LocalPlayerProvisioningAuthoring
LocalPlayerHostAuthoring
UnityPlayerInputGateAdapter
PlayerGameplayCameraAuthoring
CameraOutputSessionBinding
```

## Out of scope

```text
GameFlowRuntime mutation
ActivityFlowRuntime mutation
P3J.6 lifecycle participant GameplayReady integration
same-Route Activity pre-activation
Route Startup Activity handoff
transition or loading presentation
FIRSTGAME integration
```

## QA

Run in a fresh Play Mode session:

```text
Immersive Framework
  > QA
    > Player
      > P3K.7F Run Session Gameplay Runtime Composition Smoke
```

Expected:

```text
[P3K7F_SESSION_GAMEPLAY_RUNTIME_COMPOSITION_SMOKE]
status='Passed'
cases='48'
```

## Follow-up integration

```text
P3K.7G — Same-Route Activity Lifecycle Admission Integration
```

P3K.7G consumes this host module and integrates the official Player transaction
with a real same-Route Activity request. See:

```text
P3K7G-SAME-ROUTE-ACTIVITY-LIFECYCLE-ADMISSION.md
```
