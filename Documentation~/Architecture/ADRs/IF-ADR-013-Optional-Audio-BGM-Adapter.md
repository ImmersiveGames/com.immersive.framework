# IF-ADR-013 — Optional Audio BGM Adapter

Status: **Accepted / Experimental — technical boundary certified; consumer gate proven**  
Last updated: **2026-08-24**  
Package implementation: **Implemented — IF-ADR-013A + BGM-CONTINUITY-1 + BGM-ROUTE-POLICY-1 + Startup Activity lifecycle completion**  
Technical QA: **Certified — Audio QA 44/44**  
FIRSTGAME / Samples: **Proven — Game Flow contextual BGM + Minimal Game Route BGM + Player Provisioning Activity BGM**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-006, IF-ADR-008, IF-ADR-010, IF-ADR-014  
External provider currently certified: `com.immersive.audio`

> Current mutable implementation and QA status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. Historical BGM-CONTINUITY-1 certification remains in
> `../Reconciliation/IF-ADR-013-BGM-Continuity-Technical-Certification-2026-08-19.md`.
> The current Startup Activity lifecycle correction is recorded in
> `../Reconciliation/IF-ADR-013-Startup-Activity-BGM-Lifecycle-Reconciliation-2026-08-24.md`.

## Context

BGM integration is optional and may depend on an external audio package. The Framework needs a narrow adapter boundary without making physical audio playback part of Framework Core, Route identity, Activity identity or generic lifecycle ownership.

The Framework owns **Route/Activity BGM intent and provider-confirmed presentation evidence**. The external audio package owns **physical playback and transition execution**.

The current certified provider is `com.immersive.audio`. Concrete provider types remain isolated inside the optional `Immersive.Framework.Audio` integration assembly. Framework Core must remain valid when the optional Audio package is absent.

## Decision

The Framework exposes optional Route/Activity BGM intent through:

- `FrameworkBgmDirector`;
- `FrameworkRouteBgmBinding`;
- `FrameworkActivityBgmBinding`;
- `FrameworkBgmRoutePolicy`;
- `FrameworkBgmActivityPolicy`;
- `FrameworkBgmOperationResult`.

BGM intent has three semantic states:

```text
No Request / Unspecified  -> Preserve confirmed presentation
Play(cue)                 -> Apply / transition to cue
Silence                    -> Explicitly release to silence
```

The confirmed presentation is sticky. Activity exit, Route exit, missing BGM declaration, content release or scene lifetime changes do not by themselves request Stop or restore an older cue.

Only an explicit Play or Silence intent may mutate the provider presentation.

## Route and Activity authoring independence

Route and Activity authoring are independent authorities.

```text
FrameworkRouteBgmBinding
  owns only Route BGM intent

FrameworkActivityBgmBinding
  owns only the intent of its Activity
```

There is **no Route -> Activity BGM binding reference** in the current contract.

Removed from the public/current implementation:

```text
startupActivityBgmBinding
StartupActivityBgmBinding
TryApplyStartupActivityBgm
```

A Route does not discover, reference or invoke an Activity BGM authoring component.

Canonical authoring rule:

```text
Want Route music?
  -> author FrameworkRouteBgmBinding

Want Activity-specific music?
  -> author FrameworkActivityBgmBinding for that Activity

Want Activity to inherit Route intent?
  -> use the Activity policy

Want no new intent?
  -> omit the binding or choose a No Request policy where applicable
```

A `FrameworkActivityBgmBinding` does not require a `FrameworkRouteBgmBinding`. An Activity may publish its own BGM when the Route has no BGM binding at all.

## Sticky confirmed presentation — BGM-CONTINUITY-1

Normative behavior:

```text
No request
  -> FrameworkBgmOperation.Preserve
  -> FrameworkBgmOperationOutcome.NoChange
  -> no provider mutation
  -> confirmed presentation unchanged

Play(same confirmed cue)
  -> NoChange
  -> no unnecessary provider restart

Play(different cue)
  -> request provider Play
  -> provider success: Applied + commit new confirmed cue
  -> provider failure: Rejected + preserve previous confirmed presentation

Silence
  -> request provider Stop
  -> provider success: Released + confirmed explicit silence
  -> provider failure: Rejected + preserve previous confirmed presentation

Owner exit
  -> clear owner intent/evidence only
  -> preserve confirmed presentation
```

Confirmed explicit silence is also sticky. After Silence is provider-confirmed, later owner exit or No Request operations preserve silence until a later Play succeeds.

## Route policy — BGM-ROUTE-POLICY-1

Route BGM is an explicit intent, not an optional cue shorthand:

| Policy | Published intent |
|---|---|
| `PlayOwn` | Play the required Route cue. |
| `PreserveCurrent` | No Request; preserve the confirmed presentation. |
| `Silence` | Explicit Silence. |

`FrameworkBgmDirector` retains the complete current Route intent. `CurrentRouteBgm` is therefore only the cue carried by a `PlayOwn` intent; `PreserveCurrent` and `Silence` do not fabricate a cue.

