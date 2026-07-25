# Player Usage

Status: Current
Last updated: 2026-07-25

## Configure participation

1. Create `PlayerSlotProfile` assets for stable local participation seats.
2. Optionally assign each Slot a default `ActorProfile`.
3. Add the Profiles to `GameApplicationAsset` in allocation order.
4. Choose the explicit duplicate Actor-selection policy.
5. Configure each Activity participation requirement.

Slot order is product configuration. Unity player index, hierarchy order and
join callback order are not Slot identity.

## Runtime-provisioned local Player

In Persistent Content, configure one `LocalPlayerProvisioningAuthoring` with an
explicit manual-join `PlayerInputManager`, then reference it through
`LocalPlayerProvisioningHostRegistration`.

The Player prefab contains:

```text
PlayerInput
LocalPlayerHostAuthoring
empty Actor Mount
```

Do not pre-author a `PlayerSlotId`. Runtime admission associates the official
host with its logical Slot.

## Scene-owned local Player

Use `SceneLocalPlayerAdmissionAuthoring` when the Activity scene already owns
the local Player Host and logical Actor. The framework admits and releases
contextual evidence without instantiating, destroying or silently deactivating
the scene-owned objects.

## Pause integration

Physical Pause input belongs to the official Player:

```text
PlayerInput
UnityPlayerInputGateAdapter
PausePlayerInputBinding
```

`Global` is an action map of that PlayerInput, not a second global Player.

`PauseRequestTrigger` is not a Player component. It may live in Persistent
Content, Route scenes or Activity scenes and receives its request port from the
corresponding composition lifecycle.

Authored buttons can apply application-only Pause without an active Player
binding. In that mode the framework changes logical Pause, TimeScale and
presentation but does not modify action maps.

Therefore:

```text
Escape / Gamepad Start
  requires official Player binding

Pause / Resume / Toggle Button
  does not require a Player
  requires a composed PauseRequestTrigger
```

See [Pause Usage](Pause-Usage.md).

## Diagnose

Inspect Slot allocation, admission, Actor selection, logical Actor preparation,
input eligibility and camera eligibility as separate evidence.

For Pause, distinguish:

```text
PauseRequestTrigger.ProductRequestBindingStatus
PauseRequestTrigger.LastProductStatus
PauseRequestTrigger.LastExecutionMode
PausePlayerInputBinding.BindingStatus
```

A bound Trigger does not imply that a Player binding exists; it may legitimately
execute in `ApplicationOnly` mode.
