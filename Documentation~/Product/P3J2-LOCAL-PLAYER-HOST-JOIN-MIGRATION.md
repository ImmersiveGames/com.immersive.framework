# P3J.2 — Local Player Host Authoring and Join Contract Migration

Status: implementation cut; Unity compile and QA pending.  
Type: product authoring, runtime contract migration and technical integration.

## Objective

Separate the stable Unity object provisioned by `PlayerInputManager` from the contextual Logical Actor selected through `ActorProfile`.

```text
PlayerInputManager
  -> LocalPlayerHostAuthoring
       PlayerInput
       ActorMount
       runtime PlayerSlotDeclaration after admission

ActorProfile.LogicalActorHostPrefab
  -> not materialized by this cut
```

## Product surface

The reusable Player Prefab assigned to `PlayerInputManager` now uses:

```text
Local Player Host
├── PlayerInput
├── LocalPlayerHostAuthoring
└── ActorMount
```

It must not contain:

```text
ActorDeclaration
PlayerActorDeclaration
PlayerSlotDeclaration
ActorProfile-specific gameplay
Presentation
camera request
```

Create a valid host through:

```text
GameObject
  Immersive Framework
    Player
      Local Player Host
```

The command creates `PlayerInput`, `LocalPlayerHostAuthoring` and an explicit `ActorMount`. `LocalPlayerHostAuthoring` has a designer-first Inspector, embedded validation and Advanced/Debug evidence. It has no `Awake`, `OnEnable`, `Start` or gameplay behavior.

## Join transaction

```text
1. validate request and technical backend
2. reserve next configured Session Slot
3. provision PlayerInput through PlayerInputManager
4. correlate direct return and joined callback
5. resolve and validate LocalPlayerHostAuthoring
6. stage PlayerSlotDeclaration on the technical host
7. commit Slot as Joined in PlayerParticipationRuntimeContext
8. commit the staged host binding
9. return LocalPlayerJoinResult with host evidence
```

Failure before Slot commit:

```text
staged host binding rolled back
reservation released
invalid host rejected physically by backend
original failure and rollback evidence preserved
```

## Contract migration

Removed from the join result:

```text
PlayerActorDeclaration
```

Added:

```text
LocalPlayerHostAuthoring LocalPlayerHost
HasLocalPlayerHostEvidence
```

Statuses:

```text
RejectedMissingLocalPlayerHost
RejectedInvalidLocalPlayerHost
```

A successful join proves only:

```text
stable local technical host exists
PlayerInput exists
Slot is Joined
Slot declaration is bound to the host
ActorMount exists and is empty
```

It does not prove:

```text
ActorProfile selected
ActorId allocated
Logical Actor Host materialized
Presentation materialized
occupancy applied
gameplay input enabled
camera request published
```

## Contextual Player Actor authoring

`PlayerActorDeclaration` no longer requires or discovers a same-object `PlayerInput`.

```text
PlayerActorDeclaration : ActorDeclaration
PlayerInput evidence = optional runtime injection
```

The Actor Profile validator now rejects `PlayerInput` inside a Player Logical Actor Host prefab. `PlayerInput` belongs to `LocalPlayerHostAuthoring`.

## Files created

```text
Runtime/PlayerParticipation/Authoring/LocalPlayerHostAuthoring.cs
Editor/PlayerParticipation/LocalPlayerHostAuthoringEditor.cs
Editor/PlayerParticipation/LocalPlayerHostAuthoringValidator.cs
Editor/PlayerParticipation/LocalPlayerHostCreationUtility.cs
Documentation~/Product/P3J2-LOCAL-PLAYER-HOST-JOIN-MIGRATION.md
```

## Files changed

```text
Runtime/Actors/PlayerActorDeclaration.cs
Runtime/PlayerParticipation/Contracts/LocalPlayerJoinResult.cs
Runtime/PlayerParticipation/Contracts/LocalPlayerJoinStatus.cs
Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningBridge.cs
Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningRuntimeHostModule.cs
Editor/PlayerParticipation/ActorDeclarationEditor.cs
Editor/PlayerParticipation/LocalPlayerProvisioningAuthoringEditor.cs
Editor/PlayerParticipation/LocalPlayerProvisioningValidator.cs
Editor/PlayerParticipation/PlayerActorSelectionAuthoringValidator.cs
```

## QA

Outside Play Mode:

```text
Immersive Framework/QA/Player/P3J.2 Run Local Player Host Migration Smoke
Immersive Framework/QA/Player/P3G.2 Run Join Contract Authoring Smoke
Immersive Framework/QA/Player/P3G.3 Run Provisioning Bridge Synthetic Smoke
Immersive Framework/QA/Player/P3H.2 Run Actor Selection Authoring Smoke
Immersive Framework/QA/Player/P3J.1 Run Actor Declaration Hierarchy Smoke
```

Apply the real fixture outside Play Mode:

```text
Immersive Framework/QA/Player/P3G.4 Apply Real Join Fixture
```

Then enter Play Mode and run:

```text
Immersive Framework/QA/Player/P3G.4 Run Runtime Host Real Join Smoke
```

For P3H.4 regression, apply its fixture again because Actor Profiles now reference separate Logical Actor Host prefabs.

## Technical acceptance

```text
package compiles
QA compiles
missing/invalid technical host is explicitly rejected
no reservation remains after failed admission
joined host exposes the exact committed PlayerSlotId
join does not create or select an Actor
PlayerActorDeclaration no longer requires PlayerInput
ActorProfile validator rejects PlayerInput on Logical Actor Host
no Runtime dependency on Editor
no singleton or global lookup introduced
```

## Product acceptance

```text
user can author the PlayerInputManager prefab as a clear Local Player Host
Inspector explains technical host versus Logical Actor
ActorMount is explicit
invalid host configuration is visible before Play Mode
join result diagnostics identify host, Slot and ActorMount
```

## Out of scope

```text
Logical Actor materialization
ActorId allocation
RuntimeContent handle integration
prepare / release / replace
Presentation / Skin
PlayerSlotOccupancy
camera target rebinding
gameplay input activation
local Player leave
FIRSTGAME integration
```

## Architectural gain

The framework no longer treats the Unity provisioning prefab as both the stable PlayerInput host and the replaceable game Actor. Session join and Actor composition now have independent contracts and lifetimes.

## Usability gain

The provisioning prefab has one explicit purpose and one visible authoring component. Designers no longer need to place an Actor identity on a reusable technical host before an Actor has been selected.

## Suggested commits

```text
Framework:
P3J.2 — migrate local join to stable Local Player Host

QAFramework:
P3J.2 — validate Local Player Host join migration
```
