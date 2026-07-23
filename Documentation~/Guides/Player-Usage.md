# Player Usage

Status: Current
Last updated: 2026-07-23

## Configure participation

1. Create `PlayerSlotProfile` assets for stable local participation seats.
2. Optionally assign each Slot a default `ActorProfile`.
3. Add the Profiles to `GameApplicationAsset` in allocation order.
4. Choose the explicit duplicate Actor-selection policy.
5. On each `ActivityAsset`, configure Projection, zero-participant behavior and
   Requirement Level.

Slot order is product configuration. Unity player index, hierarchy order and
join callback order are not Slot identity.

## Runtime-provisioned local Player

In `UIGlobal`, configure one `LocalPlayerProvisioningAuthoring` with an explicit
manual-join `PlayerInputManager`, then reference it through
`LocalPlayerProvisioningHostRegistration`.

The Player prefab contains:

```text
PlayerInput
LocalPlayerHostAuthoring
empty Actor Mount
```

Do not pre-author a `PlayerSlotId`. The official join request reserves a Slot,
calls `PlayerInputManager.JoinPlayer`, validates the host and commits or rolls
back the reservation.

## Scene-owned local Player

Use `SceneLocalPlayerAdmissionAuthoring` when the Activity scene already owns
the local Player Host and logical Actor.

1. Reference the exact host, Slot Profile, Actor Profile and logical Actor evidence.
2. Validate/Apply the authoring surface.
3. Use the Activity lifecycle timing defined by the component.
4. Keep physical ownership `ExternalSceneOwned`.

The framework admits and releases contextual evidence. It does not instantiate,
destroy or silently deactivate these scene-owned objects.

## Actor selection and readiness

Actor selection occurs after join and targets `PlayerSlotId`. A joined Slot may
remain without a selection. Activity requirements progress through:

```text
None
JoinedSlots
SelectedActors
LogicalActorsPrepared
GameplayReady
```

Changing selection after logical Actor preparation requires a future explicit
replacement transaction and is rejected by the current model.

## Pause and Camera integration

Add `PausePlayerInputBinding` to the same GameObject as the relevant
`PlayerInput`. Add `PlayerGameplayCameraAuthoring` to the admitted Player Actor
when gameplay camera publication is required. Both become eligible through the
canonical Player/Activity lifecycle; neither is a parallel Player authority.

`PauseActivityBindingAuthoring` declares that an Activity requires Pause for its
officially admitted local Player. The current policy supports one eligible local
Player and rejects ambiguity.

## Diagnose

Inspect Slot allocation/reservation, selected Actor Profile, preparation,
occupancy, input eligibility, camera eligibility and admission as separate
evidence. Never infer one layer from another.

## Manual validation

1. Compile Framework and QAFramework.
2. Run focused Player join, selection, preparation, gameplay admission and
   scene-admission suites.
3. Confirm failed joins release reservations.
4. Confirm Activity exit releases in reverse dependency order.
5. Confirm scene-owned host/Actor objects survive successful release.
6. Validate the same official flow in FIRSTGAME before a product release.
