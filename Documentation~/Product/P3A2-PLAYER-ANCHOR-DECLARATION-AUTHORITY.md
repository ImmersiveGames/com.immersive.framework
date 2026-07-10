# P3A.2 — Player Anchor Generation and Declaration Authority

## Decisions

### Camera anchors

When camera binding is required and anchor references are empty:

- `Apply/Rebuild` may create `Anchors/CameraTarget`;
- `Apply/Rebuild` may create `Anchors/LookAtTarget`;
- the objects are children of the logical Player object;
- creation occurs only when `Create Anchors If Missing` is enabled;
- validation fails before side effects only when a required reference is absent and automatic creation is disabled;
- materialization verifies the resulting effective references and reports an explicit blocking issue if generation fails.

Camera binding itself remains owned by `CameraComposer`. `PlayerComposer` only authors the actor anchors.

### Player declarations

`PlayerActorDeclaration` and `PlayerSlotDeclaration` remain runtime `MonoBehaviour` components because other runtime adapters consume typed scene-instance declarations.

They are not independent authoring authorities.

Canonical authority:

```text
PlayerRecipe defaults
  -> PlayerComposer effective instance intent
    -> Apply/Rebuild
      -> PlayerActorDeclaration
      -> PlayerSlotDeclaration
```

Their custom inspectors are read-only when viewed directly and route the user back to `PlayerComposer`.

## Modified

- `Runtime/PlayerAuthoring/PlayerComposer.cs`
- `Editor/PlayerAuthoring/PlayerComposerApplyRebuildUtility.cs`
- `Editor/PlayerAuthoring/PlayerComposerEditor.cs`

## Created

- `Editor/PlayerAuthoring/PlayerMaterializedDeclarationEditors.cs`

## Suggested commit

```text
P3A.2 — generate player camera anchors and lock declaration authoring
```
