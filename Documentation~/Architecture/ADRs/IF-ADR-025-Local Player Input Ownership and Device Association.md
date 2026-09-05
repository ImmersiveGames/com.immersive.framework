# IF-ADR-025 — Local Player Input Ownership and Device Association

Status: **Accepted — implementation and QA certification pending**  
Accepted: **2026-09-05**  
Last updated: **2026-09-05**  
Type: architecture / Player input ownership / public observation / local multiplayer  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-023, IF-ADR-024

## Context

The current Player product already owns the main structural and physical boundaries required for local multiplayer.

`PlayerSessionProfile.SupportedSlots` defines the complete Session Slot universe and the deterministic order used by ordinary untargeted Join. `LocalPlayerJoinRequest` already permits an optional `InputDevice` hint, and Manager-Provisioned Join forwards that device to the Unity Input System provisioning backend. Successful admission correlates the provisioned `PlayerInput`, `LocalPlayerHostAuthoring` and reserved Session Slot.

The current runtime therefore already has the physical chain:

```text
Join request
  -> reserve next eligible Supported Slot
  -> PlayerInputManager.JoinPlayer(...)
  -> exact PlayerInput
  -> exact Local Player Host
  -> commit Session Slot
  -> Session-owned Player occurrence
```

Gameplay projection also resolves the Player gameplay input reader against the admitted Player/Actor chain rather than through a global input authority.

However, the public Session observation surface does not currently expose immutable input ownership evidence for each joined Slot.

The current `PlayerSessionScopedSlotObservation` exposes:

```text
Slot
HostEvidence
Preparation
CurrentActor
GameplayAdmission
```

but not:

```text
Unity playerIndex
paired InputDevice identity
current control scheme
device -> current Session Player occurrence correlation
```

`LocalPlayerJoinResult` exposes the materialized `PlayerInput` and `UnityPlayerIndex` for the individual Join operation, but that result is command-local evidence. It is not a durable read-only Session observation surface and cannot prove later, from the Session state alone, which input resources remain associated with a current Player occurrence.

The designer-facing `PlayerSessionJoinCommandTrigger` also currently invokes ordinary Join with no explicit `InputDevice`, even though `LocalPlayerJoinRequest` and the internal provisioning backend already support one.

The existing Player QA proves a second simultaneous Player, Slot allocation, Leave and Rejoin. Its current second-Player path intentionally uses the same Editor keyboard as explicit provisioning input. It therefore proves Player/Slot cardinality and lifecycle, but does not certify distinct-device local multiplayer or absence of cross-input.

This leaves Local Multiplayer blocked at the public-product proof boundary even though the underlying runtime already contains most of the required mechanism.

## Decision

Local Player input ownership is part of the current **Session Player occurrence**.

The Framework must expose immutable, per-Slot evidence describing the input resources currently owned by that occurrence, while retaining `PlayerInput` and Unity Input System mutation authority inside the existing Framework-owned Player provisioning/runtime chain.

Conceptually:

```text
Session Player occurrence
├── PlayerSlotId
├── Slot occurrence/revision evidence
├── admitted Local Player Host
│   └── PlayerInput
│       ├── Unity playerIndex
│       ├── current control scheme
│       └── paired InputDevice(s)
├── Actor selection/preparation
└── current Activity gameplay projection
```

The device is not Player identity.

The stable Player Slot is not Unity `playerIndex`.

The consumer does not become the owner of `PlayerInput`, `InputUser`, device pairing, Slot allocation or Join correlation.

### 1. Authority model

| Concern | Authority |
|---|---|
| Supported Slot universe and untargeted Join order | `PlayerSessionProfile` resolved Session configuration |
| Slot reservation / Joined state | `PlayerParticipationRuntimeContext` |
| Local Player provisioning | existing Manager-Provisioned Player provisioning chain |
| Physical input owner | admitted `PlayerInput` for the current Session Player occurrence |
| Device pairing | Unity Input System through the Framework-owned provisioning path |
| Host/Slot correlation | existing Local Player provisioning/admission bridge |
| Gameplay input projection | existing Player gameplay input binding/reader chain |
| Public observation | `IPlayerSessionScopedAccess` / `PlayerSessionObserver` |
| Leave and terminal resource release | IF-ADR-020 |

