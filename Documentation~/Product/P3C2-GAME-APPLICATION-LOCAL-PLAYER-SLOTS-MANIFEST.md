# P3C.2 — Game/Application Ordered Local Player Slots

Status: implementation cut; Unity compile/import and QA smoke pending.

Package: `com.immersive.framework`

Depends on: `P3C.1 — Player Slot and Participation Requirement Profiles`

## Objective

Integrate ordered `PlayerSlotProfile` authoring into the existing `GameApplicationAsset` product surface without creating a parallel Player settings authority.

## Scope

- Add serialized `PlayerSlotProfile[] localPlayerSlots` to `GameApplicationAsset`.
- Expose ordered read-only runtime access and explicit indexed lookup.
- Add a designer-first reorderable list to the Game Application Inspector.
- State visibly that array order is the canonical local allocation order.
- Validate empty configuration, null entries, repeated Profile references, invalid identities and duplicate `PlayerSlotId` values.
- Merge Player participation findings into the Game Application Inspector validation report.

## Files altered

```text
Runtime/Authoring/GameApplicationAsset.cs
Editor/Authoring/GameApplicationAssetEditor.cs
```

## Files created

```text
Editor/PlayerParticipation/PlayerParticipationAuthoringValidator.cs
Editor/PlayerParticipation/PlayerParticipationAuthoringValidator.cs.meta
Editor/PlayerParticipation.meta
Documentation~/Product/P3C2-GAME-APPLICATION-LOCAL-PLAYER-SLOTS-MANIFEST.md
Documentation~/Product/P3C2-GAME-APPLICATION-LOCAL-PLAYER-SLOTS-MANIFEST.md.meta
```

## Product surface

```text
Game Application Inspector
  -> Local Player Participation
      -> Local Player Slots — Allocation Order
          Slot 1: PlayerSlotProfile
          Slot 2: PlayerSlotProfile
          ...
```

The list itself is the allocation authority:

```text
First Available By Configured Order
```

`PlayerSlotProfile.DisplayOrder` remains presentation metadata and cannot override the configured array order.

## Expected use

```text
Designer creates Player Slot Profiles
-> opens the active Game Application
-> adds Profiles in desired join order
-> reorders the list when product allocation order changes
-> Inspector validation reports invalid or duplicate configuration immediately
```

## Validation behavior

Blocking findings:

```text
no configured Local Player Slots
null Profile reference
same Profile referenced twice
invalid/empty PlayerSlotId
same PlayerSlotId owned by two configured Profiles
```

Valid configuration reports:

```text
configured Slot count
allocationPolicy='FirstAvailableByConfiguredOrder'
```

The validator reads immutable Profile references only. It does not mutate assets or represent runtime Slot state.

## Out of scope

```text
project-wide scan of unreferenced Profile assets
custom PlayerSlotProfile Inspector
official Slot template assets
ActorProfile defaults
runtime capacity or reservation
PlayerInputManager integration
Activity participation projection
central project-settings validator integration
QAFramework and FIRSTGAME changes
```

Central validator integration remains a later P3C microcut so this cut does not modify the large legacy validator while the new product surface is still being established.

## Technical smoke expected

```text
GameApplicationAsset serializes ordered PlayerSlotProfile references
reordering persists after save/reload
LocalPlayerSlots accessor preserves serialized order
DisplayOrder does not change allocation order
empty array reports Error
null entry reports Error
repeated Profile reports Error
duplicate PlayerSlotId reports Error
valid Profiles report Info
Profile assets remain unchanged in Play Mode
```

## Technical acceptance

```text
runtime assembly has no Editor dependency
GameApplication remains the only application-level authoring authority
no fallback Player Slot is created
no PlayerSlotId string is duplicated in GameApplication authoring
configuration access is read-only to runtime consumers
```

## Product acceptance

```text
designer can see and change local join order directly
allocation semantics are explained beside the list
missing configuration is visible and blocking
invalid configured positions identify their exact array index
```

## Suggested commit

```text
P3C.2 — add ordered Local Player Slots to Game Application
```
