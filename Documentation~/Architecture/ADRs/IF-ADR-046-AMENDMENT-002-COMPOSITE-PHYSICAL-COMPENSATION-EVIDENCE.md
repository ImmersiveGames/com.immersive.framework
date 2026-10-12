# IF-ADR-046 Amendment 002 — Evidência composta de compensação física

Status: **Accepted**
Date: **2026-10-12**
Amends: [IF-ADR-046 — Stable Manager-Provisioned Player Join Boundary](IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md), §7 gate 5
Decision record: **Explicit governance decision supplied for this amendment**
Evidence/status: [Player recertification matrix](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md) and [Framework tracker](../Tracking/IF-TRACK-Framework.md)
Approved route: [IF-PLAN-ADR-046 v3](../Plans/IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v3.md)

## Context

IF-ADR-046 §7 originally required integrated Play Mode evidence that callback-then-throw compensation physically destroys the operation-owned `PlayerInput`/Host. Existing evidence separates the bridge failure test from real Unity provisioning and cleanup scenarios. The responsible authority explicitly accepts that evidence by composition for the bounded `PlayerSessionJoinTrigger` maturity decision.

## Accepted amendment

1. Replace the requirement for one integrated callback-then-throw Play Mode execution with acceptance of the following composed evidence:
   - **Bridge and logical rollback — executed test, user-reported with partial provenance:** `LocalPlayerProvisioningBridgeRollbackTests.BackendCreatesCallbackPlayerThenThrows_RollsBackAndRequestsCompensation` exercises callback followed by exception, releases the Session reservation, preserves the original failure, leaves no admitted/staged Host or pending operation, and asserts that the exact callback `PlayerInput` reference is sent to compensation. Its fake backend records the request; it does not destroy a Unity object.
   - **Physical compensation — static implementation inspection:** `UnityLocalPlayerProvisioningBackend.RejectPlayer` deactivates the received `PlayerInput` and destroys that exact object's GameObject through the shared physical-destruction helper.
   - **Unity runtime behavior — user-reported Play Mode observations with partial provenance:** Player ownership, Leave/Rejoin and IF-ADR-042/043/044 scenarios demonstrate real Manager provisioning and physical release/recovery on their covered normal lifecycle paths. Slice 1 was reported as a post-correction `PASS 8/8`, `BaselineRestored`; this remains a report without associated raw log, timestamp or execution SHA.
   - **Framework/QA regression — user-reported test results with partial provenance:** Framework EditMode `292/292`, PlayerParticipation `101/101`, and QA IF-ADR-042/043/044 `22/22`, with cleanup `3/3` and `BaselineRestored`.
2. Record the physical-compensation gate as **ACCEPTED BY COMPOSITE EVIDENCE**. Do not record it as an integrated Play Mode PASS.
3. This decision changes the evidence criterion for this gate only. It does not change Runtime behavior, weaken the rollback contract, promote any API automatically, or extend Stable status to Session, Slot, Actor, Input or the Framework as a whole.

## Accepted residual risks

The composition does not:

- observe one integrated transaction in which the real Unity backend receives the callback object and compensates after the callback throws;
- observe physical destruction in the same execution that injects the exception;
- demonstrate a subsequent Join specifically after that exception; or
- guarantee compensation if `Object.Destroy` itself fails. The current backend `RejectPlayer` returns `void`, so that failure is not reportable through the Join outcome.

The responsible authority accepts these limits as residual risk for the maturity decision of the public Join boundary. No unobserved behavior is reported as a test PASS. If future product requirements require observable physical-compensation failure or direct proof of that integrated path, reopen this decision and define a separately reviewed contract/evidence change.

## Remaining IF-ADR-046 gates

This amendment does not waive the independent gates in IF-ADR-046 §7. In particular, reported executions still need candidate-linked provenance, and FirstGame must be imported/compiled and exercised against the same identified immutable Framework candidate. The reachability of a failed Session commit must also be decided; a deterministic package-owned test is required only if that failure is reachable within the public guarantee. The existing public `InputDevice` dependency is recorded in the API/asmdefs and `package.json` pins Input System 1.19.0; the package Unity floor is 6000.5, the preview changelog records the boundary/removal, and Amendment 001 records migration. This documentation review closes §7's dependency/changelog item for the current closure; no package version or release is changed.

Amendment 002 supplements IF-ADR-046 §7 by replacing only its physical-compensation evidence criterion. It preserves the original requirement and evidence history as historical context; it does not revise Amendment 001 or the approved execution order in v3.
