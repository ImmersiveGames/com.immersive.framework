# Getting Started

This guide creates the smallest navigable Framework application. It does not require a Player; Route and Activity navigation can be authoritative before Player admission.

## Requirements and installation

Use the Unity and dependency versions listed in the [package README](../../README.md). Install `com.immersive.framework` through the documented package source, then let Unity import the package before authoring assets.

## Create the application

1. Create a `GameApplicationAsset` from the Immersive Framework asset menu.
2. Create one or more `RouteAsset` and `ActivityAsset` assets.
3. Set the application’s `Startup Route` to the first `RouteAsset`.
4. Configure the Route’s primary scene and, if the Route should open an Activity automatically, its `Startup Activity`.
5. Create a Persistent Content Scene from `File > New Scene > Immersive Persistent Content`.
6. Save the resulting scene as a game-owned `.unity` scene and assign it to `GameApplicationAsset > Persistent Content > Content Scene`.
7. Use the Game Application Inspector action to add or enable that scene in the active Build Profile Scene List.
8. Use the owning Inspector’s validation action to validate the application and authored content.

The Persistent Content template is a starting composition. It does not create, save, assign or add a consumer scene to the Build Profile automatically.

## Add navigation

Use the application’s Startup Route for the initial Route request. To navigate later, add a `RouteRequestTrigger` or `ActivityRequestTrigger` to an active scene and connect its public request method to a UnityEvent or UI Button. A Route transition uses the destination Route’s authored Startup Activity. An Activity request changes or clears the current Activity within the current Route.

A Route does not require an admitted Player. If an Activity requires Player readiness and that evidence is unavailable, navigation can still commit while the Activity reports `NotReady` and its gameplay gates remain closed.

See [Game Flow](Game-Flow.md) for the ownership model and runtime behavior.

## Add gameplay features

After the navigation path works, add only the domains the game needs:

- [Player Participation](Player-Usage.md) for a scene-authored Local Player or the experimental manager-provisioned workflow;
- [Camera](Camera-Usage.md) for Session Camera Outputs and Assignments;
- [Input and Pause](Pause-Usage.md) for a Player Pause action or authored Pause UI.

These features have their own required composition and validation. A minimal application does not prove optional Player, Camera or Pause authoring is complete.

## Samples

This package has no consumer `Samples~` directory. FIRSTGAME and QAFramework provide separate consumer and technical evidence; neither is an installed sample contract. Use package implementation and linked architecture decisions for supported contracts.

## Related

- [Framework Usage](Framework-Usage.md) — cross-cutting authoring principles.
- [Persistent Content Scene Template](Persistent-Content-Scene-Template.md)
- [Public API Reference](../API/Public-API.md)
