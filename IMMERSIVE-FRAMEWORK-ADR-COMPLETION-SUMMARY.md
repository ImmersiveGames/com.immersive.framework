# Immersive Framework — ADR Completion Summary

Date: 2026-08-07  
Package Git baseline: `20b03efff3fe284f2098e12daf1f9274612ea40a` (`Audits`)  
QA baseline: `db1f90fa5dd0a847ff2791435c292d76e49f88db` (`Corte 4 — Ownership e Readiness Isolation`)  
FIRSTGAME baseline: `ab1bfe65c09af8988c2fe21ce06db780fe12aa70` (`Demo03Etapa04`)  
Prepared package amendment: `IF-READY-WAITCOVERED-PLAYER-AUTHORING-WARNING-01` based on package `20b03eff` — **not committed yet**  
Portfolio average: **80.0% equivalent**

> Portfolio arithmetic treats IF-ADR-014 `Complete for current accepted scope` as
> 100% for the accepted scope. IF-ID-07 remains explicitly deferred by design and
> does not block IF-ADR-014 closure.

## Important baseline changes

1. The package HEAD advanced from `9ed698e` to `20b03eff`.
2. **IF-ID is closed for the current framework boundary.** IF-ADR-014 is now `Accepted` and `Complete for current accepted scope`.
3. IF-ID-02..06 and IF-ID-08 are complete. IF-ID-07, the application-scoped stable-ID resolver, is explicitly deferred until a real persistence/external boundary requires it.
4. Package identity tests passed for the accepted scope, including exact-reference versus stable-ID semantics, required `RuntimeDefinitionToken` ownership isolation, collision context, explicit stable-ID regeneration and Undo.
5. QAFramework now has the canonical IF-ID authority runner and the official closure evidence records **6/6 passed twice**, including cleanup/idempotency and readiness/ownership collision isolation.
6. FIRSTGAME completed the IF-ID duplication/remediation workflow: copied definitions expose collisions, the conflicting asset is navigable, only the copied stable ID is regenerated, rename/move preserves identity, and the repaired definitions run successfully.
7. The latest package `Audits` cut records the causal readiness and Player-participation audits that distinguish Activity readiness authority from Player participation progression and Loading presentation.
8. A new package amendment is prepared for the FIRSTGAME-discovered composition trap:

   ```text
   WaitCovered
   + ExplicitSlots
   + PlayerParticipationRequirementLevel >= JoinedSlots
   ```

   The amendment adds a **non-mutating authoring warning** and updates IF-ADR-003, IF-ADR-007 and IF-ADR-011. It deliberately does **not** change Activity Readiness, Required/Optional semantics, Player reconciliation, Loading completion, Transition/Gate behavior or `WaitCovered` semantics.
9. The accepted interpretation is now explicit: an unjoined Explicit Slot may legitimately remain `Preparing / WaitingForJoin`; if the only action capable of advancing it is hidden behind the retained cover, the problem is a **control-plane composition cycle**, not a Loading or readiness defect.
10. The `WaitCovered` amendment is prepared in ZIP form against `20b03eff` but is not counted as committed package evidence until applied and validated in Unity/QA.

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
| IF-ADR-001 | Core Lifecycle and Runtime Authority | Accepted | **88%** | Substantially implemented; architectural residuals remain |
| IF-ADR-002 | Product Authoring Model | Accepted | **65%** | Partially implemented across the product portfolio |
| IF-ADR-003 | Player Participation and Actor Lifecycle | Accepted | **84%** | Runtime substantially implemented; product and hardening gaps remain; covered-readiness control-plane boundary now documented in prepared amendment |
| IF-ADR-004 | Camera Requests and Output Authority | Accepted | **78%** | Core runtime implemented; isolated product proof incomplete |
| IF-ADR-005 | Input, Pause, Gate and Reset | Accepted | **76%** | Integrated runtime exists; product extraction and negative coverage incomplete |
| IF-ADR-006 | Loading, Transition, Persistence and Diagnostics | Accepted | **88%** | Core orchestration implemented; recovery and product gaps remain |
| IF-ADR-007 | Activity Entry Readiness and Reveal Gating | Accepted | **96%** | Runtime contract complete; current QA recertification remains; covered external-progression cycle now documented in prepared amendment |
| IF-ADR-008 | Persistent Application Content Composition | Accepted | **90%** | Product model implemented; portfolio expansion and QA remain |
| IF-ADR-009 | Activity Local Visibility Rules | Accepted | **88%** | Runtime integrated; authoring and regression polish remain |
| IF-ADR-010 | Editor and Inspector Product Surface Authority | Proposed | **70%** | Broad foundation exists; decision not fully accepted or consistently applied |
| IF-ADR-011 | Participant-Aware Activity Readiness Loading Progress | Accepted | **92%** | Runtime complete; current QA and product presentation recertification remain; Loading-side non-repair rule now documented in prepared amendment |
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

