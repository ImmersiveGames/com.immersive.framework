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
            CompleteSplitScreenJoinTransaction();
            _playerSession.Changed -= OnPlayerSessionChanged;
            _physicalPlayers.SessionPhysicalHostChanged -=
                OnSessionPhysicalHostChanged;

            var slots = new List<PlayerSlotId>(_applied.Keys);
            for (int index = 0; index < slots.Count; index++)
            {
                ReleaseApplied(slots[index]);
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
            if (_splitScreenManager == null ||
                !_splitScreenManager.splitScreen)
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
            _splitScreenManager.splitScreen = false;
        }

        private void CompleteSplitScreenJoinTransaction()
        {
            if (!_splitScreenJoinTransactionActive)
            {
                return;
            }

            _splitScreenJoinTransactionActive = false;
            _splitScreenJoinSlot = default;
            if (_splitScreenManager != null &&
                !_splitScreenManager.splitScreen)
            {
                _splitScreenManager.splitScreen = true;
            }
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
                return true;
            }

            PlayerInput playerInput = host.PlayerInput;
            if (playerInput == null ||
                !host.IsJoined ||
                !host.HasJoinedSlot ||
                host.JoinedPlayerSlotId != playerSlotId)
            {
                ReleaseApplied(playerSlotId);
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

            if (!ReferenceEquals(playerInput.camera, resolvedCamera))
            {
                playerInput.camera = resolvedCamera;
            }

            _applied[playerSlotId] = new AppliedBinding(
                playerInput,
                resolvedCamera);
            return true;
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
