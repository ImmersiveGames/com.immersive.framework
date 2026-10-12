# IF-ADR-046 — Stable Manager-Provisioned Player Join Boundary

Status: **Accepted — implemented; bounded Manager-Provisioned Join closure promoted Stable**
Proposed: **2026-10-11**  
Accepted: **2026-10-11**
Type: Player Participation / public compatibility boundary / scoped authoring  
Related decisions: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md), IF-ADR-003, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020  
Current evidence and gates: [Player full recertification matrix (P6/P7)](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md)
Implementation route: [IF-PLAN-ADR-046 v3](../Plans/IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v3.md) — approved; [Amendment 001](IF-ADR-046-AMENDMENT-001-ONE-PUBLIC-JOIN-BOUNDARY.md) defines finite migration and retirement; [Amendment 002](IF-ADR-046-AMENDMENT-002-COMPOSITE-PHYSICAL-COMPENSATION-EVIDENCE.md) records accepted composite evidence; [Amendment 003](IF-ADR-046-AMENDMENT-003-PARTIAL-PROVENANCE-ACCEPTANCE.md) records the scoped provenance exception and Stable decision in accordance with IF-GOV-001

> The initial 2026-10-11 acceptance established the architecture and contract design, but did not itself authorize implementation or Stable promotion. The later [Stable-promotion decision](IF-ADR-046-STABLE-PROMOTION-DECISION.md) and accepted evidence amendments record the completed implementation and promote only the bounded closure listed there. IF-GOV-001 remains authoritative for maturity and compatibility policy.

## 1. Context

At proposal time, the Manager-Provisioned entry point was `ILocalPlayerJoinAccess.RequestJoin(LocalPlayerJoinRequest) -> LocalPlayerJoinResult`. Its result published reservation/commit/rollback records, Session/Slot snapshots, Actor selection evidence, Host and `PlayerInput` references, callback evidence and assignment projections, all on one Experimental result type. Stabilizing that type would have committed consumers to a substantially broader Session/Slot/assignment closure than the Join operation needs.

Consumers also need a scoped route to the capability. The existing `PlayerSessionScopedAccessRuntimeHostModule` discovers `PlayerSessionScopedAccessConsumer` components inside the current Route/Activity discovery scopes, validates their authored scope, binds an access object to a `RuntimeContentOwner`, and releases stale bindings. Its broad typed access remains Experimental and covers several separate operations. A consumer of Stable Join does not need that interface merely to invoke Join.

The authoritative operation remains in `LocalPlayerProvisioningBridge`, called by the Manager scoped Join port; `LocalPlayerProvisioningPreparationExtensions.RegisterJoinWithActorPreparation` registers Host preparation lifecycle evidence and rolls back if that internal registration fails. The public surface projects this operation and preserves the existing authority.

## 2. Accepted decision

The implemented contract is a narrow Stable authored command component with a typed synchronous outcome. It is bound through the existing Route/Activity lifecycle discovery and ownership checks, and receives only a private, narrow Join projection. `IPlayerSessionScopedAccess` and `LocalPlayerJoinResult` are not part of its public invocation signature.

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

## 3. Implemented contract and Stable closure

The declarations below record the accepted first-version contract shape, now implemented. The current Runtime attributes classify this bounded closure as Stable under the separate promotion decision. No adjacent Session, Slot, Actor, Leave, participation, readiness or broad scoped-access contract is promoted by this decision.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-046 scoped Manager-Provisioned Join command.")]
[DisallowMultipleComponent]
[AddComponentMenu("Immersive Framework/Player/Join (Stable)")]
public sealed class PlayerSessionJoinTrigger : MonoBehaviour
{
    [SerializeField]
    private LocalPlayerProvisioningConsumerScope scope =
        LocalPlayerProvisioningConsumerScope.Unspecified;

    [SerializeField, TextArea(2, 4)] private string reason;
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
    public bool HasDeviceHint { get; }
    public bool HasControlSchemeHint { get; }
    public bool IsValid { get; }
    public bool TryValidate(out string issue);
    public string ToDiagnosticString();
}

[FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-046 Route/Activity lifecycle binding scope.")]
public enum LocalPlayerProvisioningConsumerScope
{
    Unspecified = 0,
    Route = 10,
    Activity = 20
}

[FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-046 bounded Manager-Provisioned Join outcome.")]
public sealed class LocalPlayerJoinOutcome
{
    internal LocalPlayerJoinOutcome(
        LocalPlayerJoinOutcomeCode code,
        LocalPlayerJoinOutcomeCode? originalCode,
        LocalPlayerJoinAdmissionState admissionState);

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

`LocalPlayerProvisioningConsumerScope` is included in the Stable authoring closure as a whole enum because `FrameworkApiStatus` classifies the type, not individual values. The closure includes `Unspecified = 0`, `Route = 10` and `Activity = 20`; all names and numeric values are compatibility commitments. `Unspecified` remains invalid for effective binding, as do unrecognized serialized values; both fail closed as configuration rejection. Future values may only be appended without renumbering existing values and require an explicit compatibility/migration review. No value is removed or renumbered.

The `LocalPlayerJoinRequest` contract closure includes every current public member:

| Member | Compatibility guarantee in the accepted design |
|---|---|
| Constructor `(string source, string reason, InputDevice pairWithDevice = null, string controlScheme = null)` | Parameter order/types/defaults and synchronous construction remain compatible. Text inputs are normalized by the existing `NormalizeText` behavior; the supplied `InputDevice` reference is retained by identity and is not resolved/replaced by the boundary. |
| `Source`, `Reason`, `ControlScheme` | Names/types and normalized-value semantics are committed. |
| `PairWithDevice` | Name/type and exact reference semantics are committed. `InputDevice` is explicitly in the dependency closure. |
| `HasDeviceHint`, `HasControlSchemeHint` | Computed meaning is committed: non-null device reference; non-empty normalized control scheme. |
| `IsValid`, `TryValidate(out string issue)` | Validity rule is committed: non-empty normalized Source and Reason. The `issue` category/meaning is contractual, but localized/exact diagnostic wording is not. |
| `ToDiagnosticString()` | Remains diagnostic-only. Its exact text, formatting, and field order are not a Stable serialization or parsing contract and may evolve. |

The new public contract therefore does not delete, rename, or change the existing request members or their current behavior. Its adoption makes the listed typed semantics compatible; it does not stabilize free-form diagnostic text.

The Stable component is standalone and does not inherit the public `PlayerSessionScopedAccessConsumer` surface, avoiding inherited `TryGetAccess`/`TryGetJoinAccess` APIs in its public closure.

The promoted Stable closure is therefore: `PlayerSessionJoinTrigger`, the entire `LocalPlayerProvisioningConsumerScope` enum, `LocalPlayerJoinRequest` and every public member listed above, `LocalPlayerJoinOutcome`, `LocalPlayerJoinOutcomeCode`, `LocalPlayerJoinAdmissionState`, Unity `MonoBehaviour`, and Unity Input System `InputDevice`. The Framework already declares `com.unity.inputsystem` 1.19.0 as a package dependency; that upstream request-hint type is an explicit compatibility dependency, not an accidental transitive reference. The package minimum/support range must explicitly include the Unity Input System API version used by the public signature; changing that dependency floor later is a compatibility decision. Unity base types are part of the declared package Unity compatibility range, not framework-owned types.

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

The Experimental `LocalPlayerJoinStatus` calls a null provisioner result a rejection. The Stable outcome maps it to `FailedAdmission` because the request reached the Manager backend and it failed to produce the requested Player. This deliberate semantic normalization does not rewrite or reclassify the Experimental result.

`FailedRollback` identifies failed Session reservation/Slot recovery; `ILocalPlayerProvisioningBackend.RejectPlayer` returns `void`, so the outcome cannot report whether physical destruction succeeded. The Stable contract does not expose `PlayerInput`, Host references, destruction flags, assignment, snapshots or raw `Message` text. It does not promise physical absence of orphans; physical compensation was accepted by composite evidence with residual risks recorded in Amendment 002. A reportable physical-compensation failure would require a separate contract review.

## 5. Scoped discovery and Runtime adaptation

The Stable component uses the same authored Route/Activity scope and discovery scopes as current Player Session consumers. The existing `PlayerSessionScopedAccessRuntimeHostModule` remains the only binder/owner:

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

The module's **internal binding recipient** shares owner validation and lifetime release logic between broad scoped consumers and the standalone trigger. The trigger receives only a private/internal Join projection; it is not given `IPlayerSessionScopedAccess`. It introduces no public resolver, global lookup or alternative Join state.

`RuntimeContentOwner` alone is insufficient as an occurrence lease: its `RuntimeDefinitionToken` is derived from the Route/Activity asset, so re-entering the same asset can reproduce the same owner value. The current code confirms a Route occurrence sequence (`RouteLifecycleRuntime._routeOccurrenceSequence` / `RoutePlayerSpatialEntryContext.OccurrenceSequence`) and an Activity occurrence sequence (`ActivityReadinessOccurrence.TransitionSequence`). The private binding lease MUST include and compare the exact current occurrence identity in addition to `RuntimeContentOwner`: Route scope uses the exact active Route occurrence sequence; Activity scope uses the exact active Route occurrence and Activity transition sequence. Validate against the current lifecycle runtime on every invocation. A return to the same definition in a later occurrence must invalidate the former lease. This is local lifecycle state already owned by Route/Activity, not a new global generation or public identity.

Binding absence, released binding, stale occurrence, disposed lifecycle/runtime module, or unavailable Manager return `RejectedUnavailable`. `Unspecified`/unknown scope values and a mismatch between authored scope and actual discovered Route/Activity scope return `RejectedConfiguration`. None throw as ordinary availability handling; none fall back globally or to Scene-Provided Join. The port revalidates exact owner and occurrence, live runtime/Manager readiness, and captured recipient identity immediately before delegation. Module/consumer disposal releases the same lease through the existing owner module.

On successful Bridge Join, the existing Host preparation registration remains required. A failure of that registration continues through the existing committed-Join rollback path; the projection maps the final result and must never return `Joined` if registration or rollback produces a failure. `Joined` is assigned only after this full canonical call returns its successful final result.

### Binding and invocation result classification

| Condition | Result code | Admission state |
|---|---|---|
| No binding before first request; binding released; stale Route/Activity occurrence; participation/runtime host or module disposed; Manager missing/not ready | `RejectedUnavailable` | `NotAdmitted` |
| `Unspecified` or unrecognized authored scope; authored Route/Activity scope mismatches discovered owner; malformed request or explicit-device entry point given null/not-currently-added device | `RejectedConfiguration` for scope/configuration; `RejectedInvalidRequest` for request/device | `NotAdmitted` |
| Unexpected managed exception escapes the private projection/canonical call | `FailedUnknown` | `Unknown`; preserve details only in internal diagnostics |

The bridge's own mapped `FailedRollback` takes precedence over the generic unexpected-exception mapping when returned as a normal result, preserving `OriginalCode`.

### Totality and LastOutcome

Every invocation path (`RequestJoin`, `Invoke`, `InvokeFromDevice`) that returns normally MUST return a non-null outcome and assign that same outcome to `LastOutcome`. The adapter converts an unexpected exception escaping its private projection to `FailedUnknown/Unknown`, records internal diagnostics, and stores the outcome before returning; exception-driven unavailability is not the normal availability contract. Thus `LastOutcome` is null only before the first invocation that completes. Destruction during an in-flight synchronous call does not authorize another binding: the captured occurrence/recipient lease is checked immediately before delegation, and the completed call stores its outcome on the managed component instance. No guarantee is made that a destroyed Unity native object remains externally observable through Unity APIs.

## 6. Migration and compatibility

The finite migration and retirement policy in Amendment 001 supersedes the
original additive-migration proposal:

1. `PlayerSessionJoinTrigger` is the sole public Manager-Provisioned Join
   invocation boundary after convergence.
2. The old Join-only command/access entry points and their public
   `LocalPlayerJoinResult` exposure are retired after consumer migration. Their
   package-owned implementation evidence may remain internal where still used.
3. `IPlayerSessionScopedAccess`, Session observation, Slot/Actor evidence,
   Leave, Joining policy, Actor Selection/replacement and Gameplay Availability
   remain Experimental contracts with separate responsibilities; they are not
   removed merely because they share implementation types with Join.
4. Consumer scenes, scripts, prefabs, UnityEvents and script references were
   within the migration gate defined by Amendment 001. This paragraph does not
   claim that consumers outside the inspected QAFramework/FirstGame snapshots
   have been migrated.

## 7. Certification and promotion disposition

The implementation, certification claims, evidence provenance limits and
Stable scope are recorded in the [Stable-promotion decision](IF-ADR-046-STABLE-PROMOTION-DECISION.md)
and accepted [Amendments 001](IF-ADR-046-AMENDMENT-001-ONE-PUBLIC-JOIN-BOUNDARY.md),
[002](IF-ADR-046-AMENDMENT-002-COMPOSITE-PHYSICAL-COMPENSATION-EVIDENCE.md) and
[003](IF-ADR-046-AMENDMENT-003-PARTIAL-PROVENANCE-ACCEPTANCE.md). Those records
supersede this ADR's original pre-implementation gate wording. In particular,
physical compensation was accepted by composed evidence, not reported as an
integrated Play Mode PASS, and partial execution provenance was explicitly
accepted for this closure only. No new evidence or broader maturity claim is
implied here.

## 8. Consequences and risks

- The public Stable surface remains small and does not stabilize Session, Slot, assignment, Actor, Host, `PlayerInput` evidence or broad Session access.
- Input System `InputDevice` is an intentional upstream dependency of the Stable request API; the package must preserve supported compatibility with its declared Input System range.
- The standalone trigger uses the existing lifecycle binder through a narrowly scoped internal binding recipient. Its scope ownership/disposal rules remain shared with the existing binder; no second lifecycle owner is introduced.
- A typed `Unknown`/`FailedRollback` result can report that admission/recovery is uncertain; it cannot claim physical cleanup while the backend contract provides no observable destruction result.
- The finite migration avoids a permanent choice between multiple public Manager-Provisioned Join boundaries. Inspector labeling and public guidance must identify the supported Stable component without implying that the entire multiplayer tutorial is Stable.
- Stable enum expansion and outcome semantics require compatibility review; unknown current/future internal statuses fail closed as `FailedUnknown`.

## 9. Relationship to existing decisions

This ADR defines only the compatibility surface and its scoped projection. IF-ADR-003/016 continue to own Session configuration and provisioning-mode policy; IF-ADR-015 continues to separate commands from observation; IF-ADR-019/020 continue to own Player lifetime and explicit Leave. No accepted lifecycle semantics are replaced. IF-GOV-001 continues to define Stable compatibility expectations. The Player recertification matrix owns mutable evidence and gates; this ADR does not duplicate that status board.

## 10. Approval state

The user accepted architecture direction B, its bounded signatures, full mapping, scoped lease model and migration policy on 2026-10-11. That initial acceptance did not itself authorize implementation, certification, `FrameworkApiStatus` changes, consumer migration, release or Stable promotion. The subsequent implementation and bounded Stable promotion are recorded in the linked decision/amendments and current Runtime attributes. This history remains scoped to `PlayerSessionJoinTrigger` and its declared closure.

**Amendment 001 accepted:** the finite convergence and retirement policy in [IF-ADR-046 Amendment 001](IF-ADR-046-AMENDMENT-001-ONE-PUBLIC-JOIN-BOUNDARY.md) supplements the original migration decision. The approved execution route was v3; the current bounded Stable disposition and evidence are recorded in the linked promotion decision and amendments.

**Amendment 002 accepted:** [IF-ADR-046 Amendment 002](IF-ADR-046-AMENDMENT-002-COMPOSITE-PHYSICAL-COMPENSATION-EVIDENCE.md) supplements §7 by accepting composed evidence for the physical-compensation gate and recording its residual risks. It does not report an integrated Play Mode PASS.

**Amendment 003 accepted:** [IF-ADR-046 Amendment 003](IF-ADR-046-AMENDMENT-003-PARTIAL-PROVENANCE-ACCEPTANCE.md) records the bounded provenance exception and the separate Stable-promotion decision. Both decisions apply only to the closure enumerated in that record.
