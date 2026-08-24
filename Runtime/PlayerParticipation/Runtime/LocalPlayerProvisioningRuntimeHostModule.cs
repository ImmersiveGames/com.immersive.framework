using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.Identity;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped composition adapter that owns one LocalPlayerProvisioningBridge for the
    /// established FrameworkRuntimeHost lifetime. Slot state remains in the plain C# context and
    /// Unity technical-host creation remains in PlayerInputManager.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3G/P3J host-scoped local Player technical-host provisioning composition adapter.")]
    internal sealed partial class LocalPlayerProvisioningRuntimeHostModule : MonoBehaviour
    {
        private FrameworkRuntimeHost _runtimeHost;
        private PlayerParticipationRuntimeContext _participationContext;
        private LocalPlayerProvisioningAuthoring _authoring;
        private LocalPlayerProvisioningBridge _bridge;
        private readonly Dictionary<
            LocalPlayerProvisioningConsumerAccessBinding,
            LocalPlayerProvisioningConsumerAccess> _consumerAccesses = new();
        private string _diagnostic = "Local Player provisioning runtime is not initialized.";
        private int _requestCount;

        internal bool IsReady =>
            _runtimeHost != null &&
            _participationContext != null &&
            _authoring != null &&
            _bridge != null;

        internal string Diagnostic => _diagnostic;
        internal LocalPlayerProvisioningAuthoring Authoring => _authoring;
        internal LocalPlayerJoinResult LastJoinResult => _bridge?.LastResult;
        internal int RequestCount => _requestCount;

        internal static bool TryAttach(
            FrameworkRuntimeHost runtimeHost,
            LocalPlayerProvisioningAuthoring authoring,
            out LocalPlayerProvisioningRuntimeHostModule module,
            out string issue)
        {
            module = null;
            issue = string.Empty;

            if (runtimeHost == null)
            {
                issue = "Local Player provisioning requires an explicit FrameworkRuntimeHost.";
                authoring?.ReportRuntimeInitializationFailure(issue);
                return false;
            }

            if (authoring == null)
            {
                issue = "Local Player provisioning requires an explicit authoring component.";
                return false;
            }

            module = runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>();
            if (module == null)
            {
                module = runtimeHost.gameObject.AddComponent<LocalPlayerProvisioningRuntimeHostModule>();
            }

            bool initialized = module.TryInitialize(runtimeHost, authoring, out issue);
            if (!initialized)
            {
                authoring.ReportRuntimeInitializationFailure(issue);
            }

            return initialized;
        }

        internal bool TryInitialize(
            FrameworkRuntimeHost targetRuntimeHost,
            LocalPlayerProvisioningAuthoring targetAuthoring,
            out string issue)
        {
            issue = string.Empty;

            if (IsReady)
            {
                if (ReferenceEquals(_runtimeHost, targetRuntimeHost) &&
                    ReferenceEquals(_authoring, targetAuthoring))
                {
                    return true;
                }

                issue = "Local Player provisioning runtime is already bound to another authoring surface.";
                return false;
            }

            if (targetRuntimeHost == null)
            {
                issue = "FrameworkRuntimeHost is missing.";
                _diagnostic = issue;
                return false;
            }

            if (targetAuthoring == null)
            {
                issue = "Local Player provisioning authoring is missing.";
                _diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext targetParticipationContext))
            {
                issue = "FrameworkRuntimeHost has no initialized Session Player participation context.";
                _diagnostic = issue;
                return false;
            }

            if (!targetAuthoring.TryMaterializeManagerPrefab(out string materializationDiagnostic))
            {
                issue = materializationDiagnostic;
                _diagnostic = issue;
                return false;
            }

            if (!TryValidateRuntimeConfiguration(
                    targetAuthoring,
                    targetParticipationContext,
                    out issue))
            {
                _diagnostic = issue;
                return false;
            }

            PlayerInputManager targetManager = targetAuthoring.PlayerInputManager;
            targetManager.DisableJoining();
            if (targetManager.joiningEnabled)
            {
                issue =
                    $"PlayerInputManager '{targetManager.name}' did not close its technical joining gate during Session initialization.";
                _diagnostic = issue;
                return false;
            }

            if (!LocalPlayerProvisioningBridge.TryCreate(
                    targetParticipationContext,
                    targetAuthoring,
                    targetRuntimeHost.transform,
                    out LocalPlayerProvisioningBridge targetBridge,
                    out issue))
            {
                _diagnostic = issue;
                return false;
            }

            _runtimeHost = targetRuntimeHost;
            _participationContext = targetParticipationContext;
            _authoring = targetAuthoring;
            _bridge = targetBridge;
            _requestCount = 0;
            _diagnostic =
                $"Local Player provisioning runtime is ready. manager='{_authoring.PlayerInputManager.name}' localPlayerHostPrefab='{_authoring.LocalPlayerHostPrefab.name}'. {materializationDiagnostic}";

            try
            {
                _authoring.BindRuntime(this);
            }
            catch (Exception exception)
            {
                _bridge.Dispose();
                _bridge = null;
                _authoring = null;
                _participationContext = null;
                _runtimeHost = null;
                _diagnostic =
                    $"Local Player provisioning authoring rejected Session runtime binding. {exception.Message}";
                issue = _diagnostic;
                return false;
            }

            return true;
        }

        private void Update()
        {
            if (IsReady)
            {
                RefreshConsumerAccessBindings();
            }
        }

        private void RefreshConsumerAccessBindings()
        {
            var desired = new Dictionary<
                LocalPlayerProvisioningConsumerAccessBinding,
                RuntimeContentOwner>();
            var flow = _runtimeHost.CurrentGameFlowRuntime;
            if (flow != null && flow.CurrentRoute != null)
            {
                RouteAsset route = flow.CurrentRoute;
                RouteLifecycleRuntime routeLifecycle =
                    flow.CurrentRouteLifecycleRuntime;
                if (routeLifecycle != null &&
                    routeLifecycle.TryCreateCurrentRouteContentDiscoveryScope(
                        route,
                        out RouteContentDiscoveryScope routeScope))
                {
                    RuntimeContentOwner routeOwner = RuntimeContentOwner.Route(
                        route.RouteId.StableText,
                        route.RouteName,
                        RuntimeDefinitionToken.FromUnityObject(route));
                    AddBindings(
                        SceneCompositionComponentQuery.GetComponents<
                            LocalPlayerProvisioningConsumerAccessBinding>(
                            routeScope),
                        LocalPlayerProvisioningConsumerScope.Route,
                        routeOwner,
                        desired);
                }
            }

            Immersive.Framework.ActivityFlow.ActivityFlowRuntime activityFlow =
                flow?.CurrentRouteLifecycleRuntime?.CurrentActivityFlowRuntime;
            ActivityAsset activity = flow?.CurrentActivity;
            if (activityFlow != null && activity != null &&
                activityFlow.TryCreateCurrentActivityContentDiscoveryScope(
                    activity,
                    out Immersive.Framework.ActivityFlow.ActivityContentDiscoveryScope activityScope))
            {
                RuntimeContentOwner activityOwner = RuntimeContentOwner.Activity(
                    activity.ActivityId.StableText,
                    activity.ActivityName,
                    RuntimeDefinitionToken.FromUnityObject(activity));
                AddBindings(
                    SceneCompositionComponentQuery.GetComponents<
                        LocalPlayerProvisioningConsumerAccessBinding>(
                        activityScope,
                        activity),
                    LocalPlayerProvisioningConsumerScope.Activity,
                    activityOwner,
                    desired);
            }

            var staleBindings = new List<
                LocalPlayerProvisioningConsumerAccessBinding>();
            foreach (var pair in _consumerAccesses)
            {
                if (pair.Key == null || !desired.TryGetValue(
                        pair.Key,
                        out RuntimeContentOwner desiredOwner) ||
                    pair.Value.Snapshot.Owner != desiredOwner)
                {
                    staleBindings.Add(pair.Key);
                }
            }

            for (int index = 0; index < staleBindings.Count; index++)
            {
                LocalPlayerProvisioningConsumerAccessBinding binding =
                    staleBindings[index];
                if (_consumerAccesses.TryGetValue(binding, out var access))
                {
                    access.Dispose();
                    if (binding != null)
                    {
                        binding.Release(
                            "Local Player provisioning consumer binding was released because its Route or Activity scope changed.",
                            true);
                    }

                    _consumerAccesses.Remove(binding);
                }
            }

            foreach (var pair in desired)
            {
                if (_consumerAccesses.ContainsKey(pair.Key))
                {
                    continue;
                }

                LocalPlayerProvisioningConsumerScope actualScope =
                    pair.Value.Scope == RuntimeContentScope.Route
                        ? LocalPlayerProvisioningConsumerScope.Route
                        : LocalPlayerProvisioningConsumerScope.Activity;
                var access = new LocalPlayerProvisioningConsumerAccess(
                    _authoring,
                    actualScope,
                    pair.Value,
                    pair.Key,
                    IsCurrentConsumerScope);
                if (!pair.Key.TryBind(access, actualScope, out string issue))
                {
                    access.Dispose();
                    pair.Key.Release(issue);
                    continue;
                }

                _consumerAccesses.Add(pair.Key, access);
            }
        }

        private bool IsCurrentConsumerScope(RuntimeContentOwner expectedOwner)
        {
            var flow = _runtimeHost != null ? _runtimeHost.CurrentGameFlowRuntime : null;
            if (flow == null)
            {
                return false;
            }

            if (expectedOwner.Scope == RuntimeContentScope.Route)
            {
                RouteAsset currentRoute = flow.CurrentRoute;
                return currentRoute != null && expectedOwner == RuntimeContentOwner.Route(
                    currentRoute.RouteId.StableText,
                    currentRoute.RouteName,
                    RuntimeDefinitionToken.FromUnityObject(currentRoute));
            }

            if (expectedOwner.Scope == RuntimeContentScope.Activity)
            {
                ActivityAsset currentActivity = flow.CurrentActivity;
                return currentActivity != null && expectedOwner == RuntimeContentOwner.Activity(
                    currentActivity.ActivityId.StableText,
                    currentActivity.ActivityName,
                    RuntimeDefinitionToken.FromUnityObject(currentActivity));
            }

            return false;
        }

        private static void AddBindings(
            IReadOnlyList<LocalPlayerProvisioningConsumerAccessBinding>
                candidates,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            Dictionary<LocalPlayerProvisioningConsumerAccessBinding,
                RuntimeContentOwner> target)
        {
            if (candidates == null)
            {
                return;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                LocalPlayerProvisioningConsumerAccessBinding binding =
                    candidates[index];
                if (binding == null)
                {
                    continue;
                }

                if (!binding.Scope.IsDefinedScope())
                {
                    binding.Release(
                        "Local Player provisioning consumer binding requires an explicit Route or Activity scope.");
                    continue;
                }

                if (binding.Scope != scope)
                {
                    continue;
                }

                target[binding] = owner;
            }
        }

        private static bool TryValidateRuntimeConfiguration(
            LocalPlayerProvisioningAuthoring targetAuthoring,
            PlayerParticipationRuntimeContext targetParticipationContext,
            out string issue)
        {
            issue = string.Empty;
            PlayerInputManager manager = targetAuthoring.PlayerInputManager;
            if (manager == null)
            {
                issue = "Local Player provisioning authoring has no explicit PlayerInputManager.";
                return false;
            }

            if (!targetAuthoring.UsesManualJoin)
            {
                issue =
                    $"PlayerInputManager '{manager.name}' must use Join Players Manually. Current join behavior is '{manager.joinBehavior}'.";
                return false;
            }

            if (!targetAuthoring.UsesCSharpJoinNotifications)
            {
                issue =
                    $"PlayerInputManager '{manager.name}' must use Invoke C# Events notifications. Current notification behavior is '{manager.notificationBehavior}'.";
                return false;
            }

            int configuredSlotCount = targetParticipationContext
                .CreateSnapshot()
                .ConfiguredSlotCount;
            if (manager.maxPlayerCount != configuredSlotCount)
            {
                issue =
                    $"PlayerInputManager '{manager.name}' has derived bridge limit '{manager.maxPlayerCount}', but the initialized Player Session has '{configuredSlotCount}' Supported Slots. Update the serialized PlayerInputManager bridge configuration before boot.";
                return false;
            }

            GameObject localPlayerHostPrefab = targetAuthoring.LocalPlayerHostPrefab;
            if (localPlayerHostPrefab == null)
            {
                issue = "Local Player provisioning authoring has no Local Player Host Prefab.";
                return false;
            }

            if (!ReferenceEquals(manager.playerPrefab, localPlayerHostPrefab))
            {
                issue =
                    $"PlayerInputManager '{manager.name}' is not materialized with the authored Local Player Host Prefab '{localPlayerHostPrefab.name}'.";
                return false;
            }

            PlayerInput prefabPlayerInput = localPlayerHostPrefab.GetComponent<PlayerInput>();
            if (prefabPlayerInput == null)
            {
                issue = $"Local Player Host Prefab '{localPlayerHostPrefab.name}' has no PlayerInput component.";
                return false;
            }

            LocalPlayerHostAuthoring prefabHost =
                localPlayerHostPrefab.GetComponent<LocalPlayerHostAuthoring>();
            if (prefabHost == null)
            {
                issue = $"Local Player Host Prefab '{localPlayerHostPrefab.name}' has no LocalPlayerHostAuthoring.";
                return false;
            }

            if (!ReferenceEquals(prefabHost.PlayerInput, prefabPlayerInput))
            {
                issue =
                    $"LocalPlayerHostAuthoring on Local Player Host Prefab '{localPlayerHostPrefab.name}' does not resolve the prefab PlayerInput.";
                return false;
            }

            if (!prefabHost.TryValidateConfiguration(out string hostIssue))
            {
                issue = $"Local Player Host Prefab '{localPlayerHostPrefab.name}' is invalid. {hostIssue}";
                return false;
            }

            return true;
        }

        internal bool IsReadyFor(LocalPlayerProvisioningAuthoring targetAuthoring)
        {
            return IsReady && ReferenceEquals(_authoring, targetAuthoring);
        }

        internal LocalPlayerJoinResult TryJoin(LocalPlayerJoinRequest request)
        {
            if (!IsReady)
            {
                return LocalPlayerJoinResult.RuntimeUnavailable(request, _diagnostic);
            }

            _requestCount++;
            LocalPlayerJoinResult result = _bridge.TryJoin(request);
            _diagnostic = result != null
                ? result.ToDiagnosticString()
                : "Local Player join returned no result.";
            return result ?? LocalPlayerJoinResult.RuntimeUnavailable(
                request,
                "Local Player provisioning bridge returned no result.");
        }

        internal SessionPlayerLeaveResult TryLeave(
            SessionPlayerLeaveRequest request)
        {
            if (!IsReady || _runtimeHost == null)
            {
                return SessionPlayerLeaveResult.RuntimeUnavailable(
                    request,
                    _diagnostic);
            }

            if (!SessionPlayerLeaveRuntimeHostModule.TryAttach(
                    _runtimeHost,
                    out SessionPlayerLeaveRuntimeHostModule leaveRuntime,
                    out string issue))
            {
                _diagnostic =
                    "Session Player Leave runtime could not be composed for the explicit request. " +
                    issue;
                return SessionPlayerLeaveResult.RuntimeUnavailable(
                    request,
                    _diagnostic);
            }

            _requestCount++;
            SessionPlayerLeaveResult result = leaveRuntime.TryLeave(request);
            _diagnostic = result != null
                ? result.ToDiagnosticString()
                : "Session Player Leave returned no result.";
            return result ?? SessionPlayerLeaveResult.RuntimeUnavailable(
                request,
                "Session Player Leave orchestration returned no result.");
        }

        internal LocalPlayerJoinResult RollbackCommittedJoin(
            LocalPlayerJoinResult joinResult,
            string reason)
        {
            return RollbackCommittedJoin(joinResult, reason, explicitCallerRollback: true);
        }

        internal LocalPlayerJoinResult RollbackCommittedJoin(
            LocalPlayerJoinResult joinResult,
            string reason,
            bool explicitCallerRollback)
        {
            const string source = nameof(LocalPlayerProvisioningRuntimeHostModule);
            if (_bridge == null)
            {
                return LocalPlayerJoinResult.RuntimeUnavailable(
                    joinResult != null ? joinResult.Request : default,
                    "Local Player provisioning bridge is unavailable for committed join rollback.");
            }

            if (joinResult == null || !joinResult.Succeeded ||
                !joinResult.Slot.PlayerSlotId.IsValid ||
                joinResult.LocalPlayerHost == null ||
                joinResult.PlayerInput == null)
            {
                return CreateRollbackFailure(
                    joinResult,
                    "Committed Local Player join rollback requires complete successful join and physical Host evidence.");
            }

            if (!_runtimeHost.TryGetPlayerActorPreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparation))
            {
                return CreateRollbackFailure(
                    joinResult,
                    "FrameworkRuntimeHost has no ready Player Actor preparation authority for committed join rollback.");
            }

            if (preparation.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot preparationSnapshot) &&
                TryGetActivePreparation(preparationSnapshot, joinResult.Slot.PlayerSlotId,
                    out PlayerActorPreparationSummary activePreparation))
            {
                return CreateRollbackFailure(
                    joinResult,
                    "Committed Local Player join rollback is blocked by active Player Actor preparation. " +
                    $"slot='{joinResult.Slot.PlayerSlotId.StableText}' preparation='{activePreparation.Token.StableText}'.");
            }

            if (_runtimeHost.TryGetPlayerGameplayRuntimeSnapshot(
                    out PlayerGameplayRuntimeHostSnapshot gameplaySnapshot) &&
                HasActiveGameplayState(gameplaySnapshot, joinResult.Slot.PlayerSlotId))
            {
                return CreateRollbackFailure(
                    joinResult,
                    "Committed Local Player join rollback is blocked by active Gameplay ownership, candidate or handoff state. " +
                    $"slot='{joinResult.Slot.PlayerSlotId.StableText}' " +
                    $"gameplay='{gameplaySnapshot.ToDiagnosticString()}'.");
            }

            bool retainedEvidence = preparation.TryGetRetainedHostEvidence(
                joinResult.Slot.PlayerSlotId,
                out _);
            PlayerHostEvidenceResult hostEvidenceRelease = retainedEvidence
                ? preparation.ReleaseSessionPhysicalHost(
                    joinResult.Slot.PlayerSlotId,
                    joinResult.LocalPlayerHost,
                    source,
                    reason)
                : null;
            if (hostEvidenceRelease != null && !hostEvidenceRelease.Succeeded)
            {
                return CreateRollbackFailure(
                    joinResult,
                    "Committed Local Player join rollback could not release exact Host evidence. " +
                    hostEvidenceRelease.ToDiagnosticString());
            }

            LocalPlayerJoinResult rollback = _bridge.RollbackCommittedJoin(
                joinResult,
                reason,
                explicitCallerRollback);
            bool bridgeSucceeded = rollback?.RollbackResult != null &&
                                  rollback.RollbackResult.Succeeded;
            if (bridgeSucceeded)
            {
                _diagnostic =
                    $"Committed Local Player join was rolled back explicitly. " +
                    $"slot='{joinResult.Slot.PlayerSlotId.StableText}' " +
                    $"hostEvidenceReleased='{(hostEvidenceRelease != null)}'.";
                return rollback;
            }

            PlayerHostEvidenceResult compensation = hostEvidenceRelease != null
                ? preparation.RegisterSessionPhysicalHost(
                    joinResult.Slot.PlayerSlotId,
                    joinResult.LocalPlayerHost,
                    source,
                    "rollback-bridge-failed-compensation")
                : null;
            return CreateRollbackFailure(
                joinResult,
                "Committed Local Player join rollback failed after Host evidence release. " +
                $"hostEvidenceReleased='{(hostEvidenceRelease != null)}' " +
                $"bridgeRollbackStatus='{rollback?.Status}' " +
                $"assignmentReleased='{rollback?.AssignmentRollbackResult?.Succeeded}' " +
                $"hostAdmissionReleased='{(rollback != null && rollback.RollbackResult != null)}' " +
                $"slotReleased='{rollback?.RollbackResult?.Succeeded}' " +
                $"compensationAttempted='{(compensation != null)}' " +
                $"compensationSucceeded='{(compensation?.Succeeded == true)}' " +
                $"bridge='{rollback?.ToDiagnosticString()}' compensation='{compensation?.ToDiagnosticString()}'.");
        }

        private static bool TryGetActivePreparation(
            PlayerActorPreparationRuntimeHostSnapshot snapshot,
            PlayerSlotId slotId,
            out PlayerActorPreparationSummary preparation)
        {
            preparation = default;
            PlayerActorPreparationSnapshot summaries = snapshot?.Preparation;
            if (summaries == null) return false;
            for (int index = 0; index < summaries.Slots.Count; index++)
            {
                PlayerActorPreparationSummary candidate = summaries.Slots[index];
                if (candidate.PlayerSlotId != slotId || !candidate.IsPrepared) continue;
                preparation = candidate;
                return true;
            }

            return false;
        }

        private static bool HasActiveGameplayState(
            PlayerGameplayRuntimeHostSnapshot snapshot,
            PlayerSlotId slotId)
        {
            bool admitted = snapshot?.Admission != null &&
                snapshot.Admission.TryGetSummary(
                    slotId,
                    out PlayerGameplayAdmissionSummary admission) &&
                admission.IsAdmitted;
            bool occupied = snapshot?.Occupancy != null &&
                snapshot.Occupancy.TryGetSummary(
                    slotId,
                    out PlayerGameplayOccupancySummary occupancy) &&
                occupancy.IsOccupied;
            bool inputBound = snapshot?.InputBinding != null &&
                snapshot.InputBinding.TryGetSummary(
                    slotId,
                    out PlayerGameplayInputBindingSummary input) &&
                input.IsBound;
            bool cameraEligible = snapshot?.CameraEligibility != null &&
                snapshot.CameraEligibility.TryGetSummary(
                    slotId,
                    out PlayerGameplayCameraEligibilitySummary camera) &&
                camera.IsEligible;
            return admitted || occupied || inputBound || cameraEligible;
        }

        private static LocalPlayerJoinResult CreateRollbackFailure(
            LocalPlayerJoinResult joinResult,
            string message)
        {
            return new LocalPlayerJoinResult(
                LocalPlayerJoinStatus.FailedRollback,
                joinResult != null ? joinResult.OperationId : default,
                joinResult != null ? joinResult.Request : default,
                joinResult?.ReservationResult,
                joinResult?.CommitResult,
                null,
                joinResult != null ? joinResult.Slot : default,
                joinResult?.PlayerInput,
                joinResult?.LocalPlayerHost,
                joinResult != null ? joinResult.UnityPlayerIndex : -1,
                joinResult != null
                    ? joinResult.CallbackConfirmation
                    : LocalPlayerJoinCallbackConfirmation.None,
                message,
                LocalPlayerJoinStatus.FailedRollback,
                joinResult?.AssignmentResult,
                null);
        }

        internal PlayerParticipationOperationResult TryOpenJoining(
            string source,
            string reason)
        {
            if (_participationContext == null)
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "OpenJoining",
                    source,
                    reason,
                    _diagnostic);
            }

            PlayerParticipationSnapshot before = _participationContext.CreateSnapshot();
            PlayerParticipationOperationResult result =
                _participationContext.TryOpenJoining(source, reason);
            if (!result.Completed || !result.Snapshot.JoiningOpen)
            {
                _diagnostic = result.ToDiagnosticString();
                return result;
            }

            PlayerInputManager manager = _authoring != null
                ? _authoring.PlayerInputManager
                : null;
            if (manager == null || !manager.isActiveAndEnabled)
            {
                PlayerParticipationOperationResult rollback =
                    _participationContext.TryCloseJoining(
                        nameof(LocalPlayerProvisioningRuntimeHostModule),
                        "technical-joining-gate-unavailable");
                PlayerParticipationSnapshot afterRollback = _participationContext.CreateSnapshot();
                string message = manager == null
                    ? "PlayerInputManager is missing after logical joining was opened."
                    : $"PlayerInputManager '{manager.name}' is not active and enabled after logical joining was opened.";
                var failed = new PlayerParticipationOperationResult(
                    PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                    "OpenJoining",
                    source.NormalizeTextOrFallback(nameof(LocalPlayerProvisioningRuntimeHostModule)),
                    reason.NormalizeTextOrFallback("technical-joining-gate-unavailable"),
                    message + " Logical joining rollback status='" +
                        (rollback != null ? rollback.Status.ToString() : "Missing") + "'.",
                    before.Revision,
                    afterRollback.Revision,
                    default,
                    default,
                    afterRollback);
                _diagnostic = failed.ToDiagnosticString();
                return failed;
            }

            manager.EnableJoining();
            if (!manager.joiningEnabled)
            {
                PlayerParticipationOperationResult rollback =
                    _participationContext.TryCloseJoining(
                        nameof(LocalPlayerProvisioningRuntimeHostModule),
                        "technical-joining-gate-enable-failed");
                PlayerParticipationSnapshot afterRollback = _participationContext.CreateSnapshot();
                var failed = new PlayerParticipationOperationResult(
                    PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                    "OpenJoining",
                    source.NormalizeTextOrFallback(nameof(LocalPlayerProvisioningRuntimeHostModule)),
                    reason.NormalizeTextOrFallback("technical-joining-gate-enable-failed"),
                    $"PlayerInputManager '{manager.name}' did not enable its technical joining gate. " +
                        "Logical joining rollback status='" +
                        (rollback != null ? rollback.Status.ToString() : "Missing") + "'.",
                    before.Revision,
                    afterRollback.Revision,
                    default,
                    default,
                    afterRollback);
                _diagnostic = failed.ToDiagnosticString();
                return failed;
            }

            _diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal PlayerParticipationOperationResult TryCloseJoining(
            string source,
            string reason)
        {
            if (_participationContext == null)
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "CloseJoining",
                    source,
                    reason,
                    _diagnostic);
            }

            PlayerInputManager manager = _authoring != null
                ? _authoring.PlayerInputManager
                : null;
            if (manager != null)
            {
                manager.DisableJoining();
                if (manager.joiningEnabled)
                {
                    PlayerParticipationSnapshot snapshot = _participationContext.CreateSnapshot();
                    var failed = new PlayerParticipationOperationResult(
                        PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                        "CloseJoining",
                        source.NormalizeTextOrFallback(nameof(LocalPlayerProvisioningRuntimeHostModule)),
                        reason.NormalizeTextOrFallback("technical-joining-gate-disable-failed"),
                        $"PlayerInputManager '{manager.name}' did not disable its technical joining gate. Logical joining remains unchanged.",
                        snapshot.Revision,
                        snapshot.Revision,
                        default,
                        default,
                        snapshot);
                    _diagnostic = failed.ToDiagnosticString();
                    return failed;
                }
            }

            PlayerParticipationOperationResult result =
                _participationContext.TryCloseJoining(source, reason);
            _diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal bool TryGetSnapshot(out PlayerParticipationSnapshot snapshot)
        {
            if (_participationContext == null)
            {
                snapshot = PlayerParticipationSnapshot.Empty(
                    PlayerParticipationOperationStatus.RejectedInvalidState,
                    _diagnostic);
                return false;
            }

            snapshot = _participationContext.CreateSnapshot();
            return true;
        }

        private void OnDestroy()
        {
            foreach (var pair in _consumerAccesses)
            {
                pair.Value.Dispose();
                if (pair.Key != null)
                {
                    pair.Key.Release(
                        "Local Player provisioning consumer binding was released because the Session provisioning runtime was disposed.");
                }
            }

            _consumerAccesses.Clear();
            if (_authoring != null && _authoring.PlayerInputManager != null)
            {
                _authoring.PlayerInputManager.DisableJoining();
            }

            _bridge?.Dispose();
            _bridge = null;

            if (_authoring != null)
            {
                _authoring.UnbindRuntime(this, "Session Local Player provisioning runtime was released.");
            }

            _authoring = null;
            _participationContext = null;
            _runtimeHost = null;
            _diagnostic = "Session Local Player provisioning runtime was released.";
        }
    }

    /// <summary>
    /// Per-consumer, per-lifetime forwarding endpoint. Its only mutable state
    /// is whether its Framework-owned binding is still current.
    /// </summary>
    internal sealed class LocalPlayerProvisioningConsumerAccess :
        ILocalPlayerProvisioningConsumerAccess,
        IDisposable
    {
        private readonly LocalPlayerProvisioningAuthoring _authoring;
        private readonly LocalPlayerProvisioningConsumerScope _scope;
        private readonly RuntimeContentOwner _owner;
        private readonly LocalPlayerProvisioningConsumerAccessBinding _binding;
        private readonly Func<RuntimeContentOwner, bool> _isCurrentScope;
        private string _diagnostic;
        private bool _disposed;

        internal LocalPlayerProvisioningConsumerAccess(
            LocalPlayerProvisioningAuthoring authoring,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            LocalPlayerProvisioningConsumerAccessBinding binding,
            Func<RuntimeContentOwner, bool> isCurrentScope)
        {
            this._authoring = authoring ??
                throw new ArgumentNullException(nameof(authoring));
            this._scope = scope;
            this._owner = owner;
            this._binding = binding ??
                throw new ArgumentNullException(nameof(binding));
            this._isCurrentScope = isCurrentScope ??
                throw new ArgumentNullException(nameof(isCurrentScope));
            _diagnostic = CreateReadyDiagnostic(owner);
        }

        public LocalPlayerProvisioningConsumerAccessSnapshot Snapshot
        {
            get
            {
                bool available = IsCurrent() && _authoring != null &&
                    _authoring.RuntimeReady;
                return new LocalPlayerProvisioningConsumerAccessSnapshot(
                    _scope,
                    _owner,
                    available,
                    _disposed,
                    available ? CreateReadyDiagnostic(_owner) : CurrentIssue);
            }
        }

        public bool TryGetObservation(
            out LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            if (TryGetAuthoring(out string issue))
            {
                if (_authoring.TryGetConsumerObservation(
                        _scope,
                        _owner,
                        out observation))
                {
                    return true;
                }

                issue = _authoring.RuntimeDiagnostic;
            }

            observation =
                LocalPlayerProvisioningConsumerObservationSnapshot.Unavailable(
                    _scope,
                    _owner,
                    issue);
            return false;
        }

        public PlayerParticipationOperationResult OpenJoining(
            string source,
            string reason)
        {
            return TryGetAuthoring(out string issue)
                ? _authoring.OpenJoining(source, reason)
                : PlayerParticipationOperationResult.RuntimeUnavailable(
                    "OpenJoining",
                    source,
                    reason,
                    issue);
        }

        public PlayerParticipationOperationResult CloseJoining(
            string source,
            string reason)
        {
            return TryGetAuthoring(out string issue)
                ? _authoring.CloseJoining(source, reason)
                : PlayerParticipationOperationResult.RuntimeUnavailable(
                    "CloseJoining",
                    source,
                    reason,
                    issue);
        }

        public LocalPlayerJoinResult RequestJoin(LocalPlayerJoinRequest request)
        {
            return TryGetAuthoring(out string issue)
                ? _authoring.RequestJoin(request)
                : LocalPlayerJoinResult.RuntimeUnavailable(request, issue);
        }

        public SessionPlayerLeaveResult RequestLeave(
            SessionPlayerLeaveRequest request)
        {
            return TryGetAuthoring(out string issue)
                ? _authoring.RequestLeave(request)
                : SessionPlayerLeaveResult.RuntimeUnavailable(request, issue);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _diagnostic =
                "Local Player provisioning consumer access was released because its framework scope was replaced or disposed.";
        }

        private bool TryGetAuthoring(out string issue)
        {
            if (!IsCurrent())
            {
                issue = CurrentIssue;
                return false;
            }

            if (_authoring == null || !_authoring.RuntimeReady)
            {
                issue = _authoring != null
                    ? _authoring.RuntimeDiagnostic
                    : "Local Player provisioning authority is unavailable.";
                _diagnostic = issue;
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

            if (_binding == null)
            {
                _disposed = true;
                _diagnostic =
                    "Local Player provisioning consumer access was released because its scene-local binding was destroyed.";
                return false;
            }

            if (!_isCurrentScope(_owner))
            {
                _disposed = true;
                _diagnostic =
                    "Local Player provisioning consumer access was released because its Route or Activity scope was replaced or disposed.";
                return false;
            }

            return true;
        }

        private string CurrentIssue => string.IsNullOrWhiteSpace(_diagnostic)
            ? "Local Player provisioning consumer access is unavailable."
            : _diagnostic;

        private static string CreateReadyDiagnostic(RuntimeContentOwner owner)
        {
            return
                $"Local Player provisioning consumer access is bound to '{owner.StableText}'.";
        }
    }

    /// <summary>
    /// Typed same-host access. A caller must already hold the FrameworkRuntimeHost reference;
    /// this does not introduce a static provisioning registry or service locator.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3G/P3J typed FrameworkRuntimeHost access to its local Player provisioning module.")]
    internal static class FrameworkRuntimeHostLocalPlayerProvisioningExtensions
    {
        internal static bool TryGetLocalPlayerProvisioningRuntime(
            this FrameworkRuntimeHost runtimeHost,
            out LocalPlayerProvisioningRuntimeHostModule module)
        {
            module = runtimeHost != null
                ? runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>()
                : null;
            return module != null && module.IsReady;
        }
    }
}
