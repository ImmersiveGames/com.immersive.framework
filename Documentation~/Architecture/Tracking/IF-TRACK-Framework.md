# IF-TRACK — Immersive Framework

Status: Active
Last updated: 2026-07-25
Package version: `1.0.0-preview.17`
Plan: [IF-PLAN-Framework-Evolution.v1.md](../Plans/IF-PLAN-Framework-Evolution.v1.md)

## Summary

The package has one internal application/session composition root and explicit
feature runtime ports. The current source contains lifecycle, Player, Camera,
Pause/InputMode, Gate, Reset, loading, transition, snapshot/save and diagnostics
modules.

The Player product lane now has a validated Scene-Provided authoring shape in
FIRSTGAME:

```text
Player_SceneProvided
  Local Player Host
  Scene-Provided Player Composer
  Actor Mount
    Actor_PlayerSceneProvided
```

This is authoring evidence only. Route Primary Scene runtime admission remains an
explicit pending gap.

Documentation was consolidated from 243 Markdown files into canonical ADRs, one
immutable plan, this tracker and current usage guides. Historical manifests,
audits, closeouts, mutable roadmaps and micro-cut notes remain available in Git
history, not active navigation.

## Track board

| Track | Planned gate | Real status | Coverage | Pending work | Next action | Validation |
|---|---|---|---|---|---|---|
| Runtime authority | Explicit narrow ports; no static host lookup | Closed | Bootstrap and internal host bindings | None in current scope | Preserve boundary | H2.4 user evidence: 10 focused cases passed |
| Package hygiene | Remove superseded Pause/Input and UnityInputTarget paths | Closed | Source and QA migration delivered | None in current scope | Do not restore compatibility APIs | User evidence: compile, boot and focused regressions passed |
| Player | One canonical P3 lane | Partial product | Provisioned Player; Scene-Provided prefab/composer authoring; Activity-content admission path; selection, admission and release contracts | Route Primary Scene admission; Session-Persistent source; multiplayer/reconnect; focused real-game runtime proof | Validate Route-scene runtime path before creating or changing code | Host and composer authoring PASS in FIRSTGAME; no Route-scene Play Mode admission claim |
| Camera | Request/output authority | Closed for current single-output scope | Recipe/Composer, output session/context and scoped requests | Split-screen/multiple outputs | Keep single-output boundary explicit | Prior QA/FIRSTGAME evidence recorded; not rerun |
| Audio BGM adapter | Optional Route/Activity BGM semantics | Implemented experimental | Separate adapter assembly delegates playback to `com.immersive.audio` | Product maturity and current consumer proof | Preserve optional dependency boundary | Not rerun for docs cut |
| Pause/InputMode | One product binding and one physical writer | Closed for current single-player scope | Running/paused posture, lifecycle release and Gate integration | Interactive Pause UI; multiplayer policy | Preserve canonical binding | User evidence includes Pause lifecycle/reentry |
| Reset | Explicit ports and distinct Object/Cycle Reset | Implemented | Registry, executor, Unity participants, triggers and Activity Restart | Public naming cleanup candidate | Validate through focused QA when changed | Not rerun for docs cut |
| Activity transaction | Separate authority/readiness/finalization | Partial | Readiness and previous-scope cleanup foundations exist | Explicit commit/phase/finalization model | Requires a new approved runtime cut | No completion claim |
| Persistence | Snapshot/preferences/progression contracts | Implemented foundation | Runtime contracts exist | Product authoring, sample and real-game proof | Needs product decision | No current release claim |

## Current execution priority

The current Player checkpoint is:

```text
authoring shape
  closed for the tested Scene-Provided prefab

Route Primary Scene runtime admission
  pending focused verification
```

Do not create a runtime correction before observing Play Mode evidence from the
current FIRSTGAME fixture. If the composer is not admitted, create one scoped
package cut followed by QA and FIRSTGAME validation.

## Manual decisions needed

- Confirm the desired Route Primary Scene admission and release lifecycle after
  focused Play Mode evidence.
- Select when Session-Persistent Logical Player becomes implementation scope.
- Decide whether Reset's public `RegisterWithCurrentHost` method name warrants a
  migration despite its now-explicit port implementation.
- Decide when multi-output Camera or multiplayer Player/Pause becomes product scope.

## Validation log index

- H2.4: user-provided framework/QA import and compile plus focused Play Mode
  smoke, `Passed`, 10 cases.
- `FRAMEWORK-HYGIENE-1`: user-provided package compile, QA compile, framework
  boot, focused regressions, Pause lifecycle/reentry and `Time.timeScale == 1`.
- `ACTOR-DECLARATION-UX-1-R1`: user-provided package compilation completed
  without blocking errors after the compile correction.
- `HOST-VALIDATION-UX-1`: FIRSTGAME Host validation reported
  `Ready — shared Local Player Host invariants are valid`.
- `SCENE-PROVIDED-COMPOSER-1-R1`: FIRSTGAME `Apply / Rebuild` reported
  `status='Valid'`, `succeeded='True'`, internal typed profile evidence created;
  subsequent Inspector validation reported valid.
- The Scene-Provided evidence above is authoring-only. No Route Primary Scene
  Play Mode admission, readiness or release pass is recorded.
- `PLAYER-SCENE-PROVIDED-DOCS-1`: documentation/static consolidation only; no
  Unity command was executed for this docs cut.

Do not convert historical `pending Unity validation` text from removed manifests
into a current failure or a current pass. Only this tracker records operational
status going forward.
