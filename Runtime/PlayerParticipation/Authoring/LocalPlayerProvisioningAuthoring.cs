using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Designer-facing declaration and explicit product endpoint for the one
    /// Session-authorized local PlayerInputManager. The component never
    /// provisions a Player from Unity lifecycle callbacks; runtime operations
    /// occur only after Framework Core injects the Session module and a caller
    /// explicitly requests them.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Player/Provisioning/Authoring")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3G/P3J local Player provisioning authoring, preparation and lifecycle observation surface.")]
    public sealed class LocalPlayerProvisioningAuthoring : MonoBehaviour
    {
        [SerializeField]
        [Tooltip(
            "Explicit Session-authorized PlayerInputManager. Runtime code must not use PlayerInputManager.instance as a distributed lookup.")]
        private PlayerInputManager playerInputManager;

        [SerializeField]
        [Tooltip(
            "Technical Local Player Host prefab created by this provisioning authority when a ManagerProvisioned join is requested. This must contain PlayerInput and LocalPlayerHostAuthoring; it is not a Logical Actor prefab.")]
        private GameObject localPlayerHostPrefab;

        [NonSerialized]
        private LocalPlayerProvisioningRuntimeHostModule _runtimeModule;

        [NonSerialized]
        private string _runtimeDiagnostic =
            "Local Player provisioning runtime is not bound.";

        public PlayerInputManager PlayerInputManager =>
            playerInputManager;

        public bool HasPlayerInputManager =>
            playerInputManager != null;

        public bool UsesManualJoin =>
            playerInputManager != null &&
            playerInputManager.joinBehavior ==
                PlayerJoinBehavior.JoinPlayersManually;

        public bool UsesCSharpJoinNotifications =>
            playerInputManager != null &&
            playerInputManager.notificationBehavior ==
                PlayerNotifications.InvokeCSharpEvents;

        /// <summary>
        /// Explicit product authority for the technical Local Player Host
        /// created by a manual join.
        /// </summary>
        public GameObject LocalPlayerHostPrefab =>
            localPlayerHostPrefab;

        /// <summary>
        /// Compatibility alias for existing consumers. New code should use
        /// LocalPlayerHostPrefab.
        /// </summary>
        public GameObject PlayerPrefab =>
            LocalPlayerHostPrefab;

        public bool IsManagerPrefabMaterialized =>
            playerInputManager != null &&
            localPlayerHostPrefab != null &&
            playerInputManager.playerPrefab ==
                localPlayerHostPrefab;

        public bool HasManagerPrefabDivergence =>
            playerInputManager != null &&
            playerInputManager.playerPrefab != null &&
            localPlayerHostPrefab != null &&
            playerInputManager.playerPrefab !=
                localPlayerHostPrefab;

        public bool RuntimeReady =>
            _runtimeModule != null &&
            _runtimeModule.IsReadyFor(this);

        public string RuntimeDiagnostic =>
            RuntimeReady
                ? _runtimeModule.Diagnostic
                : _runtimeDiagnostic;

        public LocalPlayerJoinResult LastJoinResult =>
            RuntimeReady
                ? _runtimeModule.LastJoinResult
                : null;

        public PlayerParticipationSnapshot RuntimeSnapshot
        {
            get
            {
                if (RuntimeReady &&
                    _runtimeModule.TryGetSnapshot(
                        out PlayerParticipationSnapshot snapshot))
                {
                    return snapshot;
                }

                return PlayerParticipationSnapshot.Empty(
                    PlayerParticipationOperationStatus
                        .RejectedInvalidState,
                    RuntimeDiagnostic);
            }
        }

        /// <summary>
        /// Current read-only Manager-Provisioned Player lifecycle evidence.
        /// Reading this property does not mutate or advance runtime state.
        /// </summary>
        public ManagerProvisionedPlayerLifecycleSnapshot
            ManagerProvisionedLifecycleSnapshot
        {
            get
            {
                TryGetManagerProvisionedLifecycleSnapshot(
                    out ManagerProvisionedPlayerLifecycleSnapshot
                        snapshot);
                return snapshot;
            }
        }

        /// <summary>
        /// Reads the consolidated Player lifecycle projection from the
        /// explicitly bound Session runtime module.
        /// </summary>
        public bool TryGetManagerProvisionedLifecycleSnapshot(
            out ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            if (RuntimeReady &&
                _runtimeModule.TryGetLifecycleSnapshot(out snapshot))
            {
                return true;
            }

            snapshot =
                ManagerProvisionedPlayerLifecycleSnapshot.Unavailable(
                    RuntimeDiagnostic);
            return false;
        }

        internal bool TryGetConsumerObservation(
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner scopeOwner,
            out LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            if (RuntimeReady && _runtimeModule.TryGetObservation(
                    scope,
                    scopeOwner,
                    out observation))
            {
                return true;
            }

            observation =
                LocalPlayerProvisioningConsumerObservationSnapshot.Unavailable(
                    scope,
                    scopeOwner,
                    RuntimeDiagnostic);
            return false;
        }

        /// <summary>
        /// Explicitly opens Session local joining. This never runs
        /// automatically from a Unity lifecycle callback.
        /// </summary>
        public PlayerParticipationOperationResult OpenJoining(
            string source,
            string reason)
        {
            return RuntimeReady
                ? _runtimeModule.TryOpenJoining(source, reason)
                : PlayerParticipationOperationResult
                    .RuntimeUnavailable(
                        "OpenJoining",
                        source,
                        reason,
                        RuntimeDiagnostic);
        }

        /// <summary>
        /// Explicitly closes Session local joining without removing already
        /// joined Players.
        /// </summary>
        public PlayerParticipationOperationResult CloseJoining(
            string source,
            string reason)
        {
            return RuntimeReady
                ? _runtimeModule.TryCloseJoining(source, reason)
                : PlayerParticipationOperationResult
                    .RuntimeUnavailable(
                        "CloseJoining",
                        source,
                        reason,
                        RuntimeDiagnostic);
        }

        /// <summary>
        /// Executes one explicitly authorized local Player join against the
        /// Session runtime. A successful result is registered with the
        /// host-scoped Actor preparation authority before this endpoint
        /// returns.
        /// </summary>
        public LocalPlayerJoinResult RequestJoin(
            LocalPlayerJoinRequest request)
        {
            if (!RuntimeReady)
            {
                return LocalPlayerJoinResult.RuntimeUnavailable(
                    request,
                    RuntimeDiagnostic);
            }

            LocalPlayerJoinResult result =
                _runtimeModule.TryJoin(request);
            return _runtimeModule
                .RegisterJoinWithActorPreparation(result);
        }

        public LocalPlayerJoinResult RequestJoin(
            string source,
            string reason)
        {
            return RequestJoin(
                new LocalPlayerJoinRequest(source, reason));
        }

        /// <summary>
        /// Executes one exact Session Player Leave request through the canonical
        /// host-scoped ADR-020 orchestration authority. The request must already
        /// carry the target Slot and joined occurrence revision.
        /// </summary>
        public SessionPlayerLeaveResult RequestLeave(
            SessionPlayerLeaveRequest request)
        {
            return RuntimeReady
                ? _runtimeModule.TryLeave(request)
                : SessionPlayerLeaveResult.RuntimeUnavailable(
                    request,
                    RuntimeDiagnostic);
        }

        internal PlayerActorSelectionResult RequestSelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            return RuntimeReady
                ? _runtimeModule.TrySelectActorProfile(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectActorProfile",
                    request,
                    RuntimeDiagnostic);
        }

        internal PlayerActorSelectionResult RequestSelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            return RuntimeReady
                ? _runtimeModule.TrySelectDefaultActor(
                    playerSlotId,
                    expectedSelectionRevision,
                    source,
                    reason)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectDefaultActor",
                    new PlayerActorSelectionRequest(
                        playerSlotId,
                        null,
                        source,
                        reason,
                        expectedSelectionRevision),
                    RuntimeDiagnostic);
        }

        internal PlayerActorSelectionResult RequestReplaceActorSelection(
            PlayerActorSelectionRequest request)
        {
            return RuntimeReady
                ? _runtimeModule.TryReplaceActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ReplaceActorSelection",
                    request,
                    RuntimeDiagnostic);
        }

        internal PlayerActorSelectionResult RequestClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            return RuntimeReady
                ? _runtimeModule.TryClearActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ClearActorSelection",
                    request,
                    RuntimeDiagnostic);
        }

        internal void BindRuntime(
            LocalPlayerProvisioningRuntimeHostModule module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (_runtimeModule != null &&
                !ReferenceEquals(_runtimeModule, module))
            {
                throw new InvalidOperationException(
                    "Local Player provisioning authoring is already bound to another Session runtime module.");
            }

            module.RegisterActivityPlayerActorLifecycleSource();
            module
                .RegisterSceneLocalPlayerAdmissionLifecycleSourceIfAvailable();
            _runtimeModule = module;
            _runtimeDiagnostic = module.Diagnostic;
        }

        internal bool TryMaterializeManagerPrefab(
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (playerInputManager == null)
            {
                diagnostic =
                    "Local Player provisioning authoring has no explicit PlayerInputManager.";
                return false;
            }

            if (localPlayerHostPrefab == null)
            {
                diagnostic =
                    "Local Player Host Prefab is required on LocalPlayerProvisioningAuthoring.";
                return false;
            }

            GameObject managerPrefab =
                playerInputManager.playerPrefab;
            if (managerPrefab == null)
            {
                playerInputManager.playerPrefab =
                    localPlayerHostPrefab;
                diagnostic =
                    $"Local Player Host Prefab '{localPlayerHostPrefab.name}' was materialized on PlayerInputManager '{playerInputManager.name}'.";
                return true;
            }

            if (managerPrefab != localPlayerHostPrefab)
            {
                diagnostic =
                    $"PlayerInputManager '{playerInputManager.name}' has divergent Player Prefab '{managerPrefab.name}'. " +
                    $"Expected authored Local Player Host Prefab '{localPlayerHostPrefab.name}'.";
                return false;
            }

            diagnostic =
                $"Local Player Host Prefab '{localPlayerHostPrefab.name}' is already materialized on PlayerInputManager '{playerInputManager.name}'.";
            return true;
        }

        internal void ReportRuntimeInitializationFailure(
            string diagnostic)
        {
            if (RuntimeReady)
            {
                return;
            }

            _runtimeDiagnostic =
                string.IsNullOrWhiteSpace(diagnostic)
                    ? "Local Player provisioning runtime initialization failed without a diagnostic."
                    : diagnostic.Trim();
        }

        internal void UnbindRuntime(
            LocalPlayerProvisioningRuntimeHostModule module,
            string diagnostic)
        {
            if (_runtimeModule != null &&
                ReferenceEquals(_runtimeModule, module))
            {
                _runtimeModule = null;
            }

            _runtimeDiagnostic =
                string.IsNullOrWhiteSpace(diagnostic)
                    ? "Local Player provisioning runtime is not bound."
                    : diagnostic.Trim();
        }
    }
}
