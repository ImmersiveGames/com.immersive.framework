# Input and Pause Usage

Status: **Stable single-player Pause/Input/Gate product surface. Multiplayer Pause policy is out of scope.**
Last updated: **2026-09-30**

## Choose a request path

The Framework supports two ways to request Pause:

1. **Player input** through `PlayerPauseInput` on the admitted Local Player Host.
2. **Authored UI or UnityEvent** through `PauseRequestTrigger`, which can request Pause, Resume or Toggle without Player input.

Both paths request logical Pause. The request trigger does not own Pause state.

## Player input setup

On the same Local Player Host GameObject, add:

- exactly one `UnityPlayerInputGateAdapter`;
- one `PlayerPauseInput`.

Configure the Gate Adapter with the exact `PlayerInput` and Gameplay Action Map. On `PlayerPauseInput`, assign the Pause `InputActionReference` (for example, the `Global/Pause` action). The Global map is derived from that action; do not configure a second map by name.

The binding belongs to the admitted Session Local Player Host lifetime. Activity scene changes do not release it while that Host remains current. Pause/Input/Gate apply as a transaction; an invalid or missing binding is reported and does not switch to an unrelated input map.

## Pause request trigger

Add a `PauseRequestTrigger` to a scene that lives as long as the UI/request needs it. Connect one of its public methods—request Pause, request Resume or toggle—to the UI Button/UnityEvent.

A trigger can be authored in Persistent Content, Route content or Activity content. SceneLifecycle injects its request port from the explicit roots for the Scene scope; Persistent Content uses the same participant under the Session scope and releases it at shutdown. Additive Route/Activity scenes detach when released and can bind again if composition is compensated. A UI request without Player input is an explicit Application-only request path; it does not create a Player or change action maps. See [IF-ADR-040](../Architecture/ADRs/IF-ADR-040-Scene-Composition-Binding-Model.md).

## Activity requirement

Add `ActivityPauseAuthoring` to an Activity’s authoring composition only when that Activity declares product Pause binding as a requirement for its admitted Local Player. Its `Requiredness` defaults to `Required`. This declaration does not create Pause runtime or bind input; Activity lifecycle resolves the declared requirement.

## Pause presentation

Choose the adapter matching the authored Canvas lifetime:

- `UnityPauseSurfaceAdapter` projects Pause state onto explicit Canvas/CanvasGroup references for an application-scoped Persistent Content surface.
- `UnityPauseResidentSurfaceAdapter` projects Pause state onto an already-authored resident surface (such as a global UI surface).

Both adapters only present a Pause snapshot. They do not own Pause state, input, Gate evaluation or `Time.timeScale`, and they do not create the UI. A resume button can call `PauseRequestTrigger.RequestResume`.

## Common mistakes

- Adding a second `PlayerInput` or Gate Adapter to the Local Player Host.
- Assigning a Gameplay Action Map by display name rather than the GUID-backed `PlayerInputActionMapReference`.
- Putting a short-lived trigger in an Activity scene when its UI must persist across Activity changes.
- Treating a Pause surface adapter as the owner of Pause or time scale.
- Assuming multiplayer Pause semantics are supported.
- Using internal runtime/binding result types as a gameplay API. Use the authoring components and their supported public methods.

## Public surfaces

- Authoring/request: `PlayerPauseInput`, `PauseRequestTrigger`, `ActivityPauseAuthoring`.
- Unity adapters: `UnityPlayerInputGateAdapter`, `UnityPauseSurfaceAdapter`, `UnityPauseResidentSurfaceAdapter`.
- Stable contracts: `IPauseSurfaceAdapter` and the Pause request/result observation types listed in the [Public API Reference](../API/Public-API.md#input-and-pause).

## Related architecture

- [IF-ADR-005 — Input, Pause, Gate and Reset](../Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md)
