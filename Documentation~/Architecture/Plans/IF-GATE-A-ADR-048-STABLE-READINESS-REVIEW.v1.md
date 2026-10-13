# IF-ADR-048 — Gate A Integrated Plan Review Record (v1)

**Status:** PREPARED FOR FORMAL REVIEW — GATE A APPROVAL PENDING  
**Review date:** 2026-10-13  
**Framework repository HEAD reviewed:** `5ebffb5333b6cabad2a7dc7cb674f59834caf9b0` (`master`)  
**Plan identity:** [`IF-PLAN-ADR-048-STABLE-READINESS.v1.md`](IF-PLAN-ADR-048-STABLE-READINESS.v1.md), Git blob `628f64b2878cdad9fbf8d89392e8e39725b6fe3a`  
**Source revision described by the plan:** `617547a337e52022999c71c30f2e493a3dacbd26`; static analysis provenance only, not executed candidate evidence.  
**Effect:** Documentary review and approval preparation only. This record is not Gate A approval, implementation authorization, certification or publication authorization.

## Review basis and provenance

The review checked the exact master-plan blob and its references to IF-ADR-001,
IF-ADR-040, IF-ADR-018, IF-ADR-015, IF-GOV-001, IF-ADR-036/044/046/047/048,
Public-API and IF-TRACK-Framework. No new Runtime audit or test execution was
performed. The plan source SHA above is inherited from its recorded analysis;
this record does not claim to revalidate those source findings.

The project owner has approved the integrated architectural direction as a
basis for this review. The repository contains no formal per-authority
approvals or risk acceptances for Gate A. No approver names or independent
opinions are inferred. A read-only review of preliminary commit
`cd395b8a6070674ff646bd3a8a6373a53e99d6c8` found it adds a draft Gate A record
against the same plan and ADR blobs. This local record was checked against
current `master`; the preliminary commit was not merged, cherry-picked,
checked out or synchronized.

## Exact documentary baseline

