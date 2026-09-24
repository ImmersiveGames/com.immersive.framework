using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Narrow Session integration that publishes an explicitly bound Framework Camera
    /// to the exact PlayerInput owned by current physical Player Host evidence.
    /// It owns no Player Session, Camera Output, Subject, arbitration or layout geometry.
    /// For automatic split-screen it brackets Manager-Provisioned joins so the
    /// PlayerInputManager performs its own recomposition only after the exact Camera
    /// association exists.
    /// </summary>
    internal sealed class PlayerCameraOutputIntegrationRuntime : IDisposable
    {
        private readonly struct AppliedBinding
        {
            internal AppliedBinding(
                PlayerInput playerInput,
                UnityEngine.Camera camera)
            {
                PlayerInput = playerInput;
                Camera = camera;
            }

            internal PlayerInput PlayerInput { get; }
            internal UnityEngine.Camera Camera { get; }
        }

        private readonly PlayerParticipationRuntimeContext _playerSession;
        private readonly PlayerActorPreparationRuntimeHostModule _physicalPlayers;
        private readonly PlayerInputManager _splitScreenManager;
        private readonly CameraOutputSessionTopology _outputs;
        private readonly PlayerCameraOutputTopology _bindings;
        private readonly Dictionary<PlayerSlotId, AppliedBinding> _applied = new();
        private readonly bool _automaticSplitScreenConfigured;
        private PlayerSlotId _splitScreenJoinSlot;
        private bool _splitScreenJoinTransactionActive;
        private bool _disposed;

        private PlayerCameraOutputIntegrationRuntime(
            PlayerParticipationRuntimeContext playerSession,
            PlayerActorPreparationRuntimeHostModule physicalPlayers,
            PlayerInputManager splitScreenManager,
            CameraOutputSessionTopology outputs,
            PlayerCameraOutputTopology bindings)
        {
            _playerSession = playerSession;
            _physicalPlayers = physicalPlayers;
            _splitScreenManager = splitScreenManager;
            _outputs = outputs;
            _bindings = bindings;
            _automaticSplitScreenConfigured =
                splitScreenManager != null &&
                splitScreenManager.splitScreen;
            _playerSession.Changed += OnPlayerSessionChanged;
            _physicalPlayers.SessionPhysicalHostChanged +=
                OnSessionPhysicalHostChanged;
        }

        internal bool LastReconciliationSucceeded { get; private set; }
        internal string Diagnostic { get; private set; }

        internal static bool TryCreate(
            PlayerParticipationRuntimeContext playerSession,
            PlayerActorPreparationRuntimeHostModule physicalPlayers,
            PlayerInputManager splitScreenManager,
            CameraOutputSessionTopology outputs,
            PlayerCameraOutputTopology bindings,
            out PlayerCameraOutputIntegrationRuntime runtime,
            out string diagnostic)
        {
            runtime = null;
            if (playerSession == null || physicalPlayers == null ||
                outputs == null || bindings == null)
            {
                diagnostic =
                    "Player Camera Output integration requires the current Player Session, physical Player evidence, Camera Output topology and explicit binding topology.";
                return false;
            }

            var candidate = new PlayerCameraOutputIntegrationRuntime(
                playerSession,
                physicalPlayers,
                splitScreenManager,
                outputs,
                bindings);
            if (!candidate.ReconcileAll(out diagnostic))
            {
                candidate.Dispose();
                return false;
            }

            candidate.RefreshPhysicalParticipation();

            candidate.LastReconciliationSucceeded = true;
            candidate.Diagnostic =
                $"Player Camera Output integration is ready with '{bindings.BindingCount}' explicit binding(s).";
            runtime = candidate;
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _splitScreenJoinTransactionActive = false;
            _splitScreenJoinSlot = default;
            _playerSession.Changed -= OnPlayerSessionChanged;
            _physicalPlayers.SessionPhysicalHostChanged -=
                OnSessionPhysicalHostChanged;

            var slots = new List<PlayerSlotId>(_applied.Keys);
            for (int index = 0; index < slots.Count; index++)
            {
                ReleaseApplied(slots[index]);
            }

            for (int index = 0;
                 index < _bindings.Bindings.Count;
                 index++)
            {
                SetOutputPhysicalParticipation(
                    _bindings.Bindings[index],
                    false);
            }

            if (_automaticSplitScreenConfigured &&
                _splitScreenManager != null &&
                _splitScreenManager.splitScreen)
            {
                _splitScreenManager.splitScreen = false;
            }

            LastReconciliationSucceeded = true;
            Diagnostic = "Player Camera Output integration was released.";
        }

        private void OnPlayerSessionChanged(PlayerSessionChange change)
        {
            if (_disposed || change == null ||
                change.Kind != PlayerSessionChangeKind.SlotAllocationChanged)
            {
                return;
            }

            if (IsManagerProvisionedReservationStarted(change))
            {
                BeginSplitScreenJoinTransaction(change.PlayerSlotId);
            }

            ReconcileObservedSlot(change.PlayerSlotId);

            if (_splitScreenJoinTransactionActive &&
                change.PlayerSlotId == _splitScreenJoinSlot &&
                change.CurrentSlot.AllocationState !=
                    PlayerSlotAllocationState.Reserved &&
                change.CurrentSlot.AllocationState !=
                    PlayerSlotAllocationState.Joined)
            {
                CompleteSplitScreenJoinTransaction();
            }
            else
            {
                RefreshPhysicalParticipation();
            }
        }

        private void OnSessionPhysicalHostChanged(PlayerSlotId playerSlotId)
        {
            if (_disposed)
            {
                return;
            }

            ReconcileObservedSlot(playerSlotId);
            if (_splitScreenJoinTransactionActive &&
                playerSlotId == _splitScreenJoinSlot)
            {
                CompleteSplitScreenJoinTransaction();
            }
            else
            {
                RefreshPhysicalParticipation();
            }
        }

        private bool IsManagerProvisionedReservationStarted(
            PlayerSessionChange change) =>
            change.CurrentSlot.IsValid &&
            _playerSession.TryGetHostProvisioningMode(
                change.PlayerSlotId,
                out PlayerHostProvisioningMode provisioningMode) &&
            provisioningMode == PlayerHostProvisioningMode.ManagerProvisioned &&
            change.CurrentSlot.AllocationState ==
                PlayerSlotAllocationState.Reserved &&
            change.PreviousSlot.AllocationState !=
                PlayerSlotAllocationState.Reserved;

        private void BeginSplitScreenJoinTransaction(PlayerSlotId playerSlotId)
        {
            if (!_automaticSplitScreenConfigured ||
                _splitScreenManager == null)
            {
                return;
            }

            if (_splitScreenJoinTransactionActive)
            {
                LastReconciliationSucceeded = false;
                Diagnostic =
                    $"PlayerInput split-screen join transaction for Slot '{_splitScreenJoinSlot.StableText}' was still active when Slot '{playerSlotId.StableText}' was reserved.";
                return;
            }

            _splitScreenJoinSlot = playerSlotId;
            _splitScreenJoinTransactionActive = true;
            if (_splitScreenManager.splitScreen)
            {
                _splitScreenManager.splitScreen = false;
            }
        }

        private void CompleteSplitScreenJoinTransaction()
        {
            if (!_splitScreenJoinTransactionActive)
            {
                return;
            }

            _splitScreenJoinTransactionActive = false;
            _splitScreenJoinSlot = default;
            RefreshPhysicalParticipation();
        }

        private void ReconcileObservedSlot(PlayerSlotId playerSlotId)
        {
            try
            {
                LastReconciliationSucceeded = Reconcile(playerSlotId, out string issue);
                Diagnostic = LastReconciliationSucceeded
                    ? $"Player Camera Output is current for Slot '{playerSlotId.StableText}'."
                    : issue;
            }
            catch (Exception exception)
            {
                LastReconciliationSucceeded = false;
                Diagnostic =
                    $"Player Camera Output reconciliation failed for Slot '{playerSlotId.StableText}'. {exception.Message}";
            }
        }

        private bool ReconcileAll(out string diagnostic)
        {
            PlayerParticipationSnapshot snapshot = _playerSession.CreateSnapshot();
            if (snapshot == null || !snapshot.IsInitialized)
            {
                diagnostic =
                    "Player Camera Output reconciliation requires an initialized Player Session snapshot.";
                return false;
            }

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotId playerSlotId = snapshot.Slots[index].PlayerSlotId;
                if (!Reconcile(playerSlotId, out diagnostic))
                {
                    return false;
                }
            }

            diagnostic = string.Empty;
            return true;
        }

        private bool Reconcile(PlayerSlotId playerSlotId, out string issue)
        {
            issue = string.Empty;
            if (_disposed || !playerSlotId.IsValid)
            {
                issue =
                    "Player Camera Output reconciliation requires an active integration and valid Player Slot identity.";
                return false;
            }

            if (!_bindings.TryGetBinding(playerSlotId, out PlayerCameraOutputBinding binding))
            {
                ReleaseApplied(playerSlotId);
                return true;
            }

            if (!_playerSession.TryGetSlotSnapshot(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot) ||
                !slot.IsJoined)
            {
                ReleaseApplied(playerSlotId);
                SetOutputPhysicalParticipation(binding, false);
                return true;
            }

            if (!_physicalPlayers.TryGetCurrentSessionPhysicalHost(
                    playerSlotId,
                    out LocalPlayerHostAuthoring host,
                    out _))
            {
                // Slot allocation commits before physical Host evidence registration.
                // The dedicated evidence event reconciles the same Slot immediately
                // after registration; no index/order/hierarchy fallback is allowed.
                ReleaseApplied(playerSlotId);
                SetOutputPhysicalParticipation(binding, false);
                return true;
            }

            PlayerInput playerInput = host.PlayerInput;
            if (playerInput == null ||
                !host.IsJoined ||
                !host.HasJoinedSlot ||
                host.JoinedPlayerSlotId != playerSlotId)
            {
                ReleaseApplied(playerSlotId);
                SetOutputPhysicalParticipation(binding, false);
                issue =
                    $"Current Session physical Host for Slot '{playerSlotId.StableText}' has no exact valid PlayerInput evidence.";
                return false;
            }

            if (!_outputs.TryGetOutput(
                    binding.OutputId,
                    out CameraOutputAuthoring output,
                    out string outputIssue))
            {
                ReleaseApplied(playerSlotId);
                issue = outputIssue;
                return false;
            }

            UnityEngine.Camera resolvedCamera = output.UnityCamera;
            if (resolvedCamera == null)
            {
                ReleaseApplied(playerSlotId);
                SetOutputPhysicalParticipation(binding, false);
                issue =
                    $"Camera Output '{binding.OutputId}' has no explicit Unity Camera.";
                return false;
            }

            if (_applied.TryGetValue(playerSlotId, out AppliedBinding previous) &&
                (!ReferenceEquals(previous.PlayerInput, playerInput) ||
                 !ReferenceEquals(previous.Camera, resolvedCamera)))
            {
                ReleaseApplied(playerSlotId);
            }

            SetOutputPhysicalParticipation(binding, true);

            if (!ReferenceEquals(playerInput.camera, resolvedCamera))
            {
                playerInput.camera = resolvedCamera;
            }

            _applied[playerSlotId] = new AppliedBinding(
                playerInput,
                resolvedCamera);
            return true;
        }

        private void RefreshPhysicalParticipation()
        {
            bool hasAssociatedPlayerOutput =
                _applied.Count > 0;

            // Session Output capacity and Default continuity exist independently
            // from Player count. Before any Player Output association exists, all
            // configured Player-bound Outputs remain physically available so their
            // Default/Route/Session presentation can render. Once at least one exact
            // Player association exists, only currently associated Player-bound
            // Outputs participate physically. Unbound Session Outputs are untouched.
            for (int index = 0;
                 index < _bindings.Bindings.Count;
                 index++)
            {
                PlayerCameraOutputBinding binding =
                    _bindings.Bindings[index];
                bool participating =
                    !hasAssociatedPlayerOutput ||
                    _applied.ContainsKey(
                        binding.PlayerSlotId);
                SetOutputPhysicalParticipation(
                    binding,
                    participating);
            }

            if (!_automaticSplitScreenConfigured ||
                _splitScreenManager == null)
            {
                return;
            }

            // Join is deliberately bracketed while PlayerInput.camera is still being
            // correlated. Do not let PlayerInputManager recompute from a partial set.
            if (_splitScreenJoinTransactionActive)
            {
                if (_splitScreenManager.splitScreen)
                {
                    _splitScreenManager.splitScreen = false;
                }

                return;
            }

            bool shouldSplit = _applied.Count >= 2;
            if (_splitScreenManager.splitScreen != shouldSplit)
            {
                _splitScreenManager.splitScreen = shouldSplit;
                return;
            }

        }

        private void SetOutputPhysicalParticipation(
            PlayerCameraOutputBinding binding,
            bool participating)
        {
            if (!_outputs.TryGetOutput(
                    binding.OutputId,
                    out CameraOutputAuthoring output,
                    out _))
            {
                return;
            }

            UnityEngine.Camera camera = output.UnityCamera;
            if (camera != null && camera.enabled != participating)
            {
                camera.enabled = participating;
            }
        }

        private void ReleaseApplied(PlayerSlotId playerSlotId)
        {
            if (!_applied.TryGetValue(playerSlotId, out AppliedBinding applied))
            {
                return;
            }

            if (applied.PlayerInput != null &&
                ReferenceEquals(applied.PlayerInput.camera, applied.Camera))
            {
                applied.PlayerInput.camera = null;
            }

            _applied.Remove(playerSlotId);
        }
    }
}
