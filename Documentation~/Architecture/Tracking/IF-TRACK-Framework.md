# IF-TRACK — Immersive Framework

Status: Active
Last updated: 2026-07-23
Package version: `1.0.0-preview.17`
Plan: [IF-PLAN-Framework-Evolution.v1.md](../Plans/IF-PLAN-Framework-Evolution.v1.md)

## Summary

The package has one internal application/session composition root and explicit
feature runtime ports. The current source contains lifecycle, Player, Camera,
Pause/InputMode, Gate, Reset, loading, transition, snapshot/save and diagnostics
modules. No post-H2 product cut is currently selected.

Documentation was consolidated from 243 Markdown files into canonical ADRs, one
immutable plan, this tracker and current usage guides. Historical manifests,
audits, closeouts, mutable roadmaps and micro-cut notes remain available in Git
history, not active navigation.

## Track board

| Track | Planned gate | Real status | Coverage | Pending work | Next action | Validation |
|---|---|---|---|---|---|---|
| Runtime authority | Explicit narrow ports; no static host lookup | Closed | Bootstrap and internal host bindings | None in current scope | Preserve boundary | H2.4 user evidence: 10 focused cases passed |
| Package hygiene | Remove superseded Pause/Input and UnityInputTarget paths | Closed | Source and QA migration delivered | None in current scope | Do not restore compatibility APIs | User evidence: compile, boot and focused regressions passed |
| Player | One canonical P3 lane | Partial product | Provisioned and scene-owned Players, selection, admission and release exist | Multiplayer/reconnect; some product proof | Select only through a new approved cut | Past focused QA exists; no validation run for this docs cut |
| Camera | Request/output authority | Closed for current single-output scope | Recipe/Composer, output session/context and scoped requests | Split-screen/multiple outputs | Keep single-output boundary explicit | Prior QA/FIRSTGAME evidence recorded; not rerun |
| Audio BGM adapter | Optional Route/Activity BGM semantics | Implemented experimental | Separate adapter assembly delegates playback to `com.immersive.audio` | Product maturity and current consumer proof | Preserve optional dependency boundary | Not rerun for docs cut |
| Pause/InputMode | One product binding and one physical writer | Closed for current single-player scope | Running/paused posture, lifecycle release and Gate integration | Interactive Pause UI; multiplayer policy | Preserve canonical binding | User evidence includes Pause lifecycle/reentry |
| Reset | Explicit ports and distinct Object/Cycle Reset | Implemented | Registry, executor, Unity participants, triggers and Activity Restart | Public naming cleanup candidate | Validate through focused QA when changed | Not rerun for docs cut |
| Activity transaction | Separate authority/readiness/finalization | Partial | Readiness and previous-scope cleanup foundations exist | Explicit commit/phase/finalization model | Requires a new approved runtime cut | No completion claim |
| Persistence | Snapshot/preferences/progression contracts | Implemented foundation | Runtime contracts exist | Product authoring, sample and real-game proof | Needs product decision | No current release claim |

## Current execution priority

No runtime implementation cut is active. The next action is to select one
product lane and create a scoped ADR/plan version only if the existing canonical
ADRs do not already decide it.

## Manual decisions needed

- Choose the next product lane.
- Decide whether Reset's public `RegisterWithCurrentHost` method name warrants a
  migration despite its now-explicit port implementation.
- Decide when multi-output Camera or multiplayer Player/Pause becomes product scope.

## Validation log index

- H2.4: user-provided framework/QA import and compile plus focused Play Mode
  smoke, `Passed`, 10 cases.
- `FRAMEWORK-HYGIENE-1`: user-provided package compile, QA compile, framework
  boot, focused regressions, Pause lifecycle/reentry and `Time.timeScale == 1`.
- Documentation consolidation: read/static validation only; no Unity command
  was executed.

Do not convert historical `pending Unity validation` text from removed manifests
into a current failure or a current pass. Only this tracker records operational
status going forward.
