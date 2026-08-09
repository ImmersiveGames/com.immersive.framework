# Immersive Framework — ADR Completion Summary

Date: 2026-08-09  
Package Git baseline: `cf0a37fbcbf72ad2a08556d6045c908521bfd2c1` (`P4 — IF-PLAYER-SURFACE-06`)  
QA Git baseline inspected: `52a31aa9cd237d934ed3241392b87b7990f11dc8` (`fix2`); certification evidence: local Unity Play Mode run on 2026-08-09  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Portfolio average: **84.6% equivalent**

> Portfolio arithmetic treats IF-ADR-014 `Complete for current accepted scope` as 100% for its accepted scope. IF-ID-07 remains explicitly deferred and does not block that closure. Percentages are planning estimates, not release scores.

## Important baseline changes

1. Package Git baseline is `cf0a37f` (`P4 — IF-PLAYER-SURFACE-06`) and QAFramework Git baseline inspected is `52a31aa` (`fix2`).
2. IF-TXN-01 remains closed for Game Application startup, Route request and Activity request transition authority.
3. IF-TXN-02 remains closed for Activity Clear/Restart transition-authority parity.
4. **IF-TXN-03A — Transition Gate Release Terminal Integrity is now CLOSED / CERTIFIED.**
5. `TransitionGateSnapshot` now represents only the GameFlow Transition Gate; readiness recovery is exposed through `ActivityEntryReadinessGateSnapshot` and the broader operational composition remains available through `CurrentGateSnapshot`.
6. The canonical Transition Gate is internal GameFlow state. Its release is unconditional internal state replacement; there is no external lease/release refusal contract to compensate.
7. A committed-target readiness failure may validly have `Transition Gate released + Readiness Recovery active`; this is now directly certified and no longer misreported as Transition Gate leakage.
8. Existing IF-TXN-01/02 authority semantics remain green.
9. Readiness/Loading compatibility is green across terminal failure, Direct WaitVisible/WaitCovered, participant-aware progress and both startup parity paths.
10. No generic transaction manager, rollback manager, release token or silent fallback was introduced.
11. FIRSTGAME change is not required for IF-TXN-03A technical closure.
12. **IF-ADR-016 has progressed through package implementation, designer authoring and QA closure:** IF-SESSION-CONFIG-05 is 6/6 PASS and IF-SESSION-CONFIG-07 is 17/17 PASS.
13. **IF-SESSION-CONFIG-05B is now closed and QA-certified 4/4:** the complete creation-time Session Profile override replaces the GameApplication default, does not field-merge and does not fall back after an invalid explicit override.
14. IF-ADR-016 remains **Proposed** because FIRSTGAME manual consumer proof is deferred and full Route/Activity non-reapplication is not yet directly certified through real integration.
15. **IF-ADR-015 P1–P4 are shipped in the official package:** scoped access, immutable observation, command authoring and status/diagnostics binding.
16. **QA-PLAYER-SURFACE-01 is behaviorally certified in Unity Play Mode — PASS 29/29.**
17. **QA-PLAYER-SURFACE-02 is behaviorally certified in Unity Play Mode — PASS 36/36.**
18. Joint certification ended `PLAYER SURFACE QA CERTIFIED`; no package runtime gap remained.
19. IF-ADR-015 remains **Proposed** because FIRSTGAME real-consumer proof, post-FIRSTGAME P5 creation-workflow disposition and final product documentation/acceptance remain. P5 does not mandate a Wizard or Composer.

## Method

Each percentage considers:

```text
Normative contract and architectural clarity   20%
Runtime implementation                         30%
Authoring, diagnostics and documentation        20%
Current QA evidence                             15%
Current FIRSTGAME consumer evidence             15%
```

Percentages are deliberately reduced when runtime code exists but current QA, product surface, diagnostics, documentation or consumer proof is incomplete.

## ADR matrix

