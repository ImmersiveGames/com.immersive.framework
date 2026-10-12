# IF-PLAN — Stable Manager-Provisioned Player Join Boundary

Status: **Proposed for review; execution not authorized**
Version: **v1**
Last updated: **2026-10-11**
Supersedes: none
Superseded by: none
Decision: [IF-ADR-046 — Stable Manager-Provisioned Player Join Boundary](../ADRs/IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md)
Governance: [IF-GOV-001 — API Maturity and Validation Governance](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
Evidence/status: [Player full recertification matrix](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md), [Framework tracker](../Tracking/IF-TRACK-Framework.md)

## Purpose

Define a reviewable route to implement and eventually certify IF-ADR-046. The ADR accepts architecture and contract design only. This proposed plan authorizes no Runtime, contract, test, consumer, maturity or release changes. Each stage needs explicit authorization; Stable promotion remains a separate IF-GOV-001 decision after evidence review.

The implementation delegates exactly once through the current `PlayerSessionScopedAccessRuntimeHostModule` to canonical `LocalPlayerProvisioningBridge`. It creates no second Session authority, reservation, manager, registry, service locator, fallback, Player-state store, or public access to `IPlayerSessionScopedAccess`.

## Accepted contract constraints

- Stable closure is exactly the public closure in IF-ADR-046: all `LocalPlayerJoinRequest` members, all `LocalPlayerProvisioningConsumerScope` values (`Unspecified = 0`, `Route = 10`, `Activity = 20`), `InputDevice`, and required Unity base types.
- Constructor signature/defaults, normalized `Source`/`Reason`/`ControlScheme`, exact `PairWithDevice` reference, hint/validity meanings and validation categories are compatibility commitments. Exact diagnostic wording and `ToDiagnosticString()` formatting are not.
- Enum names/numbers cannot be removed or renumbered. `Unspecified` and unknown values remain invalid and fail closed.
- `InputDevice` is an intentional public dependency. Framework currently declares Input System 1.19.0; I6 must review supported dependency range and changelog.
- `Joined` means technical Session admission after Host commit/registration, not Actor preparation/materialization or gameplay admission.
- `FailedRollback` preserves `OriginalCode`; `AdmissionState.Unknown` unless exact Session result is proven. Admission certainty is not proof of physical PlayerInput/Host destruction.
- Every normally completed invocation returns/stores the same non-null `LastOutcome`; expected unavailability is typed. Escaping unexpected projection exceptions convert to `FailedUnknown/Unknown` and internal diagnostics.
- Private lease = `RuntimeContentOwner` plus current occurrence identity. Route lease uses active Route occurrence sequence; Activity lease uses Route occurrence and Activity `TransitionSequence`. Asset-derived owner tokens alone cannot distinguish re-entry of the same definition.
- Existing Experimental trigger, interfaces, result/status and serialized consumer references remain operational and additive.
- Every new Unity-recognized `.cs` source, test, Editor script or QA asset must include its matching `.meta`; preserve all existing GUIDs and serialized references.

## Planned order

### I1 — Models and typed projection

**Likely Framework files:**

- `Runtime/PlayerParticipation/Contracts/LocalPlayerJoinOutcome.cs` (new)
- `Runtime/PlayerParticipation/Contracts/LocalPlayerJoinOutcomeCode.cs` (new)
- `Runtime/PlayerParticipation/Contracts/LocalPlayerJoinAdmissionState.cs` (new)
- `Runtime/PlayerParticipation/Runtime/LocalPlayerJoinOutcomeProjection.cs` (new internal mapper)
- `Tests/Editor/PlayerParticipation/LocalPlayerJoinOutcomeProjectionTests.cs` (new)
- `Tests/Editor/PlayerParticipation/Immersive.Framework.PlayerParticipation.Editor.Tests.asmdef` (verify direct references; retain `Unity.InputSystem` where signature use requires it)

**Preconditions:** approve plan and separately authorize I1; reread actual constructors/status semantics; no maturity promotion in this cut.

**Tests:** all mapping rows; `None`; unknown values; all `OriginalCode` and AdmissionState cases; null result; only `Joined` admits; `RejectedUnexpectedJoin` fails unknown if returned.

**Acceptance:** exhaustive mapping, non-null typed projection, original cause preserved, future statuses fail closed, no PlayerInput/Host/Session snapshot/Slot/Actor/message leaks into outcome.

**Risks:** legacy semantic drift; enum numeric compatibility; accidental success mapping for an unknown state.

### I2 — Scoped binding lease

**Likely Framework files:**

- `Runtime/PlayerParticipation/Runtime/PlayerSessionScopedAccessRuntimeHostModule.cs`
- `Runtime/PlayerParticipation/Authoring/PlayerSessionScopedAccessConsumer.cs` (internal recipient seam only)
- `Runtime/RouteLifecycle/RouteLifecycleRuntime.cs` (internal access to already-owned current Route occurrence sequence, if no accessor exists)
- `Runtime/ActivityFlow/ActivityFlowRuntime.cs` (current `ActivityReadinessOccurrence.TransitionSequence`)
- `Runtime/PlayerParticipation/Runtime/PlayerSessionJoinBindingLease.cs` (only if it owns cohesive shared validation)
- `Tests/Editor/PlayerParticipation/PlayerSessionJoinBindingTests.cs` (new)

**Preconditions:** I1 user-run Unity EditMode evidence; confirm sequence call paths; separate I2 authorization. Do not add a global counter or change public `RuntimeContentOwner` semantics.

**Tests:** exact Route/Activity discovery, invalid authored scope, mismatch, no/released binding, owner/module/runtime disposal, missing/unready Manager, current owner, stale lease after re-entering same Route or Activity asset, current-only rebinding, old broad-consumer regression.

**Acceptance:** existing host module remains sole binder/lifecycle owner; validate owner + occurrence + Manager before every call; no public resolver or broad access on the trigger; typed non-throwing availability/configuration results; one disposal/release.

**Risks:** mixing Route and Activity occurrence; refresh is polled so request-time validation is mandatory; old component becoming valid again on same-definition re-entry.

### I3 — Standalone authoring component

**Likely Framework files:**

- `Runtime/PlayerParticipation/Authoring/PlayerSessionJoinTrigger.cs` (new standalone MonoBehaviour)
- `Runtime/PlayerParticipation/Runtime/PlayerSessionScopedAccessRuntimeHostModule.cs` (discover/bind through same owner)
- `Editor/PlayerParticipation/PlayerSessionJoinTriggerEditor.cs` (only if attributes/tooltips cannot provide needed guidance)
- `Editor/PlayerParticipation/PlayerSessionCommandTriggerEditor.cs` (review distinction; do not rename/change legacy serialized fields)
- `Tests/Editor/PlayerParticipation/PlayerSessionJoinTriggerTests.cs` (new)
- Public API/usage/changelog docs updated only at I6

**Preconditions:** I2 user-run Unity EditMode evidence; separate I3 authorization; no old serialized field changes.

**Tests:** direct `RequestJoin` without access lookup; `Invoke`/`InvokeFromDevice`; exact InputDevice reference; null/unregistered device rejection before backend; stale/disposed/missing binding typed response; returned outcome equals `LastOutcome`; unavailable first call stores a result; injected projection exception stores FailedUnknown; self-destruction reentrancy; menu/Inspector and scope default.

**Acceptance:** new component distinct in Add Component from `PlayerSessionJoinCommandTrigger`; old assets/scripts remain untouched; one canonical join call maximum; LastOutcome invariant; Joined label excludes Actor/gameplay.

**Risks:** Unity callback reentrancy; duplicate authoring affordance; accidentally inheriting broad Experimental APIs; exact null semantics for `InvokeFromDevice` must follow ADR (invalid request).

### I4 — Integrated QA and physical compensation

**Owner and likely QAFramework files:**

- `Assets/QA-IF-ADR-PLAYER-SESSION/Scripts/QaPlayerSessionJoinScenario.cs` and `.meta` if Slice 1 needs direct trigger or causal assertions.
- `Assets/QA-IF-ADR-PLAYER-SESSION/Editor/QaPlayerSessionJoinSetup.cs` and `.meta` if persisted setup needs the new component; preserve idempotence.
- `Assets/QA-IF-ADR-PLAYER-SESSION/README.md`, `Documentation~/Certification/PlayerJoin/README.md`.
- New evidence in `Documentation~/Certification/PlayerJoin/Evidence/` from current provenance manifest template; preserve old evidence.

**Preconditions:** I1–I3 authorized/completed; recheck QA instructions, canonical patterns and paths; immutable candidate Framework SHA resolved; create manifest before manual Play Mode. Agent does not run Unity.

**Evidence:** typed success, invalid request and Joining-closed rejection; Session revision/reservation baseline; Actor preparation separate from Join; existing Experimental consumer regression; callback-then-throw plus observed destruction of exact operation-owned PlayerInput and Host; pending closed, Host not retained, Session restored, retry; FailedRollback original cause/uncertainty; replay Slice 1 after bridge fix on same SHA. Preserve first failure and cleanup details.

**Acceptance:** QA uses public actions/observations; deterministic contracts remain package NUnit, not duplicate QA menu tests; raw log/capture, timestamp, Unity/package versions, Framework SHA, setup identity and baseline recorded. Fake compensation request does not prove physical destruction; no PASS until user output reviewed.

**Risks:** `RejectPlayer` is `void`, so physical destruction failure is not representable; cleanup may obscure primary failure; old Slice 1 predates bridge fix.

### I5 — Consumer compatibility

**Owner and likely FirstGame files:**

- `LocalMultiplayerJoinTutorialController` plus its scene/prefab references, read-only by default; edit only with separate optional migration authorization.
- Package manifest/lock only if an exact candidate pin/update is authorized; do not treat local path as SHA provenance.

**Preconditions:** I4 evidence complete for candidate SHA; verify FirstGame resolved package source/version; authorize any consumer asset/source/dependency edit separately.

**Evidence:** compile/import and Play Mode success/rejection against same SHA; preserve existing Experimental observation, Slot/index presentation and tutorial behavior. Optional additive use of the trigger still retains separately Experimental observation/display needs; verify serialized references and no forced migration.

**Acceptance:** package, QA and FirstGame evidence identify the same candidate SHA; no tutorial regression; optional consumer migration remains additive. Static inspection is not consumer PASS.

**Risks:** trigger alone cannot stabilize broader Session observation/Slot display; local path lock may resolve different bytes.

### I6 — Compatibility review and separate maturity gate

**Likely Framework files:**

- `Runtime/PlayerParticipation/Contracts/LocalPlayerJoinRequest.cs`, `LocalPlayerProvisioningConsumerScope.cs`, new outcome types and `PlayerSessionJoinTrigger.cs` (status metadata only, if separately approved)
- `Documentation~/API/Public-API.md`, `Documentation~/Guides/Player-Usage.md`, `CHANGELOG.md`
- canonical Player matrix and `Documentation~/Architecture/Tracking/IF-TRACK-Framework.md`

**Preconditions:** I1–I5 evidence reviewed and same-SHA linked; full public closure audit including Input System range; separate explicit promotion decision under IF-GOV-001.

**Review/tests:** every public member; direct asmdef references to all consumed/signature-exposed assemblies (Runtime and Editor tests explicitly list `Unity.InputSystem` as needed; asmdef dependencies do not transit); Input System supported minimum and changelog; unknown statuses/enums fail closed; compatibility/migration notes; rerun authorized validation if candidate bytes change.

**Acceptance:** either separately approved Stable metadata with reproducible same-SHA evidence, or keep maturity Experimental and record concrete blockers. ADR acceptance/test PASS/plan completion alone never promotes.

**Risks:** trigger Stable with Experimental signature dependencies; enum maturity is type-level and affects existing consumers; dependency floor is consumer-visible; evidence may mismatch if bytes change.

## Gates and dependencies

- Review/approve this plan and authorize I1.
- Complete and review I1 before separately authorizing I2.
- Complete and review I2 before separately authorizing I3.
- Complete and review I3 before separately authorizing I4.
- Complete and review I4 before separately authorizing I5.
- Complete I5 before a separate IF-GOV-001 maturity decision for I6.

No stage authorizes the next. A material route change requires v2; progress/evidence status belongs only in the mutable tracker and canonical Player matrix.

## Non-goals

- No implementation or Runtime/API/test/consumer change in this documentation task.
- No maturity promotion before a separate I6 authorization and evidence decision.
- No QAFramework or FirstGame edits in this task.
- No changes to broad Session/Slot/assignment/Host/Actor contracts, Session authority, reservation policy, Scene-Provided Join, gameplay readiness or camera policy.
- No physical cleanup guarantee without observable evidence; no QA duplication of deterministic package tests.
- No commit, push, tag or release.

## Validation policy

Documentation-only validation: check links, paths, symbol names, occurrence claims and consistency with IF-ADR-046, IF-GOV-001, the Player matrix and tracker. No Unity, NUnit, Play Mode, smoke or batchmode. Future implementation stages must reread current source/asmdefs and owner state. Manual Unity/import/EditMode/Play Mode/consumer evidence must be user-run where required.

## Change policy

This v1 is proposed for review and cannot guide execution until approved and separately authorized. After approval, do not rewrite its body for progress; create a superseding version for material design changes. Use the Framework tracker for mutable status and the canonical Player matrix for certification evidence.
