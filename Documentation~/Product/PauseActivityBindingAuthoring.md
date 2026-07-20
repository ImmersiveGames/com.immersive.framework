# Pause Activity Binding Authoring

`PauseActivityBindingAuthoring` is the Activity-owned product declaration that
the Activity requires product Pause binding for its officially admitted Local
Player.

## Placement

Place exactly one component in the authored content belonging to an Activity.
The component does not reference a Player prefab, `PlayerInput`,
`LocalPlayerHostAuthoring`, `PausePlayerInputBinding`, slot, scene path, or
runtime host.

An Activity without this component explicitly has no Pause binding intent.
More than one declaration in the roots supplied by Activity composition is a
blocking configuration error. Inactive declarations are included because this
is serialized Activity intent, not enable-state-driven behavior.

## What it declares

The current surface declares one thing only:

```text
This Activity requires product Pause binding for its admitted Local Player.
```

The first policy is deliberately single-local-player. A future runtime must
fail explicitly when the Activity exposes more than one eligible Local Player;
it must not select one implicitly.

## What it does not execute

This cut does not resolve a Player or host, register a
`PausePlayerInputBinding`, issue a token, subscribe to input, switch action
maps, alter time scale, or create/request Pause runtime. It also performs no
global discovery.

`TryCreateIntent` and `PauseActivityBindingAuthoringValidator` produce passive,
immutable evidence only. The caller supplies explicit declarations or Activity
roots and decides when to log the resulting diagnostic.

## Intent versus runtime binding

`PauseActivityBindingIntent` is authored intent. The session-owned
`PauseProductBindingRuntimeContext` remains the runtime authority. The future
Activity-scoped registration will consume typed admitted-host evidence and this
intent; it will not infer a host from scene hierarchy or use a singleton.

## Next cuts

P2.1B will define the typed handoff from Activity Player admission to the Pause
binding materializer. P2.1C will register/release the exact binding token in
the ordered Activity lifecycle. Neither behavior exists in P2.1A.
