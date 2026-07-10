# 070 — Player Binding and Composer History

Status: **Historical / superseded by PlayerComposer and CameraComposer product cuts.**

F49 established passive Player contracts and diagnostics. F50 added authoring validation. F51 proved explicit PlayerView/camera target/activation adapters. F52 proved explicit PlayerControl/PlayerInput bridge and activation adapters. F53 proved consumer wiring and audited typed identity.

Those cuts remain valid technical evidence, but their historical sequence is not the current product workflow and is not the active roadmap.

## Preserved decisions

- `PlayerSlotDeclaration` owns slot identity.
- `PlayerActorDeclaration` owns actor identity.
- `PlayerInput`, camera targets and cameras use typed references.
- `GameObject.name` is diagnostic only.
- Input action names require validation against `InputActionAsset`.
- Passive contracts do not imply runtime authority.

## Superseded guidance

- proving PlayerView/camera before creating a product authoring surface;
- `CanonicalPlayerBindingAuthoring` as the planned facade;
- F53C1/F53C2/F53C3/F53D as the active sequence;
- legacy camera anchors/director/setup helpers as the current camera model;
- PlayerView Binding Adapter as the next mandatory lane.

Current replacements:

```text
PlayerRecipe -> PlayerComposer -> Validate -> Apply/Rebuild
PlayerComposer -> CameraComposer -> Validate -> Apply/Rebuild
Next lane: P2 — Player Control Product
```

