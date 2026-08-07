# IF-TRACK — Immersive Framework

Status: Active  
Last updated: 2026-08-07  
Package version: `1.0.0-preview.17`

## Source baseline

```text
com.immersive.framework
  193e7e954deaa430920f7967b5061b4b950ed1bb
  IF-TXN-02

QAFramework
  cf3cf625260ff717d6bcc919703e6868b085285f
  IF-TXN-02

FIRSTGAME
  ab1bfe65c09af8988c2fe21ce06db780fe12aa70
  Demo03Etapa04
```

The QA evidence below includes manual Play Mode and Edit Mode executions on 2026-08-07 against the IF-TXN-02 package/QA workspaces.

## Summary

The package has one internal application/session composition root and explicit feature runtime ports. Product areas include lifecycle, Player, Camera, Pause/Input/Gate, Reset, Activity readiness, Loading/Transition foundations, persistence foundations and diagnostics.

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
| Player — Scene-Provided | **Closed / approved** | authoring, Route Primary Scene admission, Slot, Host, Actor adoption, readiness, release, reentry, Activity Restart, teardown | broader automated admit/release matrix desirable | preserve baseline |
| PLAYER-DIAG-1 | **Closed / approved** | invalid-evidence formatting, safe token text, immutable last-operation snapshot, Advanced/Debug projection | optional automated matrix | preserve semantics |
| Player — Manager-Provisioned | **Package Experimental** | `LocalPlayerProvisioningAuthoring` + runtime + Editor inspectors exist | canonical public command/observation surface and consumer hardening | IF-ADR-015 remains primary Player product gap |
| Player — Session-Persistent | **Blocked** | origin reserved in architecture | authoring, admission and lifetime contracts | runtime currently rejects; needs approved package cut |
| Activity readiness + reveal | **Implemented (Experimental); current core QA re-certified** | WaitVisible/WaitCovered 42/42; post-transition PASS; supersession/identity 6/6; authoring warning for covered control-plane risk | ObserveOnly-focused negatives, Player public-only waiting/joining matrix, product guidance | preserve semantics; add focused coverage only when needed |
| WaitCovered Loading progress | **Implemented (Experimental); current core QA re-certified** | participant-aware progress 32/32; terminal/failure 34/34; WaitCovered integration 42/42 | public-only WaitingForJoin proof, startup/product presentation parity, Advanced/Debug polish | preserve aggregate-only Loading authority boundary |
| Camera | Closed for current single-output scope | persistent output, Player request/restoration; Stable product + Internal output authority | split-screen / multiple outputs | preserve one-output boundary |
| Pause / Input / Gate | Closed for current single-player scope | Player-bound Pause, resume, input restoration; Stable product surfaces | multiplayer policy | preserve explicit binding |
| Reset | Implemented (mostly Experimental) | Object Reset, Group Reset, Cycle Reset, Activity Restart | unload `update-retry` recomposition finding | separate Reset lifecycle cut |
| Activity transaction | **IF-TXN-01 + IF-TXN-02 closed for Start/Route/Activity/Clear/Restart authority** | IF-TXN-02 16/16; IF-TXN-01 22/22; readiness/loading/identity non-regressions green | gate-release failure, partial-presentation cleanup, concrete compensation/cleanup receipts | audit next exceptional terminal path; no generic transaction manager |
| Persistence / ProgressionSave | Foundation | contracts and store exist | product authoring and real consumer proof | product decision |
| ObjectEntry / Local visibility | Implemented (Experimental) | adapters and declarations exist | dedicated product guides deferred | keep advanced/foundation |
| Editor product surface (IF-ADR-010) | **Proposed** | Editor-Authoring-Standard guide + many Custom Editors | accept ADR and finish migration | keep guide as usage slice |
| ADR-012 participation profile | **Accepted / substantially implemented** | requirement levels, evidence and runtime compatibility | product/QA consolidation | preserve contract; continue product hardening |
| Authored identity (IF-ADR-014) | **Closed / approved for current boundary** | IF-ID-02..06 package + tests; canonical QA 6/6; FIRSTGAME IF-ID-08 duplication/remediation proof | IF-ID-07 deferred by design | preserve exact-reference + token authority |
| IF-ADR-015 provisioning command/observation surface | **Proposed / 30%** | ADR and consumer prototype exist | canonical package commands, immutable observations, authoring, QA, FIRSTGAME migration | next major Player product cut |

## Transaction authority closure record

### IF-TXN-01

Closed for the canonical Game Application startup, Route request, and Activity request boundary.

### IF-TXN-02

Closed for Activity Clear and Activity Restart authority parity.

Normative transaction rule across the combined supported boundary:

```text
Transition Before not accepted
→ abort before the governing lifecycle mutation
→ previous authority remains
→ typed pre-commit Transition failure

Transition After/reveal not accepted after commit
→ preserve the authority that actually committed
→ operation is not success
→ no blind rollback
→ typed FailedCommittedTargetReveal
→ apply reveal recovery only when a valid Activity occurrence remains authoritative

Clear-specific post-commit authority
→ CurrentActivity remains None
→ previous Activity is never recreated as rollback

Restart-specific post-commit authority
→ re-entered Activity / new occurrence remains authoritative
→ old occurrence is never restored
→ Restart is not Completed on reveal failure

CompletedWithWarnings
→ accepted through TransitionResult.Completed

Intentional policy/no-visual Skipped
→ accepted

Required Failed/Rejected/Cancelled/invalid
→ not accepted
```

Canonical QA evidence:

```text
IF-TXN-02 Clear/Restart Transition Authority
  Passed 16/16

IF-TXN-01 Transition Failure Authority
  Passed 22/22

Direct Activity Readiness Policies
  Passed 42/42
  WaitVisible Passed
  WaitCovered Passed

Participant-Aware Readiness Loading Terminal
  Passed 34/34

Participant-Aware Readiness Loading Progress
  Passed 32/32

Activity Readiness Post-Transition
  Passed
  newRequest=False

Identity Authority Regression
  Passed 6/6
  failed=0
```

IF-TXN-01 and IF-TXN-02 are **COMPLETE** for the currently approved transaction-authority boundary. A dedicated host Play Mode deliberate failing Transition adapter for Clear/Restart remains optional hardening rather than a closure blocker.

## IF-ID closure record

Normative decision:

- `IF-ADR-014 — Authored Definition and Stable Identity Authority` is Accepted.
- Exact Route/Activity asset reference is authored-definition authority.
- `RouteId` / `ActivityId` remain stable boundary/diagnostic evidence.
- Route/Activity operational owners require a `RuntimeDefinitionToken`.
- Occurrence/readiness/supersession authority remains definition/occurrence scoped.
- Stable-ID collision is diagnosable and explicitly repairable; it is not runtime equality.

QA proof remains:

```text
status Passed
6/6 completed
failures none
cleanup none
teardown none
```

Deferred:

```text
IF-ID-07
  application-scoped stable-ID resolver
  open only when a real save/external boundary requires it
```

## Closed finding — WaitCovered + externally-driven Player progression

The high-risk readiness audit is closed for the reported causal issue.

Accepted interpretation:

```text
Logical Actors Prepared / Player requirement
+ WaitCovered
+ Explicit Slot not yet Joined
→ Required Player contribution may legitimately remain Preparing / WaitingForJoin
→ Loading remains below successful terminal completion
→ WaitCovered remains covered
```

If the only Join/progression control is inside covered destination gameplay, the composition creates a control-plane dependency cycle. The framework does not repair this through fake readiness, timeout, auto-Join, false Loading completion or premature reveal.

Package authoring warns on the known risk combination. The product direction is to keep control-plane operations reachable through pre-entry, automatic, persistent/external, or intentionally visible (`WaitVisible`) composition.

## Open findings

### Transaction residuals after IF-TXN-02

Clear/Restart authority is no longer an open residual. Separate future audit/cut candidates are:

```text
transition/gate release failure
consumer/loading hook exception after commit
disposal during partial presentation
adapter partial-side-effect compensation
full terminal cleanup receipts
```

Do not fold these into a generic transaction manager without concrete evidence.

### Reset unload retry

Observed sequence:

```text
SceneReleasing unregister
→ update-retry register
→ on-disable unregister
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
  CLOSED for Activity Clear/Restart authority parity

ADR-001 / ADR-006 exceptional terminal integrity
  next focused audit should select one concrete path:
    transition/gate-release failure
    consumer/loading hook exception after commit
    disposal during partial presentation
    adapter partial-side-effect compensation / terminal cleanup receipts
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

### IF-TXN-02

Closed 2026-08-07 after package implementation plus canonical QA certification: focused IF-TXN-02 16/16, IF-TXN-01 22/22, Direct Readiness 42/42, terminal Loading 34/34, progress 32/32, post-transition readiness PASS, Identity 6/6. Reopen only with evidence that Clear or Restart can again cross a non-accepted Transition Before, report success after a non-accepted After, or restore authority that did not actually remain committed.

### IF-TXN-01

Closed 2026-08-07 for the approved canonical GameFlow boundary. Do not reopen merely because another subsystem has an unrelated lifecycle bug.

### IF-ID

Closed 2026-08-07 for the current boundary. Reopen only with concrete identity-authority evidence.

### PLAYER-DIAG-1

Host-scoped last-operation diagnostics, safe invalid-evidence formatting and manual restart/teardown regression are approved. Do not reopen without new evidence.

## Historic programs

Large readiness/M07/completeness program documents are historic planning context and are not current product truth. Current mutable status belongs in this tracker.
