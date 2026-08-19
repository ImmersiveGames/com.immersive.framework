# Audio BGM Usage

Status: Current / Experimental
Last updated: 2026-08-19

## Dependency and boundary

Install a compatible `com.immersive.audio` package. The optional `Immersive.Framework.Audio` assembly is enabled by version define; Framework Core remains independent from Audio.

The Framework owns Route/Activity BGM intent. `com.immersive.audio` owns physical playback. Neither package introduces a global AudioManager, service locator, or hidden static authority.

## Canonical composition

For BGM that must survive transient Route/Activity scenes, compose the physical and intent authorities under Framework Persistent Content:

```text
Persistent Content / Session lifetime
└─ Audio Runtime
   ├─ AudioRuntimeHost
   └─ FrameworkBgmDirector
          ↑ injected into loaded scene consumers
          │
Route / Activity scenes
├─ FrameworkRouteBgmBinding
└─ FrameworkActivityBgmBinding
```

`FrameworkBgmDirector` does not make itself persistent. `AudioRuntimeHost` also does not make itself persistent. Persistence is explicit composition owned by the Framework Persistent Content scene or another deliberate application/session lifetime.

The director owns an internal scene-injection runtime that attaches itself to `IFrameworkBgmDirectorConsumer` instances in loaded scenes and on later scene loads. This is not a global/static authority and does not replace the persistent composition requirement.

Do not serialize cross-scene references from transient Route/Activity bindings to the persistent director. Let the runtime injection attach the authority.

## Authoring

1. In Framework Persistent Content, configure an `AudioRuntimeHost` with an explicit `AudioDefaultsAsset`.
2. Add `FrameworkBgmDirector` and assign the `AudioRuntimeHost` explicitly.
3. Add `FrameworkRouteBgmBinding` to Route content and assign the Route cue only when the Route has an explicit BGM opinion.
4. Add `FrameworkActivityBgmBinding` to Activity content when that Activity has an explicit BGM policy.
5. Assign `Assigned Activity`, cue when applicable, and one of the policies below.
6. If a Route Startup Activity must publish an immediate Play/Silence intent during Route entry, assign its explicit `FrameworkActivityBgmBinding` to `FrameworkRouteBgmBinding.StartupActivityBgmBinding`.

## Activity policies

| Policy | Published intent |
|---|---|
| `UseOwnOrRoute` | Play Activity cue when authored; otherwise Play Route cue when authored; if neither exists, publish no request. |
| `UseOwnOrPreserveCurrent` | Play Activity cue when authored; otherwise publish no request and preserve the confirmed presentation. |
| `UseRoute` | Play the Route cue when authored; if the Route has no cue, publish no request. |
| `Silence` | Explicitly request silence/stop. |

`UseOwnOrRetainActivityUntilRouteExit` is not the current contract. Sticky continuity is no longer based on automatic restoration at owner exit.

## Sticky confirmed presentation

BGM-CONTINUITY-1 defines three semantic intents:

```text
No Request / Unspecified  -> Preserve confirmed presentation
Play(cue)                 -> Apply/transition to cue
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

A confirmed BGM presentation remains active across Activity exit, Route exit, and transient scene lifetime changes until a new explicit Play or Silence intent is successfully applied.

Absence of a binding, `routeBgm = null`, `activityBgm = null` under a preserve/no-request policy, owner exit, or absence of a new winner is not Silence.

Explicit Silence is also sticky: once silence is provider-confirmed, later no-request/owner-exit operations preserve that silence until a new Play succeeds.

## Runtime flow

```text
Route/Activity lifecycle
        ↓
Framework Route/Activity BGM binding
        ↓
FrameworkBgmDirector pending intent
        ↓
No Request? ────────────────> Preserve confirmed state / no provider call
        │
        └─ Play / Silence
                ↓
          AudioRuntimeHost
                ↓
          provider result
                ↓
        success -> commit confirmed presentation
        failure -> preserve previous confirmed presentation
