# IF-ADR-048 — Gate A Integrated Plan Review Record (v1)

**Status:** REVIEWED — FORMAL GATE A APPROVAL PENDING  
**Review date:** 2026-10-13  
**Source branch/head reviewed:** `master` at `5ebffb5333b6cabad2a7dc7cb674f59834caf9b0`  
**Runtime source analyzed by the master plan:** `617547a337e52022999c71c30f2e493a3dacbd26` (static source review; not executed evidence)  
**Effect:** Documentary review only. No Runtime/test/QA/consumer/package/workflow implementation authorization and no 1.1.0 release authorization.

## Exact documentary review baseline

| Document | Git blob SHA reviewed | Disposition |
|---|---|---|
| [IF-PLAN-ADR-048-STABLE-READINESS.v1](IF-PLAN-ADR-048-STABLE-READINESS.v1.md) | `628f64b2878cdad9fbf8d89392e8e39725b6fe3a` | Integrated proposal reviewed; Gate A still Proposed |
| [IF-ADR-001](../ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | `2300da02716b70f8f2cf4b06c884b9c82f8a6983` | Proposed Host phase, operation-admission and controlled-stop amendment: recommended for architectural acceptance |
| [IF-ADR-040](../ADRs/IF-ADR-040-Scene-Composition-Binding-Model.md) | `d5039b03cb68d40d2adbce500b1e80a73a8a22d7` | Proposed Session participant ledger/retry amendment: recommended for architectural acceptance |
| [IF-ADR-018](../ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | `156e0b9bd6ecc2e600c832fe22c5b2d73955f822` | Proposed §11 maturity clarification: recommended as editorial acceptance |

This records a documentary consistency review, not an independent verification of execution, compiler behavior, physical cleanup, or QA. The project owner's previously expressed approval of the architecture direction is **not** treated as formal sign-off by each designated reviewer.

## Proposed Gate A decision — technically recommended, not yet accepted

1. **Core Host (IF-ADR-001):** One existing `FrameworkRuntimeHost` owns internal composing/operational/stopping/inert-retryable/released phases. Admit new product commands only when operational; allow exact, owner-held release/retry during stop. Coordinate canonical Leave of joined occurrences before destroying their physical owners. Retain a non-operational Host on recoverable cleanup failure. Actual Unity/process terminal destruction remains best-effort, with no post-exit retry guarantee.
2. **Scene composition (IF-ADR-040):** Record every discovered Session participant *before* invoking its `Available`, including the participant that rejects; retain scope/owner and failure diagnostics across partial rollback and repeated aggregate release until confirmed success. Feature-owned cleanup remains idempotent; no cross-feature registry.
3. **Save maturity (IF-ADR-018 §11):** The existing 13 backend-neutral contract types stay Stable; concrete JSON/provider/composition/runtime surfaces keep their declared maturities. No added Save durability, retry, provider-disposal or gameplay Snapshot promises.
4. **Accepted boundaries preserved:** IF-ADR-036/044/046/047/048 are unchanged. No general promotion of Experimental Observer, Actor, Leave, Provisioning or Camera topologies; no pre-1.1.0 generic migration duty.
5. **Session identity:** `ApplicationName` remains display/diagnostic data. The already-existing exact Host `EntityId` is the Session scope owner authority; no new public `ApplicationId`.
6. **Route/Activity and Camera:** No blanket atomic Route rollback. An operation reports its real logical commit and outstanding physical cleanup; Camera post-commit owners/receipts must survive until precise release and retry.
7. **Save lifecycle:** The Stable store stays synchronous and has no generic disposal contract. Resource-owning Experimental providers own any additional resource lifetime and are not silently certified as Stable.

## API Governance approval needed before Gate A

**Proposed boundary:** restrict the inherited public `PlayerSessionScopedAccessConsumer.TryGetAccess` helper to derived implementation (such as `protected`), so `PlayerSessionObserver` and the Stable `PlayerGameplayAvailabilityBlockTrigger` cannot expose the broad mutable `IPlayerSessionScopedAccess` to an external caller. Preserve typed public Observer readings and the Availability trigger's one-owner block/release behavior; do not promote the Experimental interface or create parallel Join authority.

**Governance review must explicitly decide:** (a) whether IF-ADR-015's existing scoped-access description needs a narrow normative clarification in addition to Public-API inventory; (b) the exact supported consumer API after restriction; (c) whether every in-scope QA callsite has a replacement without a new product trigger. Until that review, this is a proposal rather than an Accepted breaking API decision.

**Proposed QA migration and test scope:** migrate known QAFramework scenarios for IF-ADR-036/042/043/044/046 and Player Session to existing Open/Close/Leave/Selection triggers for commands, Observer for observation, and two Availability triggers for independent consumer-owned blocks. Move raw foreign/stale token probes to Availability-owner NUnit; place prepared Actor replacement proof in Actor Preparation owner NUnit if still required. This migration must be approved by the QA authority; do not claim it is already implemented or validated. FirstGame requires a focused callsite/compile check; PEGA remains an exact-artifact consumer certification target.

## Execution and evidence ownership (proposed roles, not personal sign-offs)

| Package | Implementation owner | Test/evidence owner | Decision |
|---|---|---|---|
| I-01 Session participant ledger | Framework Core / Scene Lifecycle | Framework package NUnit + integrated QA owner | Pending execution authorization |
| I-02 scoped Availability and Pause/Input physical receipts | Player Participation + Pause/Input | Framework NUnit + QAFramework | Pending execution authorization |
| I-03 Host admission, bootstrap and controlled stop | Framework Core / Host + Bootstrap | Framework NUnit/Unity + QAFramework | Pending execution authorization |
| I-04 canonical Leave, Camera and Activity teardown | Player/Camera/Activity feature authorities | Focused fault-injection + integrated QAFramework | Pending execution authorization |
| I-05 GameFlow partial-failure truth | GameFlow / Route / Activity | Fault injection + QAFramework; Runtime changes conditional on demonstrated Stable break | Pending execution authorization |
| I-06 immutable Progression Save payload | Progression Save contract owner | Save conformance NUnit + scoped Unity/QA smoke | Pending execution authorization |
| I-07 identity/maturity conformance and Session owner key | Framework Identity / Host | Identity/metadata NUnit + authoring round-trip | Pending execution authorization |
| I-08 Observer/Provisioning ingress and QA migration | API Governance + owning Runtime modules | QAFramework consumer/compile and command-boundary proof | Pending execution authorization |

**Dependency waves:** Wave 1 = I-01, I-06, I-07 independently; Wave 2 = I-02 then I-08's approved migration; Wave 3 = I-03 after I-01/I-02/I-08; Wave 4 = I-04; Wave 5 = I-05, with Runtime change only if an accepted Stable guarantee is reproduced as violated.

**PASS/FAIL/BLOCKED and named cases:** use the exact I-01..I-08 matrices of the reviewed master plan. They identify owners, failure injection, exact outcomes, retry and baseline cleanup. This Gate A review does **not** convert those proposed cases or historical reports into executed PASS.

## Residual risks requiring named authority acceptance

| Risk | Proposed accepting authority | Status |
|---|---|---|
| Terminal Unity/process exit prevents retry after owner destruction | Framework Architecture + Release | Pending explicit acceptance |
| A GameFlow operation that never settles can retain a controlled-stopping Host indefinitely; no arbitrary timeout is promised | Framework Architecture + Release | Pending explicit acceptance |
| Route can have truthful partial outcomes; no full Route atomicity guaranteed | GameFlow contract owner + Release | Pending explicit acceptance |
| Experimental-only Player/Camera/Provisioning capabilities remain outside bounded Stable guarantees unless they causally affect Stable behavior | API Governance + Release | Pending explicit acceptance of the boundaries |
| Save has no generic atomicity/idempotency/async/disposal contract; custom provider behavior belongs to its integration | Save contract owner + Release | Pending explicit acceptance |

## Formal Gate A decision — not yet recorded

| Required authority | Required action | Recorded decision |
|---|---|---|
| Framework Architecture | Accept the exact IF-ADR-001/040 proposed normative amendments and any owner-level scope impact | **PENDING** |
| Save/Architecture | Accept IF-ADR-018 §11 editorial clarification | **PENDING** |
| API Governance | Decide `TryGetAccess` public boundary, IF-ADR-015 disposition and supported QA migration | **PENDING** |
| Runtime owners | Confirm exact scope, owner map, dependency waves, and cleanup invariants | **PENDING** |
| QAFramework / test authority | Approve named existing/new evidence, failure injection and consumer migration | **PENDING** |
| Release authority | Accept bounded risks, exact-artifact validation matrix and scope | **PENDING** |
| Project Gate A authority | Explicit dated GO/NO-GO on the exact reviewed plan revision and accepted decisions | **PENDING** |

The review recommends approval **after** these recorded decisions, not before. The eventual final Gate A record must identify approvers and dates, the accepted normative decision versions, plan revision/source identity, risks, NUnit/QA deliverables and approval verdict. Updating the status of an Accepted ADR or calling the plan Approved without the required authority record is prohibited by this plan.

Even a future **GO at Gate A** is only integrated-plan approval: it does **not** authorize implementation. Require a separate explicit implementation instruction. IF-ADR-048 Phase 2 remains open; Phases 3–5 are pending, and `1.1.0` stays **NO-GO** until exact-artifact certification and publication GO.
