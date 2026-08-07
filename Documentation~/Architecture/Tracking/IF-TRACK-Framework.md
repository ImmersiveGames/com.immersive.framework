# IF-TRACK — Immersive Framework

Status: Active  
Last updated: 2026-08-07  
Package version: `1.0.0-preview.17`

## Source baseline

```text
com.immersive.framework
  99052a13ad3e1cd23bcb2a5b33fde0356f1d317c
  IF-ID-finale
```

QAFramework and FIRSTGAME closure evidence below includes manual/local validation performed after the package identity implementation. The tracker does not invent unpublished consumer/QA commit SHAs.

## Summary

The package has one internal application/session composition root and explicit feature runtime ports. Product areas include lifecycle, Player, Camera, Pause/Input/Gate, Reset, Activity readiness, loading/transition foundations, persistence foundations and diagnostics.

`FrameworkRuntimeHost` is Internal. Required runtime dependencies are supplied through typed bindings and fail explicitly when unavailable. There is no public static host registry or service-locator API.

Documentation topology:

```text
Guides/                 product usage
Architecture/ADRs/      decisions (unique numbers)
Architecture/Tracking/  this board (only mutable status)
Architecture/Archive/   historic plans, audits, fix notes
Architecture/Plans/     no closed execution records
```

## Track board

| Track | Real status | Proven coverage | Pending work | Next action |
|---|---|---|---|---|
| Runtime authority | Closed for current boundary | Internal host composition; narrow typed ports | preserve boundary | reject static/global lookup |
| Package hygiene | Closed for current boundary | package + QA import discipline | ongoing | do not restore compatibility facades |
| Player — Scene-Provided | **Closed / approved** | authoring, Route Primary Scene admission, Slot, Host, Actor adoption, readiness, release, reentry, Activity Restart, teardown; Stable product subset | broader automated admit/release matrix desirable | preserve baseline |
| PLAYER-DIAG-1 | **Closed / approved** | invalid-evidence formatting, safe token text, immutable last-operation snapshot, Advanced/Debug projection, manual restart/teardown regression | optional automated matrix | preserve semantics |
| Player — Manager-Provisioned | **Package Experimental** | `LocalPlayerProvisioningAuthoring` + runtime + Editor inspectors exist | active Activity late-join/waiting semantics and consumer hardening | follow readiness high-risk audit |
| Player — Session-Persistent | **Blocked** | origin reserved in architecture | authoring, admission and lifetime contracts | runtime currently rejects; needs approved package cut |
| Activity readiness + reveal | **Implemented (Experimental); high-risk semantics under audit** | ObserveOnly / WaitVisible / WaitCovered; participants; package Player readiness contribution | zero-participant/waiting-for-join + late-join/loading causal audit | complete high-risk audit before new runtime cut |
| WaitCovered Loading progress | **Implemented (Experimental)** | participant-aware determinate progress (IF-ADR-011) | verify semantics against active waiting/late join | preserve aggregate-only Loading until audit decides otherwise |
| Camera | Closed for current single-output scope | persistent output, Player request/restoration; Stable product + Internal output authority | split-screen / multiple outputs | preserve one-output boundary |
| Pause / Input / Gate | Closed for current single-player scope | Player-bound Pause, resume, input restoration; Stable product surfaces | multiplayer policy | preserve explicit binding |
| Reset | Implemented (mostly Experimental) | Object Reset, Group Reset, Cycle Reset, Activity Restart | unload `update-retry` recomposition finding | separate Reset lifecycle cut |
| Activity transaction | Partial | readiness and cleanup foundations | explicit commit/finalization model | separate approved runtime cut |
| Persistence / ProgressionSave | Foundation | contracts and store exist | product authoring and real consumer proof | product decision |
| ObjectEntry / Local visibility | Implemented (Experimental) | adapters and declarations exist | dedicated product guides deferred | keep advanced/foundation |
| Editor product surface (IF-ADR-010) | **Proposed** | Editor-Authoring-Standard guide + many Custom Editors | accept ADR and finish migration | keep guide as usage slice |
| ADR-012 participation profile | **Proposed / not shipped** | requirement levels + evidence exist inline on `ActivityAsset` | profile asset, migration, circular validation, preflight | accept then implement; do not document profile as shipped |
| Authored identity (IF-ADR-014) | **Closed / approved for current boundary** | IF-ID-02..06 package + tests; canonical QA 6/6 twice with idempotent cleanup; FIRSTGAME IF-ID-08 duplication/remediation proof | **IF-ID-07 deferred by design** | preserve exact-reference + token authority; reopen only on concrete boundary need/regression |

## IF-ID closure record

Normative decision:

- `IF-ADR-014 — Authored Definition and Stable Identity Authority` is Accepted.
- Exact Route/Activity asset reference is authored-definition authority.
- `RouteId` / `ActivityId` remain stable boundary/diagnostic evidence.
- Route/Activity operational owners require a `RuntimeDefinitionToken`.
- Occurrence/readiness/supersession authority remains definition/occurrence scoped.
- Stable-ID collision is diagnosable and explicitly repairable; it is not runtime equality.

Package proof:

```text
IF-ID-02..06 complete
runtime identity tests passed
Editor identity tests passed
Unity minimum 6000.5.0f1
```

QA proof:

```text
runner:
  Immersive Framework QA/Game Flow/Run Identity Authority Regression

cases:
  baseline-authority-snapshot
  route-collision-transition
  activity-collision-transition
  ownership-release-isolation
  readiness-collision-isolation
  legitimate-supersession-preservation

two consecutive Play Mode executions:
  status Passed
  6/6 completed
  failures none
  cleanup none
  teardown none
  roots 3 -> 3
```

