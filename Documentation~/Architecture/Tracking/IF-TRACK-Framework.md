# IF-TRACK — Immersive Framework

Status: Active  
Last updated: 2026-08-07  
Package version: `1.0.0-preview.17`

## Source baseline

```text
com.immersive.framework
  c457e8cd7a11b8f2ce816734b4d97a3a820b4eec
  IF-TXN-03A

QAFramework
  c99df1e77a8408e6b48124a5d371f09e9af52019
  IF-TXN-03A

FIRSTGAME
  ab1bfe65c09af8988c2fe21ce06db780fe12aa70
  Demo03Etapa04
```

The QA evidence below includes manual Unity Play Mode/Edit Mode executions on 2026-08-07 against the IF-TXN-03A package/QA workspaces.

## Summary

The package has one internal application/session composition root and explicit feature runtime ports. Product areas include lifecycle, Player, Camera, Pause/Input/Gate, Reset, Activity readiness, Loading/Transition foundations, persistence foundations and diagnostics.

`FrameworkRuntimeHost` is Internal. Required runtime dependencies are supplied through typed bindings and fail explicitly when unavailable. There is no public static host registry or service-locator API.

Documentation topology:

```text
Guides/                 product usage
Architecture/ADRs/      normative decisions
Architecture/Tracking/  this board; mutable current status
Architecture/Archive/   historic audits, certifications and closed execution records
Architecture/Plans/     open plans only
```

## Track board

| Track | Real status | Proven coverage | Pending work | Next action |
|---|---|---|---|---|
| Runtime authority | Closed for current boundary | Internal host composition; narrow typed ports | preserve boundary | reject static/global lookup |
| Package hygiene | Closed for current boundary | package + QA import discipline | ongoing | do not restore compatibility facades |
| Player — Scene-Provided | **Closed / approved** | authoring, Route Primary Scene admission, Slot, Host, Actor adoption, readiness, release, reentry, Activity Restart, teardown | broader automated admit/release matrix desirable | preserve baseline |
| PLAYER-DIAG-1 | **Closed / approved** | invalid-evidence formatting, safe token text, immutable last-operation snapshot, Advanced/Debug projection | optional automated matrix | preserve semantics |
| Player — Manager-Provisioned | **Package Experimental** | `LocalPlayerProvisioningAuthoring` + runtime + Editor inspectors exist | canonical public command/observation surface and consumer hardening | IF-ADR-015 remains primary Player product gap |
| Player — Session-Persistent | **Blocked** | origin reserved in architecture | authoring, admission and lifetime contracts | runtime currently rejects; needs approved package cut |
| Activity readiness + reveal | **Implemented (Experimental); current core QA re-certified** | WaitVisible/WaitCovered 42/42; terminal recovery 34/34; startup parity Route 25/25 + Game Application 20/20; IF-TXN-03A separation 16/16 | ObserveOnly-focused negatives, Player public-only waiting/joining matrix, product guidance | preserve semantics; add focused coverage only when needed |
| WaitCovered Loading progress | **Implemented (Experimental); current core QA re-certified** | participant-aware progress 32/32; terminal/failure 34/34; WaitCovered integration 42/42; startup parity 25/25 + 20/20 | public-only WaitingForJoin proof, product presentation guidance, Advanced/Debug polish | preserve aggregate-only Loading authority boundary |
| Camera | Closed for current single-output scope | persistent output, Player request/restoration; Stable product + Internal output authority | split-screen / multiple outputs | preserve one-output boundary |
| Pause / Input / Gate | Closed for current single-player scope; product hardening remains | Player-bound Pause, resume, input restoration; IF-TXN-03A clarifies pure Transition Gate vs readiness recovery | broader negative gate/pause/reset matrix | preserve declared gate authority model |
| Reset | Implemented (mostly Experimental) | Object Reset, Group Reset, Cycle Reset, Activity Restart | unload `update-retry` recomposition finding | separate Reset lifecycle cut |
| Activity transaction | **IF-TXN-01 + IF-TXN-02 + IF-TXN-03A CLOSED / CERTIFIED** | IF-TXN-03A 16/16; IF-TXN-02 16/16; IF-TXN-01 22/22; readiness/loading/startup compatibility green | consumer/loading hook exception after commit; disposal during partial presentation; concrete adapter compensation/cleanup receipts | select one concrete exceptional path; no generic transaction manager |
| Persistence / ProgressionSave | Foundation | contracts and store exist | product authoring and real consumer proof | product decision |
| ObjectEntry / Local visibility | Implemented (Experimental) | adapters and declarations exist | dedicated product guides deferred | keep advanced/foundation |
| Editor product surface (IF-ADR-010) | **Proposed** | Editor-Authoring-Standard guide + many Custom Editors | accept ADR and finish migration | keep guide as usage slice |
| ADR-012 participation profile | **Accepted / substantially implemented** | requirement levels, evidence and runtime compatibility | product/QA consolidation | preserve contract; continue product hardening |
| Authored identity (IF-ADR-014) | **Closed / approved for current boundary** | IF-ID-02..06 package + tests; canonical QA 6/6; FIRSTGAME IF-ID-08 duplication/remediation proof | IF-ID-07 deferred by design | preserve exact-reference + token authority |
| IF-ADR-015 provisioning command/observation surface | **Proposed / 30%** | ADR and consumer prototype exist | canonical package commands, immutable observations, authoring, QA, FIRSTGAME migration | next major Player product cut |

