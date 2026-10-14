# IF-ADR-048 — Gate A Integrated Plan Approval Record (v2)

**Status:** APPROVED — GO
**Decision date:** 2026-10-13
**Framework repository:** `master` at `861620ca00b662a31d3542ff79b5c1f632d843ee` before this record
**Plan:** [`IF-PLAN-ADR-048-STABLE-READINESS.v1.md`](IF-PLAN-ADR-048-STABLE-READINESS.v1.md), exact Git blob `628f64b2878cdad9fbf8d89392e8e39725b6fe3a`
**Preparation record:** [`IF-GATE-A-ADR-048-STABLE-READINESS-REVIEW.v1.md`](IF-GATE-A-ADR-048-STABLE-READINESS-REVIEW.v1.md)
**Prior approval:** Explicit user authorization in the current Codex session, following the consolidated Gate A decision request. The requester is recorded as **Project Owner (current human requester; display name is not exposed to the agent)**. No personal name is inferred.
**Effect:** Approves the integrated implementation route and the decisions below. The conditional implementation authorization in the mission is effective. This record does not close IF-ADR-048 Phase 2, certify a candidate or authorize publication.

## Authority assignment and decision

The Project Owner acts cumulatively as the designated authority for Framework Architecture, Save/Architecture decision, API Governance, QA evidence-plan approval, Release route approval, and the integrated Gate A decision. The project rules reviewed for this gate require approval by these functional authorities but do not require distinct people. The same Project Owner is accountable for assigning implementation, NUnit, QAFramework and Release owners for I-01 through I-08; execution is coordinated by Codex in this task. No independent reviewer is claimed.

The user message “não precisa de confirmação formal, é só fazer eu autorizo”, in direct response to the consolidated decision request, is recorded as approval of that request's full scope, including the decisions and bounded risk acceptances below. This interpretation records the approval in context and does not invent a separate signature or approver.

**Decision: GO** for the exact plan blob stated above, including workpackages I-01–I-08 and the approved dependency waves:

1. Wave 1: I-01, I-06 and I-07.
2. Wave 2: I-02, then I-08.
3. Wave 3: I-03 after I-01, I-02 and I-08.
4. Wave 4: I-04 after I-02 and I-03.
5. Wave 5: I-05, conditional on a demonstrated Stable contract violation.

## Accepted normative and API decisions

- **IF-ADR-001:** Accept Amendment 001 covering internal Host phases, fail-closed admission, the exact cleanup/retry allowlist, dependency-driven controlled shutdown, retained Host/owners/receipts on recoverable rejection, and terminal Unity callbacks as best-effort. No public Host state, shutdown API, global registry or second authority is added.
- **IF-ADR-040:** Accept Amendment 001 retaining every potentially activated Session participant before `Available`, including the rejecting participant; preserve the original composition failure and compensation diagnostics; aggregate release and retain the complete exact owner/participant ledger until successful release; allow same-scope idempotent retry.
- **IF-ADR-018 §11:** Accept the editorial correction. The 13-type backend-neutral Stable Save set remains Stable; `ProgressionSaveRuntime`, request orchestration, providers, profiles, catalog and concrete backend APIs retain their declared maturity. No atomicity, idempotency, async, durability or provider-disposal guarantee is added.
- **API Governance:** Restrict inherited `TryGetAccess` to derived implementation (`protected` or equivalent); preserve `PlayerSessionObserver` as read-only; preserve `PlayerGameplayAvailabilityBlockTrigger` as owner of its one Stable block/release; retain `IPlayerSessionScopedAccess` as Experimental; make no IF-ADR-015 amendment; migrate the QA callsites listed in the exact plan to existing command triggers and Observer reads. Do not add command triggers to expose test-only capabilities.
- **Preserved decisions:** IF-ADR-036, IF-ADR-044 and IF-ADR-046 remain unchanged. No Experimental surface is promoted and no generic pre-1.1 compatibility requirement is created.

