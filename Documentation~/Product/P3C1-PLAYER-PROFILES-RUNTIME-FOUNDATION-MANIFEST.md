# P3C.1 — Player Profiles Runtime Foundation

Status: implementation cut; Unity compile/import validation pending.

## Objective

Introduce the immutable runtime-facing Profile assets and progressive requirement vocabulary required by P3C without changing Game/Application authoring, PlayerComposer or runtime participation state.

## Created

```text
Runtime/PlayerParticipation/Contracts/PlayerParticipationRequirementLevel.cs
Runtime/PlayerParticipation/Authoring/PlayerSlotProfile.cs
Runtime/PlayerParticipation/Authoring/PlayerParticipationRequirementsProfile.cs
```

Associated Unity `.meta` files and new folder metadata are included.

## Product surface

```text
Create > Immersive Framework > Player > Player Slot Profile
Create > Immersive Framework > Player > Participation Requirements Profile
```

## Decisions preserved

```text
PlayerSlotProfile owns the canonical serialized PlayerSlotId.
Profile assets contain immutable product identity/policy only.
Runtime allocation, join, selection, occupancy and readiness do not mutate Profiles.
Requirement levels are progressive:
  None
  JoinedSlots
  SelectedActors
  LogicalActorsPrepared
  GameplayReady
Null is not an implicit None policy.
Configured Game/Application array order, not DisplayOrder, will control allocation.
```

## Deliberately not changed

```text
PlayerRecipe
PlayerComposer
Game/Application authoring
Activity authoring
runtime Slot state
join provisioning
Actor selection/materialization
Editor inspectors and duplicate-ID validators
QAFramework
FIRSTGAME
```

`PlayerRecipe` and `PlayerComposer` still contain historical string identity fields. P3C.1 does not migrate or remove them because this cut establishes Profile contracts only. Their reconciliation must occur through an explicit later product migration rather than accidental dual authority.

## Expected validation

```text
package compiles in Unity 6.5
both assets appear in the Create menu
valid PlayerSlotId resolves to the existing typed PlayerSlotId
empty/invalid PlayerSlotId fails explicitly
requirement level is always one defined progressive enum value
runtime code has no Editor dependency
Profiles expose no mutable runtime-state API
```

## Next cut

P3C.2 must audit and extend the existing Game/Application asset with the ordered `PlayerSlotProfile[]` configuration and explicit initial capacity policy. It must not create a parallel Player Settings authority.

## Suggested commit

```text
P3C.1 — add Player Slot and participation requirement Profiles
```
