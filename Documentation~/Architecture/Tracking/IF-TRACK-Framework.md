# IF-TRACK — Immersive Framework

Status: Active  
Last updated: 2026-07-31  
Package version: `1.0.0-preview.17`

## Source baseline

```text
com.immersive.framework
  bdb76a06a3b75adc9ac7fa5d3e63fbe457ed5ae2

QAFramework
  64f900a5c26ab07ad37f2e7d6e578e8efcfb72a4

planet-devourer
  116225d50a3c6af976355715d3216c0cb80852eb
```

## Summary

The package has one internal application/session composition root and explicit feature runtime ports. Current product areas include lifecycle, Player, Camera, Pause/Input, Gate, Reset, loading, transition, persistence foundations and diagnostics.

The Scene-Provided Player lane now has both:

```text
authoring proof
runtime/manual consumer proof
```

The approved FIRSTGAME shape is:

```text
Player_SceneProvided
  PlayerInput
  Local Player Host
  Scene-Provided Player Composer
  Actor Mount
    Actor_PlayerSceneProvided
```

`PLAYER-DIAG-1` added persistent last-operation diagnostics and hardened teardown formatting. The focused manual regression is approved.

## Track board

| Track | Real status | Proven coverage | Pending work | Next action |
|---|---|---|---|---|
| Runtime authority | Closed for current boundary | internal host composition and narrow typed ports | preserve boundary | reject static/global lookup |
| Package hygiene | Closed for current boundary | current package and QA import | ongoing discipline | do not restore compatibility facades |
| Player — Scene-Provided | Consumer baseline approved | authoring, Route Primary Scene admission, Slot, Host, Actor adoption, readiness, release, reentry, Activity Restart and teardown; **API status Wave D**: Scene-Provided product subset `Stable` | separate Reset unload finding; Manager-Provisioned remains Experimental | freeze baseline and move to source comparison |
| PLAYER-DIAG-1 | **Closed / approved** | total invalid-evidence formatting, safe token text, persistent immutable snapshot, Advanced/Debug projection, manual restart/teardown regression | broader automated admit/release matrix remains desirable | preserve semantics |
| Player — Manager-Provisioned | Package implemented; FIRSTGAME pending | provisioning contracts and authoring exist | real consumer assembly and negative rollback proof | create dedicated Route/scene |
| Player — Session-Persistent | Blocked by package gap | architecture accepted | authoring, admission and lifetime contracts | new approved package cut |
| Camera | Closed for current single-output scope | persistent output, Player request and restoration; **API status Wave 0+B**: product surfaces `Stable`, output authority `Internal` | split-screen/multiple outputs | preserve one-output boundary |
| Pause/Input/Gate | Closed for current single-player scope | Player-bound Pause, resume and input restoration; **API status Wave C**: product surfaces `Stable` | multiplayer policy | preserve explicit binding |
| Reset | Implemented | Object Reset, Group Reset and Activity Restart | unload `update-retry` recomposition finding | create separate Reset cut |
| Activity transaction | Partial | readiness and cleanup foundations | explicit commit/finalization model | separate approved runtime cut |
| Persistence | Foundation | contracts exist | product authoring and real consumer proof | product decision |

## PLAYER-DIAG-1 acceptance

### Implementation

- `PlayerHostEvidenceResult.ToDiagnosticString()` formats invalid evidence explicitly.
- `SceneLocalPlayerAdmissionToken.StableText` does not dereference an invalid assignment token.
- The host-scoped module records an immutable last-operation snapshot.
- The snapshot stores values/identities only.
- `FrameworkRuntimeHost > Advanced / Debug` exposes the projection.
- Existing statuses remain:
  - `SucceededReleased`;
  - `SucceededAlreadyReleased`.

### QA/import

- QAFramework initialized and compiled without blocking errors.
- `QaPlayerHostEvidenceDiagnosticFormattingSmoke` is an Editor menu smoke, not an NUnit Test Runner case.
- The smoke covers partial/invalid Host-evidence formatting.

### FIRSTGAME manual proof

Approved flow:

```text
Menu
→ Gameplay
→ Activity Restart
→ valid active admission
→ Menu
→ successful release snapshot
→ Stop
```

Approved active state:

```text
Active Count = 1
Occupied Slot Count = 1
Last Status = SucceededAdmitted
Host Evidence Present = Yes
```

Approved released state:

```text
Active Count = 0
Occupied Slot Count = 0
Last Status = SucceededReleased
Release Succeeded = Yes
Host Evidence Present = No
```

