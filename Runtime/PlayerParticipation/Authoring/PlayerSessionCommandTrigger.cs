using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerSlots;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// The supported, explicitly invoked Player provisioning operations that a
    /// scene or prefab consumer may request.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-05 designer command selection for Player provisioning.")]
    public enum PlayerProvisioningCommandOperation
    {
        OpenJoining = 10,
        CloseJoining = 20,
        // 30 is intentionally retired. Do not reuse it: serialized Unity content may still contain this value.
        RequestJoin = 40,
        RequestDefaultActorSelection = 50,
        RequestLeave = 60
    }

    /// <summary>
    /// Identifies which existing typed result contract was returned by the
    /// last explicit trigger invocation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-05 typed result kind for authored Player commands.")]
    public enum PlayerProvisioningCommandResultKind
    {
        None = 0,
        ParticipationOperation = 10,
        LocalPlayerJoin = 20,
        ActorSelection = 30,
        SessionPlayerLeave = 40
    }

    /// <summary>
    /// Scene/prefab authoring surface that translates an explicit UnityEvent
    /// invocation into one supported Player command. It owns neither Player
    /// state nor runtime authority.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Player Session Command Trigger")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-05 explicit designer command trigger over public Player surfaces.")]
    public sealed class PlayerSessionCommandTrigger : MonoBehaviour
    {
        private const string Source = nameof(PlayerSessionCommandTrigger);

        [Header("Command")]
        [SerializeField]
        private PlayerProvisioningCommandOperation operation =
            PlayerProvisioningCommandOperation.OpenJoining;

        [SerializeField]
        [Tooltip("Explicit Route or Activity scoped access binding supplied by Framework Core.")]
        private LocalPlayerProvisioningConsumerAccessBinding consumerAccessBinding;

        [Header("Request Join")]
        [SerializeField]
        [Tooltip("Optional Unity Input System control scheme hint for this manual Join request.")]
        private string controlScheme;

        [Header("Request Default Actor Selection")]
        [SerializeField]
        [Tooltip("Existing public Actor-selection command surface. This remains separate from provisioning authority.")]
        private LocalPlayerActorSelectionRequestAuthoring defaultActorSelectionRequest;

        [SerializeField]
        [Tooltip("Slot whose configured default Actor will be selected. This does not select an arbitrary Actor.")]
        private PlayerSlotProfile selectedPlayerSlot;

        [SerializeField]
        [Tooltip("Expected selection revision, or -1 when no optimistic revision check is required.")]
        private int expectedSelectionRevision =
            PlayerActorSelectionRequest.NoExpectedRevision;

        [Header("Request Leave")]
        [SerializeField]
        [Tooltip("Exact Player Slot whose current joined Session occurrence will Leave. A target is always required, including single-player products.")]
        private PlayerSlotProfile leavePlayerSlot;

        [SerializeField]
        [Tooltip("Advanced/debug override for the exact joined occurrence revision. Use -1 to resolve the current occurrence from the scoped observation.")]
        private int expectedLeaveOccurrenceRevision = -1;

        [Header("Request Metadata")]
        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional diagnostic reason. An operation-specific reason is used when left empty.")]
        private string reason;

        [NonSerialized]
        private PlayerParticipationOperationResult _lastParticipationResult;

        [NonSerialized]
        private LocalPlayerJoinResult _lastJoinResult;

        [NonSerialized]
        private PlayerActorSelectionResult _lastActorSelectionResult;

        [NonSerialized]
        private SessionPlayerLeaveResult _lastLeaveResult;

        [NonSerialized]
        private SessionPlayerLeaveRequest _lastLeaveRequest;

        [NonSerialized]
        private PlayerProvisioningCommandResultKind _lastResultKind;

        [NonSerialized]
        private string _lastDiagnostic =
            "No Player provisioning command has been invoked.";

        [NonSerialized]
        private int _invocationCount;

        public PlayerProvisioningCommandOperation Operation => operation;
        public LocalPlayerProvisioningConsumerAccessBinding ConsumerAccessBinding =>
            consumerAccessBinding;
        public string ControlScheme => controlScheme ?? string.Empty;
        public LocalPlayerActorSelectionRequestAuthoring DefaultActorSelectionRequest =>
            defaultActorSelectionRequest;
        public PlayerSlotProfile SelectedPlayerSlot => selectedPlayerSlot;
        public int ExpectedSelectionRevision => expectedSelectionRevision;
        public PlayerSlotProfile LeavePlayerSlot => leavePlayerSlot;
        public int ExpectedLeaveOccurrenceRevision => expectedLeaveOccurrenceRevision;
        public SessionPlayerLeaveRequest LastLeaveRequest => _lastLeaveRequest;
        public string Reason => reason ?? string.Empty;
        public int InvocationCount => _invocationCount;
        public PlayerProvisioningCommandResultKind LastResultKind => _lastResultKind;
        public PlayerParticipationOperationResult LastParticipationResult =>
            _lastParticipationResult;
        public LocalPlayerJoinResult LastJoinResult => _lastJoinResult;
        public PlayerActorSelectionResult LastActorSelectionResult =>
            _lastActorSelectionResult;
        public SessionPlayerLeaveResult LastLeaveResult => _lastLeaveResult;
        public bool HasLastTypedResult => _lastResultKind !=
            PlayerProvisioningCommandResultKind.None;
        public string LastDiagnostic => _lastDiagnostic;
        public string LastResultSummary => BuildLastResultSummary();
        public bool IsScopedAccessAvailable => consumerAccessBinding != null &&
            consumerAccessBinding.IsBound;
        public string ScopeBindingStatus => consumerAccessBinding == null
            ? "Missing"
            : consumerAccessBinding.IsBound ? "Bound" : "Unavailable";
        public string ScopeBindingDiagnostic => consumerAccessBinding == null
            ? "Player Session Command Trigger requires an explicit Local Player Provisioning Consumer Access binding."
            : consumerAccessBinding.Diagnostic;

        /// <summary>
        /// UnityEvent entry point. Commands run only when this method is
        /// explicitly invoked by a consumer.
        /// </summary>
        [ContextMenu("Invoke Configured Player Provisioning Command")]
        public void InvokeConfiguredOperation()
        {
            _invocationCount++;
            ClearLastResult();
            string resolvedReason = ResolveReason();
            LogCommandRequested(resolvedReason);

            switch (operation)
            {
                case PlayerProvisioningCommandOperation.OpenJoining:
                    InvokeParticipationOperation(
                        "OpenJoining",
                        resolvedReason,
                        access => access.OpenJoining(Source, resolvedReason));
                    return;

                case PlayerProvisioningCommandOperation.CloseJoining:
                    InvokeParticipationOperation(
                        "CloseJoining",
                        resolvedReason,
                        access => access.CloseJoining(Source, resolvedReason));
                    return;

                case PlayerProvisioningCommandOperation.RequestJoin:
                    InvokeJoin(resolvedReason);
                    return;

                case PlayerProvisioningCommandOperation.RequestDefaultActorSelection:
                    InvokeDefaultActorSelection(resolvedReason);
                    return;

                case PlayerProvisioningCommandOperation.RequestLeave:
                    InvokeLeave(resolvedReason);
                    return;

                default:
                    _lastDiagnostic =
                        $"Player Session Command Trigger operation '{operation}' is not supported.";
                    LogCommandRejected(_lastDiagnostic);
                    return;
            }
        }

        /// <summary>
        /// Validates authored references and operation-specific inputs only.
        /// It never resolves or changes runtime authority.
        /// </summary>
        public bool TryValidateConfiguration(out string issue)
        {
            if (!IsDefinedOperation(operation))
            {
                issue =
                    $"Player Session Command Trigger has unsupported operation '{operation}'.";
                return false;
            }

            if (consumerAccessBinding == null)
            {
                issue =
                    "Player Session Command Trigger requires an explicit Local Player Provisioning Consumer Access binding.";
                return false;
            }

            LocalPlayerProvisioningConsumerScope scope =
                consumerAccessBinding.Scope;
            if (scope != LocalPlayerProvisioningConsumerScope.Route &&
                scope != LocalPlayerProvisioningConsumerScope.Activity)
            {
                issue =
                    "Player Session Command Trigger binding requires an explicit Route or Activity scope.";
                return false;
            }

            if (operation == PlayerProvisioningCommandOperation.RequestLeave)
            {
                if (leavePlayerSlot == null)
                {
                    issue =
                        "Request Leave requires an explicit Player Slot Profile target, including single-player products.";
                    return false;
                }

                if (expectedLeaveOccurrenceRevision < -1)
                {
                    issue =
                        "Expected Leave Occurrence Revision must be -1 or a non-negative revision.";
                    return false;
                }

                return leavePlayerSlot.TryGetPlayerSlotId(out _, out issue);
            }

            if (operation != PlayerProvisioningCommandOperation
                    .RequestDefaultActorSelection)
            {
                issue = string.Empty;
                return true;
            }

            if (defaultActorSelectionRequest == null)
            {
                issue =
                    "Request Default Actor Selection requires an explicit Local Player Actor Selection Requests component.";
                return false;
            }

            if (!defaultActorSelectionRequest.TryValidateConfiguration(
                    out issue))
            {
                return false;
            }

            if (selectedPlayerSlot == null)
            {
                issue =
                    "Request Default Actor Selection requires a Player Slot Profile. It never accepts a raw Slot identity string.";
                return false;
            }

            if (expectedSelectionRevision <
                PlayerActorSelectionRequest.NoExpectedRevision)
            {
                issue =
                    "Expected Selection Revision must be -1 or a non-negative revision.";
                return false;
            }

            return selectedPlayerSlot.TryGetPlayerSlotId(out _, out issue);
        }

        private void InvokeParticipationOperation(
            string operationName,
            string resolvedReason,
            Func<ILocalPlayerProvisioningConsumerAccess,
                PlayerParticipationOperationResult> request)
        {
            if (!TryGetScopedAccess(out ILocalPlayerProvisioningConsumerAccess access,
                    out string issue))
            {
                Complete(PlayerParticipationOperationResult.RuntimeUnavailable(
                    operationName,
                    Source,
                    resolvedReason,
                    issue));
                return;
            }

            Complete(request(access));
        }

        private void InvokeJoin(string resolvedReason)
        {
            LocalPlayerJoinRequest request = new LocalPlayerJoinRequest(
                Source,
                resolvedReason,
                null,
                controlScheme);
            if (!TryGetScopedAccess(out ILocalPlayerProvisioningConsumerAccess access,
                    out string issue))
            {
                Complete(LocalPlayerJoinResult.RuntimeUnavailable(request, issue));
                return;
            }

            Complete(access.RequestJoin(request));
        }

        private void InvokeDefaultActorSelection(string resolvedReason)
        {
            PlayerSlotId playerSlotId = default;
            if (selectedPlayerSlot != null)
            {
                selectedPlayerSlot.TryGetPlayerSlotId(out playerSlotId, out _);
            }

            var request = new PlayerActorSelectionRequest(
                playerSlotId,
                null,
                Source,
                resolvedReason,
                expectedSelectionRevision);
            if (!TryGetScopedAccess(out ILocalPlayerProvisioningConsumerAccess unusedAccess,
                    out string scopeIssue))
            {
                Complete(PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectDefaultActor",
                    request,
                    scopeIssue));
                return;
            }

            if (defaultActorSelectionRequest == null)
            {
                _lastDiagnostic =
                    "Request Default Actor Selection was not submitted because its explicit Local Player Actor Selection Requests component is missing.";
                LogCommandRejected(_lastDiagnostic);
                return;
            }

            Complete(defaultActorSelectionRequest.RequestDefaultActorSelection(
                playerSlotId,
                expectedSelectionRevision,
                Source,
                resolvedReason));
        }

        private void InvokeLeave(string resolvedReason)
        {
            PlayerSlotId playerSlotId = default;
            if (leavePlayerSlot != null)
            {
                leavePlayerSlot.TryGetPlayerSlotId(out playerSlotId, out _);
            }

            if (_lastLeaveRequest.IsValid &&
                _lastLeaveRequest.PlayerSlotId != playerSlotId)
            {
                _lastLeaveRequest = default;
            }

            if (!TryGetScopedAccess(out ILocalPlayerProvisioningConsumerAccess access,
                    out string scopeIssue))
            {
                Complete(SessionPlayerLeaveResult.RuntimeUnavailable(
                    default,
                    scopeIssue));
                return;
            }

            if (!access.TryGetObservation(
                    out LocalPlayerProvisioningConsumerObservationSnapshot observation) ||
                observation == null || !observation.IsAvailable)
            {
                const string observationIssue =
                    "Request Leave could not read the current scoped Player observation required to correlate the target occurrence.";
                Complete(SessionPlayerLeaveResult.RuntimeUnavailable(
                    default,
                    observationIssue));
                return;
            }

            LocalPlayerProvisioningConsumerSlotObservation target = default;
            bool found = false;
            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation candidate =
                    observation.Slots[index];
                if (candidate.Slot.PlayerSlotId != playerSlotId)
                {
                    continue;
                }

                target = candidate;
                found = true;
                break;
            }

            int occurrenceRevision = expectedLeaveOccurrenceRevision >= 0
                ? expectedLeaveOccurrenceRevision
                : found ? target.Slot.Revision : 0;
            if (expectedLeaveOccurrenceRevision < 0 &&
                found &&
                target.Slot.AllocationState == PlayerSlotAllocationState.Leaving &&
                _lastLeaveRequest.IsValid &&
                _lastLeaveRequest.PlayerSlotId == playerSlotId)
            {
                occurrenceRevision = _lastLeaveRequest.ExpectedOccurrenceRevision;
            }

            var request = new SessionPlayerLeaveRequest(
                playerSlotId,
                occurrenceRevision,
                Source,
                resolvedReason);
            Complete(access.RequestLeave(request));
        }

        private bool TryGetScopedAccess(
            out ILocalPlayerProvisioningConsumerAccess access,
            out string issue)
        {
            access = null;
            if (consumerAccessBinding == null)
            {
                issue =
                    "Player Session Command Trigger requires an explicit Local Player Provisioning Consumer Access binding.";
                return false;
            }

            return consumerAccessBinding.TryGetAccess(out access, out issue);
        }

        private void Complete(PlayerParticipationOperationResult result)
        {
            _lastParticipationResult = result;
            _lastResultKind =
                PlayerProvisioningCommandResultKind.ParticipationOperation;
            _lastDiagnostic = result != null
                ? result.ToDiagnosticString()
                : "Player participation command returned no typed result.";
            LogCommandCompleted();
        }

        private void Complete(LocalPlayerJoinResult result)
        {
            _lastJoinResult = result;
            _lastResultKind = PlayerProvisioningCommandResultKind.LocalPlayerJoin;
            _lastDiagnostic = result != null
                ? result.ToDiagnosticString()
                : "Local Player Join returned no typed result.";
            LogCommandCompleted();
        }

        private void Complete(PlayerActorSelectionResult result)
        {
            _lastActorSelectionResult = result;
            _lastResultKind = PlayerProvisioningCommandResultKind.ActorSelection;
            _lastDiagnostic = result != null
                ? result.ToDiagnosticString()
                : "Default Actor selection returned no typed result.";
            LogCommandCompleted();
        }

        private void Complete(SessionPlayerLeaveResult result)
        {
            _lastLeaveResult = result;
            if (result != null && result.Request.IsValid)
            {
                _lastLeaveRequest = result.Request;
            }

            _lastResultKind = PlayerProvisioningCommandResultKind.SessionPlayerLeave;
            _lastDiagnostic = result != null
                ? result.ToDiagnosticString()
                : "Session Player Leave returned no typed result.";
            LogCommandCompleted();
        }

        private void ClearLastResult()
        {
            _lastParticipationResult = null;
            _lastJoinResult = null;
            _lastActorSelectionResult = null;
            _lastLeaveResult = null;
            _lastResultKind = PlayerProvisioningCommandResultKind.None;
        }

        private string ResolveReason()
        {
            return !string.IsNullOrWhiteSpace(reason)
                ? reason.Trim()
                : operation.ToString();
        }

        private string BuildLastResultSummary()
        {
            return HasLastTypedResult
                ? _lastDiagnostic
                : string.IsNullOrWhiteSpace(_lastDiagnostic)
                    ? "No Player provisioning command result is available."
                    : _lastDiagnostic;
        }

        private void LogCommandRequested(string resolvedReason)
        {
            FrameworkLogger.Create(typeof(PlayerSessionCommandTrigger))
                .Info(
                    "Player provisioning command requested.",
                    BuildCommandFields("Requested", resolvedReason));
        }

        private void LogCommandRejected(string issue)
        {
            FrameworkLogger.Create(typeof(PlayerSessionCommandTrigger))
                .Warning(
                    "Player provisioning command rejected before submission.",
                    BuildCommandFields("Rejected", issue));
        }

        private void LogCommandCompleted()
        {
            string resultStatus = GetLastResultStatus();
            string outcome = GetLastResultOutcome();
            LogField[] fields = LogFields.Of(
                LogFields.Field("component", name),
                LogFields.Field("scene", gameObject.scene.name),
                LogFields.Field("operation", operation),
                LogFields.Field("invocation", _invocationCount),
                LogFields.Field("scope", GetConfiguredScopeLabel()),
                LogFields.Field("bindingStatus", ScopeBindingStatus),
                LogFields.Field("resultKind", _lastResultKind),
                LogFields.Field("resultStatus", resultStatus),
                LogFields.Field("outcome", outcome),
                LogFields.Field("playerSlot", GetLastResultPlayerSlot()),
                LogFields.Field("selectedActor", GetLastResultSelectedActor()),
                LogFields.Field("localPlayerHost", GetLastResultLocalPlayerHost()),
                LogFields.Field("unityPlayerIndex", GetLastResultUnityPlayerIndex()),
                LogFields.Field("message", _lastDiagnostic ?? string.Empty));

            FrameworkLogger logger =
                FrameworkLogger.Create(typeof(PlayerSessionCommandTrigger));
            if (string.Equals(outcome, "Succeeded", StringComparison.Ordinal) ||
                string.Equals(outcome, "IgnoredNoChange", StringComparison.Ordinal))
            {
                logger.Info("Player provisioning command completed.", fields);
                return;
            }

            logger.Warning("Player provisioning command completed without success.", fields);
        }

        private string GetLastResultStatus()
        {
            return _lastResultKind switch
            {
                PlayerProvisioningCommandResultKind.ParticipationOperation =>
                    _lastParticipationResult != null
                        ? _lastParticipationResult.Status.ToString()
                        : "Missing",
                PlayerProvisioningCommandResultKind.LocalPlayerJoin =>
                    _lastJoinResult != null
                        ? _lastJoinResult.Status.ToString()
                        : "Missing",
                PlayerProvisioningCommandResultKind.ActorSelection =>
                    _lastActorSelectionResult != null
                        ? _lastActorSelectionResult.Status.ToString()
                        : "Missing",
                PlayerProvisioningCommandResultKind.SessionPlayerLeave =>
                    _lastLeaveResult != null
                        ? _lastLeaveResult.Status.ToString()
                        : "Missing",
                _ => "None"
            };
        }

        private string GetLastResultOutcome()
        {
            switch (_lastResultKind)
            {
                case PlayerProvisioningCommandResultKind.ParticipationOperation:
                    if (_lastParticipationResult == null) return "Missing";
                    if (_lastParticipationResult.Succeeded) return "Succeeded";
                    if (_lastParticipationResult.IgnoredNoChange) return "IgnoredNoChange";
                    if (_lastParticipationResult.Failed) return "Failed";
                    if (_lastParticipationResult.Rejected) return "Rejected";
                    return "Incomplete";

                case PlayerProvisioningCommandResultKind.LocalPlayerJoin:
                    if (_lastJoinResult == null) return "Missing";
                    if (_lastJoinResult.Succeeded) return "Succeeded";
                    if (_lastJoinResult.Failed) return "Failed";
                    if (_lastJoinResult.Rejected) return "Rejected";
                    return "Incomplete";

                case PlayerProvisioningCommandResultKind.ActorSelection:
                    if (_lastActorSelectionResult == null) return "Missing";
                    return _lastActorSelectionResult.Succeeded
                        ? "Succeeded"
                        : _lastActorSelectionResult.Rejected
                            ? "Rejected"
                            : "Incomplete";

                case PlayerProvisioningCommandResultKind.SessionPlayerLeave:
                    if (_lastLeaveResult == null) return "Missing";
                    if (_lastLeaveResult.Succeeded) return "Succeeded";
                    if (_lastLeaveResult.Failed) return "Failed";
                    if (_lastLeaveResult.Rejected) return "Rejected";
                    return "Incomplete";

                default:
                    return "None";
            }
        }

        private string GetLastResultPlayerSlot()
        {
            if (_lastParticipationResult != null &&
                _lastParticipationResult.Slot.PlayerSlotId.IsValid)
            {
                return _lastParticipationResult.Slot.PlayerSlotId.StableText;
            }

            if (_lastJoinResult != null && _lastJoinResult.Slot.PlayerSlotId.IsValid)
            {
                return _lastJoinResult.Slot.PlayerSlotId.StableText;
            }

            if (_lastActorSelectionResult != null &&
                _lastActorSelectionResult.PlayerSlotId.IsValid)
            {
                return _lastActorSelectionResult.PlayerSlotId.StableText;
            }

            if (_lastLeaveResult != null)
            {
                if (_lastLeaveResult.Slot.PlayerSlotId.IsValid)
                {
                    return _lastLeaveResult.Slot.PlayerSlotId.StableText;
                }

                if (_lastLeaveResult.Request.PlayerSlotId.IsValid)
                {
                    return _lastLeaveResult.Request.PlayerSlotId.StableText;
                }
            }

            return string.Empty;
        }

        private string GetLastResultSelectedActor()
        {
            return _lastActorSelectionResult != null &&
                _lastActorSelectionResult.SelectedActorProfileId.IsValid
                    ? _lastActorSelectionResult.SelectedActorProfileId.StableText
                    : string.Empty;
        }

        private string GetLastResultLocalPlayerHost()
        {
            return _lastJoinResult != null && _lastJoinResult.LocalPlayerHost != null
                ? _lastJoinResult.LocalPlayerHost.name
                : string.Empty;
        }

        private int GetLastResultUnityPlayerIndex()
        {
            return _lastJoinResult != null ? _lastJoinResult.UnityPlayerIndex : -1;
        }

        private LogField[] BuildCommandFields(string status, string message)
        {
            return LogFields.Of(
                LogFields.Field("component", name),
                LogFields.Field("scene", gameObject.scene.name),
                LogFields.Field("status", status),
                LogFields.Field("operation", operation),
                LogFields.Field("invocation", _invocationCount),
                LogFields.Field("scope", GetConfiguredScopeLabel()),
                LogFields.Field("bindingStatus", ScopeBindingStatus),
                LogFields.Field("controlScheme", controlScheme ?? string.Empty),
                LogFields.Field("selectedPlayerSlot", selectedPlayerSlot != null ? selectedPlayerSlot.name : string.Empty),
                LogFields.Field("selectedPlayerSlotId", GetPlayerSlotId(selectedPlayerSlot)),
                LogFields.Field("leavePlayerSlot", leavePlayerSlot != null ? leavePlayerSlot.name : string.Empty),
                LogFields.Field("leavePlayerSlotId", GetPlayerSlotId(leavePlayerSlot)),
                LogFields.Field("expectedSelectionRevision", expectedSelectionRevision),
                LogFields.Field("expectedLeaveOccurrenceRevision", expectedLeaveOccurrenceRevision),
                LogFields.Field("message", message ?? string.Empty));
        }

        private static string GetPlayerSlotId(PlayerSlotProfile profile)
        {
            return profile != null &&
                profile.TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out _) &&
                playerSlotId.IsValid
                    ? playerSlotId.StableText
                    : string.Empty;
        }

        private string GetConfiguredScopeLabel()
        {
            return consumerAccessBinding != null
                ? consumerAccessBinding.Scope.ToString()
                : "Missing";
        }

        private static bool IsDefinedOperation(
            PlayerProvisioningCommandOperation value)
        {
            return value == PlayerProvisioningCommandOperation.OpenJoining ||
                value == PlayerProvisioningCommandOperation.CloseJoining ||
                value == PlayerProvisioningCommandOperation.RequestJoin ||
                value == PlayerProvisioningCommandOperation.RequestDefaultActorSelection ||
                value == PlayerProvisioningCommandOperation.RequestLeave;
        }
    }
}
