# IF-PLAN — Stable Manager-Provisioned Player Join: Convergence Route

Status: **Approved — execution remains task-gated**
Version: **v3**
Date: **2026-10-11**
Supersedes: [v2](IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v2.md)
Decision: [IF-ADR-046](../ADRs/IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md)
Accepted amendment: [IF-ADR-046 Amendment 001](../ADRs/IF-ADR-046-AMENDMENT-001-ONE-PUBLIC-JOIN-BOUNDARY.md)
Governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
Mutable status/evidence: [Framework tracker](../Tracking/IF-TRACK-Framework.md) and [canonical Player matrix](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md)

## Purpose

Define the approved finite migration from the existing Experimental Manager-Provisioned Join entrypoints to `PlayerSessionJoinTrigger` as the single public Join boundary. This plan supersedes v2 as the current route and preserves v1/v2 as history. Plan approval does not itself authorize I4, consumer edits, API removal, maturity promotion, commit or release; each implementation stage remains subject to its task authorization. This task is documentation-only.

## Observed baseline

- Framework: `master`, HEAD `7b196a02b2e8c746c852050e37c89b759e083aca`; I2/I3 Runtime, tests and docs are present as uncommitted changes. It is not yet a Git baseline containing I2/I3.
- QAFramework: `main`, HEAD `6bb7d12aa22353041024742a03de9b121fc3e85b`, clean; resolves Framework through the local file path.
- FirstGame (`planet-devourer`): `main`, HEAD `98a0c23ea33f11e721afe80161fe0aba8cb7acc7`, clean; resolves Framework through the same local file path.
- PEGA: branch `main`, HEAD `0db7450366a4ed8b8884ae69faa072de9f5ed7f8` was readable; Git status could not be read (`git status` reported that the operation must run in a work tree / access denied). Its package lock and cache resolve Framework `1.1.0-preview.7` from the registry; that installed source predates I3.
- User-reported Unity Test Runner result: EditMode **289/289 PASS**, PlayerParticipation **98/98 PASS**, zero failures. The run is reported by the user, not executed by the agent; no run SHA/log/XML/timestamp was supplied, so association to exact source bytes remains partial. Counts are aggregate and do not identify individual fixture results.

## Consumer and QA inventory

| Repository | Join invocation dependencies | Independent dependencies that remain |
|---|---|---|
| FirstGame | `LocalMultiplayerJoinTutorialController` uses `PlayerSessionJoinCommandTrigger.InvokeFromDevice` and `LastJoinResult` for Join/status; Slot ID and Unity player index are displayed. The trigger GUID `5a1ab0b68f54b074babd906b60740182` is serialized in one prefab and two scenes. | The same controller uses `IPlayerSessionScopedAccess.Changed` and `TryGetObservation` for Joining state, Slot participation and owned-device observations. This is observation, not a Join request dependency. Migrate Join only; provide presentation data through the separate Experimental observation surface or remove the presentation requirement by product decision.
| PEGA | No matching Join symbol or script GUID was found in project code, scenes, prefabs, assets or configuration. | No migration is indicated by this scan. Recheck after the consumer resolves a candidate that actually includes I3; current resolved `.7` does not contain `PlayerSessionJoinTrigger`.
| QAFramework | The old trigger is constructed by three Editor setup scripts and referenced by three scenario scripts and three serialized Activity scenes. The I4 proposal already calls `PlayerSessionJoinTrigger`, while Slice 1's old-trigger regression is explicitly retained. Other QA scenarios call `LocalPlayerProvisioningAuthoring.RequestJoin` directly. | QA-IF-ADR-044 also uses scoped observation and `IPlayerSessionScopedAccess` block/release for its actual Gameplay Availability contract. QA-IF-ADR-042/043 and Player Session scenarios inspect Slot, Host, `PlayerInput`, Actor and rollback evidence; keep those assertions through suitable public observation or package-owned tests, not through a broad deletion of evidence contracts.

## Approved order and gates

### Stage A — I2/I3 baseline

