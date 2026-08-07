# IF-TRACK — Immersive Framework

Status: Active  
Last updated: 2026-08-06  
Package version: `1.0.0-preview.17`

## Source baseline

```text
com.immersive.framework
  95f2626caf0f9e387cc3efd46deff4b3d0831ee2
```

## Summary

The package has one internal application/session composition root and explicit
feature runtime ports. Product areas include lifecycle, Player, Camera,
Pause/Input/Gate, Reset, Activity readiness, loading/transition foundations,
persistence foundations and diagnostics.

`FrameworkRuntimeHost` is Internal. Required runtime dependencies are supplied
through typed bindings and fail explicitly when unavailable. There is no public
static host registry or service-locator API.

Documentation topology (after 2026-08-06 condensation):

```text
Guides/                 product usage
Architecture/ADRs/      decisions (unique numbers)
Architecture/Tracking/  this board (only mutable status)
Architecture/Archive/   historic plans, audits, fix notes
```

## Track board

| Track | Real status | Proven coverage | Pending work | Next action |
|---|---|---|---|---|
| Runtime authority | Closed for current boundary | Internal host composition; narrow typed ports | preserve boundary | reject static/global lookup |
| Package hygiene | Closed for current boundary | package + QA import discipline | ongoing | do not restore compatibility facades |
| Player — Scene-Provided | **Closed / approved** | authoring, Route Primary Scene admission, Slot, Host, Actor adoption, readiness, release, reentry, Activity Restart, teardown; Stable product subset | broader automated admit/release matrix desirable | preserve baseline |
| PLAYER-DIAG-1 | **Closed / approved** | invalid-evidence formatting, safe token text, immutable last-operation snapshot, Advanced/Debug projection, manual restart/teardown regression | optional automated matrix | preserve semantics |
| Player — Manager-Provisioned | **Package Experimental** | `LocalPlayerProvisioningAuthoring` + runtime + Editor inspectors exist | FIRSTGAME consumer assembly and negative rollback proof | dedicated Route/scene consumer proof |
| Player — Session-Persistent | **Blocked** | origin reserved in architecture | authoring, admission and lifetime contracts | runtime currently rejects; needs approved package cut |
| Activity readiness + reveal | **Implemented (Experimental)** | ObserveOnly / WaitVisible / WaitCovered; participants; package Player readiness contribution | multiplayer policy; ADR-012 profile extract | preserve occurrence-scoped model |
| WaitCovered Loading progress | **Implemented (Experimental)** | participant-aware determinate progress (IF-ADR-011) | product polish after more consumer proof | preserve aggregate-only Loading |
| Camera | Closed for current single-output scope | persistent output, Player request/restoration; Stable product + Internal output authority | split-screen / multiple outputs | preserve one-output boundary |
| Pause / Input / Gate | Closed for current single-player scope | Player-bound Pause, resume, input restoration; Stable product surfaces | multiplayer policy | preserve explicit binding |
| Reset | Implemented (mostly Experimental) | Object Reset, Group Reset, Cycle Reset, Activity Restart | unload `update-retry` recomposition finding | separate Reset lifecycle cut |
| Activity transaction | Partial | readiness and cleanup foundations | explicit commit/finalization model | separate approved runtime cut |
| Persistence / ProgressionSave | Foundation | contracts and store exist | product authoring and real consumer proof | product decision |
| ObjectEntry / Local visibility | Implemented (Experimental) | adapters and declarations exist | dedicated product guides deferred | keep advanced/foundation |
| Editor product surface (IF-ADR-010) | **Proposed** | Editor-Authoring-Standard guide + many Custom Editors | accept ADR and finish migration | keep guide as usage slice |
| ADR-012 participation profile | **Proposed / not shipped** | requirement levels + evidence exist inline on `ActivityAsset` | profile asset, migration, circular validation, preflight | accept then implement; do not document profile as shipped |
| Authored identity (IF-ADR-014) | **In progress (~75%)** | IF-ID-02..06: stable-ID vocabulary, reference authority, owner definition tokens, validation scopes, regenerate UX, package tests | IF-ID-07 resolver (when needed); IF-ID-08 FIRSTGAME proof; broader QA matrix | preserve reference + definition-token owners; no automatic ID mutation |

## Implementation confirmation (code-backed)

Confirmed present in Runtime/Editor:

