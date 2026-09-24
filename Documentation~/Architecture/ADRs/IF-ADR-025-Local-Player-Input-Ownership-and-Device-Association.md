# IF-ADR-025 — Local Player Input Ownership and Device Association

Status: **Accepted / Implemented / Player QA Certified**  
Accepted: **2026-09-05**  
Last updated: **2026-09-06**  
Type: architecture / Player input ownership / public observation / local multiplayer  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-023, IF-ADR-024  
Technical certification: `IF-ADR-025-LOCAL-PLAYER-INPUT-OWNERSHIP-TECHNICAL-CERTIFICATION-2026-09-06.md`

## Context

The Player product already owned the main structural and physical boundaries required for local multiplayer before this ADR.

`PlayerSessionProfile.SupportedSlots` defines the complete Session Slot universe and the deterministic order used by ordinary untargeted Join. `LocalPlayerJoinRequest` permits an optional `InputDevice` hint, and Manager-Provisioned Join forwards that device to the Unity Input System provisioning backend. Successful admission correlates the provisioned `PlayerInput`, `LocalPlayerHostAuthoring` and reserved Session Slot.

The physical chain is:

```text
Join request
  -> reserve next eligible Supported Slot
  -> PlayerInputManager.JoinPlayer(...)
  -> exact PlayerInput
  -> exact Local Player Host
  -> commit Session Slot
  -> Session-owned Player occurrence
```

Gameplay projection resolves the Player gameplay input reader against the admitted Player/Actor chain rather than through a global input authority.

At ADR acceptance time, the public Session observation surface did not expose immutable input ownership evidence for each joined Slot, and the designer-facing `PlayerSessionJoinCommandTrigger` did not expose explicit device-aware invocation even though the lower Join request already supported `PairWithDevice`.

That left Local Multiplayer blocked at the public-product proof boundary even though the underlying runtime already contained most of the required mechanism.

This ADR closes that boundary without introducing a parallel input authority.

## Decision

Local Player input ownership is part of the current **Session Player occurrence**.

The Framework exposes immutable, per-Slot evidence describing the input resources currently owned by that occurrence, while retaining `PlayerInput` and Unity Input System mutation authority inside the existing Framework-owned Player provisioning/runtime chain.

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

The scoped Session observation surface exposes immutable input ownership evidence for each current joined Local Player Slot.

The canonical public summary is:

```text
LocalPlayerInputOwnershipSummary
```

It represents evidence only. It does not expose mutation authority.

The summary correlates at minimum:

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

The exact serialized/internal storage strategy is implementation detail. The public surface remains immutable.

`PlayerSessionScopedSlotObservation` exposes:

```text
InputOwnership
HasInputOwnershipEvidence
```

Conceptually:

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

The public input summary does not expose mutable `PlayerInput` or `InputUser` authority through `PlayerSessionScopedSlotObservation`.

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

The Framework passes that device through the existing provisioning backend to the Unity Input System.

The designer-facing Join command exposes explicit device-aware invocation so a caller that already owns an input event can invoke the same Join operation with the device that produced that event.

The canonical surface is:

```csharp
InvokeFromDevice(InputDevice device)
```

while existing:

```csharp
Invoke()
```

continues to request ordinary Join without a device hint.

`InvokeFromDevice(...)` does not choose a Slot and does not bypass scoped Session access.

An explicit device-aware invocation with a missing/invalid device rejects explicitly. It does not silently degrade into device-less Join.

The command component continues to own only its typed `LocalPlayerJoinResult`.

### 9. Event detection is separate from Join authority

`PlayerSessionJoinCommandTrigger` is not a global device listener.

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

IF-ADR-032 defines Camera as explicit Session-owned `1..N` Output capacity and keeps
Player Subject contribution separate from Camera ownership. Player count never creates
Camera Outputs and PlayerInputManager remains the external physical split-layout writer.

The IF-ADR-032 runtime migration is pending. Historical Camera QA remains evidence for
the former boundaries only and does not change this ADR's input-ownership contract.

A Local Multiplayer Sample may prove multiple Player input ownership independently from
the selected Camera Presentation topology.

## Public contract requirements

The implemented ADR-025 contract provides the following.

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

`PlayerSessionScopedSlotObservation` exposes:

```text
HasInputOwnershipEvidence
InputOwnership
```

### Device-aware Join command

`PlayerSessionJoinCommandTrigger` allows explicit invocation using the originating `InputDevice` while preserving the existing device-less `Invoke()` behavior.

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

## QA certification

ADR-025 is technically certified by the focused Player QA proof executed on **2026-09-06**.

The proof uses two distinct deterministic QA `InputDevice`s:

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

The certified proof establishes all required ADR-025 invariants:

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
14. A's Slot reaches terminal availability only after the required release boundary;
15. Rejoin into the released Slot creates a fresh Player occurrence and fresh input ownership evidence;
16. no previous ownership/`PlayerInput` correlation is treated as current after Slot reuse.

The existing second-Player QA that intentionally shares the Editor keyboard remains separate evidence for Player/Slot lifecycle and is not relabeled as distinct-device certification.

The focused ADR-025 proof uses deterministic QA-created devices and does not depend on a developer physically attaching two controllers.

### Final consolidated evidence

The final Full Player QA run completed:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
cases='17/17'
```

The ADR-025 focused proof completed with:

```text
leaveA='True'
bPreserved='True'
snapshotImmutable='True'
rejoinA='True'
staleOwnershipRejected='True'