## Activity policy

Current policies:

| Policy | Published intent |
|---|---|
| `UseOwnOrRoute` | Play Activity cue when authored; otherwise inherit the complete current Route intent. |
| `UseOwnOrPreserveCurrent` | Play Activity cue when authored; otherwise No Request. |
| `UseRoute` | Inherit the complete current Route intent. |
| `Silence` | Explicit Silence. |

A Route `Silence` is inherited as Silence. A Route `PreserveCurrent` is inherited as No Request by `UseRoute` and cue-less `UseOwnOrRoute`.

The former restoration model is not current behavior. Owner exit never automatically restores Route BGM or another prior presentation.

## Startup Activity resolution

When a Route has a Startup Activity, Route refresh may be deferred until the Activity entry reaches a deterministic lifecycle completion point.

The current flow is:

```text
Route Enter
  -> FrameworkRouteBgmBinding publishes Route intent
  -> Startup Activity exists
  -> Route intent is retained as pending

Startup Activity Enter
  -> FrameworkActivityBgmBinding publishes Activity intent if authored

Activity entry completes
  -> ActivityFlowRuntime emits one typed entry-completion notification
  -> explicitly attached persistent completion receivers are notified
  -> FrameworkBgmDirector closes Startup Activity BGM resolution
```

Resolution rule:

```text
Activity published BGM intent during entry
  -> keep/apply Activity intent
  -> pending Route intent must not transiently play first

Activity published no BGM intent
  -> evaluate the pending Route intent

No pending explicit intent
  -> preserve sticky confirmed presentation
```

This completion is a lifecycle fact, not an authoring lookup. It must work even when the Startup Activity has:

```text
ActivityContentProfile = null
activityContentHandles = 0
no FrameworkActivityBgmBinding
```

That case is valid and, with a Route `PlayOwn`, resolves to the Route cue after Activity entry completion.

## Persistent completion wiring

`FrameworkBgmDirector` is a Persistent Content authority. It is not owned by Route or Activity content.

Therefore Activity entry completion is delivered through explicit runtime wiring rather than Route/Activity scene discovery:

```text
GlobalUiSceneRuntime / Persistent roots
  -> FrameworkRuntimeHost
  -> GameFlowRuntime
  -> RouteLifecycleRuntime
  -> ActivityFlowRuntime
  -> explicitly attached IActivityContentEntryCompletionReceiver
  -> FrameworkBgmDirector
```

`ActivityFlowRuntime` owns deterministic completion emission. Route/Activity content discovery is not used to find the persistent Director.

The existing BGM consumer injection path remains responsible for attaching the explicitly composed Director to Route/Activity BGM bindings. It is not used as a reverse discovery mechanism for lifecycle completion.

## Provider-confirmed execution evidence — IF-ADR-013A

Authored/requested state and provider-confirmed state are distinct.

Current outcomes:

```text
Applied
  provider confirmed Play

Released
  provider confirmed Stop / Silence

NoChange
  no provider mutation required

OptionalAuthorityUnavailable
  optional provider authority is absent; core lifecycle remains valid

Rejected
  provider operation failed; previous confirmed presentation remains Framework truth
```

Rejected Play/Release remains retryable because rejected intent does not overwrite the confirmed presentation.

## Architectural constraints

- Framework Core works when the Audio package/adapter is absent.
- Framework Core/Runtime does not reference concrete audio package types.
- Concrete provider types stay inside the optional audio integration assembly.
- `Applied` and `Released` require provider-confirmed execution.
- Failed/rejected provider operations do not mutate confirmed Framework BGM state.
- `No Request` never means Stop, Silence, automatic fallback or restoration.
- Activity exit and Route exit do not physically stop confirmed BGM merely because ownership ended.
- Route and Activity BGM authoring remain independent.
- No Route -> Activity BGM authoring reference is permitted.
- No singleton, service locator, global AudioManager, static BGM authority or hidden bootstrap is introduced.
- No `FindObjectOfType`, global scene scan, reflection, polling, timeout, coroutine or arbitrary frame delay is used to close Startup Activity BGM ordering.
- Persistent BGM continuity requires explicit composition of `FrameworkBgmDirector` and `AudioRuntimeHost` under a lifetime that survives transient Route/Activity scenes.
- Framework Persistent Content is the canonical Framework-owned composition surface for that session/application lifetime.

## Accepted integration model

```text
Framework Persistent Content / Session lifetime
  FrameworkBgmDirector
  AudioRuntimeHost
        ↑
        │ explicit consumer injection
        │
Transient Route / Activity content
  FrameworkRouteBgmBinding
  FrameworkActivityBgmBinding
        ↓
explicit Play / Silence / No Request intent
        ↓
FrameworkBgmDirector
        ↓
No Request? -> Preserve confirmed presentation / no provider call
        ↓ otherwise
com.immersive.audio provider
        ↓
provider result
        ↓
framework-side typed execution evidence
        ↓
confirmed sticky presentation
```