| ADR | Decision | Normative status | Completion | Current classification |
|---|---|---:|---:|---|
| IF-ADR-001 | Core Lifecycle and Runtime Authority | Accepted | **92%** | IF-TXN-01/02/03A implemented and QA-certified; Session-Persistent Player and exceptional post-commit cleanup/compensation residuals remain |
| IF-ADR-002 | Product Authoring Model | Accepted | **65%** | Partially implemented across the product portfolio |
| IF-ADR-003 | Player Participation and Actor Lifecycle | Accepted | **84%** | Runtime substantially implemented; product and hardening gaps remain |
| IF-ADR-004 | Camera Requests and Output Authority | Accepted | **78%** | Core runtime implemented; isolated product proof incomplete |
| IF-ADR-005 | Input, Pause, Gate and Reset | Accepted | **78%** | Gate authority taxonomy clarified; IF-TXN-03A Transition Gate semantics certified; broader product/negative matrix remains |
| IF-ADR-006 | Loading, Transition, Persistence and Diagnostics | Accepted | **94%** | IF-TXN-01/02/03A certified; remaining gaps are concrete post-commit exception/compensation diagnostics and product templates |
| IF-ADR-007 | Activity Entry Readiness and Reveal Gating | Accepted | **96%** | Runtime contract complete; WaitVisible/WaitCovered, terminal recovery, startup parity and gate/recovery separation certified |
| IF-ADR-008 | Persistent Application Content Composition | Accepted | **90%** | Product model implemented; portfolio expansion and QA remain |
| IF-ADR-009 | Activity Local Visibility Rules | Accepted | **88%** | Runtime integrated; authoring and regression polish remain |
| IF-ADR-010 | Editor and Inspector Product Surface Authority | Proposed | **70%** | Broad foundation exists; decision not fully accepted or consistently applied |
| IF-ADR-011 | Participant-Aware Activity Readiness Loading Progress | Accepted | **94%** | Runtime complete; progress/terminal/startup parity/gate separation plus Player public WaitingForJoin/WaitCovered path certified; presentation guidance remains |
| IF-ADR-012 | Activity Player Participation Profile and Readiness Compatibility | Accepted | **90%** | Contract and runtime implemented; product/QA consolidation remains |
| IF-ADR-013 | Optional Audio BGM Adapter | Accepted / Experimental | **65%** | Technical adapter exists; product promotion incomplete |
| IF-ADR-014 | Authored Definition and Stable Identity Authority | **Accepted** | **100%*** | **Complete for current accepted scope; IF-ID closed; IF-ID-07 deferred by design** |
| IF-ADR-015 | Player Provisioning Commands and Consumer Observation Surface | Proposed | **80%** | P1–P4 official package surface shipped and Q1/Q2 QA-certified; FIRSTGAME proof, P5 UX/tooling disposition and final acceptance remain |
| IF-ADR-016 | Player Session Initial Configuration and Provisioning Profiles | Proposed | **90%** | Contracts, Profiles, resolver, Session runtime initialization, complete creation-time Profile override, Inspectors and QA are implemented; FIRSTGAME proof and full Route/Activity integration evidence remain |

`*` IF-ADR-014 uses 100% only for portfolio arithmetic. Its official ADR wording remains `Complete for current accepted scope`.

Portfolio arithmetic: `(92+65+84+78+78+94+96+90+88+70+94+90+65+100+80+90) / 16 = 84.6%` rounded to one decimal.

## IF-TXN-03A closure incorporated

### Certified operational model

```text
Transition Gate
  internal GameFlow operation state
  no external resource acquire/release protocol
  no release refusal/ownership-token failure contract

TransitionGateSnapshot
  pure Transition Gate

CurrentTransitionGateMode
  pure Transition Gate mode

ActivityEntryReadinessGateSnapshot
  Transition Gate + Activity Entry Readiness Recovery Gate

CurrentGateSnapshot
  broader host operational composition
```

### Critical separation invariant

```text
Given:
  Transition Gate = released
  Readiness Recovery Gate = blocked

Then:
  CurrentTransitionGateMode == None
  TransitionGateSnapshot.HasBlockers == false
  ActivityEntryReadinessGateSnapshot.HasBlockers == true
```

This state is intentional recovery protection and must not be reported as a Transition Gate leak.

### Terminal integrity

The source audit and regression harness certify:

```text
success terminal cleanup
failure terminal cleanup
fallback-only cleanup through finally
exception/fault cleanup after Apply
Clear/Restart cleanup wiring
readiness cancellation/supersession compatibility
Transition-vs-recovery current-state separation
recovery cleanup to fully clean state
```

No additional release abstraction, generic transaction manager, rollback manager, lease/token or FIRSTGAME cut is justified by current evidence.

## Certification matrix

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

The terminal suite intentionally emits an error record for its required-participant failure scenario. Its final runner result is `Passed`; that scenario is negative evidence, not a failed certification.

### IF-ADR-016 current certification

```text
IF-SESSION-CONFIG-05 Player Session Runtime Integration
  PASS — 6/6

IF-SESSION-CONFIG-07 Player Session Contract Closure
  PASS — 17/17
  PUBLIC-ONLY cases PASS
  PARTIAL PUBLIC EVIDENCE cases PASS
  INTERNAL TECHNICAL cases PASS

IF-SESSION-CONFIG-05B Session Profile Override
  PASS — 4/4
  no override uses GameApplication default
  explicit override replaces default completely
  invalid explicit override does not fall back
  explicit override does not field-merge
```

