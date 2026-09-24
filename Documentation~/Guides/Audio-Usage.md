# Audio BGM Usage

Status: Current / Experimental  
Last updated: 2026-08-24

## Dependency and boundary

Install a compatible `com.immersive.audio` package when BGM integration is required. The optional `Immersive.Framework.Audio` assembly isolates the provider dependency; Framework Core remains independent from Audio.

The Framework owns Route/Activity BGM intent and provider-confirmed presentation evidence. `com.immersive.audio` owns physical playback and transitions.

## Canonical composition

For BGM that must survive transient Route/Activity scenes, compose the physical and intent authorities under Framework Persistent Content:

```text
Persistent Content / Session lifetime
└─ Audio Runtime
   ├─ AudioRuntimeHost
   └─ FrameworkBgmDirector
          ↑ injected into loaded Route/Activity BGM consumers

Route content, when Route intent is needed
└─ RouteBgmAuthoring

Activity content, when Activity intent is needed
└─ ActivityBgmAuthoring
```

`FrameworkBgmDirector` and `AudioRuntimeHost` do not make themselves persistent. Persistence is composition-owned.

Do not serialize cross-scene references from transient bindings to the persistent Director.

## Authoring

1. In Framework Persistent Content, configure an `AudioRuntimeHost` with an explicit `AudioDefaultsAsset`.
2. Add `FrameworkBgmDirector` and assign the `AudioRuntimeHost` explicitly.
3. If the Route owns a BGM intent, add `RouteBgmAuthoring` to Route-owned content and choose a Route policy.
4. If an Activity owns a BGM intent, add `ActivityBgmAuthoring` to Activity-owned content, assign the Activity, and choose an Activity policy.
5. Do not create Route -> Activity BGM references. They are not part of the current product surface.

Canonical mental model:

```text
Want one/default BGM for a Route?
  -> Route BGM Binding

Want a particular Activity to change BGM?
  -> Activity BGM Binding on that Activity

Want an Activity to inherit Route intent?
  -> UseRoute or UseOwnOrRoute

Want no new intent?
  -> omit the binding or use a No Request policy where applicable
```

`ActivityBgmAuthoring` works without a `RouteBgmAuthoring`.

## Route policy

| Policy | Published intent |
|---|---|
| `PlayOwn` | Play the required Route cue. |
| `PreserveCurrent` | No Request; preserve confirmed presentation. |
| `Silence` | Explicitly request silence/stop. |

The policy is the complete Route intent. Cue presence alone is not the policy.

## Activity policies

| Policy | Published intent |
|---|---|
| `UseOwnOrRoute` | Play Activity cue when authored; otherwise inherit the complete Route intent. |
| `UseOwnOrPreserveCurrent` | Play Activity cue when authored; otherwise No Request. |
| `UseRoute` | Inherit the complete Route intent. |
| `Silence` | Explicitly request silence/stop. |

Owner exit never automatically restores an older Route or Activity cue.

## Sticky confirmed presentation

```text
No Request / Unspecified  -> Preserve confirmed presentation
Play(cue)                 -> Apply / transition to cue
Silence                    -> Explicitly release to silence
```

Required invariants:

```text
No request        -> Preserve / NoChange
Same cue          -> NoChange; no provider restart
Different cue     -> provider-controlled transition
Explicit Silence  -> provider-controlled transition to silence
Owner exit        -> Preserve / NoChange
```

A confirmed presentation survives Activity exit, Route exit and transient scene lifetime changes until another explicit Play or Silence succeeds.

Absence of a binding is not Silence.

## Startup Activity behavior

When a Route has a Startup Activity, the Route BGM intent may be retained as pending until the Activity entry finishes.

Current flow:

```text
Route Enter
  -> Route binding publishes intent
  -> Startup Activity exists
  -> Route intent may be deferred

Activity Enter
  -> Activity binding publishes its own intent if one exists

Activity Entry Completion
  -> ActivityFlowRuntime emits deterministic completion
  -> persistent FrameworkBgmDirector receives completion through explicit runtime wiring
```

Resolution:

```text
Activity published BGM intent
  -> Activity intent wins
  -> no transient Route playback

Activity published no BGM intent
  -> pending Route intent is evaluated

No pending explicit intent
  -> preserve confirmed presentation
```

This works even when the Startup Activity has no `ActivityContentProfile`, no Activity BGM binding and `activityContentHandles = 0`.

Do **not** add a fake Activity BGM binding just to make Route BGM play.

## Why there is no Startup Activity BGM field on Route

The old product surface required a Route to reference a Startup Activity BGM binding. That created cross-authoring ownership and existed only to compensate for ordering.

Current ownership is simpler:

```text
Route binding     -> Route intent only
Activity binding  -> Activity intent only
ActivityFlow      -> entry completion/order
Director          -> resolves pending intent + confirmed presentation
```

