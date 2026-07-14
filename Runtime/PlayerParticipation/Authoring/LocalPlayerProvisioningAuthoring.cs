using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Designer-facing declaration and explicit product endpoint for the one Session-authorized
    /// local PlayerInputManager. The component never provisions a Player from Unity lifecycle
    /// callbacks; runtime operations occur only after Framework Core injects the Session module
    /// and a caller explicitly requests them.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Local Player Provisioning Authoring")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3G/P3J local Player provisioning authoring and preparation-aware runtime request surface.")]
    public sealed class LocalPlayerProvisioningAuthoring : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit Session-authorized PlayerInputManager. Runtime code must not use PlayerInputManager.instance as a distributed lookup.")]
        private PlayerInputManager playerInputManager;

        [NonSerialized]
        private LocalPlayerProvisioningRuntimeHostModule runtimeModule;

        [NonSerialized]
        private string runtimeDiagnostic = "Local Player provisioning runtime is not bound.";

        public PlayerInputManager PlayerInputManager => playerInputManager;

        public bool HasPlayerInputManager => playerInputManager != null;

        public bool UsesManualJoin =>
            playerInputManager != null &&
            playerInputManager.joinBehavior == PlayerJoinBehavior.JoinPlayersManually;

        public bool UsesCSharpJoinNotifications =>
            playerInputManager != null &&
            playerInputManager.notificationBehavior == PlayerNotifications.InvokeCSharpEvents;

        public GameObject PlayerPrefab =>
            playerInputManager != null ? playerInputManager.playerPrefab : null;

        public int TechnicalMaxPlayerCount =>
            playerInputManager != null ? playerInputManager.maxPlayerCount : 0;

        public bool RuntimeReady =>
            runtimeModule != null && runtimeModule.IsReadyFor(this);

        public string RuntimeDiagnostic => RuntimeReady
            ? runtimeModule.Diagnostic
            : runtimeDiagnostic;

        public LocalPlayerJoinResult LastJoinResult => RuntimeReady
            ? runtimeModule.LastJoinResult
            : null;

        public PlayerParticipationSnapshot RuntimeSnapshot
        {
            get
            {
                if (RuntimeReady && runtimeModule.TryGetSnapshot(out PlayerParticipationSnapshot snapshot))
                {
                    return snapshot;
                }

                return PlayerParticipationSnapshot.Empty(
                    PlayerParticipationOperationStatus.RejectedInvalidState,
                    RuntimeDiagnostic);
            }
        }

        /// <summary>
        /// Explicitly opens Session local joining. This never runs automatically from a Unity
        /// lifecycle callback.
        /// </summary>
        public PlayerParticipationOperationResult OpenJoining(string source, string reason)
        {
            return RuntimeReady
                ? runtimeModule.TryOpenJoining(source, reason)
                : PlayerParticipationOperationResult.RuntimeUnavailable(
                    "OpenJoining",
                    source,
                    reason,
                    RuntimeDiagnostic);
        }

        /// <summary>
        /// Explicitly closes Session local joining without removing already joined Players.
        /// </summary>
        public PlayerParticipationOperationResult CloseJoining(string source, string reason)
        {
            return RuntimeReady
                ? runtimeModule.TryCloseJoining(source, reason)
                : PlayerParticipationOperationResult.RuntimeUnavailable(
                    "CloseJoining",
                    source,
                    reason,
                    RuntimeDiagnostic);
        }

        /// <summary>
        /// Changes current Session join capacity without evicting existing participation.
        /// </summary>
        public PlayerParticipationOperationResult SetDynamicCapacity(
            int requestedCapacity,
            string source,
            string reason)
        {
            return RuntimeReady
                ? runtimeModule.TrySetDynamicCapacity(requestedCapacity, source, reason)
                : PlayerParticipationOperationResult.RuntimeUnavailable(
                    "SetDynamicCapacity",
                    source,
                    reason,
                    RuntimeDiagnostic);
        }

        /// <summary>
        /// Executes one explicitly authorized local Player join against the Session runtime.
        /// A successful result is also registered with the host-scoped Actor preparation authority
        /// before this public product endpoint returns.
        /// </summary>
        public LocalPlayerJoinResult RequestJoin(LocalPlayerJoinRequest request)
        {
            if (!RuntimeReady)
            {
                return LocalPlayerJoinResult.RuntimeUnavailable(request, RuntimeDiagnostic);
            }

            LocalPlayerJoinResult result = runtimeModule.TryJoin(request);
            return runtimeModule.RegisterJoinWithActorPreparation(result);
        }

        public LocalPlayerJoinResult RequestJoin(string source, string reason)
        {
            return RequestJoin(new LocalPlayerJoinRequest(source, reason));
        }

        internal void BindRuntime(LocalPlayerProvisioningRuntimeHostModule module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (runtimeModule != null && !ReferenceEquals(runtimeModule, module))
            {
                throw new InvalidOperationException(
                    "Local Player provisioning authoring is already bound to another Session runtime module.");
            }

            module.RegisterActivityPlayerActorLifecycleSource();
            runtimeModule = module;
            runtimeDiagnostic = module.Diagnostic;
        }

        internal void UnbindRuntime(
            LocalPlayerProvisioningRuntimeHostModule module,
            string diagnostic)
        {
            if (runtimeModule != null && ReferenceEquals(runtimeModule, module))
            {
                runtimeModule = null;
            }

            runtimeDiagnostic = string.IsNullOrWhiteSpace(diagnostic)
                ? "Local Player provisioning runtime is not bound."
                : diagnostic.Trim();
        }
    }
}
