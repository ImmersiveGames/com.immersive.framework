# Pause PlayerInput Authoring

Status: Current  
Last updated: 2026-07-26

## Product surface

The official single-player physical Pause path is authored on the admitted
Local Player Host:

```text
PlayerInput
LocalPlayerHostAuthoring
PausePlayerInputBinding
UnityPlayerInputGateAdapter
```

`PausePlayerInputBinding` is the designer-facing composition surface.
`UnityPlayerInputGateAdapter` is the materialized physical writer adapter.

## References

```text
Pause Action
  InputActionReference
  resolved by action GUID

Global Action Map
  derived from Pause Action.actionMap
  not separately typed by the designer

Gameplay Action Map
  PlayerInputActionMapReference
  stores InputActionAsset + Action Map GUID
```

Runtime never falls back to Action Map names. The selected Gameplay map is
resolved by GUID against the exact `PlayerInput.actions` instance, including
PlayerInput-owned action-asset copies.

A cached map name exists only for Inspector display and diagnostics.

## Authoring flow

1. Add `PausePlayerInputBinding` to the same GameObject as `PlayerInput`.
2. Assign the exact `PlayerInput`.
3. Assign the Pause `InputActionReference`.
4. Select the Gameplay Action Map from the typed popup.
5. Press **Apply / Rebuild**.

Apply / Rebuild:

```text
creates one missing UnityPlayerInputGateAdapter
reuses one compatible adapter
copies the same typed Gameplay map identity
rejects duplicates
rejects a different PlayerInput target
validates the complete composition
```

The button remains in the primary authoring flow. Technical commands and verbose
runtime evidence live in the collapsed `Advanced / Debug` foldout.

## Legacy migration

Older serialized components may contain:

```text
globalActionMapName
gameplayActionMapName
```

Those exact serialized field names are retained, hidden, as migration evidence.
This preserves existing prefabs and editor setup utilities.

`OnValidate` materializes the typed Gameplay map reference from the exact legacy
name when the GUID reference is still empty. After that materialization, runtime
resolution uses only the GUID-backed reference.

The Global map is always derived from the assigned Pause Action.

## Failure behavior

The composition fails explicitly when:

```text
PlayerInput or actions are missing
Pause Action is missing
Pause Action GUID is absent from PlayerInput.actions
Gameplay Action Map reference is missing or invalid
Gameplay map GUID is absent from PlayerInput.actions
Global and Gameplay resolve to the same map
Gate Adapter is missing, duplicated or targets another PlayerInput
Pause binding and Gate Adapter use different Gameplay map GUIDs
```

No runtime map-name fallback, hierarchy search, singleton or service locator is
introduced.
