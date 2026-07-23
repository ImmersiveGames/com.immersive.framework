# IF-ADR-003 — Player Participation and Actor Lifecycle

Status: Accepted
Last updated: 2026-07-23
Supersedes: Player F45/F49 notes, P3 plans/manifests and product ADRs 0007–0018
Superseded by: none

## Context

Local Player provisioning, participation Slot identity, Actor selection,
logical Actor lifetime, Activity readiness and scene-owned Players are related
but distinct authorities. Earlier documentation repeated these decisions across
dozens of plans, audits and implementation manifests.

## Decision

The canonical model separates:

```text
PlayerSlotProfile / PlayerSlotId
  stable participation seat

Local Player Host
  Unity Input System host with PlayerInput and LocalPlayerHostAuthoring

ActorProfile / ActorProfileId
  immutable selectable Actor identity

Logical Actor Host / ActorId
  contextual runtime Actor instance

Activity participation intent
  projected Slots plus progressive readiness requirement
```

`GameApplicationAsset` owns the ordered `PlayerSlotProfile[]` capacity and the
Actor duplicate-selection policy. Allocation is first available by configured
order. `PlayerInput.playerIndex` is diagnostic evidence, never Slot identity.

`PlayerParticipationRuntimeContext` is session-scoped. It owns allocation,
reservation, joined state and selected `ActorProfile` per Slot. Join and Actor
selection are separate transactions. Selection targets `PlayerSlotId`, supports
revision checks, obeys the explicit duplicate policy and does not create an
`ActorId` or materialize an Actor.

Runtime-created local Players use one path:

```text
explicit join request
-> reserve ordered Slot
-> PlayerInputManager manual join
-> validate Local Player Host
-> bind typed PlayerSlotId
-> commit or explicit rollback
```

`PlayerInputManager` is the technical provisioner and uses manual join. Required
provisioning is referenced explicitly from the `UIGlobal` composition surface;
there is no scene-wide discovery fallback.

An `ActivityAsset` owns its participation configuration inline:

```text
Projection: NoSlots | AllJoinedSlots | ExplicitSlots
Zero-participant policy
Ordered explicit PlayerSlotProfile references when applicable
Requirement: None | JoinedSlots | SelectedActors |
             LogicalActorsPrepared | GameplayReady
```

Projection selects Slots; requirement defines progressive evidence. They are
not reusable Profile assets. Invalid combinations fail validation.

`SceneLocalPlayerAdmissionAuthoring` admits a scene-existing host and logical
Actor into the same participation domain with
`ExternalSceneOwned` physical ownership. The framework owns contextual
admission/release evidence but does not instantiate, destroy or silently
deactivate the external host or Actor.

Activity-scoped Player release occurs in reverse dependency order: gameplay,
camera/input occupancy, Actor preparation/adoption, host admission and Slot.
Failures retain typed evidence for explicit retry; no silent rollback or
fallback Slot is allowed.

## Accepted scope

- Ordered Slot allocation and session-persistent Actor selection.
- Manual local Player provisioning through `PlayerInputManager`.
- Scene-owned local Player admission with explicit physical ownership.
- Activity-owned participation projection and requirement level.
- Contextual Actor preparation, gameplay admission, camera/input eligibility
  and reverse-order release.
- Optional Activity-owned Pause intent for one explicitly eligible local Player.

## Rejected scope

- Slot or Actor identity inferred from names, paths or Unity player index.
- Pre-authored Slot identity on a Player prefab.
- Automatic join, fallback Slot or first-discovered Actor selection.
- Runtime mutation of Profile assets.
- A second Player runtime lane or compatibility facade.
- Multiplayer Pause policy, networking, teams and role quotas in the current cut.

## Consequences

Join can complete before character selection. Selection can persist across
Route/Activity changes while logical Actors remain contextual. Activities can
gate readiness without owning session state. Provisioned and scene-owned Players
share participation semantics without sharing physical creation/destruction.

## Current implementation coverage

The P3 lane, inline Activity participation configuration, Actor selection,
provisioned Player path, scene-local admission/adoption and gameplay admission
contexts exist in runtime source. The former passive F49/F51/F52 topology and
alternative pre-authored Player surface are absent.

`PauseActivityBindingAuthoring` and its session-owned runtime handoff support
the initial single-local-player Activity intent. QA/FIRSTGAME coverage for this
specific Activity Pause surface is not recorded as complete.

## Pending decisions

- Product policy for more than one eligible local Player in Activity Pause.
- Network/remote participation and reconnect semantics.
- Explicit Actor replacement transaction after logical Actor preparation.
