# 10 — Player Identity and Typed Binding Decisions

Status: **current decisions only**. The superseded F53C0 audit and sequence are archived in [History/070](../History/070-Player-Binding-and-Composer-History.md).

- `PlayerSlotDeclaration` is slot identity.
- `PlayerActorDeclaration` is actor identity.
- `PlayerInput` is an explicit typed reference where consumed.
- `GameObject.name` is diagnostic text, not authority or object lookup.
- Input action-map/action names are permitted only when validated against the assigned `InputActionAsset`.
- Identities from slot, actor, reset and object-entry domains are related evidence, not interchangeable keys.
- `PlayerComposer` is the current Player authoring surface.
- `CameraComposer` is the current main gameplay-camera authoring surface.
- `PlayerViewBehaviour` remains passive evidence.

The only active next lane is [P2 — Player Control Product](01-Roadmap.md).
