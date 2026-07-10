# P3A — Player Product Simplification

## Objective

Reduce `PlayerComposer` to the canonical player product surface before rebuilding FIRSTGAME from an empty gameplay scene.

## Type

Technical + UX/product.

## Package changes

### Modified

- `Runtime/PlayerAuthoring/PlayerComposer.cs`
- `Runtime/PlayerAuthoring/PlayerRecipe.cs`
- `Editor/PlayerAuthoring/PlayerComposerApplyRebuildUtility.cs`
- `Editor/PlayerAuthoring/PlayerComposerEditor.cs`

### Created

- `Documentation~/Product/P3A-PLAYER-PRODUCT-SIMPLIFICATION-MANIFEST.md`

### Removed

None in this package delta. Obsolete primitive types remain available temporarily for QA migration, but `PlayerComposer` no longer materializes them.

## Canonical product surface

`PlayerComposer` now authors:

- semantic Actor identity;
- semantic Player Slot identity;
- typed `PlayerInput`;
- Gate participation;
- typed camera anchors;
- explicit LookAt policy;
- optional Reset integration.

Movement remains game-owned. Camera binding remains owned by `CameraComposer`.

## Materialization

Apply/Rebuild creates or repairs only:

- `PlayerActorDeclaration`;
- `PlayerSlotDeclaration`;
- `UnityPlayerInputGateAdapter`, when enabled;
- `UnityResetSubjectAdapter`, when enabled;
- `UnityTransformResetParticipant`, when selected;
- `_Framework` organization root;
- `Anchors/CameraTarget`;
- `Anchors/LookAtTarget`, when explicit LookAt is selected.

Apply/Rebuild removes legacy materialization found under the Player:

- `PlayerControlBindingTargetBehaviour`;
- `UnityPlayerInputBridgeTargetBehaviour`;
- `UnityPlayerInputActivationTargetBehaviour`;
- `PlayerSlotOccupancy`;
- passive Entry/View/Control evidence;
- Player-owned `FrameworkCameraAnchorHost`;
- empty `_Framework/_Bindings`.

## Input decision

The effective gameplay map is derived from:

```text
PlayerInput.defaultActionMap
```

The designer no longer authors a free gameplay-map string in `PlayerComposer` or `PlayerRecipe`.

## Camera decision

`CameraTarget` has no silent fallback to the Player root.

`LookAtTarget` is either:

- an explicit Transform; or
- explicitly configured to reuse the Follow target.

`CameraComposer` must reference `PlayerComposer` and materialize the actual Cinemachine binding.

## Reset decision

Reset scope is an enum in the product surface. The runtime adapter receives the corresponding enum name during materialization.

## QA expected

- Apply creates the minimal shape.
- Second Apply is idempotent.
- Missing required PlayerInput fails explicitly.
- Empty or invalid `PlayerInput.defaultActionMap` fails explicitly.
- Missing required camera anchors fail explicitly when automatic anchor creation is disabled.
- Legacy bridge/activation/control target components are not materialized.
- Existing legacy components below the Player are removed by Apply/Rebuild.
- Gate receives typed `PlayerInput` and `PlayerSlotDeclaration`.
- Optional Reset remains functional.

## Out of scope

- gameplay movement;
- player spawning;
- multiplayer join;
- CameraComposer implementation changes;
- FIRSTGAME scene creation;
- deleting legacy primitive types before QA migration.

## Suggested commit

```text
P3A — simplify PlayerComposer materialization and typed bindings
```