Review the current Framework diff, confirm the source/test inventory and record the user-reported 289/289 EditMode and 98/98 PlayerParticipation result as partial-provenance aggregate evidence. Prepare a local Git baseline that includes I2/I3 and identifies candidate bytes; if the aggregate run cannot be associated with that candidate, run the package tests again in the authorized Unity context. The current task does not create the commit.

### Stage B — I4 in QAFramework

After separate I4 task authorization, certify `PlayerSessionJoinTrigger` through public behavior. Exercise current and stale Route/Activity occurrence bindings, reentry, stale-call rejection, no unintended Session effect, cleanup and baseline restoration. Preserve first failure and candidate provenance. This approval task does not implement or execute I4.

### Stage C — consumer migration

After I4 evidence is accepted and consumer work is authorized, migrate the FirstGame Join request/result handling and QA scenarios that depend on replaced entrypoints. Verify PEGA against the effective candidate and migrate only if actual references exist. Preserve separate Session/Slot/Actor observation and other unrelated capabilities. Repair scene, prefab, UnityEvent and GUID references through controlled Unity authoring.

### Stage D — retire replaced surfaces

Audit all remaining C#, QA, configuration and serialized references. With separate removal authorization, remove only the superseded public Join invocation surfaces; retain the canonical Runtime authority and contracts that still serve independent responsibilities. Reimport/reopen migrated assets and run a new regression after removal.

### Stage E — final Stable gate

Review public closure and dependencies; associate Framework, QAFramework and consumer evidence with the actual candidate bytes; confirm no obsolete public Join entrypoints remain; run final QA regression after removal; and then make a separate IF-GOV-001 maturity decision. I4 completion or consumer migration alone is insufficient for Stable.

## Replacement classification and boundaries

- **A — replaceable Join request path:** `PlayerSessionJoinCommandTrigger`; `ILocalPlayerJoinAccess` and `PlayerSessionScopedAccessConsumer.TryGetJoinAccess`; public `LocalPlayerProvisioningAuthoring.RequestJoin` overloads. `LocalPlayerJoinResult` public exposure through legacy `LastJoinResult`/request methods is a retirement candidate; its internal Runtime evidence may remain. `LocalPlayerJoinRequest` remains in the new public contract.
- **B — still needed for other contracts:** `IPlayerSessionScopedAccess` and the shared scoped-consumer infrastructure remain for observation, Slot/Actor operations, Leave, Joining policy and Gameplay Availability. `LocalPlayerJoinStatus` remains Experimental while the public `PlayerActorPreparationRuntimeHostSnapshot.LastJoinStatus` uses it; removal requires separate review. FirstGame's Slot ID/player-index display is not supplied by the bounded outcome.
- **C — canonical Runtime, preserve:** `LocalPlayerProvisioningAuthoring` setup/Manager ownership, `LocalPlayerProvisioningBridge`, Manager/backend, reservation/provisioning rules, Host preparation/rollback and the existing canonical call. The public Join request methods on the authoring component are category A; the authoring/runtime owner itself is category C.

## Serialized-reference and validation policy

The old trigger GUID appears in three FirstGame assets and three QAFramework scenes. Replacing/deleting the script before those components and UnityEvent target type strings are migrated risks Missing Script references or broken callbacks. Preserve the old script/GUID until all referencing scenes/prefabs are migrated and reopened/saved/verified in Unity. GUID absence from PEGA's scanned assets does not prove Git cleanliness there because PEGA `git status` was unavailable.

Do not treat static search as compile/import or scene serialization proof. Unity import, consumer compile, Play Mode, asset reference validation and full QA regression remain manual gates. No execution is attributed to this plan.

## Responsibilities

- **Framework:** API/Runtime authority, deterministic package tests, public contracts/governance and implementation of the authorized retirement.
- **QAFramework:** integrated lifecycle scenarios, Session/cleanup evidence and final post-migration/post-removal regression.
- **FirstGame and PEGA:** consumer compatibility, import/compile/functional validation and controlled migration where needed.

## Non-goals and execution boundary

No I4 implementation/execution, consumer edit, dependency update, Runtime/API removal, Stable promotion, commit, tag, push or release is authorized by this documentation task. Those steps follow this approved route only when the corresponding task grants authorization. Track progress in the Framework tracker; do not rewrite this approved plan.
