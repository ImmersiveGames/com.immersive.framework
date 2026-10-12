# IF-ADR-046 Amendment 001 — Convergência para uma única boundary pública de Join

Status: **Accepted**
Date: **2026-10-11**
Amends: [IF-ADR-046 — Stable Manager-Provisioned Player Join Boundary](IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md)
Evidence/status: [Player recertification matrix](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md) and [Framework tracker](../Tracking/IF-TRACK-Framework.md)
Approved route: [IF-PLAN-ADR-046 v3](../Plans/IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v3.md)

## Context

IF-ADR-046 accepts an additive transition and explicitly requires a later migration proposal before removing the old API (§6.1–6.5). I3 now provides the standalone `PlayerSessionJoinTrigger`, but it remains Experimental until I4, consumer validation and the separate IF-GOV-001 maturity decision. The production-consumer audit finds old Join entrypoints in FirstGame and QAFramework. PEGA currently has no code or serialized-asset references in the inspected project files.

The accepted product decision is to converge on `PlayerSessionJoinTrigger` as the one public Manager-Provisioned Join request boundary after the migration gates. The current additive period is transitional, not a permanent dual-surface commitment.

## Accepted amendment

1. `PlayerSessionJoinTrigger` is the only intended public boundary for requesting Manager-Provisioned Join after convergence. It continues to delegate to the existing canonical Join flow.
2. Keep replaced Experimental entrypoints operational during I4 and coordinated consumer migration; migrate then retire them before the final Stable decision. Do not create a permanent compatibility layer without a separately justified responsibility.
3. The retirement scope includes `PlayerSessionJoinCommandTrigger`, `ILocalPlayerJoinAccess` and `PlayerSessionScopedAccessConsumer.TryGetJoinAccess`, and the direct public `LocalPlayerProvisioningAuthoring.RequestJoin` overloads when their consumers have migrated. The provisioning authoring component and its setup/runtime-owner responsibility remain.
4. Retire public `LocalPlayerJoinResult` exposure through the replaced `LastJoinResult`/request members with those entrypoints. Its Runtime evidence may remain internal. `LocalPlayerJoinRequest` remains because it is part of the new boundary. `LocalPlayerJoinStatus` remains Experimental while used by `PlayerActorPreparationRuntimeHostSnapshot.LastJoinStatus`; review it separately if that responsibility changes.
5. Retain `IPlayerSessionScopedAccess`, `PlayerSessionScopedAccessConsumer` and unrelated Session observation, Slot/Actor evidence, Leave, Joining policy, Actor selection/replacement and Gameplay Availability responsibilities. Evaluate each public contract on its own; do not remove it merely because it shares an implementation type with Join.
6. Preserve `LocalPlayerProvisioningBridge`, Manager/backend, Session authority, reservation/provisioning policy and Host preparation/rollback as the canonical Runtime implementation. This amendment does not authorize their removal or a second authority.
7. Migrate FirstGame and QAFramework scripts, scenes, prefabs, UnityEvents and script GUID references before retiring old components. Verify PEGA against the effective candidate package; absence in the current scan is not a permanent exemption.
8. Run final QA regression after retirement against the actual candidate bytes intended for Stable. Any old surface retained must have a distinct responsibility and explicit justification.
9. Do not promote the new boundary by this amendment. `PlayerSessionJoinTrigger` and its closure remain Experimental until I4/I5 evidence, final post-removal regression and the separate I6 decision under IF-GOV-001.

## Migration and retirement gates

Retirement requires an identified Framework candidate containing I2/I3, accepted integrated I4 evidence, migrated and validated FirstGame and QAFramework references, a PEGA recheck against its resolved package, no remaining supported consumer/QA references to the retiring request entrypoints, repaired Unity serialized references, package/consumer import and Play Mode validation, and final post-removal QA regression on the same candidate bytes. API removal and package release still require their own implementation/release workflow. Stable promotion remains a distinct I6 decision.

## Consequences

The boundary is the only intended consumer-facing Manager-Provisioned Join invocation after convergence. Rich Slot/index/Host/Actor evidence remains Experimental and must be obtained from separate observation contracts or remain in package-owned diagnostics. The amendment changes the migration policy, not the accepted request/outcome shape, scoped binding, canonical Join authority or maturity policy.

This accepted amendment supplements IF-ADR-046 §§6.1–6.5 and supersedes their migration policy only where the additive/no-removal wording conflicts with the finite convergence and retirement route above. The original ADR text and decision history remain intact. Approval of this amendment and v3 establishes the normative route; it does not authorize I4 execution, consumer edits, API removal, Runtime changes, maturity promotion or release in this task.
