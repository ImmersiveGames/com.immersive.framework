using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Unity target that switches one configured PlayerInput action map from explicit bridge evidence.
    /// It does not toggle PlayerInput.enabled, route InputActions, enable movement, execute gameplay or own lifecycle.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Binding/Unity PlayerInput Activation Target")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52C Unity target for explicit PlayerInput action-map activation.")]
    public sealed class UnityPlayerInputActivationTargetBehaviour : MonoBehaviour, IUnityPlayerInputActivationTarget
    {
        [Tooltip("Human-readable activation target name used in diagnostics only.")]
        [SerializeField] private string activationTargetName = "Unity PlayerInput Activation Target";
        [Tooltip("Expected PlayerSlotId for the explicit PlayerInput activation. This is not a PlayerInput playerIndex.")]
        [SerializeField] private string expectedPlayerSlotId = "player.1";
        [Tooltip("Explicit Unity PlayerInput whose current action map may be switched by F52C.")]
        [SerializeField] private PlayerInput playerInput;
        [Tooltip("Configured action map to switch to when input is activated.")]
        [SerializeField] private string actionMapName = "Gameplay";

        private UnityPlayerInputActivationSnapshot _currentActivation;
        private bool _hasActivation;

        public string ActivationTargetName => activationTargetName.NormalizeTextOrFallback(name);

        public bool HasUnityPlayerInput => playerInput != null;

        public string UnityPlayerInputName => playerInput != null ? playerInput.name.NormalizeText() : string.Empty;

        public string ConfiguredActionMapName => actionMapName.NormalizeText();

        public string CurrentActionMapName => playerInput != null && playerInput.currentActionMap != null ? playerInput.currentActionMap.name.NormalizeText() : string.Empty;

        public bool HasConfiguredActionMapName => !string.IsNullOrEmpty(ConfiguredActionMapName);

        public bool HasUnityPlayerInputActionAsset => playerInput != null && playerInput.actions != null;

        public bool HasConfiguredActionMap => HasUnityPlayerInputActionAsset && playerInput.actions.FindActionMap(ConfiguredActionMapName, false) != null;

        public bool HasUnityPlayerInputActivation => _hasActivation;

        public UnityPlayerInputActivationSnapshot CurrentUnityPlayerInputActivation
        {
            get
            {
                if (!_hasActivation)
                {
                    throw new InvalidOperationException("Unity PlayerInput activation target has no current activation.");
                }

                return _currentActivation;
            }
        }

        public bool BindsView => false;

        public bool BindsControl => _hasActivation && _currentActivation.BindsControl;

        public bool BridgesUnityPlayerInput => _hasActivation && _currentActivation.BridgesUnityPlayerInput;

        public bool ActivatesInput => _hasActivation && _currentActivation.ActivatesInput;

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

        public UnityPlayerInputActivationResult ApplyUnityPlayerInputActivation(
            UnityPlayerInputActivationSnapshot activation,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityPlayerInputActivationTargetBehaviour));
            if (!activation.PlayerSlotId.IsValid || !activation.BindsControl || !activation.BridgesUnityPlayerInput)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.InvalidUnityPlayerInputBridge,
                    activation.PlayerSlotId.IsValid ? activation.PlayerSlotId.StableText : string.Empty,
                    activation.BridgeTargetName,
                    ActivationTargetName,
                    UnityPlayerInputName,
                    ConfiguredActionMapName,
                    activation.PreviousActionMapName,
                    CurrentActionMapName,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput activation target rejected invalid bridge evidence.");
            }

            UnityPlayerInputActivationResult targetValidation = ValidateTargetCanApply(activation, normalizedSource, reason);
            if (targetValidation.Failed || targetValidation.NoOp)
            {
                return targetValidation;
            }

            try
            {
                playerInput.SwitchCurrentActionMap(ConfiguredActionMapName);
            }
            catch (Exception exception)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.ActionMapSwitchFailed,
                    activation.PlayerSlotId.StableText,
                    activation.BridgeTargetName,
                    ActivationTargetName,
                    UnityPlayerInputName,
                    ConfiguredActionMapName,
                    activation.PreviousActionMapName,
                    CurrentActionMapName,
                    normalizedSource,
                    reason,
                    exception.Message);
            }

            _currentActivation = activation;
            _hasActivation = true;
            return UnityPlayerInputActivationResult.Success(
                activation.PlayerSlotId,
                activation.BridgeTargetName,
                ActivationTargetName,
                UnityPlayerInputName,
                ConfiguredActionMapName,
                activation.PreviousActionMapName,
                CurrentActionMapName,
                normalizedSource,
                reason,
                "Unity PlayerInput action map activated on target.");
        }

        public UnityPlayerInputActivationResult ClearUnityPlayerInputActivation(
            PlayerSlotId playerSlotId,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityPlayerInputActivationTargetBehaviour));
            string playerSlotIdText = playerSlotId.IsValid ? playerSlotId.StableText : string.Empty;

            if (!_hasActivation)
            {
                return UnityPlayerInputActivationResult.NoOperation(
                    UnityPlayerInputActivationFailureKind.MissingExistingActivation,
                    playerSlotIdText,
                    string.Empty,
                    ActivationTargetName,
                    UnityPlayerInputName,
                    ConfiguredActionMapName,
                    string.Empty,
                    CurrentActionMapName,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput activation target had no activation to clear.");
            }

            if (_currentActivation.PlayerSlotId != playerSlotId)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.TargetPlayerSlotMismatch,
                    playerSlotIdText,
                    _currentActivation.BridgeTargetName,
                    ActivationTargetName,
                    UnityPlayerInputName,
                    _currentActivation.ActionMapName,
                    _currentActivation.PreviousActionMapName,
                    CurrentActionMapName,
                    normalizedSource,
                    reason,
                    $"Unity PlayerInput activation target is bound to '{_currentActivation.PlayerSlotId.StableText}' and cannot clear '{playerSlotIdText}'.");
            }

            if (!HasUnityPlayerInput)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingUnityPlayerInput,
                    playerSlotIdText,
                    _currentActivation.BridgeTargetName,
                    ActivationTargetName,
                    string.Empty,
                    _currentActivation.ActionMapName,
                    _currentActivation.PreviousActionMapName,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput activation clear requires an explicit Unity PlayerInput.");
            }

            if (!string.IsNullOrEmpty(_currentActivation.PreviousActionMapName))
            {
                if (!HasUnityPlayerInputActionAsset)
                {
                    return UnityPlayerInputActivationResult.Failure(
                        UnityPlayerInputActivationFailureKind.MissingActionAsset,
                        playerSlotIdText,
                        _currentActivation.BridgeTargetName,
                        ActivationTargetName,
                        UnityPlayerInputName,
                        _currentActivation.ActionMapName,
                        _currentActivation.PreviousActionMapName,
                        CurrentActionMapName,
                        normalizedSource,
                        reason,
                        "Unity PlayerInput activation clear requires PlayerInput.actions to restore the previous action map.");
                }

                if (playerInput.actions.FindActionMap(_currentActivation.PreviousActionMapName, false) == null)
                {
                    return UnityPlayerInputActivationResult.Failure(
                        UnityPlayerInputActivationFailureKind.MissingActionMap,
                        playerSlotIdText,
                        _currentActivation.BridgeTargetName,
                        ActivationTargetName,
                        UnityPlayerInputName,
                        _currentActivation.ActionMapName,
                        _currentActivation.PreviousActionMapName,
                        CurrentActionMapName,
                        normalizedSource,
                        reason,
                        $"Unity PlayerInput activation clear could not find previous action map '{_currentActivation.PreviousActionMapName}'.");
                }

                try
                {
                    playerInput.SwitchCurrentActionMap(_currentActivation.PreviousActionMapName);
                }
                catch (Exception exception)
                {
                    return UnityPlayerInputActivationResult.Failure(
                        UnityPlayerInputActivationFailureKind.ActionMapSwitchFailed,
                        playerSlotIdText,
                        _currentActivation.BridgeTargetName,
                        ActivationTargetName,
                        UnityPlayerInputName,
                        _currentActivation.ActionMapName,
                        _currentActivation.PreviousActionMapName,
                        CurrentActionMapName,
                        normalizedSource,
                        reason,
                        exception.Message);
                }
            }

            UnityPlayerInputActivationSnapshot previousActivation = _currentActivation;
            _currentActivation = default;
            _hasActivation = false;
            return UnityPlayerInputActivationResult.Success(
                playerSlotId,
                previousActivation.BridgeTargetName,
                ActivationTargetName,
                UnityPlayerInputName,
                previousActivation.ActionMapName,
                previousActivation.PreviousActionMapName,
                CurrentActionMapName,
                normalizedSource,
                reason,
                "Unity PlayerInput action-map activation cleared from target.",
                false);
        }

        private UnityPlayerInputActivationResult ValidateTargetCanApply(
            UnityPlayerInputActivationSnapshot activation,
            string source,
            string reason)
        {
            if (!HasUnityPlayerInput)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingUnityPlayerInput,
                    activation.PlayerSlotId.StableText,
                    activation.BridgeTargetName,
                    ActivationTargetName,
                    string.Empty,
                    ConfiguredActionMapName,
                    activation.PreviousActionMapName,
                    CurrentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation target requires an explicit Unity PlayerInput.");
            }

            if (!TryGetExpectedPlayerSlotId(out PlayerSlotId expectedSlot))
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingExpectedPlayerSlot,
                    activation.PlayerSlotId.StableText,
                    activation.BridgeTargetName,
                    ActivationTargetName,
                    UnityPlayerInputName,
                    ConfiguredActionMapName,
                    activation.PreviousActionMapName,
                    CurrentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation target requires an expected PlayerSlotId.");
            }

            if (expectedSlot != activation.PlayerSlotId)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.PlayerSlotMismatch,
                    activation.PlayerSlotId.StableText,
                    activation.BridgeTargetName,
                    ActivationTargetName,
                    UnityPlayerInputName,
                    ConfiguredActionMapName,
                    activation.PreviousActionMapName,
                    CurrentActionMapName,
                    source,
                    reason,
                    $"Unity PlayerInput activation target expects '{expectedSlot.StableText}' but activation snapshot targets '{activation.PlayerSlotId.StableText}'.");
            }

            if (!HasConfiguredActionMapName)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingActionMapName,
                    activation.PlayerSlotId.StableText,
                    activation.BridgeTargetName,
                    ActivationTargetName,
                    UnityPlayerInputName,
                    string.Empty,
                    activation.PreviousActionMapName,
                    CurrentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation target requires a configured action map name.");
            }

            if (!string.Equals(activation.ActionMapName, ConfiguredActionMapName, StringComparison.Ordinal))
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingActionMap,
                    activation.PlayerSlotId.StableText,
                    activation.BridgeTargetName,
                    ActivationTargetName,
                    UnityPlayerInputName,
                    ConfiguredActionMapName,
                    activation.PreviousActionMapName,
                    CurrentActionMapName,
                    source,
                    reason,
                    $"Unity PlayerInput activation target action map '{ConfiguredActionMapName}' does not match activation snapshot action map '{activation.ActionMapName}'.");
            }

            if (!HasUnityPlayerInputActionAsset)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingActionAsset,
                    activation.PlayerSlotId.StableText,
                    activation.BridgeTargetName,
                    ActivationTargetName,
                    UnityPlayerInputName,
                    ConfiguredActionMapName,
                    activation.PreviousActionMapName,
                    CurrentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation target requires PlayerInput.actions.");
            }

            if (!HasConfiguredActionMap)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingActionMap,
                    activation.PlayerSlotId.StableText,
                    activation.BridgeTargetName,
                    ActivationTargetName,
                    UnityPlayerInputName,
                    ConfiguredActionMapName,
                    activation.PreviousActionMapName,
                    CurrentActionMapName,
                    source,
                    reason,
                    $"Unity PlayerInput activation target could not find configured action map '{ConfiguredActionMapName}'.");
            }

            return UnityPlayerInputActivationResult.Success(
                activation.PlayerSlotId,
                activation.BridgeTargetName,
                ActivationTargetName,
                UnityPlayerInputName,
                ConfiguredActionMapName,
                activation.PreviousActionMapName,
                CurrentActionMapName,
                source,
                reason,
                "Unity PlayerInput activation target validation passed.");
        }

        internal void ConfigureForDiagnostics(string targetName, string slotId, PlayerInput input, string actionMap)
        {
            activationTargetName = targetName.NormalizeTextOrFallback(name);
            expectedPlayerSlotId = slotId.NormalizeTextOrFallback("player.1");
            playerInput = input;
            actionMapName = actionMap.NormalizeText();
        }

        private void Reset()
        {
            if (string.IsNullOrWhiteSpace(activationTargetName))
            {
                activationTargetName = name;
            }
        }
    }
}
