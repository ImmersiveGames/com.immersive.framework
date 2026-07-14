using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
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
    internal sealed class LocalPlayerProvisioningRuntimeHostModule : MonoBehaviour
    {
        private FrameworkRuntimeHost runtimeHost;
        private PlayerParticipationRuntimeContext participationContext;
        private LocalPlayerProvisioningAuthoring authoring;
        private LocalPlayerProvisioningBridge bridge;
        private string diagnostic = "Local Player provisioning runtime is not initialized.";
        private int requestCount;

        internal bool IsReady =>
            runtimeHost != null &&
            participationContext != null &&
            authoring != null &&
            bridge != null;

        internal string Diagnostic => diagnostic;
        internal LocalPlayerProvisioningAuthoring Authoring => authoring;
        internal LocalPlayerJoinResult LastJoinResult => bridge?.LastResult;
        internal int RequestCount => requestCount;

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

            return module.TryInitialize(runtimeHost, authoring, out issue);
        }

        internal bool TryInitialize(
            FrameworkRuntimeHost targetRuntimeHost,
            LocalPlayerProvisioningAuthoring targetAuthoring,
            out string issue)
        {
            issue = string.Empty;

            if (IsReady)
            {
                if (ReferenceEquals(runtimeHost, targetRuntimeHost) &&
                    ReferenceEquals(authoring, targetAuthoring))
                {
                    return true;
                }

                issue = "Local Player provisioning runtime is already bound to another authoring surface.";
                return false;
            }

            if (targetRuntimeHost == null)
            {
                issue = "FrameworkRuntimeHost is missing.";
                diagnostic = issue;
                return false;
            }

            if (targetAuthoring == null)
            {
                issue = "Local Player provisioning authoring is missing.";
                diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext targetParticipationContext))
            {
                issue = "FrameworkRuntimeHost has no initialized Session Player participation context.";
                diagnostic = issue;
                return false;
            }

            if (!TryValidateRuntimeConfiguration(
                    targetAuthoring,
                    targetParticipationContext,
                    out issue))
            {
                diagnostic = issue;
                return false;
            }

            PlayerInputManager targetManager = targetAuthoring.PlayerInputManager;
            targetManager.DisableJoining();
            if (targetManager.joiningEnabled)
            {
                issue =
                    $"PlayerInputManager '{targetManager.name}' did not close its technical joining gate during Session initialization.";
                diagnostic = issue;
                return false;
            }

            if (!LocalPlayerProvisioningBridge.TryCreate(
                    targetParticipationContext,
                    targetAuthoring,
                    out LocalPlayerProvisioningBridge targetBridge,
                    out issue))
            {
                diagnostic = issue;
                return false;
            }

            runtimeHost = targetRuntimeHost;
            participationContext = targetParticipationContext;
            authoring = targetAuthoring;
            bridge = targetBridge;
            requestCount = 0;
            diagnostic =
                $"Local Player provisioning runtime is ready. manager='{authoring.PlayerInputManager.name}' hostPrefab='{authoring.PlayerPrefab.name}'.";

            try
            {
                authoring.BindRuntime(this);
            }
            catch (Exception exception)
            {
                bridge.Dispose();
                bridge = null;
                authoring = null;
                participationContext = null;
                runtimeHost = null;
                diagnostic =
                    $"Local Player provisioning authoring rejected Session runtime binding. {exception.Message}";
                issue = diagnostic;
                return false;
            }

            return true;
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

            GameObject playerPrefab = targetAuthoring.PlayerPrefab;
            if (playerPrefab == null)
            {
                issue = $"PlayerInputManager '{manager.name}' has no Player Prefab.";
                return false;
            }

            PlayerInput prefabPlayerInput = playerPrefab.GetComponent<PlayerInput>();
            if (prefabPlayerInput == null)
            {
                issue = $"Player Prefab '{playerPrefab.name}' has no PlayerInput component.";
                return false;
            }

            LocalPlayerHostAuthoring prefabHost =
                playerPrefab.GetComponent<LocalPlayerHostAuthoring>();
            if (prefabHost == null)
            {
                issue = $"Player Prefab '{playerPrefab.name}' has no LocalPlayerHostAuthoring.";
                return false;
            }

            if (!ReferenceEquals(prefabHost.PlayerInput, prefabPlayerInput))
            {
                issue =
                    $"LocalPlayerHostAuthoring on prefab '{playerPrefab.name}' does not resolve the prefab PlayerInput.";
                return false;
            }

            if (!prefabHost.TryValidateConfiguration(out string hostIssue))
            {
                issue = $"Player Prefab '{playerPrefab.name}' has an invalid Local Player Host. {hostIssue}";
                return false;
            }

            PlayerParticipationSnapshot snapshot = targetParticipationContext.CreateSnapshot();
            int technicalMax = targetAuthoring.TechnicalMaxPlayerCount;
            if (technicalMax > 0 && snapshot.ConfiguredSlotCount > technicalMax)
            {
                issue =
                    $"Session configures '{snapshot.ConfiguredSlotCount}' Player Slots, but PlayerInputManager technical max is '{technicalMax}'.";
                return false;
            }

            return true;
        }

        internal bool IsReadyFor(LocalPlayerProvisioningAuthoring targetAuthoring)
        {
            return IsReady && ReferenceEquals(authoring, targetAuthoring);
        }

        internal LocalPlayerJoinResult TryJoin(LocalPlayerJoinRequest request)
        {
            if (!IsReady)
            {
                return LocalPlayerJoinResult.RuntimeUnavailable(request, diagnostic);
            }

            requestCount++;
            LocalPlayerJoinResult result = bridge.TryJoin(request);
            diagnostic = result != null
                ? result.ToDiagnosticString()
                : "Local Player join returned no result.";
            return result ?? LocalPlayerJoinResult.RuntimeUnavailable(
                request,
                "Local Player provisioning bridge returned no result.");
        }

        internal PlayerParticipationOperationResult TryOpenJoining(
            string source,
            string reason)
        {
            if (participationContext == null)
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "OpenJoining",
                    source,
                    reason,
                    diagnostic);
            }

            PlayerParticipationSnapshot before = participationContext.CreateSnapshot();
            PlayerParticipationOperationResult result =
                participationContext.TryOpenJoining(source, reason);
            if (!result.Completed || !result.Snapshot.JoiningOpen)
            {
                diagnostic = result.ToDiagnosticString();
                return result;
            }

            PlayerInputManager manager = authoring != null
                ? authoring.PlayerInputManager
                : null;
            if (manager == null || !manager.isActiveAndEnabled)
            {
                PlayerParticipationOperationResult rollback =
                    participationContext.TryCloseJoining(
                        nameof(LocalPlayerProvisioningRuntimeHostModule),
                        "technical-joining-gate-unavailable");
                PlayerParticipationSnapshot afterRollback = participationContext.CreateSnapshot();
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
                diagnostic = failed.ToDiagnosticString();
                return failed;
            }

            manager.EnableJoining();
            if (!manager.joiningEnabled)
            {
                PlayerParticipationOperationResult rollback =
                    participationContext.TryCloseJoining(
                        nameof(LocalPlayerProvisioningRuntimeHostModule),
                        "technical-joining-gate-enable-failed");
                PlayerParticipationSnapshot afterRollback = participationContext.CreateSnapshot();
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
                diagnostic = failed.ToDiagnosticString();
                return failed;
            }

            diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal PlayerParticipationOperationResult TryCloseJoining(
            string source,
            string reason)
        {
            if (participationContext == null)
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "CloseJoining",
                    source,
                    reason,
                    diagnostic);
            }

            PlayerInputManager manager = authoring != null
                ? authoring.PlayerInputManager
                : null;
            if (manager != null)
            {
                manager.DisableJoining();
                if (manager.joiningEnabled)
                {
                    PlayerParticipationSnapshot snapshot = participationContext.CreateSnapshot();
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
                    diagnostic = failed.ToDiagnosticString();
                    return failed;
                }
            }

            PlayerParticipationOperationResult result =
                participationContext.TryCloseJoining(source, reason);
            diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal PlayerParticipationOperationResult TrySetDynamicCapacity(
            int requestedCapacity,
            string source,
            string reason)
        {
            if (participationContext == null)
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "SetDynamicCapacity",
                    source,
                    reason,
                    diagnostic);
            }

            if (authoring != null &&
                authoring.TechnicalMaxPlayerCount > 0 &&
                requestedCapacity > authoring.TechnicalMaxPlayerCount)
            {
                PlayerParticipationSnapshot snapshot = participationContext.CreateSnapshot();
                string message =
                    $"Requested capacity '{requestedCapacity}' exceeds PlayerInputManager technical max player count '{authoring.TechnicalMaxPlayerCount}'.";
                var rejected = new PlayerParticipationOperationResult(
                    PlayerParticipationOperationStatus.RejectedInvalidRequest,
                    "SetDynamicCapacity",
                    source.NormalizeTextOrFallback(nameof(LocalPlayerProvisioningRuntimeHostModule)),
                    reason.NormalizeTextOrFallback("technical-capacity-exceeded"),
                    message,
                    snapshot.Revision,
                    snapshot.Revision,
                    default,
                    default,
                    snapshot);
                diagnostic = rejected.ToDiagnosticString();
                return rejected;
            }

            PlayerParticipationOperationResult result = participationContext.TrySetDynamicCapacity(
                requestedCapacity,
                source,
                reason);
            diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal bool TryGetSnapshot(out PlayerParticipationSnapshot snapshot)
        {
            if (participationContext == null)
            {
                snapshot = PlayerParticipationSnapshot.Empty(
                    PlayerParticipationOperationStatus.RejectedInvalidState,
                    diagnostic);
                return false;
            }

            snapshot = participationContext.CreateSnapshot();
            return true;
        }

        private void OnDestroy()
        {
            if (authoring != null && authoring.PlayerInputManager != null)
            {
                authoring.PlayerInputManager.DisableJoining();
            }

            bridge?.Dispose();
            bridge = null;

            if (authoring != null)
            {
                authoring.UnbindRuntime(this, "Session Local Player provisioning runtime was released.");
            }

            authoring = null;
            participationContext = null;
            runtimeHost = null;
            diagnostic = "Session Local Player provisioning runtime was released.";
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
