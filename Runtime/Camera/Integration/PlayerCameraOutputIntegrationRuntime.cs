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
    /// It owns no Player Session, Camera Assignment, Subject or layout geometry.
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
        private PlayerCameraOutputTopology _bindings;
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

        internal bool TrySetTopology(
            PlayerCameraOutputTopology bindings,
            out string diagnostic)
        {
            if (_disposed || bindings == null)
            {
                diagnostic = "Player Camera Output integration requires an active runtime and explicit Assignment topology.";
                return false;
            }

            _bindings = bindings;
            if (!ReconcileAll(out diagnostic))
            {
                LastReconciliationSucceeded = false;
                Diagnostic = diagnostic;
                return false;
            }

            RefreshPhysicalParticipation();
            LastReconciliationSucceeded = true;
            Diagnostic = "Player Camera Output integration was reconciled from the current Session Camera Assignments.";
            return true;
        }

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
            TryDispose(out string issue);
            if (!string.IsNullOrEmpty(issue))
            {
                LastReconciliationSucceeded = false;
                Diagnostic = issue;
            }
        }

        internal bool TryDispose(out string diagnostic)
        {
            diagnostic = string.Empty;
            if (_disposed)
            {
                return true;
            }

            var diagnostics = new List<string>();
            var slots = new List<PlayerSlotId>(_applied.Keys);
            for (int index = 0; index < slots.Count; index++)
            {
                if (!ReleaseApplied(slots[index], out string releaseIssue))
                {
                    diagnostics.Add(releaseIssue);
                }
            }

            for (int index = 0;
                 index < _bindings.Bindings.Count;
                 index++)
            {
                PlayerCameraOutputBinding binding = _bindings.Bindings[index];
                if (!TrySetOutputPhysicalParticipation(
                        binding,
                        false,
                        out string participationIssue))
                {
                    diagnostics.Add(participationIssue);
                }
            }

            if (_automaticSplitScreenConfigured && _splitScreenManager != null)
            {
                try
                {
                    _splitScreenManager.splitScreen = false;
                    if (_splitScreenManager.splitScreen)
                    {
                        diagnostics.Add(
                            "Player Camera Output cleanup could not confirm split-screen restoration.");
                    }
                }
                catch (Exception exception)
                {
                    diagnostics.Add(
                        $"Player Camera Output split-screen restoration remains pending. {exception.GetType().Name}: {exception.Message}");
                }
            }

            if (diagnostics.Count > 0)
            {
                diagnostic = string.Join(" ", diagnostics);
                LastReconciliationSucceeded = false;
                Diagnostic = diagnostic;
                return false;
            }

            _splitScreenJoinTransactionActive = false;
            _splitScreenJoinSlot = default;
            _playerSession.Changed -= OnPlayerSessionChanged;
            _physicalPlayers.SessionPhysicalHostChanged -=
                OnSessionPhysicalHostChanged;
            _disposed = true;
            LastReconciliationSucceeded = true;
            Diagnostic = "Player Camera Output integration was released.";
            diagnostic = string.Empty;
            return true;
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
                return ReleaseApplied(playerSlotId, out issue);
            }

            if (!_playerSession.TryGetSlotSnapshot(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot) ||
                !slot.IsJoined)
            {
                return ReleaseApplied(playerSlotId, out issue);
            }

            if (!_physicalPlayers.TryGetCurrentSessionPhysicalHost(
                    playerSlotId,
                    out LocalPlayerHostAuthoring host,
                    out _))
            {
                // Slot allocation commits before physical Host evidence registration.
                // The dedicated evidence event reconciles the same Slot immediately
                // after registration; no index/order/hierarchy fallback is allowed.
                return ReleaseApplied(playerSlotId, out issue);
            }

            PlayerInput playerInput = host.PlayerInput;
            if (playerInput == null ||
                !host.IsJoined ||
                !host.HasJoinedSlot ||
                host.JoinedPlayerSlotId != playerSlotId)
            {
                if (!ReleaseApplied(playerSlotId, out string releaseIssue))
                {
                    issue = releaseIssue;
                    return false;
                }
                issue =
                    $"Current Session physical Host for Slot '{playerSlotId.StableText}' has no exact valid PlayerInput evidence.";
                return false;
            }

            if (!_outputs.TryGetOutput(
                    binding.OutputId,
                    out CameraOutputAuthoring output,
                    out string outputIssue))
            {
                if (!ReleaseApplied(playerSlotId, out string releaseIssue))
                {
                    issue = releaseIssue;
                    return false;
                }
                issue = outputIssue;
                return false;
            }

            UnityEngine.Camera resolvedCamera = output.UnityCamera;
            if (resolvedCamera == null)
            {
                if (!ReleaseApplied(playerSlotId, out string releaseIssue))
                {
                    issue = releaseIssue;
                    return false;
                }
                issue =
                    $"Camera Output '{binding.OutputId}' has no explicit Unity Camera.";
                return false;
            }

            if (_applied.TryGetValue(playerSlotId, out AppliedBinding previous) &&
                (!ReferenceEquals(previous.PlayerInput, playerInput) ||
                 !ReferenceEquals(previous.Camera, resolvedCamera)))
            {
                if (!ReleaseApplied(playerSlotId, out issue))
                {
                    return false;
                }
            }

            SetCamera(playerInput, resolvedCamera);

            _applied[playerSlotId] = new AppliedBinding(
                playerInput,
                resolvedCamera);
            return true;
        }

        private void RefreshPhysicalParticipation()
        {
            // Assignment reservation and logical Fallback coverage persist
            // independently from physical Camera participation. With no bound
            // Players, retain one deterministic physical Fallback on the first
            // stable Slot binding. Once a Player is bound, only bound Outputs render.
            int emptyFallbackIndex = _applied.Count == 0 &&
                _bindings.Bindings.Count > 0
                ? 0
                : -1;

            RefreshPhysicalOutputParticipation(emptyFallbackIndex);

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
                    RefreshPhysicalOutputParticipation(emptyFallbackIndex);
                }

                return;
            }

            bool shouldSplit = _applied.Count >= 2;
            if (_splitScreenManager.splitScreen != shouldSplit)
            {
                _splitScreenManager.splitScreen = shouldSplit;
                // The Manager owns viewport recomposition. Reassert Framework-owned
                // physical participation after its synchronous layout change.
                RefreshPhysicalOutputParticipation(emptyFallbackIndex);
            }
        }

        private void RefreshPhysicalOutputParticipation(int emptyFallbackIndex)
        {
            // Enable all current Outputs before disabling stale ones so recomposition
            // cannot expose a frame without physical Camera coverage.
            for (int index = 0;
                 index < _bindings.Bindings.Count;
                 index++)
            {
                PlayerCameraOutputBinding binding =
                    _bindings.Bindings[index];
                if (_applied.ContainsKey(binding.PlayerSlotId) ||
                    index == emptyFallbackIndex)
                {
                    SetOutputPhysicalParticipation(binding, true);
                }
            }

            for (int index = 0;
                 index < _bindings.Bindings.Count;
                 index++)
            {
                PlayerCameraOutputBinding binding =
                    _bindings.Bindings[index];
                if (!_applied.ContainsKey(binding.PlayerSlotId) &&
                    index != emptyFallbackIndex)
                {
                    SetOutputPhysicalParticipation(binding, false);
                }
            }
        }

        private void SetOutputPhysicalParticipation(
            PlayerCameraOutputBinding binding,
            bool participating)
        {
            if (!_outputs.TryGetOutput(
                    binding.OutputId,
                    out CameraOutputAuthoring output,
                    out string outputIssue))
            {
                return;
            }

            output.SetPlayerPhysicalParticipation(participating);
        }

        private bool TrySetOutputPhysicalParticipation(
            PlayerCameraOutputBinding binding,
            bool participating,
            out string issue)
        {
            issue = string.Empty;
            if (!_outputs.TryGetOutput(
                    binding.OutputId,
                    out CameraOutputAuthoring output,
                    out issue))
            {
                return false;
            }

            UnityEngine.Camera camera = output.UnityCamera;
            if (camera == null)
            {
                issue =
                    $"Camera Output '{binding.OutputId}' lost its exact Unity Camera while physical participation cleanup was pending.";
                return false;
            }

            try
            {
                output.SetPlayerPhysicalParticipation(participating);
                if (camera.enabled != participating)
                {
                    issue =
                        $"Camera Output '{binding.OutputId}' did not confirm physical participation='{participating}'.";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                issue =
                    $"Camera Output '{binding.OutputId}' physical participation cleanup failed. {exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        private bool ReleaseApplied(
            PlayerSlotId playerSlotId,
            out string issue)
        {
            issue = string.Empty;
            if (!_applied.TryGetValue(playerSlotId, out AppliedBinding applied))
            {
                return true;
            }

            try
            {
                if (applied.PlayerInput != null &&
                    ReferenceEquals(applied.PlayerInput.camera, applied.Camera))
                {
                    applied.PlayerInput.camera = null;
                }

                if (applied.PlayerInput != null &&
                    ReferenceEquals(applied.PlayerInput.camera, applied.Camera))
                {
                    issue =
                        $"Player Camera Output cleanup still owns the exact Camera assignment for Slot '{playerSlotId.StableText}'.";
                    return false;
                }
            }
            catch (Exception exception)
            {
                issue =
                    $"Player Camera Output cleanup for Slot '{playerSlotId.StableText}' remains pending. {exception.GetType().Name}: {exception.Message}";
                return false;
            }

            _applied.Remove(playerSlotId);
            return true;
        }

        internal static void SetCamera(
            PlayerInput playerInput,
            UnityEngine.Camera camera)
        {
            if (playerInput != null && !ReferenceEquals(playerInput.camera, camera))
            {
                playerInput.camera = camera;
            }
        }

        internal static void ClearCamera(
            PlayerInput playerInput,
            UnityEngine.Camera ownedCamera)
        {
            if (playerInput != null &&
                ReferenceEquals(playerInput.camera, ownedCamera))
            {
                playerInput.camera = null;
            }
        }
    }
}