No new global Player input manager, device registry, Slot/device dictionary, service locator or consumer-owned routing layer is introduced.

### 2. Device association belongs to the current Player occurrence

A paired device is a physical resource associated with one admitted Local Player occurrence.

Example:

```text
player.1
occurrence A
  -> PlayerInput A
  -> Gamepad 1
```

After successful Leave, occurrence A is terminal.

A later Join may reuse the stable Slot:

```text
player.1
occurrence B
  -> PlayerInput B
  -> Gamepad 2
```

Occurrence B is a new Player occurrence even though `PlayerSlotId` is the same.

No device association, `PlayerInput`, control scheme, gameplay input binding or other mutable occurrence state from A may be inherited merely because the Slot is reused.

### 3. Slot identity and Unity playerIndex remain separate

`PlayerSlotId` is Framework Session identity.

Unity `PlayerInput.playerIndex` is technical evidence of the materialized Unity Input System Player.

The caller does not choose `playerIndex` as Player identity and the Framework does not derive the target Slot from `playerIndex`.

Canonical relationship:

```text
PlayerSlotId
  -> current admitted Local Player Host
  -> current PlayerInput
  -> Unity playerIndex
```

Not:

```text
PlayerSlotId == playerIndex
```

The public evidence may correlate them for the current occurrence, but must not collapse them into one identifier.

### 4. Ordinary Join remains untargeted by Slot

This ADR does not introduce Exact-Slot Join.

The existing Session rule remains:

```text
SupportedSlots
  -> ordered eligible Slot universe

ordinary Join
  -> first eligible vacant Supported Slot
```

An explicit input device identifies the physical input resource requesting admission. It does not select a specific Slot.

Therefore:

```text
Device A requests Join
  -> next eligible Slot

Device B requests Join
  -> next eligible Slot
```

The consumer must not implement:

```text
Gamepad 1 -> player.1
Gamepad 2 -> player.2
```

as a parallel authority unless a future explicit product contract defines fixed device/Slot assignment.

### 5. Public immutable input ownership evidence

The scoped Session observation surface gains immutable input ownership evidence for each current joined Local Player Slot.

The canonical public summary is:

```text
LocalPlayerInputOwnershipSummary
```

It represents evidence only. It does not expose mutation authority.

The summary must correlate at minimum:

```text
PlayerSlotId
current Host/assignment identity
UnityPlayerIndex
ControlScheme
paired device summaries
```

The paired-device element is:

```text
LocalPlayerInputDeviceSummary
```

and exposes stable diagnostic/observation data sufficient to distinguish the devices participating in the current occurrence, including at minimum:

```text
DeviceId
Layout
DisplayName
```

The exact serialized/internal storage strategy is implementation detail. The public surface must remain immutable.

`PlayerSessionScopedSlotObservation` gains:

```text
InputOwnership
HasInputOwnershipEvidence
```

conceptually:

```csharp
public LocalPlayerInputOwnershipSummary InputOwnership { get; }

public bool HasInputOwnershipEvidence { get; }
```

A consumer observing the Session can therefore establish:

```text
player.1
  -> host/assignment A
  -> Unity playerIndex X
  -> Gamepad device 7

player.2
  -> host/assignment B
  -> Unity playerIndex Y
  -> Gamepad device 12
```

without locating Player GameObjects, scanning scenes, reading hierarchy names or retaining command-local `LocalPlayerJoinResult`.

### 6. Observation is not authority

The public input summary must not expose mutable `PlayerInput` or `InputUser` authority through `PlayerSessionScopedSlotObservation`.

Consumers may inspect current ownership evidence but may not use that observation object to:

```text
pair/unpair devices
activate/deactivate PlayerInput
change action maps
change Slot
change playerIndex
force Join
force Leave
replace the current input owner
```

Those mutations remain governed by their existing owners and explicit public commands.

A `LocalPlayerJoinResult` may continue to expose operation-local technical evidence already present in the current API. This ADR does not redefine that result as the durable Session observation authority.

### 7. Input ownership is valid only for the current admitted occurrence

Input ownership evidence is published only when it can be correlated to the current admitted Local Player occurrence.

