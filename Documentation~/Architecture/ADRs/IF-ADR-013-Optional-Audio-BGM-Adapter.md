# IF-ADR-013 — Optional Audio BGM Adapter

Status: **Accepted / Experimental — technical boundary certified**  
Last updated: **2026-08-19**  
Package implementation: **Implemented — IF-ADR-013A + BGM-CONTINUITY-1**  
Technical QA: **Certified — Audio QA 30/30**  
FIRSTGAME: **Not Proven — real consumer integration remains the promotion gate**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-006, IF-ADR-008, IF-ADR-010, IF-ADR-014  
External provider currently certified: `com.immersive.audio`

> Current mutable implementation, QA and FIRSTGAME status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. Current technical certification is recorded in
> `../Reconciliation/IF-ADR-013-BGM-Continuity-Technical-Certification-2026-08-19.md`.

## Context

BGM integration is optional and may depend on an external audio package. The Framework needs a narrow adapter boundary without making audio playback authority part of Framework Core, Route identity, Activity identity, or global lifecycle authority.

The current external provider is `com.immersive.audio`. Concrete provider types remain isolated inside the optional `Immersive.Framework.Audio` assembly. Framework Core must continue to operate without the audio package installed.

## Decision

The Framework exposes optional Route/Activity BGM intent through:

- `FrameworkBgmDirector`;
- `FrameworkRouteBgmBinding`;
- `FrameworkActivityBgmBinding`;
- `FrameworkBgmActivityPolicy`;
- `FrameworkBgmOperationResult`.

The Framework owns **BGM intent and provider-confirmed presentation evidence**. The audio package owns **physical playback and transition execution**.

BGM intent has three semantic states:

```text
No Request / Unspecified  -> Preserve confirmed presentation
Play(cue)                 -> Apply/transition to cue
Silence                    -> Explicitly release to silence
```

The confirmed presentation is sticky. Activity exit, Route exit, missing BGM declaration, or scene lifetime changes do not by themselves request Stop or restore an older cue.

Only an explicit Play or Silence intent may mutate the provider presentation.

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

Confirmed explicit silence is also sticky. After Silence is provider-confirmed, later owner exit or no-request operations preserve silence until a later Play succeeds.

## Activity policy

Current policies:

| Policy | Published intent |
|---|---|
| `UseOwnOrRoute` | Play Activity cue when authored; otherwise Play Route cue when authored; otherwise No Request. |
| `UseOwnOrPreserveCurrent` | Play Activity cue when authored; otherwise No Request. |
| `UseRoute` | Play Route cue when authored; otherwise No Request. |
| `Silence` | Explicit Silence. |

The former `UseOwnOrRetainActivityUntilRouteExit` restoration model is not the current contract. A retained Activity cue may exist as diagnostic/confirmed evidence, but owner exit does not automatically restore Route BGM or another prior presentation.

## Provider-confirmed execution evidence — IF-ADR-013A

Authored/requested state and provider-confirmed state are distinct.

`Applied` and `Released` mean the provider confirmed the requested physical operation. A request must not become confirmed state merely because Framework configuration was valid or dispatch occurred.

Current outcomes:

```text
Applied
  provider confirmed Play

Released
  provider confirmed Stop/Silence

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
- `No Request` never means Stop, Silence, Clear, automatic fallback, or restoration.
- Activity exit and Route exit never physically stop confirmed BGM merely because ownership ended.
- Stable authored Route/Activity identity is not BGM playback ownership.
- No singleton, service locator, global AudioManager, static BGM authority, or hidden bootstrap is introduced.
- Runtime scene injection may attach one explicitly composed `FrameworkBgmDirector` to loaded `IFrameworkBgmDirectorConsumer` instances, but injection does not create persistence or global authority.
- Persistent BGM continuity requires explicit composition of `FrameworkBgmDirector` and `AudioRuntimeHost` under a lifetime that survives transient Route/Activity scenes.
- Framework Persistent Content is the canonical Framework-owned composition surface for that session/application lifetime.

## Accepted integration model

```text
Framework Persistent Content / Session lifetime
  FrameworkBgmDirector
  AudioRuntimeHost
        ↑
        │ scene injection to consumers
        │
Transient Route / Activity scene
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

The director does not make itself persistent. `AudioRuntimeHost` does not make itself persistent. Persistence is composition-owned.

## Startup Activity behavior

When a Route has a Startup Activity, Route BGM refresh is deferred so an explicit Startup Activity BGM binding can publish its intent before the pending Route intent is evaluated.

If a valid `StartupActivityBgmBinding` is present, the Startup Activity intent is applied immediately.

If no valid explicit Startup Activity BGM binding is assigned, the current runtime warns and evaluates the pending Route intent. A Route with `routeBgm = null` therefore still publishes No Request. An Activity using `UseOwnOrPreserveCurrent` with no cue also publishes No Request.

This allows a Route transition to have a valid Startup Activity/readiness contract while remaining BGM-neutral. The warning is diagnostic/product-surface debt and must not be “fixed” by inventing a cue or Silence intent.

## Identity authority compatibility — IF-ADR-014

This adapter does not introduce a parallel identity model for Route or Activity.

Authored Route/Activity authority remains the exact typed `RouteAsset` / `ActivityAsset` definition governed by IF-ADR-014; `RouteId` / `ActivityId` remain stable boundary projections only.

Audio cue identity, pending BGM intent, confirmed BGM presentation, and provider execution evidence remain audio-integration concerns. They do not become Route/Activity definition equality, lifecycle ownership, occurrence identity, or release authority.

