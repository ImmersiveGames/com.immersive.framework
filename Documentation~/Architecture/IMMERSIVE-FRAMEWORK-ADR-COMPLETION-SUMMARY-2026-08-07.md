# Immersive Framework — ADR Completion Summary

Date: 2026-08-07  
Package Git baseline: `193e7e954deaa430920f7967b5061b4b950ed1bb` (`IF-TXN-02`)  
QA baseline: `cf3cf625260ff717d6bcc919703e6868b085285f` (`IF-TXN-02`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Portfolio average: **80.5% equivalent**

> Portfolio arithmetic treats IF-ADR-014 `Complete for current accepted scope` as
> 100% for the accepted scope. IF-ID-07 remains explicitly deferred by design and
> does not block IF-ADR-014 closure.

## Important baseline changes

1. Package HEAD is now `193e7e9` (`IF-TXN-02`) and QAFramework HEAD is `cf3cf62` (`IF-TXN-02`).
2. **IF-TXN-01 — GameFlow Transition Failure Authority remains closed** for Game Application startup, Route request and Activity request.
3. **IF-TXN-02 — Activity Clear/Restart Transition Authority Parity is now closed and technically certified.**
4. The same Transition acceptance rule now governs Start/Route/Activity/Clear/Restart: `TransitionResult.Completed` or intentional policy/no-visual `Skipped` may continue; required `Failed`/`Rejected`/`Cancelled`/invalid outcomes may not.
5. Clear `Before` failure aborts before clear; Clear `After` failure after commit keeps `CurrentActivity=None`, returns non-success and never restores the removed Activity.
6. Restart `Before` failure performs neither Clear nor Re-enter; Restart `After` failure after re-enter keeps the new Activity/occurrence authoritative, returns non-completion and never restores the previous occurrence.
7. No transaction manager, generic rollback, retry or silent fallback was introduced.
8. Canonical QA evidence is green: IF-TXN-02 16/16, IF-TXN-01 22/22, Direct Activity Readiness 42/42, participant-aware Loading terminal 34/34, participant-aware Loading progress 32/32, post-transition readiness PASS, Identity Authority 6/6.
9. WaitVisible/WaitCovered, participant-aware Loading, occurrence mutation, identity ownership, supersession and successful cleanup Clear paths remain non-regressed.
10. FIRSTGAME remains at `ab1bfe6`; no FIRSTGAME change is required for IF-TXN-02 technical closure.

## Method

Each percentage considers:

```text
Normative contract and architectural clarity   20%
Runtime implementation                         30%
Authoring, diagnostics and documentation        20%
Current QA evidence                             15%
Current FIRSTGAME consumer evidence             15%
```

The percentages are planning estimates. They are deliberately reduced when code is present but the current QA harness, product surface, or consumer proof is incomplete.

For IF-ADR-014 only, the official state is no longer expressed as an implementation percentage: it is `Complete for current accepted scope`. The portfolio arithmetic therefore uses **100% for the accepted scope** while retaining IF-ID-07 as a deferred, non-blocking future boundary.

## ADR matrix

| ADR | Decision | Normative status | Completion | Current classification |
|---|---|---:|---:|---|
| IF-ADR-001 | Core Lifecycle and Runtime Authority | Accepted | **91%** | IF-TXN-01/02 implemented and QA-certified; Session-Persistent Player and exceptional terminal cleanup/compensation residuals remain |
| IF-ADR-002 | Product Authoring Model | Accepted | **65%** | Partially implemented across the product portfolio |
| IF-ADR-003 | Player Participation and Actor Lifecycle | Accepted | **84%** | Runtime substantially implemented; product and hardening gaps remain; covered-readiness control-plane boundary and authoring warning are present |
| IF-ADR-004 | Camera Requests and Output Authority | Accepted | **78%** | Core runtime implemented; isolated product proof incomplete |
| IF-ADR-005 | Input, Pause, Gate and Reset | Accepted | **76%** | Integrated runtime exists; product extraction and negative coverage incomplete |
| IF-ADR-006 | Loading, Transition, Persistence and Diagnostics | Accepted | **92%** | IF-TXN-01/02 implemented and QA-certified; exceptional gate/presentation cleanup and product-template gaps remain |
| IF-ADR-007 | Activity Entry Readiness and Reveal Gating | Accepted | **96%** | Runtime contract complete; WaitVisible/WaitCovered and post-transition QA re-certified; focused ObserveOnly/Player public-only matrix remains |
| IF-ADR-008 | Persistent Application Content Composition | Accepted | **90%** | Product model implemented; portfolio expansion and QA remain |
| IF-ADR-009 | Activity Local Visibility Rules | Accepted | **88%** | Runtime integrated; authoring and regression polish remain |
| IF-ADR-010 | Editor and Inspector Product Surface Authority | Proposed | **70%** | Broad foundation exists; decision not fully accepted or consistently applied |
| IF-ADR-011 | Participant-Aware Activity Readiness Loading Progress | Accepted | **92%** | Runtime complete; canonical progress/terminal QA re-certified; public-only waiting/joining and product presentation parity remain |
| IF-ADR-012 | Activity Player Participation Profile and Readiness Compatibility | Accepted | **90%** | Contract and runtime implemented; product/QA consolidation remains |
| IF-ADR-013 | Optional Audio BGM Adapter | Accepted / Experimental | **65%** | Technical adapter exists; product promotion incomplete |
| IF-ADR-014 | Authored Definition and Stable Identity Authority | **Accepted** | **100%*** | **Complete for current accepted scope; IF-ID closed; IF-ID-07 deferred by design** |
| IF-ADR-015 | Player Provisioning Commands and Consumer Observation Surface | Proposed | **30%** | ADR and consumer prototype exist; official package surface not shipped |

`*` IF-ADR-014 uses 100% only for portfolio arithmetic. Its official ADR wording is `Complete for current accepted scope`, not a numeric completion claim.

The portfolio moves from 80.3% to 80.5% because two explicit runtime/contract residuals were actually closed and certified (`IF-ADR-001` 90→91 and `IF-ADR-006` 91→92). This is not a score increase merely for executing additional smokes.

## IF-TXN-02 closure incorporated

### Supported transaction-authority boundary

```text
Game Application startup
Route request
Activity request
Activity Clear
Activity Restart
```

### Canonical rule

```text
Before not accepted
→ do not advance the governing lifecycle mutation
→ preserve previous committed authority
→ typed pre-commit transition failure

After not accepted after commit
→ preserve the authority that actually committed
→ operation is not success
→ typed committed-target reveal failure
→ no blind rollback
```

### Clear authority

```text
Before failure
→ ClearActivity lifecycle not called
→ previous Activity remains authority

After failure after Clear commit
→ CurrentActivity=None remains authority
→ previous Activity not restored
→ request not Succeeded
```

### Restart authority

```text
Before failure
→ no Clear
→ no Re-enter
→ previous Activity/occurrence remain authority

Re-enter committed + After failure
→ new Activity/occurrence remain authority
→ Restart not Completed
→ old occurrence not restored
```

### Result vocabulary

No new transition-failure kinds were required. IF-TXN-02 reuses:

```text
FailedPreCommitTransition
FailedCommittedTargetReveal
```

`FrameworkActivityRequestResult` factories carry `GameFlowRequestOperationKind` so Clear is distinguishable as `ActivityClear` without duplicating the terminal vocabulary.

## Certification matrix

```text
IF-TXN-02 Clear/Restart Transition Authority Regression
  PASS — 16/16

IF-TXN-01 Transition Failure Authority Regression
  PASS — 22/22

Direct Activity Readiness Policies Regression
  PASS — 42/42
  WaitVisible PASS
  WaitCovered PASS

Participant-Aware Readiness Loading Terminal Regression
  PASS — 34/34

Participant-Aware Readiness Loading Progress Regression
  PASS — 32/32

Activity Readiness Post-Transition Smoke
  PASS
  ReadyToNotReady
  NotReadyToReady
  IdenticalValueIgnored
  newRequest=False

Identity Authority Regression
  PASS — 6/6
  failed=0
```

The participant-aware terminal suite intentionally emits an error record for its required-participant failure case. This is expected negative evidence; its final status is `Passed`, the committed destination remains authoritative, Loading does not falsely complete, and recovery protection remains retained.

A dedicated host Play Mode failing Transition adapter for Clear/Restart remains optional hardening. It is not required for the current IF-TXN-02 technical certification because the focused authority regression plus Play Mode non-regression matrix already establish the supported contract boundary.

## IF-ID closure remains incorporated

The canonical authority model remains:

| Dimension | Authority |
|---|---|
| Exact authored definition | exact typed `RouteAsset` / `ActivityAsset` reference |
| Stable boundary identity | `RouteId` / `ActivityId` |
| Runtime occurrence | definition reference + occurrence / revision / sequence |
| Operational ownership | scoped owner + `RuntimeDefinitionToken` |
| Presentation | display name only |

IF-ID remains closed for the current accepted scope. IF-ID-07 stays deferred until a real persistence/external boundary requires an application-scoped stable-ID resolver.

## WaitCovered + Player readiness decision remains unchanged

A valid Explicit Player Slot may remain `Preparing / WaitingForJoin` while `WaitCovered` retains cover. If the only Join/progression control exists inside the covered destination, that is a product/control-plane dependency cycle, not a reason to weaken readiness or Loading semantics.

The framework continues to reject fake Ready, timeout-to-success, silent auto-Join, false Loading completion and premature reveal. The package authoring warning remains the correct advisory product response.

## Priority order

### Closed — transaction authority

- **IF-TXN-01:** closed for startup/Route/Activity request authority.
- **IF-TXN-02:** closed for Activity Clear/Restart authority parity.
- Combined supported paths preserve real committed authority and reject false success after non-accepted Transition phases.

### P0 — next focused architecture cut

- **IF-ADR-001 / IF-ADR-006 exceptional terminal integrity:** audit one concrete path before implementation. Current candidates are transition/gate-release failure, consumer/loading hook exception after commit, disposal during partial presentation, adapter partial-side-effect compensation, or full terminal cleanup receipts.
- Do **not** create a generic transaction/rollback manager without concrete evidence.
- **IF-ADR-015 — 30%:** remains the major Player product gap and can follow the next focused terminal-integrity decision depending on priority.

### P1 — product authoring consistency

- **IF-ADR-002 — 65%:** apply the product model consistently beyond mature composers.
- **IF-ADR-010 — 70%:** standardize guided creation, remediation, receipts, Advanced/Debug and contextual cross-domain warnings.
- Preserve the WaitCovered/Player warning as advisory product guidance, not runtime policy.

### P2 — remaining hardening

- IF-ADR-003 Player provisioning hardening, Leave/disconnect boundaries and public-only QA.
- IF-ADR-004 Camera priority/release/override negative matrix.
- IF-ADR-005 Gate/Pause/Reset/Restart exceptional cleanup matrix.
- IF-ADR-007 focused ObserveOnly and Player waiting/joining/replacement matrix.
- IF-ADR-011 public-only waiting/joining proof, startup/product presentation parity and Advanced/Debug diagnostics.

### P3 — product demonstrations and promotion

- Dedicated Player Camera, Camera Override, Reset/Restart, Pause and Transition/Loading demonstrations.
- Add FIRSTGAME demonstrations only where they improve product understanding; do not use FIRSTGAME as technical certification.
- IF-ADR-013 BGM FIRSTGAME demonstration and promotion decision.

## Portfolio interpretation

```text
Core runtime architecture
  strong and mostly implemented

Transaction authority
  IF-TXN-01 CLOSED
  IF-TXN-02 CLOSED
  supported boundary = Start/Route/Activity/Clear/Restart
  focused + non-regression QA green
  next gaps are exceptional cleanup/compensation paths, not Clear/Restart authority

Stable identity / IF-ID
  closed for current accepted scope
  Identity Authority 6/6 PASS
  IF-ID-07 deferred by design

Readiness/reveal
  WaitVisible + WaitCovered 42/42 PASS
  post-transition readiness PASS

Participant-aware Loading
  progress 32/32 PASS
  terminal/failure 34/34 PASS
  Loading remains projection, not lifecycle/readiness authority

Player lifecycle
  technically strong
  canonical consumer command/observation surface still missing (IF-ADR-015)

QA
  IF-TXN-01/02 regressions live in QAFramework, not package runtime
  current transaction/readiness/loading/identity evidence is green
  QA menu consolidation remains separate governance work

FIRSTGAME
  current consumer baseline remains ab1bfe6
  continues to provide real product-composition evidence
```
