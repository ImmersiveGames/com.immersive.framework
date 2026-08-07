# Immersive Framework — ADR Completion Summary

Date: 2026-08-07  
Package Git baseline: `d0955e0dc58a3cc70f8533f92d63246d941d5e20` (`IF-TXN-01 COMPLETE`)  
QA baseline: `00cedcb78d200b1b2094eafc500e348e07dc36ab` (`IF-TXN-01 COMPLETE`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Portfolio average: **80.3% equivalent**

> Portfolio arithmetic treats IF-ADR-014 `Complete for current accepted scope` as
> 100% for the accepted scope. IF-ID-07 remains explicitly deferred by design and
> does not block IF-ADR-014 closure.

## Important baseline changes

1. The package HEAD is now `d0955e0` (`IF-TXN-01 COMPLETE`) and the QAFramework baseline is `00cedcb` (`IF-TXN-01 COMPLETE`).
2. **IF-TXN-01 — GameFlow Transition Failure Authority is closed for the approved canonical Start/Route/Activity request boundary.**
3. The package now treats non-accepted Transition Before as a pre-commit abort and non-accepted Transition After/reveal after commit as a committed-target reveal failure with recovery protection and no blind rollback.
4. `CompletedWithWarnings` remains accepted through `TransitionResult.Completed`; intentional policy/no-visual `Skipped` remains accepted; required `Failed`/`Rejected`/`Cancelled` outcomes are not masked as `Skipped`.
5. Canonical QA evidence is current: IF-TXN-01 22/22, Direct Activity Readiness Policies 42/42, participant-aware Loading terminal 34/34, participant-aware Loading progress 32/32, post-transition readiness PASS, Identity Authority 6/6.
6. WaitVisible and WaitCovered Play Mode behavior are re-certified. WaitCovered retains cover/gate until Ready; WaitVisible permits reveal while the request remains pending; both end with correct destination authority and gate release.
7. Participant-aware Loading is re-certified for success and terminal failure: optional failure is non-blocking, progress reaches 100% only at required 4/4 Ready, terminal failure remains below successful completion, and recovery protection is retained.
8. The WaitCovered + ExplicitSlots + Player requirement warning is now present in the current package documentation/authoring boundary; it is no longer a merely prepared/uncommitted amendment.
9. IF-ID remains closed for the current accepted boundary; IF-ID-07 remains deferred by design.
10. FIRSTGAME remains at `ab1bfe6` for the current consumer baseline. Deliberately broken Transition surfaces are not required to close IF-TXN-01 because technical failure authority is certified in QA.

## Method

Each percentage considers:

```text
Normative contract and architectural clarity   20%
Runtime implementation                         30%
Authoring, diagnostics and documentation        20%
Current QA evidence                             15%
Current FIRSTGAME consumer evidence             15%
```

The percentages are planning estimates. They are deliberately reduced when code is
present but the current QA harness, product surface, or consumer proof is incomplete.

For IF-ADR-014 only, the official state is no longer expressed as an implementation
percentage: it is `Complete for current accepted scope`. The portfolio arithmetic
therefore uses **100% for the accepted scope** while retaining IF-ID-07 as a deferred,
non-blocking future boundary.

## ADR matrix

| ADR | Decision | Normative status | Completion | Current classification |
|---|---|---:|---:|---|
| IF-ADR-001 | Core Lifecycle and Runtime Authority | Accepted | **90%** | IF-TXN-01 implemented and QA-certified; Session-Persistent Player and broader compensation/cleanup residuals remain |
| IF-ADR-002 | Product Authoring Model | Accepted | **65%** | Partially implemented across the product portfolio |
| IF-ADR-003 | Player Participation and Actor Lifecycle | Accepted | **84%** | Runtime substantially implemented; product and hardening gaps remain; covered-readiness control-plane boundary and authoring warning are present in the current package |
| IF-ADR-004 | Camera Requests and Output Authority | Accepted | **78%** | Core runtime implemented; isolated product proof incomplete |
| IF-ADR-005 | Input, Pause, Gate and Reset | Accepted | **76%** | Integrated runtime exists; product extraction and negative coverage incomplete |
| IF-ADR-006 | Loading, Transition, Persistence and Diagnostics | Accepted | **91%** | IF-TXN-01 implemented and QA-certified; Clear/Restart, gate-release/partial-presentation cleanup and product gaps remain |
| IF-ADR-007 | Activity Entry Readiness and Reveal Gating | Accepted | **96%** | Runtime contract complete; WaitVisible/WaitCovered and post-transition QA re-certified; focused ObserveOnly/Player public-only matrix remains |
| IF-ADR-008 | Persistent Application Content Composition | Accepted | **90%** | Product model implemented; portfolio expansion and QA remain |
| IF-ADR-009 | Activity Local Visibility Rules | Accepted | **88%** | Runtime integrated; authoring and regression polish remain |
| IF-ADR-010 | Editor and Inspector Product Surface Authority | Proposed | **70%** | Broad foundation exists; decision not fully accepted or consistently applied |
| IF-ADR-011 | Participant-Aware Activity Readiness Loading Progress | Accepted | **92%** | Runtime complete; canonical progress/terminal QA re-certified; public-only waiting/joining and product presentation parity remain |
| IF-ADR-012 | Activity Player Participation Profile and Readiness Compatibility | Accepted | **90%** | Contract and runtime implemented; product/QA consolidation remains |
| IF-ADR-013 | Optional Audio BGM Adapter | Accepted / Experimental | **65%** | Technical adapter exists; product promotion incomplete |
| IF-ADR-014 | Authored Definition and Stable Identity Authority | **Accepted** | **100%*** | **Complete for current accepted scope; IF-ID closed; IF-ID-07 deferred by design** |
| IF-ADR-015 | Player Provisioning Commands and Consumer Observation Surface | Proposed | **30%** | ADR and consumer prototype exist; official package surface not shipped |

`*` IF-ADR-014 uses 100% only for portfolio arithmetic. Its official ADR wording is
`Complete for current accepted scope`, not a numeric completion claim.

## IF-ID closure incorporated

### IF-ADR-014 — closed authority model

The canonical authority model is now:

| Dimension | Authority |
|---|---|
| Exact authored definition | exact typed `RouteAsset` / `ActivityAsset` reference |
| Stable boundary identity | `RouteId` / `ActivityId` |
| Runtime occurrence | definition reference + occurrence / revision / sequence |
| Operational ownership | scoped owner + `RuntimeDefinitionToken` |
| Presentation | display name only |

Closed proof includes:

```text
Package vocabulary + reference authority        Complete
Required operational definition tokens          Complete
Validation scopes + regenerate UX               Complete
Package runtime/Editor tests                     Passed
Lifecycle/ownership/readiness QA matrix          Passed
QA idempotency / second execution                Passed
FIRSTGAME duplication/remediation workflow       Passed
Application-scoped ID resolver IF-ID-07          Deferred by design
```

IF-ID should not be reopened for general cleanup. Reopen only on new evidence of
stable-ID definition collapse, cross-definition release authority, wrong occurrence
correlation, implicit identity mutation, or a real persistence/external boundary that
requires IF-ID-07.

## Current decision — WaitCovered + Player readiness

### Problem recorded

A valid Activity composition may intentionally contain an Explicit Player Slot that is
not Joined when Activity entry starts. With `PlayerParticipationRequirementLevel` at
`JoinedSlots` or stronger, the Required Player contribution may correctly remain:

```text
Preparing / WaitingForJoin
```

When the same Activity uses `WaitCovered`, a deadlock-like product composition occurs
if the only Join/selection action capable of advancing the Required contribution is
inside the destination that remains covered until readiness becomes `Ready`.

### Decision recorded

The runtime contracts remain unchanged:

```text
Required readiness remains Required
WaitCovered remains covered until Ready or typed terminal interruption
Loading never becomes readiness authority
Loading never publishes successful 100% before aggregate Ready
an unjoined Explicit Slot is not silently converted to NoParticipants
no timeout-to-Ready fallback is introduced
no automatic Join is introduced
no same-Activity re-request is used as reconciliation
```

The package solution is product-facing and diagnostic:

```text
Activity Validate
→ detect WaitCovered
→ detect ExplicitSlots
→ detect Player requirement >= JoinedSlots
→ emit non-mutating Warning
→ explain valid compositions and control-plane responsibility
```

The combination remains valid when at least one of these is true:

```text
required Player state is satisfied before Activity entry;
provisioning progresses automatically while covered;
a persistent/external control plane can issue Join/selection while covered;
WaitVisible is intentionally selected for visible Player preparation.
```

### ADRs carrying the decision

- **IF-ADR-003:** records Player Join/progression as a control-plane dependency and preserves `Preparing / WaitingForJoin` semantics.
- **IF-ADR-007:** records the covered external-progression dependency cycle and explicitly rejects weakening `WaitCovered` as a repair.
- **IF-ADR-011:** records that Loading must remain below successful terminal completion while Required readiness is still preparing and must not compensate for inaccessible controls.

The current package carries this decision and the authoring warning. IF-ADR-007 and IF-ADR-011 now also carry current QA evidence for the executed WaitVisible/WaitCovered and participant-aware Loading suites. Their percentages remain unchanged because focused public-only Player progression and product-presentation work is still open.

## Priority order

### Closed — Transaction failure authority

- **IF-TXN-01:** closed for canonical Start/Route/Activity request paths.
- Pre-commit Transition failure cannot start destination lifecycle or change authority.
- Post-commit reveal failure preserves committed authority, returns non-success and applies reveal recovery protection.
- Canonical QA and Play Mode non-regression evidence are green.

### P0 — Next focused architecture/product cuts

- **IF-ADR-001 / IF-ADR-006 residual terminal integrity:** audit/select a focused follow-up for Clear/Restart transition authority, gate-release failure, partial-presentation cleanup or compensation evidence. Do not introduce a generic transaction manager without concrete need.
- **IF-ADR-015 — 30%:** canonicalize Player provisioning commands, immutable observation, authoring, QA and FIRSTGAME migration.

### P1 — Product authoring consistency

- **IF-ADR-002 — 65%:** apply the product model consistently beyond mature composers.
- **IF-ADR-010 — 70%:** standardize guided creation, remediation, receipts, Advanced/Debug and contextual cross-domain warnings.
- Preserve the current WaitCovered/Player authoring warning as advisory product guidance; do not turn it into runtime policy.

### P2 — Remaining hardening

- IF-ADR-003 Player provisioning hardening, Leave/disconnect boundaries and public-only QA.
- IF-ADR-004 Camera priority/release/override negative matrix.
- IF-ADR-005 Gate/Pause/Reset/Restart terminal cleanup matrix.
- IF-ADR-007 focused ObserveOnly and Player waiting/joining/replacement matrix.
- IF-ADR-011 public-only waiting/joining proof, startup/product presentation parity and Advanced/Debug diagnostics.

### P3 — Product demonstrations and promotion

- Dedicated Player Camera, Camera Override, Reset/Restart, Pause and Transition/Loading demonstrations.
- Add a FIRSTGAME demonstration that makes the control-plane/gameplay-plane distinction obvious for Manager-Provisioned Player entry when useful.
- IF-ADR-013 BGM FIRSTGAME demonstration and promotion decision.

## Portfolio interpretation

```text
Core runtime architecture
  strong and mostly implemented

IF-TXN-01 transaction authority
  CLOSED for canonical Start/Route/Activity paths
  22/22 contract regression PASS
  Play Mode readiness/loading non-regressions PASS
  remaining Clear/Restart and broader cleanup/compensation are separate cuts

Stable identity / IF-ID
  closed for current accepted scope
  Identity Authority regression 6/6 PASS
  IF-ID-07 deferred by design

Readiness/reveal
  WaitVisible + WaitCovered 42/42 PASS
  post-transition readiness PASS
  covered Player external-progression trap remains a product/control-plane composition concern

Participant-aware Loading
  progress 32/32 PASS
  terminal/failure 34/34 PASS
  Loading remains projection, not lifecycle/readiness authority

Player lifecycle
  technically strong
  canonical consumer command/observation surface still missing (IF-ADR-015)

QA
  IF-TXN-01 canonical regression is in QAFramework, not package runtime
  current transaction/readiness/loading/identity evidence is green
  QA menu consolidation remains a future governance task, not part of this documentation cut

FIRSTGAME
  current consumer baseline remains ab1bfe6
  continues to provide real product-composition evidence
```