```

`Applied` and `Released` mean provider-confirmed execution. A rejected operation never commits the requested presentation and can be retried.

## Route and Activity exit

`FrameworkRouteBgmBinding` calls `ClearRouteBgm` on Route exit. `FrameworkActivityBgmBinding` calls `ClearActivityBgm` on Activity exit.

Those clear operations clear owner intent/evidence only. They do not physically stop the confirmed BGM. The director records `FrameworkBgmOperation.Preserve` + `FrameworkBgmOperationOutcome.NoChange` and leaves the confirmed presentation unchanged.

This is the central BGM-CONTINUITY-1 distinction: **owner lifetime is not playback-presentation lifetime**.

## Startup Activity

When a Route has a Startup Activity, Route BGM refresh is deferred so the Startup Activity can publish its explicit intent first.

If `StartupActivityBgmBinding` is valid and matches the Route Startup Activity, it is applied immediately.

If no valid explicit Startup Activity BGM binding is assigned, the current runtime emits a warning and evaluates the pending Route intent. This can still be semantically valid for an intentionally no-op Startup Activity where both the Route and Activity publish no BGM request; the warning is diagnostic/product-surface debt, not an instruction to fabricate a cue or Silence intent.

Do not add a fake BGM binding merely to suppress that warning.

## Diagnostics

Inspect:

- `FrameworkBgmDirector.ConfirmedBgm`;
- `FrameworkBgmDirector.ConfirmedExplicitSilence`;
- Route and Activity authored cues/policies;
- `LastOperationResult` on director/bindings;
- requested cue / requested silence;
- previous confirmed cue;
- resulting confirmed cue/silence;
- operation and outcome.

Framework operation values:

```text
Operation
  Apply
  Release
  Preserve

Outcome
  Applied
  Released
  NoChange
  OptionalAuthorityUnavailable
  Rejected
```

A missing optional `AudioRuntimeHost` produces `OptionalAuthorityUnavailable`; do not solve this with scene search or a global audio manager.

## Certified QA — BGM-CONTINUITY-1

Technical certification completed on 2026-08-19 through the canonical QAFramework Audio surface:

```text
Core Audio         7/7 PASS
Framework BGM     14/14 PASS
ADR-013A            5/5 PASS
Audio continuity    4/4 PASS
TOTAL              30/30 PASS
FAILED               0
```

The suite proves:

- Route Play and same-confirmed cue NoChange;
- Startup Activity Play;
- Activity-own Play;
- Activity exit preserves confirmed BGM;
- Route exit preserves confirmed BGM;
- Route no-request preserves;
- Activity no-request preserves;
- `UseRoute` with no Route cue preserves;
- explicit Silence;
- owner exit and no-request preserve confirmed Silence;
- Play after Silence;
- rejected Play/Release preserve previous confirmed state and can retry;
- optional authority absence remains non-corrupting;
- physical same-cue no-restart;
- physical different-cue controlled transition;
- explicit stop fade-to-silence.

A separate real Framework lifecycle proof also completed:

```text
QA Hub
  -> Route A / QA_Audio
  -> Own Activity BGM confirmed
  -> Route A exit / QA_Audio unload
  -> Route B / QA_AudioRouteB
  -> Startup Activity = Retain Previous / NoRequest
  -> Route B Ready
  -> BGM remained playing
```

That closes the technical continuity boundary in QA. It is not FIRSTGAME promotion evidence.

## Experimental status

ADR-013 remains `Experimental` because the optional BGM boundary still requires real consumer integration/usability proof in Sample/FIRSTGAME before API-maturity promotion.

Technical continuity is closed; the remaining promotion gate is consumer proof, not another synthetic BGM runtime redesign.

## Validation checklist

- Persistent `AudioRuntimeHost` + `FrameworkBgmDirector` are explicitly composed under the intended long-lived Framework content lifetime.
- Transient Route/Activity bindings receive the director through runtime injection.
- No cross-scene serialized director dependency is required.
- `NoRequest` is never translated to Silence.
- Owner exit does not call physical Stop.
- Same confirmed cue does not restart provider playback.
- Different cue transitions through the provider.
- Explicit Silence is the only normal lifecycle intent that stops BGM.
- Rejected provider operations preserve previous confirmed state.
- Run canonical Audio QA from Framework bootstrap/QA Hub; do not treat `QA_Audio.unity` as a standalone application entrypoint.
