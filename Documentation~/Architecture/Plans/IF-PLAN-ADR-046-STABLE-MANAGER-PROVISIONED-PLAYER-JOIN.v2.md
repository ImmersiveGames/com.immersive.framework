# IF-PLAN — Stable Manager-Provisioned Player Join Boundary

Status: **Superseded (was Approved)**
Version: **v2**  
Last updated: **2026-10-11**  
Supersedes: [v1](IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v1.md)  
Superseded by: [v3](IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v3.md)
Decision: [IF-ADR-046 — Stable Manager-Provisioned Player Join Boundary](../ADRs/IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md)  
Governance: [IF-GOV-001 — API Maturity and Validation Governance](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)  
Evidence/status: [Player full recertification matrix](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md), [Framework tracker](../Tracking/IF-TRACK-Framework.md)  
QA scenario reference: QAFramework checkout, `Documentation~/Certification/PlayerJoin/IF-ADR-046-I4-Scoped-Binding-Scenario.md` (local proposal; not published).

## Purpose

Define the revised route for IF-ADR-046 after assigning deterministic implementation tests to the Framework and integrated public-contract certification to QAFramework. I1 and I2 implementation work is present. I2 certification is incomplete and moves to I4; its EditMode tests are written but have not been executed in the Unity Test Runner. This plan is approved. I3 is authorized as the next implementation increment; QA scenario implementation, maturity promotion, release, and consumer migration remain separately gated.

I3 is explicitly authorized as the next implementation increment, subject to its defined static/dependency gate. Each later increment still requires its own explicit implementation authorization. Stable promotion remains a separate IF-GOV-001 decision based on reviewed, same-candidate evidence.

## Constraints accepted from IF-ADR-046

- The new public boundary delegates to the existing canonical Manager-Provisioned Join path; it adds no Session authority, reservation policy, global lookup, Manager, registry, fallback, or Player-state store.
- The Stable closure remains exactly the request, scope enum, bounded outcome/code/admission state, Unity types, and Input System dependency defined by IF-ADR-046. The broad Session/Slot/assignment/Host/Actor APIs and `LocalPlayerJoinResult` remain Experimental.
- Route/Activity scope discovery stays in the existing lifecycle and binder. The private lease combines exact `RuntimeContentOwner` with current Route occurrence and, for Activity scope, current Activity transition occurrence.
- `Joined` means canonical technical Session admission and Host commit/registration. It does not imply Actor preparation/materialization or gameplay admission.
- `FailedRollback` preserves `OriginalCode` and reports `AdmissionState.Unknown` unless exact Session recovery is proven. Neither status promises physical destruction without Unity evidence.
- Existing Experimental consumers remain supported during additive migration. `InputDevice` is an intentional dependency; every consuming test/QA asmdef must directly reference `Unity.InputSystem` when it uses that assembly's symbols.
- IF-GOV-001 controls maturity. An accepted ADR, a completed plan stage, or a reported test run does not promote an API.

## Planned order and gates

### I1 — Bounded outcome and projection

**State at v2:** implementation and tests present; static review recorded. The user reported Unity EditMode **265/265 PASS**, PlayerParticipation **74/74 PASS**, zero failures, including 36 I1 test invocations. The report has partial provenance: no execution SHA, timestamp, XML, or raw log links that run to exact workspace bytes. Do not assign retrospective SHA or treat that run as evidence for I2–I4.

**Acceptance:** retain only essential outcome information; exhaustively map known statuses; preserve the original cause for rollback failure; fail closed for unknown statuses; only `Joined` maps to `Admitted`. All I1 types remain Experimental.

### I2 — Internal scoped binding lease

**Owner:** Framework package implementation and deterministic package tests.

**State at v2:**

| Dimension | Recorded state |
|---|---|
| Implementation | Present in the existing binder/consumer path; occurrence-bound lease uses the current Route and Activity lifecycle identities. |
| Static review | Recorded as complete; no public contract or Session authority change is part of I2. |
| EditMode tests | Written; **not yet executed**. Existing sources do not provide a Unity result for this workspace state. |
| Integrated certification | Deferred to I4 in QAFramework. I2 is **not certified**. |

**Gate to begin I3:** review the current I2 diff and its lease/recipient invariants; confirm the public contract and implementation dependencies from IF-ADR-046, including direct Input System assembly references; resolve any concrete static defect. Integrated I2 certification is not a precondition to start I3 and must not be represented as complete.

**Acceptance:** the existing binder remains the sole owner; no occurrence counter/authority is introduced; invocation validates current owner, exact recipient, runtime and Manager before delegating; old and current Experimental consumers remain additive. Package tests may prove deterministic mechanics, but only I4 can certify the integrated public behavior in real composition.

### I3 — Standalone public Join boundary

**Owner:** Framework package.

**Likely files:** `PlayerSessionJoinTrigger`, its private internal join projection/recipient integration, API status metadata kept Experimental, `.meta`, direct-reference asmdefs, and package EditMode tests. Final paths must be checked against current source during I3.