## Transaction authority closure record

### IF-TXN-01 — CLOSED

Closed for canonical Game Application startup, Route request and Activity request transition authority.

### IF-TXN-02 — CLOSED

Closed for Activity Clear and Activity Restart transition-authority parity.

### IF-TXN-03A — CLOSED / CERTIFIED

Closed for Transition Gate release terminal integrity and current-state projection semantics.

Canonical gate model:

```text
Transition Gate
  internal GameFlow operation state
  not an external acquired resource
  canonical release = unconditional internal state replacement
  no external release-refusal/ownership-token contract

TransitionGateSnapshot
  pure Transition Gate state

CurrentTransitionGateMode
  pure Transition Gate mode

ActivityEntryReadinessGateSnapshot
  Transition Gate + Activity Entry Readiness Recovery Gate

CurrentGateSnapshot
  broader operational composition, including Pause + readiness composition
```

Critical certified state:

```text
Transition Gate released
Readiness Recovery Gate blocked

TransitionGateSnapshot.HasBlockers == false
CurrentTransitionGateMode == None
ActivityEntryReadinessGateSnapshot.HasBlockers == true
```

This is intentional recovery authority, not a leaked Transition Gate.

### Combined normative transaction rule

```text
Transition Before not accepted
-> abort before governing lifecycle mutation
-> previous authority remains
-> typed pre-commit Transition failure

Transition After not accepted after commit
-> preserve authority that actually committed
-> operation is not success
-> no blind rollback
-> typed committed-target reveal failure

Clear post-commit
-> CurrentActivity remains None
-> previous Activity is not recreated

Restart post-commit
-> re-entered Activity/new occurrence remains authoritative
-> old occurrence is not restored
-> Restart is not Completed on reveal failure

Terminal Transition cleanup
-> pure Transition Gate is released before terminal observation
-> readiness recovery may remain independently active
```

## Canonical QA certification

```text
IF-TXN-03A Transition Gate Terminal Integrity
  PASS — 16/16

IF-TXN-02 Clear/Restart Transition Authority
  PASS — 16/16

IF-TXN-01 Transition Failure Authority
  PASS — 22/22

Participant-Aware Readiness Loading Terminal
  PASS — 34/34

Direct Activity Readiness Policies
  PASS — 42/42
  WaitVisible PASS
  WaitCovered PASS

Participant-Aware Readiness Loading Progress
  PASS — 32/32

Participant-Aware Startup Parity — Route
  PASS — 25/25

Participant-Aware Startup Parity — Game Application
  PASS — 20/20
```

The terminal suite intentionally emits a runtime error for the deliberate required-participant failure case; its runner ends `Passed`. That path proves `gateReleased=true` together with `recoveryGate=true`, then proves full cleanup.

IF-TXN-01/02/03A are **COMPLETE** for the currently approved transaction/gate boundary. Reopen only with new evidence that contradicts these certified contracts.