The previous teardown identity exception was not reproduced.

## Current execution priority

```text
Scene-Provided comparison baseline
  closed

Manager-Provisioned comparison path
  next

Session-Persistent Player
  blocked by package
```

The Manager-Provisioned FIRSTGAME cut should preserve the same movement, Camera, Pause and reset behavior so the comparison isolates provisioning and admission UX.

## Open findings

### Reset unload retry

Observed sequence:

```text
SceneReleasing unregister
→ update-retry register
→ on-disable unregister
```

Classification:

- non-blocking in current Player tests;
- possible transient recomposition during unload;
- outside `PLAYER-DIAG-1`;
- requires its own Reset lifecycle cut.

### Automated Player matrix

The current QA formatting smoke is intentionally narrow. A future technical cut may add automated:

- admit/release state proof;
- duplicate release;
- Activity Restart reentry;
- Route reentry;
- no residual Slot/Host/Actor evidence.

This does not reopen the manually approved FIRSTGAME regression.

## API status promotion

See `../Plans/IF-CUT-ApiStatus-Promotion.v1.md`.

```text
API status Wave 0+B
  date: 2026-07-31
  types promoted to Stable: 42 (Camera product + CameraAuthoring)
  types marked Internal: 17 (Camera output/lifecycle authority + debug snapshot)
  UNMARKED under Runtime/Camera + CameraAuthoring: 0
  boundary: single-output Camera product API; multi-output/split-screen out of scope
  evidence: IF-ADR-004 + TRACK Camera Closed + Camera-Usage guide
  behavior change: none (metadata only)

API status Wave A
  date: 2026-07-31
  types promoted to Stable: 29
    Authoring: GameApplication/Route/Activity assets, content profiles/entries/policies,
               PersistentContentComposition, settings, validation mode, RouteId/ActivityId
    GameFlow: Route/Activity request triggers, events, UnityEvent bridges, phase/outcome
    Identity: IFrameworkIdentity, FrameworkIdentityValue/Key/Domain
  boundary: product authoring + Game Flow request envelope; Activity commit internals remain Experimental
  evidence: IF-ADR-001/002/008 + Framework-Usage guide
  behavior change: none (metadata only)
  package status counts after Wave A:
    Stable 72 | Experimental 542 | Internal 192 | DevelopmentTooling 32 | Deferred 2

API status Wave C
  date: 2026-07-31
  types promoted to Stable: 36
    Pause product triggers/bindings/adapters/vocabulary
    Gate decision primitives
    InputMode product vocabulary (Kind/Definitions/Rules/request result types)
    UnityPlayerInputGateAdapter + PlayerInputActionMapReference
  kept Experimental: PauseActivityBindingAuthoring*
  kept Internal: PauseRuntime, surface/timeScale runtimes, GateRequestAdmission, UnityPlayerInputStateWriter
  boundary: single-player Pause/Input/Gate; multiplayer out of scope
  evidence: IF-ADR-005 + TRACK Pause/Input/Gate Closed
  behavior change: none (metadata only)

API status Wave D
  date: 2026-07-31
  types promoted to Stable: 6
    PlayerSlotId, PlayerSlotProfile, LocalPlayerHostAuthoring,
    SceneLocalPlayerAdmissionAuthoring, SceneLogicalPlayerActorEvidence,
    PlayerGameplayCameraAuthoring
  kept Experimental: LocalPlayerProvisioning*, LocalPlayerActorSelectionRequestAuthoring,
    remainder of PlayerParticipation runtime
  boundary: Scene-Provided local Player product only
  evidence: IF-ADR-003 + TRACK Scene-Provided approved + FIRSTGAME
  behavior change: none (metadata only)

  package status counts after Waves 0+B, A, C, D:
    Stable 114 | Experimental 501 | Internal 192 | DevelopmentTooling 32 | Deferred 2
```

## Validation log

- Scene-Provided Composer authoring: passed.
- Local Player Host validation: passed.
- Menu → Gameplay → Menu → Stop: passed.
- Menu → Gameplay → Menu → Gameplay → Menu → Stop: passed.
- active persistent admission snapshot: passed.
- persistent release snapshot: passed.
- Activity Restart operation: passed.
- Activity Restart → Menu → Stop teardown regression: passed.
- visual readmission after Activity Restart: user-confirmed.
- previous `Framework identity value must be valid` exception: not reproduced.