Certified behaviors include authored Slot order, Capacity bounds, mixed per-Slot Scene/Manager provisioning, no provisioning fallback/skip, post-initialization structural freeze, Actor resolution policy separation, late-Join frozen provisioning, typed failures and immutable effective evidence.

### IF-ADR-015 Player Surface certification

```text
QA-PLAYER-SURFACE-01 Public-only positive contract
  PASS — 29/29

QA-PLAYER-SURFACE-02 Negative / stale-scope / lifecycle hardening
  PASS — 36/36

Joint certification
  navigation=PASS
  q1=PASS
  q2=PASS
  PLAYER SURFACE QA CERTIFIED
```

This certifies the shipped P1–P4 public consumer boundary without promoting internal reconcile/preparation/materialization authorities into public APIs.

Not directly certified by this Edit Mode suite: full Route/Activity transition non-reapplication through `FrameworkRuntimeHost`.

## Priority order

### Closed — transaction/gate integrity

- **IF-TXN-01:** closed for startup/Route/Activity transition authority.
- **IF-TXN-02:** closed for Activity Clear/Restart transition-authority parity.
- **IF-TXN-03A:** closed for Transition Gate terminal cleanup and current-state projection integrity.
- Do not reopen generic “Transition Gate release failure” without new evidence; canonical release is internal state replacement and has no fallible external release contract.

### P0 — next focused architecture decision

Choose one **concrete** unresolved exceptional path before implementation:

```text
consumer/loading hook exception after commit
disposal during partial presentation
adapter partial-side-effect compensation
full terminal cleanup/correlation receipts
```

Do not introduce a generic transaction/rollback manager without concrete evidence.

Player architecture now has two focused open fronts:

- **IF-ADR-016:** creation-time complete Session Profile override is closed; FIRSTGAME manual proof remains deferred and real Route/Activity non-reapplication is not directly certified.
- **IF-ADR-015:** P1–P4 consumer surface and Q1/Q2 technical QA are closed; remaining gates are FIRSTGAME real-consumer proof, P5 disposition and final ADR/documentation closure.

These can be prioritized independently of the next exceptional terminal-integrity audit.

### P1 — product authoring consistency

- IF-ADR-002: apply the product model consistently beyond mature composers.
- IF-ADR-010: standardize guided creation, remediation, receipts, Advanced/Debug and contextual cross-domain warnings.
- Preserve WaitCovered/Player warnings as advisory product guidance, not runtime policy.

### P2 — remaining hardening

- IF-ADR-003 Player provisioning hardening and Leave/disconnect boundaries; Player Surface public-only QA is now certified.
- IF-ADR-004 Camera priority/release/override negative matrix.
- IF-ADR-005 broader Gate/Pause/Reset/Restart exceptional matrix.
- IF-ADR-007 focused ObserveOnly and Player waiting/joining/replacement matrix.
- IF-ADR-011 Advanced/Debug and presentation guidance; the public WaitingForJoin/WaitCovered Player path is now certified.

### P3 — product demonstrations and promotion

Add FIRSTGAME demonstrations only where they improve product understanding; do not use FIRSTGAME as technical certification.

## Portfolio interpretation

```text
Core runtime architecture
  strong and mostly implemented

Transaction authority and gate integrity
  IF-TXN-01 CLOSED
  IF-TXN-02 CLOSED
  IF-TXN-03A CLOSED / CERTIFIED
  Start/Route/Activity/Clear/Restart authority green
  terminal Transition Gate cleanup green
  readiness recovery remains independently observable

Readiness/reveal
  WaitVisible + WaitCovered 42/42 PASS
  terminal/recovery 34/34 PASS
  startup parity Route 25/25 + Game Application 20/20 PASS

Participant-aware Loading
  progress 32/32 PASS
  Loading remains projection, not lifecycle/readiness authority

Player lifecycle
  technically strong
  IF-ADR-016 authored Session initialization implemented and QA green
  IF-ADR-016 creation-time complete Profile override CLOSED / 4/4 PASS
  IF-ADR-016 FIRSTGAME manual proof deferred
  IF-ADR-016 full Route/Activity non-reapply integration not directly certified
  IF-ADR-015 P1–P4 consumer surface shipped and QA-certified
  FIRSTGAME manual proof + P5 UX/tooling disposition remain

QA
  transaction regressions live in QAFramework, not package runtime
  current IF-TXN/readiness/loading evidence is green
  Player Surface Q1 29/29 + Q2 36/36 — QA CERTIFIED

FIRSTGAME
  current consumer baseline remains ab1bfe6
  no new IF-TXN-03A consumer proof required
```
