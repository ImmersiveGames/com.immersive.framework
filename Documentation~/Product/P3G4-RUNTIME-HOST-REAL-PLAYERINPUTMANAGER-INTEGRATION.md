# P3G.4 — Runtime Host and real PlayerInputManager integration

## Type

Runtime integration and technical QA.

## Objective

Compose one local Player provisioning runtime per framework Session, inject the explicit
`LocalPlayerProvisioningAuthoring`, and execute the P3G join transaction through Unity's
real `PlayerInputManager`.

## Runtime shape

```text
loaded scene
  LocalPlayerProvisioningAuthoring
    explicit PlayerInputManager reference

FrameworkRuntimeHost
  PlayerParticipationRuntimeHostModule
    PlayerParticipationRuntimeContext

  LocalPlayerProvisioningRuntimeHostModule
    UnityLocalPlayerProvisioningBackend
    LocalPlayerProvisioningBridge
```

The bootstrap is the composition root. It resolves authoring by component type among
loaded scenes after route and UIGlobal preparation. It does not use names, tags,
hierarchy paths, `PlayerInputManager.instance`, or a framework service locator.

## Authoring multiplicity policy

```text
zero loaded declarations
  explicit NotConfigured diagnostic
  boot continues
  local joins remain unavailable

one loaded declaration
  validate and bind one Session bridge

more than one loaded declaration
  boot fails explicitly
```

Invalid manager configuration or failed binding also blocks boot. No declaration is
silently selected from an ambiguous set.

## Product runtime surface

`LocalPlayerProvisioningAuthoring` exposes explicit operations:

```text
OpenJoining(source, reason)
CloseJoining(source, reason)
SetDynamicCapacity(capacity, source, reason)
RequestJoin(request)
```

The component has no `Awake`, `OnEnable`, or `Start` gameplay behavior. No join occurs
unless a caller invokes `RequestJoin` after joining has been opened.

## Real join flow

```text
explicit request
-> reserve first Available configured Slot
-> create Pending Local Player Join
-> PlayerInputManager.JoinPlayer
-> correlate direct return and joined callback
-> validate PlayerInput
-> validate PlayerActorDeclaration
-> commit Slot Joined
-> expose typed evidence through LocalPlayerJoinResult
```

The physical Player host is created only by `PlayerInputManager`. Unity `playerIndex`
remains diagnostic evidence and never becomes `PlayerSlotId`.

## Failure policy

```text
invalid request/configuration
  reject before physical provisioning when possible

failure after reservation
  explicitly release reservation

unexpected PlayerInputManager callback
  do not admit the Player
  reject the physical host
  preserve diagnostic evidence
```

## Files

```text
Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs
Runtime/PlayerParticipation/Authoring/LocalPlayerProvisioningAuthoring.cs
Runtime/PlayerParticipation/Contracts/LocalPlayerJoinResult.cs
Runtime/PlayerParticipation/Contracts/PlayerParticipationOperationResult.cs
Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningAuthoringDiscovery.cs
Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningRuntimeHostModule.cs
Editor/PlayerParticipation/LocalPlayerProvisioningAuthoringEditor.cs
```

## QA

1. Outside Play Mode, execute:

```text
Immersive Framework/QA/Player/P3G.4 Apply Real Join Fixture
```

2. Enter Play Mode with normal Framework startup enabled.
3. Wait for the successful boot and provisioning runtime initialization logs.
4. Execute:

```text
Immersive Framework/QA/Player/P3G.4 Run Runtime Host Real Join Smoke
```

Expected:

```text
[P3G4_RUNTIME_HOST_REAL_JOIN_SMOKE] status='Passed' cases='18'
```

The smoke is one-shot per Play Mode because local leave/release belongs to a later cut.

## Out of scope

```text
ActorProfile selection
Actor-specific logical composition
Presentation/Skin materialization
Activity admission and readiness
occupancy
camera and gameplay input activation
leave/disconnect/reconnect
FIRSTGAME integration
```

## Acceptance

```text
one bridge is composed per Session
manager injection is explicit
join window is explicit
real PlayerInputManager creates the host
returned PlayerInput and callback correlate
PlayerActorDeclaration is validated
first configured Available Slot becomes Joined
Session context identity remains stable
closing joining does not remove the admitted Player
no hidden/global lookup is introduced
```

## Suggested commit

```text
P3G.4 — integrate real local Player joins with the runtime host
```
