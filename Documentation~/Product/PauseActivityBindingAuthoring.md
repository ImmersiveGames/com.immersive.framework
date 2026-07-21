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

## Runtime binding in P2.1B

`PauseActivityBindingRuntimeContext` is the package-owned runtime handoff for
this intent. It is an internal, non-MonoBehaviour authority that receives all
runtime evidence explicitly:

```text
PauseActivityBindingScope
+ PauseActivityBindingIntentResolution
+ IReadOnlyList<LocalPlayerHostAuthoring>
+ IPauseProductBindingPort
```

`PauseActivityBindingScope` is the Pause-specific projection of one concrete
Activity entry. The future lifecycle supplies its canonical `RuntimeContentOwner`
and Activity transition sequence; the Pause runtime does not generate a counter
or own admission. This keeps the scope valid through the active Activity until
its ordered exit, rather than retaining the temporary admission-stage receipt.
Its owner and entry sequence distinguish an entry from a stale re-entry.
The runtime accepts exactly one explicitly supplied, Joined Local Player Host
with a valid Player Slot and `PlayerInput`. It requires exactly one
`PausePlayerInputBinding` co-located on that host GameObject and verifies that
both components reference the same `PlayerInput`.

There is no host discovery or implicit player selection. Empty intent is a
valid no-op; multiple hosts, duplicate evidence, foreign scopes and stale
entry sequences are rejected explicitly. Registration and release retain their
evidence transactionally: a failed registration leaves the binding reusable,
and a failed release retains the token and port for retry.

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
`PauseProductBindingRuntimeContext` remains the Pause runtime authority through
`IPauseProductBindingPort`. The activity-scoped runtime only registers/releases
the exact binding; it does not create a second Pause runtime or use a singleton.

## Next cuts

P2.1C must still connect this reusable runtime to the ordered Activity
lifecycle. `FrameworkRuntimeHost` does not call it yet, Activity lifecycle is
not connected, and QA does not use the feature. No vertical Pause P1 flow is
complete in this cut.
