# IF-ADR-046 — Stable Manager-Provisioned Player Join Boundary

Status: **Proposed — pending architectural approval; not normative and not implemented**  
Proposed: **2026-10-11**  
Type: Player Participation / public compatibility boundary / scoped authoring  
Related decisions: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md), IF-ADR-003, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020  
Current evidence and gates: [Player full recertification matrix (P6/P7)](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md)

> This is a design proposal only. It authorizes no Runtime/API/test/consumer changes, no maturity promotion, and no release. IF-GOV-001 remains authoritative for status and compatibility policy.

## 1. Context

The current Manager-Provisioned entry point is `ILocalPlayerJoinAccess.RequestJoin(LocalPlayerJoinRequest) -> LocalPlayerJoinResult`. The current result publishes reservation/commit/rollback records, Session/Slot snapshots, Actor selection evidence, Host and `PlayerInput` references, callback evidence and assignment projections. These members are all on one Experimental result type. Stabilizing that type would commit consumers to a substantially broader Session/Slot/assignment closure than the Join operation needs.

Consumers also need a scoped route to the capability. Today `PlayerSessionScopedAccessRuntimeHostModule` discovers `PlayerSessionScopedAccessConsumer` components inside the current Route/Activity discovery scopes, validates their authored scope, binds an access object to a `RuntimeContentOwner`, and releases stale bindings. Its public typed access is `IPlayerSessionScopedAccess`, an Experimental interface covering many unrelated operations. A consumer of stable Join must not need to resolve that broad interface merely to invoke Join.

The current authoritative operation remains in `LocalPlayerProvisioningBridge`, called by `ManagerPlayerSessionScopedAccess.RequestJoin`; `LocalPlayerProvisioningPreparationExtensions.RegisterJoinWithActorPreparation` registers Host preparation lifecycle evidence and rolls back if that internal registration fails. The new surface must only project this operation and preserve the existing authority.

## 2. Proposed decision

Add a narrow Stable authored command component with a typed synchronous outcome. Bind it through the existing Route/Activity lifecycle discovery and ownership checks, but give it only a private, narrow join projection. Do not make `IPlayerSessionScopedAccess` or `LocalPlayerJoinResult` part of its public invocation signature.

The component MUST:

- invoke the existing Manager-Provisioned Join flow exactly once per request;
- use only the currently bound Route or Activity owner and fail closed when unavailable, mismatched, stale or disposed;
- preserve the exact input device reference when one is supplied;
- return a typed outcome for all completed calls; the last-outcome property is null only before the first invocation;
- define `Joined` as successful technical Session admission and Host commit/registration, not Actor preparation, materialization, Gameplay Readiness or gameplay admission;
- preserve the original semantic cause when rollback itself fails;
- delegate physical compensation to the current Manager provisioning owner;
- avoid promising physical destruction or complete recovery unless integrated evidence can observe and verify it.

The component MUST NOT create a second Session, reservation mechanism, Manager, global registry, fallback path or Player-state store. `LocalPlayerProvisioningBridge` remains the operation and compensation authority. Scene-Provided Join remains a separate lifecycle and is never a fallback.

## 3. Proposed public surface

The names below are the recommended complete first version. They are subject to approval before implementation.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-046 scoped Manager-Provisioned Join command.")]
public sealed class PlayerSessionJoinTrigger : MonoBehaviour
{
    [SerializeField]
    private LocalPlayerProvisioningConsumerScope scope =
        LocalPlayerProvisioningConsumerScope.Unspecified;

    [SerializeField] private string reason;
    [SerializeField] private string controlScheme;

    public LocalPlayerProvisioningConsumerScope Scope => scope;
    public LocalPlayerJoinOutcome LastOutcome { get; }

    public LocalPlayerJoinOutcome RequestJoin(LocalPlayerJoinRequest request);
    public void Invoke();
    public void InvokeFromDevice(InputDevice device);
}

[FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-046 Manager-Provisioned Join request intent.")]
public readonly struct LocalPlayerJoinRequest
{
    public LocalPlayerJoinRequest(
        string source,
        string reason,
        InputDevice pairWithDevice = null,
        string controlScheme = null);

    public string Source { get; }
    public string Reason { get; }
    public InputDevice PairWithDevice { get; }
    public string ControlScheme { get; }
    public bool TryValidate(out string issue);
}

[FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-046 bounded Manager-Provisioned Join outcome.")]
public sealed class LocalPlayerJoinOutcome
{
    public LocalPlayerJoinOutcomeCode Code { get; }
    public LocalPlayerJoinOutcomeCode? OriginalCode { get; }
    public LocalPlayerJoinAdmissionState AdmissionState { get; }
}

[FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-046 consumer-level Join result codes.")]
public enum LocalPlayerJoinOutcomeCode
{
    Joined = 1,
    RejectedInvalidRequest = 10,
    RejectedBusy = 11,
    RejectedUnavailable = 12,
    RejectedConfiguration = 13,
    RejectedJoiningClosed = 14,
    RejectedNoAvailableSlot = 15,
    RejectedDeviceAlreadyOwned = 16,
    RejectedSessionChanged = 17,
    FailedAdmission = 30,
    FailedRollback = 31,
    FailedUnknown = 39
}

[FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-046 certainty of Session Player admission for one Join result.")]
public enum LocalPlayerJoinAdmissionState
{
    NotAdmitted = 1,
    Admitted = 2,
    Unknown = 3
}
```

`LocalPlayerProvisioningConsumerScope` is included in the Stable authoring closure only for its explicit Route/Activity ownership choice. Its status would need an explicit maturity update in a later, separately authorized implementation; this proposal does not change its current Experimental attribute. The Stable component is standalone and does not inherit the public `PlayerSessionScopedAccessConsumer` surface, avoiding inherited `TryGetAccess`/`TryGetJoinAccess` APIs in its public closure.

The proposed Stable closure is therefore: `PlayerSessionJoinTrigger`, `LocalPlayerProvisioningConsumerScope` (Route/Activity values only), `LocalPlayerJoinRequest`, `LocalPlayerJoinOutcome`, `LocalPlayerJoinOutcomeCode`, `LocalPlayerJoinAdmissionState`, Unity `MonoBehaviour`, and Unity Input System `InputDevice`. The Framework already declares `com.unity.inputsystem` 1.19.0 as a package dependency; that upstream request-hint type is an explicit compatibility dependency, not an accidental transitive reference.

The outcome intentionally omits operation IDs: `RequestJoin` is synchronous and returns its result directly to the caller. Operation identity remains Experimental diagnostic evidence. If the operation becomes asynchronous or detached from the call stack, a separately reviewed correlation contract is required.

## 4. Outcome and status mapping

`Code` describes the consumer-level disposition. `OriginalCode` is populated only for `FailedRollback` and records the mapped cause that triggered recovery. For every ordinary rejection or failure, `AdmissionState` is `NotAdmitted` only when Session admission is known not to have committed; `Joined` maps to `Admitted`. Any `FailedRollback` maps conservatively to `Unknown` unless a future implementation proves the exact Session result. `Unknown` never means success.

| Current `LocalPlayerJoinStatus` | Proposed public result | Admission state / rule |
|---|---|---|
| `None` | `FailedUnknown` if an internal completed-result path unexpectedly returns it; never emitted as a completed public result | `Unknown`; `LastOutcome == null` only before invocation |
| `SucceededJoined` | `Joined` | `Admitted`; no implication for Actor or gameplay |
| `RejectedInvalidRequest` | `RejectedInvalidRequest` | `NotAdmitted` |
| `RejectedOperationInFlight` | `RejectedBusy` | `NotAdmitted` |
| `RejectedRuntimeUnavailable`, `RejectedManagerUnavailable` | `RejectedUnavailable` | `NotAdmitted` |
| `RejectedManagerConfiguration` | `RejectedConfiguration` | `NotAdmitted` |
| `RejectedJoiningClosed` | `RejectedJoiningClosed` | `NotAdmitted` |
| `RejectedNoAvailableSlot` | `RejectedNoAvailableSlot` | `NotAdmitted` |
| `RejectedDeviceAlreadyOwned` | `RejectedDeviceAlreadyOwned` | `NotAdmitted` |
| `RejectedForeignOrStaleReservation` | `RejectedSessionChanged` | `NotAdmitted` when recovery succeeds; `Unknown` if final code becomes `FailedRollback` |
| `RejectedProvisioningReturnedNull` | `FailedAdmission` | `NotAdmitted` only after confirmed Session rollback; otherwise `Unknown` / `FailedRollback` |
| `RejectedCorrelationMismatch`, `RejectedMissingPlayerInput`, `RejectedMissingLocalPlayerHost`, `RejectedInvalidLocalPlayerHost` | `FailedAdmission` | `NotAdmitted` only after confirmed Session rollback; otherwise `Unknown` / `FailedRollback` |
| `FailedAdmission` | `FailedAdmission` | `NotAdmitted` only after confirmed Session rollback; otherwise `Unknown` / `FailedRollback` |
| `FailedRollback` | `FailedRollback`, with `OriginalCode` equal to the mapped original status | `Unknown`; do not infer that the Session or physical resources were fully recovered |
| `RejectedUnexpectedJoin` | Not returned by this synchronous command: it represents an uncorrelated callback recorded as `LastUnexpectedJoinResult` | Remains Experimental diagnostic evidence; if an impossible adapter path returns it, map to `FailedUnknown`, never `Joined` |
| Any future or unrecognized status | `FailedUnknown` | `Unknown`; fail closed, never infer success |

The current `LocalPlayerJoinStatus` calls a null provisioner result a rejection. The proposal classifies it as `FailedAdmission` because the request reached the Manager backend and it failed to produce the requested Player. This is a deliberate semantic normalization for a new boundary; it does not rewrite or reclassify the Experimental result.

`FailedRollback` currently identifies failed Session reservation/Slot recovery; `ILocalPlayerProvisioningBackend.RejectPlayer` returns `void`, so the existing outcome cannot report whether physical destruction succeeded. The Stable contract will not expose `PlayerInput`, Host references, destruction flags, assignment, snapshots or raw `Message` text. It will not promise physical absence of orphans until Unity integrated QA observes the exact Manager-owned object being destroyed. If later QA requires a reportable physical-compensation failure, the backend/result seam must be designed in a separate review rather than inferred from this status.

## 5. Scoped discovery and Runtime adaptation

The stable component will use the same authored Route/Activity scope and discovery scopes as current Player Session consumers. The existing `PlayerSessionScopedAccessRuntimeHostModule` remains the only binder/owner:

```text
RouteContentDiscoveryScope / ActivityContentDiscoveryScope
  -> discover PlayerSessionJoinTrigger in exact owner scope
  -> validate explicit scope against actual Route or Activity
  -> bind private narrow join projection to exact RuntimeContentOwner
  -> RequestJoin -> existing ManagerPlayerSessionScopedAccess / provisioning host
  -> LocalPlayerProvisioningBridge.TryJoin(request)
  -> project LocalPlayerJoinResult to LocalPlayerJoinOutcome
  -> scope/owner change or destruction invalidates the binding
```

Implementation proposal: refactor only the module's **internal binding recipient** so both the current broad consumer and the new standalone trigger can share the exact owner validation and lifetime release logic. The new trigger receives only a private/internal join projection; it is not given `IPlayerSessionScopedAccess`. The projection checks that its captured Route/Activity `RuntimeContentOwner` is still current and that Manager provisioning is available, then delegates once to the existing Manager access/bridge. It introduces no public resolver, global lookup or alternative Join state. Binding absence, scope mismatch, Manager absence and stale scope return `RejectedUnavailable` (or `RejectedConfiguration` for an invalid authored scope) without a global or Scene-Provided fallback.

On successful Bridge Join, the existing Host preparation registration remains required. A failure of that registration continues through the existing committed-Join rollback path; the projection maps the final result and must never return `Joined` if registration or rollback produces a failure.

## 6. Migration and compatibility

Migration is additive:

1. Ship the new trigger/outcome API while retaining `PlayerSessionJoinCommandTrigger`, `ILocalPlayerJoinAccess`, `LocalPlayerJoinResult`, `LocalPlayerJoinStatus` and current authoring fields as Experimental and operational.
2. Existing scenes, scripts and serialized references continue to use their current types without forced migration.
3. Consumers that need only stable Join may adopt `PlayerSessionJoinTrigger.RequestJoin` or its UnityEvent entry points. Consumers that use Slot IDs, Unity player indices, Session observation, Host/`PlayerInput`, or richer failure text continue on Experimental surfaces until each has its own supported contract.
4. FirstGame's current tutorial also reads `IPlayerSessionScopedAccess` for Session updates and uses `Slot.PlayerSlotId`/`UnityPlayerIndex` for presentation. It therefore cannot migrate its entire tutorial to this boundary alone. A later optional migration can use the new trigger for Join while retaining its separate Experimental observation/display dependencies; no consumer edit is required to introduce the additive API.
5. Do not remove, redirect or silently change the old API during this ADR. Any deprecation/removal requires a later migration proposal and consumer review.

No FirstGame or QAFramework changes are part of this proposal.

## 7. Certification gates

Before the proposed API can be promoted or advertised as Stable:

- Add package-owned tests for every mapping row, null/sentinel/unrecognized handling, `OriginalCode`, all `AdmissionState` combinations, and the invariant that only `Joined` reports `Admitted`.
- Test direct trigger invocation without consumer code calling `TryGetAccess`/`IPlayerSessionScopedAccess`; test Route and Activity binding, mismatch, stale owner, Manager unavailability, disposal and destruction.
- Preserve current Join/rollback suites and verify Session reservation release, no Host staged/joined, pending state cleared, original failure retained, and retry behavior after recoverable failure.
- Replay the existing Slice 1 after the bridge correction on an immutable Framework SHA; retain raw output, timestamps and execution provenance.
- Add integrated Play Mode evidence that Manager-owned physical compensation destroys the exact operation-owned PlayerInput/Host on callback-then-throw and other covered failure paths. A fake backend request alone does not satisfy this gate.
- Decide whether a failed Session commit is reachable and add a deterministic package-owned seam/test if it is inside the public guarantee.
- Compile/import and run FirstGame against the same candidate SHA. Verify the existing Experimental consumer still works and, if a sample adopts the new component, verify the direct stable Join call, Session observation boundary, rejection and cleanup.
- Record public API/asmdef dependency on Input System and review the package minimum supported version, changelog and migration notes.

Prior reported evidence remains bounded: NUnit `229/229`, PlayerParticipation `38/38`, Join `26/26`, rollback `9/9` were reported by the user with partial provenance; Slice 1 `8/8` predates the bridge fix. They do not certify this proposed API, new binding path, physical destruction or FirstGame compatibility at a fixed SHA.

## 8. Consequences and risks

- The public Stable surface remains small and does not stabilize Session, Slot, assignment, Actor, Host, `PlayerInput` evidence or broad Session access.
- Input System `InputDevice` becomes an intentional upstream dependency of the Stable request API; the package must preserve supported compatibility with its declared Input System range.
- The new standalone trigger requires a narrowly scoped internal binding-recipient adaptation. The risk is duplicating the existing lifecycle binder; implementation must share exact scope ownership/disposal rules instead of creating another lifecycle owner.
- A typed `Unknown`/`FailedRollback` result can report that admission/recovery is uncertain; it cannot claim physical cleanup while the backend contract provides no observable destruction result.
- Additive migration avoids breaking current assets but creates a temporary choice between Experimental and proposed Stable authoring components. Inspector labeling and public guide must clearly identify the supported Stable component without implying that the entire multiplayer tutorial is Stable.
- Stable enum expansion and outcome semantics require compatibility review; unknown current/future internal statuses fail closed as `FailedUnknown`.

## 9. Relationship to existing decisions

This ADR proposes only the compatibility surface and its scoped projection. IF-ADR-003/016 continue to own Session configuration and provisioning-mode policy; IF-ADR-015 continues to separate commands from observation; IF-ADR-019/020 continue to own Player lifetime and explicit Leave. No accepted lifecycle semantics are replaced. IF-GOV-001 continues to define Stable compatibility expectations. The current Player recertification matrix owns mutable evidence and gates; this ADR does not duplicate that status board.

## 10. Approval state

Architecture direction B is approved for design by the user. The concrete scoped-binding shape, signatures, outcome mapping and compatibility effects in this document are **Proposed and awaiting review**. On approval, a separate implementation plan and authorization are required. No `FrameworkApiStatus` value or Runtime code changed while preparing this proposal.