## IF-ID closure record

Normative decision remains:

- IF-ADR-014 is Accepted.
- Exact Route/Activity asset reference is authored-definition authority.
- `RouteId` / `ActivityId` remain stable boundary/diagnostic evidence.
- Route/Activity operational owners require a `RuntimeDefinitionToken`.
- Occurrence/readiness/supersession authority remains definition/occurrence scoped.
- Stable-ID collision is diagnosable and explicitly repairable; it is not runtime equality.

IF-ID-07 remains deferred until a real persistence/external boundary requires an application-scoped stable-ID resolver.

## Closed finding — WaitCovered + externally-driven Player progression

Accepted interpretation remains:

```text
Logical Actors Prepared / Player requirement
+ WaitCovered
+ Explicit Slot not yet Joined
-> Required Player contribution may legitimately remain Preparing / WaitingForJoin
-> Loading remains below successful terminal completion
-> WaitCovered remains covered
```

If the only Join/progression control is inside covered destination gameplay, the composition creates a control-plane dependency cycle. The framework does not repair this through fake readiness, timeout, auto-Join, false Loading completion or premature reveal.

## Open findings

### Exceptional transaction residuals after IF-TXN-03A

The generic Transition Gate release/leak suspicion is closed. Current canonical release is internal state cleanup and has no fallible external release protocol.

Remaining candidates must be concrete:

```text
consumer/loading hook exception after commit
disposal during partial presentation
adapter partial-side-effect compensation
full terminal cleanup receipts / diagnostic correlation
```

Do not fold these into a generic transaction manager without concrete evidence.

### Reset unload retry

Observed sequence remains:

```text
SceneReleasing unregister
-> update-retry register
-> on-disable unregister
```

Non-blocking in current Player tests; possible transient recomposition during unload; requires its own Reset lifecycle cut.

### Missing canonical Slot–Host–Actor assignment snapshot

No single authority currently answers which Logical Player, Host and Actor occupy a `PlayerSlotId` under which owner/scope/lifetime. Truth remains split across participation, preparation, scene admission, occupancy and capability tokens.

### Session-Persistent package gap

Architecture accepted in IF-ADR-003; product workflow unavailable. Do not simulate with an unscoped Persistent Content prefab.

## Current execution priority

```text
IF-TXN-01
  CLOSED

IF-TXN-02
  CLOSED

IF-TXN-03A
  CLOSED / CERTIFIED
  no CUT-03 required
  no FIRSTGAME proof required for this technical boundary

Next exceptional terminal-integrity audit
  choose one concrete unresolved path:
    consumer/loading hook exception after commit
    disposal during partial presentation
    adapter partial-side-effect compensation
    full terminal cleanup/correlation receipts
  do not reopen generic Transition Gate release failure without new evidence
  do not introduce generic rollback/transaction manager by default

IF-ADR-015
  canonical Player provisioning command/observation surface
  remains the major Player product gap

Session-Persistent Player
  blocked by package

Reset unload recomposition
  separate lifecycle finding

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

### IF-TXN-03A

Closed 2026-08-07 after package implementation, focused regression, QA compatibility update and manual Unity certification. Reopen only with evidence that a terminal can expose an active pure Transition Gate, that `CurrentTransitionGateSnapshot` again includes readiness recovery, or that the canonical internal release can fail/refuse cleanup under an actual modeled contract.

### IF-TXN-02

Closed 2026-08-07 after package implementation plus canonical QA certification. Reopen only with evidence that Clear or Restart can cross a non-accepted Transition Before, report success after a non-accepted After, or restore authority that did not actually remain committed.

### IF-TXN-01

Closed 2026-08-07 for the approved canonical GameFlow boundary. Do not reopen merely because another subsystem has an unrelated lifecycle bug.

### IF-ID

Closed 2026-08-07 for the current boundary. Reopen only with concrete identity-authority evidence.

### PLAYER-DIAG-1

Host-scoped last-operation diagnostics, safe invalid-evidence formatting and manual restart/teardown regression are approved. Do not reopen without new evidence.

## Historic programs

Large readiness/M07/completeness program documents are historic planning context and are not current product truth. Current mutable status belongs in this tracker.