A valid record must not be fabricated from:

```text
PlayerInput.all
scene search
GameObject name
hierarchy position
device activity guessing
last command result
cached previous occurrence
```

The observation builder resolves the evidence from the Framework-owned current Player/Host correlation.

If no valid current input ownership can be correlated, the Slot observation reports:

```text
HasInputOwnershipEvidence = false
```

rather than inventing a fallback.

### 8. Join with an explicit device

`LocalPlayerJoinRequest.PairWithDevice` remains the canonical request-level device hint for Manager-Provisioned local Join.

The Framework continues to pass that device through the existing provisioning backend to the Unity Input System.

The designer-facing Join command is extended so a caller that already owns an input event can invoke the same Join operation with the device that produced that event.

The canonical surface is an explicit device-aware invocation on `PlayerSessionJoinCommandTrigger`, equivalent to:

```csharp
InvokeFromDevice(InputDevice device)
```

while existing:

```csharp
Invoke()
```

continues to request ordinary Join without a device hint.

`InvokeFromDevice(...)` does not choose a Slot and does not bypass scoped Session access.

An explicit device-aware invocation with a missing/invalid device must reject explicitly. It must not silently degrade into device-less Join.

The command component continues to own only its typed `LocalPlayerJoinResult`.

### 9. Event detection is separate from Join authority

This ADR does not require `PlayerSessionJoinCommandTrigger` itself to become a global device listener.

A game/sample may detect a Join action through authored Unity Input System configuration and forward the originating `InputDevice` to the explicit Join command.

Conceptually:

```text
authored Join action
  -> originating InputDevice
  -> PlayerSessionJoinCommandTrigger.InvokeFromDevice(device)
  -> scoped Session Join access
  -> canonical Framework Join
```

The event detector does not allocate Slots, instantiate Players, pair devices independently or store device ownership.

This preserves the existing separation:

```text
input event detection = consumer/authored interaction surface
Join authority         = Framework Player Session surface
```

### 10. Gameplay input isolation

Each Player gameplay reader remains bound through the existing gameplay input projection to the `PlayerInput` belonging to that Player occurrence.

This ADR does not introduce a second gameplay input router.

For two joined local Players:

```text
Player occurrence A
  -> PlayerInput A
  -> Gameplay Reader A

Player occurrence B
  -> PlayerInput B
  -> Gameplay Reader B
```

The required product invariant is:

```text
input from device(s) owned by A
  -> observable by Reader A according to its authored actions
  -> must not become Reader B input merely because both Players use the same action asset

input from device(s) owned by B
  -> observable by Reader B
  -> must not become Reader A input
```

This is an isolation requirement, not a requirement for duplicated action assets.

### 11. Leave releases input ownership

IF-ADR-020 remains authoritative for Leave.

Input ownership is occurrence-owned state and therefore participates in terminal release.

Canonical Local Player Leave includes, as applicable:

```text
validate exact current occurrence
  -> stage Leaving
  -> retire Activity gameplay/input authority
  -> retire Actor/representation authority
  -> release admitted Local Player Host
  -> deactivate/release current PlayerInput and its device association
  -> clear input ownership evidence
  -> commit Slot Available/Vacant
```

A Slot must not be published as terminally available while still exposing input ownership from the released occurrence.

Unity destruction may settle after logical release according to the existing Leave contract, but public current-occurrence observation must never retain stale ownership as if the previous Player were still current.

### 12. Rejoin creates fresh input ownership

After successful Leave:

```text
old Slot occurrence = terminal
old input ownership = terminal
```

A later Join into the same stable Slot creates new evidence.

Required invariant:

```text
same PlayerSlotId may be reused
but
old occurrence correlation != new occurrence correlation
old PlayerInput ownership != new PlayerInput ownership
```

The new Join may use the same physical device again or a different physical device. Either case is a new occurrence and must be observed from the new current correlation.

### 13. Manager-Provisioned V1 boundary

The device-aware Join capability defined by this ADR applies to **Manager-Provisioned Local Players** in V1.

That is the current path in which the Framework explicitly provisions `PlayerInput` through the Session-authorized Unity `PlayerInputManager` integration and therefore owns the Join/device correlation.

