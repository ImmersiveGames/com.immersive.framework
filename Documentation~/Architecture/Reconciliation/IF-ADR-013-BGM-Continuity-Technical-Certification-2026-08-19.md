# IF-ADR-013 — BGM Continuity Technical Certification — 2026-08-19

Status: **CERTIFIED / CLOSED FOR TECHNICAL BOUNDARY**  
ADR: [IF-ADR-013 — Optional Audio BGM Adapter](../ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md)  
Framework package baseline: `1c422f7f22ec5d17a25e7caea8108eb5b0c08a4c` (`Audio Fix`)  
Audio provider baseline: current `Audiofix` runtime cut in `ImmersiveGames/com.immersive.audio`  
Promotion status: **Experimental remains until FIRSTGAME / real consumer proof**

## Purpose

This record closes BGM-CONTINUITY-1 as a technical Framework + provider integration boundary.

The defect addressed by the cut was not “which cue has precedence”. It was lifetime semantics:

```text
owner exits / transient scene unloads
  must not imply Stop, Silence, Clear or automatic fallback
```

The accepted result is a sticky confirmed BGM presentation with explicit three-state intent:

```text
No Request  -> Preserve
Play(cue)   -> Apply/transition
Silence     -> Explicit release to silence
```

## Accepted architecture

```text
Framework Persistent Content / session lifetime
└─ BGM authority
   ├─ AudioRuntimeHost
   └─ FrameworkBgmDirector
          ↑ runtime scene injection
          │
Transient Route / Activity scenes
├─ FrameworkRouteBgmBinding
└─ FrameworkActivityBgmBinding
```

Authority split:

- `FrameworkBgmDirector` owns Framework BGM intent and provider-confirmed presentation evidence.
- `AudioRuntimeHost` / `AudioBgmService` own physical playback execution.
- Route/Activity bindings are transient declarations/consumers.
- Persistent lifetime is explicit composition; neither host nor director makes itself globally persistent.
- Scene injection attaches the explicit director to loaded consumers without creating a static/global BGM authority.

## Contract certified

```text
No new BGM request
  -> Preserve / NoChange
  -> no provider call
  -> confirmed presentation unchanged

Same confirmed cue
  -> NoChange
  -> no provider restart

Different cue
  -> provider-controlled transition
  -> commit only after provider success

Explicit Silence
  -> provider-controlled stop/fade
  -> confirmed explicit silence only after provider success

Activity exit
  -> owner intent cleared
  -> confirmed presentation preserved

Route exit
  -> owner intent cleared
  -> confirmed presentation preserved
```

Explicit silence is sticky under later no-request/owner-exit operations.

Rejected Play/Release preserves the previous confirmed presentation and remains retryable.

## Framework implementation evidence

The Framework cut introduces/updates the existing optional audio bridge rather than creating a second audio system:

- `FrameworkBgmDirector` keeps pending explicit intent distinct from provider-confirmed presentation.
- `FrameworkBgmOperation.Preserve` represents no provider mutation.
- `FrameworkBgmOperationOutcome.NoChange` records preserved/idempotent presentation.
- `FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent` publishes no request when no Activity cue is authored.
- `ClearActivityBgm` and `ClearRouteBgm` preserve confirmed BGM instead of restoring/falling back/stopping on owner exit.
- confirmed explicit silence is tracked independently from `confirmedBgm == null`.
- Route/Activity consumers receive the explicitly composed director through the Audio assembly scene-injection runtime.

## Provider implementation evidence

`com.immersive.audio` `AudioBgmService` provides:

- provider-idempotent same-cue `Play` success;
- no restart/playback-position reset for same-cue requests;
- controlled single-source different-cue transition;
- old cue remains playing during fade-out before source reconfiguration;
- transition completes to the requested cue;
- explicit Stop fades current cue to zero and then clears source state.

The cue-to-cue transition is sequential on one dedicated source:

```text
fade old cue out
-> reconfigure source
-> play/fade new cue in
```

It is not a simultaneous dual-source crossfade.

## QA topology

Canonical Framework entry:

```text
GameApplication
├─ startup Route -> QA Hub
└─ Persistent Content -> QA_UIGlobal
```

BGM authority:

```text
QA_UIGlobal
└─ QA_FrameworkBgm_SessionAuthority
   ├─ FrameworkBgmDirector
   └─ QA_FrameworkBgm_AudioRuntimeHost
```

Transient fixtures:

```text
QA_Audio
QA_AudioRouteB
```

Route B is intentionally BGM-neutral:

```text
Route B routeBgm = null
Startup Activity = Retain Previous
Activity policy = UseOwnOrPreserveCurrent
Activity cue = null
=> No Request
```

The Startup Activity exists to satisfy the current Route activation gate without inventing a BGM Play/Silence request.

## Setup certification

`Configure Audio QA` was run twice consecutively and produced the same valid topology:

```text
status='Applied'
sessionAuthorityScene='QA_UIGlobal'
sessionHostSource='QA_AudioValidatedHost'
sessionDefaults='Resolved'
routeBStartupActivity='RetainPreviousNoRequest'
routeBIntent='NoRequest'
```

This is accepted setup-idempotence evidence for the exercised Audio/BGM fixture.

## Automated Play Mode verdict

```text
Core Audio         7/7 PASS
Framework BGM     14/14 PASS
ADR-013A            5/5 PASS
Audio continuity    4/4 PASS
TOTAL              30/30 PASS
FAILED               0
```

Framework sticky cases include:

- `route-apply`;
- `same-confirmed-route`;
- `startup-activity`;
- `activity-own`;
- `activity-exit-preserves-confirmed`;
- `route-exit-preserves-confirmed`;
- `route-no-request-preserves`;
- `activity-no-request-preserves`;
- `use-route-without-route-preserves`;
- `explicit-silence`;
- `owner-exit-preserves-silence`;
- `no-request-after-silence`;
- `play-after-silence`;
- `route-exit-sticky-play`.

ADR-013A cases prove rejection/retry and optional-authority behavior.

Physical provider cases:

```text
same-cue-no-restart                  PASS
different-cue-no-abrupt-cut          PASS
different-cue-transition-completes  PASS
explicit-stop-fades-to-silence       PASS
```

## Real Framework lifecycle proof

A separate Play Mode session exercised the actual Route lifecycle instead of only the synthetic suite:

```text
QA Hub
  -> QA Framework BGM Route / QA_Audio
  -> Startup Activity Ready
  -> Own Activity request Succeeded
  -> BGM active
  -> request QA Framework BGM Route B
  -> QA_Audio unload
  -> QA_AudioRouteB load
  -> Startup Activity = Retain Previous
  -> Route Request Succeeded
  -> Activity Active + Ready
  -> blockingIssues=0
  -> BGM remained playing across Route A -> Route B
```

This proves the intended contract across real Activity change, Route exit, scene unload, Route B scene load, and Startup Activity activation with no new BGM request.

## Non-blocking diagnostics

Current `FrameworkRouteBgmBinding` emits a warning when a Route has a Startup Activity but no explicit valid `StartupActivityBgmBinding` is assigned.

For the Route B certification fixture this is semantically intentional: Route B and its Startup Activity both publish No Request.

Disposition:

```text
functional defect: NO
diagnostic/product-surface debt: YES
technical continuity certification impact: NONE
```

Do not fabricate a cue or Silence intent merely to suppress this warning.

## Out of scope

This certification does not promote ADR-013 to Stable and does not claim:

- FIRSTGAME / Sample real-consumer promotion;
- simultaneous dual-source crossfade;
- AudioMixer routing implementation;
- global AudioManager/service locator;
- implicit persistence of `AudioRuntimeHost` or `FrameworkBgmDirector`;
- generic audio authoring redesign.

## Disposition

```text
IF-ADR-013A provider-confirmed evidence      CLOSED / CERTIFIED
BGM-CONTINUITY-1 sticky semantics            CLOSED / CERTIFIED
Physical provider continuity                 CLOSED / CERTIFIED
Real Framework Route A -> B continuity       CLOSED / CERTIFIED IN QA
ADR-013 API maturity                         EXPERIMENTAL
FIRSTGAME / real consumer promotion          PENDING
```

No additional synthetic BGM runtime redesign is required for the accepted technical boundary. Reopen only for a reproducible contract regression, an accepted architecture change, or a concrete consumer defect that demonstrates the accepted contract is broken.