```text
GameApplicationAsset → bootstrap → Persistent Content → FrameworkRuntimeHost
SceneLocalPlayerAdmissionAuthoring (Scene-Provided Composer)
LocalPlayerProvisioningAuthoring (Manager-Provisioned; Experimental)
ActivityReadinessParticipant + entry policies + loading progress bridge
CameraRigComposer / PlayerGameplayCameraAuthoring / CameraOutputSessionBinding
PausePlayerInputBinding / PauseRequestTrigger / Gate / UnityPlayerInputGateAdapter
ResetRegistry / ObjectReset / CycleReset / ActivityRestart
FrameworkBgmDirector (optional Immersive.Audio; Experimental)
SceneLifecycleEvents
ActivityLocalVisibilityAdapter, ObjectEntryDeclaration
ApiStatus: Stable ~114 | Experimental ~509 | Internal ~203 | DevelopmentTooling 32 | Deferred 2
```

Confirmed **not** present:

```text
ActivityPlayerParticipationProfileAsset (IF-ADR-012)
ManagerProvisionedPlayerComposer / ManagerProvisionedPlayerRecipe
Session-Persistent product admission path (origin rejected at runtime)
CameraRigRecipe (removed; Unity Preset optional)
Public static FrameworkRuntimeHost locator
```

## Open findings

### Reset unload retry

Observed sequence:

```text
SceneReleasing unregister
→ update-retry register
→ on-disable unregister
```

Non-blocking in current Player tests; possible transient recomposition during
unload; requires its own Reset lifecycle cut.

### Missing canonical Slot–Host–Actor assignment snapshot

Point-in-time audit: no single authority answers “which Logical Player, Host and
Actor currently occupy a `PlayerSlotId` under which owner/scope/lifetime?” Truth
is split across participation, preparation, scene admission, occupancy and
capability tokens. Historic detail:

- [Auditoria-Slot-Assignment](../Archive/Audits/Auditoria-Slot-Assignment.md)

### Session-Persistent package gap

Architecture accepted in IF-ADR-003; product workflow unavailable. Do not
simulate with an unscoped Persistent Content prefab.

### Automated Player matrix

QA formatting smoke is intentionally narrow. Future technical cut may add
automated admit/release, duplicate release, Activity Restart reentry, Route
reentry and residual evidence checks. Manual FIRSTGAME regression remains
approved.

## Current execution priority

```text
IF-ADR-014 identity authority (IF-ID)
  IF-ID-02..06 landed (vocabulary, reference authority, ownership tokens, validation UX)
  next: IF-ID-08 FIRSTGAME proof; IF-ID-07 only if save/external boundary needs resolver
  plan: Architecture/Plans/IF-ID-IDENTITY-AUTHORITY-EXECUTION-PLAN-2026-08-06.md

Manager-Provisioned FIRSTGAME comparison path
  package surface exists

Session-Persistent Player
  blocked by package

IF-ADR-012 profile extract (if accepted)
  proposed only

Reset unload recomposition cut
  open finding

ObjectEntry / Local visibility product guides
  deferred
```

## API status waves (metadata only; no behavior change)

Wave evidence remains embedded here. There is no separate IF-CUT plan file.

```text
Wave 0+B (2026-07-31) — Camera product Stable; output authority Internal
Wave A   (2026-07-31) — Authoring assets, GameFlow request envelope, Identity Stable
Wave C   (2026-07-31) — Pause / Gate / InputMode product vocabulary Stable
Wave D   (2026-07-31) — Scene-Provided local Player product subset Stable

Approximate package counts after those waves (re-counted 2026-08-06):
  Stable ~114 | Experimental ~509 | Internal ~203 | DevelopmentTooling 32 | Deferred 2
```

Most Activity readiness, Loading/Transition, ProgressionSave, Manager-Provisioned
and large PlayerParticipation runtime surfaces remain Experimental or Internal.

## Closed notes (short)

### PLAYER-DIAG-1

Host-scoped last-operation diagnostics, safe invalid-evidence formatting and
manual Menu → Gameplay → Activity Restart → Menu → Stop regression are approved.
Do not reopen without new evidence.

### Activity readiness Player source ordering

Discovery must resolve Player projection before content execution so a Required
Player participant exists when the readiness occurrence begins. Historic fix note:

- [IF-M07-10-FIX1](../Archive/Fixes/IF-M07-10-FIX1-Player-Readiness-Source-Ordering.md)

## Historic programs

Large readiness/M07/completeness program documents are archived (not product truth):

- [Archive/Plans](../Archive/Plans/)