Ordering is therefore solved by lifecycle, not by a Route Inspector reference.

## Persistent completion wiring

`FrameworkBgmDirector` belongs to Persistent Content, not Route/Activity content.

The current completion path is explicitly wired:

```text
GlobalUiSceneRuntime / persistent roots
  -> FrameworkRuntimeHost
  -> GameFlowRuntime
  -> RouteLifecycleRuntime
  -> ActivityFlowRuntime
  -> FrameworkBgmDirector completion receiver
```

This path does not use global scene search, `FindObjectOfType`, singleton/service locator, reflection, polling or frame delay.

The normal scene-consumer injection path remains responsible for injecting the persistent Director into Route/Activity BGM bindings.

## Common configurations

### One BGM for the whole Route

```text
Route
  RouteBgmAuthoring
    Policy = PlayOwn
    Route BGM = RouteMusic

Activities
  no Activity BGM binding unless they need to override/inherit explicitly
```

A content-less Startup Activity does not block Route BGM. The pending Route intent is applied after Activity entry completion.

### Activity-specific BGM

```text
Route
  Route BGM binding optional

Activity Combat
  ActivityBgmAuthoring
    Activity BGM = CombatMusic
```

If `CombatMusic` is published during Startup Activity entry, it is applied directly without first presenting a deferred Route cue.

### Activity BGM with no Route BGM binding

```text
Route
  no RouteBgmAuthoring

Activity
  ActivityBgmAuthoring
    Activity BGM = ActivityMusic
```

This is valid. Activity intent is independent from Route BGM authoring.

### Inherit Route

```text
Route
  Policy = PlayOwn / RouteMusic

Activity
  Policy = UseRoute
```

Result: `RouteMusic`.

## Route and Activity exit

`ClearRouteBgm` and `ClearActivityBgm` clear owner intent/evidence only. They do not physically stop confirmed BGM.

This is the central BGM-CONTINUITY-1 distinction:

> owner lifetime is not playback-presentation lifetime.

## Diagnostics

Inspect:

- `FrameworkBgmDirector.ConfirmedBgm`;
- `FrameworkBgmDirector.ConfirmedExplicitSilence`;
- current Route policy / Route cue where applicable;
- Route and Activity authored policies;
- `LastOperationResult`;
- requested cue / requested silence;
- previous confirmed cue;
- resulting confirmed cue/silence;
- operation and outcome.

Framework outcomes:

```text
Applied
Released
NoChange
OptionalAuthorityUnavailable
Rejected
```

A missing optional `AudioRuntimeHost` produces `OptionalAuthorityUnavailable`; do not solve it with scene search or a global AudioManager.

## Current certification

Current canonical Audio QA result after the Startup Activity lifecycle cut:

```text
Core Audio         7/7 PASS
Framework BGM     28/28 PASS
ADR-013A            5/5 PASS
Audio continuity    4/4 PASS
TOTAL              44/44 PASS
FAILED               0
```

Focused Startup Activity proof:

```text
startup-activity-neutral-baseline
  PASS

startup-route-is-deferred
  PASS
  provider remains without RouteCue presentation

startup-activity-prevents-route-transient-play
  PASS
  ActivityCue Applied directly
```

The earlier 2026-08-19 `30/30` certification remains historical evidence for BGM-CONTINUITY-1 and must not be relabeled as proof of this later lifecycle cut.

## Consumer examples currently proven

```text
Getting Started / Minimal Game
  Route PlayOwn / BGM_Floresta
  Startup Activity has no Activity BGM
  activityContentHandles = 0
  completion -> BGM_Floresta Applied

Player / Provisioning
  no Route BGM binding
  Activity BGM = BGM_Antiguidade
  Activity enter -> BGM_Antiguidade Applied

Game Flow
  contextual Route/Activity Play
  No Request / Preserve
  owner-exit preservation
  explicit Silence
```

Audio may be ambient/supporting in samples where it is natural. Optional dependency boundaries must remain explicit.

## Experimental status

ADR-013 remains `Experimental` as API maturity governance. Runtime behavior, current QA and consumer integration are closed for the present accepted contract.

## Validation checklist

- Persistent `AudioRuntimeHost` + `FrameworkBgmDirector` are explicitly composed.
- Route and Activity BGM bindings are authored independently.
- No Route -> Activity BGM binding reference exists.
- Startup Activity ordering is closed by lifecycle completion.
- A Startup Activity with zero content handles still allows pending Route intent to resolve.
- Activity own BGM prevents transient Route playback.
- Activity BGM works without a Route BGM binding.
- No Request is never translated to Silence.
- Owner exit does not call physical Stop.
- Same confirmed cue does not restart provider playback.
- Different cue transitions through the provider.
- Explicit Silence is the normal lifecycle intent that stops BGM.
- Rejected provider operations preserve previous confirmed state.
- Run canonical Audio QA from Framework bootstrap/QA Hub.