This ADR does not invent device-pairing authority for Scene-Provided Players.

Scene-Provided may expose input ownership evidence only where the existing adopted Local Player Host provides a valid current `PlayerInput` correlation under the Session-owned occurrence. Any additional Scene-Provided device acquisition/rebinding semantics require a separately proven contract.

Local Multiplayer Sample certification under this ADR uses Manager-Provisioned Players.

### 14. Camera is outside this ADR

This ADR defines local Player input ownership and isolation only.

It does not introduce:

```text
split-screen
multiple Camera outputs
per-device Camera
per-Slot Camera output
```

The current Camera product remains single-output unless a separate Camera ADR changes that boundary.

A Local Multiplayer Sample may therefore use one Activity-owned shared Camera while independently proving multiple Player input ownership.

## Public contract requirements

Implementation of this ADR must provide all of the following.

### Input ownership summary

A public immutable `LocalPlayerInputOwnershipSummary` associated with the current Slot observation.

Minimum semantic evidence:

```text
PlayerSlotId
current assignment/Host correlation
UnityPlayerIndex
ControlScheme
paired device collection
```

### Device summary

A public immutable `LocalPlayerInputDeviceSummary`.

Minimum semantic evidence:

```text
DeviceId
Layout
DisplayName
```

### Slot observation extension

`PlayerSessionScopedSlotObservation` must expose:

```text
HasInputOwnershipEvidence
InputOwnership
```

### Device-aware Join command

`PlayerSessionJoinCommandTrigger` must allow explicit invocation using the originating `InputDevice` while preserving the existing device-less `Invoke()` behavior.

No Exact-Slot parameter is added.

## Failure semantics

The input ownership extension fails closed.

Examples:

```text
Joined Slot but no valid current Host/Input correlation
  -> no fabricated input ownership evidence

explicit device Join with invalid device
  -> reject
  -> do not perform device-less fallback

device-specific Join fails
  -> preserve existing Join rollback semantics
  -> no Slot remains falsely Joined
  -> no public ownership evidence remains current

stale previous occurrence evidence
  -> never reported as current after Leave/Rejoin

multiple Players
  -> no cross-Slot ownership merge
```

Failure to publish valid input evidence must not mutate Session truth to make observation appear complete.

## QA certification requirement

Local Multiplayer is not certified merely by having two joined Slots.

A new focused Player QA proof must establish real distinct-device ownership and input isolation.

The positive proof requires two distinct deterministic QA `InputDevice`s:

```text
Device A
  -> Join
  -> Slot A
  -> PlayerInput A

Device B
  -> Join
  -> Slot B
  -> PlayerInput B
```

The QA must prove:

1. two distinct joined supported Slots;
2. two distinct admitted Local Player Hosts;
3. two distinct `PlayerInput` instances;
4. two distinct Unity `playerIndex` values for the simultaneous Players;
5. Slot A public observation reports Device A;
6. Slot B public observation reports Device B;
7. the ownership evidence correlates to the current Host/assignment for each Slot;
8. gameplay input from Device A reaches Player A's gameplay reader;
9. the same input does not become Player B gameplay input;
10. gameplay input from Device B reaches Player B's gameplay reader;
11. the same input does not become Player A gameplay input;
12. Leave of A does not disturb B;
13. Leave of A removes A's current input ownership evidence;
14. A's Slot reaches terminal availability only after the existing required release boundary;
15. Rejoin into the released Slot creates a fresh Player occurrence and fresh input ownership evidence;
16. no previous ownership/`PlayerInput` correlation is treated as current after Slot reuse.

The existing second-Player QA that intentionally shares the Editor keyboard remains valid evidence for Player/Slot lifecycle and must not be relabeled as distinct-device certification.

The focused ADR-025 proof may use deterministic QA-created `InputDevice`s. It must not depend on a developer physically attaching two controllers in order to produce repeatable certification.

## Local Multiplayer product gate

The official Local Multiplayer Sample remains blocked until the ADR-025 implementation and focused QA proof are complete.

The gate is cleared when the Framework can publicly and objectively prove:

```text
Input Device
  -> admitted Local Player occurrence
  -> Player Slot
  -> PlayerInput
  -> isolated PlayerGameplayInputReader
  -> Leave release
  -> fresh Rejoin ownership
```