FIRSTGAME proof:

```text
duplicate
→ diagnose collision
→ open conflicting asset
→ regenerate copied stable ID
→ validate
→ run
→ rename/move
→ run again
```

Final consumer state is valid and contains no deliberate collision.

Deferred:

```text
IF-ID-07
  application-scoped stable-ID resolver
  open only when a real save/external boundary requires it
```

Closed execution record:

- [IF-ID archived plan](../Archive/Plans/IF-ID-IDENTITY-AUTHORITY-EXECUTION-PLAN-2026-08-06.md)

## Implementation confirmation (code-backed)

Confirmed present in Runtime/Editor:

```text
GameApplicationAsset → bootstrap → Persistent Content → FrameworkRuntimeHost
SceneLocalPlayerAdmissionAuthoring (Scene-Provided Composer)
LocalPlayerProvisioningAuthoring (Manager-Provisioned; Experimental)
ActivityReadinessParticipant + entry policies + loading progress bridge
CameraRigComposer / PlayerGameplayCameraAuthoring / CameraOutputSessionBinding
PausePlayerInputBinding / PauseRequestTrigger / Gate / UnityPlayerInputGateAdapter
ResetRegistry / ObjectReset / Cycle Reset / Activity Restart
FrameworkBgmDirector (optional Immersive.Audio; Experimental)
SceneLifecycleEvents
ActivityLocalVisibilityAdapter, ObjectEntryDeclaration
RuntimeDefinitionToken + Route/Activity owner token requirement
Route/Activity stable-ID collision validation + regenerate UX
```

Confirmed **not** present / deliberately unavailable:

```text
ActivityPlayerParticipationProfileAsset (IF-ADR-012)
ManagerProvisionedPlayerComposer / ManagerProvisionedPlayerRecipe
Session-Persistent product admission path (origin rejected at runtime)
CameraRigRecipe (removed; Unity Preset optional)
Public static FrameworkRuntimeHost locator
Application-scoped stable-ID resolver (IF-ID-07 deferred)
```

## Open findings

### Activity readiness — active waiting / late join / loading

A high-risk audit is the immediate next architecture activity.

It must determine the causal contract for:

```text
Logical Actors Prepared
+ WaitCovered
+ zero Player initially
+ late join
+ loading completion/reveal
```

The audit must distinguish a legitimate active waiting state from terminal failure and determine whether active-Activity reconciliation can progress without a same-Activity re-request.

No runtime fix is approved until that audit identifies the exact authority and first non-progress point.

### Reset unload retry

Observed sequence:

```text
SceneReleasing unregister
→ update-retry register
→ on-disable unregister
```

Non-blocking in current Player tests; possible transient recomposition during unload; requires its own Reset lifecycle cut.

### Missing canonical Slot–Host–Actor assignment snapshot

No single authority currently answers “which Logical Player, Host and Actor currently occupy a `PlayerSlotId` under which owner/scope/lifetime?” Truth is split across participation, preparation, scene admission, occupancy and capability tokens. Historic detail remains in the archived Slot Assignment audit.

### Session-Persistent package gap

Architecture accepted in IF-ADR-003; product workflow unavailable. Do not simulate with an unscoped Persistent Content prefab.

### Automated Player matrix

Future technical work may add automated admit/release, duplicate release, Activity Restart reentry, Route reentry and residual evidence checks. Do not merge this with the closed IF-ID runner.

## Current execution priority

```text
Activity Readiness high-risk audit
  zero-participant / waiting-for-join semantics
  late-join reconciliation
  Logical Actors Prepared dependency
  WaitCovered loading/reveal gate
  occurrence correlation

Manager-Provisioned Player
  follow only after the readiness audit establishes the correct causal contract

Session-Persistent Player
  blocked by package

IF-ADR-012 profile extract
  proposed only

Reset unload recomposition cut
  open finding

ObjectEntry / Local visibility product guides
  deferred

IF-ID
  CLOSED for current scope
  IF-ID-07 deferred
```

## API status waves (metadata only; no behavior change)

```text
Wave 0+B (2026-07-31) — Camera product Stable; output authority Internal
Wave A   (2026-07-31) — Authoring assets, GameFlow request envelope, Identity Stable
Wave C   (2026-07-31) — Pause / Gate / InputMode product vocabulary Stable
Wave D   (2026-07-31) — Scene-Provided local Player product subset Stable
```

Most Activity readiness, Loading/Transition, ProgressionSave, Manager-Provisioned and large PlayerParticipation runtime surfaces remain Experimental or Internal.

## Closed notes

### IF-ID

Closed 2026-08-07 for the current boundary.

Do not reopen stable identity work merely because another subsystem has a readiness, loading, or Player lifecycle bug. Reopen only with concrete evidence that authored-definition, occurrence, operational ownership, or explicit stable-ID remediation semantics regressed.

### PLAYER-DIAG-1

Host-scoped last-operation diagnostics, safe invalid-evidence formatting and manual Menu → Gameplay → Activity Restart → Menu → Stop regression are approved. Do not reopen without new evidence.

### Activity readiness Player source ordering

Discovery must resolve Player projection before content execution so a Required Player participant exists when the readiness occurrence begins. Historic fix notes remain archived.

## Historic programs

Large readiness/M07/completeness program documents are archived and are not current product truth. Current mutable status belongs in this tracker.
