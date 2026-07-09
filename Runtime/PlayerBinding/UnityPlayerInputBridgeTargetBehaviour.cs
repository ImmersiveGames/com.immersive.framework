using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Unity target that stores explicit bridge evidence between PlayerControl and PlayerInput.
    /// It does not enable PlayerInput, switch action maps, route InputActions, enable movement or execute gameplay control.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Binding/Unity PlayerInput Bridge Target")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52B Unity target for explicit PlayerInput bridge evidence.")]
    public sealed class UnityPlayerInputBridgeTargetBehaviour : MonoBehaviour, IUnityPlayerInputBridgeTarget
    {
        [Tooltip("Human-readable bridge target name used in diagnostics only.")]
        [SerializeField] private string bridgeTargetName = "Unity PlayerInput Bridge Target";
        [Tooltip("Expected PlayerSlotId for the explicit PlayerInput bridge. This is not a PlayerInput playerIndex.")]
        [SerializeField] private string expectedPlayerSlotId = "player.1";
        [Tooltip("Explicit Unity PlayerInput used as bridge evidence. This component is not enabled, disabled, switched or driven by F52B.")]
        [SerializeField] private PlayerInput playerInput;

        private UnityPlayerInputBridgeSnapshot _currentBridge;
        private bool _hasBridge;

        public string BridgeTargetName => bridgeTargetName.NormalizeTextOrFallback(name);

        public bool HasUnityPlayerInput => playerInput != null;

        public string UnityPlayerInputName => playerInput != null ? playerInput.name.NormalizeText() : string.Empty;

        public bool HasUnityPlayerInputBridge => _hasBridge;

        public UnityPlayerInputBridgeSnapshot CurrentUnityPlayerInputBridge
        {
            get
            {
                if (!_hasBridge)
                {
                    throw new InvalidOperationException("Unity PlayerInput bridge target has no current bridge.");
                }

                return _currentBridge;
            }
        }

        public bool BindsView => false;

        public bool BindsControl => _hasBridge && _currentBridge.BindsControl;

        public bool BridgesUnityPlayerInput => _hasBridge && _currentBridge.BridgesUnityPlayerInput;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public bool TryGetExpectedPlayerSlotId(out PlayerSlotId playerSlotId)
        {
            playerSlotId = default;
            string normalized = expectedPlayerSlotId.NormalizeText();
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            try
            {
                playerSlotId = PlayerSlotId.From(normalized);
                return playerSlotId.IsValid;
            }
            catch (ArgumentException)
            {
                playerSlotId = default;
                return false;
            }
        }

        public UnityPlayerInputBridgeResult ApplyUnityPlayerInputBridge(
            UnityPlayerInputBridgeSnapshot bridge,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityPlayerInputBridgeTargetBehaviour));
            if (!bridge.PlayerSlotId.IsValid || !bridge.BindsControl)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.InvalidPlayerControlBinding,
                    bridge.PlayerSlotId.IsValid ? bridge.PlayerSlotId.StableText : string.Empty,
                    bridge.ControlBindingTargetName,
                    BridgeTargetName,
                    UnityPlayerInputName,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput bridge target rejected invalid PlayerControl binding evidence.");
            }

            if (!HasUnityPlayerInput)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.MissingUnityPlayerInput,
                    bridge.PlayerSlotId.StableText,
                    bridge.ControlBindingTargetName,
                    BridgeTargetName,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput bridge target requires an explicit Unity PlayerInput.");
            }

            if (!TryGetExpectedPlayerSlotId(out PlayerSlotId expectedSlot))
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.MissingExpectedPlayerSlot,
                    bridge.PlayerSlotId.StableText,
                    bridge.ControlBindingTargetName,
                    BridgeTargetName,
                    UnityPlayerInputName,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput bridge target requires an expected PlayerSlotId.");
            }

            if (expectedSlot != bridge.PlayerSlotId)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.PlayerSlotMismatch,
                    bridge.PlayerSlotId.StableText,
                    bridge.ControlBindingTargetName,
                    BridgeTargetName,
                    UnityPlayerInputName,
                    normalizedSource,
                    reason,
                    $"Unity PlayerInput bridge target expects '{expectedSlot.StableText}' but bridge snapshot targets '{bridge.PlayerSlotId.StableText}'.");
            }

            _currentBridge = bridge;
            _hasBridge = true;
            return UnityPlayerInputBridgeResult.Success(
                bridge.PlayerSlotId,
                bridge.ControlBindingTargetName,
                BridgeTargetName,
                UnityPlayerInputName,
                normalizedSource,
                reason,
                "Unity PlayerInput bridge evidence stored on target.");
        }

        public UnityPlayerInputBridgeResult ClearUnityPlayerInputBridge(
            PlayerSlotId playerSlotId,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityPlayerInputBridgeTargetBehaviour));
            string playerSlotIdText = playerSlotId.IsValid ? playerSlotId.StableText : string.Empty;

            if (!_hasBridge)
            {
                return UnityPlayerInputBridgeResult.NoOperation(
                    UnityPlayerInputBridgeFailureKind.MissingExistingBridge,
                    playerSlotIdText,
                    string.Empty,
                    BridgeTargetName,
                    UnityPlayerInputName,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput bridge target had no bridge to clear.");
            }

            if (_currentBridge.PlayerSlotId != playerSlotId)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.TargetPlayerSlotMismatch,
                    playerSlotIdText,
                    _currentBridge.ControlBindingTargetName,
                    BridgeTargetName,
                    UnityPlayerInputName,
                    normalizedSource,
                    reason,
                    $"Unity PlayerInput bridge target is bound to '{_currentBridge.PlayerSlotId.StableText}' and cannot clear '{playerSlotIdText}'.");
            }

            string controlBindingTargetName = _currentBridge.ControlBindingTargetName;
            _currentBridge = default;
            _hasBridge = false;
            return UnityPlayerInputBridgeResult.Success(
                playerSlotId,
                controlBindingTargetName,
                BridgeTargetName,
                UnityPlayerInputName,
                normalizedSource,
                reason,
                "Unity PlayerInput bridge evidence cleared from target.",
                false);
        }

        internal void ConfigureForDiagnostics(string targetName, string slotId, PlayerInput input)
        {
            bridgeTargetName = targetName.NormalizeTextOrFallback(name);
            expectedPlayerSlotId = slotId.NormalizeTextOrFallback("player.1");
            playerInput = input;
        }

        private void Reset()
        {
            if (string.IsNullOrWhiteSpace(bridgeTargetName))
            {
                bridgeTargetName = name;
            }
        }
    }
}