## Product surface

No BGM Recipe, Profile, Composer, Wizard, global manager, or generic Apply/Rebuild workflow is required for the accepted boundary.

Normal authoring remains:

```text
persistent AudioRuntimeHost + FrameworkBgmDirector
Route BGM intent
Activity BGM intent/policy
explicit optional Startup Activity BGM binding when needed
```

Advanced/debug surfaces may expose requested cue, requested Silence, previous confirmed cue, confirmed cue/silence, operation, outcome, and reason. Those diagnostics must not become the primary designer workflow.

## IF-ADR-013A technical closure — 2026-08-10

IF-ADR-013A established truthful provider-confirmed execution semantics:

- provider Play success -> `Applied`;
- provider Play rejection -> `Rejected`, previous confirmed state preserved, retry allowed;
- provider Stop success -> `Released`;
- provider Stop rejection -> `Rejected`, previous confirmed state preserved, retry allowed;
- same confirmed request -> `NoChange` without unnecessary mutation;
- optional authority absence -> `OptionalAuthorityUnavailable` without corrupting core lifecycle.

That cut was technically certified before BGM-CONTINUITY-1. Historical 26/26 evidence remains valid for the boundary it executed; the current aggregate is superseded by the 2026-08-19 30/30 certification below.

## BGM-CONTINUITY-1 technical closure — 2026-08-19

BGM-CONTINUITY-1 closes the owner-lifetime and physical-continuity gap without introducing a new audio authority.

Package implementation:

```text
ImmersiveGames/com.immersive.framework
1c422f7f22ec5d17a25e7caea8108eb5b0c08a4c
Audio Fix

ImmersiveGames/com.immersive.audio
Audiofix runtime cut
AudioBgmService provider-idempotence + controlled transitions
```

Framework behavior now proves:

- Route Play;
- same confirmed cue -> NoChange;
- Startup Activity Play;
- Activity-owned Play;
- Activity exit preserves confirmed BGM;
- Route exit preserves confirmed BGM;
- Route no-request preserves;
- Activity no-request preserves;
- `UseRoute` with no Route cue preserves;
- explicit Silence;
- owner exit/no-request preserve confirmed Silence;
- Play after confirmed Silence.

Provider behavior proves:

- same cue does not restart physical playback;
- different cue begins by fading the old cue rather than abruptly stopping it;
- cue transition completes to the requested cue;
- explicit Stop fades to silence and clears playback after fade completion.

Canonical Unity Play Mode verdict:

```text
Core Audio         7/7 PASS
Framework BGM     14/14 PASS
ADR-013A            5/5 PASS
Audio continuity    4/4 PASS
TOTAL              30/30 PASS
FAILED               0
```

Setup was executed twice consecutively with the same valid topology:

```text
sessionAuthorityScene='QA_UIGlobal'
sessionHostSource='QA_AudioValidatedHost'
sessionDefaults='Resolved'
routeBStartupActivity='RetainPreviousNoRequest'
routeBIntent='NoRequest'
```

A separate real Framework lifecycle proof then executed:

```text
QA Hub
  -> QA Framework BGM Route / QA_Audio
  -> Own Activity Active + Ready
  -> Route A exit / QA_Audio unload
  -> QA Framework BGM Route B / QA_AudioRouteB
  -> Startup Activity = QA Framework BGM Retain Previous Activity
  -> Activity Active + Ready
  -> blockingIssues=0
  -> BGM remained playing across A -> B
```

This proves continuity across real Framework Route/Activity and scene-lifetime changes with a persistent playback/intent authority and no new BGM request.

The QA real-lifecycle proof is technical integration evidence. It is not FIRSTGAME consumer-promotion evidence.

## Experimental promotion

Technical gates are closed:

```text
IF-ADR-013A provider-confirmed execution semantics     DONE
BGM-CONTINUITY-1 sticky intent/runtime implementation  DONE
QAFramework 30/30 technical certification              DONE
real Framework Route A -> B lifecycle continuity       DONE
FIRSTGAME / real consumer integration                  PENDING
```

ADR-013 remains `Experimental` only because the supported optional BGM boundary has not yet completed real consumer integration/usability proof in Sample/FIRSTGAME.

Experimental status is maturity governance, not an unresolved technical BGM defect.

## Current disposition

```text
Architecture: Accepted
Package: Implemented — IF-ADR-013A + BGM-CONTINUITY-1
QA: Certified — Audio QA 30/30
Real Framework lifecycle continuity: Certified in QA
FIRSTGAME: Not Proven
Status: Accepted / Experimental — technical boundary certified
Next: real consumer integration/usability proof in Sample/FIRSTGAME
```

## Normative summary

```text
Keep Audio optional and outside Framework Core authority.
Keep the concrete provider behind the optional bridge.
Compose the BGM director and physical audio host explicitly under the lifetime that must survive transient content.
Treat authored Route/Activity BGM as explicit intent, not physical ownership.
Distinguish No Request, Play and Silence.
No Request means Preserve; it never means Stop or automatic fallback.
Activity exit and Route exit preserve the confirmed presentation.
Same confirmed cue is NoChange and must not restart provider playback.
Applied and Released require provider-confirmed execution.
Rejected provider operations preserve the previous confirmed presentation and remain retryable.
Explicit Silence is the only normal lifecycle intent that releases BGM to silence.
BGM-CONTINUITY-1 is technically implemented and certified; FIRSTGAME remains the promotion gate.
```
