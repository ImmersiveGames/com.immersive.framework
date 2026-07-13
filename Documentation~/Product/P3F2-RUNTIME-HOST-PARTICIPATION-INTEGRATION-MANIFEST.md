# P3F.2 — Runtime Host Player Participation Integration

## Objective

Compose exactly one Session Player participation runtime during framework boot without introducing `PlayerInputManager`, Actor selection, Actor materialization or Activity admission.

## Type

Runtime integration + diagnostics + QA handoff.

## Implementation shape

```text
ImmersiveFrameworkBootstrap
  creates FrameworkRuntimeHost
  attaches one PlayerParticipationRuntimeHostModule to the same persistent GameObject

PlayerParticipationRuntimeHostModule
  host-scoped composition adapter
  owns one plain C# PlayerParticipationRuntimeContext
  has no static registry
  exposes only internal typed access from a known FrameworkRuntimeHost reference
```

The domain authority remains `PlayerParticipationRuntimeContext`. The MonoBehaviour module only binds that plain C# context to the established host lifetime. It is not a Player manager and contains no Slot state machine logic.

## Initialization policy

```text
ordered roster: GameApplicationAsset.LocalPlayerSlots
initial dynamic capacity: configured Slot count
initial joining state: closed
initial Slot state: Available
```

Invalid runtime configuration fails boot explicitly. No empty roster, hidden default Profile or permissive fallback is created.

## Created

```text
Runtime/PlayerParticipation/Runtime/PlayerParticipationRuntimeHostModule.cs
Documentation~/Product/P3F2-RUNTIME-HOST-PARTICIPATION-INTEGRATION-MANIFEST.md
```

## Altered

```text
Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs
```

## Boot diagnostics

Expected information:

```text
Player participation Session runtime initialized.
configuredSlots='<count>'
dynamicCapacity='<count>'
joiningOpen='False'
revision='1'
```

The canonical boot debug record also includes:

```text
playerParticipationInitialized
playerParticipationContext
playerParticipationRevision
playerParticipationSlots
playerParticipationCapacity
playerParticipationJoiningOpen
```

## Out of scope

```text
manual join request
PlayerInputManager.JoinPlayer
PlayerInput admission
ActorProfile selection
Actor materialization
Activity projection evaluation
Activity admission
local leave
FIRSTGAME integration
```

## QA

Enter Play Mode with Framework startup enabled, then run:

```text
Immersive Framework > QA > Player > P3F.2 Run Runtime Host Integration Smoke
```

Expected:

```text
[P3F2_RUNTIME_HOST_INTEGRATION_SMOKE] status='Passed' cases='12'
```

## Suggested commit

```text
P3F.2 — integrate Session Player participation with runtime host
```