Startup Activity completion is an additional explicit runtime path into the persistent Director; it does not create another audio authority.

## Product surface

No BGM Recipe, Profile, Composer, Wizard, global manager or generic Apply/Rebuild workflow is required for the accepted boundary.

Normal authoring remains:

```text
Persistent Content
  AudioRuntimeHost
  FrameworkBgmDirector

Route content, when Route intent is needed
  FrameworkRouteBgmBinding

Activity content, when Activity intent is needed
  FrameworkActivityBgmBinding
```

No Startup Activity BGM reference appears in the Route Inspector.

## Historical certification — BGM-CONTINUITY-1 — 2026-08-19

The 2026-08-19 certification remains valid dated evidence for the boundary it executed:

```text
Core Audio         7/7 PASS
Framework BGM     14/14 PASS
ADR-013A            5/5 PASS
Audio continuity    4/4 PASS
TOTAL              30/30 PASS
FAILED               0
```

It must not be relabeled as proof of the later Startup Activity lifecycle/wiring cut.

## Current certification — Startup Activity lifecycle cut — 2026-08-24

The current Audio QA run proves the present contract:

```text
Core Audio         7/7 PASS
Framework BGM     28/28 PASS
ADR-013A            5/5 PASS
Audio continuity    4/4 PASS
TOTAL              44/44 PASS
FAILED               0
```

The focused Startup Activity isolation cases prove:

```text
startup-activity-neutral-baseline
  -> confirmed=<null>
  -> provider stopped

startup-route-is-deferred
  -> RouteCue retained pending
  -> no provider RouteCue presentation

startup-activity-prevents-route-transient-play
  -> ActivityCue Applied
  -> provider plays ActivityCue directly
  -> no transient RouteCue presentation
```

The current lifecycle path also proves Route `PlayOwn` with a content-less Startup Activity:

```text
Route = PlayOwn / RouteMusic
Startup Activity:
  ActivityContentProfile = null
  no Activity BGM binding
  activityContentHandles = 0

Activity entry completion
  -> pending Route intent applied
  -> RouteMusic confirmed
```

## Consumer evidence

### Game Flow

Game Flow remains the primary contextual BGM demonstration. It proves lifecycle changes among Play, No Request/Preserve and explicit Silence across Route/Activity transitions.

### Getting Started / Minimal Game

Minimal Game now also proves the simplest Route-owned BGM composition:

```text
Route BGM = BGM_Floresta / PlayOwn
Startup Activity publishes no Activity BGM intent
activityContentHandles = 0
entry completion resolves pending Route intent
BGM_Floresta -> Applied / confirmed
```

Audio is ambient/supporting there, not the primary lesson.

### Player / Provisioning

Player Provisioning proves Activity-owned BGM without a Route BGM binding:

```text
Route BGM binding = absent
Activity BGM = BGM_Antiguidade
Activity enter -> BGM_Antiguidade Applied / confirmed
```

This is consumer evidence that `FrameworkActivityBgmBinding` is independently useful and does not require a Route BGM binding.

## Experimental promotion

The technical runtime, current QA and real-consumer gates are closed. ADR-013 remains `Experimental` until a separate explicit product-maturity promotion cut updates supported API status consistently.

Experimental status is maturity governance, not an unresolved BGM defect.

## Current disposition

```text
Architecture: Accepted
Package: Implemented
Route/Activity authoring: independent
Startup Activity ordering: lifecycle-completion driven
Persistent completion wiring: explicit via FrameworkRuntimeHost -> ActivityFlowRuntime
QA: Certified — Audio QA 44/44
Consumer evidence: Game Flow + Minimal Game + Player Provisioning
Status: Accepted / Experimental
Next: optional explicit product-maturity promotion decision
```

## Normative summary

```text
Keep Audio optional and outside Framework Core physical-playback authority.
Keep the concrete provider behind the optional bridge.
Compose FrameworkBgmDirector + AudioRuntimeHost explicitly under Persistent Content.
Treat Route and Activity BGM bindings as independent lifecycle intent publishers.
Never require or reintroduce Route -> Startup Activity BGM authoring references.
Defer Route intent when Startup Activity ordering requires it.
Close Startup Activity ordering through deterministic ActivityFlowRuntime entry completion.
Deliver completion to the persistent Director through explicit runtime wiring.
If Activity publishes intent, Activity intent wins without transient Route playback.
If Activity publishes no intent, evaluate the pending Route intent.
No Request means Preserve; it never means Stop or automatic restoration.
Owner exit preserves confirmed presentation.
Same confirmed cue is NoChange and must not restart provider playback.
Applied and Released require provider-confirmed execution.
Rejected provider operations preserve previous confirmed presentation and remain retryable.
Explicit Silence is the only normal lifecycle intent that releases BGM to silence.
Current Audio QA certification is 44/44 PASS.
API maturity remains Experimental until a separate explicit promotion cut changes it.
```
