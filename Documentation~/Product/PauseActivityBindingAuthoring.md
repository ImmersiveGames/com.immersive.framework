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

## Runtime binding and lifecycle integration

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
Activity entry. `ActivityFlowRuntime` supplies its canonical `RuntimeContentOwner`
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

`FrameworkRuntimeHost` owns one session-scoped
`PauseActivityBindingRuntimeContext` and its narrow
`PauseActivityBindingRuntimeHostModule`. The module receives the existing
`PauseProductBindingRuntimeContext` only through `IPauseProductBindingPort`;
it does not create another Pause authority.

The official order is:

```text
Activity scene composition
-> explicit-root intent resolution
-> official Player admission/materialization
-> typed admitted Local Player Host evidence
-> Pause binding activation
-> Activity readiness
```

The Activity transaction supplies its canonical transition sequence. On exit
or committed-transition compensation, Pause binding release occurs before the
Player participant can release its host/actor and before Activity scenes are
unloaded. A failed release is blocking: the participant exit, scope
finalization and scene release do not continue, and retained P2.1B evidence
allows a later release retry. Absence remains a valid no-op.

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

## Limits

QA authoring/vertical flow was not created and FIRSTGAME was not validated.
Multiplayer Pause is unsupported. `PauseProductBindingSceneLifecycleParticipant`
remains available for physically scene-local bindings; this Activity path does
not use it to discover a session-scoped player host.
