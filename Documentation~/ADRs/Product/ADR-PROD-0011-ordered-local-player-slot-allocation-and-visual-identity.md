# ADR-PROD-0011 — Ordered local Player Slot allocation and visual identity

Status: Accepted  
Date: 2026-07-12  
Package: `com.immersive.framework`  
Area: Local Player Configuration / Slot Allocation / Product Presentation  
Related: `ADR-PROD-0007`, `ADR-PROD-0009`, `ADR-PROD-0010`

## Context

Local products need one consistent Slot allocation model that supports:

```text
single-player startup
Press Start to Join
local cooperative lobby
simultaneous character selection
Players leaving and later rejoining
dynamic join capacity
```

The product also needs each participant to have visible identity before an Actor is
selected.

Representative uses include:

```text
selection pointer color
UI panel accent
Player icon
nameplate
viewport indicator
device prompt
pause ownership indicator
diagnostic overlay
```

The Unity `playerIndex` cannot be treated as `PlayerSlotId`, and color/name/icon cannot
be used as logical identity.

The default product flow should not require every join caller to know or choose a
specific Slot.

## Decision

The Game/Application local-player configuration contains an explicit ordered array of
`PlayerSlotProfile` references.

```text
Local Player Slots[]
  [0] Player 1
  [1] Player 2
  [2] Player 3
  [3] Player 4
```

The initial and canonical local Slot allocation policy is:

```text
First Available By Configured Order
```

An ordinary local join request does not specify a Slot.

The participation context:

```text
evaluates the configured array from first to last
skips ineligible or unavailable Slots
reserves the first eligible Available Slot
creates the pending join operation
only then calls PlayerInputManager.JoinPlayer
```

The array position controls default allocation order.

`PlayerSlotProfile.DisplayOrder`, when present, is presentation metadata and does not
override the configured allocation order.

## PlayerSlotProfile product identity

`PlayerSlotProfile` is the stable identity and static presentation source for one
participation seat.

Representative fields:

```text
PlayerSlotId
Display Name
Accent Color
Icon
optional Description
optional Display Order
optional static presentation metadata
```

The Profile may be used immediately after Slot reservation/join for:

```text
pointer presentation
character-selection UI
lobby panels
Player labels
viewport decoration
device prompts
diagnostics
```

A Player does not need an `ActorProfile` merely to receive this Slot presentation.

Logical identity remains:

```text
PlayerSlotProfile.PlayerSlotId
```

The following are presentation only:

```text
color
icon
display name
array position
Unity playerIndex
```

## Runtime allocation state

Slot allocation state is mutable Session runtime state and must not be stored in
`PlayerSlotProfile`.

Initial allocation states:

```text
Unavailable
Available
Reserved
Joined
Leaving
```

Meaning:

```text
Unavailable
  not currently eligible under product/context policy

Available
  eligible for a new join

Reserved
  atomically held by one pending join operation

Joined
  bound to an admitted local participant

Leaving
  explicit release/leave is in progress
```

Actor selection, Activity readiness and occupancy are separate facts. They must not be
encoded by pretending the Slot is Available.

A joined Slot without an `ActorProfile` remains `Joined`.

## Atomic reservation

Reservation occurs before `PlayerInputManager.JoinPlayer(...)`.

This prevents two requests in the same frame or call chain from selecting the same Slot.

Canonical transition:

```text
Available
-> Reserved
-> Joined
```

On provisioning or admission failure:

```text
Reserved
-> Available
```

The release must be explicit and diagnostic.

No Slot remains silently stranded in `Reserved`.

## Eligibility

A Slot is eligible for default allocation only when:

```text
it exists in the configured ordered array
its Profile reference is valid
its runtime state is Available
it is allowed by current Session/context policy
dynamic capacity permits another join
no pending join already owns it
```

Technical `PlayerInputManager.maxPlayerCount` remains a separate hard ceiling.

## Character-selection relationship

Slot allocation and Actor selection are separate.

```text
Join
-> reserve/bind PlayerSlotProfile
-> Slot presentation becomes available
-> ActorProfile may be selected later
```

In simultaneous character selection:

```text
Player 1 Slot: Joined, selecting Actor
Player 2 Slot: Joined, selecting Actor
Player 3 Slot: Joined, selecting Actor
```

None of these Slots is available for another join.

Rules such as:

```text
allow duplicate Actors
unique Actor selection
limited copies
team restrictions
```

are Actor-selection policies and are outside Slot allocation.

## Single-player

Single-player uses the same ordered configuration.

```text
Local Player Slots[]
  [0] Player 1
```

The first local join receives that Slot.

No separate single-player Slot architecture is introduced.

## Guardrails

```text
Do not require the ordinary local join request to choose a Slot.

Do not equate PlayerInput.playerIndex with PlayerSlotId.

Do not allocate by color, icon, display name or GameObject name.

Do not allow two pending joins to reserve the same Slot.

Do not treat a Joined Slot without ActorProfile as Available.

Do not mutate PlayerSlotProfile to store runtime allocation state.

Do not silently skip an invalid configured Slot Profile.

Do not add random, spatial, team-based, account-bound or MMO Slot allocation
policies without a real product requirement.
```

## Out of scope

This ADR does not decide:

```text
explicit Slot targeting
account-bound Slots
online/network Slot assignment
persistent user-to-Slot ownership
team-based allocation
spatial/proximity allocation
Actor-selection uniqueness
leave/reconnect state machines
```

Those features may add explicit policies later without changing the default ordered flow.

## Technical acceptance criteria

```text
Game/Application configuration references an ordered PlayerSlotProfile array.

Default join reserves the first eligible Available Slot by array order.

Reservation exists before PlayerInputManager.JoinPlayer is called.

Failed provisioning releases the reservation explicitly.

Joined Slots are not reused while Actor selection is pending.

playerIndex and presentation metadata never become Slot identity.

Runtime allocation state does not mutate Profile assets.
```

## Product acceptance criteria

```text
A designer can order local Player Slots explicitly.

A joined participant immediately has a name, color and icon before Actor selection.

Press Start to Join receives the next configured empty Slot.

Simultaneous character-selection pointers can use Slot presentation consistently.

Single-player uses the same configuration with one Slot.

Advanced/Debug shows:
configured index,
PlayerSlotProfile,
PlayerSlotId,
allocation state,
pending operation,
PlayerInput/playerIndex,
selected ActorProfile when present.
```

## Consequences

### Positive

```text
Default local join is deterministic and simple.

Join callers do not need product Slot knowledge.

Slot presentation is available before character selection.

Single-player, lobby and Press Start flows share one model.

Atomic reservation prevents duplicate allocation.
```

### Cost

```text
The framework needs explicit Slot runtime allocation state.

The configured array order becomes meaningful product authoring.

Invalid or duplicate Slot Profile references require validation.

Explicit Slot targeting remains a later extension.
```

## Suggested commit message

```text
Docs: define ordered local Player Slot allocation
```