## Evidence plan approval

The Project Owner approves the plan's evidence matrix and PASS/FAIL/BLOCKED criteria:

- Focused NUnit conformance, fault injection, rollback, retry, release and cleanup evidence for each applicable workpackage.
- Unity import/compile and the required EditMode/Play Mode integration scenarios when the authorized Unity environment is available. An unavailable execution environment remains BLOCKED; it is not reported as PASS.
- QAFramework integrated scenarios, with QA-owned fixtures and cleanup evidence.
- FirstGame and PEGA consumer integration and validation against the same immutable candidate bytes, with each consumer's own resolved package provenance recorded.
- Candidate inventory and contract-to-test-to-consumer traceability, source SHA and artifact SHA-256, plus repeated evidence for any later byte, metadata or code change.

Approval of this plan approves evidence collection only; it does not assert that a test has run or passed.

## Explicit residual-risk acceptance

The Project Owner, acting in the responsible functional roles named above, accepts these bounded residuals for the 1.1.0 readiness route:

| Risk accepted | Bounded disposition |
|---|---|
| Terminal Unity/process callbacks are best-effort; process/owner destruction ends in-memory retry. | Controlled stop must retain a live Host and exact owners while retry remains possible. No retry after terminal destruction is promised. |
| An in-flight GameFlow operation may keep controlled shutdown pending indefinitely. | No arbitrary timeout or owner destruction is introduced; pending work retains the Host and dependencies until a terminal result. |
| Route can produce truthful partial outcomes. | Results report the authority actually committed; no blanket atomic rollback is promised. |
| Save has no general atomicity, idempotency, async-completion, durability or provider-disposal guarantee. | The existing Stable Store contract remains backend-neutral; custom provider lifetime remains with its integration. |
| Experimental Player, Leave, Observer, Provisioning, Actor Selection and Camera behavior is outside Stable guarantees. | Excluded unless evidence demonstrates a causal violation of an existing Stable guarantee; no maturity promotion is authorized. |

## Accountable ownership

The Project Owner is the named accountable authority for assigning and tracking implementation, NUnit, QAFramework and Release owners for each workpackage. Codex `/root` coordinates implementation and evidence in this task; specialized executors may assist without receiving architecture, API Governance, QA-approval or Release authority. Existing technical ownership remains as mapped in the approved plan: Scene Lifecycle; Availability and Pause/Input; Framework Host/Bootstrap; canonical Leave/Camera/Activity; GameFlow/Route/Activity; Progression Save; Identity; and API Governance/QAFramework. Assignments and execution evidence will be recorded in the tracker before declaring a workpackage complete.

## Gate and release boundary

Gate A is **GO**. IF-ADR-048 Phase 2 remains open until operational evidence is executed, reviewed and accepted. Phases 3–4 remain pending until candidate-specific recertification and exact-artifact consumer validation complete. Stable 1.1.0 publication remains **NO-GO** until the separate Phase 5 decision for the exact version, source SHA and artifact hash. This record grants no push, tag, merge, GitHub Release, registry publication or other external release action.

## Provenance

- The master plan's blob identity was verified as `628f64b2878cdad9fbf8d89392e8e39725b6fe3a` before approval documentation changed.
- The prepared v1 review record is preserved unchanged as the history of pending review.
- Normative amendments are added to IF-ADR-001, IF-ADR-040 and IF-ADR-018 without changing their original accepted decisions or expanding public API beyond the approved API Governance disposition.

## Project Owner implementation ruling — Route Primary Scene

On 2026-10-13, the Project Owner explicitly selected **preserve the active
Primary Scene** when asked how controlled Route exit should resolve the active
Unity scene. The Primary Scene stays loaded and active, its GameObjects remain
in place, and Route authority plus Framework composition scopes are released.
Only owned additive Route scenes are unloaded. This ruling is recorded as
IF-ADR-001 Amendment 001-A and does not change the approved plan blob.