bHostPreserved='True'
bHasHostEvidence='True'
bHostBindingMatches='True'
bAssignmentMatches='True'
bHostReferenceAlive='True'
bHostIsJoined='True'

bOwnershipPreserved='True'
bReaderPreserved='True'
bInputOperational='True'

cleanup='succeeded'
proof='succeeded'
```

## QA reconciliation — preservation baseline semantics

During certification, the focused Leave/Rejoin proof initially compared Player B after Leave A against Host/assignment evidence captured immediately after Join.

That baseline was semantically invalid because Manager-Provisioned Join first establishes the retained Session physical Player, while Activity contextual Host/assignment projection is established later when GameplayReady is reached.

The failing diagnostic showed:

```text
B current HostBindingIdentity   = valid contextual identity
B expected HostBindingIdentity  = empty

B current AssignmentToken       = valid contextual assignment
B expected AssignmentToken      = empty
```

while simultaneously proving:

```text
bHasHostEvidence       = True
bHostReferenceAlive    = True
bHostIsJoined          = True
bOwnershipPreserved    = True
bReaderPreserved       = True
```

The defect was therefore classified as:

```text
QA ASSERTION SEMANTICS DEFECT
```

not a Framework Host lifecycle defect.

The preservation baseline was corrected to use the scoped observation immediately before Leave A, after GameplayReady contextual projection exists.

Canonical preservation proof:

```text
B immediately before Leave A
    ==
B immediately after Leave A
    ==
B after Rejoin A
```

No equality was weakened and no Framework lifecycle rule was bypassed.

After correction:

```text
bHostBindingMatches = True
bAssignmentMatches  = True
bHostPreserved      = True
bPreserved          = True
```

and the proof continued through input operation, Rejoin and stale-ownership rejection to `proof='succeeded'`.

This reconciliation also confirms that Slot revision is not treated as immutable Player occurrence identity. B's revision remained unchanged across Leave A in the certified run, but preservation is established through the current Host/assignment/input ownership correlation, not by revision equality alone.

## Local Multiplayer product gate

The ADR-025 technical product gate is **cleared**.

The Framework can now publicly and objectively prove:

```text
Input Device
  -> admitted Local Player occurrence
  -> Player Slot
  -> PlayerInput
  -> isolated PlayerGameplayInputReader
  -> Leave release
  -> fresh Rejoin ownership
```

The canonical first Local Multiplayer Sample may therefore use:

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

Clearing the Framework technical gate does **not** itself certify the FIRSTGAME Local Multiplayer consumer sample. That sample remains to be constructed and proven independently in consumer Play Mode.

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
- implementation of split-screen or multi-output Camera work (owned by IF-ADR-032).
- Treating device disconnect by itself as Session Player Leave.

## Consequences

The Framework now has the public proof boundary required for local multiplayer without changing the existing Session identity model.

`PlayerSlotId` remains the stable Session Slot identity.

`PlayerInput` remains the physical local-input owner for the current admitted occurrence.

Unity `playerIndex` remains technical evidence, not semantic Player identity.

`InputDevice` remains an occurrence-owned physical resource rather than a permanent Player identity.

Consumers have sufficient read-only evidence to display, diagnose and verify current local Player input ownership without receiving a second mutation authority.

The existing Join, Host, Actor, gameplay and Leave transactions remain separate.

The Local Multiplayer Sample does not need consumer-authored Slot/device routing.

## Current implementation and certification coverage

The ADR-025 contract is implemented and certified.

Current product coverage:

```text
SupportedSlots ordered untargeted Join
LocalPlayerJoinRequest.PairWithDevice
PlayerInputManager-backed Manager-Provisioned Join
PlayerSessionJoinCommandTrigger.InvokeFromDevice(InputDevice)
Join result with PlayerInput and UnityPlayerIndex
current Host/Slot correlation
immutable per-Slot input ownership observation
LocalPlayerInputOwnershipSummary
LocalPlayerInputDeviceSummary
HasInputOwnershipEvidence / InputOwnership
per-Player gameplay input reader binding
distinct-device gameplay isolation
Session Player Leave
PlayerInput release/destruction
input ownership removal on Leave
Slot reuse / Rejoin
fresh ownership after Rejoin
stale previous ownership rejection
```

Technical certification:

```text
ADR-025 focused ownership proof = PASS
Full Player QA                  = 17/17 PASS
verdict                         = PLAYER QA CERTIFIED
```

No previous QA result is retroactively reinterpreted as ADR-025 evidence; certification is based on the focused proof added for this contract.

## Follow-up

Technical Framework implementation and QA certification are complete.

Remaining work is consumer/sample work:

1. Reconcile IF-ADR-015 so the generic Slot/device/input blocker is closed while Exact-Slot Join remains future scope.
2. Reconcile FIRSTGAME `FG-ADR-002` and Player Sample documentation to mark the Local Multiplayer technical gate as cleared.
3. Construct the canonical FIRSTGAME Local Multiplayer sample using the public ADR-025 surface.
4. Produce independent FIRSTGAME consumer Play Mode proof.
5. Keep split-screen/multi-output Camera work outside this Player contract unless separately required.

## Deferred decisions

- Exact-Slot public Join, if a real product requirement later appears.
- Fixed Player/device assignment policies.
- Device reassignment during a still-current Player occurrence.
- Device disconnect/reconnect policy beyond current Unity Input System behavior.
- Scene-Provided device acquisition/rebinding semantics beyond observable adopted `PlayerInput` evidence.
- Split-screen or multi-output Camera implementation details under IF-ADR-032.
