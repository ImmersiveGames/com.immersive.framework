using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

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
        private readonly Dictionary<PlayerSessionScopedAccessConsumer,
            PlayerSessionScopedAccess> _accesses = new();
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
            var desired = new Dictionary<PlayerSessionScopedAccessConsumer,
                RuntimeContentOwner>();
            var flow = _runtimeHost.CurrentGameFlowRuntime;
            if (flow != null && flow.CurrentRoute != null &&
                flow.CurrentRouteLifecycleRuntime != null &&
                flow.CurrentRouteLifecycleRuntime.TryCreateCurrentRouteContentDiscoveryScope(
                    flow.CurrentRoute,
                    out RouteContentDiscoveryScope routeScope))
            {
                RouteAsset route = flow.CurrentRoute;
                AddBindings(
                    SceneCompositionComponentQuery.GetComponents<
                        PlayerSessionScopedAccessConsumer>(routeScope),
                    LocalPlayerProvisioningConsumerScope.Route,
                    RuntimeContentOwner.Route(route.RouteId.StableText,
                        route.RouteName, RuntimeDefinitionToken.FromUnityObject(route)),
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
                    SceneCompositionComponentQuery.GetComponents<
                        PlayerSessionScopedAccessConsumer>(activityScope, activity),
                    LocalPlayerProvisioningConsumerScope.Activity,
                    RuntimeContentOwner.Activity(activity.ActivityId.StableText,
                        activity.ActivityName, RuntimeDefinitionToken.FromUnityObject(activity)),
                    desired);
            }

            var stale = new List<PlayerSessionScopedAccessConsumer>();
            foreach (var pair in _accesses)
            {
                bool managerJoinAvailable = HasManagerJoinCapability();
                if (pair.Key == null || !desired.TryGetValue(pair.Key, out RuntimeContentOwner owner) ||
                    pair.Value.Snapshot.Owner != owner ||
                    pair.Value.HasJoinCapability != managerJoinAvailable)
                {
                    stale.Add(pair.Key);
                }
            }

            for (int index = 0; index < stale.Count; index++)
            {
                PlayerSessionScopedAccessConsumer consumer = stale[index];
                if (_accesses.TryGetValue(consumer, out PlayerSessionScopedAccess access))
                {
                    access.Dispose();
                    consumer?.ReleaseScopedAccess(
                        "Player Session scoped access was released because its Route, Activity or join capability changed.",
                        true);
                    _accesses.Remove(consumer);
                }
            }

            foreach (var pair in desired)
            {
                if (_accesses.ContainsKey(pair.Key))
                {
                    continue;
                }

                LocalPlayerProvisioningConsumerScope scope =
                    pair.Value.Scope == RuntimeContentScope.Route
                        ? LocalPlayerProvisioningConsumerScope.Route
                        : LocalPlayerProvisioningConsumerScope.Activity;
                PlayerSessionScopedAccess access = CreateAccess(scope, pair.Value, pair.Key);
                if (!pair.Key.TryBind(access, scope, out string issue))
                {
                    access.Dispose();
                    pair.Key.ReleaseScopedAccess(issue);
                    continue;
                }

                _accesses.Add(pair.Key, access);
            }
        }

        private PlayerSessionScopedAccess CreateAccess(
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            PlayerSessionScopedAccessConsumer consumer)
        {
            LocalPlayerProvisioningRuntimeHostModule manager =
                _runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>();
            return manager != null && manager.IsReady
                ? new ManagerPlayerSessionScopedAccess(
                    _runtimeHost, _participationContext, scope, owner, consumer, manager)
                : new PlayerSessionScopedAccess(
                    _runtimeHost, _participationContext, scope, owner, consumer);
        }

        private bool HasManagerJoinCapability()
        {
            LocalPlayerProvisioningRuntimeHostModule manager =
                _runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>();
            return manager != null && manager.IsReady;
        }

        private static void AddBindings(
            IReadOnlyList<PlayerSessionScopedAccessConsumer> candidates,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            Dictionary<PlayerSessionScopedAccessConsumer, RuntimeContentOwner> target)
        {
            if (candidates == null)
            {
                return;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                PlayerSessionScopedAccessConsumer consumer = candidates[index];
                if (consumer == null)
                {
                    continue;
                }

                if (!consumer.TryValidateScope(out string issue))
                {
                    consumer.ReleaseScopedAccess(issue);
                    continue;
                }

                if (consumer.Scope == scope)
                {
                    target[consumer] = owner;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var pair in _accesses)
            {
                pair.Value.Dispose();
                pair.Key?.ReleaseScopedAccess(
                    "Player Session scoped access was released because the Framework runtime was disposed.",
                    true);
            }

            _accesses.Clear();
            _participationContext = null;
            _runtimeHost = null;
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

        public PlayerParticipationOperationResult OpenJoining(string source, string reason)
        {
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
            if (!TryGetContext(out string issue))
            {
                return SessionPlayerLeaveResult.RuntimeUnavailable(request, issue);
            }

            return SessionPlayerLeaveRuntimeHostModule.TryAttach(
                _runtimeHost, out SessionPlayerLeaveRuntimeHostModule leave, out issue)
                ? leave.TryLeave(request)
                : SessionPlayerLeaveResult.RuntimeUnavailable(request, issue);
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

        private bool TryGetContext(out string issue)
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

    internal sealed class ManagerPlayerSessionScopedAccess :
        PlayerSessionScopedAccess,
        ILocalPlayerJoinAccess
    {
        private readonly LocalPlayerProvisioningRuntimeHostModule _manager;

        internal ManagerPlayerSessionScopedAccess(
            FrameworkRuntimeHost runtimeHost,
            PlayerParticipationRuntimeContext participationContext,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            PlayerSessionScopedAccessConsumer consumer,
            LocalPlayerProvisioningRuntimeHostModule manager)
            : base(runtimeHost, participationContext, scope, owner, consumer)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        internal override bool HasJoinCapability => _manager.IsReady;

        internal bool TryGetLegacyObservation(
            out LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            return _manager.TryGetObservation(
                Snapshot.Scope,
                Snapshot.Owner,
                out observation);
        }

        public LocalPlayerJoinResult RequestJoin(LocalPlayerJoinRequest request)
        {
            return _manager.IsReady
                ? _manager.RegisterJoinWithActorPreparation(_manager.TryJoin(request))
                : LocalPlayerJoinResult.RuntimeUnavailable(request,
                    "Manager-Provisioned Player join capability is unavailable.");
        }
    }

    /// <summary>
    /// Source-compatible facade for the retired combined Manager endpoint.
    /// It owns no state and delegates to the same live ACCESS-2 transport.
    /// </summary>
    internal sealed class LegacyManagerProvisioningConsumerAccess :
        ILocalPlayerProvisioningConsumerAccess
    {
        private readonly ManagerPlayerSessionScopedAccess _access;

        internal LegacyManagerProvisioningConsumerAccess(
            ManagerPlayerSessionScopedAccess access)
        {
            _access = access ?? throw new ArgumentNullException(nameof(access));
        }

        public PlayerSessionScopedAccessSnapshot Snapshot => _access.Snapshot;

        public event Action<PlayerSessionChange> Changed
        {
            add => _access.Changed += value;
            remove => _access.Changed -= value;
        }

        public bool TryGetObservation(
            out PlayerSessionScopedObservationSnapshot observation) =>
            _access.TryGetObservation(out observation);

        public bool TryGetObservation(
            out LocalPlayerProvisioningConsumerObservationSnapshot observation) =>
            _access.TryGetLegacyObservation(out observation);

        public PlayerParticipationOperationResult OpenJoining(string source, string reason) =>
            _access.OpenJoining(source, reason);

        public PlayerParticipationOperationResult CloseJoining(string source, string reason) =>
            _access.CloseJoining(source, reason);

        public LocalPlayerJoinResult RequestJoin(LocalPlayerJoinRequest request) =>
            _access.RequestJoin(request);

        public SessionPlayerLeaveResult RequestLeave(SessionPlayerLeaveRequest request) =>
            _access.RequestLeave(request);

        public PlayerActorSelectionResult RequestSelectActorProfile(
            PlayerActorSelectionRequest request) =>
            _access.RequestSelectActorProfile(request);

        public PlayerActorSelectionResult RequestSelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason) =>
            _access.RequestSelectDefaultActor(
                playerSlotId, expectedSelectionRevision, source, reason);

        public PlayerActorSelectionResult RequestReplaceActorSelection(
            PlayerActorSelectionRequest request) =>
            _access.RequestReplaceActorSelection(request);

        public PlayerActorSelectionResult RequestClearActorSelection(
            PlayerActorSelectionRequest request) =>
            _access.RequestClearActorSelection(request);
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
            for (int index = 0; index < participation.Slots.Count; index++)
            {
                slots.Add(CreateSlotObservation(participation.Slots[index], preparation,
                    preparationAvailable ? preparationSnapshot : null,
                    gameplayAvailable ? gameplaySnapshot : null, activityOwner));
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
            RuntimeContentOwner currentActivityOwner)
        {
            PlayerHostEvidenceSummary hostEvidence = default;
            PlayerHostEvidenceSnapshot retainedHost = default;
            bool hasHostEvidence = preparation != null && preparation.TryGetRetainedHostEvidence(
                slot.PlayerSlotId, out retainedHost);
            if (hasHostEvidence)
            {
                hostEvidence = new PlayerHostEvidenceSummary(retainedHost.PlayerSlotId,
                    retainedHost.AssignmentOrigin, retainedHost.AssignmentToken,
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

            return new PlayerSessionScopedSlotObservation(slot, hostEvidence,
                hasHostEvidence, preparationSummary, hasPreparationEvidence,
                currentActor, hasCurrentActorEvidence, gameplayAdmission,
                hasGameplayAdmissionEvidence);
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