**Scope:** implement the standalone component and complete request/result behavior already accepted in IF-ADR-046. The component must not inherit or expose `IPlayerSessionScopedAccess`, `PlayerSessionJoinBindingLease`, `ValidateCurrent`, `LocalPlayerJoinResult`, Session snapshots, Slot/Host/Actor state, or a test-only public resolver.

**Tests:** component invocation and `LastOutcome`; exact `InputDevice` reference; all typed mappings; binding absent/released/stale/unavailable; no success inference from Actor/gameplay; legacy Experimental consumer regression. Preserve direct `Unity.InputSystem` references in each consuming test asmdef.

**Acceptance:** one canonical Join delegation maximum per request; all completed requests have the specified typed result; unavailable/stale access fails closed; legacy serialized components remain usable; no public type is promoted by implementation.

**Risks:** destroyed Unity wrappers may not be valid invocation targets; this must be tested through supported public behavior and reported as BLOCKED if Unity cannot safely exercise the retained reference. Do not add a public lifecycle/test backdoor to bypass that constraint.

### I4 — Integrated QAFramework certification of I2 + I3

**Owner:** QAFramework. The separate scenario specification is in the QAFramework checkout at `Documentation~/Certification/PlayerJoin/IF-ADR-046-I4-Scoped-Binding-Scenario.md` (local proposal; not published). It is a scenario design, not authority over the Framework contract.

**Reused infrastructure:** preserve Slice 1. Reuse its validated Manager-Provisioned setup conventions and, where references can be shared without changing Slice 1 ownership, its Session/Slot/Host/Camera/Persistent assets. Reuse the causal Route A→B→A pattern in QA-NEW-003 and Activity occurrence/pending-operation patterns in QA-NEW-002. Create a separate QA-owned setup and scenario assets for scoped binding; do not edit or extend Slice 1 to carry this proof.

**Proof:** public `PlayerSessionJoinTrigger` calls and outcomes; real Framework bootstrap, Session, Manager and Route/Activity composition; same-definition reentry; Session snapshots/revisions and reservation/Join state; exact synthetic device/Host cleanup. The scenario must distinguish an observable fail-closed/no-effect result from the stronger literal assertion that no internal bridge method was invoked. QA may assert only public behavior. If literal zero invocation is a required invariant, retain it in a package-owned deterministic test seam; do not expose a QA backdoor or claim the terminal proves an unobservable call count.

**Required gates:** package EditMode suite and QAFramework scenario executed by the user in Unity on an identified candidate; raw terminal/log and provenance recorded; Route and Activity stale binding rejected; current binding remains usable; zero Join/reservation/Player effect for stale invocation; Session/physical cleanup evidence; legacy consumer unaffected; no Scene-Provided behavior or Actor/gameplay state inferred from `Joined`. Slice 1 post-rollback-fix replay and its provenance remain separate required Player certification evidence.

**Acceptance:** I2 and I3 integrated guarantees are supported by distinct per-case evidence and the terminal cleanup establishes the declared baseline. First causal failure survives unwind/cleanup failures. QA does not duplicate deterministic NUnit contracts. I4 blocks I5 if a stale-reference, cleanup, or observability criterion remains BLOCKED or unproven.

### I5 — Consumer compatibility

**Owner:** FirstGame, only after I4 acceptance.

Review the installed package source/version and migrate only if separately authorized. Compile/import and exercise the existing tutorial and any additive trigger use against the same candidate SHA. Existing Experimental observation, Slot/index presentation, and serialized references remain explicit compatibility concerns; the new Join outcome does not stabilize those contracts.

**Acceptance:** Framework package, QA evidence, and consumer run identify the same candidate; existing flow remains operational; no static inspection is reported as consumer PASS. No FirstGame/PEGA modifications are authorized by this plan alone.

### I6 — Compatibility and maturity decision

**Owner:** Framework governance under IF-GOV-001, only after I1–I5 evidence review.

Audit every public member and dependency closure, including the supported Input System version range, migration/changelog, unknown enum/status behavior, Experimental neighboring contracts, same-SHA package/QA/consumer evidence, and unresolved physical-cleanup promises. Promotion requires a separate explicit decision; absent it, keep the whole new closure Experimental and record blockers in the matrix/tracker.

## Non-goals

- I3 is the next authorized increment. I4 implementation, consumer migration, maturity promotion, package release, commit, tag, and push are not authorized by this plan.
- No QAFramework reproduction of Framework NUnit internals or exposure of lease internals to QA.
- No changes to `LocalPlayerProvisioningBridge`, Session authority/policy, Scene-Provided Join, Actor/gameplay semantics, broad Session/Slot/Host contracts, FirstGame or PEGA as part of I2/I3.
- No claim of physical destruction or complete rollback without direct integrated evidence.

## Validation policy

Documentation changes: static link/path/role consistency only. Future code stages must re-read current source, asmdefs, package locks, ownership and setup paths. Do not run Unity, NUnit, Play Mode, build or smoke from the agent. The user-run Unity Test Runner and QAFramework logs must identify their candidate bytes and retain the first failure and cleanup disposition. No PASS is registered from source review alone.

## Change policy

This approved v2 is the immutable execution route; progress and evidence belong in the single Framework tracker and canonical Player matrix. Material architectural change requires another version. I3 is authorized as the next increment; later increments still require separate authorization.