## Prepared amendment — WaitCovered + Player readiness

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

### ADRs affected by the prepared amendment

- **IF-ADR-003:** records Player Join/progression as a control-plane dependency and preserves `Preparing / WaitingForJoin` semantics.
- **IF-ADR-007:** records the covered external-progression dependency cycle and explicitly rejects weakening `WaitCovered` as a repair.
- **IF-ADR-011:** records that Loading must remain below successful terminal completion while Required readiness is still preparing and must not compensate for inaccessible controls.

The existing percentages for IF-ADR-003, IF-ADR-007 and IF-ADR-011 are intentionally
unchanged until the amendment is applied and its current package/QA validation is
recorded. The value of this cut is primarily architectural clarification and product
authoring prevention, not additional runtime implementation.

## Priority order

### Closed — Cross-cutting identity authority

- **IF-ADR-014 / IF-ID:** closed for current accepted scope. Remove it from active implementation priority. IF-ID-07 remains deferred until a real persistence/external boundary exists.

### P0 — Canonical Player consumer surface

- **IF-ADR-015 — 30%:** convert Demo03 findings into canonical typed commands, immutable observation, authoring, QA, and migration.
- Ensure the future command surface explicitly distinguishes **control-plane Player operations** such as Join from gameplay capabilities that may be gated during `WaitCovered`.

### P1 — Product authoring consistency

- **IF-ADR-002 — 65%:** apply the product model consistently beyond the currently mature composers.
- **IF-ADR-010 — 70%:** standardize guided creation, remediation, receipts, Advanced/Debug, and contextual cross-domain warnings.
- Apply and validate `IF-READY-WAITCOVERED-PLAYER-AUTHORING-WARNING-01`; after package validation, add the corresponding canonical QA evidence without turning the warning into runtime policy.

### P2 — Runtime hardening and current QA recertification

- IF-ADR-003 Player provisioning hardening, Leave/disconnect boundaries, and current public-only QA.
- IF-ADR-004 Camera priority/release/override negative matrix.
- IF-ADR-005 Gate/Pause/Reset/Restart terminal cleanup matrix.
- IF-ADR-006/007 replacement, cancellation, supersession, reveal, cleanup, and Player-readiness covered/visible composition matrix.
- IF-ADR-011 loading monotonicity, stale occurrence, failure/release, zero-participant, optional-only and supersession recertification.

### P3 — Product demonstrations and promotion

- Dedicated Player Camera, Camera Override, Reset/Restart, Pause, and Transition/Loading demonstrations.
- Add a FIRSTGAME demonstration that makes the control-plane/gameplay-plane distinction obvious for Manager-Provisioned Player entry.
- IF-ADR-013 BGM FIRSTGAME demonstration and promotion decision.

## Portfolio interpretation

```text
Core runtime architecture
  strong and mostly implemented

Stable identity / IF-ID
  closed for current accepted scope
  exact definition authority + RuntimeDefinitionToken proven
  IF-ID-07 deferred by design

Readiness/loading
  contractually mature
  Route replacement/supersession fix incorporated
  WaitCovered + Player external-progression trap classified as product/control-plane composition
  non-mutating authoring warning prepared; runtime semantics intentionally unchanged

Player lifecycle
  technically strong
  late join/reconciliation exists
  canonical consumer command/observation surface still missing
  Join must remain distinguishable from gated gameplay capability

Product authoring
  proven in selected systems but inconsistent across the portfolio
  cross-domain composition warnings are now part of the expected product surface

QA
  canonical IF-ID authority proof restored and passed
  broader readiness/loading/player recertification still required

FIRSTGAME
  now provides real IF-ID remediation proof and active Player UX evidence
  continues to expose product-composition issues that permanent package tooling must absorb
```
