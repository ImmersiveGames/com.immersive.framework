# IF-ADR-013 — Startup Activity BGM Lifecycle Reconciliation — 2026-08-24

Status: **CURRENT / CLOSED — IMPLEMENTED + PLAY MODE CERTIFIED**  
Related ADR: `IF-ADR-013 — Optional Audio BGM Adapter`  
Scope: Route/Activity BGM authoring independence, Startup Activity ordering, persistent Director completion wiring, current Audio QA

## Problem

The earlier BGM authoring model allowed `FrameworkRouteBgmBinding` to hold an explicit reference to the Startup Activity's `FrameworkActivityBgmBinding`.

That solved ordering by making Route authoring know about Activity authoring:

```text
Route Enter
  -> defer Route BGM
  -> Route invokes referenced Startup Activity BGM binding
```

This was an ownership leak. A Route BGM binding should describe Route intent only, while an Activity BGM binding should describe Activity intent only.

The cross-reference was removed, but the first lifecycle cut exposed two regressions.

## Regression 1 — Startup Activity with no content

Minimal Game reproduced:

```text
Route
  PlayOwn = BGM_Floresta

Startup Activity
  ActivityContentProfile = null
  no FrameworkActivityBgmBinding
  activityContentHandles = 0
```

Observed before correction:

```text
BGM intent refresh deferred for Startup Activity
Activity committed / Ready
no later FrameworkBgmDirector operation
confirmedBgm = <none>
```

The Route intent remained pending indefinitely.

The problem was not zero iteration over Activity BGM bindings. The lifecycle completion was not reaching the persistent BGM authority in this topology.

## Regression 2 — completion receiver scope

A subsequent cut emitted Activity entry completion through `ActivityContentRuntime`, resolving completion receivers only in explicit Route/Activity content scope.

That still failed because `FrameworkBgmDirector` is a **Persistent Content authority** and therefore does not belong to Route/Activity content discovery.

The mismatch was:

```text
Activity lifecycle completion
  -> Route/Activity receiver discovery

FrameworkBgmDirector
  -> Persistent Content
  -> outside that discovery scope
```

## Final authority correction

The final correction keeps the lifecycle fact and persistent authority explicitly connected.

Owner of wiring:

```text
FrameworkRuntimeHost
```

Current flow:

```text
GlobalUiSceneRuntime / persistent roots
  -> FrameworkRuntimeHost
  -> GameFlowRuntime
  -> RouteLifecycleRuntime
  -> ActivityFlowRuntime
  -> explicitly attached IActivityContentEntryCompletionReceiver
  -> FrameworkBgmDirector
```

`ActivityFlowRuntime` now owns deterministic entry-completion emission and sends it once after the Activity entry transition completes.

`ActivityContentRuntime` no longer performs completion fan-out by Route/Activity discovery.

## Current authoring contract

```text
FrameworkRouteBgmBinding
  -> Route intent only

FrameworkActivityBgmBinding
  -> Activity intent only
```

Removed from active code/product surface:

```text
startupActivityBgmBinding
StartupActivityBgmBinding
TryApplyStartupActivityBgm
```

There is no Route -> Activity BGM authoring reference.

## Startup Activity resolution contract

```text
Route Enter
  -> publish Route intent
  -> if Startup Activity ordering requires it, keep Route intent pending

Activity Enter
  -> Activity publishes its own BGM intent if authored

Activity entry completion
  -> persistent FrameworkBgmDirector receives completion
```

Resolution:

```text
Activity intent published
  -> Activity intent wins
  -> pending Route cue must not transiently play first

no Activity intent published
  -> evaluate pending Route intent

no pending explicit intent
  -> preserve sticky confirmed presentation
```

The completion is required even when:

```text
ActivityContentProfile = null
activityContentHandles = 0
no Activity BGM binding
```

## Constraints preserved

The correction does not introduce:

- Route -> Activity authoring references;
- `FindObjectOfType` / `FindObjectsOfType`;
- global scene discovery;
- singleton or service locator;
- global registry;
- reflection;
- coroutine/frame delay;
- timeout/polling;
- silent fallback;
- parallel Audio authority.

`com.immersive.audio` remains the physical provider only.

## Play Mode evidence — Minimal Game

Observed current sequence:

```text
Persistent Content Activity entry completion receivers attached.
receiverCount='1'

FrameworkBgmDirector
requestedBgm='BGM_Floresta'
reason='BGM intent refresh deferred for Startup Activity.'

Activity entry completes

FrameworkBgmDirector
operation='Apply'
outcome='Applied'
requestedBgm='BGM_Floresta'
confirmedBgm='BGM_Floresta'
reason='Succeeded'
```

Boot diagnostics also show:

```text
activityContentHandles='0'
activityReadiness='Ready'
blockingIssues='0'
```

Verdict:

```text
Route PlayOwn + content-less Startup Activity
  PASS
```

## Play Mode evidence — Player Provisioning

Current consumer composition:

```text
Route
  no FrameworkRouteBgmBinding

Activity
  FrameworkActivityBgmBinding
  BGM = BGM_Antiguidade
```

Observed:

```text
FrameworkBgmDirector
operation='Apply'
outcome='Applied'
requestedBgm='BGM_Antiguidade'
confirmedBgm='BGM_Antiguidade'
reason='Succeeded'
```

Verdict:

```text
Activity BGM without Route BGM binding
  PASS
```

This proves that Route and Activity BGM authoring are operationally independent in a real consumer sample.

## Current Audio QA certification

Final Play Mode aggregate:

```text
Core Audio         7/7 PASS
Framework BGM     28/28 PASS
ADR-013A            5/5 PASS
Audio continuity    4/4 PASS
TOTAL              44/44 PASS
FAILED               0
```

Focused Startup Activity isolation:

```text
startup-activity-neutral-baseline
  PASS
  confirmed=<null>
  provider stopped

startup-route-is-deferred
  PASS
  RouteCue remains pending
  provider has no RouteCue presentation

startup-activity-prevents-route-transient-play
  PASS
  ActivityCue Applied
  provider plays ActivityCue directly
```

The earlier false FAIL occurred because the synthetic case inherited `ActivityCue` as already confirmed. The QA was corrected by isolating this scenario to a neutral baseline rather than weakening the expected outcome to `NoChange`.

## Historical evidence handling

The 2026-08-19 BGM-CONTINUITY-1 `30/30` record remains historical evidence for the boundary executed on that date.

It is not rewritten or relabeled as proof of the later Route/Activity authoring decoupling and Activity-entry-completion wiring.

The current certification for the present contract is `44/44 PASS`.

## Final disposition

```text
Route BGM authoring independence           CLOSED
Activity BGM authoring independence        CLOSED
Route -> Startup Activity BGM reference    REMOVED
Startup Activity ordering                  LIFECYCLE-OWNED
content-less Startup Activity              PASS
persistent Director completion wiring      EXPLICIT / PASS
transient Route playback before Activity   PREVENTED / PASS
current Audio QA                           44/44 PASS
com.immersive.audio physical provider      UNCHANGED
```

No additional BGM runtime redesign is required by the evidence in this cut.