At that point the canonical first Local Multiplayer Sample may use:

```text
PlayerSessionProfile
  HostProvisioning = ManagerProvisioned
  ActorResolution = ResolveConfiguredDefault
  SupportedSlots
    -> Player 1 / default Actor A
    -> Player 2 / default Actor B

two device-originated Join requests
two simultaneous Players
isolated gameplay input
one shared Activity-owned Camera
```

Split-screen is not required to certify this Player contract.

## Accepted scope

- Manager-Provisioned Local Player device-aware Join.
- Current-occurrence input ownership associated with Session Slot observation.
- Immutable public device/`playerIndex`/control-scheme evidence.
- Reuse of existing `LocalPlayerJoinRequest.PairWithDevice`.
- Reuse of existing Player provisioning and gameplay reader ownership paths.
- Device-aware invocation on the explicit Join command.
- Distinct-device QA.
- Negative cross-input QA.
- Leave/Rejoin freshness proof.
- Local Multiplayer Sample unblock gate after certification.

## Rejected scope

- Exact-Slot Join.
- `playerIndex` as Player Slot identity.
- Fixed Gamepad-to-Slot mapping.
- Global device registry.
- Parallel multiplayer input router.
- Consumer-owned `InputUser` or `PlayerInput` mutation through Session observation.
- Scene/hierarchy scans for Player/device correlation.
- Silent fallback from explicit device Join to device-less Join.
- Reusing old input ownership after Slot reuse.
- Duplicating gameplay input authority outside the existing binding/reader chain.
- Split-screen or multi-output Camera work.
- Treating device disconnect by itself as Session Player Leave.

## Consequences

The Framework gains the missing public proof boundary for local multiplayer without changing the existing Session identity model.

`PlayerSlotId` remains the stable Session Slot identity.

`PlayerInput` remains the physical local-input owner for the current admitted occurrence.

Unity `playerIndex` remains technical evidence, not semantic Player identity.

`InputDevice` remains an occurrence-owned physical resource rather than a permanent Player identity.

Consumers gain sufficient read-only evidence to display, diagnose and verify current local Player input ownership without receiving a second mutation authority.

The existing Join, Host, Actor, gameplay and Leave transactions remain separate.

The Local Multiplayer Sample no longer needs consumer-authored Slot/device routing once this ADR is implemented and certified.

## Current implementation coverage

At acceptance time, the following mechanics already exist:

```text
SupportedSlots ordered untargeted Join
LocalPlayerJoinRequest.PairWithDevice
PlayerInputManager-backed Manager-Provisioned Join
Join result with PlayerInput and UnityPlayerIndex
current Host/Slot correlation
per-Player gameplay input reader binding
Session Player Leave
PlayerInput release/destruction
Slot reuse / Rejoin
```

The following ADR-025 requirements are not yet implemented/certified:

```text
immutable per-Slot input ownership observation
device-aware designer Join invocation
two-distinct-device QA
negative cross-input QA
fresh public ownership proof after Leave/Rejoin
```

No existing QA result is reinterpreted as proof of those missing requirements.

## Required follow-up

1. Implement the immutable input ownership summaries.
2. Extend current scoped Slot observation with input ownership evidence.
3. Extend `PlayerSessionJoinCommandTrigger` with explicit device-aware invocation.
4. Add focused ADR-025 QA for two distinct devices, isolation, Leave and Rejoin.
5. Produce a dated technical certification record after QA passes.
6. Reconcile IF-ADR-015 to remove the now-closed generic Slot/device/input blocker while preserving Exact-Slot Join as future scope.
7. Reconcile FIRSTGAME `FG-ADR-002` and Player Sample documentation only after technical certification.
8. Build the Local Multiplayer Sample only after that gate is closed.

## Deferred decisions

- Exact-Slot public Join, if a real product requirement later appears.
- Fixed Player/device assignment policies.
- Device reassignment during a still-current Player occurrence.
- Device disconnect/reconnect policy beyond current Unity Input System behavior.
- Scene-Provided device acquisition/rebinding semantics beyond observable adopted `PlayerInput` evidence.
- Split-screen or multi-output Camera architecture.