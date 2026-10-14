using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Identity;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// FrameworkRuntimeHost-scoped owner for provider-neutral Player Session
    /// consumer bindings. It observes existing Session authority only; Manager
    /// provisioning and Scene admission remain independent contributors.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PlayerSessionScopedAccessRuntimeHostModule : MonoBehaviour
    {
        private readonly Dictionary<Component, ActiveScopedBinding> _accesses = new();
        private readonly HashSet<string> _inputOwnershipDiagnosticKeys = new();
        private FrameworkRuntimeHost _runtimeHost;
        private PlayerParticipationRuntimeContext _participationContext;

        internal static bool TryAttach(
            FrameworkRuntimeHost runtimeHost,
            out PlayerSessionScopedAccessRuntimeHostModule module,
            out string issue)
        {
            module = null;
            issue = string.Empty;
            if (runtimeHost == null)
            {
                issue = "Player Session scoped access requires an explicit FrameworkRuntimeHost.";
                return false;
            }

            if (!runtimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext participationContext))
            {
                issue = "Player Session scoped access requires an initialized participation context.";
                return false;
            }

            module = runtimeHost.GetComponent<PlayerSessionScopedAccessRuntimeHostModule>();
            if (module == null)
            {
                module = runtimeHost.gameObject.AddComponent<PlayerSessionScopedAccessRuntimeHostModule>();
            }

            return module.TryInitialize(runtimeHost, participationContext, out issue);
        }

        private bool TryInitialize(
            FrameworkRuntimeHost runtimeHost,
            PlayerParticipationRuntimeContext participationContext,
            out string issue)
        {
            issue = string.Empty;
            if (_runtimeHost != null)
            {
                if (ReferenceEquals(_runtimeHost, runtimeHost) &&
                    ReferenceEquals(_participationContext, participationContext))
                {
                    return true;
                }

                issue = "Player Session scoped access runtime is already bound to another Session authority.";
                return false;
            }

            _runtimeHost = runtimeHost;
            _participationContext = participationContext;
            return true;
        }

        private void Update()
        {
            if (_runtimeHost != null && _participationContext != null)
            {
                RefreshBindings();
            }
        }

        private void RefreshBindings()
        {
            var desired = new Dictionary<Component, DesiredScopedBinding>();
            var flow = _runtimeHost.CurrentGameFlowRuntime;
            if (flow != null && flow.CurrentRoute != null &&
                flow.CurrentRouteLifecycleRuntime != null &&
                flow.CurrentRouteLifecycleRuntime.TryCreateCurrentRouteContentDiscoveryScope(
                    flow.CurrentRoute,
                    out RouteContentDiscoveryScope routeScope))
            {
                RouteAsset route = flow.CurrentRoute;
                AddBindings(
                    CreateRecipients(
                        SceneCompositionComponentQuery.GetComponents<
                            PlayerSessionScopedAccessConsumer>(routeScope),
                        SceneCompositionComponentQuery.GetComponents<
                            PlayerSessionJoinTrigger>(routeScope)),
                    LocalPlayerProvisioningConsumerScope.Route,
                    RuntimeContentOwner.Route(route.RouteId.StableText,
                        route.RouteName, RuntimeDefinitionToken.FromUnityObject(route)),
                    flow.CurrentRouteLifecycleRuntime.CurrentRouteOccurrenceSequence,
                    0,
                    desired);
            }

            ActivityFlowRuntime activityFlow =
                flow?.CurrentRouteLifecycleRuntime?.CurrentActivityFlowRuntime;
            ActivityAsset activity = flow?.CurrentActivity;
            if (activityFlow != null && activity != null &&
                activityFlow.TryCreateCurrentActivityContentDiscoveryScope(
                    activity, out ActivityContentDiscoveryScope activityScope))
            {
                AddBindings(
                    CreateRecipients(
                        SceneCompositionComponentQuery.GetComponents<
                            PlayerSessionScopedAccessConsumer>(activityScope, activity),
                        SceneCompositionComponentQuery.GetComponents<
                            PlayerSessionJoinTrigger>(activityScope, activity)),
                    LocalPlayerProvisioningConsumerScope.Activity,
                    RuntimeContentOwner.Activity(activity.ActivityId.StableText,
                        activity.ActivityName, RuntimeDefinitionToken.FromUnityObject(activity)),
                    flow.CurrentRouteLifecycleRuntime.CurrentRouteOccurrenceSequence,
                    flow.CurrentOccurrence.TransitionSequence,
                    desired);
            }

            bool managerJoinAvailable = HasManagerJoinCapability();
            var stale = new List<Component>();
            foreach (var pair in _accesses)
            {
                if (pair.Key == null ||
                    !desired.TryGetValue(pair.Key, out DesiredScopedBinding candidate) ||
                    !pair.Value.Lease.HasSameBindingIdentity(candidate.Lease) ||
                    pair.Value.ManagerJoinAvailable != managerJoinAvailable)
                {
                    stale.Add(pair.Key);
                }
            }

            for (int index = 0; index < stale.Count; index++)
            {
                Component component = stale[index];
                if (_accesses.TryGetValue(component, out ActiveScopedBinding binding))
                {
                    bool released = binding.TryRelease(
                        "Player Session scoped access was released because its Route, Activity or join capability changed.",
                        true,
                        out _);
                    if (released)
                    {
                        _accesses.Remove(component);
                    }
                }
            }

            foreach (var pair in desired)
            {
                if (_accesses.ContainsKey(pair.Key))
                {
                    pair.Value.Lease.Dispose();
                    continue;
                }

                PlayerSessionScopedBindingRecipient recipient = pair.Value.Recipient;
                IDisposable binding = recipient.CreateBinding(pair.Value.Lease);
                string issue = string.Empty;
                if (binding == null || !recipient.TryBind(binding, recipient.Scope, out issue))
                {
                    binding?.Dispose();
                    pair.Value.Lease.Dispose();
                    recipient.Release(
                        string.IsNullOrWhiteSpace(issue)
                            ? "Player Session recipient could not create its scoped binding."
                            : issue,
                        false,
                        out _);
                    continue;
                }

                _accesses.Add(pair.Key, new ActiveScopedBinding(
                    recipient,
                    pair.Value.Lease,
                    binding,
                    managerJoinAvailable));
            }
        }

        private List<PlayerSessionScopedBindingRecipient> CreateRecipients(
            IReadOnlyList<PlayerSessionScopedAccessConsumer> consumers,
            IReadOnlyList<PlayerSessionJoinTrigger> joinTriggers)
        {
            var recipients = new List<PlayerSessionScopedBindingRecipient>();
            if (consumers != null)
            {
                for (int index = 0; index < consumers.Count; index++)
                {
                    PlayerSessionScopedAccessConsumer consumer = consumers[index];
                    if (consumer == null)
                        continue;

                    recipients.Add(PlayerSessionScopedBindingRecipient.ForLegacyConsumer(
                        consumer,
                        lease => CreateAccess(lease, consumer)));
                }
            }

            if (joinTriggers != null)
            {
                for (int index = 0; index < joinTriggers.Count; index++)
                {
                    PlayerSessionJoinTrigger trigger = joinTriggers[index];
                    if (trigger != null)
                    {
                        recipients.Add(PlayerSessionScopedBindingRecipient.ForJoinTrigger(trigger));
                    }
                }
            }

            return recipients;
        }

        private bool HasManagerJoinCapability()
        {
            LocalPlayerProvisioningRuntimeHostModule manager =
                _runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>();
            return manager != null && manager.IsReady;
        }

        private void AddBindings(
            IReadOnlyList<PlayerSessionScopedBindingRecipient> candidates,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            int routeOccurrenceSequence,
            int activityTransitionSequence,
            Dictionary<Component, DesiredScopedBinding> target)
        {
            if (candidates == null)
            {
                return;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                PlayerSessionScopedBindingRecipient recipient = candidates[index];
                if (recipient == null || recipient.Component == null)
                {
                    continue;
                }

                if (!recipient.TryValidateScope(out string issue))
                {
                    recipient.Release(issue, false, out _);
                    continue;
                }

                if (recipient.Scope == scope)
                {
                    LocalPlayerProvisioningRuntimeHostModule manager =
                        _runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>();
                    PlayerSessionJoinBindingLeaseValidation validation =
                        PlayerSessionJoinBindingLease.TryCapture(
                            _runtimeHost,
                            this,
                            _participationContext,
                            recipient,
                            scope,
                            owner,
                            manager,
                            out PlayerSessionJoinBindingLease lease);
                    if (validation != PlayerSessionJoinBindingLeaseValidation.Current)
                    {
                        if (validation == PlayerSessionJoinBindingLeaseValidation.RejectedConfiguration)
                        {
                            recipient.Release(
                                "Player Session recipient scope or owner configuration is invalid.",
                                false,
                                out _);
                        }

                        continue;
                    }

                    if (target.TryGetValue(recipient.Component, out DesiredScopedBinding previous))
                        previous.Lease.Dispose();

                    target[recipient.Component] = new DesiredScopedBinding(recipient, lease);
                }
            }
        }

        private IDisposable CreateAccess(
            PlayerSessionJoinBindingLease lease,
            PlayerSessionScopedAccessConsumer consumer)
        {
            LocalPlayerProvisioningRuntimeHostModule manager = lease.Manager;
            return manager != null && manager.IsReady &&
                ReferenceEquals(manager, _runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>())
                ? new ManagerPlayerSessionScopedAccess(lease, consumer)
                : new PlayerSessionScopedAccess(
                    lease.RuntimeHost,
                    lease.ParticipationContext,
                    lease.Scope,
                    lease.Owner,
                    consumer);
        }

        private void OnDestroy()
        {
            foreach (var pair in _accesses)
            {
                bool released = pair.Value.TryRelease(
                    "Player Session scoped access was released because the Framework runtime was disposed.",
                    true,
                    out string issue);
                if (!released)
                {
                    Debug.LogWarning(
                        $"Player Session scoped access cleanup remained pending at terminal Framework runtime disposal. {issue}",
                        this);
                    pair.Value.Dispose();
                }
            }

            _accesses.Clear();
            _inputOwnershipDiagnosticKeys.Clear();
            _participationContext = null;
            _runtimeHost = null;
        }

        internal void LogInputOwnershipDiagnosticOnce(
            PlayerSlotRuntimeSnapshot slot,
            string status,
            string stage,
            string issue,
            bool hasHostEvidence,
            PlayerHostEvidenceSnapshot retainedHost,
            LocalPlayerHostAuthoring resolvedHost,
            PlayerInput resolvedPlayerInput)
        {
            string slotId = slot.PlayerSlotId.IsValid
                ? slot.PlayerSlotId.StableText
                : "<invalid>";
            string diagnosticIssue = issue ?? string.Empty;
            string registeredHostIssue = stage.StartsWith(
                    "RegisteredHost.", StringComparison.Ordinal)
                ? diagnosticIssue
                : string.Empty;
            string key =
                $"{slotId}|{slot.Revision}|{stage}|{diagnosticIssue}";
            if (!_inputOwnershipDiagnosticKeys.Add(key))
            {
                return;
            }

            LocalPlayerHostAuthoring diagnosticHost =
                !ReferenceEquals(resolvedHost, null)
                    ? resolvedHost
                    : retainedHost.Host;
            bool hostReferenceExists = !ReferenceEquals(diagnosticHost, null);
            bool hostAlive = hostReferenceExists && diagnosticHost != null;
            PlayerInput diagnosticPlayerInput =
                !ReferenceEquals(resolvedPlayerInput, null)
                    ? resolvedPlayerInput
                    : hostAlive
                        ? diagnosticHost.PlayerInput
                        : null;
            bool playerInputReferenceExists =
                !ReferenceEquals(diagnosticPlayerInput, null);
            bool playerInputAlive =
                playerInputReferenceExists && diagnosticPlayerInput != null;
            bool sameGameObject = hostAlive && playerInputAlive &&
                ReferenceEquals(diagnosticHost.gameObject,
                    diagnosticPlayerInput.gameObject);

            var fields = LogFields.Of(
                LogFields.Field("status", status),
                LogFields.Field("stage", stage),
                LogFields.Field("slot", slotId),
                LogFields.Field("slotRevision", slot.Revision),
                LogFields.Field("slotState", slot.AllocationState),
                LogFields.Field("issue", diagnosticIssue),
                LogFields.Field("registeredHostIssue", registeredHostIssue),
                LogFields.Field("hasHostEvidence", hasHostEvidence),
                LogFields.Field("retainedHostSlot",
                    retainedHost.PlayerSlotId.IsValid
                        ? retainedHost.PlayerSlotId.StableText
                        : "<invalid>"),
                LogFields.Field("retainedHostAvailable",
                    retainedHost.HostIsAvailable),
                LogFields.Field("assignmentOrigin",
                    retainedHost.PhysicalProvisioningMode),
                LogFields.Field("assignmentToken",
                    retainedHost.AssignmentToken.IsValid
                        ? retainedHost.AssignmentToken.StableText
                        : "<invalid>"),
                LogFields.Field("hostBinding",
                    retainedHost.HostBindingIdentity.IsValid
                        ? retainedHost.HostBindingIdentity.StableText
                        : "<invalid>"),
                LogFields.Field("runtimeOwnerCorrelation",
                    "not-exposed-by-registered-host-resolver"),
                LogFields.Field("hostExists", hostReferenceExists),
                LogFields.Field("hostAlive", hostAlive),
                LogFields.Field("hostIsJoined",
                    hostAlive && diagnosticHost.IsJoined),
                LogFields.Field("hostHasJoinedSlot",
                    hostAlive && diagnosticHost.HasJoinedSlot),
                LogFields.Field("hostJoinedSlot",
                    hostAlive && diagnosticHost.HasJoinedSlot &&
                    diagnosticHost.JoinedPlayerSlotId.IsValid
                        ? diagnosticHost.JoinedPlayerSlotId.StableText
                        : "<invalid>"),
                LogFields.Field("hostGameObject",
                    hostAlive
                        ? $"{diagnosticHost.gameObject.name}#{diagnosticHost.gameObject.GetEntityId()}"
                        : "<unavailable>"),
                LogFields.Field("playerInputExists",
                    playerInputReferenceExists),
                LogFields.Field("playerInputAlive", playerInputAlive),
                LogFields.Field("playerInputGameObject",
                    playerInputAlive
                        ? $"{diagnosticPlayerInput.gameObject.name}#{diagnosticPlayerInput.gameObject.GetEntityId()}"
                        : "<unavailable>"),
                LogFields.Field("sameHostGameObject", sameGameObject),
                LogFields.Field("playerIndex",
                    playerInputAlive ? diagnosticPlayerInput.playerIndex : -1),
                LogFields.Field("deviceCount",
                    playerInputAlive ? diagnosticPlayerInput.devices.Count : 0),
                LogFields.Field("deviceIds",
                    DescribeInputDeviceIds(diagnosticPlayerInput)),
                LogFields.Field("deviceNames",
                    DescribeInputDeviceNames(diagnosticPlayerInput)));

            FrameworkLogger logger = FrameworkLogger.Create(
                typeof(PlayerSessionScopedAccessRuntimeHostModule));
            if (string.Equals(status, "Succeeded", StringComparison.Ordinal))
            {
                logger.Info("[FRAMEWORK_PLAYER_INPUT_OWNERSHIP_DIAG]", fields);
                return;
            }

            logger.Warning("[FRAMEWORK_PLAYER_INPUT_OWNERSHIP_DIAG]", fields);
        }

        private static string DescribeInputDeviceIds(PlayerInput playerInput)
        {
            if (playerInput == null)
            {
                return "<unavailable>";
            }

            var devices = playerInput.devices;
            if (devices.Count == 0)
            {
                return "<none>";
            }

            var descriptions = new string[devices.Count];
            for (int index = 0; index < devices.Count; index++)
            {
                InputDevice device = devices[index];
                descriptions[index] = ReferenceEquals(device, null)
                    ? $"[{index}]=<null>"
                    : device.deviceId.ToString();
            }

            return string.Join(",", descriptions);
        }

        private static string DescribeInputDeviceNames(PlayerInput playerInput)
        {
            if (playerInput == null)
            {
                return "<unavailable>";
            }

            var devices = playerInput.devices;
            if (devices.Count == 0)
            {
                return "<none>";
            }

            var descriptions = new string[devices.Count];
            for (int index = 0; index < devices.Count; index++)
            {
                InputDevice device = devices[index];
                descriptions[index] = ReferenceEquals(device, null)
                    ? $"[{index}]=<null>"
                    : $"{device.displayName} ({device.layout})";
            }

            return string.Join(",", descriptions);
        }

        private sealed class DesiredScopedBinding
        {
            internal DesiredScopedBinding(
                PlayerSessionScopedBindingRecipient recipient,
                PlayerSessionJoinBindingLease lease)
            {
                Recipient = recipient;
                Lease = lease;
            }

            internal PlayerSessionScopedBindingRecipient Recipient { get; }
            internal PlayerSessionJoinBindingLease Lease { get; }
        }

        internal sealed class ActiveScopedBinding : IDisposable
        {
            private bool _disposed;

            internal ActiveScopedBinding(
                PlayerSessionScopedBindingRecipient recipient,
                PlayerSessionJoinBindingLease lease,
                IDisposable binding,
                bool managerJoinAvailable)
            {
                Recipient = recipient;
                Lease = lease;
                Binding = binding;
                ManagerJoinAvailable = managerJoinAvailable;
            }

            internal PlayerSessionScopedBindingRecipient Recipient { get; }
            internal PlayerSessionJoinBindingLease Lease { get; }
            internal IDisposable Binding { get; }
            internal bool ManagerJoinAvailable { get; }

            internal bool TryRelease(
                string reason,
                bool isStale,
                out string issue)
            {
                if (_disposed)
                {
                    issue = string.Empty;
                    return true;
                }

                if (!Recipient.Release(reason, isStale, out issue))
                    return false;

                Dispose();
                issue = string.Empty;
                return true;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                Binding?.Dispose();
                Lease?.Dispose();
            }
        }
    }

    internal class PlayerSessionScopedAccess : IPlayerSessionScopedAccess, IDisposable
    {
        private readonly FrameworkRuntimeHost _runtimeHost;
        private readonly PlayerParticipationRuntimeContext _participationContext;
        private readonly LocalPlayerProvisioningConsumerScope _scope;
        private readonly RuntimeContentOwner _owner;
        private readonly PlayerSessionScopedAccessConsumer _consumer;
        private event Action<PlayerSessionChange> _changed;
        private bool _disposed;
        private string _diagnostic;

        internal PlayerSessionScopedAccess(
            FrameworkRuntimeHost runtimeHost,
            PlayerParticipationRuntimeContext participationContext,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            PlayerSessionScopedAccessConsumer consumer)
        {
            _runtimeHost = runtimeHost ?? throw new ArgumentNullException(nameof(runtimeHost));
            _participationContext = participationContext ?? throw new ArgumentNullException(nameof(participationContext));
            _scope = scope;
            _owner = owner;
            _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
            _diagnostic = ReadyDiagnostic(owner);
            _participationContext.Changed += ForwardChange;
        }

        internal virtual bool HasJoinCapability => false;

        public event Action<PlayerSessionChange> Changed
        {
            add => _changed += value;
            remove => _changed -= value;
        }

        public PlayerSessionScopedAccessSnapshot Snapshot
        {
            get
            {
                bool available = IsCurrent();
                return new PlayerSessionScopedAccessSnapshot(
                    _scope, _owner, available, _disposed, HasJoinCapability,
                    available ? ReadyDiagnostic(_owner) : CurrentIssue);
            }
        }

        public bool TryGetObservation(out PlayerSessionScopedObservationSnapshot observation)
        {
            if (TryGetContext(out string issue) &&
                _runtimeHost.TryGetPlayerSessionScopedObservation(
                    _scope, _owner, out observation))
            {
                return true;
            }

            observation = PlayerSessionScopedObservationSnapshot.Unavailable(
                _scope, _owner, issue);
            return false;
        }

        public PlayerGameplayAvailabilityBlockResult RequestBlockRuntimeGameplay(
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            if (!TryGetContext(out string issue))
            {
                return PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedRuntimeUnavailable,
                    playerSlotId,
                    issue);
            }

            return _runtimeHost.TryGetPlayerGameplayRuntime(
                    out PlayerGameplayRuntimeHostModule gameplay)
                ? gameplay.RequestBlockRuntimeGameplay(playerSlotId, source, reason)
                : PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedRuntimeUnavailable,
                    playerSlotId,
                    "Player gameplay runtime is unavailable.");
        }

        public PlayerGameplayAvailabilityBlockResult RequestReleaseRuntimeGameplay(
            PlayerGameplayAvailabilityBlockToken blockToken,
            string source,
            string reason)
        {
            if (!TryGetContext(out string issue))
            {
                return PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedRuntimeUnavailable,
                    default,
                    issue);
            }

            return _runtimeHost.TryGetPlayerGameplayRuntime(
                    out PlayerGameplayRuntimeHostModule gameplay)
                ? gameplay.RequestReleaseRuntimeGameplay(blockToken, source, reason)
                : PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedRuntimeUnavailable,
                    default,
                    "Player gameplay runtime is unavailable.");
        }

        public PlayerParticipationOperationResult OpenJoining(string source, string reason)
        {
            if (!_runtimeHost.TryEnterRuntimeOperation(
                    cleanup: false,
                    out IDisposable admission))
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "OpenJoining",
                    source,
                    reason,
                    $"Player Session command rejected while Host phase is '{_runtimeHost.LifecyclePhase}'.");
            }

            using IDisposable admissionLease = admission;
            if (!TryGetContext(out string issue))
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "OpenJoining", source, reason, issue);
            }

            LocalPlayerProvisioningRuntimeHostModule manager =
                _runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>();
            return manager != null && manager.IsReady
                ? manager.TryOpenJoining(source, reason)
                : _participationContext.TryOpenJoining(source, reason);
        }

        public PlayerParticipationOperationResult CloseJoining(string source, string reason)
        {
            if (!_runtimeHost.TryEnterRuntimeOperation(
                    cleanup: true,
                    out IDisposable admission))
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "CloseJoining",
                    source,
                    reason,
                    $"Player Session command rejected while Host phase is '{_runtimeHost.LifecyclePhase}'.");
            }

            using IDisposable admissionLease = admission;
            if (!TryGetContext(out string issue))
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "CloseJoining", source, reason, issue);
            }

            LocalPlayerProvisioningRuntimeHostModule manager =
                _runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>();
            return manager != null && manager.IsReady
                ? manager.TryCloseJoining(source, reason)
                : _participationContext.TryCloseJoining(source, reason);
        }

        public SessionPlayerLeaveResult RequestLeave(SessionPlayerLeaveRequest request)
        {
            if (!_runtimeHost.TryEnterRuntimeOperation(
                    cleanup: true,
                    out IDisposable admission))
            {
                return SessionPlayerLeaveResult.RuntimeUnavailable(
                    request,
                    $"Player Leave rejected while Host phase is '{_runtimeHost.LifecyclePhase}'.");
            }

            using IDisposable admissionLease = admission;
            if (!TryGetContext(out string issue))
            {
                return SessionPlayerLeaveResult.RuntimeUnavailable(request, issue);
            }

            return SessionPlayerLeaveRuntimeHostModule.TryAttach(
                _runtimeHost, out SessionPlayerLeaveRuntimeHostModule leave, out issue)
                ? leave.TryLeave(request)
                : SessionPlayerLeaveResult.RuntimeUnavailable(request, issue);
        }

        public PlayerPreparedActorReplacementResult RequestReplacePreparedActor(
            PlayerPreparedActorReplacementRequest request)
        {
            if (!_runtimeHost.TryEnterRuntimeOperation(
                    cleanup: false,
                    out IDisposable admission))
            {
                return new PlayerPreparedActorReplacementResult(
                    PlayerPreparedActorReplacementStatus.RejectedRuntimeUnavailable,
                    request.PlayerSlotId, default, default, default, default, 0,
                    false, false, false,
                    $"Prepared Actor replacement rejected while Host phase is '{_runtimeHost.LifecyclePhase}'.");
            }

            using IDisposable admissionLease = admission;
            if (!TryGetContext(out string issue) ||
                !_runtimeHost.TryGetPlayerActorPreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparation))
            {
                return new PlayerPreparedActorReplacementResult(
                    PlayerPreparedActorReplacementStatus.RejectedRuntimeUnavailable,
                    request.PlayerSlotId, default, default, default, default, 0,
                    false, false, false,
                    string.IsNullOrWhiteSpace(issue)
                        ? "Prepared Actor replacement runtime is unavailable."
                        : issue);
            }

            return preparation.TryReplacePreparedActorForCurrentActivity(request);
        }

        public PlayerActorSelectionResult RequestSelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            return TryGetSelectionModule(
                    "SelectActorProfile", request, out var module,
                    out PlayerActorSelectionResult unavailable)
                ? module.TrySelectActorProfile(request)
                : unavailable;
        }

        public PlayerActorSelectionResult RequestSelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            var request = new PlayerActorSelectionRequest(
                playerSlotId, null, source, reason, expectedSelectionRevision);
            return TryGetSelectionModule(
                    "SelectDefaultActor", request, out var module,
                    out PlayerActorSelectionResult unavailable)
                ? module.TrySelectDefaultActor(
                    playerSlotId, expectedSelectionRevision, source, reason)
                : unavailable;
        }

        public PlayerActorSelectionResult RequestReplaceActorSelection(
            PlayerActorSelectionRequest request)
        {
            return TryGetSelectionModule(
                    "ReplaceActorSelection", request, out var module,
                    out PlayerActorSelectionResult unavailable)
                ? module.TryReplaceActorSelection(request)
                : unavailable;
        }

        public PlayerActorSelectionResult RequestClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            return TryGetSelectionModule(
                    "ClearActorSelection", request, out var module,
                    out PlayerActorSelectionResult unavailable)
                ? module.TryClearActorSelection(request)
                : unavailable;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _participationContext.Changed -= ForwardChange;
            _changed = null;
            _diagnostic = "Player Session scoped access was released because its Route or Activity scope was replaced or disposed.";
        }

        private bool TryGetSelectionModule(
            string operation,
            PlayerActorSelectionRequest request,
            out PlayerParticipationRuntimeHostModule module,
            out PlayerActorSelectionResult unavailable)
        {
            module = null;
            if (!TryGetContext(out string issue))
            {
                unavailable = PlayerActorSelectionResult.RuntimeUnavailable(
                    operation, request, issue);
                return false;
            }

            module = _runtimeHost.GetComponent<PlayerParticipationRuntimeHostModule>();
            if (module != null && module.IsInitialized)
            {
                unavailable = null;
                return true;
            }

            unavailable = PlayerActorSelectionResult.RuntimeUnavailable(
                    operation, request,
                    "Player participation runtime module is not initialized.");
            return false;
        }

        protected bool TryGetContext(out string issue)
        {
            if (!IsCurrent())
            {
                issue = CurrentIssue;
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool IsCurrent()
        {
            if (_disposed)
            {
                return false;
            }

            if (_consumer == null)
            {
                _disposed = true;
                _diagnostic = "Player Session scoped access was released because its consumer component was destroyed.";
                return false;
            }

            if (!_runtimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext current) ||
                !ReferenceEquals(current, _participationContext) ||
                !MatchesCurrentScope(_runtimeHost, _owner))
            {
                _disposed = true;
                _diagnostic = "Player Session scoped access was released because its Route or Activity scope was replaced or disposed.";
                return false;
            }

            return true;
        }

        private void ForwardChange(PlayerSessionChange change)
        {
            if (IsCurrent())
            {
                _changed?.Invoke(change);
            }
        }

        private string CurrentIssue => string.IsNullOrWhiteSpace(_diagnostic)
            ? "Player Session scoped access is unavailable."
            : _diagnostic;

        private static bool MatchesCurrentScope(
            FrameworkRuntimeHost runtimeHost,
            RuntimeContentOwner expectedOwner)
        {
            var flow = runtimeHost != null ? runtimeHost.CurrentGameFlowRuntime : null;
            if (flow == null)
            {
                return false;
            }

            if (expectedOwner.Scope == RuntimeContentScope.Route)
            {
                RouteAsset route = flow.CurrentRoute;
                return route != null && expectedOwner == RuntimeContentOwner.Route(
                    route.RouteId.StableText, route.RouteName,
                    RuntimeDefinitionToken.FromUnityObject(route));
            }

            if (expectedOwner.Scope == RuntimeContentScope.Activity)
            {
                ActivityAsset activity = flow.CurrentActivity;
                return activity != null && expectedOwner == RuntimeContentOwner.Activity(
                    activity.ActivityId.StableText, activity.ActivityName,
                    RuntimeDefinitionToken.FromUnityObject(activity));
            }

            return false;
        }

        private static string ReadyDiagnostic(RuntimeContentOwner owner) =>
            $"Player Session scoped access is bound to '{owner.StableText}'.";
    }

    internal sealed class ManagerPlayerSessionScopedAccess : PlayerSessionScopedAccess
    {
        private readonly LocalPlayerProvisioningRuntimeHostModule _manager;

        internal ManagerPlayerSessionScopedAccess(
            PlayerSessionJoinBindingLease joinBindingLease,
            PlayerSessionScopedAccessConsumer consumer)
            : base(
                joinBindingLease.RuntimeHost,
                joinBindingLease.ParticipationContext,
                joinBindingLease.Scope,
                joinBindingLease.Owner,
                consumer)
        {
            if (joinBindingLease == null)
                throw new ArgumentNullException(nameof(joinBindingLease));
            _manager = joinBindingLease.Manager ?? throw new ArgumentNullException(nameof(joinBindingLease));
        }

        internal override bool HasJoinCapability => _manager.IsReady;
    }

    internal static class PlayerSessionScopedObservationProjection
    {
        internal static bool TryGetPlayerSessionScopedObservation(
            this FrameworkRuntimeHost runtimeHost,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner scopeOwner,
            out PlayerSessionScopedObservationSnapshot observation)
        {
            if (runtimeHost == null || !runtimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext participationContext))
            {
                observation = PlayerSessionScopedObservationSnapshot.Unavailable(
                    scope, scopeOwner,
                    "Player Session participation context is unavailable.");
                return false;
            }

            PlayerParticipationSnapshot participation = participationContext.CreateSnapshot();
            if (participation == null || !participation.IsInitialized)
            {
                observation = PlayerSessionScopedObservationSnapshot.Unavailable(
                    scope, scopeOwner,
                    "Player Session participation snapshot is unavailable.");
                return false;
            }

            PlayerParticipationRuntimeHostModule participationModule =
                runtimeHost.GetComponent<PlayerParticipationRuntimeHostModule>();
            EffectivePlayerSessionConfiguration initializationConfiguration =
                participationModule != null && participationModule.IsInitialized
                    ? participationModule.EffectiveConfiguration
                    : null;
            var occurrence = runtimeHost.CurrentGameFlowRuntime?.CurrentOccurrence ?? default;
            RuntimeContentOwner activityOwner = occurrence.IsValid &&
                occurrence.Activity != null && occurrence.Activity.HasValidActivityId
                    ? RuntimeContentOwner.Activity(
                        occurrence.Activity.ActivityId.StableText,
                        occurrence.Activity.ActivityName,
                        RuntimeDefinitionToken.FromUnityObject(occurrence.Activity))
                    : default;
            int activityOccurrence = occurrence.IsValid ? occurrence.TransitionSequence : 0;

            PlayerActorPreparationRuntimeHostModule preparation = null;
            PlayerActorPreparationRuntimeHostSnapshot preparationSnapshot = null;
            bool preparationAvailable = runtimeHost.TryGetPlayerActorPreparationRuntime(
                out preparation) && preparation.TryGetSnapshot(out preparationSnapshot) &&
                preparationSnapshot != null && preparationSnapshot.IsInitialized;

            PlayerGameplayRuntimeHostSnapshot gameplaySnapshot = null;
            bool gameplayAvailable = runtimeHost.TryGetPlayerGameplayRuntimeSnapshot(
                out gameplaySnapshot) && gameplaySnapshot != null &&
                gameplaySnapshot.IsInitialized && gameplaySnapshot.Admission != null;

            var slots = new List<PlayerSessionScopedSlotObservation>(
                participation.Slots.Count);
            PlayerSessionScopedAccessRuntimeHostModule diagnostics =
                runtimeHost.GetComponent<PlayerSessionScopedAccessRuntimeHostModule>();
            for (int index = 0; index < participation.Slots.Count; index++)
            {
                slots.Add(CreateSlotObservation(participation.Slots[index], preparation,
                    preparationAvailable ? preparationSnapshot : null,
                    gameplayAvailable ? gameplaySnapshot : null, activityOwner,
                    diagnostics));
            }

            observation = new PlayerSessionScopedObservationSnapshot(
                true, scope, scopeOwner, participation, initializationConfiguration,
                activityOwner, activityOccurrence, slots,
                $"Player Session observation: preparation='{(preparationAvailable ? "available" : "unavailable")}' gameplay='{(gameplayAvailable ? "available" : "unavailable")}'.");
            return true;
        }

        private static PlayerSessionScopedSlotObservation CreateSlotObservation(
            PlayerSlotRuntimeSnapshot slot,
            PlayerActorPreparationRuntimeHostModule preparation,
            PlayerActorPreparationRuntimeHostSnapshot preparationSnapshot,
            PlayerGameplayRuntimeHostSnapshot gameplaySnapshot,
            RuntimeContentOwner currentActivityOwner,
            PlayerSessionScopedAccessRuntimeHostModule diagnostics)
        {
            PlayerHostEvidenceSummary hostEvidence = default;
            PlayerHostEvidenceSnapshot retainedHost = default;
            bool hasHostEvidence = preparation != null && preparation.TryGetRetainedHostEvidence(
                slot.PlayerSlotId, out retainedHost);
            if (hasHostEvidence)
            {
                hostEvidence = new PlayerHostEvidenceSummary(retainedHost.PlayerSlotId,
                    retainedHost.PhysicalProvisioningMode, retainedHost.AssignmentToken,
                    retainedHost.HostBindingIdentity, retainedHost.HostIsAvailable,
                    retainedHost.Source, retainedHost.Reason, retainedHost.HostIsAvailable
                        ? "Retained Local Player Host evidence is available."
                        : "Retained Local Player Host evidence references an unavailable Host.");
            }

            bool hasPreparationEvidence = TryGetPreparation(
                preparationSnapshot?.Preparation, slot.PlayerSlotId,
                out PlayerActorPreparationSummary preparationSummary) &&
                IsCurrentPreparation(preparationSummary);
            if (!hasPreparationEvidence) preparationSummary = default;

            CurrentPlayerSlotActorSnapshot currentActor = default;
            bool hasCurrentActorEvidence = preparation != null &&
                preparation.TryGetCurrentSlotActorSnapshot(slot.PlayerSlotId,
                    out currentActor) && IsCurrentActor(currentActor);
            if (!hasCurrentActorEvidence) currentActor = default;

            PlayerGameplayAdmissionSummary gameplayAdmission = default;
            bool hasGameplayAdmissionEvidence = gameplaySnapshot?.Admission != null &&
                gameplaySnapshot.Admission.TryGetSummary(slot.PlayerSlotId,
                    out gameplayAdmission) &&
                IsCurrentGameplayAdmission(gameplayAdmission, currentActivityOwner);
            if (!hasGameplayAdmissionEvidence) gameplayAdmission = default;

            bool hasInputOwnershipEvidence = TryCreateInputOwnership(
                slot, preparation, hasHostEvidence, retainedHost, diagnostics,
                out LocalPlayerInputOwnershipSummary inputOwnership);

            return new PlayerSessionScopedSlotObservation(slot, hostEvidence,
                hasHostEvidence, preparationSummary, hasPreparationEvidence,
                currentActor, hasCurrentActorEvidence, gameplayAdmission,
                hasGameplayAdmissionEvidence,
                inputOwnership, hasInputOwnershipEvidence);
        }

        private static bool TryCreateInputOwnership(
            PlayerSlotRuntimeSnapshot slot,
            PlayerActorPreparationRuntimeHostModule preparation,
            bool hasHostEvidence,
            PlayerHostEvidenceSnapshot retainedHost,
            PlayerSessionScopedAccessRuntimeHostModule diagnostics,
            out LocalPlayerInputOwnershipSummary summary)
        {
            summary = default;
            if (!slot.IsValid || !slot.IsJoined)
            {
                return false;
            }

            if (preparation == null)
            {
                diagnostics?.LogInputOwnershipDiagnosticOnce(
                    slot, "Failed", "Preparation.Unavailable",
                    "Player Actor preparation runtime is unavailable.",
                    hasHostEvidence, retainedHost, null, null);
                return false;
            }

            if (!preparation.TryGetRegisteredHost(slot.PlayerSlotId,
                    out LocalPlayerHostAuthoring host, out string hostIssue))
            {
                diagnostics?.LogInputOwnershipDiagnosticOnce(
                    slot, "Failed",
                    RegisteredHostFailureStage(
                        slot.PlayerSlotId, hasHostEvidence, retainedHost),
                    hostIssue,
                    hasHostEvidence, retainedHost, host, null);
                return false;
            }

            // O Host físico da Session pode existir sem assignment ou Actor de Activity.
            var playerInput = host.PlayerInput;
            if (playerInput == null)
            {
                diagnostics?.LogInputOwnershipDiagnosticOnce(
                    slot, "Failed", "PlayerInput.Unavailable",
                    "Registered Local Player Host has no live PlayerInput.",
                    hasHostEvidence, retainedHost, host, playerInput);
                return false;
            }

            if (playerInput.gameObject != host.gameObject)
            {
                diagnostics?.LogInputOwnershipDiagnosticOnce(
                    slot, "Failed", "PlayerInput.GameObjectMismatch",
                    "Registered Local Player Host and PlayerInput do not share the same GameObject.",
                    hasHostEvidence, retainedHost, host, playerInput);
                return false;
            }

            var pairedDevices = playerInput.devices;
            var devices = new LocalPlayerInputDeviceSummary[pairedDevices.Count];
            for (int index = 0; index < pairedDevices.Count; index++)
            {
                var device = pairedDevices[index];
                if (device == null)
                {
                    diagnostics?.LogInputOwnershipDiagnosticOnce(
                        slot, "Failed", "Devices.NullReference",
                        $"Paired device at index '{index}' is null.",
                        hasHostEvidence, retainedHost, host, playerInput);
                    return false;
                }

                devices[index] = new LocalPlayerInputDeviceSummary(
                    device.deviceId, device.layout, device.displayName);
            }

            summary = new LocalPlayerInputOwnershipSummary(
                playerInput.playerIndex, playerInput.currentControlScheme, devices);
            diagnostics?.LogInputOwnershipDiagnosticOnce(
                slot, "Succeeded", "Succeeded", string.Empty,
                hasHostEvidence, retainedHost, host, playerInput);
            return true;
        }

        private static string RegisteredHostFailureStage(
            PlayerSlotId expectedSlot,
            bool hasHostEvidence,
            PlayerHostEvidenceSnapshot retainedHost)
        {
            if (!hasHostEvidence || !retainedHost.HasRetainedHostReference)
            {
                return "RegisteredHost.NotRegistered";
            }

            if (!retainedHost.HostIsAvailable)
            {
                return "RegisteredHost.Destroyed";
            }

            LocalPlayerHostAuthoring host = retainedHost.Host;
            if (!host.IsJoined)
            {
                return "RegisteredHost.NotJoined";
            }

            if (!host.HasJoinedSlot || host.JoinedPlayerSlotId != expectedSlot)
            {
                return "RegisteredHost.SlotMismatch";
            }

            return "RegisteredHost.Other";
        }

        private static bool TryGetPreparation(PlayerActorPreparationSnapshot snapshot,
            PlayerSlotId playerSlotId, out PlayerActorPreparationSummary summary)
        {
            if (snapshot != null)
            {
                for (int index = 0; index < snapshot.Slots.Count; index++)
                {
                    if (snapshot.Slots[index].PlayerSlotId == playerSlotId)
                    {
                        summary = snapshot.Slots[index];
                        return true;
                    }
                }
            }

            summary = default;
            return false;
        }

        private static bool IsCurrentPreparation(PlayerActorPreparationSummary summary) =>
            summary.IsValid && (summary.IsUnprepared ||
                (summary.HasActorEvidence &&
                 summary.ActorEvidence.Owner.Scope == RuntimeContentScope.Session));

        private static bool IsCurrentActor(CurrentPlayerSlotActorSnapshot snapshot) =>
            snapshot.HasCurrentActor &&
            snapshot.ActorEvidence.Owner.Scope == RuntimeContentScope.Session;

        private static bool IsCurrentGameplayAdmission(
            PlayerGameplayAdmissionSummary summary,
            RuntimeContentOwner currentActivityOwner) =>
            currentActivityOwner.IsValid && summary.IsValid &&
            summary.Owner == currentActivityOwner;
    }
}