| Document | Git blob reviewed | Disposition |
|---|---|---|
| [Master readiness plan](IF-PLAN-ADR-048-STABLE-READINESS.v1.md) | `628f64b2878cdad9fbf8d89392e8e39725b6fe3a` | Proposed route; no implementation authority |
| [IF-ADR-001](../ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | `2300da02716b70f8f2cf4b06c884b9c82f8a6983` | Proposed internal Host phases/admission/controlled-stop amendment; Accepted status unchanged |
| [IF-ADR-040](../ADRs/IF-ADR-040-Scene-Composition-Binding-Model.md) | `d5039b03cb68d40d2adbce500b1e80a73a8a22d7` | Proposed Session participant ledger/retry amendment; Accepted status unchanged |
| [IF-ADR-018](../ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | `156e0b9bd6ecc2e600c832fe22c5b2d73955f822` | Proposed editorial clarification of §11; Accepted status unchanged |
| [IF-ADR-015](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | `3fe63c6fcb7ba8d37507188f48af81cbdff92695` | Observer/read and explicit command architecture preserved; no amendment proposed below |
| [Public-API](../../API/Public-API.md) | `e8a5a7972491b33e0ed5aefd31b990ff6efa766a` | Curated supported surface does not list `TryGetAccess`; Stable Availability trigger remains one-owner block surface |
| [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md) | `695b0b85b3f3d48344901b8df9213b70db632708` | Stable/Experimental boundaries and evidence policy retained |
| [IF-ADR-036](../ADRs/IF-ADR-036-Gameplay-Readiness-and-Runtime-Gameplay-Availability-Separation.md) | `f7eaa955e353e6831832a8c4e59189f81b389abf` | Structural/operational readiness distinction preserved |
| [IF-ADR-044](../ADRs/IF-ADR-044-Consumer-Controlled-Player-Runtime-Gameplay-Availability.md) | `f07cf5fb56427d3dca90f067840298f5257f8635` | Stable token/result boundary preserved; no amendment proposed |
| [IF-ADR-046](../ADRs/IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md) | `61af780806e1953a89c1557fb13044c17038ce6e` | Accepted bounded Join closure preserved |
| [IF-ADR-047](../ADRs/IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md) | `7e875e2f044e861fad7140ac741cbd8ebf3feafc` | First Stable baseline policy preserved |
| [IF-ADR-048](../ADRs/IF-ADR-048-Framework-1.1.0-Stable-Readiness-Roadmap.md) | `51d00108b2f63b1f1ddc1cf23914e91f53061bf3` | Readiness phases and separate release GO preserved |
| [IF-TRACK-Framework](../Tracking/IF-TRACK-Framework.md) | `3db80505d4a69b817b470a72e8a185cb348d44f4` | Phase 2 remains in progress; plan approval, certification and release remain distinct |

## Proposed Gate A decision — not yet accepted

The following is the integrated recommendation for the designated authorities.
It is not an Accepted amendment or formal sign-off.

1. **Host lifecycle — IF-ADR-001.** Use the existing `FrameworkRuntimeHost`
   as the single owner, with internal composing, operational, stopping,
   inert/retryable and released phases. Gate new operations at canonical
   owners. Controlled shutdown returns a result; on recoverable release
   rejection retain the same Host, owners and exact receipts for retry.
   Terminal Unity/process destruction remains best-effort.
2. **Admission and cleanup — IF-ADR-001 with existing feature ADRs.** Reject
   new product intent before bootstrap is operational and while stopping.
   Permit only the explicit cleanup/retry allowlist for live exact identities,
   occurrences, tokens, bindings and receipts. Do not add a public Host state,
   global recovery registry or second command authority.
3. **Leave and teardown — IF-ADR-020 plus feature owners.** During controlled
   shutdown close Joining through the existing Session owner and coordinate
   canonical Leave for each still-Joined occurrence. Keep Activity
   preconditions and feature owners alive until their own release is
   confirmed. Clear Camera/Output and `PlayerInput.camera`, release exact
   Availability tokens and restore Pause/Input physical state before destroying
   the corresponding Player/Actor owners. Then exit remaining Activity/Route,
   release the retained Session participant set, Scene Lifecycle and Persistent
   Content, and destroy Host dependencies only after aggregate success. Retries
   use the same occurrence and exact receipts.
4. **Session participant ledger — IF-ADR-040.** Record each participant, its
   order and exact Session owner before calling `Available`; include the
   rejecting participant in compensation. Preserve the original failure and
   additional compensation diagnostics. Retain the complete participant set
   across partial rollback/release; clear the ledger only after aggregate
   release succeeds. Revisit already released participants only under the
   existing idempotent same-scope contract.
5. **Progression Save — IF-ADR-018 / IF-GOV-001.** Keep only
   `IProgressionSaveStore` and its twelve associated types Stable. Runtime,
   request, profiles/providers, composition/catalog and concrete backend
   surfaces retain their current maturity. Do not add generic durability,
   atomicity, idempotency, async or provider-disposal guarantees.
6. **Preserved accepted boundaries.** IF-ADR-036, IF-ADR-044, IF-ADR-046,
   IF-ADR-047 and IF-ADR-048 remain unchanged. Preserve structural
   `GameplayReady` versus operational `RuntimeGameplayAvailable`, exact
   availability token/result scope, bounded Stable Join, the first-Stable
   baseline and readiness phases. No Experimental API or behavior is promoted.
7. **Route, Camera and identity.** Report Route's real commit and partial
   cleanup state; do not promise blanket atomic rollback. Keep Camera's
   post-commit assignment/output receipts until exact release. Treat
   `ApplicationName` as presentation data and reuse the existing Host
   `EntityId` for Session ownership; add no public `ApplicationId`.

## API Governance disposition — approval pending

**Recommended boundary:** restrict the inherited `PlayerSessionScopedAccessConsumer.TryGetAccess`
helper inherited by `PlayerSessionObserver` and the Stable Availability trigger
to derived implementation (`protected` or equivalent). Preserve
`PlayerSessionObserver` as read-only, the Stable
`PlayerGameplayAvailabilityBlockTrigger` as owner of its one block/release,
and `IPlayerSessionScopedAccess` as Experimental. Migrate QA consumers to the
existing explicit command triggers and Observer reads; keep direct capability
matrices in their owning module NUnit tests. Do not add triggers merely to
replace test-only access.

**IF-ADR-015 disposition:** no amendment is recommended. Its accepted contract
already separates Observer/read from explicit command/request surfaces and
does not promise public visibility for the inherited implementation helper.
The method is absent from the curated supported Public-API list. The current
publicly callable inheritance is an implementation exposure to remove, not a
new Stable consumer contract. API Governance must confirm this interpretation
before implementation; if it considers the helper a declared consumer promise,
Gate A must require a reviewed ADR-015/API migration decision instead.

**Public-API disposition:** no new API row or maturity promotion is proposed.
The implementation/certification change must verify the Stable Availability
trigger exposes only its documented owner-scoped operations and that Observer
remains read-only. QA migration is proposed, not performed or certified.

## I-01 to I-08 execution route and responsibility roles

Roles below identify accountable functions, not named people. The actual
owners/reviewers must be assigned in the formal approval record. Every package
requires current-candidate NUnit evidence where applicable and QAFramework
integration evidence for composition/lifecycle behavior; historical counts do
not satisfy candidate certification.

| Package | Implementation owner | NUnit / focused evidence | QA / consumer evidence | Status |
|---|---|---|---|---|
| I-01 Session participant ledger | Framework Core / Scene Lifecycle | participant-before-Available, failure/compensation aggregation, complete-ledger retention, idempotent retry | bootstrap composition failure and retry; baseline restoration | Pending authorization |
| I-02 Availability and Pause/Input receipts | Player Participation + Pause/Input owners | exact/foreign/stale tokens; physical release rejection, retained receipt and retry | Leave, scope teardown and baseline restoration with independent blocks | Pending authorization |
| I-03 Host admission/bootstrap/stop | Framework Core / Host + Bootstrap | all canonical ingress phases; pre/post-composition failure; stop success and reject-then-retry on same Host | Host bootstrap/stop across cleanup allowlist; prove no bypass and retained Host | Pending authorization |
| I-04 Leave, Camera and Activity teardown | Player/Leave + Camera + Activity authorities | exact occurrence, Camera post-commit output failure, Leave stage rejection and retry | integrated Camera/Input/Availability/Leave ordering and cleanup | Pending authorization |
| I-05 GameFlow partial-failure truth | GameFlow / Route / Activity | pre/post-commit fault injection, truthful authority and retry; Runtime fix only if Stable violation is reproduced | transition failure and baseline recovery | Conditional; pending authorization |
| I-06 Progression Save payload | Save contract owner | immutability, identity/result integrity and backend-neutral method matrix | scoped consumer evidence only where the Stable contract is consumed | Pending authorization |
| I-07 Identity/maturity conformance | Identity + Framework Core owners | equality/domain/default/parse/serialization and Stable metadata inventory | authoring reference round-trip on exact candidate | Pending authorization |
| I-08 Observer/API Governance/QA migration | API Governance + owning modules | helper unavailable to external callers and usable by derived components; capability tests stay with owners | migrate existing QA to explicit commands/Observer; compile and baseline cleanup | Pending authorization |

**Dependency waves:** Wave 1: I-01, I-06, I-07. Wave 2: I-02, then the
approved I-08 migration. Wave 3: I-03 after I-01/I-02/I-08. Wave 4: I-04 after
I-02/I-03. Wave 5: I-05, conditional on demonstrated Stable contract failure.
Release authority owns candidate freeze, exact-artifact identity and publication
gates after implementation and certification; release is not an I-01–I-08
implementation owner.

## Residual risks requiring explicit acceptance

| Residual risk | Recommended accepting authority | Current status |
|---|---|---|
| Terminal Unity/process exit ends in-memory retry after owner destruction | Framework Architecture + Release | Pending; no acceptance recorded |
| An in-flight GameFlow operation may keep controlled shutdown pending indefinitely; no arbitrary timeout | Framework Architecture + Release | Pending; no acceptance recorded |
| Route may return a truthful partial outcome; no blanket atomicity | GameFlow/Route contract owner + Release | Pending; no acceptance recorded |
| Experimental Player, Leave, Observer, Provisioning and Camera behavior remains outside Stable guarantees except causal Stable impact | API Governance + Release | Pending; no acceptance recorded |
| Save has no general atomicity/idempotency/async/provider-disposal guarantee; custom provider lifetime belongs to its integration | Save contract owner + Release | Pending; no acceptance recorded |

No risk acceptance is inferred from the prior approval of architectural direction.
The formal record must identify the accepting authority and bounded risk.

## Conditions for formal Gate A approval

Gate A may be recorded **GO** only when the record is updated with all of the
following evidence:

- Exact approved plan blob/revision and accepted normative decision references.
- Framework Architecture decision on IF-ADR-001 and IF-ADR-040 proposal text;
  Save/Architecture decision on IF-ADR-018 §11 clarification.
- API Governance disposition for `TryGetAccess`, IF-ADR-015 interpretation,
  Public-API scope and the QA migration; QA authority approval of the evidence
  plan and consumer callsites.
- Named accountable implementation, NUnit, QAFramework and Release owners for
  each applicable I-01–I-08 deliverable; no names are supplied by this draft.
- Explicit acceptance or rejection by the responsible authorities for every
  residual risk above, with scope and rationale.
- Objective PASS/FAIL criteria, dependency waves, rollback boundaries and
  required evidence retained as specified by the exact master-plan revision.
- Dated, explicit GO/NO-GO decision by the designated project Gate A authority.

## Gate and release separation

Gate A approves only the integrated implementation route. It does **not**
authorize implementation: a separate explicit implementation instruction is
required. Implementation completion does not certify the candidate. Certification
requires the IF-ADR-048 Phase 2 exit and subsequent candidate-specific evidence
through Phases 3–4. Publication requires a separate Phase 5 GO for the exact
version, source and artifact.

**Current disposition:** Gate A approval **PENDING**; no implementation is
authorized; IF-ADR-048 Phase 2 remains open; Stable 1.1.0 remains **NO-GO**.
